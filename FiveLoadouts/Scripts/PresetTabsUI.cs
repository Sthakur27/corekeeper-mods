using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FiveLoadouts
{
    /// <summary>
    /// Adds preset tabs 4 and 5 to the character window by cloning the vanilla third tab.
    ///
    /// CharacterWindowUI drives everything from its presetTabs list (SetActivePreset and
    /// UpdatePresetTabs loop over it), so appending two CharacterWindowTab clones is enough for
    /// highlighting and clicks. The cloned tab's inspector-wired onClick would call
    /// SetActivePreset(2); those persistent calls are switched off and a runtime listener with
    /// the right index is added. The icon is replaced by a generated pixel digit.
    /// </summary>
    public static class PresetTabsUI
    {
        private static bool _loggedError;
        private static readonly Dictionary<int, Sprite> _digitCache = new Dictionary<int, Sprite>();

        public static void EnsureExtended()
        {
            try
            {
                if (Manager.main == null) return;
                var ui = Manager.ui;
                if (ui == null) return;
                var window = ui.characterWindow;
                if (window == null || window.presetTabs == null) return;
                var tabs = window.presetTabs;
                if (tabs.Count >= PresetLayout.PresetCount || tabs.Count < PresetLayout.VanillaPresetCount) return;

                var t1 = tabs[1];
                var t2 = tabs[2];
                if (t1 == null || t2 == null) return;

                Vector3 step = t2.transform.localPosition - t1.transform.localPosition;
                var prev = t2;
                while (tabs.Count < PresetLayout.PresetCount)
                {
                    int index = tabs.Count;
                    var go = Object.Instantiate(t2.gameObject, t2.transform.parent);
                    go.name = $"{t2.gameObject.name}_FiveLoadouts{index + 1}";
                    go.transform.localPosition = t2.transform.localPosition + step * (index - 2);
                    go.transform.localRotation = t2.transform.localRotation;
                    go.transform.localScale = t2.transform.localScale;

                    var tab = go.GetComponent<CharacterWindowTab>();
                    if (tab == null)
                    {
                        Object.Destroy(go);
                        Debug.LogError($"[{FiveLoadoutsMod.Name}] Cloned preset tab has no CharacterWindowTab; UI not extended.");
                        return;
                    }

                    WireClick(tab, window, index);
                    ReplaceIcon(tab, index + 1);
                    RenameHover(tab, index + 1);
                    WireNavigation(prev, tab);

                    tab.SetActive(false);
                    tabs.Add(tab);
                    prev = tab;
                }
                Debug.Log($"[{FiveLoadoutsMod.Name}] Character window extended to {tabs.Count} preset tabs.");
            }
            catch (System.Exception ex)
            {
                if (_loggedError) return;
                _loggedError = true;
                Debug.LogError($"[{FiveLoadoutsMod.Name}] EnsureExtended failed: {ex}");
            }
        }

        private static void WireClick(CharacterWindowTab tab, CharacterWindowUI window, int index)
        {
            if (tab.onClick == null) tab.onClick = new UnityEvent();
            int persistent = tab.onClick.GetPersistentEventCount();
            for (int i = 0; i < persistent; i++)
                tab.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            tab.onClick.RemoveAllListeners();
            tab.onClick.AddListener(() => window.SetActivePreset(index));
        }

        private static void WireNavigation(CharacterWindowTab left, CharacterWindowTab right)
        {
            // Controller/keyboard navigation between tabs. Lists cloned by Instantiate still
            // reference the ORIGINAL neighbours, so give the clone its own lists.
            right.leftUIElements = new List<UIelement> { left };
            right.rightUIElements = new List<UIelement>();
            if (left.rightUIElements == null) left.rightUIElements = new List<UIelement>();
            left.rightUIElements.Clear();
            left.rightUIElements.Add(right);
        }

        private static void RenameHover(CharacterWindowTab tab, int number)
        {
            try
            {
                string term = tab.hoverTitle.mTerm;
                if (string.IsNullOrEmpty(term)) return;
                // Vanilla terms end in the preset number; a missing term just shows the key.
                if (char.IsDigit(term[term.Length - 1]))
                    tab.hoverTitle.mTerm = term.Substring(0, term.Length - 1) + number;
            }
            catch (System.Exception)
            {
                // Cosmetic only.
            }
        }

        private static void ReplaceIcon(CharacterWindowTab tab, int number)
        {
            if (tab.icon == null) return;
            var sprite = DigitSprite(number, tab.icon.sprite);
            if (sprite != null) tab.icon.sprite = sprite;
        }

        // 3x5 pixel glyphs for the digits we need.
        private static readonly string[] Glyph4 = { "#.#", "#.#", "###", "..#", "..#" };
        private static readonly string[] Glyph5 = { "###", "#..", "###", "..#", "###" };

        private static Sprite DigitSprite(int number, Sprite template)
        {
            if (_digitCache.TryGetValue(number, out var cached) && cached != null) return cached;
            string[] glyph = number == 4 ? Glyph4 : number == 5 ? Glyph5 : null;
            if (glyph == null) return null;

            int w = 8, h = 8;
            float ppu = 16f;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (template != null)
            {
                w = Mathf.Max(5, Mathf.RoundToInt(template.rect.width));
                h = Mathf.Max(7, Mathf.RoundToInt(template.rect.height));
                ppu = template.pixelsPerUnit;
                pivot = new Vector2(template.pivot.x / template.rect.width, template.pivot.y / template.rect.height);
            }

            int scale = Mathf.Max(1, Mathf.Min((w - 2) / 3, (h - 2) / 5));
            int gw = 3 * scale, gh = 5 * scale;
            int ox = (w - gw) / 2, oy = (h - gh) / 2;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color32(0, 0, 0, 0);
            var white = new Color32(255, 255, 255, 255);
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            for (int gy = 0; gy < 5; gy++)
            {
                string row = glyph[gy];
                for (int gx = 0; gx < 3; gx++)
                {
                    if (row[gx] != '#') continue;
                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int px = ox + gx * scale + sx;
                        int py = oy + (4 - gy) * scale + sy; // texture rows go bottom-up
                        pixels[py * w + px] = white;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = $"FiveLoadouts_Digit{number}";
            _digitCache[number] = sprite;
            return sprite;
        }
    }
}
