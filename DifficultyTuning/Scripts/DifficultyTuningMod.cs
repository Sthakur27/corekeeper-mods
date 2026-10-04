using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DifficultyTuning
{
    /// <summary>
    /// Tune enemies separately for hard mode worlds and normal worlds (formerly Hard Mode Tuning).
    ///
    /// Enemy stats are baked in when a world's prefabs are converted on the server. In hard mode every
    /// enemy attack converter doubles its damage and HealthAuthoring.ComputeMaxHealth gives enemies 1.5x
    /// their level-based health. Each world uses one <see cref="Profile"/> (<see cref="Tuning.Hard"/> or
    /// <see cref="Tuning.Normal"/>): regular enemies get damage = normal-mode damage x Damage, health =
    /// level-based health x Health, plus move/projectile/recharge speed; bosses get BossDamage and
    /// BossHealth on top of vanilla for that mode (see Patches). Defaults: hard mode regular enemies
    /// 1.5x damage (vanilla 2x), everything else vanilla. Settings apply the next time a world is loaded.
    /// </summary>
    public sealed class DifficultyTuningMod : IMod
    {
        public const string Name = "DifficultyTuning";
        public const string Version = "2.0.0";

        private sealed class Handles
        {
            public Setting<string> Damage, Health, MoveSpeed, ProjectileSpeed, RechargeSpeed, BossDamage, BossHealth;
        }

        private static readonly Handles HardHandles = new Handles();
        private static readonly Handles NormalHandles = new Handles();

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "Hard mode worlds only; applies next time you load a world. Regular enemy damage/health are vs normal mode (vanilla hard: 2x damage, 1.5x health). Speeds apply to regular enemies; recharge speed 1.25x = attacks come back 1.25x as fast. Boss damage/health are vs vanilla hard (1x = vanilla).";

        public const string NormalSettingsHint = "Normal (non-hard) worlds only; applies next time you load a world. Everything is vs vanilla normal mode (1x = vanilla). Speeds apply to regular enemies; recharge speed 1.25x = attacks come back 1.25x as fast.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var hard = SettingsPages.Create(this, "Difficulty Tuning (Hard)").Hint(SettingsHint);
            RegisterSettings(hard);
            hard.Build();
            var normal = SettingsPages.Create(this, "Difficulty Tuning (Normal)", "Normal Mode: ").Hint(NormalSettingsHint);
            RegisterNormalSettings(normal);
            normal.Build();
        }

        /// <summary>Hard mode options. Labels are unchanged from Hard Mode Tuning, so saved values carry over.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            Register(section, HardHandles, Tuning.Hard, Tuning.Ladder, Tuning.DefaultToken, "Boss damage (vs vanilla hard)", "Boss health (vs vanilla hard)");
        }

        /// <summary>Normal mode options (put them on a page with their own key prefix).</summary>
        public static void RegisterNormalSettings(SettingsPage section)
        {
            Register(section, NormalHandles, Tuning.Normal, Tuning.NormalLadder, Tuning.NormalDefaultToken, "Boss damage", "Boss health");
        }

        private static void Register(SettingsPage section, Handles h, Profile profile, string[] statLadder, string statDefault, string bossDamageLabel, string bossHealthLabel)
        {
            section
                .Choice(out h.Damage, "Regular enemy damage", statLadder, statDefault)
                .Choice(out h.Health, "Regular enemy health", statLadder, statDefault)
                .Choice(out h.MoveSpeed, "Regular enemy move speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out h.ProjectileSpeed, "Regular enemy projectile speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out h.RechargeSpeed, "Regular enemy recharge speed", Tuning.SpeedLadder, Tuning.FineDefault)
                .Choice(out h.BossDamage, bossDamageLabel, Tuning.FineLadder, Tuning.FineDefault)
                .Choice(out h.BossHealth, bossHealthLabel, Tuning.FineLadder, Tuning.FineDefault);

            float statFallback = Tuning.Parse(statDefault, 1f);
            void Apply()
            {
                profile.Damage = Tuning.Parse(h.Damage.Value, statFallback);
                profile.Health = Tuning.Parse(h.Health.Value, statFallback);
                profile.MoveSpeed = Tuning.Parse(h.MoveSpeed.Value, 1f);
                profile.ProjectileSpeed = Tuning.Parse(h.ProjectileSpeed.Value, 1f);
                profile.RechargeSpeed = Tuning.Parse(h.RechargeSpeed.Value, 1f);
                profile.BossDamage = Tuning.Parse(h.BossDamage.Value, 1f);
                profile.BossHealth = Tuning.Parse(h.BossHealth.Value, 1f);
            }

            Apply();
            foreach (var handle in new[] { h.Damage, h.Health, h.MoveSpeed, h.ProjectileSpeed, h.RechargeSpeed, h.BossDamage, h.BossHealth })
                handle.OnChanged += _ => { Apply(); Log(profile); };
            Log(profile);
        }

        private static void Log(Profile p)
        {
            Debug.Log($"[{Name}] {p.Name}: regular enemies damage {p.Damage:0.##}x, health {p.Health:0.##}x, " +
                      $"move {p.MoveSpeed:0.##}x, projectiles {p.ProjectileSpeed:0.##}x, recharge {p.RechargeSpeed:0.##}x; " +
                      $"bosses damage {p.BossDamage:0.##}x, health {p.BossHealth:0.##}x (applies on next world load)");
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
