using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossBonusLoot.Systems
{
    /// <summary>
    /// Pins chosen items to a fixed chance in a loot table's own random roll, taking the extra share from that
    /// table's empty ("nothing") entry, so every other drop keeps exactly its vanilla chance.
    ///
    /// Infected Caveling (LootTableID 490, one roll per kill, vanilla total weight 2.3225):
    /// Tome of the Dead (TomeOfMelee) 0.43% -> 5%, Mold Ring and Mold Vein Necklace 0.215% -> 4% each,
    /// Moth pet egg 1.72% -> 2%. "Nothing" goes from 77.1% to 64.6%.
    ///
    /// The table total is kept at its vanilla value: pinned weight = chance x total, and the empty entry loses
    /// what the pinned entries gained. Vanilla weights are remembered the first time a slot is seen (the blob is
    /// built once per process from the vanilla asset), so re-applying is idempotent. If the empty entry is too
    /// small to absorb the change, the table is left vanilla and an error is logged. Same blob patch as
    /// <see cref="HydraGemSystem"/>, in both worlds so client prediction matches the server.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class TomeDropSystem : PugSimulationSystemBase
    {
        private static readonly Dictionary<LootTableID, Dictionary<ObjectID, float>> Pinned =
            new Dictionary<LootTableID, Dictionary<ObjectID, float>>
            {
                [LootTableID.InfectedCaveling] = new Dictionary<ObjectID, float>
                {
                    [ObjectID.TomeOfMelee] = 0.05f,      // Tome of the Dead
                    [ObjectID.MoldRing] = 0.04f,
                    [ObjectID.MoldVeinNecklace] = 0.04f,
                    [ObjectID.PetMothEgg] = 0.02f,
                },
            };

        // (table, slot) -> vanilla weight, remembered on first sight.
        private static readonly Dictionary<(LootTableID, int), float> Vanilla = new Dictionary<(LootTableID, int), float>();

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
                if (!Pinned.TryGetValue(table.lootTableID, out var pins)) continue;
                var id = table.lootTableID;
                int count = table.lootTable.Length;

                float total = 0f;
                int empty = -1;
                for (int n = 0; n < count; n++)
                {
                    var key = (id, n);
                    if (!Vanilla.TryGetValue(key, out float w)) Vanilla[key] = w = table.lootTable[n].weight;
                    total += w;
                    if (table.lootTable[n].objectID == ObjectID.None && empty < 0) empty = n;
                }
                if (empty < 0 || total <= 0f)
                {
                    Debug.LogError($"[{BossBonusLootMod.Name}] {id}: no empty entry to take pinned drops from; left vanilla.");
                    continue;
                }

                float gained = 0f;
                for (int n = 0; n < count; n++)
                    if (pins.TryGetValue(table.lootTable[n].objectID, out float chance))
                        gained += chance * total - Vanilla[(id, n)];
                float emptyWeight = Vanilla[(id, empty)] - gained;
                if (emptyWeight < 0f)
                {
                    Debug.LogError($"[{BossBonusLootMod.Name}] {id}: pinned chances exceed the empty roll; left vanilla.");
                    continue;
                }

                for (int n = 0; n < count; n++)
                {
                    ref var entry = ref table.lootTable[n];
                    float target = n == empty ? emptyWeight
                        : pins.TryGetValue(entry.objectID, out float chance) ? chance * total
                        : Vanilla[(id, n)];
                    entry.weight = target;
                }
                Debug.Log($"[{BossBonusLootMod.Name}] {id}: pinned {pins.Count} drops, nothing {Vanilla[(id, empty)] / total:P1} -> {emptyWeight / total:P1} in {World.Name}.");
            }
        }
    }
}
