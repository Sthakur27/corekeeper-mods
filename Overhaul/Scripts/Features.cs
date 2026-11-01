using System.Collections.Generic;
using System.Text;
using ModOptions;
using PugMod;
using UnityEngine;

namespace SidsOverhaul
{
    /// <summary>
    /// Per-feature on/off switches (Mod Options > "Turn Sub-Mods On/Off", pinned at the top). A switched-off feature does nothing:
    /// release/build_overhaul.py adds a <c>Features.On("Key")</c> guard to every Harmony patch class
    /// (<c>Prepare</c>), IMod method, system <c>OnUpdate</c> and chat command of that feature. The standalone
    /// mods are not changed.
    ///
    /// Harmony patches are applied before any IMod.EarlyInit, so the switches are read straight from the
    /// config file (API.ConfigFilesystem is ready by then) and frozen for the session: changes take
    /// effect after a restart.
    /// </summary>
    public static class Features
    {
        public const string PageTitle = "Turn Sub-Mods On/Off";
        public const string KeyPrefix = "Feature: "; // kept from "Features On/Off" so saved switches carry over
        private const string ConfigPath = SidsOverhaulMod.Name + "/config.cfg";

        /// <summary>
        /// Switchable features: mod folder (= key used by the injected guards) and the label shown in the menu.
        /// Loadout Fallback, Five Loadouts, Ender Stash and Runic Storage are not listed: they add inventory slots, and
        /// a player saved without those slots loses the items in them.
        /// </summary>
        public static readonly string[,] All =
        {
            { "AutoReplant", "Auto Replant" },
            { "QuickBuff", "Quick Buff" },
            { "BuffDurationFloor", "Longer Buffs" },
            { "PotionSeller", "Potion Seller" },
            { "FishSeller", "Fish Seller" },
            { "MerchantRestock", "Merchant Restock" },
            { "FasterMushrooms", "Faster Mushrooms" },
            { "BiggerWateringCans", "Bigger Watering Cans" },
            { "FastAutoFishing", "Fast Auto Fishing" },
            { "BetterFishingLoot", "Better Fishing Loot" },
            { "DurabilityMultiplier", "Durability Multiplier" },
            { "VehicleSpeed", "Vehicle Speed" },
            { "GoldenChance", "Golden Chance" },
            { "DifficultyTuning", "Difficulty Tuning" },
            { "SkillXPMultiplier", "Skill XP & Talent Points" },
            { "KeepMinions", "Keep Minions On Teleport" },
            { "PetEditor", "Pet Editor" },
            { "BossBonusLoot", "Boss Bonus Loot" },
            { "InfiniteOreBoulders", "Infinite Ore Boulders" },
            { "StimHits", "Hit Sound Effects" },
            { "ArmorDye", "Armor Dye" },
            { "MushroomSlayer", "Mushroom Slayer" },
            { "MagicMirror", "Magic Mirror" },
            { "BuildTools", "Build Tools" },
            { "FasterMachines", "Faster Machines" },
            { "ItemSpawner", "Item Spawner" },
        };

        public const string Hint = "Turn whole sub-mods on or off. Changes take effect after restarting the game. "
            + "Shared Loadout Gear & Vanity, Five Loadouts, Ender Stash and Runic Storage are always on (they hold items in extra inventory slots). "
            + "In multiplayer, use the same switches as the host.";

        private static Dictionary<string, bool> _snapshot;

        /// <summary>False when the player switched <paramref name="key"/> (a mod folder name) off. Fixed for the session.</summary>
        public static bool On(string key)
        {
            if (_snapshot == null) _snapshot = ReadSnapshot();
            return !_snapshot.TryGetValue(key, out bool on) || on;
        }

        /// <summary>Adds one toggle per feature to the switches page (same keys the snapshot reads).</summary>
        public static void RegisterSettings(SettingsPage page)
        {
            // Seed renamed switches from the saved value (read via the snapshot, which knows the old labels)
            // before binding any new config keys.
            page.PinToTop();
            for (int i = 0; i < All.GetLength(0); i++)
                page.Toggle(out _, All[i, 1], !LegacyLabels.ContainsValue(All[i, 0]) || On(All[i, 0]));
        }

        /// <summary>Earlier menu labels of renamed features (old label → mod folder), so saved switches carry over.</summary>
        private static readonly Dictionary<string, string> LegacyLabels = new Dictionary<string, string>
        {
            { "Stim Hits", "StimHits" },
            { "Buff Duration Floor", "BuffDurationFloor" },
        };

        private static Dictionary<string, bool> ReadSnapshot()
        {
            var result = new Dictionary<string, bool>();
            var legacy = new Dictionary<string, bool>();
            var byLabel = new Dictionary<string, string>();
            for (int i = 0; i < All.GetLength(0); i++)
                byLabel[KeyPrefix + All[i, 1]] = All[i, 0];

            try
            {
                var fs = API.ConfigFilesystem;
                if (fs != null && fs.FileExists(ConfigPath))
                {
                    // Same format CoreLib's ConfigFile writes: "[Section]" lines and "key = value" lines.
                    string section = "";
                    foreach (string raw in Encoding.UTF8.GetString(fs.Read(ConfigPath)).Split('\n'))
                    {
                        string line = raw.Trim();
                        if (line.StartsWith("#")) continue;
                        if (line.StartsWith("[") && line.EndsWith("]")) { section = line.Substring(1, line.Length - 2); continue; }
                        int eq = line.IndexOf('=');
                        if (section != "Settings" || eq < 0) continue;
                        string label = line.Substring(0, eq).Trim();
                        bool value = line.Substring(eq + 1).Trim().ToLowerInvariant() != "false";
                        if (byLabel.TryGetValue(label, out string key))
                            result[key] = value;
                        else if (label.StartsWith(KeyPrefix) && LegacyLabels.TryGetValue(label.Substring(KeyPrefix.Length), out string oldKey))
                            legacy[oldKey] = value;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{SidsOverhaulMod.Name}] Could not read feature switches, all features on: {e.Message}");
            }

            // The new key wins even when the legacy line appears later in the file.
            foreach (var kv in legacy)
                if (!result.ContainsKey(kv.Key))
                    result[kv.Key] = kv.Value;

            var off = new List<string>();
            foreach (var kv in result)
                if (!kv.Value) off.Add(kv.Key);
            Debug.Log($"[{SidsOverhaulMod.Name}] Features switched off: {(off.Count == 0 ? "none" : string.Join(", ", off))}");
            return result;
        }
    }
}
