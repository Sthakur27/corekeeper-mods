using System.Linq;
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
    /// enemy attack converter doubles its damage, and HealthAuthoring.ComputeMaxHealth gives enemies
    /// 1.5x their level-based health. For regular
    /// (non-boss) enemies this mod instead gives damage = normal-mode damage x
    /// <see cref="Tuning.DamageMultiplier"/> and health = level-based health x
    /// <see cref="Tuning.HealthMultiplier"/> (see Patches). Bosses, their parts and projectiles,
    /// loot, and non-hard worlds are untouched. Settings apply the next time a world is loaded.
    /// </summary>
    public sealed class HardModeTuningMod : IMod
    {
        public const string Name = "HardModeTuning";
        public const string Version = "1.0.0";

        private static SettingHandle<string> _damage;
        private static SettingHandle<string> _health;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "Hard mode, regular enemies only (bosses keep full hard mode). Compared to normal mode; vanilla hard is 2x damage. Applies next time you load a world. Regular enemy health, as a multiple of the normal (level-based) health.";

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
                .Choice(out _damage, prefix + "Regular enemy damage", Tuning.Ladder, Tuning.DefaultToken)
                .Choice(out _health, prefix + "Regular enemy health", Tuning.Ladder, Tuning.DefaultToken);


            Tuning.DamageMultiplier = Tuning.Parse(_damage.Value);
            Tuning.HealthMultiplier = Tuning.Parse(_health.Value);
            _damage.OnChanged += t => { Tuning.DamageMultiplier = Tuning.Parse(t); Log(); };
            _health.OnChanged += t => { Tuning.HealthMultiplier = Tuning.Parse(t); Log(); };
            Log();
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
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
