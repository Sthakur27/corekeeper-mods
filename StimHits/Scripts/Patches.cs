using System;
using HarmonyLib;
using UnityEngine;

namespace StimHits
{
    /// <summary>
    /// OnTakeDamage prefix decides whether this damage reaction gets a ding; if so, every vanilla
    /// sound started inside the method (take-damage sfx, OnTakeDamage sound triggers, shield block)
    /// is muted at AudioManager.PlayAudioClip, the single private funnel all Sfx* calls end in.
    /// The finalizer always clears the flag (even if the method throws) and then plays the ding.
    /// </summary>
    [HarmonyPatch(typeof(EntityMonoBehaviour), "OnTakeDamage")]
    public static class OnTakeDamagePatch
    {
        internal static bool Muting;

        public static void Prefix(EntityMonoBehaviour __instance, out int __state)
        {
            __state = -1;
            Muting = false;
            try
            {
                var kind = Classify(__instance);
                if (kind == null) return;
                var setting = kind == SoundBank.Kind.Hit ? StimHitsMod.HitSound : StimHitsMod.HurtSound;
                if (setting == null || setting.Value == "Off") return;
                __state = (int)kind.Value;
                Muting = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] {e}");
            }
        }

        public static Exception Finalizer(EntityMonoBehaviour __instance, int __state, Exception __exception)
        {
            Muting = false;
            if (__state >= 0)
            {
                try { SoundBank.Play((SoundBank.Kind)__state, __instance.transform); }
                catch (Exception e) { Debug.LogWarning($"[{StimHitsMod.Name}] {e}"); }
            }
            return __exception;
        }

        /// <summary>Hurt = the local player; Hit = a creature (or object, if enabled) near the local player.</summary>
        private static SoundBank.Kind? Classify(EntityMonoBehaviour entity)
        {
            var main = Manager.main;
            var player = main != null ? main.player : null;
            if (player == null || entity == null) return null;
            if (entity == player) return SoundBank.Kind.Hurt;
            if (entity is PlayerController) return null; // other players getting hit: leave vanilla

            var info = entity.objectInfo;
            if (info == null) return null;
            var type = info.objectType;
            var creature = type == ObjectType.Creature || type == ObjectType.TrainingDummy || type == ObjectType.Critter;
            if (!creature && !(StimHitsMod.HitObjects != null && StimHitsMod.HitObjects.Value)) return null;

            var range = StimHitsMod.HitRange != null ? StimHitsMod.HitRange.Value : 10;
            var d = entity.WorldPosition - player.WorldPosition;
            d.y = 0f;
            return d.sqrMagnitude <= range * range ? SoundBank.Kind.Hit : (SoundBank.Kind?)null;
        }
    }

    [HarmonyPatch(typeof(AudioManager), "PlayAudioClip")]
    public static class PlayAudioClipPatch
    {
        public static bool Prefix() => !OnTakeDamagePatch.Muting;
    }
}
