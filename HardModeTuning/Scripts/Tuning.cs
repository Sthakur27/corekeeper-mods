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

        /// <summary>Vanilla hard mode enemy damage multiplier (Constants.hardModeEnemyDamageMultiplier).</summary>
        private const float VanillaHardDamage = 2f;

        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1.5f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1.5f;
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

        /// <summary>True when this converter is running for a hard mode world and the prefab is a regular enemy.</summary>
        public static bool ShouldTuneDamage(Converter converter, MonoBehaviour authoring)
        {
            if (converter == null || authoring == null) return false;
            if (!HardConverters.TryGetValue(converter, out _)) return false;
            if (Math.Abs(DamageFactor - 1f) < 0.0001f) return false;
            return IsRegularEnemy(authoring.gameObject);
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
