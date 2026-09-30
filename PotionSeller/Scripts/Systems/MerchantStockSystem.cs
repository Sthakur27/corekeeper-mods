using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace PotionSeller.Systems
{
    /// <summary>
    /// Server-side fix-up for merchants that already exist in a save (and belt-and-braces for the prefab):
    /// every couple of seconds, for each Caveling / Slime Merchant entity, append its items to its
    /// MerchantItemInfoBuffer, sync their amounts to the stock setting and grow its inventory
    /// (see <see cref="MerchantStock"/>). The first time potions get added to a live merchant its restock
    /// timer (ObjectDataCD.amount, counted down by MerchantBuyInventorySystem) is zeroed so the new stock
    /// shows up within a few seconds instead of after the next 25-35 minute restock.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantStockSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 2f;

        private EntityQuery _merchants;
        private float _timer;
        private readonly System.Collections.Generic.HashSet<Entity> _checked = new System.Collections.Generic.HashSet<Entity>();

        private bool HasAnyStocked(Entity e, PotionPrices.Entry[] list)
        {
            var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e);
            for (int i = 0; i < contained.Length; i++)
            {
                var id = contained[i].objectID;
                if (id == ObjectID.None) continue;
                for (int j = 0; j < list.Length; j++)
                    if (list[j].id == id) return true;
            }
            return false;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadWrite<MerchantItemInfoBuffer>(),
                    ComponentType.ReadWrite<ContainedObjectsBuffer>(),
                    ComponentType.ReadWrite<InventoryBuffer>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmptyIgnoreFilter) return;

            int stock = PotionSellerConfig.Stock;
            var entities = _merchants.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                var list = PotionPrices.ForMerchant(EntityManager.GetComponentData<ObjectDataCD>(e).objectID);
                if (list == null) continue;
                bool isPrefab = EntityManager.HasComponent<Prefab>(e);
                bool changed = MerchantStock.Apply(EntityManager, e, stock, out bool addedItems);

                // A merchant loaded from a save can already carry our items in his list (the prefab was
                // fixed first) while his shelves still hold the old stock until the next 25-35 min
                // restock. Once per session, if none of our items is on his shelves, restock now.
                if (!isPrefab && _checked.Add(e) && !HasAnyStocked(e, list))
                {
                    addedItems = true;
                    changed = true;
                }
                if (!changed) continue;

                if (addedItems && !isPrefab)
                {
                    var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                    od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                    EntityManager.SetComponentData(e, od);
                }
                Debug.Log($"[{PotionSellerMod.Name}] {EntityManager.GetComponentData<ObjectDataCD>(e).objectID} {(isPrefab ? "prefab" : e.ToString())}: {EntityManager.GetBuffer<MerchantItemInfoBuffer>(e).Length} items, {EntityManager.GetBuffer<ContainedObjectsBuffer>(e).Length} slots, stock {stock}{(addedItems && !isPrefab ? ", restock forced" : "")} (server).");
            }
            entities.Dispose();
        }
    }
}
