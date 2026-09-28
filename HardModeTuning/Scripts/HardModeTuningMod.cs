using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HardModeTuning
{
    /// <summary>
    /// Tones hard mode down for regular enemies while bosses keep full hard mode.
    ///
    /// Vanilla hard mode is applied when a world's prefabs are converted on the server: every
    /// enemy attack converter doubles its damage, and HealthAuthoring.ComputeMaxHealth replaces the
    /// level-based health of level-scaled enemies with 1.5x the prefab's raw base health. For regular
    /// (non-boss) enemies this mod instead gives damage = normal-mode damage x
    /// <see cref="Tuning.DamageMultiplier"/> and health = level-based health x
    /// <see cref="Tuning.HealthMultiplier"/> (see Patches). Bosses, their parts and projectiles,
    /// loot, and non-hard worlds are untouched. Settings apply the next time a world is loaded.
    /// </summary>
    public sealed class HardModeTuningMod : IMod
    {
        public const string Name = "HardModeTuning";
        public const string Version = "1.0.0";

        private SettingHandle<string> _damage;
        private SettingHandle<string> _health;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            ModSettings.Section(this)
                .Hint("Hard mode, regular enemies only (bosses keep full hard mode). Compared to normal mode; vanilla hard is 2x damage. Applies next time you load a world.")
                .Choice(out _damage, "Regular enemy damage", Tuning.Ladder, Tuning.DefaultToken)
                .Hint("Regular enemy health, as a multiple of the normal (level-based) health.")
                .Choice(out _health, "Regular enemy health", Tuning.Ladder, Tuning.DefaultToken)
                .Build();

            Tuning.DamageMultiplier = Tuning.Parse(_damage.Value);
            Tuning.HealthMultiplier = Tuning.Parse(_health.Value);
            _damage.OnChanged += t => { Tuning.DamageMultiplier = Tuning.Parse(t); Log(); };
            _health.OnChanged += t => { Tuning.HealthMultiplier = Tuning.Parse(t); Log(); };
            Log();
        }

        private static void Log()
        {
            Debug.Log($"[{Name}] Regular enemies in hard mode: damage {Tuning.DamageMultiplier:0.##}x, health {Tuning.HealthMultiplier:0.##}x (applies on next world load)");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
