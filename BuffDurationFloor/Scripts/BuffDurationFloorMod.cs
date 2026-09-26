using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuffDurationFloor
{
    /// <summary>
    /// Raises every food/potion buff that would last less than the configured floor up to that
    /// floor. Buffs already longer are untouched, and so are equipment, skill, aura and
    /// environmental conditions, every negative effect and (unless "Also extend healing" is on)
    /// health/mana regeneration over time.
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
        public const string Version = "1.1.0";

        private SettingHandle<string> _floor;
        private SettingHandle<bool> _extendHealing;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            ModSettings.Section(this)
                .Hint("Food and potion buffs shorter than this last this long instead. Longer buffs and debuffs are unchanged.")
                .Choice(out _floor, "Minimum buff duration", FloorSettings.Ladder, FloorSettings.DefaultToken)
                .Toggle(out _extendHealing, "Also extend healing", false)
                .Build();

            FloorSettings.Set(FloorSettings.Parse(_floor.Value));
            FloorSettings.SetExtendHealing(_extendHealing.Value);
            _floor.OnChanged += token => FloorSettings.Set(FloorSettings.Parse(token));
            _extendHealing.OnChanged += value => FloorSettings.SetExtendHealing(value);

            Debug.Log($"[{Name}] Loaded. Minimum buff duration: {FloorSettings.Token((int)FloorSettings.FloorSeconds)}, extend healing: {FloorSettings.ExtendHealing}");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
