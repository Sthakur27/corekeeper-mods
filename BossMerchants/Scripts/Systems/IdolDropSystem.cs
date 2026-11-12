using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Server side. When a catalog boss dies (EntityDestroyedCD present, health 0) it drops its merchant's idol
    /// at the body, pulled towards the killing player like vanilla loot (same pattern as Boss Bonus Loot).
    /// Every kill drops one, so a lost or second idol (another base) is always one fight away.
    /// The query is BossCD-only, so ordinary enemies dying never wake it.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class IdolDropSystem : PugSimulationSystemBase
    {
        private readonly HashSet<Entity> _handled = new HashSet<Entity>();
        private System.Predicate<Entity> _isGone; // cached: a lambda here would allocate every tick
        private EntityQuery _deadBosses;
        private EntityQuery _database;

        protected override void OnCreate()
        {
            _deadBosses = GetEntityQuery(
                ComponentType.ReadOnly<BossCD>(),
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<HealthCD>(),
                ComponentType.ReadOnly<EntityDestroyedCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _isGone = e => !EntityManager.Exists(e);
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (_handled.Count > 0) _handled.RemoveWhere(_isGone);
            // IsEmpty (not IsEmptyIgnoreFilter): EntityDestroyedCD is enableable, and living bosses carry it disabled.
            if (_deadBosses.IsEmpty || _database.IsEmpty)
            {
                base.OnUpdate();
                return;
            }

            Catalog.Resolve();
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            var entities = _deadBosses.ToEntityArray(Allocator.Temp);
            var objects = _deadBosses.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var merchant = Catalog.ByBoss(objects[i].objectID);
                if (merchant == null || merchant.IdolId == ObjectID.None) continue;
                if (_handled.Contains(entities[i])) continue;
                if (EntityManager.GetComponentData<HealthCD>(entities[i]).health > 0) continue;
                _handled.Add(entities[i]);

                Entity killer = Entity.Null;
                if (EntityManager.HasComponent<KilledByPlayerCD>(entities[i])
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entities[i]))
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entities[i]).playerEntity;
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null;

                var pos = EntityManager.GetComponentData<LocalTransform>(entities[i]).Position;
                EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
                {
                    objectData = new ObjectDataCD { objectID = merchant.IdolId, amount = 1, variation = 0 }
                }, pos, bank, killer);
                Debug.Log($"[{BossMerchantsMod.Name}] {objects[i].objectID} died: dropped the {merchant.Key} merchant idol.");
            }
            entities.Dispose();
            objects.Dispose();
            base.OnUpdate();
        }
    }
}
