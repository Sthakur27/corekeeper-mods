using HarmonyLib;
using UnityEngine;

namespace PotionSeller.Patches
{
    /// <summary>
    /// The merchant buy window (BuyUI -> BuyInventoryUI) instantiates a fixed MAX_ROWS x MAX_COLUMNS slot grid
    /// (3x3 in vanilla) in ItemSlotsUIContainer.Init and only ever activates that many slots, whatever the
    /// merchant's inventory size. Both limits are virtual getters, so raise them to the modded grid before
    /// the slots are instantiated. Everything else in InventoryUI is driven by the InventoryHandler
    /// (columns = InventoryBuffer.sizeX, rows = ceil(size / columns)) and lays the slots out centred on
    /// the container origin, so only the background needs help (below).
    /// </summary>
    [HarmonyPatch(typeof(BuyInventoryUI), "MAX_ROWS", MethodType.Getter)]
    public static class BuyInventoryRowsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (__result < MerchantStock.Rows) __result = MerchantStock.Rows;
        }
    }

    [HarmonyPatch(typeof(BuyInventoryUI), "MAX_COLUMNS", MethodType.Getter)]
    public static class BuyInventoryColumnsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (__result < MerchantStock.Columns) __result = MerchantStock.Columns;
        }
    }

    /// <summary>
    /// Grows the buy window background with the grid and keeps its top edge where vanilla puts it (the
    /// slots are centred on the container origin, so a taller grid would otherwise grow off the top of the
    /// screen). Sized from the live InventoryHandler, so vending machines (which share this window) keep
    /// their vanilla look. Idempotent: vanilla sizes are captured on the first call and the original method
    /// re-sets root.localPosition on every call.
    /// </summary>
    [HarmonyPatch(typeof(BuyUI), "ShowContainerUI")]
    public static class BuyUIShowPatch
    {
        private const int VanillaColumns = 3;
        private const int VanillaRows = 3;

        private static bool _captured;
        private static Vector2 _bgSize;
        private static Vector3 _bgPos;
        private static Vector3 _titlePos;

        [HarmonyPostfix]
        public static void Postfix(BuyUI __instance)
        {
            var inv = __instance.buyInventory;
            if (inv == null || __instance.root == null) return;

            int cols = VanillaColumns;
            int rows = VanillaRows;
            var handler = Manager.main != null && Manager.main.player != null ? Manager.main.player.activeBuyInventoryHandler : null;
            if (handler != null && handler.columns > 0 && handler.size > 0)
            {
                cols = Mathf.Min(handler.columns, inv.MAX_COLUMNS);
                rows = Mathf.Min(Mathf.CeilToInt((float)handler.size / handler.columns), inv.MAX_ROWS);
            }
            float dx = Mathf.Max(0, cols - VanillaColumns) * inv.spread;
            float dy = Mathf.Max(0, rows - VanillaRows) * inv.spread;

            var bg = __instance.background;
            if (!_captured)
            {
                _captured = true;
                if (bg != null) { _bgSize = bg.size; _bgPos = bg.transform.localPosition; }
                if (__instance.title != null) _titlePos = __instance.title.transform.localPosition;
            }

            if (bg != null)
            {
                if (bg.drawMode != SpriteDrawMode.Simple) bg.size = _bgSize + new Vector2(dx, dy);
                else bg.transform.localScale = new Vector3(_bgSize.x > 0 ? (_bgSize.x + dx) / _bgSize.x : 1f, _bgSize.y > 0 ? (_bgSize.y + dy) / _bgSize.y : 1f, 1f);
                bg.transform.localPosition = _bgPos;
            }

            // Original sets root.localPosition fresh each call; push it down by half the extra height.
            __instance.root.transform.localPosition += new Vector3(0f, -dy / 2f, 0f);

            // The title sits above the background; if it is inside root it moved down with it, so lift it
            // back up by the amount the background grew upwards.
            if (__instance.title != null && __instance.title.transform.IsChildOf(__instance.root.transform))
            {
                __instance.title.transform.localPosition = _titlePos + new Vector3(0f, dy / 2f, 0f);
            }
        }
    }
}
