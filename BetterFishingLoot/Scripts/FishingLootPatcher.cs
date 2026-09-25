using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace BetterFishingLoot
{
    /// <summary>
    /// Rewrites entry weights inside a LootTableBankBlob. Only tables whose LootTableID name ends in
    /// "FishingLoot" (DirtFishingLoot, LarvaFishingLoot, ... PassageFishingLoot) are touched; the
    /// *Fishes tables are left alone.
    ///
    /// An entry counts as rare when the item is equipment (armor, accessories, bags, lanterns,
    /// pouches, weapons, tools, pets) or its rarity is Rare or better. Everything else (kelp, ore,
    /// scrap, fiber, keys, chests, bombs, seeds...) keeps its vanilla weight.
    ///
    /// Vanilla weights are remembered per (table, slot, item) the first time an entry is seen. The
    /// LootTableBank data is built once per process from the vanilla asset and every world's blob is
    /// converted from it, so that first sighting is always the vanilla value and re-applying is
    /// idempotent: weight = vanilla × multiplier, no matter how many worlds or how often the setting
    /// changes. The client and server worlds of a single-player game share one blob (deduplicated by
    /// BlobAssetStore); writing the same values twice is harmless.
    /// </summary>
    public static class FishingLootPatcher
    {
        private struct Key
        {
            public LootTableID table;
            public int slot;
            public ObjectID objectID;

            public override int GetHashCode() => ((int)table * 397 + slot) * 397 + (int)objectID;
            public override bool Equals(object obj) => obj is Key k && k.table == table && k.slot == slot && k.objectID == objectID;
        }

        private static readonly Dictionary<Key, float> _vanillaWeights = new Dictionary<Key, float>();
        private static readonly Dictionary<LootTableID, bool> _isFishingLootTable = new Dictionary<LootTableID, bool>();
        private static float _lastLoggedMultiplier = float.NaN;

        public static bool IsFishingLootTable(LootTableID id)
        {
            if (!_isFishingLootTable.TryGetValue(id, out bool result))
            {
                result = id.ToString().EndsWith("FishingLoot");
                _isFishingLootTable[id] = result;
            }
            return result;
        }

        /// <summary>Equipment, or anything the game marks Rare/Epic/Legendary.</summary>
        public static bool IsRareEntry(ObjectID objectID, out ObjectInfo info)
        {
            info = null;
            if (objectID == ObjectID.None) return false;
            if (!PugDatabase.TryGetObjectInfo(objectID, out info) || info == null) return false;
            if (info.rarity >= Rarity.Rare) return true;
            int type = (int)info.objectType;
            if (type >= 100 && type <= 108) return true;      // Helm..Pouch (armor, necklace, ring, offhand, bag, lantern, pouch)
            if (type >= 500 && type <= 502) return true;      // melee / range / summoning weapons
            if (type >= 600 && type <= 610) return true;      // shovel, hoe, casting item, pick, fishing rod, sledge, drill, beam...
            if (info.objectType == ObjectType.Pet) return true;
            return false;
        }

        /// <summary>Applies <paramref name="multiplier"/> to every rare entry of every fishing loot table in the blob.</summary>
        public static void Apply(BlobAssetReference<LootTableBankBlob> bank, float multiplier, string worldName)
        {
            if (!bank.IsCreated) return;
            bool log = _lastLoggedMultiplier != multiplier;
            StringBuilder sb = log ? new StringBuilder() : null;
            int tablesTouched = 0, entriesBoosted = 0;

            ref LootTableBankBlob root = ref bank.Value;
            for (int i = 0; i < root.lootTables.Length; i++)
            {
                ref EntityLootTable table = ref root.lootTables[i];
                if (!IsFishingLootTable(table.lootTableID)) continue;
                tablesTouched++;

                float totalBefore = 0f, totalAfter = 0f, rareBefore = 0f, rareAfter = 0f;
                List<string> lines = log ? new List<string>() : null;

                for (int n = 0; n < table.lootTable.Length; n++)
                {
                    ref EntityLootInfo entry = ref table.lootTable[n];
                    Key key = new Key { table = table.lootTableID, slot = n, objectID = entry.objectID };
                    if (!_vanillaWeights.TryGetValue(key, out float vanilla))
                    {
                        vanilla = entry.weight;
                        _vanillaWeights[key] = vanilla;
                    }

                    bool rare = IsRareEntry(entry.objectID, out ObjectInfo info);
                    float target = rare ? vanilla * multiplier : vanilla;
                    if (entry.weight != target) entry.weight = target;

                    totalBefore += vanilla;
                    totalAfter += target;
                    if (rare)
                    {
                        entriesBoosted++;
                        rareBefore += vanilla;
                        rareAfter += target;
                        if (lines != null)
                        {
                            string rarity = info != null ? info.rarity.ToString() : "?";
                            string type = info != null ? info.objectType.ToString() : "?";
                            lines.Add($"{entry.objectID}[{rarity}/{type}] w {vanilla:0.####}->{target:0.####}");
                        }
                    }
                }

                if (log && lines.Count > 0)
                {
                    float pBefore = totalBefore > 0f ? 100f * rareBefore / totalBefore : 0f;
                    float pAfter = totalAfter > 0f ? 100f * rareAfter / totalAfter : 0f;
                    sb.AppendLine($"  {table.lootTableID}: {lines.Count}/{table.lootTable.Length} rare entries, P(rare) {pBefore:0.0}% -> {pAfter:0.0}%: {string.Join(", ", lines)}");
                }
            }

            if (log)
            {
                _lastLoggedMultiplier = multiplier;
                Debug.Log($"[{BetterFishingLootMod.Name}] Applied {multiplier:0.##}x to {entriesBoosted} rare entries in {tablesTouched} fishing loot tables ({worldName}).\n{sb}");
            }
        }
    }
}
