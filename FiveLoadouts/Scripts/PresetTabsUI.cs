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
    /// SetActivePreset(2); that event is replaced by a runtime listener with the right index.
    ///
    /// Numerals: vanilla tabs show Roman numerals I, II, III as pixel sprites with the preset
    /// colour baked into the pixels (CharacterWindowTab.SetActive only multiplies icon.color by
    /// white / inactiveColor). The clones get hand-drawn "IV" and "V" pixel grids in the same
    /// style (built into point-filtered textures at the vanilla pixels-per-unit and pivot).
    ///
    /// Placement: computed purely from the three vanilla tabs. With y1/y3 the local y of tabs
    /// 1 and 3 and spacing = (y1 - y3) / 2, the five tabs are spread evenly from y1 down to
    /// y3 - spacing (one vanilla step below tab 3, which is still inside the window frame),
    /// rounded down to whole art pixels. Vanilla tabs 1-3 are moved too (the game never
    /// repositions them). Tabs keep their native scale and overlap slightly (the active tab
    /// draws on top: SetActive gives it a higher sorting order). The layout is re-applied
    /// whenever a tab's y or scale drifts from the target (e.g. if the UI were reset on open).
    /// </summary>
    public static class PresetTabsUI
    {
        private static bool _loggedError;
        private static readonly Dictionary<int, Sprite> _numeralCache = new Dictionary<int, Sprite>();

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
                if (tabs.Count < PresetLayout.VanillaPresetCount) return;
                if (tabs.Count >= PresetLayout.PresetCount)
                {
                    ReapplyLayout(window, tabs);
                    return;
                }

                var t1 = tabs[0];
                var t2 = tabs[1];
                var t3 = tabs[2];
                if (t1 == null || t2 == null || t3 == null) return;

                // Measure the vanilla column BEFORE anything is moved or cloned.
                ComputeLayout(window, t1, t3);

                Vector3 step = t3.transform.localPosition - t2.transform.localPosition;
                var prev = t3;
                while (tabs.Count < PresetLayout.PresetCount)
                {
                    int index = tabs.Count;
                    var go = Object.Instantiate(t3.gameObject, t3.transform.parent);
                    go.name = $"{t3.gameObject.name}_FiveLoadouts{index + 1}";
                    go.transform.localPosition = t3.transform.localPosition + step * (index - 2);
                    go.transform.localRotation = t3.transform.localRotation;
                    go.transform.localScale = t3.transform.localScale;

                    var tab = go.GetComponent<CharacterWindowTab>();
                    if (tab == null)
                    {
                        Object.Destroy(go);
                        Debug.LogError($"[{FiveLoadoutsMod.Name}] Cloned preset tab has no CharacterWindowTab; UI not extended.");
                        return;
                    }

                    WireClick(tab, window, index);
                    ReplaceIcon(tab, t3, index + 1);
                    RenameHover(tab, index + 1);
                    WireNavigation(prev, tab);
                    MatchSorting(tab, t3);

                    tab.SetActive(false);
                    tabs.Add(tab);
                    prev = tab;
                }

                ApplyLayout(tabs, force: true);
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
            // Replace the whole event: the clone's inspector-wired listener would select preset 3,
            // and the persistent-listener API is rejected by the mod loader's security check.
            tab.onClick = new UnityEvent();
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

        private static void MatchSorting(CharacterWindowTab clone, CharacterWindowTab original)
        {
            // Instantiate copies sorting layer/order already; re-assert it so the clone draws in
            // the same layer as the vanilla tab (SetActive only touches background.sortingOrder).
            if (clone.background != null && original.background != null)
            {
                clone.background.sortingLayerID = original.background.sortingLayerID;
                clone.background.sortingOrder = original.background.sortingOrder;
            }
            if (clone.icon != null && original.icon != null)
            {
                clone.icon.sortingLayerID = original.icon.sortingLayerID;
                clone.icon.sortingOrder = original.icon.sortingOrder;
            }
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

        private static void ReplaceIcon(CharacterWindowTab tab, CharacterWindowTab original, int number)
        {
            if (tab.icon == null) return;
            var sprite = NumeralSprite(number, original.icon != null ? original.icon.sprite : null);
            if (sprite != null) tab.icon.sprite = sprite;
            if (original.icon != null)
            {
                tab.icon.transform.localPosition = original.icon.transform.localPosition;
                tab.icon.transform.localScale = original.icon.transform.localScale;
            }
        }

        // ------------------------------------------------------------------ column placement

        private static CharacterWindowUI _layoutWindow;   // window the targets were computed for
        private static float[] _targetY;                  // local y per tab index
        private static Vector3 _targetScale;              // local scale for every tab
        private static bool _layoutValid;

        /// <summary>
        /// Derives the five-tab column from the untouched vanilla tabs 1 and 3:
        /// top = y1, bottom = y3 - spacing, newSpacing = (top - bottom) / 4.
        /// </summary>
        private static void ComputeLayout(CharacterWindowUI window, CharacterWindowTab t1, CharacterWindowTab t3)
        {
            _layoutWindow = window;
            _layoutValid = false;

            int n = PresetLayout.PresetCount;
            float y1 = t1.transform.localPosition.y;
            float y3 = t3.transform.localPosition.y;
            float spacing = (y1 - y3) / (PresetLayout.VanillaPresetCount - 1);
            if (spacing <= 0.0001f)
            {
                Debug.Log($"[{FiveLoadoutsMod.Name}] Preset tabs are not a downward column (y1={y1:F3} y3={y3:F3}); positions left as cloned.");
                return;
            }

            float top = y1;
            float bottom = y3 - spacing;
            float newSpacing = (top - bottom) / (n - 1);

            // Snap the step down to whole art pixels so every tab lands on the pixel grid.
            // Tabs keep their native scale and overlap a little instead of being shrunk: a
            // fractional scale resamples the pixel art and makes all five tabs look blurry.
            float pixel = ArtPixel(t1);
            if (pixel > 0.0001f) newSpacing = Mathf.Max(pixel, Mathf.Floor(newSpacing / pixel + 0.001f) * pixel);
            float tabHeight = TabHeight(t1);

            _targetY = new float[n];
            for (int i = 0; i < n; i++) _targetY[i] = top - i * newSpacing;
            _targetScale = t1.transform.localScale;
            _layoutValid = true;

            Debug.Log($"[{FiveLoadoutsMod.Name}] Tab column: y1={y1:F3} y3={y3:F3} spacing={spacing:F3} -> top={top:F3} newSpacing={newSpacing:F3} tabHeight={tabHeight:F3} pixel={pixel:F4}");
        }

        /// <summary>Height of the tab background in the tab's parent space (local units of the column).</summary>
        private static float TabHeight(CharacterWindowTab tab)
        {
            var sr = tab.background;
            if (sr == null || sr.sprite == null) return 0f;
            float size = sr.drawMode == SpriteDrawMode.Simple ? sr.sprite.bounds.size.y : sr.size.y;
            return Mathf.Abs(size * ScaleToParent(tab));
        }

        /// <summary>One pixel of the tab background art in the tab's parent space.</summary>
        private static float ArtPixel(CharacterWindowTab tab)
        {
            var sr = tab.background;
            if (sr == null || sr.sprite == null || sr.sprite.pixelsPerUnit <= 0f) return 0f;
            return Mathf.Abs(ScaleToParent(tab) / sr.sprite.pixelsPerUnit);
        }

        /// <summary>
        /// Scale of the background renderer relative to the tab's parent = product of local
        /// scales from the renderer up to (and including) the tab itself.
        /// </summary>
        private static float ScaleToParent(CharacterWindowTab tab)
        {
            float s = 1f;
            Transform t = tab.background.transform;
            Transform stop = tab.transform.parent;
            while (t != null && t != stop)
            {
                s *= t.localScale.y;
                t = t.parent;
            }
            return s;
        }

        /// <summary>Writes the target y and scale to every tab; x and z are left alone.</summary>
        private static void ApplyLayout(List<CharacterWindowTab> tabs, bool force)
        {
            if (!_layoutValid || _targetY == null) return;
            int n = Mathf.Min(tabs.Count, _targetY.Length);
            for (int i = 0; i < n; i++)
            {
                var tab = tabs[i];
                if (tab == null) continue;
                var tr = tab.transform;
                Vector3 p = tr.localPosition;
                if (force || Mathf.Abs(p.y - _targetY[i]) > 0.0005f)
                    tr.localPosition = new Vector3(p.x, _targetY[i], p.z);
                if (force || (tr.localScale - _targetScale).sqrMagnitude > 1e-8f)
                    tr.localScale = _targetScale;
            }
        }

        /// <summary>Every frame once five tabs exist: put them back if anything moved them.</summary>
        private static void ReapplyLayout(CharacterWindowUI window, List<CharacterWindowTab> tabs)
        {
            if (!_layoutValid || _layoutWindow != window) return;
            ApplyLayout(tabs, force: false);
        }

        // ------------------------------------------------------------------ numeral sprites

        // Hand-drawn IV and V in the style of the vanilla gearset_icons I, II, III (10x10 at
        // 16 ppu): a cap row (A), outline (B), top highlight (C), 1px stems sharing outlines
        // (D, lighter E on the lower half) and a base serif, rows listed top to bottom. Both
        // stay inside III's 9px footprint so the tab column reads as one set.
        private static readonly string[] GlyphIV =
        {
            "AAAAAAAAA.",
            "BCCCCCCCB.",
            "BDBDB.BDB.",
            "BDBDB.BDB.",
            "BDBBDBDB..",
            "BEBBEBEB..",
            "BEB.BEB...",
            "BDDDDDDDB.",
            "BBBBBBBBB.",
            "..........",
        };

        private static readonly string[] GlyphV =
        {
            "AAAAAAAAA.",
            "BCCCCCCCB.",
            ".BDB.BDB..",
            ".BDB.BDB..",
            "..BDBDB...",
            "..BEBEB...",
            "...BEB....",
            "..BDDDB...",
            "..BBBBB...",
            "..........",
        };

        // Palettes A..E, shaded like the vanilla green / blue / red numerals.
        private static readonly Color32[] PaletteIV =   // gold
        {
            new Color32(168, 100, 12, 255), new Color32(88, 50, 20, 255), new Color32(255, 234, 140, 255),
            new Color32(232, 168, 36, 255), new Color32(244, 204, 96, 255),
        };

        private static readonly Color32[] PaletteV =    // purple
        {
            new Color32(96, 36, 150, 255), new Color32(50, 28, 84, 255), new Color32(228, 176, 255, 255),
            new Color32(160, 92, 228, 255), new Color32(198, 146, 242, 255),
        };

        private static Sprite NumeralSprite(int number, Sprite template)
        {
            if (_numeralCache.TryGetValue(number, out var cached) && cached != null) return cached;
            if (number != 4 && number != 5) return null;
            var sprite = BuildGlyph(number == 4 ? GlyphIV : GlyphV, number == 4 ? PaletteIV : PaletteV, template);
            sprite.name = $"FiveLoadouts_Numeral{number}";
            _numeralCache[number] = sprite;
            return sprite;
        }

        private static Sprite BuildGlyph(string[] rows, Color32[] palette, Sprite template)
        {
            int h = rows.Length, w = rows[0].Length;
            var pixels = new Color32[w * h];
            for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[r][x];
                int y = h - 1 - r;                        // textures are bottom-up
                pixels[y * w + x] = c == '.' ? new Color32(0, 0, 0, 0) : palette[c - 'A'];
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            // Same pixels-per-unit and pivot as the vanilla numeral, so it sits where III sits.
            float ppu = 16f;
            Vector2 pivotPx = new Vector2(5f, 5f);
            if (template != null)
            {
                ppu = template.pixelsPerUnit;
                pivotPx = template.pivot;   // whole pixels, so the glyph stays on the grid
            }
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(pivotPx.x / w, pivotPx.y / h), ppu, 0, SpriteMeshType.FullRect);
        }
    }
}
