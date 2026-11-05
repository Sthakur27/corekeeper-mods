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
    /// Server side. When an enemy (EnemyCD) dies (EntityDestroyedCD enabled, health 0), drops
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
        private System.Predicate<Entity> _gone; // cached so pruning allocates nothing
        private EntityQuery _deadEnemies;
        private EntityQuery _database;

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

                bool boss = EntityManager.HasComponent<BossCD>(entities[i]);
                int coins = Roll(healths[i].maxHealth, amount * (boss ? CoinDropsMod.BossMultiplier : 1));

                Entity killer = Entity.Null;
                if (EntityManager.HasComponent<KilledByPlayerCD>(entities[i])
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entities[i]))
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entities[i]).playerEntity;
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null;

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
}
