using System;
using System.Linq;
using System.Text;
using PugMod;
using UnityEngine;

namespace BuildTools
{
    /// <summary>
    /// Is Bigger Watering Cans running? Either as its own mod, or inside Sid's Overhaul with its
    /// "Bigger Watering Cans" switch on (read from SidsOverhaul/config.cfg the same way the Overhaul's
    /// own switches are read; a missing line means on). Checked once at startup: the loaded mods and the
    /// Overhaul switches are both fixed for the session.
    /// </summary>
    public static class WateringCanMod
    {
        private const string StandaloneName = "BiggerWateringCans";
        private const string OverhaulConfig = "SidsOverhaul/config.cfg";
        private const string OverhaulSwitch = "Feature: Bigger Watering Cans";

        private static bool? _active;

        public static bool Active => _active ?? (_active = Detect()).Value;

        private static bool Detect()
        {
            var mods = API.ModLoader.LoadedMods;
            if (mods.Any(m => m.Metadata.name == StandaloneName)) return Log(true, "standalone mod loaded");
            if (!mods.Any(m => m.Metadata.name == "SidsOverhaul")) return Log(false, "not installed");
            return Log(OverhaulSwitchOn(), "Sid's Overhaul switch");
        }

        private static bool OverhaulSwitchOn()
        {
            try
            {
                var fs = API.ConfigFilesystem;
                if (fs == null || !fs.FileExists(OverhaulConfig)) return true;
                foreach (string raw in Encoding.UTF8.GetString(fs.Read(OverhaulConfig)).Split('\n'))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq < 0 || line.StartsWith("#") || line.Substring(0, eq).Trim() != OverhaulSwitch) continue;
                    return line.Substring(eq + 1).Trim().ToLowerInvariant() != "false";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{BuildToolsMod.Name}] Could not read the Overhaul switches: {e.Message}");
            }
            return true;
        }

        private static bool Log(bool active, string why)
        {
            Debug.Log($"[{BuildToolsMod.Name}] Bigger Watering Cans {(active ? "active" : "not active")} ({why}).");
            return active;
        }
    }
}
