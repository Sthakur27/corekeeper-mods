using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Pug.Conversion;
using PugMod;
using UnityEngine;

namespace DifficultyTuning
{
    /// <summary>One set of multipliers (hard mode worlds or normal worlds).</summary>
    public sealed class Profile
    {
        public readonly string Name;
        public readonly bool Hard;

        /// <summary>Regular enemy damage relative to normal mode (vanilla hard = 2).</summary>
        public float Damage;
        /// <summary>Regular enemy health relative to the level-based (normal mode) health.</summary>
        public float Health;
        /// <summary>Regular enemy movement speed.</summary>
        public float MoveSpeed = 1f;
        /// <summary>Regular enemy projectile speed (ranged attacks).</summary>
        public float ProjectileSpeed = 1f;
        /// <summary>Regular enemy attack recharge speed: cooldowns are divided by this (1.25x = 0.8x cooldown).</summary>
        public float RechargeSpeed = 1f;
        /// <summary>Boss damage relative to vanilla for this mode.</summary>
        public float BossDamage = 1f;
        /// <summary>Boss health relative to vanilla for this mode.</summary>
        public float BossHealth = 1f;

        public Profile(string name, bool hard, float damage, float health)
        {
            Name = name;
            Hard = hard;
            Damage = damage;
            Health = health;
        }

        /// <summary>Factor for a regular enemy's authoring damage (hard: before the converter doubles it).</summary>
        public float RegularDamageFactor => Hard ? Damage / Tuning.VanillaHardDamage : Damage;
    }

    /// <summary>Settings plus the "should this prefab be tuned" rules shared by the patches.</summary>
    public static class Tuning
    {
        /// <summary>Hard mode regular enemy damage/health ladder (relative to normal mode).</summary>
        public static readonly string[] Ladder = { "1x", "1.25x", "1.5x", "1.75x", "2x" };
        public const string DefaultToken = "1.5x";

        /// <summary>Normal mode regular enemy damage/health ladder (relative to vanilla normal).</summary>
        public static readonly string[] NormalLadder = { "0.5x", "0.75x", "1x", "1.25x", "1.5x", "1.75x", "2x" };
        public const string NormalDefaultToken = "1x";

        /// <summary>Hard mode worlds: defaults tone regular enemy damage down to 1.5x normal, bosses vanilla hard.</summary>
        public static readonly Profile Hard = new Profile("Hard mode", true, 1.5f, 1.5f);

        /// <summary>Normal (non-hard) worlds: everything 1x = vanilla by default.</summary>
        public static readonly Profile Normal = new Profile("Normal mode", false, 1f, 1f);

        /// <summary>0.9x to 1.5x in 0.05 steps, for speeds and boss stats.</summary>
        public static readonly string[] FineLadder = BuildFineLadder();
        public const string FineDefault = "1x";

        /// <summary>The fine ladder plus 1.75x, 2x, 2.5x, 3x, for the speed settings (big values make them easy to see).</summary>
        public static readonly string[] SpeedLadder = BuildSpeedLadder();

        private static string[] BuildFineLadder()
        {
            var list = new string[13];
            for (int i = 0; i < list.Length; i++)
                list[i] = (0.9f + 0.05f * i).ToString("0.##", CultureInfo.InvariantCulture) + "x";
            return list;
        }

        private static string[] BuildSpeedLadder()
        {
            var fine = BuildFineLadder();
            var list = new string[fine.Length + 4];
            fine.CopyTo(list, 0);
            list[fine.Length] = "1.75x";
            list[fine.Length + 1] = "2x";
            list[fine.Length + 2] = "2.5x";
            list[fine.Length + 3] = "3x";
            return list;
        }

        public static bool IsOne(float v) => Math.Abs(v - 1f) < 0.0001f;

        /// <summary>Vanilla hard mode enemy damage multiplier (Constants.hardModeEnemyDamageMultiplier).</summary>
        internal const float VanillaHardDamage = 2f;

        public static float Parse(string token, float fallback = 1.5f)
        {
            if (string.IsNullOrEmpty(token)) return fallback;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : fallback;
        }

        // Profile of the world each converter is converting for: Hard for the server world of a hard
        // world, Normal for the server world of any other world. Client and startup conversions
        // (not server) are never tuned; enemy stats are server authoritative.
        private static readonly ConditionalWeakTable<Converter, Profile> ConverterProfiles = new ConditionalWeakTable<Converter, Profile>();

        public static void SetConverterMode(Converter converter, bool isServer, bool hard)
        {
            ConverterProfiles.Remove(converter);
            if (hard) ConverterProfiles.Add(converter, Hard);
            else if (isServer) ConverterProfiles.Add(converter, Normal);
        }

        /// <summary>The profile for this converter's world, or null when nothing should be tuned.</summary>
        public static Profile ProfileFor(Converter converter) =>
            converter != null && ConverterProfiles.TryGetValue(converter, out Profile p) ? p : null;

