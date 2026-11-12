using System.Collections.Generic;
using HarmonyLib;

namespace AutoLoadouts
{
    /// <summary>
    /// Adds "Switches to loadout N when held" to the stats of a linked item's tooltip, wherever the item
    /// sits (hotbar, inventory, chests). Not on merchant buy slots.
    /// </summary>
    [HarmonyPatch(typeof(SlotUIBase), nameof(SlotUIBase.GetHoverStats), new[] { typeof(ContainedObjectsBuffer), typeof(bool), typeof(bool) })]
    public static class TooltipPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SlotUIBase __instance, ContainedObjectsBuffer containedObject, ref List<TextAndFormatFields> __result)
        {
            if (!(__instance is InventorySlotUI) || __instance is BuySlotUI) return;
            if (containedObject.objectID == ObjectID.None) return;
            if (!Bindings.TryGet(containedObject.objectID, out int preset)) return;

            if (__result == null) __result = new List<TextAndFormatFields>();
            __result.Add(new TextAndFormatFields
            {
                text = AutoLoadoutsMod.TooltipTerm,
                formatFields = new[] { AutoLoadoutsMod.Numeral(preset) },
                dontLocalizeFormatFields = true,
                color = Manager.ui != null ? Manager.ui.xpAndLevelTextColor : UnityEngine.Color.white,
            });
        }
    }
}
