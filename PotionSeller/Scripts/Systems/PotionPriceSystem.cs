using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace PotionSeller.Systems
{
    /// <summary>
    /// Safety net for prices, runs in both worlds: writes sellValue / buyValueMultiplier of every potion
    /// straight into the PugDatabase blob (what InventoryUtility.GetCoinValue reads for the price label on
    /// the client and for the coin deduction on the server). A no-op if the authoring-time edits already
    /// landed. Re-applied whenever the price multiplier setting changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class PotionPriceSystem : PugSimulationSystemBase
    {
        private EntityQuery _database;
        private float _appliedMultiplier = float.NaN;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            float mult = PotionSellerConfig.PriceMultiplier;
            if (_appliedMultiplier == mult || _database.IsEmptyIgnoreFilter) return;

            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            _appliedMultiplier = mult;

            string side = World.IsServer() ? "server" : "client";
            int changed = 0, seen = 0;
            foreach (var potion in PotionPrices.Potions)
            {
                ref var info = ref PugDatabase.GetEntityObjectInfo(potion.id, bank.databaseBankBlob);
                if (info.objectID != potion.id) continue;
                seen++;
                if (PotionPrices.Apply(ref info, potion.id)) changed++;
            }
            Debug.Log($"[{PotionSellerMod.Name}] blob prices at {mult:0.##}x: {seen}/{PotionPrices.Potions.Length} potions found, {changed} patched ({side}).");
        }
    }
}
