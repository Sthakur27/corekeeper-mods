using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Server-side safety net for boss merchants that already live in a save (and the prefab): every few
    /// seconds, keep each merchant's list equal to its catalog stock and its inventory big enough
    /// (<see cref="MerchantStock"/>). When items are newly added to a live merchant, its restock timer
    /// (ObjectDataCD.amount, counted down by MerchantBuyInventorySystem) is zeroed so the stock shows up now.
    /// Read-only in steady state: write access is only taken when a buffer actually changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantStockSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 3f;
        // Staggered start so the merchant mods' timers don't share a tick (Potion Seller 1.3, Fish Seller 0.x).
        private float _timer = 2.1f;
        private EntityQuery _merchants;

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadOnly<MerchantItemInfoBuffer>(),
                    ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                    ComponentType.ReadOnly<InventoryBuffer>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmpty) return;
            Catalog.Resolve();

            int stock = BossMerchantsConfig.Stock;
            var entities = _merchants.ToEntityArray(Allocator.Temp);
            var objects = _merchants.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var merchant = Catalog.ByMerchantId(objects[i].objectID);
                if (merchant == null) continue;
                var e = entities[i];
                bool isPrefab = EntityManager.HasComponent<Prefab>(e);
                if (!MerchantStock.Apply(EntityManager, e, merchant, stock, out bool addedItems)) continue;
                if (addedItems && !isPrefab)
                {
                    var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                    od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                    EntityManager.SetComponentData(e, od);
                }
                Debug.Log($"[{BossMerchantsMod.Name}] {merchant.Key} merchant {(isPrefab ? "prefab" : e.ToString())}: {EntityManager.GetBuffer<MerchantItemInfoBuffer>(e, true).Length} items, stock {stock}{(addedItems && !isPrefab ? ", restock forced" : "")}.");
            }
            entities.Dispose();
            objects.Dispose();
        }
    }
}
