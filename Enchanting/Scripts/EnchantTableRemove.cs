using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace Enchanting
{
    /// <summary>
    /// Extra Enchanting Table buttons, runtime copies of the forge's own button laid out in a row with it
    /// (2026-11-13): Enchant | Reroll | Remove. Shown only while the open building is the table.
    /// - Reroll (gold, arrow turned sideways) sends "/enchant tablereroll x z": the server takes the top enchant off
    ///   and rolls a new one for one roll's cost.
    /// - Remove (red, arrow flipped down) sends "/enchant tableclear x z": the server removes the item's top enchant
    ///   (newest; first in the tooltip; free, no refund).
    /// The copies keep the vanilla UpgradeForgeButton component, so the button patches in EnchantTable.cs tell them
    /// apart with <see cref="IsRemove"/> / <see cref="IsReroll"/>.
    /// </summary>
    public static class EnchantRemoveButton
    {
        public const string TitleTerm = "Enchanting/RemoveButton";
        public const string RerollTerm = "Enchanting/RerollButton";
        internal static UpgradeForgeButton Button;
        internal static UpgradeForgeButton Reroll;
        private static UpgradeForgeButton _original;
        private static Vector3 _originalPos;
        private static Vector3 _originalScale;
        private static bool _loggedLayout;

        /// <summary>Centre and height of the button row in the buttons' parent space (the result text goes below).</summary>
        internal static Vector3 ButtonCentre;
        internal static float ButtonHeight = 1f;
        internal static float RowHalfWidth = 3f;

        private static readonly Color RemoveTint = new Color(1f, 0.42f, 0.36f, 1f);
        private static readonly Color RerollTint = new Color(1f, 0.85f, 0.35f, 1f);

        public static bool IsRemove(UpgradeForgeButton b) => b != null && b == Button;
        public static bool IsReroll(UpgradeForgeButton b) => b != null && b == Reroll;

        internal static void OnShow(UpgradeForgeUI ui)
        {
            var source = ui.button;
            if (source == null) return;
            bool table = EnchantTableUI.IsTableOpen(out _);
            if (Button == null || Reroll == null || _original != source)
            {
                if (!table) return; // nothing to undo yet
                _original = source;
                _originalPos = source.transform.localPosition;
                _originalScale = source.transform.localScale;
                Button = CreateCopy(source, "EnchantingRemoveButton", OnClick, RemoveTint, flipY: true, turn: false);
                Reroll = CreateCopy(source, "EnchantingRerollButton", OnRerollClick, RerollTint, flipY: false, turn: true);
            }

            if (table)
            {
                Layout(ui, source);
                Button.gameObject.SetActive(true);
                Reroll.gameObject.SetActive(true);
                EnchantTableResult.Reset(source.transform.parent);
            }
            else
            {
                source.transform.localPosition = _originalPos;
                source.transform.localScale = _originalScale;
                Button.gameObject.SetActive(false);
                Reroll.gameObject.SetActive(false);
                EnchantTableResult.Hide();
            }
        }

        // Instantiate under an inactive holder so the copy's Awake (which caches the sprite colours that LateUpdate
        // re-applies every frame) runs only after the icon has been restyled.
        private static UpgradeForgeButton CreateCopy(UpgradeForgeButton source, string name, UnityAction click, Color tint, bool flipY, bool turn)
        {
            var parent = source.transform.parent;
            var holder = new GameObject("EnchantingButtonHolder");
            holder.SetActive(false);
            holder.transform.SetParent(parent, false);
            var copy = Object.Instantiate(source.gameObject, holder.transform);
            copy.name = name;
            copy.transform.localPosition = source.transform.localPosition;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.localScale;
            var button = copy.GetComponent<UpgradeForgeButton>();
            button.onLeftClick = new UnityEvent();
            button.onLeftClick.AddListener(click);
            button.onRightClick = new UnityEvent();
            RestyleIcon(button, tint, flipY, turn);
            copy.transform.SetParent(parent, false);
            Object.Destroy(holder);
            return button;
        }

        // Button children (from Player.log): background / backgroundPressed (frame), selectedMarker (hover hand),
        // icon / iconDisabled (the up arrow, upgrade_forge_ui_5). Restyle only the icon renderers (flip or turn the
        // arrow, tint); the frame and the hover hand are left alone.
        private static void RestyleIcon(ButtonUIElement b, Color tint, bool flipY, bool turn)
        {
            var log = new System.Text.StringBuilder();
            foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null) continue;
                string n = sr.gameObject.name.ToLowerInvariant();
                bool icon = n.StartsWith("icon") || sr.sprite.name.ToLowerInvariant().Contains("arrow");
                if (icon)
                {
                    sr.flipY = flipY;
                    if (turn) sr.transform.localRotation = sr.transform.localRotation * Quaternion.Euler(0f, 0f, -90f);
                    sr.color = sr.color * tint;
                }
                log.Append($"{sr.gameObject.name}:{sr.sprite.name}{(icon ? "*" : "")} ");
            }
            Debug.Log($"[{EnchantingMod.Name}] {b.gameObject.name} icon restyled (*): {log}");
        }

        // Enchant | Reroll | Remove, centred on the vanilla position, a fifth of a button apart. The width comes from
        // the button's BoxCollider (sized to the frame), falling back to the frame sprite. If the row would stick out
        // of the panel backdrop, all three shrink to fit.
        private static void Layout(UpgradeForgeUI ui, UpgradeForgeButton source)
        {
            float w = Width(source);
            float gap = Mathf.Max(0.25f, w * 0.2f);
            float scale = 1f;
            float avail = AvailableHalfWidth(ui, source);
            if (avail > 0f && avail < w * 1.5f + gap)
                scale = Mathf.Clamp((avail - gap) / (w * 1.5f), 0.5f, 1f);
            float step = w * scale + gap;
            source.transform.localScale = _originalScale * scale;
            Reroll.transform.localScale = _originalScale * scale;
            Button.transform.localScale = _originalScale * scale;
            source.transform.localPosition = _originalPos - new Vector3(step, 0f, 0f);
            Reroll.transform.localPosition = _originalPos;
            Button.transform.localPosition = _originalPos + new Vector3(step, 0f, 0f);
            ButtonCentre = _originalPos;
            ButtonHeight = Height(source) * scale;
            RowHalfWidth = avail > 0f ? avail : step * 1.5f;
            if (!_loggedLayout)
            {
                _loggedLayout = true;
                Debug.Log($"[{EnchantingMod.Name}] table buttons layout: width {w:0.###}, gap {gap:0.###}, " +
                          $"panel half-width {avail:0.###}, scale {scale:0.##}, step {step:0.###}");
            }
        }

        /// <summary>Button width in its parent's local units, at the vanilla scale.</summary>
        private static float Width(ButtonUIElement b)
        {
            float sx = Mathf.Abs(_originalScale.x);
            var col = b.GetComponent<BoxCollider>();
            if (col != null && col.size.x > 0.01f) return col.size.x * sx;
            float best = 0f;
            foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null) continue;
                float x = sr.drawMode == SpriteDrawMode.Simple ? sr.sprite.bounds.size.x : sr.size.x;
                best = Mathf.Max(best, x * Mathf.Abs(sr.transform.localScale.x));
            }
            return best > 0.01f ? best * sx : 2f;
        }

        private static float Height(ButtonUIElement b)
        {
            var col = b.GetComponent<BoxCollider>();
            return col != null && col.size.y > 0.01f ? col.size.y * Mathf.Abs(_originalScale.y) : 1.5f;
        }

        // Half the width of the forge panel's backdrop around the vanilla button position, minus a small margin,
        // in the button parent's local units. The backdrop is the widest visible renderer of the UI that is not
        // part of the buttons or our text; implausible results (nothing found, or a full-screen overlay) return -1.
        private static float AvailableHalfWidth(UpgradeForgeUI ui, UpgradeForgeButton source)
        {
            var parent = source.transform.parent;
            if (parent == null) return -1f;
            float bestW = 0f, minX = 0f, maxX = 0f;
            foreach (var sr in ui.GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (sr == null || sr.sprite == null || !sr.enabled) continue;
                if (sr.transform.IsChildOf(source.transform)
                    || (Button != null && sr.transform.IsChildOf(Button.transform))
                    || (Reroll != null && sr.transform.IsChildOf(Reroll.transform))
                    || EnchantTableResult.Owns(sr.transform)) continue;
                var bounds = sr.bounds;
                float a = parent.InverseTransformPoint(bounds.min).x, c = parent.InverseTransformPoint(bounds.max).x;
                float lo = Mathf.Min(a, c), hi = Mathf.Max(a, c);
                if (hi - lo > bestW) { bestW = hi - lo; minX = lo; maxX = hi; }
            }
            if (bestW <= 0f || bestW > 40f) return -1f;
            float half = Mathf.Min(_originalPos.x - minX, maxX - _originalPos.x) - 0.25f;
            return half > 0f ? half : -1f;
        }

        private static void OnClick()
        {
            if (Button == null || !Button.canBeClicked) return;
            if (EnchantTableUI.IsTableOpen(out var building)) EnchantTableUI.SendCommand(building, "tableclear");
        }

        private static void OnRerollClick()
        {
            if (Reroll == null || !Reroll.canBeClicked) return;
            if (EnchantTableUI.IsTableOpen(out var building)) EnchantTableUI.SendCommand(building, "tablereroll");
        }

        /// <summary>Clickable when the item in the slot has at least one enchant.</summary>
        internal static bool CanClick() => EnchantStorage.HasAnyClient(EnchantTableItemPatch.Item);

        /// <summary>
        /// Enchants left after a reroll takes the top one (plus any inactive extras) off, which prices the roll;
        /// -1 when the item has nothing to reroll. Same rule as EnchantTableServer.Reroll.
        /// </summary>
        internal static int CountAfterRerollRemoval(ContainedObjectsBuffer item)
        {
            int n = EnchantStorage.CountClient(item);
            if (n == 0) return -1;
            int max = EnchantRoll.MaxFor(Enchants.SlotOf(item.objectID));
            do n--; while (n >= max);
            return n;
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeUI), nameof(UpgradeForgeUI.ShowContainerUI))]
    public static class EnchantRemoveButtonShowPatch
    {
        public static void Postfix(UpgradeForgeUI __instance)
        {
            try
            {
                EnchantRemoveButton.OnShow(__instance);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{EnchantingMod.Name}] table buttons: {e.Message}");
            }
        }
    }
}
