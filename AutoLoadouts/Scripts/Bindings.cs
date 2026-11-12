using System;
using System.Collections.Generic;
using System.Text;
using CoreLib.Data.Configuration;
using PugMod;
using UnityEngine;

namespace AutoLoadouts
{
    /// <summary>
    /// Item -> loadout links for the current character. One config entry per character (section
    /// "Characters", key = character GUID), value "objectID=preset;objectID=preset" with numeric ObjectIDs
    /// so modded items survive renames. Stored on this client only.
    /// </summary>
    public static class Bindings
    {
        private const string Section = "Characters";

        private static ConfigFile _file;
        private static ConfigEntry<string> _entry;
        private static readonly Dictionary<int, int> _map = new Dictionary<int, int>();

        public static void Open(LoadedMod info)
        {
            try
            {
                _file = new ConfigFile("AutoLoadouts/bindings.cfg", true, info);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{AutoLoadoutsMod.Name}] Could not open bindings file: {e}");
            }
        }

        /// <summary>Loads the links of the character that is playing now.</summary>
        public static void SelectCharacter()
        {
            _map.Clear();
            _entry = null;
            if (_file == null || Manager.saves == null) return;

            string key;
            try
            {
                key = Manager.saves.GetCharacterGuid().ToString();
            }
            catch
            {
                key = "character" + Manager.saves.GetCharacterId();
            }

            _entry = _file.Bind(Section, key, "", "Item ObjectID = loadout index (0-based)");
            foreach (string pair in (_entry.Value ?? "").Split(';'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                if (int.TryParse(pair.Substring(0, eq), out int id) && int.TryParse(pair.Substring(eq + 1), out int preset) && preset >= 0)
                    _map[id] = preset;
            }
            Debug.Log($"[{AutoLoadoutsMod.Name}] {_map.Count} item link(s) for character {key}.");
        }

        public static bool TryGet(ObjectID id, out int preset) => _map.TryGetValue((int)id, out preset);

        public static void Set(ObjectID id, int preset)
        {
            _map[(int)id] = preset;
            Save();
        }

        public static void Remove(ObjectID id)
        {
            if (_map.Remove((int)id)) Save();
        }

        private static void Save()
        {
            if (_entry == null) return;
            var sb = new StringBuilder();
            foreach (var kv in _map)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(kv.Key).Append('=').Append(kv.Value);
            }
            _entry.Value = sb.ToString(); // SaveOnConfigSet writes the file
        }
    }
}
