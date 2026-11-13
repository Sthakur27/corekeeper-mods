using System;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace StatsHud
{
    /// <summary>
    /// Damage you dealt, read from the damage numbers the game shows (EffectID.WhiteDamageNumber / CritNumber,
    /// value1 = damage after reduction, entity2 = attacker). Counted when the attacker is you, or something
    /// you own (OwnerReferenceCD: arrows, bombs, staff bolts), or, when the attacker is already gone (a projectile
    /// destroyed on impact), a target within <see cref="NearbyRange"/> tiles of you.
    ///
    /// Live DPS = damage in the last <see cref="Window"/> seconds divided by the time covered (at least 1 s, so the
    /// first hit does not read as a spike). After <see cref="Window"/> seconds without a hit the fight ends and its
    /// average (total / duration) is kept as "last".
    /// </summary>
    public static class DamageMeter
    {
        public const float Window = 5f;
        private const float NearbyRange = 15f;
        private const int Capacity = 256; // hits kept for the window; a full ring just drops the oldest

        private static readonly float[] _times = new float[Capacity];
        private static readonly int[] _amounts = new int[Capacity];
        private static int _head, _count;

        private static float _fightStart = -1f, _lastHit = -1f;
        private static long _fightTotal;

        /// <summary>Average DPS of the fight that ended last (0 before any).</summary>
        public static float LastFightDps { get; private set; }

        public static bool InFight => _lastHit >= 0f && Time.unscaledTime - _lastHit < Window;

        internal static void OnDamageNumber(Entity target, Entity attacker, int amount)
        {
            if (amount <= 0) return;
            var main = Manager.main;
            var player = main != null ? main.player : null;
            if (player == null) return;
            if (target == player.entity) return;
            if (!IsMine(player, target, attacker)) return;
            Add(amount);
        }

        private static bool IsMine(PlayerController player, Entity target, Entity attacker)
        {
            if (attacker == player.entity) return true;
            var world = Manager.ecs.ClientWorld;
            if (world == null) return false;
            if (attacker != Entity.Null && EntityUtility.EntityExists(attacker, world))
                return EntityUtility.TryGetComponentData<OwnerReferenceCD>(attacker, world, out var owner) && owner.owner == player.entity;
            // Attacker already destroyed (projectile that hit): count it if the target is near us.
            var mono = Manager.memory.GetEntityMono(target);
            if (mono == null) return false;
            Vector3 d = mono.WorldPosition - player.WorldPosition;
            d.y = 0f;
            return d.sqrMagnitude <= NearbyRange * NearbyRange;
        }

        private static void Add(int amount)
        {
            float now = Time.unscaledTime;
            if (!InFight)
            {
                _fightStart = now;
                _fightTotal = 0;
                _count = 0;
            }
            _fightTotal += amount;
            _lastHit = now;
            _times[_head] = now;
            _amounts[_head] = amount;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        /// <summary>Damage per second over the last <see cref="Window"/> seconds; also closes a finished fight.</summary>
        public static float CurrentDps()
        {
            float now = Time.unscaledTime;
            if (_lastHit < 0f) return 0f;
            if (!InFight)
            {
                if (_fightStart >= 0f)
                {
                    float duration = Mathf.Max(1f, _lastHit - _fightStart);
                    LastFightDps = _fightTotal / duration;
                    _fightStart = -1f;
                    _count = 0;
                }
                return 0f;
            }
            long sum = 0;
            float oldest = now;
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head - 1 - i + Capacity) % Capacity;
                if (now - _times[idx] > Window) break; // ring is in time order, newest first
                sum += _amounts[idx];
                oldest = _times[idx];
            }
            // Time covered: from the first counted hit (or the fight start) to now, between 1 s and the window.
            float span = Mathf.Clamp(now - Mathf.Max(oldest, _fightStart), 1f, Window);
            if (now - _fightStart >= Window) span = Window;
            return sum / span;
        }
    }

    [HarmonyPatch(typeof(EffectEventExtensions), "PlayEffect")]
    public static class DamageNumberPatch
    {
        public static void Postfix(EffectEventCD effectEvent)
        {
            try
            {
                if (effectEvent.effectID == EffectID.WhiteDamageNumber || effectEvent.effectID == EffectID.CritNumber)
                    DamageMeter.OnDamageNumber(effectEvent.entity, effectEvent.entity2, effectEvent.value1);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StatsHudMod.Name}] {e}");
            }
        }
    }
}
