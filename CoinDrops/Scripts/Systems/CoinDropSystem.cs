using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace CoinDrops.Systems
{
    /// <summary>
    /// Server side. When an enemy (EnemyCD) is killed by a player (EntityDestroyedCD enabled, health 0,
    /// KilledByPlayerCD enabled, or a player hit it within the last
    /// 20 s per PlayerHitTrackerSystem: burn/poison finishes carry no attacker), drops
    /// sqrt(maxHealth) x AmountMultiplier Ancient Coins (x0.8..1.2, x BossMultiplier for BossCD, at least 1)
    /// at the body, pulled towards the killing player like vanilla loot. Same death hook as
    /// BossBonusLoot. Each entity is handled once; the first death of every enemy type is logged
    /// with its HP so the rate can be tuned.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class CoinDropSystem : PugSimulationSystemBase
    {
        private const float Spread = 0.2f;

        private static readonly System.Random Rng = new System.Random();

        private readonly HashSet<Entity> _handled = new HashSet<Entity>();
        private readonly HashSet<ObjectID> _logged = new HashSet<ObjectID>();
        private readonly HashSet<ObjectID> _skipLogged = new HashSet<ObjectID>();
        private System.Predicate<Entity> _gone; // cached so pruning allocates nothing
        private EntityQuery _deadEnemies;
        private EntityQuery _database;
        private PlayerHitTrackerSystem _hits;

        protected override void OnCreate()
        {
            _deadEnemies = GetEntityQuery(
                ComponentType.ReadOnly<EnemyCD>(),
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<HealthCD>(),
                ComponentType.ReadOnly<EntityDestroyedCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _gone = e => !EntityManager.Exists(e);
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            float amount = CoinDropsMod.AmountMultiplier;
            if (amount <= 0f || _deadEnemies.IsEmpty || _database.IsEmpty)
            {
                // Nothing dying: forget paid entities that have been destroyed (only does work after kills).
                if (_handled.Count > 0) _handled.RemoveWhere(_gone);
                base.OnUpdate();
                return;
            }
            // Long fights never reach the quiet branch; keep the set bounded anyway.
            if (_handled.Count > 256) _handled.RemoveWhere(_gone);

            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            var entities = _deadEnemies.ToEntityArray(Allocator.Temp);
            var objects = _deadEnemies.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            var healths = _deadEnemies.ToComponentDataArray<HealthCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (healths[i].health > 0 || _handled.Contains(entities[i])) continue;
                _handled.Add(entities[i]);

                // Player kills only. The game sets KilledByPlayerCD when the killing blow traces back to a
                // player through owner references (the player, their projectiles, bombs, minions, pets);
                // traps, enemy-on-enemy and environment kills leave it off.
                // Damage over time (burn, poison, bleed...) carries no attacker, so a burn finish never sets
                // KilledByPlayerCD; fall back to "a player (or their minion/pet/projectile) hit it recently".
                Entity killer;
                bool credited = EntityManager.HasComponent<KilledByPlayerCD>(entities[i])
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entities[i]);
                if (credited)
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entities[i]).playerEntity;
                else
                {
                    if (_hits == null) _hits = World.GetExistingSystemManaged<PlayerHitTrackerSystem>();
                    if (_hits == null || !_hits.TryGetRecentHitter(entities[i], out killer)) killer = Entity.Null;
                }
                if (killer == Entity.Null && !credited)
                {
                    if (_skipLogged.Add(objects[i].objectID))
                        Debug.Log($"[{CoinDropsMod.Name}] {objects[i].objectID} died without a player kill: no coins");
                    continue;
                }
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null; // still pays, just no pull

                bool boss = EntityManager.HasComponent<BossCD>(entities[i]);
                int coins = Roll(healths[i].maxHealth, amount * (boss ? CoinDropsMod.BossMultiplier : 1));

                float3 pos = EntityManager.GetComponentData<LocalTransform>(entities[i]).Position;
                EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
                {
                    objectData = new ObjectDataCD { objectID = ObjectID.AncientCoin, amount = coins, variation = 0 }
                }, pos, bank, killer);

                if (boss || _logged.Add(objects[i].objectID))
                    Debug.Log($"[{CoinDropsMod.Name}] {objects[i].objectID} (max HP {healths[i].maxHealth}"
                              + (boss ? ", boss" : "") + $") dropped {coins} coins");
            }
            entities.Dispose();
            objects.Dispose();
            healths.Dispose();
            base.OnUpdate();
        }

        /// <summary>sqrt(maxHealth) x mult, randomised by +-Spread; the fraction rounds up by chance; at least 1.</summary>
        private static int Roll(int maxHealth, float mult)
        {
            double expected = System.Math.Sqrt(math.max(0, maxHealth)) * mult * (1.0 + (Rng.NextDouble() * 2.0 - 1.0) * Spread);
            int coins = (int)expected;
            if (Rng.NextDouble() < expected - coins) coins++;
            return math.max(1, coins);
        }
    }
    /// <summary>
    /// Server side, right before the game applies the HealthChangeBuffer: remembers which player last damaged each
    /// enemy (directly or through a projectile, bomb, minion or pet, via the OwnerReferenceCD chain). CoinDropSystem
    /// uses it for deaths the game does not credit to a player, i.e. damage-over-time finishes (burn, poison, bleed),
    /// whose health changes have no causedByEntity. Bounded: stale entries are pruned once the map grows.
    /// </summary>
    [UpdateInGroup(typeof(UpdateHealthSystemGroup))]
    [UpdateBefore(typeof(UpdateHealthFromBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class PlayerHitTrackerSystem : PugSimulationSystemBase
    {
        /// <summary>How long after a player's last hit a death still pays (burn/poison run out well within this).</summary>
        private const double Window = 20.0;

        private struct Hit { public Entity Player; public double Time; }

        private readonly Dictionary<Entity, Hit> _lastHit = new Dictionary<Entity, Hit>();
        private readonly List<Entity> _scratch = new List<Entity>();
        private EntityQuery _healthChanges;

        protected override void OnCreate()
        {
            _healthChanges = GetEntityQuery(ComponentType.ReadOnly<HealthChangeBuffer>(), ComponentType.Exclude<ObjectDataCD>());
            base.OnCreate();
        }

        /// <summary>The player who damaged <paramref name="enemy"/> within the window, if any.</summary>
        internal bool TryGetRecentHitter(Entity enemy, out Entity player)
        {
            if (_lastHit.TryGetValue(enemy, out var hit) && World.Time.ElapsedTime - hit.Time <= Window)
            {
                player = hit.Player;
                return true;
            }
            player = Entity.Null;
            return false;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CoinDropsMod.AmountMultiplier <= 0f || _healthChanges.IsEmpty) return;
            double now = World.Time.ElapsedTime;
            if (_lastHit.Count > 512) Prune(now);

            var changes = EntityManager.GetBuffer<HealthChangeBuffer>(_healthChanges.GetSingletonEntity(), true);
            for (int i = 0; i < changes.Length; i++)
            {
                var hc = changes[i].healthChange;
                if (hc.amount >= 0 || hc.causedByEntity == Entity.Null || !EntityManager.HasComponent<EnemyCD>(hc.entity)) continue;
                Entity player = PlayerOwner(hc.causedByEntity);
                if (player != Entity.Null) _lastHit[hc.entity] = new Hit { Player = player, Time = now };
            }
        }

        /// <summary>Same walk as the game's EntityUtility.GetPlayerOwner (entity part -> owner chain), capped at 10 links.</summary>
        private Entity PlayerOwner(Entity attacker)
        {
            if (!EntityManager.Exists(attacker)) return Entity.Null;
            if (EntityManager.HasComponent<EntityPartCD>(attacker)) attacker = EntityManager.GetComponentData<EntityPartCD>(attacker).mainEntity;
            for (int n = 0; n < 10 && attacker != Entity.Null && EntityManager.Exists(attacker); n++)
            {
                if (EntityManager.HasComponent<PlayerGhost>(attacker)) return attacker;
                if (!EntityManager.HasComponent<OwnerReferenceCD>(attacker)) break;
                attacker = EntityManager.GetComponentData<OwnerReferenceCD>(attacker).owner;
            }
            return Entity.Null;
        }

        private void Prune(double now)
        {
            _scratch.Clear();
            foreach (var kv in _lastHit)
                if (now - kv.Value.Time > Window || !EntityManager.Exists(kv.Key)) _scratch.Add(kv.Key);
            for (int i = 0; i < _scratch.Count; i++) _lastHit.Remove(_scratch[i]);
        }
    }
}
