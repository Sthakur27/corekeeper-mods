using System.Linq;
using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuffDurationFloor
{
    /// <summary>
    /// Raises every food/potion buff that would last less than the configured floor up to that
    /// floor. Two independent floors: one for ordinary buffs, one for health/mana regeneration
    /// over time (each can be Off = vanilla). Buffs already longer are untouched, and so are
    /// equipment, skill, aura and environmental conditions and every negative effect.
    ///
    /// Mechanism: when a player eats or drinks, the game's Burst job
    /// (EatableSlotConsumeResultEvaluationSystem → ConditionUIExtensions.GetConditionsOnConsume)
    /// reads the buff list, including each buff's duration, from the GivesConditionsWhenConsumed
    /// buffer on the consumed item's prefab (for cooked food: the two ingredients' "when cooked"
    /// entries). <see cref="Systems.ConsumableDurationFloorSystem"/> rewrites those prefab
    /// durations in both the client and server world, so the game itself hands out the longer
    /// buff, prediction stays in sync, and item tooltips show the real duration. The few
    /// talent-injected consume buffs whose duration is hard-coded inside that job (boss damage
    /// from fish, melee attack speed from cooked food, heal-over-time from potions) are not
    /// reachable this way and keep their vanilla length.
    /// </summary>
    public sealed class BuffDurationFloorMod : IMod
    {
        public const string Name = "BuffDurationFloor";
        public const string Version = "1.2.0";

        private static SettingHandle<string> _buffFloor;
        private static SettingHandle<string> _healingFloor;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "Food and potion buffs shorter than the floor last the floor instead. Off = vanilla. Longer buffs and debuffs are unchanged.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul registers every feature's settings in one section
            var section = ModSettings.Section(this).Hint(SettingsHint);
            RegisterSettings(section, "");
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>, each label prefixed with <paramref name="prefix"/>.</summary>
        public static void RegisterSettings(SectionBuilder section, string prefix)
        {
            section
                .Choice(out _buffFloor, prefix + "Minimum buff duration", FloorSettings.Ladder, FloorSettings.DefaultBuffToken)
                .Choice(out _healingFloor, prefix + "Minimum healing duration", FloorSettings.Ladder, FloorSettings.DefaultHealingToken);


            FloorSettings.SetBuffFloor(FloorSettings.Parse(_buffFloor.Value, FloorSettings.DefaultBuffSeconds));
            FloorSettings.SetHealingFloor(FloorSettings.Parse(_healingFloor.Value, FloorSettings.DefaultHealingSeconds));
            _buffFloor.OnChanged += token => FloorSettings.SetBuffFloor(FloorSettings.Parse(token, FloorSettings.DefaultBuffSeconds));
            _healingFloor.OnChanged += token => FloorSettings.SetHealingFloor(FloorSettings.Parse(token, FloorSettings.DefaultHealingSeconds));

            Debug.Log($"[{Name}] Loaded. Minimum buff duration: {FloorSettings.Token((int)FloorSettings.BuffFloorSeconds)}, minimum healing duration: {FloorSettings.Token((int)FloorSettings.HealingFloorSeconds)}");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }


        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
