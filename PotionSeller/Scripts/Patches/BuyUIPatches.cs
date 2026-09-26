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
    /// the container origin, so only the background and the window position need help (below).
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
    /// What vanilla does: BuyUI.ShowContainerUI calls buyInventory.ShowContainerUI (InventoryUI.UpdateContainerSize
    /// positions the slots centred on the container origin: x = -(cols-1)/2*spread + k*spread), then sets
    /// root.localPosition to (-2.5, 3.3125) when the sell window is showing, else (0, 3.3125), and only swaps
    /// the background *sprite* for the theme. BuyUI.background is never sized by vanilla (it is not the
    /// InventoryUI.backgroundSR that UpdateBackgroundSize touches), so a wider grid just spills over it. The
    /// sell window position is prefab data (SellUI never moves its root).
    ///
    /// What we do: keep the vanilla root position captured after ShowContainerUI, then every frame while the
    /// window is open (postfix on BuyUI.LateUpdate, which vanilla only uses to re-apply the UI scale) grow the
    /// background by the extra columns/rows and shift the whole window LEFT by half the extra width, so its
    /// RIGHT edge stays exactly where vanilla put it and the sell window to the right is untouched. Rows are
    /// kept at the vanilla 3, so nothing grows downward into the player inventory; if the grid were ever
    /// taller the window would be pushed down by half the extra height to keep its top edge. Sized from the
    /// live InventoryHandler, so vending machines (which share this window) keep their vanilla look.
    /// </summary>
    public static class BuyUILayout
    {
        public const int VanillaColumns = 3;
        public const int VanillaRows = 3;

        private static bool _captured;
        private static bool _logged;
        private static Vector2 _bgSize;
        private static Vector3 _bgScale;
        private static Vector3 _bgPos;
        private static Vector3 _colliderSize;
        private static Vector3 _titlePos;
        private static Vector3 _vanillaRootPos;

        /// <summary>Called right after the original ShowContainerUI so the vanilla root position is known.</summary>
        public static void OnShown(BuyUI ui)
        {
            if (ui == null || ui.root == null) return;
            _vanillaRootPos = ui.root.transform.localPosition;
            Apply(ui);
        }

        public static void Apply(BuyUI ui)
        {
            if (ui == null) return;
            var inv = ui.buyInventory;
            var root = ui.root;
            if (inv == null || root == null || !root.activeInHierarchy) return;

            int cols = VanillaColumns;
            int rows = VanillaRows;
            var player = Manager.main != null ? Manager.main.player : null;
            var handler = player != null ? player.activeBuyInventoryHandler : null;
            if (handler != null && handler.columns > 0 && handler.size > 0)
            {
                cols = Mathf.Clamp(handler.columns, 1, inv.MAX_COLUMNS);
                rows = Mathf.Clamp(Mathf.CeilToInt((float)handler.size / handler.columns), 1, inv.MAX_ROWS);
            }
            float dx = Mathf.Max(0, cols - VanillaColumns) * inv.spread;
            float dy = Mathf.Max(0, rows - VanillaRows) * inv.spread;

            var bg = ui.background;
            BoxCollider collider = bg != null ? bg.GetComponent<BoxCollider>() : null;
            if (!_captured)
            {
                _captured = true;
                if (bg != null)
                {
                    _bgSize = bg.drawMode != SpriteDrawMode.Simple || bg.sprite == null ? bg.size : (Vector2)bg.sprite.bounds.size;
                    _bgScale = bg.transform.localScale;
                    _bgPos = bg.transform.localPosition;
                }
                if (collider != null) _colliderSize = collider.size;
                if (ui.title != null) _titlePos = ui.title.transform.localPosition;
            }

            string bgMode = "none";
            if (bg != null)
            {
                if (bg.drawMode == SpriteDrawMode.Simple && (dx > 0f || dy > 0f))
                {
                    // Simple sprites ignore `size`. Scaling the transform is only safe when the background has
                    // its own transform (ItemSlotsUIContainer.LateUpdate re-sets the InventoryUI's scale every
                    // frame and BuyUI.LateUpdate the root's); otherwise switch it to Sliced, which stretches.
                    if (bg.transform != inv.transform && bg.transform != root.transform)
                    {
                        bg.transform.localScale = new Vector3(
                            _bgSize.x > 0f ? _bgScale.x * (_bgSize.x + dx) / _bgSize.x : _bgScale.x,
                            _bgSize.y > 0f ? _bgScale.y * (_bgSize.y + dy) / _bgSize.y : _bgScale.y,
                            _bgScale.z);
                        bgMode = "simple/scaled";
                    }
                    else
                    {
                        bg.drawMode = SpriteDrawMode.Sliced;
                        bg.size = _bgSize + new Vector2(dx, dy);
                        bgMode = "simple->sliced";
                    }
                }
                else if (bg.drawMode != SpriteDrawMode.Simple)
                {
                    bg.size = _bgSize + new Vector2(dx, dy);
                    bgMode = bg.drawMode.ToString().ToLowerInvariant();
                }
                bg.transform.localPosition = _bgPos;
                if (collider != null) collider.size = new Vector3(_colliderSize.x + dx, _colliderSize.y + dy, _colliderSize.z);
            }

            // Slots are centred on the container origin, so half of the extra width would poke out on the
            // right (into the sell window) and half of the extra height out of the top: move the window.
            root.transform.localPosition = _vanillaRootPos + new Vector3(-dx / 2f, -dy / 2f, 0f);

            // The title sits above the background; if it is inside root it moved with it, so lift it back up
            // by the amount the background grew upwards (it stays centred over the wider window).
            if (ui.title != null && dy > 0f && ui.title.transform.IsChildOf(root.transform))
            {
                ui.title.transform.localPosition = _titlePos + new Vector3(0f, dy / 2f, 0f);
            }

            if (!_logged && handler != null)
            {
                _logged = true;
                var sell = Manager.ui != null ? Manager.ui.sellUI : null;
                string sellInfo = sell != null && sell.root != null
                    ? $"sell root {sell.root.transform.localPosition} showing={sell.isShowing}"
                    : "sell UI n/a";
                string bgInfo = bg != null
                    ? $"bg '{bg.sprite?.name}' mode {bg.drawMode}->{bgMode} size {_bgSize}->{bg.size} scale {bg.transform.localScale} pos {bg.transform.localPosition} parent '{bg.transform.parent?.name}' isInvBackgroundSR={inv.backgroundSR == bg}"
                    : "bg null";
                Debug.Log($"[{PotionSellerMod.Name}] buy window: grid {cols}x{rows} (max {inv.MAX_COLUMNS}x{inv.MAX_ROWS}, spread {inv.spread}), dx {dx} dy {dy}; {bgInfo}; root {_vanillaRootPos}->{root.transform.localPosition}; inv pos {inv.transform.localPosition} extendDown={inv.extendSlotsDownwards} keepBg={inv.keepBackgroundPositionAndSizeTheSame}; {sellInfo}");
            }
        }
    }

    [HarmonyPatch(typeof(BuyUI), "ShowContainerUI")]
    public static class BuyUIShowPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuyUI __instance)
        {
            BuyUILayout.OnShown(__instance);
        }
    }

    /// <summary>Re-applies the layout every frame the window is open so nothing vanilla does can undo it.</summary>
    [HarmonyPatch(typeof(BuyUI), "LateUpdate")]
    public static class BuyUILateUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuyUI __instance)
        {
            BuyUILayout.Apply(__instance);
        }
    }
}
