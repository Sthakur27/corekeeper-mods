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
    // never enable destruction until a later tick has observed that every queued move completed.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateHealthSystemGroup))]
    [UpdateBefore(typeof(SetEntitiesDestroyedSystem))]
    public partial class StorageRemovalSystem : PugSimulationSystemBase
    {
        public Entity ActiveChest { get; private set; }
        private EntityQuery chests;
        private EntityQuery changes;
        private EntityQuery databaseQuery;
        private bool transfersQueued;

        private struct Slot
        {
            public Entity Chest;
            public int Index;
            public ObjectDataCD Item;
        }
        private struct Move
        {
            public int Source;
            public Entity Target;
            public int Index;
            public int Amount;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            chests = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadWrite<HealthCD>(), ComponentType.ReadWrite<DontDestroyOnZeroHealthCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>());
            changes = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            databaseQuery = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            if (changes.IsEmptyIgnoreFilter || databaseQuery.IsEmptyIgnoreFilter) return;
            // All state belongs to this ECS world; unload/rejoin cannot leave stale entity IDs.
            if (ActiveChest != Entity.Null && !EntityManager.Exists(ActiveChest)) ActiveChest = Entity.Null;
            if (ActiveChest == Entity.Null)
            {
                using (var entities = chests.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var e = entities[i];
                        if (StorageCatalog.IsChest(EntityManager.GetComponentData<ObjectDataCD>(e).objectID) &&
                            EntityManager.GetComponentData<HealthCD>(e).health <= 0 &&
                            !EntityManager.GetComponentData<DontDestroyOnZeroHealthCD>(e).disabled)
                        {
                            ActiveChest = e;
                            transfersQueued = false;
                            break;
                        }
                    }
                }
            }
            if (ActiveChest == Entity.Null) return;
            EntityManager.CompleteAllTrackedJobs();
            if (transfersQueued)
            {
                // Wait for vanilla to consume the moves, including failed moves. Remaining
                // contents belong to the chest and will be dropped by vanilla destruction.
                var pending = EntityManager.GetBuffer<InventoryChangeBuffer>(changes.GetSingletonEntity());
                for (int i = 0; i < pending.Length; i++)
                    if (pending[i].inventoryChangeData.inventory1 == ActiveChest) return;
                Finish();
                return;
            }

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
            var slots = new List<Slot>();
            foreach (var chest in after.Chests)
            {
                if (!EntityManager.Exists(chest) ||
                    EntityManager.GetComponentData<HealthCD>(chest).health <= 0) continue;
                var contents = EntityManager.GetBuffer<ContainedObjectsBuffer>(chest);
                for (int i = 0; i < Math.Min(contents.Length, StorageCatalog.Capacity(EntityManager.GetComponentData<ObjectDataCD>(chest).objectID)); i++)
                    slots.Add(new Slot { Chest = chest, Index = i, Item = contents[i].objectData });
            }
            var items = new List<ObjectDataCD>();
            for (int i = 0; i < source.Length; i++) items.Add(source[i].objectData);
            var bank = databaseQuery.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            TryPlan(items, slots, id => PugDatabase.GetEntityObjectInfo(id, bank).isStackable, out var moves);
            if (moves.Count == 0) { Finish(); return; }
            var queue = EntityManager.GetBuffer<InventoryChangeBuffer>(changes.GetSingletonEntity());
            foreach (var move in moves)
                queue.Add(new InventoryChangeBuffer
                {
                    playerEntity = Entity.Null,
                    inventoryChangeData = Inventory.Create.MoveAmount(ActiveChest, move.Source,
                        move.Target, move.Index, -1, move.Amount, false)
                });
            // Concurrent deposits can make a move fail; vanilla will drop its leftovers.
            transfersQueued = true;
        }

        private static bool TryPlan(List<ObjectDataCD> source, List<Slot> slots, Func<ObjectID, bool> isStackable, out List<Move> moves)
        {
            moves = new List<Move>();
            bool allFit = true;
            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item.objectID == ObjectID.None) continue;
                bool stackable = isStackable(item.objectID);
                int remaining = item.amount;
                // Existing stacks first, then empty slots. Same ID/variation and 9999 limit are
                // the exact rules used by vanilla InventoryUtility.TryFindSlotToAddTo.
                for (int pass = 0; pass < 2 && remaining > 0; pass++)
                    for (int j = 0; j < slots.Count && remaining > 0; j++)
                    {
                        var slot = slots[j];
                        bool vacant = slot.Item.objectID == ObjectID.None;
                        if (pass == 0 ? vacant : !vacant) continue;
                        if (!vacant && (!stackable || slot.Item.objectID != item.objectID ||
                            slot.Item.variation != item.variation)) continue;
                        int capacity = stackable ? 9999 - slot.Item.amount : (vacant ? remaining : 0);
                        int amount = Math.Min(remaining, Math.Max(0, capacity));
                        if (amount == 0) continue;
                        moves.Add(new Move { Source = i, Target = slot.Chest, Index = slot.Index, Amount = amount });
                        slot.Item.objectID = item.objectID;
                        slot.Item.variation = item.variation;
                        slot.Item.amount += amount;
                        slots[j] = slot;
                        remaining -= amount;
                    }
                if (remaining > 0) allFit = false;
            }
            return allFit;
        }

        private void Finish()
        {
            EntityManager.SetComponentData(ActiveChest, new DontDestroyOnZeroHealthCD { disabled = true });
            var health = EntityManager.GetComponentData<HealthCD>(ActiveChest);
            health.health = 0;
            EntityManager.SetComponentData(ActiveChest, health);
            ActiveChest = Entity.Null;
        }

    }
}
