using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using ModOptions;
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
    ///
    /// Also sets the total talent points per skill at max level (<see cref="TalentPoints"/>) and per pet
    /// (<see cref="PetTalentPoints"/>), and lets the
    /// host decide whether their settings apply to everyone in multiplayer (<see cref="MultiplayerSync"/>).
    /// </summary>
    public sealed class SkillXPMultiplierMod : IMod
    {
        public const string Name = "SkillXPMultiplier";
        public const string Version = "1.3.1";

        private static readonly Setting<string>[] _handles = new Setting<string>[SkillXPTable.SkillCount];
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

            // Dedicated servers need the commands too: /xpsync and /xphost carry the multiplayer settings.
            CoreLibMod.LoadSubmodule(typeof(CommandModule));
            if (!InOverhaul(this)) CommandModule.AddCommands(_modInfo.ModId, Name); // the overhaul registers all commands once
        }

        public const string Title = "Skill XP & Talent Points";
        public const string SettingsHint = "XP multiplier per skill (1x = vanilla, 0x = skill frozen) and how many talent points each skill has at level 100. Applies instantly. Multiplayer: the host chooses whether their settings apply to everyone or each player uses their own.";

        private static Setting<string> _talentHandle;
        private static Setting<string> _petTalentHandle;
        private static Setting<string> _modeHandle;

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, Title).Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds one option per skill to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Choice(out _modeHandle, "Multiplayer (when hosting)", MultiplayerSync.Modes, MultiplayerSync.HostDecides);
            MultiplayerSync.LocalEachPlayer = _modeHandle.Value == MultiplayerSync.EachPlayer;
            _modeHandle.OnChanged += token =>
            {
                MultiplayerSync.LocalEachPlayer = token == MultiplayerSync.EachPlayer;
                MultiplayerSync.OnLocalSettingsChanged();
            };

            section.Choice(out _talentHandle, "Talent points at level 100", TalentPoints.Ladder, TalentPoints.DefaultToken);
            TalentPoints.Total = TalentPoints.Parse(_talentHandle.Value);
            _talentHandle.OnChanged += token =>
            {
                TalentPoints.Total = TalentPoints.Parse(token);
                MultiplayerSync.OnLocalSettingsChanged();
            };

            section.Choice(out _petTalentHandle, "Pet talent points at level 10", PetTalentPoints.Ladder, PetTalentPoints.DefaultToken);
            PetTalentPoints.Total = PetTalentPoints.Parse(_petTalentHandle.Value);
            _petTalentHandle.OnChanged += token =>
            {
                PetTalentPoints.Total = PetTalentPoints.Parse(token);
                MultiplayerSync.OnLocalSettingsChanged();
            };

            for (int i = 0; i < SkillXPTable.SkillCount; i++)
            {
                section.Choice(out _handles[i], SkillXPTable.SkillNames[i], SkillXPTable.Ladder, SkillXPTable.DefaultToken);
                int index = i;
                SkillXPTable.Set(index, SkillXPTable.Parse(_handles[index].Value));
                _handles[index].OnChanged += token =>
                {
                    SkillXPTable.Set(index, SkillXPTable.Parse(token));
                    MultiplayerSync.OnLocalSettingsChanged();
                };
            }

            Debug.Log($"[{Name}] Loaded. Multiplayer: {_modeHandle.Value}. Talent points at max level: {_talentHandle.Value}. Pet talent points: {_petTalentHandle.Value}. Multipliers: {Describe()}");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }


        /// <summary>Set one skill's multiplier (snapped to the menu ladder) and persist it. Returns the token used.</summary>
        public static string SetSkill(int skillIndex, float wanted)
        {
            string token = SkillXPTable.Snap(wanted);
            var handle = skillIndex >= 0 && skillIndex < _handles.Length ? _handles[skillIndex] : null;
            if (handle != null) handle.Value = token;      // OnChanged updates the table
            else
            {
                SkillXPTable.Set(skillIndex, SkillXPTable.Parse(token));
                MultiplayerSync.OnLocalSettingsChanged();
            }
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
        public void Update()
        {
            MultiplayerSync.Tick();
        }
    }
}
