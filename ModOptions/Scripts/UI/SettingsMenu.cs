using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModOptions.UI
{
    /// <summary>
    /// The Mod Options menu, built at runtime from the game's own Gameplay settings screen so it looks and
    /// navigates like vanilla (scrolling, mouse, keyboard, controller, Back):
    ///  - Options gets a "Mod Options" button;
    ///  - that opens a list with one row per settings page;
    ///  - a row opens the page menu with that page's options (left/right or click to change).
    /// Both menus are clones of the Gameplay settings menu with its rows replaced by our own
    /// <see cref="SettingsRow"/>s. Nothing of other mods' menus is touched.
    /// </summary>
    public static class SettingsMenu
    {
        public const string ButtonLabel = "Mod Options";
        private const string ListTitle = "Mod Options";
        // The scroll viewport is about 20 units wide (x -10..10); text outside it is clipped.
        private const float DescriptionWidth = 18.5f;
        private const float LabelWidth = 11.5f;
        // Labels are long and values short, so value rows sit right of centre (vanilla: label ends at -0.63).
        private const float ValueRowShift = 2.75f;

        private static RadicalOptionsMenu _list;
        private static RadicalOptionsMenu _page;
        private static GameObject _valueTemplate;
        private static GameObject _buttonTemplate;
        private static OpenSettingsButton _button;
        private static string _pageTitle = "";
        private static bool _failed;

        /// <summary>When a menu was last opened (unscaled time); clicks right after are ignored.</summary>
        internal static float OpenedAt = -10f;

        public static void Update()
        {
            if (_failed || SettingsPages.All.Count == 0) return;
            try
            {
                var menus = Manager.menu;
                if (menus == null || menus.gameplayOptionsMenu == null || menus.optionsMenu == null) return;
                if (_list == null || _page == null) BuildMenus(menus.gameplayOptionsMenu);
                if (_button == null) InjectButton(menus.optionsMenu);
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError("[ModOptions] Menu disabled: " + ex);
            }
        }

        public static void OpenList()
        {
            if (_list == null) return;
            var rows = Rebuild(_list);
            foreach (var page in SettingsPages.All)
            {
                var p = page;
                rows.Add(AddRow(_list, _buttonTemplate, p.Title, null, () => OpenPage(p)));
            }
            Show(_list, rows, ListTitle);
        }

        private static void OpenPage(SettingsPage page)
        {
            var rows = Rebuild(_page);
            if (!string.IsNullOrEmpty(page.Description))
            {
                var info = AddRow(_page, _buttonTemplate, page.Description, null, null);
                info.IsInfo = true;
                // Plain text, not a menu entry: no selection colours (they would show it greyed out / red).
                foreach (var effect in info.GetComponentsInChildren<PugTextEffectMenuOption>(true)) Object.DestroyImmediate(effect);
                info.canBeActivated = false;
                if (info.labelText != null) info.labelText.maxWidth = DescriptionWidth;
                rows.Add(info);
            }
            foreach (var item in page.Items)
            {
                var row = AddRow(_page, _valueTemplate, item.Label, item, null);
                if (row.labelText != null)
                {
                    row.labelText.maxWidth = LabelWidth;
                    row.labelText.transform.localPosition += new Vector3(ValueRowShift, 0f, 0f);
                }
                if (row.valueText != null) row.valueText.transform.localPosition += new Vector3(ValueRowShift, 0f, 0f);
                rows.Add(row);
            }
            if (page.Items.Count > 0)
            {
                var reset = AddRow(_page, _buttonTemplate, "Reset to defaults", null, null);
                reset.ConfirmLabel = "Click again to reset";
                reset.OnClick = () =>
                {
                    page.ResetAll();
                    foreach (var option in _page.menuOptions)
                        if (option is SettingsRow r) r.Refresh();
                };
                reset.extraVerticalSpacing = 0.5f;
                rows.Add(reset);
            }
            _pageTitle = page.Title;
            Show(_page, rows, _pageTitle);
        }

        private static void Show(RadicalOptionsMenu menu, List<RadicalMenuOption> rows, string title)
        {
            menu.menuOptions.Clear();
            menu.menuOptions.AddRange(rows);
            OpenedAt = Time.unscaledTime;
            Manager.menu.PushMenu(menu);
            SetTitle(menu, title);
            // The description row is not selectable; start keyboard / controller selection below it.
            if (menu.selectedIndex == 0 && rows.Count > 1 && rows[0] is SettingsRow first && first.IsInfo)
                menu.SelectOptionIndex(1);
        }

        // ---------------------------------------------------------------- building

        private static void BuildMenus(RadicalMenu source)
        {
            Transform sourceScroll = source.transform.Find("Options/Scroll");
            if (sourceScroll == null) throw new Exception("Gameplay settings layout changed (Options/Scroll missing)");

            var holder = new GameObject("ModOptionsTemplates");
            holder.SetActive(false);
            holder.transform.SetParent(source.transform.parent, false);
            _valueTemplate = MakeTemplate(FindRow(sourceScroll, "Season Option", 2), holder.transform);
            _buttonTemplate = MakeTemplate(FindRow(sourceScroll, "GetOutOfStuckPosition", 1), holder.transform);

            _list = CloneMenu(source, "ModOptionsList");
            _page = CloneMenu(source, "ModOptionsPage");
            Debug.Log($"[ModOptions] Menu ready ({SettingsPages.All.Count} pages).");
        }

        /// <summary>The named row, or else the first row with exactly <paramref name="texts"/> PugTexts.</summary>
        private static Transform FindRow(Transform scroll, string name, int texts)
        {
            Transform row = string.IsNullOrEmpty(name) ? null : scroll.Find(name);
            if (row != null) return row;
            for (int i = 0; i < scroll.childCount; i++)
            {
                Transform child = scroll.GetChild(i);
                if (child.GetComponent<RadicalMenuOption>() != null && child.GetComponentsInChildren<PugText>(true).Length == texts)
                    return child;
            }
            throw new Exception("No template row with " + texts + " texts in Gameplay settings");
        }

        private static GameObject MakeTemplate(Transform row, Transform holder)
        {
            GameObject t = Object.Instantiate(row.gameObject, holder);
            t.name = "Template " + row.name;
            foreach (var option in t.GetComponents<RadicalMenuOption>()) Object.DestroyImmediate(option);
            foreach (var col in t.GetComponents<BoxCollider>()) Object.DestroyImmediate(col);
            foreach (var text in t.GetComponentsInChildren<PugText>(true))
            {
                text.localize = false;
                text.formatFields = new string[0];
                text.Clear();
                for (int i = text.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(text.transform.GetChild(i).gameObject); // glyphs copied from the source
            }
            t.SetActive(true); // stays hidden under the inactive holder
            return t;
        }

        private static RadicalOptionsMenu CloneMenu(RadicalMenu source, string name)
        {
            GameObject go = Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = name;
            go.SetActive(false);
            var menu = go.GetComponent<RadicalOptionsMenu>();
            if (menu == null) throw new Exception("Gameplay settings is not a RadicalOptionsMenu");
            menu.writeSettings = false;
            Transform scroll = go.transform.Find("Options/Scroll");
            for (int i = scroll.childCount - 1; i >= 0; i--)
            {
                GameObject row = scroll.GetChild(i).gameObject;
                row.transform.SetParent(null, false);
                Object.DestroyImmediate(row);
            }
            menu.menuOptions.Clear();
            foreach (var text in go.transform.Find("Title").GetComponentsInChildren<PugText>(true))
            {
                text.localize = false;
                text.formatFields = new string[0];
            }
            return menu;
        }

        /// <summary>Removes the menu's current rows and returns an empty list for the new ones.</summary>
        private static List<RadicalMenuOption> Rebuild(RadicalOptionsMenu menu)
        {
            Transform scroll = menu.transform.Find("Options/Scroll");
            for (int i = scroll.childCount - 1; i >= 0; i--)
            {
                GameObject row = scroll.GetChild(i).gameObject;
                foreach (var text in row.GetComponentsInChildren<PugText>(true)) text.Clear(); // return pooled glyphs
                row.transform.SetParent(null, false);
                Object.Destroy(row);
            }
            menu.menuOptions.Clear();
            return new List<RadicalMenuOption>();
        }

        private static SettingsRow AddRow(RadicalOptionsMenu menu, GameObject template, string label, SettingBase setting, Action onClick)
        {
            Transform scroll = menu.transform.Find("Options/Scroll");
            GameObject go = Object.Instantiate(template, scroll);
            go.name = "Row " + label;
            var row = go.AddComponent<SettingsRow>();
            var texts = go.GetComponentsInChildren<PugText>(true);
            row.labelText = texts.Length > 0 ? texts[0] : null;
            row.valueText = texts.Length > 1 ? texts[1] : null;
            row.activeInTitle = true;
            row.activeInSPStage = true;
            row.LabelString = label;
            row.Setting = setting;
            row.OnClick = onClick;
            row.SetParentMenu(menu);
            return row;
        }

        private static void SetTitle(RadicalMenu menu, string title)
        {
            Transform t = menu.transform.Find("Title");
            if (t == null) return;
            foreach (var text in t.GetComponentsInChildren<PugText>(true)) text.Render(title);
        }

        private static void InjectButton(RadicalMenu options)
        {
            Transform scroll = options.transform.Find("Options/Scroll");
            if (scroll == null) throw new Exception("Options layout changed (Options/Scroll missing)");
            Transform source = scroll.Find("Go to gameplay settings") ?? FindRow(scroll, "", 1);
            Transform after = scroll.Find("Go to UI settings") ?? source;

            GameObject go = Object.Instantiate(source.gameObject, scroll);
            go.name = "GoToModOptions";
            foreach (var option in go.GetComponents<RadicalMenuOption>()) Object.DestroyImmediate(option);
            foreach (var col in go.GetComponents<BoxCollider>()) Object.DestroyImmediate(col);
            var label = go.GetComponentInChildren<PugText>(true);
            if (label != null)
            {
                label.localize = false;
                label.formatFields = new string[0];
                label.Clear();
                for (int i = label.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(label.transform.GetChild(i).gameObject);
            }
            go.transform.SetSiblingIndex(after.GetSiblingIndex() + 1);

            _button = go.AddComponent<OpenSettingsButton>();
            _button.labelText = label;
            _button.activeInTitle = true;
            _button.activeInSPStage = true;
            _button.SetParentMenu(options);

            // The options menu collected its rows in Awake; slot ours in after "UI settings".
            var afterOption = after.GetComponent<RadicalMenuOption>();
            int index = afterOption != null ? options.menuOptions.IndexOf(afterOption) : -1;
            if (index >= 0) options.menuOptions.Insert(index + 1, _button);
            else if (options.menuOptions.Count > 0) options.menuOptions.Add(_button);
            Debug.Log("[ModOptions] Added \"" + ButtonLabel + "\" to Options.");
        }
    }
}
