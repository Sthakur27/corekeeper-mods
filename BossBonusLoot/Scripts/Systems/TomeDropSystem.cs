using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossBonusLoot.Systems
{
    /// <summary>
    /// Raises tome drop chances in specific loot tables by pinning the tome's share of the table's random roll.
    /// Infected Caveling (LootTableID 490, one roll per kill): Tome of the Dead (TomeOfMelee) is weight 0.01
    /// of 2.3225 in vanilla, 0.43% per kill; it is pinned to 5%.
    ///
    /// The pinned weight is solved from the sum of every OTHER entry (w = p * others / (1 - p)), so the other
    /// drops keep their relative odds and only the empty roll shrinks; re-applying is idempotent. Same blob
    /// patch as <see cref="HydraGemSystem"/>, in both worlds so client prediction matches the server.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class TomeDropSystem : PugSimulationSystemBase
    {
        private static readonly Dictionary<LootTableID, (ObjectID item, float chance)> Pinned =
            new Dictionary<LootTableID, (ObjectID, float)>
            {
                [LootTableID.InfectedCaveling] = (ObjectID.TomeOfMelee, 0.05f),
            };

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

            ref LootTableBankBlob root = ref bankCD.Value.Value;
            for (int i = 0; i < root.lootTables.Length; i++)
            {
                ref EntityLootTable table = ref root.lootTables[i];
                if (!Pinned.TryGetValue(table.lootTableID, out var pin)) continue;

                float others = 0f;
                int slot = -1;
                for (int n = 0; n < table.lootTable.Length; n++)
                {
                    if (table.lootTable[n].objectID == pin.item) slot = n;
                    else others += table.lootTable[n].weight;
                }
                if (slot < 0 || others <= 0f) continue;

                float target = pin.chance * others / (1f - pin.chance);
                ref var entry = ref table.lootTable[slot];
                if (entry.weight == target) continue;
                float old = entry.weight;
                entry.weight = target;
                Debug.Log($"[{BossBonusLootMod.Name}] {table.lootTableID}: {pin.item} weight {old:0.####} -> {target:0.####} ({pin.chance:P1} per roll) in {World.Name}.");
            }
        }
    }
}
