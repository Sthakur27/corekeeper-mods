using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace EnderStash.Systems
{
    /// <summary>
    /// Server: puts the Ender Chest first on the Fishing Merchant's list (prefab and merchants already in a
    /// save). MerchantBuyInventorySystem fills shelf slot k with the k-th available list entry, so index 0
    /// is always stocked whatever else is on the list. The first time it is added to a live merchant his
    /// restock timer (ObjectDataCD.amount) is zeroed so it shows up within seconds instead of after the
    /// next 25-35 minute restock.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnderChestMerchantSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 2f;

        private EntityQuery _merchants;
        private float _timer;

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadWrite<ObjectDataCD>(),
                    ComponentType.ReadWrite<MerchantItemInfoBuffer>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;

            var id = EnderChest.Id;
            if (id == ObjectID.None || _merchants.IsEmptyIgnoreFilter) return;

            var entities = _merchants.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                if (od.objectID != ObjectID.FishingMerchant) continue;

                var items = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e);
                int idx = -1;
                for (int i = 0; i < items.Length; i++)
                    if (items[i].objectID == id) { idx = i; break; }
                if (idx == 0 && items[0].amount == EnderChest.MerchantStock) continue;

                if (idx > 0) items.RemoveAt(idx);
                var entry = new MerchantItemInfoBuffer
                {
                    objectID = id,
                    amount = EnderChest.MerchantStock,
                    requirementToBeAvailable = MerchantItemRequirement.None
                };
                if (idx == 0) items[0] = entry;
                else items.Insert(0, entry);

                bool isPrefab = EntityManager.HasComponent<Prefab>(e);
                if (idx < 0 && !isPrefab)
                {
                    od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                    EntityManager.SetComponentData(e, od);
                }
                Debug.Log($"[{EnderStashMod.Name}] Fishing Merchant {(isPrefab ? "prefab" : e.ToString())}: Ender Chest listed ({items.Length} items{(idx < 0 && !isPrefab ? ", restock forced" : "")}).");
            }
            entities.Dispose();
        }
    }
}