        /// <summary>Set by the health/loot converter patches while ComputeMaxHealth runs for them.</summary>
        [ThreadStatic] public static Profile CurrentHealthProfile;

        /// <summary>
        /// Factor for an attack's authoring damage. Hard worlds: applied before the converter doubles it, so
        /// regular enemies end at normal x Damage and bosses at vanilla hard x BossDamage. Normal worlds:
        /// regular enemies normal x Damage, bosses normal x BossDamage. 1 means leave it alone.
        /// </summary>
        public static float DamageFactorFor(Converter converter, MonoBehaviour authoring)
        {
            Profile p = ProfileFor(converter);
            if (authoring == null || p == null) return 1f;
            GameObject go = authoring.gameObject;
            if (!go.TryGetComponent(out EnemyAuthoring enemy) || !enemy.enabled) return 1f;
            return IsBoss(go) ? p.BossDamage : p.RegularDamageFactor;
        }

        /// <summary>The world's profile when this prefab is a regular enemy converted for a tuned world, else null.</summary>
        public static Profile RegularProfile(Converter converter, MonoBehaviour authoring)
        {
            Profile p = ProfileFor(converter);
            return authoring != null && p != null && IsRegularEnemy(authoring.gameObject) ? p : null;
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

        // ------------------------------------------------------------------ move speed curve

        /// <summary>Share of the speed boost the fastest regular enemy keeps (0.1: at 2x it gets 1.1x).</summary>
        public const float FastestBoostShare = 0.1f;

        private static bool _speedsRead;
        private static float _referenceSpeed;   // median base speed of regular enemies
        private static float _maxSpeed;         // fastest regular enemy
        private static float _curveExponent;    // k in (reference / base)^k

        /// <summary>
        /// Move speed boost that tapers off for fast enemies:
        ///   new = base * (1 + (m - 1) * share),  share = 1 at or below the median base speed,
        ///   share = (median / base)^k above it, with k chosen so the fastest regular enemy keeps
        ///   FastestBoostShare of the boost (at 2x: median and slower 2x, fastest 1.1x).
        /// Slow-downs (m below 1) stay a plain multiplier. Falls back to a plain multiplier if the
        /// enemy speeds could not be read.
        /// </summary>
        public static float ScaledMoveSpeed(float baseSpeed, float m)
        {
            if (!_speedsRead) ReadEnemySpeeds();
            if (m <= 1f || _curveExponent <= 0f || baseSpeed <= _referenceSpeed) return baseSpeed * m;
            float share = (float)Math.Pow(_referenceSpeed / baseSpeed, _curveExponent);
            if (share < FastestBoostShare) share = FastestBoostShare; // enemies faster than the scanned max
            return baseSpeed * (1f + (m - 1f) * share);
        }

        private static void ReadEnemySpeeds()
        {
            _speedsRead = true;
            var speeds = new List<(float speed, string name)>();
            try
            {
                // The authoring components of every object prefab (what the converters run on).
                var monos = PugDatabase.entityMonobehaviours;
                if (monos != null)
                {
                    var seen = new HashSet<GameObject>();
                    foreach (var data in monos)
                    {
                        if (!(data is MonoBehaviour mb) || mb == null) continue;
                        GameObject go = mb.gameObject;
                        if (go == null || !seen.Add(go) || !IsRegularEnemy(go)) continue;
                        if (!go.TryGetComponent(out MovementSpeedAuthoring move) || move.speed <= 0f) continue;
                        string name = data.ObjectInfo != null ? data.ObjectInfo.objectID.ToString() : go.name;
                        speeds.Add((move.speed, name));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{DifficultyTuningMod.Name}] Could not read enemy base speeds: {e.Message}");
            }
            if (speeds.Count < 2)
            {
                Debug.LogWarning($"[{DifficultyTuningMod.Name}] Found {speeds.Count} enemy base speeds; move speed setting falls back to a plain multiplier.");
                return;
            }
            speeds.Sort((a, b) => a.speed.CompareTo(b.speed));
            _referenceSpeed = speeds.Count % 2 == 1
                ? speeds[speeds.Count / 2].speed
                : (speeds[speeds.Count / 2 - 1].speed + speeds[speeds.Count / 2].speed) / 2f;
            _maxSpeed = speeds[speeds.Count - 1].speed;
            if (_maxSpeed > _referenceSpeed * 1.01f)
                _curveExponent = (float)(Math.Log(1.0 / FastestBoostShare) / Math.Log(_maxSpeed / _referenceSpeed));

            var parts = new List<string>();
            foreach (var sp in speeds) parts.Add($"{sp.name}={sp.speed:0.##}");
            Debug.Log($"[{DifficultyTuningMod.Name}] Move speed curve: {speeds.Count} regular enemies, median {_referenceSpeed:0.##}, " +
                      $"fastest {_maxSpeed:0.##}, exponent {_curveExponent:0.##} (at 2x: median 2x, fastest {1f + FastestBoostShare:0.##}x). " +
                      $"Base speeds: {string.Join(", ", parts)}");
        }
    }
}
