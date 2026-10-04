using System.Linq;
using ModOptions;
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
        public const string Version = "1.1.0";

        private static Setting<string> _damage;
        private static Setting<string> _health;
        private static Setting<string> _moveSpeed;
        private static Setting<string> _projectileSpeed;
        private static Setting<string> _rechargeSpeed;
        private static Setting<string> _bossDamage;
        private static Setting<string> _bossHealth;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "Hard mode worlds only; applies next time you load a world. Regular enemy damage/health are vs normal mode (vanilla hard: 2x damage, 1.5x health). Speeds apply to regular enemies; recharge speed 1.25x = attacks come back 1.25x as fast. Boss damage/health are vs vanilla hard (1x = vanilla).";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Hard Mode Tuning").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _damage, "Regular enemy damage", Tuning.Ladder, Tuning.DefaultToken)
                .Choice(out _health, "Regular enemy health", Tuning.Ladder, Tuning.DefaultToken)
                .Choice(out _moveSpeed, "Regular enemy move speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out _projectileSpeed, "Regular enemy projectile speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out _rechargeSpeed, "Regular enemy recharge speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out _bossDamage, "Boss damage (vs vanilla hard)", Tuning.FineLadder, Tuning.FineDefault)
                .Choice(out _bossHealth, "Boss health (vs vanilla hard)", Tuning.FineLadder, Tuning.FineDefault);

            Apply();
            foreach (var handle in new[] { _damage, _health, _moveSpeed, _projectileSpeed, _rechargeSpeed, _bossDamage, _bossHealth })
                handle.OnChanged += _ => { Apply(); Log(); };
            Log();
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }


        private static void Apply()
        {
            Tuning.DamageMultiplier = Tuning.Parse(_damage.Value);
            Tuning.HealthMultiplier = Tuning.Parse(_health.Value);
            Tuning.MoveSpeedMultiplier = Tuning.Parse(_moveSpeed.Value, 1f);
            Tuning.ProjectileSpeedMultiplier = Tuning.Parse(_projectileSpeed.Value, 1f);
            Tuning.RechargeSpeedMultiplier = Tuning.Parse(_rechargeSpeed.Value, 1f);
            Tuning.BossDamageMultiplier = Tuning.Parse(_bossDamage.Value, 1f);
            Tuning.BossHealthMultiplier = Tuning.Parse(_bossHealth.Value, 1f);
        }

        private static void Log()
        {
            Debug.Log($"[{Name}] Hard mode: regular enemies damage {Tuning.DamageMultiplier:0.##}x, health {Tuning.HealthMultiplier:0.##}x, " +
                      $"move {Tuning.MoveSpeedMultiplier:0.##}x, projectiles {Tuning.ProjectileSpeedMultiplier:0.##}x, recharge {Tuning.RechargeSpeedMultiplier:0.##}x; " +
                      $"bosses damage {Tuning.BossDamageMultiplier:0.##}x, health {Tuning.BossHealthMultiplier:0.##}x (applies on next world load)");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
