using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FishSeller.Systems
{
    /// <summary>
    /// Server, every 2 s, for every merchant (prefabs included): keeps our items on the Fishing / Caveling
    /// Merchant lists with the current stock setting (minus anything the game would not trade), keeps gated
    /// entries last and grows the slot buffer to fit the whole list (see <see cref="MerchantRoom"/>; this
    /// also catches entries other mods add after us). A merchant loaded from a save keeps his old shelves
    /// until his next 25-35 min restock, so the first time our items are missing from a live merchant his
    /// restock timer (ObjectDataCD.amount) is zeroed and MerchantBuyInventorySystem restocks him now.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class ShopStockSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 2f;

        private EntityQuery _merchants;
        private EntityQuery _database;
        private float _timer;
        private readonly HashSet<Entity> _checked = new HashSet<Entity>();
        private readonly HashSet<ObjectID> _loggedList = new HashSet<ObjectID>();
        private readonly Dictionary<ObjectID, int> _amounts = new Dictionary<ObjectID, int>();
        private bool _loggedSkips;

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
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter) return;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;

            RefreshAmounts(bank);

            var entities = _merchants.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                var merchant = EntityManager.GetComponentData<ObjectDataCD>(e).objectID;
                bool isPrefab = EntityManager.HasComponent<Prefab>(e);
                var ours = ShopItems.ForMerchant(merchant);

                if (isPrefab && _loggedList.Add(merchant)) LogVanillaList(e, merchant);

                bool changed = false, added = false;
                if (ours != null)
                {
                    changed |= MerchantRoom.ApplyItems(EntityManager, e, ours, AmountFor, out added);
                    changed |= MerchantRoom.EnsureGrid(EntityManager, e);
                }
                bool reordered = MerchantRoom.KeepGatedLast(EntityManager, e);
                changed |= reordered;
                changed |= MerchantRoom.EnsureRoom(EntityManager, e);

                bool restock = !isPrefab && (added || reordered);
                if (!isPrefab && ours != null && _checked.Add(e) && !HasAnyStocked(e, ours)) restock = true;
                if (restock)
                {
                    var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                    od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                    EntityManager.SetComponentData(e, od);
                }
                if (changed || restock)
                {
                    Debug.Log($"[{FishSellerMod.Name}] {merchant} {(isPrefab ? "prefab" : e.ToString())}: {EntityManager.GetBuffer<MerchantItemInfoBuffer>(e).Length} items, {EntityManager.GetBuffer<ContainedObjectsBuffer>(e).Length} slots, stock {ShopItems.Stock}{(reordered ? ", gated entries moved last" : "")}{(restock ? ", restock forced" : "")} (server).");
                }
            }
            entities.Dispose();
        }

        private int AmountFor(ObjectID id) => _amounts.TryGetValue(id, out int a) ? a : 0;

        /// <summary>Per item: 0 if the game would not sell it, else the stock (1 for unstackable items: one per slot).</summary>
        private void RefreshAmounts(PugDatabase.DatabaseBankCD bank)
        {
            _amounts.Clear();
            List<string> skipped = _loggedSkips ? null : new List<string>();
            foreach (var id in ShopItems.All)
            {
                ref var info = ref PugDatabase.GetEntityObjectInfo(id, bank.databaseBankBlob);
                string why = null;
                if (info.objectID != id) why = "not in the database";
                else if (info.rarity == Rarity.Legendary && !ShopItems.IsSpecial(id)) why = "legendary (the game prices it at 0)";
                else
                {
                    var prefab = PugDatabase.GetPrimaryPrefabEntity(id, bank.databaseBankBlob, 0);
                    if (prefab != Entity.Null && EntityManager.Exists(prefab) && EntityManager.HasComponent<CantBeSoldCD>(prefab) && !ShopItems.IsSpecial(id)) why = "can't be sold";
                }
                if (why != null)
                {
                    skipped?.Add($"{id} ({why})");
                    _amounts[id] = 0;
                    continue;
                }
                _amounts[id] = info.isStackable ? ShopItems.Stock : 1;
            }
            if (skipped != null)
            {
                _loggedSkips = true;
                Debug.Log(skipped.Count == 0
                    ? $"[{FishSellerMod.Name}] all {ShopItems.All.Length} items are tradeable."
                    : $"[{FishSellerMod.Name}] not sold: {string.Join(", ", skipped)}.");
            }
        }

        private bool HasAnyStocked(Entity e, ObjectID[] ours)
        {
            var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e);
            for (int i = 0; i < contained.Length; i++)
            {
                var id = contained[i].objectID;
                if (id == ObjectID.None) continue;
                for (int j = 0; j < ours.Length; j++) if (ours[j] == id && AmountFor(id) > 0) return true;
            }
            return false;
        }

        private void LogVanillaList(Entity e, ObjectID merchant)
        {
            var items = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e);
            var parts = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                if (ShopItems.IsOurs(items[i].objectID)) continue;
                var req = items[i].requirementToBeAvailable;
                parts.Add(req == MerchantItemRequirement.None ? $"{items[i].objectID} x{items[i].amount}" : $"{items[i].objectID} x{items[i].amount} [{req}]");
            }
            Debug.Log($"[{FishSellerMod.Name}] {merchant} list (without ours): {string.Join(", ", parts)}");
        }
    }
}
