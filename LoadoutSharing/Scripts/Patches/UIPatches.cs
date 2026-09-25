using HarmonyLib;
using UnityEngine;

namespace LoadoutSharing.Patches
{
    /// <summary>Draw inherited items dimmed so you can tell them from the loadout's own gear.</summary>
    [HarmonyPatch(typeof(InventorySlotUI), "UpdateSlot")]
    public static class SlotDimPatch
    {
        private static readonly Color InheritedTint = new Color(1f, 1f, 1f, 0.45f);

        private static void Postfix(InventorySlotUI __instance)
        {
            try
            {
                if (__instance == null || __instance.icon == null || __instance.icon.sprite == null) return;
                if (!HandlerSync.TryKindOf(__instance.slotType, out _)) return;

                // Vanilla's UpdateSlot returns early (without recoloring) when the item in the slot
                // hasn't changed, so we must both apply AND undo the dim ourselves every frame.
                var c = __instance.icon.color;
                bool inherited = HandlerSync.IsSlotInherited(__instance.slotType);
                float wantAlpha = inherited ? InheritedTint.a : 1f;
                if (!Mathf.Approximately(c.a, wantAlpha))
                    __instance.icon.color = new Color(c.r, c.g, c.b, wantAlpha);
            }
            catch (System.Exception)
            {
                // Purely cosmetic; never let it break the UI loop.
            }
        }
    }

    /// <summary>
    /// While a click on an equipment slot is processed, operate on the loadout's OWN slot:
    /// placing an item creates an override, clicking an inherited item does nothing.
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotUI), "OnLeftClicked")]
    public static class LeftClickRedirectPatch
    {
        private static void Prefix(InventorySlotUI __instance)
        {
            if (__instance != null && HandlerSync.TryKindOf(__instance.slotType, out _))
                HandlerSync.RedirectToPrivate();
        }

        private static void Postfix() => HandlerSync.SyncToEffective();
    }

    [HarmonyPatch(typeof(InventorySlotUI), "OnRightClicked", new[] { typeof(bool), typeof(bool), typeof(int) })]
    public static class RightClickRedirectPatch
    {
        private static void Prefix(InventorySlotUI __instance)
        {
            if (__instance != null && HandlerSync.TryKindOf(__instance.slotType, out _))
                HandlerSync.RedirectToPrivate();
        }

        private static void Postfix() => HandlerSync.SyncToEffective();
    }

    /// <summary>Shift-click / quick-equip from the inventory goes into the loadout's own slot too.</summary>
    [HarmonyPatch(typeof(UIManager), "AttemptToEquipItem")]
    public static class QuickEquipRedirectPatch
    {
        private static void Prefix() => HandlerSync.RedirectToPrivate();
        private static void Postfix() => HandlerSync.SyncToEffective();
    }
}
