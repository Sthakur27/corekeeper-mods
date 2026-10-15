using System;
using System.Collections.Generic;
using Inventory;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ConnectedStorage
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class StorageTransferSystem : PugSimulationSystemBase
    {
        private readonly Dictionary<Type, EntityQuery> queries = new Dictionary<Type, EntityQuery>();
        protected override void OnUpdate() { }
        private T Singleton<T>() where T : unmanaged, IComponentData
        {
            if (!queries.TryGetValue(typeof(T), out var query))
            {
                query = GetEntityQuery(ComponentType.ReadOnly<T>());
                queries.Add(typeof(T), query);
            }
            return query.GetSingleton<T>();
        }
        public static bool Locked(EntityManager em, Entity entity, int slot)
        {
            return em.HasBuffer<LockedObjectsBuffer>(entity) && slot >= 0
                && slot < em.GetBuffer<LockedObjectsBuffer>(entity).Length
                && em.GetBuffer<LockedObjectsBuffer>(entity)[slot].Value;
        }
        private bool Available(Entity entity)
        {
            return EntityManager.Exists(entity) && EntityManager.HasBuffer<ContainedObjectsBuffer>(entity)
                && EntityManager.HasBuffer<InventoryBuffer>(entity)
                && (!EntityManager.HasComponent<EntityDestroyedCD>(entity) || !EntityManager.IsComponentEnabled<EntityDestroyedCD>(entity))
                && !StorageRemoval.IsEvacuating(World, entity)
                && (!EntityManager.HasComponent<HealthCD>(entity) || EntityManager.GetComponentData<HealthCD>(entity).health > 0);
        }
        public string SortNetwork(List<Entity> units, bool descending)
        {
            if (!World.IsServer()) return "Storage changes must run on the server.";
            EntityManager.CompleteAllTrackedJobs();
            var addresses = new List<(Entity entity, int slot)>();
            var order = new List<int>();
            var names = new List<string>();
            var empty = new List<bool>();
            // Real slots first; occupied legacy overflow remains recoverable at the end.
            for(int pass=0;pass<2;pass++) foreach(var unit in units) {
                if(!Available(unit))continue;
                var items=EntityManager.GetBuffer<ContainedObjectsBuffer>(unit);
                int limit=StorageCatalog.Capacity(EntityManager.GetComponentData<ObjectDataCD>(unit).objectID);
                for(int i=0;i<items.Length;i++) {
                    if(pass==0 ? i>=limit : i<limit || items[i].objectID==ObjectID.None)continue;
                    if(Locked(EntityManager,unit,i))continue;
                    order.Add(addresses.Count);addresses.Add((unit,i));
                    empty.Add(items[i].objectID==ObjectID.None);
                    names.Add(PlayerController.GetObjectName(items[i],true)?.text ?? "");
                }
            }
            order.Sort((a,b)=> {
                if(empty[a]!=empty[b])return empty[a]?1:-1;
                int result=StringComparer.OrdinalIgnoreCase.Compare(names[a],names[b]);
                if(descending)result=-result;
                return result==0?a.CompareTo(b):result;
            });
            using(var ecb=new EntityCommandBuffer(Allocator.Temp)) {
                var shared=new InventoryHandlerShared(ref CheckedStateRef,Singleton<PugDatabase.DatabaseBankCD>(),
                    Singleton<SkillTalentsTableCD>(),Singleton<UpgradeCostsTableCD>(),Singleton<InventoryAuxDataSystemDataCD>());
                shared.Update(ref CheckedStateRef,ecb,Singleton<NetworkTime>());
                var positions=new int[order.Count];var identities=new int[order.Count];
                for(int i=0;i<order.Count;i++)positions[i]=identities[i]=i;
                for(int i=0;i<order.Count;i++) {
                    int from=positions[order[i]];
                    if(from==i)continue;
                    var a=addresses[i];var b=addresses[from];
                    InventoryUtility.Swap(in shared,a.entity,b.entity,a.slot,b.slot);
                    int displaced=identities[i];identities[from]=displaced;positions[displaced]=from;
                    identities[i]=order[i];positions[order[i]]=i;
                }
                ecb.Playback(EntityManager);
            }
            return null;
        }
        // This is the same replacement lifecycle vanilla uses for object transformations.
        // InventoryUtility.Swap preserves auxiliary item data and slot locks. All work is
        // synchronous on the server; no save or other command can interleave the transaction.
        public string Upgrade(Entity player, Entity unit, int gemSlot, ObjectID expectedUnit)
        {
            if (!World.IsServer()) return "Upgrades must run on the server.";
            EntityManager.CompleteAllTrackedJobs();
            if (!Available(player) || !Available(unit)) return "Storage unit unavailable.";
            var data = EntityManager.GetComponentData<ObjectDataCD>(unit);
            int tier = StorageCatalog.Tier(data.objectID);
            if (tier < 0 || data.objectID != expectedUnit) return "The unit changed; interact again.";
            if (tier == StorageCatalog.Tiers.Length - 1) return "This storage unit is already fully upgraded.";
            var playerRange = EntityManager.GetBuffer<InventoryBuffer>(player)[0];
            var bag = EntityManager.GetBuffer<ContainedObjectsBuffer>(player);
            ObjectID gem = StorageCatalog.Gem(tier + 1);
            if (gem == ObjectID.None || gemSlot < playerRange.startIndex || gemSlot >= playerRange.startIndex + playerRange.size
                || gemSlot >= bag.Length || !StorageCatalog.CanUpgrade(data.objectID, bag[gemSlot].objectID) || bag[gemSlot].amount < 1)
                return "To upgrade, select a " + StorageCatalog.Tiers[tier + 1]
                    + " Upgrade Gem in your hotbar and interact with this unit. Tiers cannot be skipped.";
            if (Locked(EntityManager, player, gemSlot)) return "Unlock the upgrade gem first.";
            if (!EntityManager.HasComponent<EntityDestroyedCD>(unit)
                || EntityManager.IsComponentEnabled<EntityDestroyedCD>(unit)
                || !EntityManager.HasComponent<DontDropLootCD>(unit)
                || !EntityManager.HasComponent<DontDropSelfCD>(unit)) return "Unit transformation is unavailable.";
            using (var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<InventoryChangeBuffer>()))
            {
                var queue = EntityManager.GetBuffer<InventoryChangeBuffer>(query.GetSingletonEntity());
                for (int i = 0; i < queue.Length; i++)
                {
                    var change = queue[i].inventoryChangeData;
                    if (change.inventory1 == unit || change.entityOrInventory2 == unit || change.entityOrInventory3 == unit)
                        return "Storage is busy; interact again in a moment.";
                }
            }
            var bank = Singleton<PugDatabase.DatabaseBankCD>();
            ObjectID nextID = StorageCatalog.Chest(tier + 1);
            Entity prefab = PugDatabase.GetPrimaryPrefabEntity(nextID, bank.databaseBankBlob);
            var original=EntityManager.GetBuffer<ContainedObjectsBuffer>(unit);
            int count=original.Length;
            while(count>0 && original[count-1].objectID==ObjectID.None)count--;
            if (prefab == Entity.Null || !EntityManager.HasBuffer<ContainedObjectsBuffer>(prefab)
                || EntityManager.GetBuffer<ContainedObjectsBuffer>(prefab).Length < count)
                return "Withdraw items from legacy overflow slots before upgrading.";
            var position = EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position;
            Entity replacement = EntityUtility.CreateEntity(World, (UnityEngine.Vector3)position, nextID, 1, bank.databaseBankBlob);
            if (replacement == Entity.Null) return "Could not create upgraded storage unit.";
            if (EntityManager.HasComponent<DirectionCD>(unit) && EntityManager.HasComponent<DirectionCD>(replacement))
                EntityManager.SetComponentData(replacement, EntityManager.GetComponentData<DirectionCD>(unit));
            if (EntityManager.HasBuffer<LockedObjectsBuffer>(unit) && !EntityManager.HasBuffer<LockedObjectsBuffer>(replacement))
                EntityManager.AddBuffer<LockedObjectsBuffer>(replacement).ResizeUninitialized(
                    EntityManager.GetBuffer<ContainedObjectsBuffer>(replacement).Length);
            if (EntityManager.HasBuffer<LockedObjectsBuffer>(replacement))
            {
                var locks = EntityManager.GetBuffer<LockedObjectsBuffer>(replacement);
                for (int i = 0; i < locks.Length; i++) locks[i] = default(LockedObjectsBuffer);
            }
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            {
                var shared = new InventoryHandlerShared(ref CheckedStateRef, bank,
                    Singleton<SkillTalentsTableCD>(), Singleton<UpgradeCostsTableCD>(), Singleton<InventoryAuxDataSystemDataCD>());
                shared.Update(ref CheckedStateRef, ecb, Singleton<NetworkTime>());
                for (int i = 0; i < count; i++) InventoryUtility.Swap(in shared, unit, replacement, i, i);
                if (!InventoryUtility.ConsumeEntityAt(in shared, player, gemSlot, 1, true, position, optionalTargetObjectID: gem))
                {
                    for (int i = 0; i < count; i++) InventoryUtility.Swap(in shared, unit, replacement, i, i);
                    ecb.Playback(EntityManager);
                    EntityManager.DestroyEntity(replacement);
                    return "The gem changed; upgrade cancelled.";
                }
                ecb.Playback(EntityManager);
            }
            if (EntityManager.HasComponent<IsBeingTransformedCD>(unit))
            {
                EntityManager.SetComponentData(unit, new IsBeingTransformedCD { transformedToEntity = replacement });
                EntityManager.SetComponentEnabled<IsBeingTransformedCD>(unit, true);
            }
            EntityManager.SetComponentEnabled<DontDropLootCD>(unit, true);
            EntityManager.SetComponentEnabled<DontDropSelfCD>(unit, true);
            EntityManager.SetComponentEnabled<EntityDestroyedCD>(unit, true);
            return "Upgraded to " + StorageCatalog.Tiers[tier + 1] + " Storage Unit ("
                + StorageCatalog.Capacities[tier + 1] + " slots).";
        }
        // Caller validates terminal proximity, network membership and the source item
        // identity immediately before this synchronous operation. Never queue stale slots.
        public string Transfer(Entity player, Entity source, int sourceSlot, List<Entity> targets, int amount)
        {
            if (!World.IsServer()) return "Storage changes must run on the server.";
            EntityManager.CompleteAllTrackedJobs();
            if (!Available(player) || !Available(source)) return "Inventory unavailable.";
            var slots = EntityManager.GetBuffer<ContainedObjectsBuffer>(source);
            if (sourceSlot < 0 || sourceSlot >= slots.Length || slots[sourceSlot].objectID == ObjectID.None) return "That slot is empty.";
            if (Locked(EntityManager, source, sourceSlot)) return "That item is locked.";
            if (amount <= 0) return "Invalid amount.";
            int remaining = amount;
            bool moved = false;
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            {
                var shared = new InventoryHandlerShared(ref CheckedStateRef, Singleton<PugDatabase.DatabaseBankCD>(),
                    Singleton<SkillTalentsTableCD>(), Singleton<UpgradeCostsTableCD>(), Singleton<InventoryAuxDataSystemDataCD>());
                shared.Update(ref CheckedStateRef, ecb, Singleton<NetworkTime>());
                foreach (var target in targets)
                {
                    if (!Available(target)) continue;
                    var ranges = EntityManager.GetBuffer<InventoryBuffer>(target);
                    if (ranges.Length == 0) continue;
                    var range = ranges[0];
                    int end = Math.Min(range.startIndex + range.size, EntityManager.GetBuffer<ContainedObjectsBuffer>(target).Length);
                    int tierLimit=StorageCatalog.Capacity(EntityManager.GetComponentData<ObjectDataCD>(target).objectID);
                    if(tierLimit>0)end=Math.Min(end,range.startIndex+tierLimit);
                    // Fill matching stacks first, then empty slots. Every chosen hint
                    // is checked against the game's own slot rules immediately before moving.
                    for (int pass = 0; pass < 2; pass++)
                    for (int destination = range.startIndex; destination < end; destination++)
                    {
                        var before = slots[sourceSlot];
                        if (before.objectID == ObjectID.None || remaining <= 0) break;
                        if (source == target && sourceSlot == destination || Locked(EntityManager, target, destination)) continue;
                        var targetContents=shared.containedObjectsBufferLookup[target];
                        var targetItem = targetContents[destination];
                        if(tierLimit>0 && targetItem.objectID==ObjectID.None) {
                            int occupied=0;
                            for(int n=0;n<targetContents.Length;n++)if(targetContents[n].objectID!=ObjectID.None)occupied++;
                            if(occupied>=tierLimit)continue;
                        }
                        if (pass == 0 ? targetItem.objectID == ObjectID.None : targetItem.objectID != ObjectID.None) continue;
                        int chosen = InventoryUtility.TryFindSlotToAddTo(in shared, before.objectID,
                            target, destination, -1, before.variation);
                        if (chosen != destination) continue;
                        // Limit the native search itself, not just our outer loop. Its endIndex
                        // parameter can otherwise fall back to searching the whole old inventory.
                        var savedRange=ranges[0];
                        var exactRange=savedRange;exactRange.startIndex=destination;exactRange.sizeX=1;exactRange.sizeY=1;exactRange.extraSize=0;
                        ranges[0]=exactRange;
                        try { InventoryUtility.MoveAmount(in shared, source, sourceSlot, target, destination, -1, remaining); }
                        finally { ranges[0]=savedRange; }
                        var after = slots[sourceSlot];
                        if (after.objectID == ObjectID.None)
                        {
                            moved = true;
                            remaining = 0;
                        }
                        else if (after.amount < before.amount)
                        {
                            moved = true;
                            remaining -= before.amount - after.amount;
                        }
                    }
                    if (slots[sourceSlot].objectID == ObjectID.None || remaining <= 0) break;
                }
                ecb.Playback(EntityManager);
            }
            if (!moved) return "No space in the destination inventory.";
            return remaining > 0 && slots[sourceSlot].objectID != ObjectID.None
                ? "Moved what fits; the remaining items stayed in their original slot." : null;
        }
    }
}
