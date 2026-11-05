using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace BoulderMover.Systems
{
    /// <summary>
    /// Runs right before UpdateHealthFromBufferSystem, which applies the HealthChangeBuffer singleton.
    /// The client predicts that system too (its own hit would show a phantom ore drop), so the client world also
    /// cancels boulder damage; the pickup meter, the pickup and the reduction fix run on the server only.
    /// For every queued change to an ore boulder:
    ///  - drill damage (bypassDamageReduction) is left alone, so drills drop ore exactly as in vanilla;
    ///  - any other damage is cancelled, so mining, explosions and weapons never drop ore;
    ///  - a player's own hit adds what a Maze Block would have taken (damage minus the Maze Block's reduction) to the
    ///    boulder's pickup meter. At MazeBlock.PickupEffort (3 Maze Blocks' health) the boulder is killed without loot and dropped as an item,
    ///    pulled to that player.
    /// Only untouched boulders (less than one ore's worth of damage taken) can be picked up: the item places a fresh
    /// boulder, so moving a half-drilled one would refill it. Infinite Ore Boulders keeps every boulder within one ore step.
    /// The "Move drilled boulders" setting (default off) lifts that limit.
    /// Also keeps live boulders at the Maze Block's damage reduction (prefab change in BoulderMoverMod covers new ones;
    /// this covers saves and a late-read Maze Block value). DamageReductionCD.reduction is a ghost field, so clients follow.
    /// </summary>
    [UpdateInGroup(typeof(UpdateHealthSystemGroup))]
    [UpdateBefore(typeof(UpdateHealthFromBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class BoulderPickupSystem : PugSimulationSystemBase
    {
        private const int ReductionCheckInterval = 120; // ticks

        private EntityQuery _healthChanges;
        private EntityQuery _boulders;
        private EntityQuery _database;
        private EntityQuery _lootObjects; // same shape IsBoulder checks: every object that can be an ore boulder
        private int _lootOrderVersion = -1;
        private bool _anyOreBoulder;
        private readonly Dictionary<Entity, int> _progress = new Dictionary<Entity, int>();
        private int _tick;

        protected override void OnCreate()
        {
            base.OnCreate();
            _healthChanges = GetEntityQuery(
                ComponentType.ReadWrite<HealthChangeBuffer>(),
                ComponentType.Exclude<ObjectDataCD>());
            _boulders = GetEntityQuery(
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<DropsLootWhenDamagedCD>(),
                ComponentType.ReadWrite<DamageReductionCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _lootObjects = GetEntityQuery(
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<DropsLootWhenDamagedCD>());
        }

        /// <summary>
        /// True while any ore boulder is loaded in this world. Recounted only when an entity with
        /// DropsLootWhenDamagedCD is created/destroyed or changes archetype (order version), so the
        /// usual tick costs one int compare instead of reading the HealthChangeBuffer singleton.
        /// </summary>
        private bool AnyOreBoulderLoaded()
        {
            int version = EntityManager.GetComponentOrderVersion<DropsLootWhenDamagedCD>();
            if (version == _lootOrderVersion) return _anyOreBoulder;
            _lootOrderVersion = version;
            _anyOreBoulder = false;
            if (_lootObjects.IsEmpty) return false;
            var objects = _lootObjects.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < objects.Length; i++)
            {
                if (!MazeBlock.IsOreBoulder(objects[i].objectID)) continue;
                _anyOreBoulder = true;
                break;
            }
            objects.Dispose();
            return _anyOreBoulder;
        }

        protected override void OnUpdate()
        {
            bool server = World.IsServer();
            if (server && ++_tick % ReductionCheckInterval == 0)
            {
                KeepMazeReduction();
                if (_progress.Count > 0)
                {
                    var gone = new List<Entity>();
                    foreach (var e in _progress.Keys)
                        if (!EntityManager.Exists(e)) gone.Add(e);
                    foreach (var e in gone) _progress.Remove(e);
                }
            }

            // The HealthChangeBuffer holder is a singleton that always exists, so its IsEmpty never skips;
            // gate on "any ore boulder loaded" first (cheap, cached on the order version).
            if (!AnyOreBoulderLoaded() || _healthChanges.IsEmpty) return;
            var holder = _healthChanges.GetSingletonEntity();
            // Read-only first: a read-write GetBuffer waits for every job writing health changes,
            // and most ticks nothing touches a boulder.
            if (!TouchesBoulder(EntityManager.GetBuffer<HealthChangeBuffer>(holder, true))) return;

            var changes = EntityManager.GetBuffer<HealthChangeBuffer>(holder);
            int count = changes.Length; // kill entries appended below are not re-processed
            for (int i = 0; i < count; i++)
            {
                var entry = changes[i];
                var hc = entry.healthChange;
                if (hc.bypassDamageReduction || (hc.amount >= 0 && !hc.wasKilled)) continue;
                if (!IsBoulder(hc.entity)) continue;

                int damage = -hc.amount;
                hc.amount = 0;
                hc.wasKilled = false;
                entry.healthChange = hc;
                changes[i] = entry;

                int counted = damage - MazeBlock.Reduction;
                if (counted <= 0) continue;
                // Cancelled damage skips the game's hit effect, so play it here: a hit that counts still wiggles the boulder.
                PlayHitEffect(hc.entity, hc.causedByEntity, hc.wasKnockedBack);

                if (!server || hc.causedByEntity == Entity.Null || !EntityManager.HasComponent<PlayerGhost>(hc.causedByEntity)) continue;

                var health = EntityManager.GetComponentData<HealthCD>(hc.entity);
                // Less than one ore's worth of damage counts as untouched: Infinite Ore Boulders tops boulders up
                // in whole ore steps, so a drilled one sits up to one step below max.
                int step = EntityManager.HasComponent<DropsLootWhenDamagedCD>(hc.entity)
                    ? EntityManager.GetComponentData<DropsLootWhenDamagedCD>(hc.entity).damageToDealToDropLoot : 0;
                if (health.health <= 0) continue;
                if (!BoulderMoverMod.MoveDrilled && health.maxHealth - health.health >= System.Math.Max(step, 1)) continue;

                _progress.TryGetValue(hc.entity, out int meter);
                meter += counted;
                if (meter < MazeBlock.PickupEffort)
                {
                    _progress[hc.entity] = meter;
                    continue;
                }
                _progress.Remove(hc.entity);
                PickUp(hc.entity, hc.causedByEntity, health.health);
                changes = EntityManager.GetBuffer<HealthChangeBuffer>(holder); // PickUp may have structurally changed things
            }
        }

        /// <summary>
        /// Same effect UpdateHealthFromBufferSystem.DoDamageEffects queues for a hit that lands: the damage trigger on the
        /// boulder plus a PlayDamageEffect event in the attacker's (else the boulder's) ghost effect ring buffer.
        /// </summary>
        private void PlayHitEffect(Entity boulder, Entity attacker, bool knockedBack)
        {
            if (EntityManager.HasComponent<DamageTakenTriggerCD>(boulder))
            {
                EntityManager.SetComponentEnabled<DamageTakenTriggerCD>(boulder, true);
                EntityManager.SetComponentData(boulder, new DamageTakenTriggerCD { skipRequestTookDamageState = true });
            }
            if (!EntityManager.HasComponent<DamageEffectCD>(boulder)) return;
            Entity target = attacker != Entity.Null && EntityManager.HasBuffer<GhostEffectEventBuffer>(attacker) ? attacker
                : EntityManager.HasBuffer<GhostEffectEventBuffer>(boulder) ? boulder : Entity.Null;
            if (target == Entity.Null || !EntityManager.HasComponent<GhostEffectEventBufferPointerCD>(target)) return;

            var health = EntityManager.GetComponentData<HealthCD>(boulder);
            var buffer = EntityManager.GetBuffer<GhostEffectEventBuffer>(target);
            var pointer = EntityManager.GetComponentData<GhostEffectEventBufferPointerCD>(target);
            var item = new GhostEffectEventBuffer
            {
                Tick = GetServerTick(),
                value = new EffectEventCD
                {
                    entity = boulder,
                    effectID = EffectID.PlayDamageEffect,
                    value1 = knockedBack ? 1 : 0,
                    value2 = health.health == health.maxHealth ? 1 : 0,
                }
            };
            buffer.AddToRingBuffer(ref pointer, in item);
            EntityManager.SetComponentData(target, pointer);
        }

        private bool IsBoulder(Entity e)
        {
            return EntityManager.HasComponent<DropsLootWhenDamagedCD>(e)
                && EntityManager.HasComponent<ObjectDataCD>(e)
                && MazeBlock.IsOreBoulder(EntityManager.GetComponentData<ObjectDataCD>(e).objectID);
        }

        private bool TouchesBoulder(DynamicBuffer<HealthChangeBuffer> changes)
        {
            for (int i = 0; i < changes.Length; i++)
            {
                var hc = changes[i].healthChange;
                if (hc.bypassDamageReduction || (hc.amount >= 0 && !hc.wasKilled)) continue;
                if (IsBoulder(hc.entity)) return true;
            }
            return false;
        }

        /// <summary>
        /// Drops the boulder as an item (pulled to the player) and queues a loot-free kill, which the game's own health
        /// system turns into a normal destroy. skipLootDropOnDestroy also stops the ore-per-damage drop.
        /// </summary>
        private void PickUp(Entity boulder, Entity player, int health)
        {
            if (_database.IsEmpty) return;
            var id = EntityManager.GetComponentData<ObjectDataCD>(boulder).objectID;
            float3 pos = EntityManager.GetComponentData<LocalTransform>(boulder).Position;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;

            var holder = _healthChanges.GetSingletonEntity();
            EntityManager.GetBuffer<HealthChangeBuffer>(holder).Add(new HealthChangeBuffer
            {
                healthChange = new HealthChange
                {
                    entity = boulder,
                    causedByEntity = player,
                    amount = -health,
                    wasKilled = true,
                    bypassDamageReduction = true,
                    bypassMaxDamagePerHit = true,
                    skipLootDropOnDestroy = true,
                }
            });
            EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
            {
                objectData = new ObjectDataCD { objectID = id, amount = 1, variation = 0 }
            }, pos, bank, player);
            Debug.Log($"[{BoulderMoverMod.Name}] {id} picked up.");
        }

        private void KeepMazeReduction()
        {
            if (_boulders.IsEmpty) return;
            var entities = _boulders.ToEntityArray(Allocator.Temp);
            var objects = _boulders.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!MazeBlock.IsOreBoulder(objects[i].objectID)) continue;
                var reduction = EntityManager.GetComponentData<DamageReductionCD>(entities[i]);
                if (reduction.reduction == MazeBlock.Reduction) continue;
                reduction.reduction = MazeBlock.Reduction;
                EntityManager.SetComponentData(entities[i], reduction);
            }
            entities.Dispose();
            objects.Dispose();
        }
    }
}
