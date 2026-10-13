using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreLib.Data.Configuration;
using PugMod;

namespace ModOptions
{
    // Deliberately reads the registry only on user request, after the loader has called Init.
    // Never constructs, binds, reloads or saves a third-party ConfigFile during discovery.
    internal static class ConfigDiscovery
    {
        internal static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            var parts = path.Replace('\\', '/').Split('/');
            if (parts.Any(p => p.Length == 0 || p == "." || p == ".." || p.Contains(":"))) return "";
            return string.Join("/", parts);
        }

        internal static List<SettingsPage> Scan()
        {
            var mods = API.ModLoader.LoadedMods.ToArray();
            return Discover(ConfigFile.AllConfigFilesReadOnly, mods.Select(m => m.Metadata.name),
                SettingsPages.HasAuthoredPage, owner => mods.First(m =>
                    string.Equals(m.Metadata.name, owner, StringComparison.OrdinalIgnoreCase)).Metadata.displayName);
        }

        internal static string FriendlyName(string name)
        {
            string words = (name ?? "").Replace('_', ' ').Replace('-', ' ');
            words = Regex.Replace(words, "([A-Z]+)([A-Z][a-z])", "$1 $2");
            words = Regex.Replace(words, "([a-z0-9])([A-Z])", "$1 $2");
            return Regex.Replace(words, @"\s+", " ").Trim();
        }

        private static string FileLabel(string path)
        {
            string relative = path.Substring(path.IndexOf('/') + 1);
            int extension = relative.LastIndexOf('.');
            if (extension > relative.LastIndexOf('/')) relative = relative.Substring(0, extension);
            return FriendlyName(relative.Replace("/", " / "));
        }

