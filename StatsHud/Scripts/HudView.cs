using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StatsHud
{
    /// <summary>
    /// The readout: up to four clones of the health bar's number text, parented to the health bar UI so they follow
    /// the HUD's scale, fade and "hide UI" without any work of ours. Placed right of the health bar background, or
    /// under the health and hunger bars (setting "Position").
    /// </summary>
    internal static class HudView
    {
        private const int Lines = 4;
        private const float Margin = 0.25f;          // gap to the bars, HUD units
        private const float FallbackLineHeight = 0.45f;
        private const float PixelsPerUnit = 16f; // the HUD's art is 16 pixels per unit
        private const float TeleportSpeed = 60f;     // tiles/s above this = a teleport, not movement

        private static PlayerHealthBarUI _bar;
        private static GameObject _root;
        private static readonly PugText[] _texts = new PugText[Lines];
        private static readonly string[] _shown = new string[Lines];
        private static bool _failed, _needsLayout;

        private static float _nextRefresh, _lastRefresh;
        private static int _frames;
        private static float _frameTime;
        private static Vector3 _lastPos;
        private static bool _hasLastPos;
        private static PlayerController _lastPlayer;

        public static void Relayout() => _needsLayout = true;

        /// <summary>Top-left of the readout in world space (the Plain style draws there too).</summary>
        internal static Vector3 Anchor => _root != null ? _root.transform.position : Vector3.zero;

        public static void Update()
        {
            if (_failed) return;
            try
            {
                _frames++;
                _frameTime += Time.unscaledDeltaTime;
                bool show = StatsHudMod.Show == null || StatsHudMod.Show.Value;
                var player = Manager.main != null ? Manager.main.player : null;
                // Hidden in menus (pause, settings) and while the game's UI is hidden.
                bool inMenu = (Manager.menu != null && Manager.menu.IsAnyMenuActive())
                    || (Manager.prefs != null && Manager.prefs.hideInGameUI);
                if (!show || player == null || inMenu)
                {
                    if (_root != null && _root.activeSelf) _root.SetActive(false);
                    StatsHudGui.Hide();
                    _hasLastPos = false;
                    return;
                }
                if (_root == null && !Build()) return;
                bool plain = StatsHudMod.Plain;
                if (_root.activeSelf == plain) _root.SetActive(!plain);
                StatsHudGui.Ensure(_bar.gameObject).enabled = true;
                if (_needsLayout || Time.unscaledTime >= _nextLayout) Layout(); // bars change width with max health

                float now = Time.unscaledTime;
                if (now < _nextRefresh) return;
                Refresh(player, now);
            }
            catch (Exception e)
            {
                _failed = true;
                Debug.LogError($"[{StatsHudMod.Name}] HUD disabled: {e}");
            }
        }

        private static bool Build()
        {
            _bar = Manager.ui != null ? Manager.ui.playerHealthBarUI : null;
            if (_bar == null || _bar.healthAmountText == null || _bar.background == null) return false;
            _root = new GameObject("StatsHud");
            _root.layer = _bar.gameObject.layer;
            _root.transform.SetParent(_bar.transform, false);
            for (int i = 0; i < Lines; i++)
            {
                var text = Object.Instantiate(_bar.healthAmountText.gameObject, _root.transform, false).GetComponent<PugText>();
                text.name = "StatsHudLine" + i;
                Prepare(text);
                _texts[i] = text;
                _shown[i] = null;
            }
            _needsLayout = true;
            Layout();
            _builtAt = Time.unscaledTime;
            _appliedOrder = int.MinValue;
            _diagnosed = false;
            Debug.Log($"[{StatsHudMod.Name}] HUD added beside the health bar.");
            return true;
        }

        /// <summary>A clone still holds the source's glyphs as children; free them and switch off localization.</summary>
        private static void Prepare(PugText text)
        {
            text.Clear(false, true);
            for (int i = text.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(text.transform.GetChild(i).gameObject);
            text.localize = false;
            text.formatFields = new string[0];
            text.maxWidth = 0f;
            text.style.horizontalAlignment = PugTextStyle.HorizontalAlignment.left;
            // The health number is drawn only inside the bar's sprite mask; ours sits outside the bar.
            text.style.maskInteraction = SpriteMaskInteraction.None;
            // PugText.Start switches the object off unless it renders on start or is told to stay enabled.
            text.DisableAutoInactivationOnStart();
            text.freeResourcesOnDisable = false;
            // The health number may carry a masked material of its own; use the default text material.
            text.overrideMaterial = null;
            text.SetTempColor(Color.white);
            text.gameObject.SetActive(true);
        }

        private static void Layout()
        {
            _needsLayout = false;
            _nextLayout = Time.unscaledTime + 5f;
            Transform t = _bar.transform;
            bool under = StatsHudMod.Position != null && StatsHudMod.Position.Value == StatsHudMod.Positions[1];
            // The bars' outline = every visible renderer of the health and hunger bars (background alone is only the
            // number's backdrop), minus the full-screen low-health border and our own text.
            _maxOrder = 0;
            bool any = false;
            Bounds b = default;
            bool log = !_loggedRenderers;
            _loggedRenderers = true;
            AddBounds(_bar.transform, ref b, ref any, log);
            var hunger = Object.FindFirstObjectByType<PlayerHungerBarUI>();
            if (hunger != null) AddBounds(hunger.transform, ref b, ref any, log);
            _barsBounds = any ? b : default;
            int order = Mathf.Max(_maxOrder + 10, MinOrder);
            bool orderChanged = _appliedOrder != order;
            _appliedOrder = order;
            if (orderChanged) foreach (var text in _texts) text.SetOrderInLayer(order);

            Vector3 world;
            bool bottom = StatsHudMod.BottomLeft;
            var uiCam = Manager.ui.UICamera != null ? Manager.ui.UICamera.GetComponent<Camera>() : null;
            if (bottom)
            {
                // Bottom left corner of the screen: the anchor is the readout's bottom-left, lines stack upward.
                if (uiCam == null) return;
                world = uiCam.ViewportToWorldPoint(new Vector3(BottomLeftCorner.x, BottomLeftCorner.y, uiCam.nearClipPlane));
                _layoutSource = "bottom-left";
            }
            else if (any && b.size.x >= 1f)
            {
                world = under ? new Vector3(b.min.x, b.min.y - Margin, 0f) : new Vector3(b.max.x + Margin, b.max.y, 0f);
                _layoutSource = "bars";
            }
            else
            {
                // Bars not found as renderers: fixed screen spot right of (or under) the bars, from the UI camera.
                var cam = Manager.ui.UICamera != null ? Manager.ui.UICamera.GetComponent<Camera>() : null;
                if (cam == null) return;
                Vector3 vp = under ? FallbackUnder : FallbackBeside;
                world = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, cam.nearClipPlane));
                _layoutSource = "viewport";
            }
            // Same depth as the bars' sprites. The health bar's transform sits at the UI camera's own z (-10), so a
            // local z of 0 put the text on the camera plane, in front of the near clip: drawn by nobody (isVisible false).
            world.z = BarsZ(b, any);
            _root.transform.localPosition = t.InverseTransformPoint(world);
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;
            if (orderChanged) for (int i = 0; i < Lines; i++) _shown[i] = null; // re-render with the new order in layer
            PlaceLines();
        }

        /// <summary>World z the bars draw at: their renderers' depth, else the health number's backdrop.</summary>
        private static readonly Vector3 BottomLeftCorner = new Vector3(0.012f, 0.025f, 0f);

        private static float BarsZ(Bounds bars, bool any)
        {
            if (any) return bars.center.z;
            return _bar.background != null ? _bar.background.bounds.center.z : 0f;
        }

        private static int _maxOrder, _appliedOrder = int.MinValue;
        private static float _nextLayout;
        private static Bounds _barsBounds;

        private const int MinOrder = 500;
        private static readonly Vector3 FallbackBeside = new Vector3(0.25f, 0.92f, 0f);
        private static readonly Vector3 FallbackUnder = new Vector3(0.03f, 0.68f, 0f);
        private static bool _loggedRenderers;
        private static string _layoutSource;

        private static void AddBounds(Transform root, ref Bounds b, ref bool any, bool log)
        {
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null) continue;
                if (_root != null && r.transform.IsChildOf(_root.transform)) continue;
                Bounds rb = r.bounds;
                bool used = r.enabled && r != _bar.screenborder
                    && rb.size.x <= 20f && rb.size.y <= 10f        // not a screen-sized overlay
                    && (rb.size.x >= 0.05f || rb.size.y >= 0.05f);  // not an empty placeholder
                if (log && n++ < 40)
                    Debug.Log($"[{StatsHudMod.Name}] renderer {r.gameObject.name} enabled={r.enabled} bounds={rb} order={r.sortingOrder} layer={r.sortingLayerName} used={used}");
                if (!used) continue;
                if (any) b.Encapsulate(rb); else { b = rb; any = true; }
                if (r.sortingOrder > _maxOrder) _maxOrder = r.sortingOrder;
            }
            if (log) Debug.Log($"[{StatsHudMod.Name}] {root.name}: {n} renderers");
        }

        private static void PlaceLines()
        {
            float h = LineHeight();
            int count = 0;
            for (int i = 0; i < Lines; i++) if (_texts[i].gameObject.activeSelf) count++;
            bool bottom = StatsHudMod.BottomLeft;
            int row = 0;
            for (int i = 0; i < Lines; i++)
            {
                if (!_texts[i].gameObject.activeSelf) continue;
                Vector3 p = _texts[i].transform.localPosition;
                // Top-anchored: rows go down from the anchor. Bottom-anchored: the last row sits on the anchor.
                float y = bottom ? h * (count - 1 - row + 0.5f) : -h * (row + 0.5f);
                _texts[i].transform.localPosition = new Vector3(0f, y, p.z);
                row++;
            }
        }

        private static float LineHeight()
        {
            var text = _texts[0];
            float h = text.dimensions.height * Mathf.Abs(text.transform.localScale.y);
            if (h <= 0.05f) h = FallbackLineHeight;
            return h + StatsHudMod.LineGapPixels / PixelsPerUnit;
        }

        private static void Refresh(PlayerController player, float now)
        {
            float dt = _lastRefresh > 0f ? now - _lastRefresh : 0f;
            _lastRefresh = now;
            _nextRefresh = now + 1f / StatsHudMod.RefreshPerSecond;

            Vector3 pos = player.WorldPosition;
            float speed = 0f;
            if (_hasLastPos && player == _lastPlayer && dt > 0f)
            {
                Vector3 d = pos - _lastPos;
                d.y = 0f;
                speed = d.magnitude / dt;
                if (speed > TeleportSpeed) speed = 0f;
            }
            _lastPos = pos;
            _lastPlayer = player;
            _hasLastPos = true;

            // Frames over their own unscaled durations (independent of timeScale and of when this refresh ran).
            float fps = _frameTime > 0f ? _frames / _frameTime : 0f;
            _frames = 0;
            _frameTime = 0f;

            int line = 0;
            bool layoutChanged = false;
            if (Want(StatsHudMod.ShowFps)) layoutChanged |= Set(line++, "FPS " + Mathf.RoundToInt(fps));
            if (Want(StatsHudMod.ShowDps))
            {
                float dps = DamageMeter.CurrentDps();
                string s = DamageMeter.InFight ? "DPS " + Format(dps)
                    : DamageMeter.LastFightDps > 0f ? "DPS 0  (last " + Format(DamageMeter.LastFightDps) + ")" : "DPS 0";
                layoutChanged |= Set(line++, s);
            }
            if (Want(StatsHudMod.ShowSpeed)) layoutChanged |= Set(line++, "Speed " + speed.ToString("0.0") + " tiles/s");
            if (Want(StatsHudMod.ShowCore))
            {
                float dist = new Vector2(pos.x, pos.z).magnitude;
                layoutChanged |= Set(line++, "Core " + Mathf.RoundToInt(dist) + " tiles");
            }
            for (int i = line; i < Lines; i++) layoutChanged |= Set(i, null);
            if (layoutChanged) PlaceLines();
            if (!_diagnosed && now > _builtAt + 10f) Diagnose();
        }

        private static string Glyph(PugText text)
        {
            if (text == null || text.glyphs.Count == 0 || text.glyphs[0] == null) return "none";
            var g = text.glyphs[0];
            return $"layer={g.gameObject.layer} active={g.gameObject.activeInHierarchy} enabled={g.enabled} color={g.color} sprite={(g.sprite != null ? g.sprite.name : "null")} "
                + $"mat={(g.sharedMaterial != null ? g.sharedMaterial.name : "null")} sortLayer={g.sortingLayerName} order={g.sortingOrder} mask={g.maskInteraction} visible={g.isVisible} pos={g.transform.position} scale={g.transform.lossyScale}";
        }

        private static bool _diagnosed;
        private static float _builtAt;

        /// <summary>One log line, ten seconds after the HUD is built, to tell where the text ended up.</summary>
        private static void Diagnose()
        {
            _diagnosed = true;
            var t = _texts[0];
            var cam = Manager.ui.UICamera != null ? Manager.ui.UICamera.GetComponent<Camera>() : null;
            Vector3 vp = cam != null ? cam.WorldToViewportPoint(t.transform.position) : Vector3.zero;
            int glyphs = 0, visible = 0;
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                glyphs++;
                if (sr.enabled && sr.gameObject.activeInHierarchy && sr.color.a > 0.01f) visible++;
            }
            Debug.Log($"[{StatsHudMod.Name}] diag: text='{_shown[0]}' active={t.gameObject.activeInHierarchy} rootLocal={_root.transform.localPosition} "
                + $"world={t.transform.position} lossyScale={t.transform.lossyScale} viewport={vp} glyphRenderers={glyphs} visible={visible} "
                + $"glyph0=[{Glyph(t)}] vanillaGlyph0=[{Glyph(_bar.healthAmountText)}] plain={StatsHudMod.Plain} "
                + $"camZ={(cam != null ? cam.transform.position.z : 0f)} near={(cam != null ? cam.nearClipPlane : 0f)} far={(cam != null ? cam.farClipPlane : 0f)} ortho={(cam != null && cam.orthographic)} "
                + $"source={_layoutSource} barsBounds={_barsBounds} order={_maxOrder + 10} glyphOrder={(t.glyphs.Count > 0 && t.glyphs[0] != null ? t.glyphs[0].sortingOrder : -1)} barActive={_bar.gameObject.activeInHierarchy} layer={t.gameObject.layer}");
        }

        private static bool Want(ModOptions.Setting<bool> s) => s == null || s.Value;

        private static string Format(float v) => v >= 10000f ? (v / 1000f).ToString("0.0") + "k" : Mathf.RoundToInt(v).ToString();

        /// <summary>Shows <paramref name="text"/> on line <paramref name="i"/> (null hides it). True when a line appeared or disappeared.</summary>
        private static bool Set(int i, string text)
        {
            StatsHudGui.Lines[i] = text;
            var label = _texts[i];
            if (text == null)
            {
                if (!label.gameObject.activeSelf) return false;
                label.gameObject.SetActive(false);
                _shown[i] = null;
                return true;
            }
            bool appeared = !label.gameObject.activeSelf;
            if (appeared) label.gameObject.SetActive(true);
            if (appeared || _shown[i] != text)
            {
                label.Render(text, false, true, true);
                label.SetTempColor(Color.white);
                _shown[i] = text;
            }
            return appeared;
        }
    }
}
