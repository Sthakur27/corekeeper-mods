using System.Collections.Generic;
using System.Text;
using ModOptions;
using PugMod;
using UnityEngine;

namespace SidsOverhaul
{
    /// <summary>
    /// Per-feature on/off switches (Mod Options > "Features On/Off"). A switched-off feature does nothing:
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
        public const string PageTitle = "Features On/Off";
        public const string KeyPrefix = "Feature: ";
        private const string ConfigPath = SidsOverhaulMod.Name + "/config.cfg";

        /// <summary>
        /// Switchable features: mod folder (= key used by the injected guards) and the label shown in the menu.
        /// Loadout Fallback, Five Loadouts and Ender Stash are not listed: they add inventory slots, and
        /// a player saved without those slots loses the items in them.
        /// </summary>
        public static readonly string[,] All =
        {
            { "AutoReplant", "Auto Replant" },
            { "QuickBuff", "Quick Buff" },
            { "BuffDurationFloor", "Buff Duration Floor" },
            { "PotionSeller", "Potion Seller" },
            { "FasterMushrooms", "Faster Mushrooms" },
            { "BiggerWateringCans", "Bigger Watering Cans" },
            { "FastAutoFishing", "Fast Auto Fishing" },
            { "BetterFishingLoot", "Better Fishing Loot" },
            { "DurabilityMultiplier", "Durability Multiplier" },
            { "VehicleSpeed", "Vehicle Speed" },
            { "GoldenChance", "Golden Chance" },
            { "DifficultyTuning", "Difficulty Tuning" },
            { "SkillXPMultiplier", "Skill XP Multiplier" },
            { "KeepMinions", "Keep Minions On Teleport" },
            { "PetEditor", "Pet Editor" },
            { "BossBonusLoot", "Boss Bonus Loot" },
            { "InfiniteOreBoulders", "Infinite Ore Boulders" },
            { "StimHits", "Stim Hits" },
            { "ArmorDye", "Armor Dye" },
        };

        public const string Hint = "Switch whole features on or off. Changes take effect after restarting the game. "
            + "Loadout Fallback, Five Loadouts and Ender Stash are always on (they hold items in extra inventory slots). "
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
            for (int i = 0; i < All.GetLength(0); i++)
                page.Toggle(out _, All[i, 1], true);
        }

        private static Dictionary<string, bool> ReadSnapshot()
        {
            var result = new Dictionary<string, bool>();
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
                        if (byLabel.TryGetValue(line.Substring(0, eq).Trim(), out string key))
                            result[key] = line.Substring(eq + 1).Trim().ToLowerInvariant() != "false";
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{SidsOverhaulMod.Name}] Could not read feature switches, all features on: {e.Message}");
            }

            var off = new List<string>();
            foreach (var kv in result)
                if (!kv.Value) off.Add(kv.Key);
            Debug.Log($"[{SidsOverhaulMod.Name}] Features switched off: {(off.Count == 0 ? "none" : string.Join(", ", off))}");
            return result;
        }
    }
}
