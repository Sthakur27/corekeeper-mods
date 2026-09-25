using HarmonyLib;

namespace FiveLoadouts.Patches
{
    /// <summary>
    /// Vanilla's "cycle equipment preset" hotkey computes (active + 1) % 3 in
    /// PlayerController.UpdateInventoryStuff and passes that to SetActiveEquipmentPreset. We
    /// cannot change the constant, so when the cycle key was pressed this frame and the
    /// requested index is exactly what the vanilla formula produces, we replace it with
    /// (active + 1) % 5. The keys for presets 1-3 are untouched; there are no vanilla input
    /// actions for presets 4 and 5 (Rewired actions cannot be added by a mod), so those are
    /// reached by clicking the tabs or by cycling.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "SetActiveEquipmentPreset")]
    public static class PresetCyclePatch
    {
        private static void Prefix(PlayerController __instance, ref int presetIndex)
        {
            try
            {
                if (__instance == null || !__instance.isLocal) return;
                var input = __instance.inputModule;
                if (input == null) return;
                if (!input.WasButtonPressedDownThisFrame(PlayerInput.InputType.EQUIP_PRESET_CYCLE)) return;

                int current = __instance.activeEquipmentPreset;
                if (presetIndex != (current + 1) % PresetLayout.VanillaPresetCount) return; // not the cycle call
                presetIndex = (current + 1) % PresetLayout.PresetCount;
            }
            catch (System.Exception)
            {
                // Leave vanilla behaviour on any surprise.
            }
        }
    }
}
