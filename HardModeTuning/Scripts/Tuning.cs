using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Pug.Conversion;
using PugMod;
using UnityEngine;

namespace HardModeTuning
{
    /// <summary>Settings plus the "should this prefab be tuned" rules shared by the patches.</summary>
    public static class Tuning
    {
        public static readonly string[] Ladder = { "1x", "1.25x", "1.5x", "1.75x", "2x" };
        public const string DefaultToken = "1.5x";

        /// <summary>Regular enemy damage relative to normal mode (vanilla hard = 2).</summary>
        public static float DamageMultiplier = 1.5f;

        /// <summary>Regular enemy health relative to the level-based (normal mode) health.</summary>
        public static float HealthMultiplier = 1.5f;

        /// <summary>0.9x to 1.5x in 0.05 steps, for speeds and boss stats.</summary>
        public static readonly string[] FineLadder = BuildFineLadder();
        public const string FineDefault = "1x";

        /// <summary>Regular enemy movement speed.</summary>
        public static float MoveSpeedMultiplier = 1f;

        /// <summary>Regular enemy projectile speed (ranged attacks).</summary>
        public static float ProjectileSpeedMultiplier = 1f;

        /// <summary>Regular enemy attack recharge speed: attack cooldowns are divided by this (1.25x = 0.8x cooldown).</summary>
        public static float RechargeSpeedMultiplier = 1f;

        /// <summary>Boss damage relative to vanilla hard mode.</summary>
        public static float BossDamageMultiplier = 1f;

        /// <summary>Boss health relative to vanilla hard mode.</summary>
        public static float BossHealthMultiplier = 1f;

        private static string[] BuildFineLadder()
        {
            var list = new string[13];
            for (int i = 0; i < list.Length; i++)
                list[i] = (0.9f + 0.05f * i).ToString("0.##", CultureInfo.InvariantCulture) + "x";
            return list;
        }

        public static bool IsOne(float v) => Math.Abs(v - 1f) < 0.0001f;

        /// <summary>Vanilla hard mode enemy damage multiplier (Constants.hardModeEnemyDamageMultiplier).</summary>
        private const float VanillaHardDamage = 2f;

        public static float Parse(string token, float fallback = 1.5f)
        {
            if (string.IsNullOrEmpty(token)) return fallback;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : fallback;
        }

        /// <summary>
        /// Factor applied to an attack's authoring damage before the converter doubles it, so the
        /// final value is normal damage x DamageMultiplier.
        /// </summary>
        public static float DamageFactor => DamageMultiplier / VanillaHardDamage;

        // Converters whose ConversionManager has hard mode on (server world of a hard world).
        private static readonly ConditionalWeakTable<Converter, object> HardConverters = new ConditionalWeakTable<Converter, object>();
        private static readonly object Marker = new object();

        public static void SetConverterMode(Converter converter, bool hard)
        {
            HardConverters.Remove(converter);
            if (hard) HardConverters.Add(converter, Marker);
        }

        public static bool IsHard(Converter converter) => converter != null && HardConverters.TryGetValue(converter, out _);

        /// <summary>
        /// Factor for an attack's authoring damage in a hard world, applied before the converter doubles it:
        /// regular enemies end at normal x DamageMultiplier, bosses at vanilla hard x BossDamageMultiplier.
        /// 1 means leave it alone.
        /// </summary>
        public static float DamageFactorFor(Converter converter, MonoBehaviour authoring)
        {
            if (authoring == null || !IsHard(converter)) return 1f;
            GameObject go = authoring.gameObject;
            if (!go.TryGetComponent(out EnemyAuthoring enemy) || !enemy.enabled) return 1f;
            return IsBoss(go) ? BossDamageMultiplier : DamageFactor;
        }

        /// <summary>True when this converter is running for a hard mode world and the prefab is a regular enemy.</summary>
        public static bool IsRegularInHardWorld(Converter converter, MonoBehaviour authoring)
        {
            return authoring != null && IsHard(converter) && IsRegularEnemy(authoring.gameObject);
        }

        /// <summary>Has an active EnemyAuthoring and is not a boss, boss part or boss projectile.</summary>
        public static bool IsRegularEnemy(GameObject go)
        {
            if (go == null) return false;
            if (!go.TryGetComponent(out EnemyAuthoring enemy) || !enemy.enabled) return false;
            return !IsBoss(go);
        }

        public static bool IsBoss(GameObject go)
        {
            if (go.TryGetComponent(out BossAuthoring boss) && boss.enabled) return true;
            ObjectID id = GetObjectID(go);
            if (id == ObjectID.OctopusTentacle) return true;
            return id != ObjectID.None && id.ToString().IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ObjectID GetObjectID(GameObject go)
        {
            if (go.TryGetComponent(out EntityMonoBehaviourData data) && data.objectInfo != null)
                return data.objectInfo.objectID;
            if (go.TryGetComponent(out ObjectAuthoring authoring) && !string.IsNullOrEmpty(authoring.objectName))
                return API.Authoring.GetObjectID(authoring.objectName);
            return ObjectID.None;
        }

        public static int Scale(int value, float factor) => (int)Math.Round(value * factor);
    }
}
