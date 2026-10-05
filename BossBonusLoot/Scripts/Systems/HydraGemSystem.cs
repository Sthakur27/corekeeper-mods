using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossBonusLoot.Systems
{
    /// <summary>
    /// Doubles the biome gemstone amounts (Jungle Emerald, Ocean Sapphire, Desert Ruby) in the three
    /// Hydra boss loot tables. Patches the LootTableBank blob once per world in both client and
    /// server, like BetterFishingLoot. Vanilla ranges are remembered on first sight so re-applying
    /// (new world, shared blob) never stacks.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class HydraGemSystem : PugSimulationSystemBase
    {
        private const int Multiplier = 2;

        /// <summary>Loot tables whose gem entries get weight 0 (BossBonusLootSystem rolls these gems itself).</summary>
        private static readonly HashSet<LootTableID> ZeroGemTables = new HashSet<LootTableID> { LootTableID.Mimite, LootTableID.OrbitalTurret };

        private static readonly HashSet<LootTableID> Tables = new HashSet<LootTableID>
        {
            LootTableID.HydraBossNature, LootTableID.HydraBossSea, LootTableID.HydraBossDesert,
        };

        private static readonly HashSet<ObjectID> Gems = new HashSet<ObjectID>
        {
            ObjectID.NatureGemstone, ObjectID.SeaGemstone, ObjectID.DesertGemstone,
        };

        // (table, guaranteed list?, slot) -> vanilla (min, max)
        private static readonly Dictionary<(LootTableID, bool, int), (int min, int max)> Vanilla =
            new Dictionary<(LootTableID, bool, int), (int, int)>();

        private EntityQuery _bank;
        private int _appliedBankHash;

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
            if (hash == _appliedBankHash) return;
            _appliedBankHash = hash;

            var log = new List<string>();
            ref LootTableBankBlob root = ref bankCD.Value.Value;
            for (int i = 0; i < root.lootTables.Length; i++)
            {
                ref EntityLootTable table = ref root.lootTables[i];
                if (ZeroGemTables.Contains(table.lootTableID))
                {
                    log.Add(DescribeAndZeroGems(table.lootTableID, ref table));
                    continue;
                }
                if (!Tables.Contains(table.lootTableID)) continue;
                Patch(table.lootTableID, false, ref table.lootTable, log);
                Patch(table.lootTableID, true, ref table.guaranteedDropsLootTable, log);
            }
            Debug.Log($"[{BossBonusLootMod.Name}] Loot tables ({World.Name}): Hydra gems x{Multiplier}; "
                      + (log.Count > 0 ? string.Join(", ", log) : "no gem entries found"));
        }

        /// <summary>Logs the whole table (vanilla values) and sets the weight of biome gem entries to 0.</summary>
        private static string DescribeAndZeroGems(LootTableID id, ref EntityLootTable table)
        {
            var parts = new List<string>();
            for (int n = 0; n < table.lootTable.Length; n++)
            {
                ref EntityLootInfo e = ref table.lootTable[n];
                var key = (id, false, n);
                if (!VanillaWeight.TryGetValue(key, out float w))
                {
                    w = e.weight;
                    VanillaWeight[key] = w;
                }
                parts.Add($"{e.objectID} w{w:0.###} x{e.amount.min}-{e.amount.max}");
                if (Gems.Contains(e.objectID)) e.weight = 0f;
            }
            for (int n = 0; n < table.guaranteedDropsLootTable.Length; n++)
            {
                ref EntityLootInfo e = ref table.guaranteedDropsLootTable[n];
                parts.Add($"guaranteed {e.objectID} x{e.amount.min}-{e.amount.max}");
            }
            return $"{id} table (unique drops {table.minUniqueDrops}-{table.maxUniqueDrops}): {string.Join("; ", parts)}; gem weights zeroed";
        }

        private static readonly Dictionary<(LootTableID, bool, int), float> VanillaWeight =
            new Dictionary<(LootTableID, bool, int), float>();

        private static void Patch(LootTableID id, bool guaranteed, ref BlobArray<EntityLootInfo> entries, List<string> log)
        {
            for (int n = 0; n < entries.Length; n++)
            {
                ref EntityLootInfo e = ref entries[n];
                if (!Gems.Contains(e.objectID)) continue;
                var key = (id, guaranteed, n);
                if (!Vanilla.TryGetValue(key, out var v))
                {
                    v = (e.amount.min, e.amount.max);
                    Vanilla[key] = v;
                }
                e.amount.min = v.min * Multiplier;
                e.amount.max = v.max * Multiplier;
                log.Add($"{id}{(guaranteed ? "[guaranteed]" : "")} {e.objectID} {v.min}-{v.max} -> {e.amount.min}-{e.amount.max}");
            }
        }
    }
}
