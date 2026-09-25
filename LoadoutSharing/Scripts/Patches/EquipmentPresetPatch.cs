using HarmonyLib;

namespace LoadoutSharing.Patches
{
    /// <summary>
    /// Vanilla's preset switch re-points the helm/chest/pants/necklace/rings/off-hand UI
    /// handlers from the preset table and assumes bag, lantern and pet never move. We re-point
    /// all ten at the effective (fallback-aware) slot and refresh the three vanilla skips.
    /// </summary>
    [HarmonyPatch(typeof(EquipmentHandler), "UpdateEquipmentPreset")]
    public static class EquipmentPresetPatch
    {
        private static void Postfix(int presetIndex)
        {
            try
            {
                HandlerSync.SyncToEffective();

                var equipmentUI = Manager.ui?.equipmentInventoryUI;
                if (equipmentUI?.itemSlots == null) return;
                foreach (var slot in equipmentUI.itemSlots)
                {
                    if (slot == null) continue;
                    if (slot.slotType == ItemSlotsUIType.BagSlot ||
                        slot.slotType == ItemSlotsUIType.LanternSlot ||
                        slot.slotType == ItemSlotsUIType.PetSlot)
                    {
                        slot.UpdateSlot();
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[{LoadoutSharingMod.Name}] EquipmentPresetPatch failed (preset {presetIndex}): {ex}");
            }
        }
    }
}
