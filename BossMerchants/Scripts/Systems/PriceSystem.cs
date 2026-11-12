using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Writes sellValue / buyValueMultiplier of every catalog item straight into the PugDatabase blob (what
    /// InventoryUtility.GetCoinValue reads for the price label on the client and the coin deduction on the
    /// server). Both worlds; a no-op if the authoring-time edits already landed; re-applied when the price
    /// multiplier changes. Same as Potion Seller's PotionPriceSystem.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class PriceSystem : PugSimulationSystemBase
    {
        private EntityQuery _database;
        private float _appliedMultiplier = float.NaN;
        private int _appliedCount = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            float mult = BossMerchantsConfig.PriceMultiplier;
            if (_database.IsEmptyIgnoreFilter) return;
            Catalog.Resolve();
            var all = Catalog.All;
            if (_appliedMultiplier == mult && _appliedCount == all.Length) return;

            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            _appliedMultiplier = mult;
            _appliedCount = all.Length;

            int changed = 0, seen = 0;
            foreach (var item in all)
            {
                ref var info = ref PugDatabase.GetEntityObjectInfo(item.id, bank.databaseBankBlob);
                if (info.objectID != item.id) continue;
                seen++;
                if (Catalog.Apply(ref info, item.id)) changed++;
            }
            Debug.Log($"[{BossMerchantsMod.Name}] blob prices at {mult:0.##}x: {seen}/{all.Length} items found, {changed} patched ({(World.IsServer() ? "server" : "client")}).");
        }
    }
}
