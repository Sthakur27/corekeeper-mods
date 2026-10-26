using System;
using System.Collections.Generic;
using Inventory;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ConnectedStorage
{
    public static class StorageRemoval
    {
        public static bool IsEvacuating(World world, Entity chest)
        {
            if (world == null || !world.IsCreated) return false;
            var system = world.GetExistingSystemManaged<StorageRemovalSystem>();
            return system != null && system.ActiveChest == chest && chest != Entity.Null;
        }

        public static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (!em.HasComponent<ObjectDataCD>(entity) ||
                !StorageCatalog.IsChest(em.GetComponentData<ObjectDataCD>(entity).objectID)) return;
            if (!em.HasComponent<DontDestroyOnZeroHealthCD>(entity))
                em.AddComponentData(entity, new DontDestroyOnZeroHealthCD());
            else em.SetComponentData(entity, new DontDestroyOnZeroHealthCD());
        }
    }

    // Vanilla's Burst destruction job honors this prefab component. Never patch that job, and
    // enable destruction only after synchronous transfers leave leftovers safe for vanilla drops.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateHealthSystemGroup))]
    [UpdateBefore(typeof(SetEntitiesDestroyedSystem))]
    public partial class StorageRemovalSystem : PugSimulationSystemBase
    {
        public Entity ActiveChest { get; private set; }
        private EntityQuery chests;
        private readonly List<Entity> broken = new List<Entity>();
        private double nextFullScan;

        protected override void OnCreate()
        {
            base.OnCreate();
            chests = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<HealthCD>(), ComponentType.ReadOnly<DontDestroyOnZeroHealthCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>());
            // Only chunks whose health was written since our last update can hold a newly broken chest.
            chests.SetChangedVersionFilter(ComponentType.ReadOnly<HealthCD>());
            // RequireForUpdate (not an early return) keeps the change version from being consumed while idle.
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<InventoryChangeBuffer>()));
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>()));
        }

        protected override void OnUpdate()
        {
            // All state belongs to this ECS world; unload/rejoin cannot leave stale entity IDs.
            if (ActiveChest != Entity.Null && !EntityManager.Exists(ActiveChest)) ActiveChest = Entity.Null;
            // Safety net: an unfiltered scan every few seconds catches any health write that skipped versioning.
            bool full = World.Time.ElapsedTime >= nextFullScan;
            if (full) { nextFullScan = World.Time.ElapsedTime + 5; chests.ResetFilter(); }
            if (!chests.IsEmpty) FindBroken();
            if (full) chests.SetChangedVersionFilter(ComponentType.ReadOnly<HealthCD>());
            while (ActiveChest == Entity.Null && broken.Count > 0)
            {
                var e = broken[0];
                broken.RemoveAt(0);
                if (IsBroken(e)) ActiveChest = e;
            }
            if (ActiveChest == Entity.Null) return;
            EntityManager.CompleteAllTrackedJobs();
            var source = EntityManager.GetBuffer<ContainedObjectsBuffer>(ActiveChest);
            bool empty = true;
            for (int i = 0; i < source.Length; i++)
                if (source[i].objectID != ObjectID.None) { empty = false; break; }
            var before = StorageNetwork.Scan(World, ActiveChest);
            if (before.Terminals.Count == 0)
            {
                Finish();
                return;
            }
            before.Terminals.Sort((a, b) => a.Index.CompareTo(b.Index));
            var after = StorageNetwork.Scan(World, before.Terminals[0], ActiveChest);
            if (empty) { Finish(); return; }
            // Resolve and execute on the same server tick. A queued destination
            // could otherwise change before MoveAmount searches for a fallback.
            var transfers=World.GetOrCreateSystemManaged<StorageTransferSystem>();
            if(transfers.HasPendingChanges(ActiveChest,before.Chests))return;
            int sourceCount=source.Length;
            for(int i=0;i<sourceCount;i++) {
                // Playback can invalidate DynamicBuffer references.
                source=EntityManager.GetBuffer<ContainedObjectsBuffer>(ActiveChest);
                if(source[i].objectID!=ObjectID.None)
                    transfers.Evacuate(ActiveChest,i,after.Chests);
            }
            Finish(); // Vanilla drops everything that did not fit, including aux data.
        }

        private void Finish()
        {
            EntityManager.SetComponentData(ActiveChest, new DontDestroyOnZeroHealthCD { disabled = true });
            var health = EntityManager.GetComponentData<HealthCD>(ActiveChest);
            health.health = 0;
            EntityManager.SetComponentData(ActiveChest, health);
            ActiveChest = Entity.Null;
        }

        private void FindBroken()
        {
            chests.CompleteDependency();
            var objectType = GetComponentTypeHandle<ObjectDataCD>(true);
            var healthType = GetComponentTypeHandle<HealthCD>(true);
            var guardType = GetComponentTypeHandle<DontDestroyOnZeroHealthCD>(true);
            var entityType = GetEntityTypeHandle();
            using (var chunks = chests.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var objects = chunk.GetNativeArray(ref objectType);
                    var health = chunk.GetNativeArray(ref healthType);
                    var guards = chunk.GetNativeArray(ref guardType);
                    var entities = chunk.GetNativeArray(entityType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        // Keep every hit: a consumed change version will not report this chunk again.
                        if (health[i].health > 0 || guards[i].disabled || !StorageCatalog.IsChest(objects[i].objectID)) continue;
                        var e = entities[i];
                        if (e != ActiveChest && !broken.Contains(e)) broken.Add(e);
                    }
                }
            }
        }

        private bool IsBroken(Entity e)
        {
            return EntityManager.Exists(e) && EntityManager.HasComponent<HealthCD>(e) &&
                EntityManager.HasComponent<DontDestroyOnZeroHealthCD>(e) &&
                EntityManager.GetComponentData<HealthCD>(e).health <= 0 &&
                !EntityManager.GetComponentData<DontDestroyOnZeroHealthCD>(e).disabled;
        }

    }
}
