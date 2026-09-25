using UnityEngine;

namespace FiveLoadouts
{
    /// <summary>
    /// Link to Sid's LoadoutSharing (Loadout Fallback) mod, which is a required dependency:
    /// the loader references dependency assemblies at compile time, so this is a direct call
    /// (reflection is rejected by the mod loader's code security check).
    /// SlotLayout.RegisterPreset grows LoadoutSharing's private-slot table so loadouts 4 and 5
    /// get the same fallback-to-loadout-1 behaviour as 2 and 3.
    /// </summary>
    public static class LoadoutSharingBridge
    {
        public static bool CanRegister => true;
        public static bool PresentButOld => false;

        public static void Probe()
        {
            Debug.Log($"[{FiveLoadoutsMod.Name}] LoadoutSharing bridge ready; loadouts 4 and 5 get private bag/lantern/pet with fallback.");
        }

        public static void RegisterPreset(int preset, EquipmentCD equipment, int petSlot)
        {
            try
            {
                LoadoutSharing.SlotLayout.RegisterPreset(preset, equipment, petSlot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{FiveLoadoutsMod.Name}] LoadoutSharing.RegisterPreset({preset}) failed: {ex}");
            }
        }
    }
}
