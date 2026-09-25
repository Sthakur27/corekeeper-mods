using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FiveLoadouts
{
    /// <summary>
    /// Optional link to Sid's LoadoutSharing (Loadout Fallback) mod.
    ///
    /// LoadoutSharing keeps a private-slot table per preset and assumes three presets. The
    /// patch described in LoadoutSharing.patch.md adds
    ///   public static void SlotLayout.RegisterPreset(int preset, EquipmentCD equipment, int petSlot)
    /// which grows that table. We look the method up by name so this mod compiles and runs
    /// with or without LoadoutSharing installed (mods cannot reference each other's assemblies
    /// at compile time, and Harmony may not patch another mod's types).
    /// </summary>
    public static class LoadoutSharingBridge
    {
        private static bool _probed;
        private static MethodInfo _register;
        private static bool _presentButOld;

        /// <summary>True when LoadoutSharing is installed AND has the RegisterPreset API.</summary>
        public static bool CanRegister
        {
            get { Probe(); return _register != null; }
        }

        /// <summary>True when LoadoutSharing is installed but predates the patch.</summary>
        public static bool PresentButOld
        {
            get { Probe(); return _presentButOld; }
        }

        public static void Probe()
        {
            if (_probed) return;
            _probed = true;
            try
            {
                var type = AccessTools.TypeByName("LoadoutSharing.SlotLayout");
                if (type == null)
                {
                    Debug.Log($"[{FiveLoadoutsMod.Name}] LoadoutSharing not detected; loadouts 4 and 5 share bag/lantern/pet like vanilla 2 and 3.");
                    return;
                }
                _register = AccessTools.Method(type, "RegisterPreset", new[] { typeof(int), typeof(EquipmentCD), typeof(int) });
                if (_register == null)
                {
                    _presentButOld = true;
                    Debug.LogWarning($"[{FiveLoadoutsMod.Name}] LoadoutSharing is installed but has no SlotLayout.RegisterPreset. " +
                                     "Apply FiveLoadouts/LoadoutSharing.patch.md, otherwise its UI sync clamps to loadout 3 while loadouts 4/5 are active.");
                    return;
                }
                Debug.Log($"[{FiveLoadoutsMod.Name}] LoadoutSharing bridge ready; loadouts 4 and 5 get private bag/lantern/pet with fallback.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{FiveLoadoutsMod.Name}] LoadoutSharing probe failed: {ex.Message}");
                _register = null;
            }
        }

        public static void RegisterPreset(int preset, EquipmentCD equipment, int petSlot)
        {
            if (_register == null) return;
            try
            {
                _register.Invoke(null, new object[] { preset, equipment, petSlot });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{FiveLoadoutsMod.Name}] LoadoutSharing.RegisterPreset({preset}) failed: {ex}");
            }
        }
    }
}
