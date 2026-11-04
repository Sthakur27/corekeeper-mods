using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace WormholePotion
{
    // Return markers are Waypoint-type map markers; show them with the potion icon
    // instead of the waypoint icon, dimmed while you cannot teleport to them.
    [HarmonyPatch(typeof(MapMarkerUIElement), "LateUpdate")]
    public static class MarkerIconPatch
    {
        private static Sprite icon;

        internal static bool IsReturnMarker(MapMarkerUIElement element)
        {
            if (element.markerType != MapMarkerType.Waypoint || element.mapMarkerEntity == Entity.Null) return false;
            var marker = WormholePotionMod.Marker;
            var world = element.world;
            return marker != ObjectID.None && world != null && world.EntityManager.Exists(element.mapMarkerEntity)
                && world.EntityManager.HasComponent<ObjectDataCD>(element.mapMarkerEntity)
                && world.EntityManager.GetComponentData<ObjectDataCD>(element.mapMarkerEntity).objectID == marker;
        }

        public static void Postfix(MapMarkerUIElement __instance)
        {
            if (__instance.sr == null || !IsReturnMarker(__instance)) return;
            if (icon == null) icon = PugDatabase.GetObjectInfo(WormholePotionMod.Potion, 0)?.icon;
            if (icon == null) return;
            // Vanilla just picked the active or inactive waypoint sprite.
            var vanilla = __instance.sr.sprite;
            bool canTeleport = vanilla == __instance.largeMapIcon || vanilla == __instance.miniMapIcon;
            __instance.sr.sprite = icon;
            __instance.sr.color = canTeleport ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }
    }

    [HarmonyPatch(typeof(MapMarkerUIElement), "GetHoverTitle")]
    public static class MarkerTitlePatch
    {
        public static void Postfix(MapMarkerUIElement __instance, ref TextAndFormatFields __result)
        {
            if (!MarkerIconPatch.IsReturnMarker(__instance)) return;
            __result = new TextAndFormatFields { text = "Wormhole Return Point", dontLocalize = true };
        }
    }
}
