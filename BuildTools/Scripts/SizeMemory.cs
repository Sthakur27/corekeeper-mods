using System;
using System.Collections.Generic;
using System.Text;
using PlayerEquipment;
using PugMod;
using UnityEngine;

namespace BuildTools
{
    /// <summary>
    /// Client-side memory of everything the player picks, saved in BuildTools/sizes.cfg so it survives
    /// quitting: one size per tool category (tool slot type: shovel, watering can, hoe, roofing gadget,
    /// seeder, paint tool), brush shape + size per placeable category, and the roofing mode. Tool sizes
    /// are raw variation indices; the game clamps them to the held item's own ceiling, so one
    /// "watering can" value serves both the 2x2 and the 4x4 can.
    /// </summary>
    public static class SizeMemory
    {
        private const string Folder = "BuildTools";
        private const string FilePath = Folder + "/sizes.cfg";

        private static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        private static bool _loaded;

        /// <summary>False = "Remember sizes" off: values live for this session only (nothing read or written).</summary>
        public static bool Persist = true;

        public static bool TryGet(EquipmentSlotType slot, out int variation) => TryGet(slot.ToString(), out variation);

        public static void Set(EquipmentSlotType slot, int variation) => Set(slot.ToString(), variation);

        public static int Get(string key, int fallback) => TryGet(key, out int value) ? value : fallback;

        public static bool TryGet(string key, out int value)
        {
            EnsureLoaded();
            return Values.TryGetValue(key, out value);
        }

        public static void Set(string key, int value)
        {
            EnsureLoaded();
            if (Values.TryGetValue(key, out int old) && old == value) return;
            Values[key] = value;
            if (Persist) Save();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (!Persist) return;
            try
            {
                if (!API.ConfigFilesystem.FileExists(FilePath)) return;
                string text = Encoding.UTF8.GetString(API.ConfigFilesystem.Read(FilePath));
                foreach (string raw in text.Split('\n'))
                {
                    string[] pair = raw.Trim().Split('=');
                    if (pair.Length != 2 || pair[0].Length == 0) continue;
                    if (!int.TryParse(pair[1], out int value)) continue;
                    Values[pair[0]] = value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{BuildToolsMod.Name}] Could not read {FilePath}: {e.Message}");
            }
        }

        private static void Save()
        {
            try
            {
                if (!API.ConfigFilesystem.DirectoryExists(Folder)) API.ConfigFilesystem.CreateDirectory(Folder);
                var sb = new StringBuilder();
                foreach (var pair in Values) sb.Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
                API.ConfigFilesystem.Write(FilePath, Encoding.UTF8.GetBytes(sb.ToString()));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{BuildToolsMod.Name}] Could not write {FilePath}: {e.Message}");
            }
        }
    }
}
