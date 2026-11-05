using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Enchanted items get a violet slot border instead of their rarity color. Vanilla
    /// InventorySlotUI.UpdateSlot colors the border (background) by rarity only when the slot's item changes,
    /// so we tint after it and remember the color we replaced: when the same item loses its enchants (remove
    /// button) we put that color back; when the item changes, vanilla has already recolored the slot.
    /// The quick-trash and locking cursor modes recolor player slots themselves, so those are left alone.
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.UpdateSlot))]
    public static class EnchantSlotBorderPatch
    {
        private static readonly Color Violet = new Color(0.72f, 0.42f, 1f, 1f);

        private struct Tinted
        {
            public ObjectID Id;
            public int Aux;
            public Color Original;
        }

        private static readonly Dictionary<InventorySlotUI, Tinted> State = new Dictionary<InventorySlotUI, Tinted>();

        public static void Postfix(InventorySlotUI __instance)
        {
            var border = __instance.background;
            if (border == null) return;
            var mode = Manager.ui.mouse.mouseMode;
            if (mode == UIMouse.MouseMode.QuickTrash || mode == UIMouse.MouseMode.Locking) return;

            var item = __instance.GetContainedObject();
            bool enchanted = item.objectID != ObjectID.None && item.auxDataIndex != 0
                             && EnchantStorage.HasAnyClient(item);
            bool had = State.TryGetValue(__instance, out var tinted);
            bool sameItem = had && tinted.Id == item.objectID && tinted.Aux == item.auxDataIndex;

            if (enchanted)
            {
                if (!sameItem) State[__instance] = new Tinted { Id = item.objectID, Aux = item.auxDataIndex, Original = border.color };
                border.color = Violet;
            }
            else if (had)
            {
                if (sameItem) border.color = tinted.Original; // enchants removed from the same item
                State.Remove(__instance);
            }
        }
    }
}
