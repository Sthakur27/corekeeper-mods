using HarmonyLib;
using PlayerEquipment;
using Unity.Entities;
using UnityEngine;

namespace BuildTools.Patches
{
    /// <summary>
    /// Draws the placement brush. Vanilla's PlacementHandler.UpdatePlaceIcon (client, managed, every
    /// frame) moves the 1x1 icon with PlacementIcon.SetPosition, which starts a short slide whenever the
    /// target changes. Moving the icon a second time afterwards made the two targets alternate every
    /// frame, so the slide restarted forever and the preview froze in place. Instead, the target vanilla
    /// passes in is shifted to the brush corner (one SetPosition per frame, one target), and the icon is
    /// stretched over the whole brush afterwards.
    /// </summary>
    [HarmonyPatch(typeof(PlacementHandler), nameof(PlacementHandler.UpdatePlaceIcon))]
    public static class BrushPreviewPatch
    {
        internal static PlacementIcon ShiftIcon;
        internal static Vector3Int Shift;

        [HarmonyPrefix]
        public static void Prefix(PlacementHandler __instance, ref PlacementCD placementCD, EquipmentSlotCD equipmentSlotCD, Entity placementPrefab)
        {
            ShiftIcon = null;
            if (!Applies(__instance, equipmentSlotCD, placementPrefab)) return;
            var extent = Brush.Extent(BuildToolsMod.ClientBrush.Shape, BuildToolsMod.ClientBrush.Edge);
            var anchor = placementCD.bestPositionToPlaceAt;
            var corner = Brush.Corner(anchor, extent);
            Shift = EntityMonoBehaviour.ToRenderFromWorld(new Vector3Int(corner.x, corner.y, corner.z))
                    - EntityMonoBehaviour.ToRenderFromWorld(new Vector3Int(anchor.x, anchor.y, anchor.z));
            ShiftIcon = __instance.placeableIcon;
        }

        [HarmonyPostfix]
        public static void Postfix(PlacementHandler __instance, EquipmentSlotCD equipmentSlotCD, Entity placementPrefab)
        {
            ShiftIcon = null;
            if (!Applies(__instance, equipmentSlotCD, placementPrefab)) return;
            var extent = Brush.Extent(BuildToolsMod.ClientBrush.Shape, BuildToolsMod.ClientBrush.Edge);
            __instance.placeableIcon.SetSize(extent.x, extent.y);
        }

        private static bool Applies(PlacementHandler handler, EquipmentSlotCD slot, Entity prefab)
        {
            return BuildToolsMod.ClientBrush.Active && handler.placeableIcon != null &&
                   slot.slotType == EquipmentSlotType.PlaceObjectSlot && prefab == BuildToolsMod.ClientBrush.Prefab;
        }
    }

    /// <summary>Shifts the one SetPosition call made inside UpdatePlaceIcon (see <see cref="BrushPreviewPatch"/>).</summary>
    [HarmonyPatch(typeof(PlacementIcon), nameof(PlacementIcon.SetPosition))]
    public static class BrushPreviewPositionPatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlacementIcon __instance, ref Vector3Int position)
        {
            if (BrushPreviewPatch.ShiftIcon != null && __instance == BrushPreviewPatch.ShiftIcon)
                position += BrushPreviewPatch.Shift;
        }
    }
}
