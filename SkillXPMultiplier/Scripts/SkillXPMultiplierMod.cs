using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkillXPMultiplier
{
    /// <summary>
    /// Per-skill XP multiplier. Every XP grant in the game is an AddSkillValueCD entity that the
    /// game's AddSkillValueSystem consumes; <see cref="Systems.ScaleSkillXPSystem"/> scales the
    /// amount on those entities first, so the multiplier applies to exactly the XP being earned,
    /// with no polling and no double counting. Multipliers are set per skill in Mod Settings
    /// (or with the /xp chat command) and persist in CoreLib's config for this mod.
    ///
    /// Do not run together with the mod.io "XP Multiplier" mod: it multiplies whatever XP it
    /// sees land, including ours.
    /// </summary>
    public sealed class SkillXPMultiplierMod : IMod
    {
        public const string Name = "SkillXPMultiplier";
        public const string Version = "1.0.0";

        private static readonly SettingHandle<string>[] _handles = new SettingHandle<string>[SkillXPTable.SkillCount];
        private LoadedMod _modInfo;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");

            _modInfo = API.ModLoader.LoadedMods.FirstOrDefault(info => info.Handlers.Contains(this));
            if (_modInfo == null)
            {
                Debug.LogError($"[{Name}] Mod metadata not found; /xp command unavailable.");
                return;
            }

            // Same pattern as the mod.io XP Multiplier: chat commands only make sense with a UI.
            if (Application.isBatchMode) return;
            CoreLibMod.LoadSubmodule(typeof(CommandModule));
            CommandModule.AddCommands(_modInfo.ModId, Name);
        }

        public void Init()
        {
            var section = ModSettings.Section(this)
                .Hint("XP multiplier per skill. 1x = vanilla, 0x = skill frozen. Applies instantly.");

            for (int i = 0; i < SkillXPTable.SkillCount; i++)
            {
                section.Choice(out _handles[i], SkillXPTable.SkillNames[i], SkillXPTable.Ladder, SkillXPTable.DefaultToken);
                int index = i;
                SkillXPTable.Set(index, SkillXPTable.Parse(_handles[index].Value));
                _handles[index].OnChanged += token => SkillXPTable.Set(index, SkillXPTable.Parse(token));
            }
            section.Build();

            Debug.Log($"[{Name}] Loaded. Multipliers: {Describe()}");
        }

        /// <summary>Set one skill's multiplier (snapped to the menu ladder) and persist it. Returns the token used.</summary>
        public static string SetSkill(int skillIndex, float wanted)
        {
            string token = SkillXPTable.Snap(wanted);
            var handle = skillIndex >= 0 && skillIndex < _handles.Length ? _handles[skillIndex] : null;
            if (handle != null) handle.Value = token;      // OnChanged updates the table
            else SkillXPTable.Set(skillIndex, SkillXPTable.Parse(token));
            return token;
        }

        public static string Describe()
        {
            var parts = new string[SkillXPTable.SkillCount];
            for (int i = 0; i < SkillXPTable.SkillCount; i++)
                parts[i] = $"{SkillXPTable.SkillNames[i]} {SkillXPTable.Get(i):0.##}x";
            return string.Join(", ", parts);
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
