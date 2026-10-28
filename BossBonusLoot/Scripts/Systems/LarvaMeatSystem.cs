using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossBonusLoot.Systems
{
    /// <summary>
    /// Multiplies the Larva Meat amount in every loot table that has it (larvae, Ghorm's own table,
    /// ...) by the "Larva Meat drops" setting. Same blob patch as <see cref="HydraGemSystem"/>, in
    /// both worlds; vanilla ranges are remembered on first sight, and the tables are rewritten
    /// whenever the setting changes. The fixed Ghorm bonus in BossBonusLootSystem is not scaled.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class LarvaMeatSystem : PugSimulationSystemBase
    {
        // (table, guaranteed list?, slot) -> vanilla (min, max)
        private static readonly Dictionary<(LootTableID, bool, int), (int min, int max)> Vanilla =
            new Dictionary<(LootTableID, bool, int), (int, int)>();

        private EntityQuery _bank;
        private int _appliedBankHash;
        private int _appliedVersion = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _bank = GetEntityQuery(ComponentType.ReadOnly<LootTableBankCD>());
            RequireForUpdate(_bank);
        }

        protected override void OnUpdate()
        {
            if (!_bank.TryGetSingleton(out LootTableBankCD bankCD) || !bankCD.Value.IsCreated) return;
            int hash = bankCD.Value.GetHashCode();
            if (hash == _appliedBankHash && _appliedVersion == BossBonusLootMod.LarvaMeatVersion) return;
            _appliedBankHash = hash;
            _appliedVersion = BossBonusLootMod.LarvaMeatVersion;

            int mult = BossBonusLootMod.LarvaMeatMultiplier;
            var log = new List<string>();
            ref LootTableBankBlob root = ref bankCD.Value.Value;
            for (int i = 0; i < root.lootTables.Length; i++)
            {
                ref EntityLootTable table = ref root.lootTables[i];
                Patch(table.lootTableID, false, ref table.lootTable, mult, log);
                Patch(table.lootTableID, true, ref table.guaranteedDropsLootTable, mult, log);
            }
            Debug.Log($"[{BossBonusLootMod.Name}] Larva Meat x{mult} ({World.Name}): "
                      + (log.Count > 0 ? string.Join(", ", log) : "no Larva Meat entries found"));
        }

        private static void Patch(LootTableID id, bool guaranteed, ref BlobArray<EntityLootInfo> entries, int mult, List<string> log)
        {
            for (int n = 0; n < entries.Length; n++)
            {
                ref EntityLootInfo e = ref entries[n];
                if (e.objectID != ObjectID.LarvaMeat) continue;
                var key = (id, guaranteed, n);
                if (!Vanilla.TryGetValue(key, out var v))
                {
                    v = (e.amount.min, e.amount.max);
                    Vanilla[key] = v;
                }
                e.amount.min = v.min * mult;
                e.amount.max = v.max * mult;
                log.Add($"{id}{(guaranteed ? "[guaranteed]" : "")} {v.min}-{v.max} -> {e.amount.min}-{e.amount.max}");
            }
        }
    }
}
