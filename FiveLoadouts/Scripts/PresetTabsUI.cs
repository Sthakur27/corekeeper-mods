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
    /// white / inactiveColor). So the clones get generated "IV" and "V" sprites: the vanilla "I"
    /// sprite is read back at runtime (blit to a RenderTexture, since the atlas is not CPU
    /// readable) to learn canvas size, pixels-per-unit, stroke width, glyph height, bar/serif
    /// shape, letter gap (from "II") and any shadow/outline shade; the same "I" pixels are
    /// reused recoloured, and a V is drawn in the same stroke and height. Falls back to a plain
    /// 1px 5-high style when the sprite cannot be read.
    ///
    /// Placement: computed purely from the three vanilla tabs. With y1/y3 the local y of tabs
    /// 1 and 3 and spacing = (y1 - y3) / 2, the five tabs are spread evenly from y1 down to
    /// y3 - spacing (one vanilla step below tab 3, which is still inside the window frame), so
    /// newSpacing = 3/4 of vanilla. Vanilla tabs 1-3 are moved too (the game never repositions
    /// them). If the tab background is taller than newSpacing, every tab is scaled uniformly by
    /// newSpacing / tabHeight so they never overlap. The layout is re-applied whenever a tab's
    /// y or scale drifts from the target (e.g. if the UI were reset on open).
    /// </summary>
    public static class PresetTabsUI
    {
        private static bool _loggedError;
        private static readonly Dictionary<int, Sprite> _numeralCache = new Dictionary<int, Sprite>();
        private static NumeralStyle _style;

        // Two new preset colours, distinct from vanilla green / blue / red.
        private static readonly Color32 ColourIV = new Color32(250, 200, 70, 255);  // gold
        private static readonly Color32 ColourV = new Color32(190, 120, 255, 255);  // purple

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

                _style = _style ?? NumeralStyle.FromVanilla(t1, t2);

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
            var sprite = NumeralSprite(number);
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

            Vector3 baseScale = t1.transform.localScale;
            float tabHeight = TabHeight(t1);
            float scale = 1f;
            if (tabHeight > newSpacing && tabHeight > 0.0001f) scale = newSpacing / tabHeight;

            _targetY = new float[n];
            for (int i = 0; i < n; i++) _targetY[i] = top - i * newSpacing;
            _targetScale = baseScale * scale;
            _layoutValid = true;

            Debug.Log($"[{FiveLoadoutsMod.Name}] Tab column: y1={y1:F3} y3={y3:F3} spacing={spacing:F3} -> top={top:F3} bottom={bottom:F3} newSpacing={newSpacing:F3} tabHeight={tabHeight:F3} scale={scale:F3}");
        }

        /// <summary>Height of the tab background in the tab's parent space (local units of the column).</summary>
        private static float TabHeight(CharacterWindowTab tab)
        {
            var sr = tab.background;
            if (sr == null || sr.sprite == null) return 0f;
            float size = sr.drawMode == SpriteDrawMode.Simple ? sr.sprite.bounds.size.y : sr.size.y;
            // Scale of the renderer relative to the tab's parent = product of local scales from
            // the renderer up to (and including) the tab itself.
            float s = 1f;
            Transform t = sr.transform;
            Transform stop = tab.transform.parent;
            while (t != null && t != stop)
            {
                s *= t.localScale.y;
                t = t.parent;
            }
            return Mathf.Abs(size * s);
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

        private static Sprite NumeralSprite(int number)
        {
            if (_numeralCache.TryGetValue(number, out var cached) && cached != null) return cached;
            if (number != 4 && number != 5) return null;
            var style = _style ?? NumeralStyle.Fallback(null);
            var sprite = style.Build(number, number == 4 ? ColourIV : ColourV);
            if (sprite != null) _numeralCache[number] = sprite;
            return sprite;
        }

        /// <summary>Everything we learned about the vanilla numeral sprites, plus the glyph builder.</summary>
        private sealed class NumeralStyle
        {
            private const byte Empty = 0, Primary = 1, Secondary = 2;

            public int CanvasW, CanvasH;
            public float Ppu = 16f;
            public Vector2 PivotN = new Vector2(0.5f, 0.5f);
            public int Stroke = 1, Height = 5, Gap = 1, Overhang;
            public int BarTop, BarBottom;         // rows (y up) of the vanilla I's bar
            public byte[,] IShape;                // vanilla I (Primary/Secondary classes), size IW x IH
            public int IW, IH;
            public int ShadowDx, ShadowDy;        // valid when ShadeMode == 1
            public int ShadeMode;                 // 0 none, 1 shadow offset, 2 outline
            public float SecondaryFactor = 0.5f;  // luminance of secondary relative to primary
            public byte SecondaryAlpha = 255;
            public string Source = "fallback";

            public static NumeralStyle FromVanilla(CharacterWindowTab t1, CharacterWindowTab t2)
            {
                Sprite s1 = t1.icon != null ? t1.icon.sprite : null;
                Sprite s2 = t2.icon != null ? t2.icon.sprite : null;
                NumeralStyle style = null;
                try
                {
                    if (s1 != null) style = Analyse(s1, s2);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{FiveLoadoutsMod.Name}] Could not read vanilla numeral sprite: {ex.Message}");
                }
                if (style == null) style = Fallback(s1);
                Debug.Log($"[{FiveLoadoutsMod.Name}] Numeral style ({style.Source}): canvas {style.CanvasW}x{style.CanvasH} ppu {style.Ppu} stroke {style.Stroke} height {style.Height} gap {style.Gap} overhang {style.Overhang} shade {style.ShadeMode}({style.ShadowDx},{style.ShadowDy}) x{style.SecondaryFactor:F2}");
                return style;
            }

            public static NumeralStyle Fallback(Sprite template)
            {
                var st = new NumeralStyle { CanvasW = 8, CanvasH = 8 };
                if (template != null)
                {
                    st.CanvasW = Mathf.Max(5, Mathf.RoundToInt(template.rect.width));
                    st.CanvasH = Mathf.Max(5, Mathf.RoundToInt(template.rect.height));
                    st.Ppu = template.pixelsPerUnit;
                    st.PivotN = new Vector2(template.pivot.x / template.rect.width, template.pivot.y / template.rect.height);
                }
                st.Height = Mathf.Min(5, st.CanvasH);
                st.IBaseY = (st.CanvasH - st.Height) / 2;   // vertically centred on the canvas
                st.BarBottom = 0;                            // bar rows relative to the I shape
                st.BarTop = st.Height - 1;
                st.IW = 1;
                st.IH = st.Height;
                st.IShape = new byte[1, st.Height];
                for (int y = 0; y < st.Height; y++) st.IShape[0, y] = Primary;
                return st;
            }

            private static NumeralStyle Analyse(Sprite s1, Sprite s2)
            {
                var st = new NumeralStyle
                {
                    CanvasW = Mathf.Max(1, Mathf.RoundToInt(s1.rect.width)),
                    CanvasH = Mathf.Max(1, Mathf.RoundToInt(s1.rect.height)),
                    Ppu = s1.pixelsPerUnit,
                    PivotN = new Vector2(s1.pivot.x / s1.rect.width, s1.pivot.y / s1.rect.height),
                    Source = "vanilla"
                };

                byte[,] cls1 = Classify(s1, out Color32 primary, out float factor, out byte secAlpha, out int w1, out int h1);
                if (cls1 == null) return null;
                st.SecondaryFactor = factor;
                st.SecondaryAlpha = secAlpha;

                // Bar of the "I": columns whose primary count equals the tallest column.
                int[] col = new int[w1];
                int maxCount = 0;
                for (int x = 0; x < w1; x++)
                {
                    for (int y = 0; y < h1; y++) if (cls1[x, y] == Primary) col[x]++;
                    if (col[x] > maxCount) maxCount = col[x];
                }
                if (maxCount < 3) return null;
                int barL = -1, barR = -1;
                for (int x = 0; x < w1; x++)
                {
                    if (col[x] == maxCount) { if (barL < 0) barL = x; barR = x; }
                    else if (barL >= 0) break;
                }
                st.Stroke = Mathf.Max(1, barR - barL + 1);
                st.Height = maxCount;
                int minX = w1, maxX = -1, minY = h1, maxY = -1;
                int barTop = -1, barBot = h1;
                for (int x = 0; x < w1; x++)
                for (int y = 0; y < h1; y++)
                {
                    if (cls1[x, y] == Empty) continue;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                    if (cls1[x, y] == Primary && x == barL) { if (y > barTop) barTop = y; if (y < barBot) barBot = y; }
                }
                st.BarTop = barTop;
                st.BarBottom = barBot;
                st.Overhang = Mathf.Max(0, (maxX - minX + 1 - st.Stroke) / 2);

                // Literal copy of the vanilla I (bounding box of all its pixels).
                st.IW = maxX - minX + 1;
                st.IH = maxY - minY + 1;
                st.IShape = new byte[st.IW, st.IH];
                for (int x = 0; x < st.IW; x++)
                for (int y = 0; y < st.IH; y++)
                    st.IShape[x, y] = cls1[minX + x, minY + y];
                st.BarTop -= minY;      // make bar rows relative to the I shape
                st.BarBottom -= minY;
                st.IBaseY = minY;

                // Shade mode: is the secondary class a shadow (offset copy) or an outline?
                st.ShadeMode = DetectShade(cls1, w1, h1, out st.ShadowDx, out st.ShadowDy);

                // Gap between letters from the "II" sprite.
                st.Gap = st.Stroke;
                if (s2 != null)
                {
                    try
                    {
                        byte[,] cls2 = Classify(s2, out _, out _, out _, out int w2, out int h2);
                        if (cls2 != null)
                        {
                            int[] c2 = new int[w2];
                            int m2 = 0;
                            for (int x = 0; x < w2; x++)
                            {
                                for (int y = 0; y < h2; y++) if (cls2[x, y] == Primary) c2[x]++;
                                if (c2[x] > m2) m2 = c2[x];
                            }
                            int g1End = -1, g2Start = -1;
                            bool inGroup = false;
                            int groups = 0;
                            for (int x = 0; x < w2; x++)
                            {
                                bool bar = c2[x] == m2;
                                if (bar && !inGroup) { groups++; if (groups == 2) g2Start = x; }
                                if (!bar && inGroup && groups == 1) g1End = x - 1;
                                inGroup = bar;
                            }
                            if (g1End >= 0 && g2Start > g1End)
                            {
                                int between = g2Start - g1End - 1;
                                st.Gap = Mathf.Max(1, between - 2 * st.Overhang);
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // Keep the default gap.
                    }
                }
                return st;
            }

            public int IBaseY;   // row in the canvas where the vanilla I's lowest pixel sat

            private static int DetectShade(byte[,] cls, int w, int h, out int dx, out int dy)
            {
                dx = 0; dy = 0;
                int secondary = 0;
                for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (cls[x, y] == Secondary) secondary++;
                if (secondary == 0) return 0;

                int[][] offsets = { new[] { 1, -1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, -1 }, new[] { -1, 0 }, new[] { 1, 1 }, new[] { 0, 1 }, new[] { -1, 1 } };
                int best = 0;
                foreach (var o in offsets)
                {
                    int hit = 0;
                    for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                    {
                        if (cls[x, y] != Secondary) continue;
                        int sx = x - o[0], sy = y - o[1];
                        if (sx >= 0 && sy >= 0 && sx < w && sy < h && cls[sx, sy] == Primary) hit++;
                    }
                    if (hit > best) { best = hit; dx = o[0]; dy = o[1]; }
                }
                if (best >= secondary * 0.75f) return 1;

                // Outline: secondary pixels are 8-neighbours of primary ones.
                int near = 0;
                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (cls[x, y] != Secondary) continue;
                    bool adj = false;
                    for (int ox = -1; ox <= 1 && !adj; ox++)
                    for (int oy = -1; oy <= 1 && !adj; oy++)
                    {
                        int sx = x + ox, sy = y + oy;
                        if (sx >= 0 && sy >= 0 && sx < w && sy < h && cls[sx, sy] == Primary) adj = true;
                    }
                    if (adj) near++;
                }
                return near >= secondary * 0.75f ? 2 : 0;
            }

            /// <summary>Reads the sprite's pixels (via RenderTexture if needed) and classifies them.</summary>
            private static byte[,] Classify(Sprite s, out Color32 primary, out float factor, out byte secondaryAlpha, out int w, out int h)
            {
                primary = new Color32(255, 255, 255, 255);
                factor = 0.5f;
                secondaryAlpha = 255;
                w = Mathf.Max(1, Mathf.RoundToInt(s.rect.width));
                h = Mathf.Max(1, Mathf.RoundToInt(s.rect.height));
                if (s.packed && s.packingMode == SpritePackingMode.Tight) return null;

                Rect tr = s.textureRect;
                int tw = Mathf.RoundToInt(tr.width), th = Mathf.RoundToInt(tr.height);
                if (tw <= 0 || th <= 0) return null;
                int offX = Mathf.RoundToInt(s.textureRectOffset.x);
                int offY = Mathf.RoundToInt(s.textureRectOffset.y);

                Color32[] px = ReadPixels(s.texture, Mathf.RoundToInt(tr.x), Mathf.RoundToInt(tr.y), tw, th);
                if (px == null) return null;

                // Most frequent opaque colour is the fill.
                var counts = new Dictionary<int, int>();
                var colours = new Dictionary<int, Color32>();
                int bestKey = 0, bestCount = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i];
                    if (c.a < 32) continue;
                    int key = (c.r >> 3 << 10) | (c.g >> 3 << 5) | (c.b >> 3);
                    counts.TryGetValue(key, out int n);
                    counts[key] = n + 1;
                    if (!colours.ContainsKey(key)) colours[key] = c;
                    if (n + 1 > bestCount) { bestCount = n + 1; bestKey = key; }
                }
                if (bestCount == 0) return null;
                primary = colours[bestKey];
                float lumP = Lum(primary);

                var cls = new byte[w, h];
                float lumSum = 0f;
                int secCount = 0;
                int alphaSum = 0;
                for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    var c = px[y * tw + x];
                    if (c.a < 32) continue;
                    int cx = x + offX, cy = y + offY;
                    if (cx < 0 || cy < 0 || cx >= w || cy >= h) continue;
                    if (ColourClose(c, primary)) cls[cx, cy] = Primary;
                    else
                    {
                        cls[cx, cy] = Secondary;
                        lumSum += Lum(c);
                        alphaSum += c.a;
                        secCount++;
                    }
                }
                if (secCount > 0 && lumP > 0.01f)
                {
                    factor = Mathf.Clamp(lumSum / secCount / lumP, 0.15f, 1.2f);
                    secondaryAlpha = (byte)Mathf.Clamp(alphaSum / secCount, 32, 255);
                }
                return cls;
            }

            private static float Lum(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

            private static bool ColourClose(Color32 a, Color32 b)
            {
                int dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
                return dr * dr + dg * dg + db * db <= 40 * 40;
            }

            private static Color32[] ReadPixels(Texture2D tex, int x, int y, int w, int h)
            {
                if (tex == null) return null;
                if (tex.isReadable)
                {
                    Color[] c = tex.GetPixels(x, y, w, h);
                    var outPx = new Color32[c.Length];
                    for (int i = 0; i < c.Length; i++) outPx[i] = c[i];
                    return outPx;
                }
                var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                var prev = RenderTexture.active;
                Texture2D tmp = null;
                try
                {
                    Graphics.Blit(tex, rt);
                    RenderTexture.active = rt;
                    tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tmp.ReadPixels(new Rect(x, y, w, h), 0, 0);
                    tmp.Apply();
                    return tmp.GetPixels32();
                }
                finally
                {
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    if (tmp != null) Object.Destroy(tmp);
                }
            }

            // ---------------------------------------------------------------- glyph building

            public Sprite Build(int number, Color32 colour)
            {
                // V drawn at unit scale then upscaled by the stroke width, like a scaled pixel font.
                int scale = Stroke;
                int h0 = Mathf.Max(3, Mathf.RoundToInt((float)Height / scale));
                if (h0 * scale != Height) { scale = 1; h0 = Height; }
                byte[,] v = BuildV(h0, scale);
                int vw = v.GetLength(0), vh = v.GetLength(1);

                int totalW = number == 4 ? IW + Gap + vw : vw;
                int totalH = Mathf.Max(IH, vh);
                int canvasW = Mathf.Max(CanvasW, totalW + 2);
                int canvasH = Mathf.Max(CanvasH, totalH + 2);
                var canvas = new byte[canvasW, canvasH];

                // Horizontal centring on the canvas; vertical rows match the vanilla I's bar.
                int ox = (canvasW - totalW) / 2;
                int vBaseY = IBaseY + BarBottom;                 // canvas row of the bar's bottom
                if (vBaseY + Height > canvasH) vBaseY = canvasH - Height;
                if (vBaseY < 0) vBaseY = 0;
                int iBaseY = IBaseY;
                if (iBaseY + IH > canvasH) iBaseY = canvasH - IH;
                if (iBaseY < 0) iBaseY = 0;

                if (number == 4)
                {
                    Blit(canvas, IShape, ox, iBaseY, IW, IH);
                    ox += IW + Gap;
                }
                Blit(canvas, v, ox, vBaseY, vw, vh);

                // Shadow / outline for the procedurally drawn V (the copied I already has its own).
                if (ShadeMode == 1) AddShadow(canvas, canvasW, canvasH, ShadowDx, ShadowDy);
                else if (ShadeMode == 2) AddOutline(canvas, canvasW, canvasH);

                var tex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var clear = new Color32(0, 0, 0, 0);
                var secondary = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(colour.r * SecondaryFactor), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(colour.g * SecondaryFactor), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(colour.b * SecondaryFactor), 0, 255),
                    SecondaryAlpha);
                var pixels = new Color32[canvasW * canvasH];
                for (int y = 0; y < canvasH; y++)
                for (int x = 0; x < canvasW; x++)
                {
                    byte c = canvas[x, y];
                    pixels[y * canvasW + x] = c == Primary ? colour : c == Secondary ? secondary : clear;
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                var sprite = Sprite.Create(tex, new Rect(0, 0, canvasW, canvasH), PivotN, Ppu, 0, SpriteMeshType.FullRect);
                sprite.name = $"FiveLoadouts_Numeral{number}";
                return sprite;
            }

            private static void Blit(byte[,] canvas, byte[,] src, int ox, int oy, int w, int h)
            {
                int cw = canvas.GetLength(0), ch = canvas.GetLength(1);
                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    int cx = ox + x, cy = oy + y;
                    if (cx < 0 || cy < 0 || cx >= cw || cy >= ch || src[x, y] == Empty) continue;
                    canvas[cx, cy] = src[x, y];
                }
            }

            /// <summary>A Roman V: two diagonals meeting at the bottom, unit height h0, upscaled.</summary>
            private static byte[,] BuildV(int h0, int scale)
            {
                int w0 = h0;
                var unit = new byte[w0, h0];
                float half = (w0 - 1) / 2f;
                for (int r = 0; r < h0; r++)              // r = 0 is the top row
                {
                    int xl = Mathf.FloorToInt(r * half / (h0 - 1) + 0.0001f);
                    int xr = w0 - 1 - xl;
                    int y = h0 - 1 - r;                   // textures are bottom-up
                    unit[xl, y] = Primary;
                    unit[xr, y] = Primary;
                }
                if (scale == 1) return unit;
                var big = new byte[w0 * scale, h0 * scale];
                for (int x = 0; x < w0; x++)
                for (int y = 0; y < h0; y++)
                {
                    if (unit[x, y] == Empty) continue;
                    for (int sx = 0; sx < scale; sx++)
                    for (int sy = 0; sy < scale; sy++)
                        big[x * scale + sx, y * scale + sy] = Primary;
                }
                return big;
            }

            private static void AddShadow(byte[,] canvas, int w, int h, int dx, int dy)
            {
                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (canvas[x, y] != Primary) continue;
                    int sx = x + dx, sy = y + dy;
                    if (sx < 0 || sy < 0 || sx >= w || sy >= h || canvas[sx, sy] != Empty) continue;
                    canvas[sx, sy] = Secondary;
                }
            }

            private static void AddOutline(byte[,] canvas, int w, int h)
            {
                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (canvas[x, y] != Primary) continue;
                    for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int sx = x + ox, sy = y + oy;
                        if (sx < 0 || sy < 0 || sx >= w || sy >= h || canvas[sx, sy] != Empty) continue;
                        canvas[sx, sy] = Secondary;
                    }
                }
            }
        }
    }
}
