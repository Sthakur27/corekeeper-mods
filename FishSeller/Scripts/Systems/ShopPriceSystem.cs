using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FishSeller.Systems
{
    /// <summary>
    /// Both worlds: buyValueMultiplier of every item we sell = vanilla x the price multiplier, written into
    /// the PugDatabase blob (what InventoryUtility.GetCoinValue reads for the price label on the client and
    /// the coin deduction on the server). sellValue is never touched, so selling fish pays vanilla prices.
    /// The vanilla value is captured once per item before the first write, so this is idempotent whether
    /// or not the worlds share a blob. A vanilla multiplier of 0 (would make the item free to list but
    /// unbuyable) counts as 1. Re-applied when the setting changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ShopPriceSystem : PugSimulationSystemBase
    {
        private static readonly Dictionary<ObjectID, float> Vanilla = new Dictionary<ObjectID, float>();

        private EntityQuery _database;
        private float _applied = float.NaN;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            float mult = ShopItems.PriceMultiplier;
            if (_applied == mult || _database.IsEmptyIgnoreFilter) return;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            _applied = mult;

            int seen = 0, changed = 0;
            foreach (var id in ShopItems.All)
            {
                ref var info = ref PugDatabase.GetEntityObjectInfo(id, bank.databaseBankBlob);
                if (info.objectID != id) continue;
                seen++;
                if (!Vanilla.ContainsKey(id)) Vanilla[id] = info.buyValueMultiplier;
                float baseMult = Vanilla[id] > 0f ? Vanilla[id] : 1f;
                float target = baseMult * mult;
                if (info.buyValueMultiplier != target)
                {
                    info.buyValueMultiplier = target;
                    changed++;
                }
            }
            Debug.Log($"[{FishSellerMod.Name}] buy prices at {mult:0.##}x: {seen}/{ShopItems.All.Length} items found, {changed} patched ({(World.IsServer() ? "server" : "client")}).");
        }
    }
}
