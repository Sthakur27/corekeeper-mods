using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace ValuableValue.Systems
{
    /// <summary>
    /// Both worlds: every ObjectType.Valuable entry in the PugDatabase blob gets
    ///   sellValue          = vanilla sell price x multiplier
    ///   buyValueMultiplier = vanilla buyValueMultiplier / multiplier
    /// InventoryUtility.GetCoinValue pays sellValue per unit when selling and charges
    /// round(sellValue x 5 x buyValueMultiplier) when buying, so selling pays N x vanilla while a merchant that
    /// stocks a valuable still asks the vanilla price.
    ///
    /// A negative vanilla sellValue means "derive from rarity" (plus a crafting-ingredient bonus and a fixed
    /// per-item +-10% jitter); that derived price is computed here exactly like GetCoinValue does and written as
    /// a positive value. sellValue 0 (cannot be sold) and Legendary items (always 0) are left alone. Vanilla
    /// values are captured once per entry before the first write, so this is idempotent whether or not the
    /// worlds share a blob. Re-applied when the setting changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ValuablePriceSystem : PugSimulationSystemBase
    {
        private struct VanillaPrice
        {
            public int sell;      // effective vanilla sell price per unit (already derived), > 0
            public float buyMult; // vanilla buyValueMultiplier
        }

        /// <summary>Keyed by objectID &lt;&lt; 16 | variation. Shared by both worlds on purpose.</summary>
        private static readonly Dictionary<long, VanillaPrice> Vanilla = new Dictionary<long, VanillaPrice>();

        private EntityQuery _database;
        private int _applied = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            int mult = math.max(1, ValuableValueMod.Multiplier);
            if (_applied == mult || _database.IsEmptyIgnoreFilter) return;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            _applied = mult;

            int seen = 0, changed = 0;
            ref var infos = ref bank.databaseBankBlob.Value.objectInfos;
            for (int i = 0; i < infos.Length; i++)
            {
                ref var info = ref infos[i];
                if (info.objectType != ObjectType.Valuable || info.objectID == ObjectID.None || info.rarity == Rarity.Legendary) continue;

                long key = ((long)(int)info.objectID << 16) | (uint)(info.variation & 0xFFFF);
                if (!Vanilla.TryGetValue(key, out var vanilla))
                {
                    if (info.sellValue == 0) continue; // unsellable in vanilla, keep it that way
                    vanilla = new VanillaPrice
                    {
                        sell = info.sellValue > 0 ? info.sellValue : DerivedSellValue(ref info, bank.databaseBankBlob),
                        buyMult = info.buyValueMultiplier,
                    };
                    Vanilla[key] = vanilla;
                }
                seen++;

                // At 1x the written price equals the vanilla (derived) one, so behaviour is vanilla.
                int sell = vanilla.sell * mult;
                float buyMult = vanilla.buyMult / mult;
                if (info.sellValue != sell || info.buyValueMultiplier != buyMult)
                {
                    info.sellValue = sell;
                    info.buyValueMultiplier = buyMult;
                    changed++;
                }
            }
            Debug.Log($"[{ValuableValueMod.Name}] valuables sell at {mult}x: {seen} items, {changed} patched ({(World.IsServer() ? "server" : "client")}).");
        }

        /// <summary>GetCoinValue's price for a negative sellValue (non-cooked, variation 0 path).</summary>
        private static int DerivedSellValue(ref PugDatabase.EntityObjectInfo info, BlobAssetReference<PugDatabase.PugDatabaseBank> blob)
        {
            int num = RaritySellValue(info.rarity);
            int craft = 0;
            ref var required = ref info.requiredObjectsToCraft;
            for (int i = 0; i < required.Length; i++)
            {
                ref var ingredient = ref PugDatabase.GetEntityObjectInfo(required[i].objectID, blob);
                if (ingredient.sellValue != 0) craft += RaritySellValue(ingredient.rarity) * required[i].amount;
            }
            if (craft > 0) num = (int)math.round(math.max(1f, num * 0.3f) + craft);
            float jitter = Unity.Mathematics.Random.CreateFromIndex((uint)info.objectID).NextFloat(-0.1f, 0.1f);
            return math.max(1, num + (int)math.round(num * jitter));
        }

        private static int RaritySellValue(Rarity rarity) => 1 + math.max(0, (int)rarity) * 5;
    }
}
