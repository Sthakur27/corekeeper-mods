using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Data.Configuration;
using PugMod;
using UnityEngine;

namespace ModOptions
{
    /// <summary>
    /// Entry point for mods: <c>SettingsPages.Create(this, "My Mod")</c>, chain options, then <c>Build()</c>.
    /// Values live in the owning mod's CoreLib config file ("&lt;mod name&gt;/config.cfg", section
    /// "Settings", key = key prefix + label), so they persist across sessions.
    /// </summary>
    public static class SettingsPages
    {
        private const string ConfigSection = "Settings";

        private static readonly List<SettingsPage> _pages = new List<SettingsPage>();
        private static readonly Dictionary<string, ConfigFile> _files = new Dictionary<string, ConfigFile>();

        /// <summary>Every built page: pinned pages first, then sorted by title.</summary>
        public static IReadOnlyList<SettingsPage> All => _pages;

        /// <summary>
        /// Starts a page owned by <paramref name="mod"/>. <paramref name="keyPrefix"/> is prepended to every
        /// label to form the stored key (lets one mod own several pages in one config file).
        /// </summary>
        public static SettingsPage Create(IMod mod, string title, string keyPrefix = "")
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            if (info == null) throw new InvalidOperationException("ModOptions: mod metadata not found for " + title);
            string modName = info.Metadata.name;
            if (!_files.TryGetValue(modName, out var file))
            {
                file = new ConfigFile(modName + "/config.cfg", true, info);
                _files[modName] = file;
            }
            return new SettingsPage(title, keyPrefix ?? "", file);
        }

        internal static void Add(SettingsPage page)
        {
            _pages.RemoveAll(p => p.Title == page.Title);
            _pages.Add(page);
            _pages.Sort((a, b) => a.PinnedToTop != b.PinnedToTop ? (a.PinnedToTop ? -1 : 1)
                : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
        }

        internal static ConfigEntry<T> Bind<T>(ConfigFile file, string key, T def)
        {
            return file.Bind(new ConfigDefinition(ConfigSection, key), def);
        }
    }

    /// <summary>One page in the Mod Options menu: a title, an optional description and its options.</summary>
    public sealed class SettingsPage
    {
        private readonly ConfigFile _file;
        private readonly string _keyPrefix;
        private readonly List<SettingBase> _items = new List<SettingBase>();

        public string Title { get; }
        public string Description { get; private set; } = "";
        /// <summary>Listed above the alphabetical pages (see <see cref="PinToTop"/>).</summary>
        public bool PinnedToTop { get; private set; }
        public IReadOnlyList<SettingBase> Items => _items;

        internal SettingsPage(string title, string keyPrefix, ConfigFile file)
        {
            Title = title;
            _keyPrefix = keyPrefix;
            _file = file;
        }

        /// <summary>Lists this page at the top of the Mod Options menu instead of alphabetically.</summary>
        public SettingsPage PinToTop()
        {
            PinnedToTop = true;
            return this;
        }

        /// <summary>Text shown above the options.</summary>
        public SettingsPage Hint(string text)
        {
            Description = text ?? "";
            return this;
        }

        public SettingsPage Toggle(out Setting<bool> handle, string label, bool defaultValue)
        {
            var s = new ToggleSetting(label, SettingsPages.Bind(_file, _keyPrefix + label, defaultValue), defaultValue);
            return Add(s, out handle);
        }

        /// <summary>Cycles through <paramref name="values"/> (left/right wraps around).</summary>
        public SettingsPage Choice(out Setting<string> handle, string label, string[] values, string defaultValue)
        {
            var s = new ChoiceSetting(label, SettingsPages.Bind(_file, _keyPrefix + label, defaultValue), values, defaultValue);
            return Add(s, out handle);
        }

        /// <summary>Whole number from <paramref name="min"/> to <paramref name="max"/>.</summary>
        public SettingsPage Stepper(out Setting<int> handle, string label, int min, int max, int defaultValue, int step = 1)
        {
            var s = new StepperSetting(label, SettingsPages.Bind(_file, _keyPrefix + label, defaultValue), min, max, defaultValue, step);
            return Add(s, out handle);
        }

        /// <summary>Decimal number from <paramref name="min"/> to <paramref name="max"/> in steps of <paramref name="step"/>.</summary>
        public SettingsPage Slider(out Setting<float> handle, string label, float min, float max, float defaultValue, float step)
        {
            var s = new SliderSetting(label, SettingsPages.Bind(_file, _keyPrefix + label, defaultValue), min, max, defaultValue, step);
            return Add(s, out handle);
        }

        private SettingsPage Add<T>(Setting<T> setting, out Setting<T> handle)
        {
            setting.SanitizeStored();
            _items.Add(setting);
            handle = setting;
            return this;
        }

        /// <summary>Sets every option on this page back to the mod's default.</summary>
        public void ResetAll()
        {
            foreach (var item in _items) item.ResetToDefault();
            Debug.Log($"[ModOptions] \"{Title}\" reset to defaults.");
        }

        /// <summary>Adds the page to the menu.</summary>
        public void Build()
        {
            SettingsPages.Add(this);
            Debug.Log($"[ModOptions] Page \"{Title}\" with {_items.Count} options.");
        }
    }
}
