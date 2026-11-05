using System.Collections.Generic;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace WormholePotion
{
    // Return markers are Waypoint-type map markers; show them with the potion icon
    // instead of the waypoint icon, dimmed while you cannot teleport to them.
    // Runs every frame for every marker on the minimap and the big map, so the
    // "is this ours" answer is cached per UI element and only rechecked when the
    // element is pointed at another entity.
    [HarmonyPatch(typeof(MapMarkerUIElement), "LateUpdate")]
    public static class MarkerIconPatch
    {
        private static readonly Color Dimmed = new Color(1f, 1f, 1f, 0.55f);
        private static Sprite icon;
        private static World cachedWorld;
        private static readonly Dictionary<MapMarkerUIElement, (Entity entity, bool ours)> cache =
            new Dictionary<MapMarkerUIElement, (Entity, bool)>();
        // Waypoint UI elements are pooled and vanilla never resets their color, so any
        // element we dimmed is put back to white once it shows something else.
        private static readonly HashSet<MapMarkerUIElement> dimmed = new HashSet<MapMarkerUIElement>();

        internal static bool IsReturnMarker(MapMarkerUIElement element)
        {
            if (element.markerType != MapMarkerType.Waypoint) return false;
            var e = element.mapMarkerEntity;
            var marker = WormholePotionMod.Marker;
            if (e == Entity.Null || marker == ObjectID.None) return false;
            var world = element.world;
            if (world == null) return false;
            if (world != cachedWorld) ResetFor(world);
            if (cache.TryGetValue(element, out var hit) && hit.entity == e) return hit.ours;
            var em = world.EntityManager;
            bool ours = em.Exists(e) && em.HasComponent<ObjectDataCD>(e)
                && em.GetComponentData<ObjectDataCD>(e).objectID == marker;
            cache[element] = (e, ours);
            return ours;
        }

        private static void ResetFor(World world)
        {
            foreach (var element in dimmed)
                if (element != null && element.sr != null) element.sr.color = Color.white;
            dimmed.Clear();
            cache.Clear();
            cachedWorld = world;
        }

        public static void Postfix(MapMarkerUIElement __instance)
        {
            if (!IsReturnMarker(__instance))
            {
                if (dimmed.Count > 0 && dimmed.Remove(__instance) && __instance.sr != null)
                    __instance.sr.color = Color.white;
                return;
            }
            var sr = __instance.sr;
            if (sr == null) return;
            if (icon == null) icon = PugDatabase.GetObjectInfo(WormholePotionMod.Potion, 0)?.icon;
            if (icon == null) return;
            // Vanilla just picked the active or inactive waypoint sprite.
            var vanilla = sr.sprite;
            bool canTeleport = vanilla == __instance.largeMapIcon || vanilla == __instance.miniMapIcon;
            if (vanilla != icon) sr.sprite = icon;
            var color = canTeleport ? Color.white : Dimmed;
            if (sr.color != color) sr.color = color;
            if (canTeleport) dimmed.Remove(__instance);
            else dimmed.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(MapMarkerUIElement), "GetHoverTitle")]
    public static class MarkerTitlePatch
    {
        public static void Postfix(MapMarkerUIElement __instance, ref TextAndFormatFields __result)
        {
            if (!MarkerIconPatch.IsReturnMarker(__instance)) return;
            __result = new TextAndFormatFields { text = "Return Point", dontLocalize = true };
        }
    }
}
