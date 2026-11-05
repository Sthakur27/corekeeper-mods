using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace Enchanting
{
    /// <summary>
    /// "Remove top enchantment" button for the Enchanting Table: a runtime copy of the forge's own button, placed next
    /// to it (with a gap) with its arrow flipped to point down and tinted red, shown only while the open building is the table. Clicking it
    /// sends "/enchant tableclear x z"; the server removes the item's top enchant (first in the tooltip; free, no
    /// refund). The copy keeps the vanilla UpgradeForgeButton component, so the button patches in EnchantTable.cs
    /// tell the two apart with <see cref="IsRemove"/>.
    /// </summary>
    public static class EnchantRemoveButton
    {
        public const string TitleTerm = "Enchanting/RemoveButton";
        internal static UpgradeForgeButton Button;
        private static UpgradeForgeButton _original;
        private static Vector3 _originalPos;
        private static Vector3 _originalScale;
        private static bool _loggedLayout;

        // Red tint so the copy reads as "take away" next to the vanilla button.
        private static readonly Color RemoveTint = new Color(1f, 0.42f, 0.36f, 1f);

        public static bool IsRemove(UpgradeForgeButton b) => b != null && b == Button;

        internal static void OnShow(UpgradeForgeUI ui)
        {
            var source = ui.button;
            if (source == null) return;
            bool table = EnchantTableUI.IsTableOpen(out _);
            if (Button == null || _original != source)
            {
                if (!table) return; // nothing to undo yet
                _original = source;
                _originalPos = source.transform.localPosition;
                _originalScale = source.transform.localScale;
                Button = CreateCopy(source);
            }

            if (table)
            {
                Layout(ui, source);
                Button.gameObject.SetActive(true);
            }
            else
            {
                source.transform.localPosition = _originalPos;
                source.transform.localScale = _originalScale;
                Button.gameObject.SetActive(false);
            }
        }

        // Instantiate under an inactive holder so the copy's Awake (which caches the sprite colours that LateUpdate
        // re-applies every frame) runs only after the icon has been flipped and tinted.
        private static UpgradeForgeButton CreateCopy(UpgradeForgeButton source)
        {
            var parent = source.transform.parent;
            var holder = new GameObject("EnchantingRemoveButtonHolder");
            holder.SetActive(false);
            holder.transform.SetParent(parent, false);
            var copy = Object.Instantiate(source.gameObject, holder.transform);
            copy.name = "EnchantingRemoveButton";
            copy.transform.localPosition = source.transform.localPosition;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.localScale;
            var button = copy.GetComponent<UpgradeForgeButton>();
            button.onLeftClick = new UnityEvent();
            button.onLeftClick.AddListener(OnClick);
            button.onRightClick = new UnityEvent();
            RestyleIcon(button);
            copy.transform.SetParent(parent, false);
            Object.Destroy(holder);
            return button;
        }

        // Button children (from Player.log): background / backgroundPressed (frame), selectedMarker (hover hand),
        // icon / iconDisabled (the up arrow, upgrade_forge_ui_5). Flip only the icon renderers so the arrow points
        // down, and tint them red. The frame and the hover hand are left alone.
        private static void RestyleIcon(ButtonUIElement b)
        {
            var log = new System.Text.StringBuilder();
            foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null) continue;
                string n = sr.gameObject.name.ToLowerInvariant();
                bool icon = n.StartsWith("icon") || sr.sprite.name.ToLowerInvariant().Contains("arrow");
                if (icon)
                {
                    sr.flipY = true;
                    sr.color = sr.color * RemoveTint;
                }
                log.Append($"{sr.gameObject.name}:{sr.sprite.name}{(icon ? "*" : "")} ");
            }
            Debug.Log($"[{EnchantingMod.Name}] remove button icon flipped down + tinted red (*): {log}");
        }

        // Place the two buttons side by side, centred on the vanilla position, with a gap of a fifth of a button.
        // The width comes from the button's BoxCollider (sized to the frame), falling back to the frame sprite.
        // If the pair would stick out of the panel backdrop, both buttons shrink to fit.
        private static void Layout(UpgradeForgeUI ui, UpgradeForgeButton source)
        {
            float w = Width(source);
            float gap = Mathf.Max(0.25f, w * 0.2f);
            float scale = 1f;
            float avail = AvailableHalfWidth(ui, source);
            if (avail > 0f && avail < w + gap * 0.5f)
                scale = Mathf.Clamp((avail - gap * 0.5f) / w, 0.6f, 1f);
            float offset = w * scale * 0.5f + gap * 0.5f;
            source.transform.localScale = _originalScale * scale;
            Button.transform.localScale = _originalScale * scale;
            source.transform.localPosition = _originalPos - new Vector3(offset, 0f, 0f);
            Button.transform.localPosition = _originalPos + new Vector3(offset, 0f, 0f);
            if (!_loggedLayout)
            {
                _loggedLayout = true;
                Debug.Log($"[{EnchantingMod.Name}] remove button layout: width {w:0.###}, gap {gap:0.###}, " +
                          $"panel half-width {avail:0.###}, scale {scale:0.##}, offset +-{offset:0.###}");
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

        // Half the width of the forge panel's backdrop around the vanilla button position, minus a small margin,
        // in the button parent's local units. The backdrop is the widest visible renderer of the UI that is not
        // part of either button; implausible results (nothing found, or a full-screen overlay) return -1.
        private static float AvailableHalfWidth(UpgradeForgeUI ui, UpgradeForgeButton source)
        {
            var parent = source.transform.parent;
            if (parent == null) return -1f;
            float bestW = 0f, minX = 0f, maxX = 0f;
            foreach (var sr in ui.GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (sr == null || sr.sprite == null || !sr.enabled) continue;
                if (sr.transform.IsChildOf(source.transform) || (Button != null && sr.transform.IsChildOf(Button.transform))) continue;
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

        /// <summary>Clickable when the item in the slot has at least one enchant.</summary>
        internal static bool CanClick() => EnchantStorage.HasAnyClient(EnchantTableItemPatch.Item);
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
                Debug.LogWarning($"[{EnchantingMod.Name}] remove button: {e.Message}");
            }
        }
    }
}
