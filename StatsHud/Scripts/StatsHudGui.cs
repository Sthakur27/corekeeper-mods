using UnityEngine;

namespace StatsHud
{
    /// <summary>
    /// "Plain" text style: draws the readout with Unity's immediate-mode GUI at the same spot as the game-font text.
    /// Only the repaint event draws; the strings come from <see cref="HudView"/> (rebuilt 4x a second), so this only
    /// costs a few label draws per frame.
    /// </summary>
    public sealed class StatsHudGui : MonoBehaviour
    {
        internal static readonly string[] Lines = new string[4];
        private static StatsHudGui _instance;
        private GUIStyle _style, _shadow;
        private int _fontSize;

        internal static StatsHudGui Ensure(GameObject host)
        {
            if (_instance == null) _instance = host.AddComponent<StatsHudGui>();
            return _instance;
        }

        internal static void Hide()
        {
            if (_instance != null) _instance.enabled = false;
        }

        private void OnGUI()
        {
            if (!StatsHudMod.Plain || Event.current.type != EventType.Repaint) return;
            if (Manager.prefs != null && Manager.prefs.hideInGameUI) return;
            var camGo = Manager.ui != null ? Manager.ui.UICamera : null;
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null) return;

            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 45f));
            if (_style == null || size != _fontSize)
            {
                _fontSize = size;
                _style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, wordWrap = false };
                _style.normal.textColor = Color.white;
                _shadow = new GUIStyle(_style);
                _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            }

            Vector3 sp = cam.WorldToScreenPoint(HudView.Anchor);
            float x = sp.x, y = Screen.height - sp.y;
            // The game font's letters are about 7 game pixels tall; scale the gap setting by the same ratio.
            float lineHeight = size + StatsHudMod.LineGapPixels * size / 7f;
            int off = Mathf.Max(1, size / 12);
            if (StatsHudMod.BottomLeft)
            {
                // The anchor is the readout's bottom edge: start high enough that the last line ends there.
                int count = 0;
                foreach (var l in Lines) if (l != null) count++;
                y -= count * lineHeight;
            }
            foreach (var line in Lines)
            {
                if (line == null) continue;
                var rect = new Rect(x, y, size * 30f, lineHeight);
                GUI.Label(new Rect(rect.x + off, rect.y + off, rect.width, rect.height), line, _shadow);
                GUI.Label(rect, line, _style);
                y += lineHeight;
            }
        }
    }
}