        internal static List<SettingsPage> Discover(IEnumerable<ConfigFile> files, IEnumerable<string> loadedNames,
            Func<string, string, bool> hasAuthoredPage, Func<string, string> displayName = null)
        {
            var owners = new HashSet<string>(loadedNames, StringComparer.OrdinalIgnoreCase);
            var pages = new List<SettingsPage>();
            // Two live instances for one path could overwrite one another's values on save. Hide both.
            foreach (var group in files.Distinct().GroupBy(f => NormalizePath(f.ConfigFilePath), StringComparer.OrdinalIgnoreCase))
            {
                string path = group.Key;
                if (path.Length == 0 || group.Count() != 1 || path.IndexOf('/') < 1) continue;
                string owner = path.Substring(0, path.IndexOf('/'));
                if (!owners.Contains(owner) || owner.Equals("CoreLib", StringComparison.OrdinalIgnoreCase) ||
                    owner.Equals("ModOptions", StringComparison.OrdinalIgnoreCase) || hasAuthoredPage(owner, path)) continue;
                var file = group.First();
                if (!file.SaveOnConfigSet || Sensitive(path)) continue;
                string title = displayName?.Invoke(owner);
                if (string.IsNullOrWhiteSpace(title)) title = FriendlyName(owner);
                var page = new SettingsPage(title, "", file, owner, true);
                foreach (var entry in file.Values.OrderBy(e => e.Definition.Section).ThenBy(e => e.Definition.Key))
                {
                    var setting = Adapt(entry);
                    if (setting != null) page.AddDiscovered(setting);
                }
                if (page.Items.Count > 0) pages.Add(page);
            }
            // Only show the file's friendly name when a mod has multiple settings files.
            foreach (var group in pages.GroupBy(p => p.Owner, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                foreach (var page in group) page.Title += " - " + FileLabel(page.FilePath);
            return pages.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool Sensitive(string text)
        {
            string s = text.ToLowerInvariant();
            return new[] { "internal", "debug", "secret", "password", "token", "apikey", "api key",
                "security", "permission", "cheat", "id bind", "idbind", "migration" }.Any(s.Contains);
        }

        internal static SettingBase Adapt(ConfigEntryBase entry)
        {
            if (entry.Scope == null || entry.Scope.accessLevel != ConfigAccessLevel.Client ||
                !entry.ConfigFile.SaveOnConfigSet || string.IsNullOrWhiteSpace(entry.Description.Description) ||
                Sensitive(entry.Definition.Section + " " + entry.Definition.Key) ||
                (entry.Description.Tags != null && entry.Description.Tags.Length != 0)) return null;
            // Unknown tags may mean hidden/read-only or supply a custom editor. Do not guess their semantics.
            var acceptable = entry.Description.AcceptableValues;
            object[] choices = null;
            if (acceptable is AcceptableValueList<string> strings) choices = strings.AcceptableValues.Cast<object>().ToArray();
            else if (acceptable is AcceptableValueList<int> ints) choices = ints.AcceptableValues.Cast<object>().ToArray();
            else if (acceptable is AcceptableValueList<float> floats) choices = floats.AcceptableValues.Cast<object>().ToArray();
            else if (acceptable is AcceptableValueList<bool> bools) choices = bools.AcceptableValues.Cast<object>().ToArray();
            if (choices != null)
            {
                if (choices.Length < 2 || choices.Length > 64 || choices.Any(v => v == null ||
                    Convert.ToString(v, CultureInfo.InvariantCulture).Length > 60 ||
                    (v is float f && (float.IsNaN(f) || float.IsInfinity(f))))) return null;
                return new DiscoveredSetting(entry, (value, direction) =>
                {
                    int index = Array.IndexOf(choices, value);
                    return choices[index < 0 ? (direction > 0 ? 0 : choices.Length - 1) :
                        (index + (direction > 0 ? 1 : choices.Length - 1)) % choices.Length];
                });
            }
            if (entry.SettingType == typeof(bool) && acceptable == null)
                return new DiscoveredSetting(entry, (value, _) => !(bool)value);
            if (acceptable is AcceptableValueRange<int> ir && entry.SettingType == typeof(int))
                return new DiscoveredSetting(entry, (value, direction) =>
                    (int)Math.Max(ir.MinValue, Math.Min(ir.MaxValue, (long)(int)value + Math.Sign(direction))));
            if (acceptable is AcceptableValueRange<float> fr && entry.SettingType == typeof(float) &&
                !float.IsNaN(fr.MinValue) && !float.IsNaN(fr.MaxValue) &&
                !float.IsInfinity(fr.MinValue) && !float.IsInfinity(fr.MaxValue))
                return new DiscoveredSetting(entry, (value, direction) =>
                    (float)Math.Max(fr.MinValue, Math.Min(fr.MaxValue, (double)(float)value +
                        Math.Sign(direction) * ((double)fr.MaxValue - fr.MinValue) / 20d)));
            return null; // Free text, keybinds, unbounded numbers, enums and custom types need authored controls.
        }
    }

    internal sealed class DiscoveredSetting : SettingBase
    {
        private readonly ConfigEntryBase _entry;
        private readonly Func<object, int, object> _step;
        internal DiscoveredSetting(ConfigEntryBase entry, Func<object, int, object> step)
            : base((string.Equals(entry.Definition.Section, "General", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.Definition.Section, "Settings", StringComparison.OrdinalIgnoreCase) ? "" :
                    ConfigDiscovery.FriendlyName(entry.Definition.Section) + ": ") +
                ConfigDiscovery.FriendlyName(entry.Definition.Key))
        {
            _entry = entry;
            _step = step;
        }
        public override string Description => (_entry.Scope.requireReload ? "Restart required. " : "") +
            _entry.Description.Description;
        public override string DisplayValue => _entry.BoxedValue is bool b ? (b ? "On" : "Off") :
            _entry.BoxedValue is float f ? f.ToString("0.####", CultureInfo.InvariantCulture) :
            Convert.ToString(_entry.BoxedValue, CultureInfo.InvariantCulture);
        public override void Step(int direction)
        {
            if (direction != 0 && ConfigDiscovery.Adapt(_entry) != null) _entry.BoxedValue = _step(_entry.BoxedValue, direction);
        }
        public override void ResetToDefault() { } // No bulk reset of third-party configs.
    }
}
