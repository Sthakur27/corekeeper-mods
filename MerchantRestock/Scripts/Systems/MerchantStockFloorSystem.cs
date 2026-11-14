using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace MerchantRestock.Systems
{
    /// <summary>
    /// Server, every 2 s: raises the amount of every regular stackable entry on the six vanilla merchants'
    /// lists (MerchantItemInfoBuffer.amount, what MerchantBuyInventorySystem stocks per restock) to the
    /// "Minimum stock per item" setting (<see cref="RestockSettings.TargetAmount"/>).
    ///
    /// The vanilla amounts are remembered from the merchant prefab (fresh every launch, so always vanilla) the
    /// first time each entry is seen, and every live merchant is set to a function of that: raising the floor
    /// raises it, lowering or switching it off puts the vanilla amount back, even on a merchant whose saved
    /// list holds an old raised value. Entries Fish Seller manages are skipped. A live merchant whose list
    /// changed has its restock countdown (ObjectDataCD.amount) zeroed so the new stock shows up now.
    /// Read-only in steady state: write access is only taken when an amount actually has to change.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantStockFloorSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 2f;

        private EntityQuery _merchants;
        private EntityQuery _database;
        // Staggered so the merchant mods' timers don't all fire on the same tick.
        private float _timer = 1.3f;
        private readonly Dictionary<long, int> _vanilla = new Dictionary<long, int>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadOnly<MerchantItemInfoBuffer>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        private static long Key(ObjectID merchant, ObjectID item) => ((long)(int)merchant << 32) | (uint)(int)item;

        private static bool IsVanillaMerchant(ObjectID id)
        {
            foreach (var m in RestockSettings.Merchants) if (m.id == id) return true;
            return false;
        }

        protected override void OnUpdate()
        {
            if (!World.IsServer()) return;
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter) return;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;

            int floor = RestockSettings.StockFloor;
            var entities = _merchants.ToEntityArray(Allocator.Temp);
            // Prefabs first: they hold the vanilla amounts the live merchants are measured against.
            for (int pass = 0; pass < 2; pass++)
            {
                bool prefabPass = pass == 0;
                for (int i = 0; i < entities.Length; i++)
                {
                    var e = entities[i];
                    if (EntityManager.HasComponent<Prefab>(e) != prefabPass) continue;
                    var merchant = EntityManager.GetComponentData<ObjectDataCD>(e).objectID;
                    if (!IsVanillaMerchant(merchant)) continue;
                    Process(e, merchant, prefabPass, floor, bank);
                }
            }
            entities.Dispose();
        }

        private void Process(Entity e, ObjectID merchant, bool isPrefab, int floor, PugDatabase.DatabaseBankCD bank)
        {
            var items = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e, true);
            bool needsWrite = false;
            for (int i = 0; i < items.Length; i++)
            {
                var id = items[i].objectID;
                if (id == ObjectID.None || RestockSettings.IsFishSellerItem(id)) continue;
                long key = Key(merchant, id);
                if (!_vanilla.TryGetValue(key, out int vanilla))
                {
                    if (!isPrefab) continue; // not on the prefab: another mod's entry, leave it
                    vanilla = items[i].amount;
                    _vanilla[key] = vanilla;
                }
                if (items[i].amount != RestockSettings.TargetAmount(vanilla, IsStackable(id, bank), floor)) { needsWrite = true; break; }
            }
            if (!needsWrite) return;

            var writable = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e);
            int changed = 0;
            for (int i = 0; i < writable.Length; i++)
            {
                var entry = writable[i];
                if (entry.objectID == ObjectID.None || RestockSettings.IsFishSellerItem(entry.objectID)) continue;
                if (!_vanilla.TryGetValue(Key(merchant, entry.objectID), out int vanilla)) continue;
                int target = RestockSettings.TargetAmount(vanilla, IsStackable(entry.objectID, bank), floor);
                if (entry.amount == target) continue;
                entry.amount = target;
                writable[i] = entry;
                changed++;
            }
            if (changed == 0) return;

            if (!isPrefab)
            {
                var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                EntityManager.SetComponentData(e, od);
            }
            Debug.Log($"[{MerchantRestockMod.Name}] {merchant} {(isPrefab ? "prefab" : e.ToString())}: {changed} entries set for minimum stock {(floor > 0 ? floor.ToString() : "off")}{(isPrefab ? "" : ", restock forced")} (server).");
        }

        private static bool IsStackable(ObjectID id, PugDatabase.DatabaseBankCD bank)
        {
            ref var info = ref PugDatabase.GetEntityObjectInfo(id, bank.databaseBankBlob);
            return info.objectID == id && info.isStackable;
        }
    }
}
