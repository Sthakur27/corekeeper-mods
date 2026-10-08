using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace StimHits
{
    /// <summary>
    /// Where the dings come from and which vanilla sounds they replace.
    ///
    /// Hit (you damaged something): the damage-number effect events (WhiteDamageNumber/CritNumber)
    /// carry the attacker in entity2: the player for melee, the projectile for bows/guns/staffs.
    /// A hit is ours when that attacker is the local player or a projectile it owns. Vanilla hit
    /// sounds muted while the Hit sound is on: the creature's take-damage sound, the melee impact
    /// sound (EffectID.HitDamageSound) and the death/impact sound of our projectiles that hit.
    ///
    /// Kill: EntityMonoBehaviour.OnDeath of a creature we hit in the last <see cref="Memory"/> seconds.
    /// The creature's own death sound stays vanilla.
    ///
    /// Hurt (you got damaged): EntityMonoBehaviour.OnTakeDamage on the local player.
    ///
    /// Muting works by a flag checked in a prefix on AudioManager.PlayAudioClip, the private funnel
    /// every Sfx*/SfxTable call ends in; finalizers always clear the flag.
    /// </summary>
    internal static class Hits
    {
        internal static bool Muting;

        // Recently hit by us: targets (to mute their take-damage sound even beyond the range) and
        // projectiles (to mute their impact sound when they die on the hit).
        private static readonly Dictionary<Entity, float> RecentTargets = new Dictionary<Entity, float>();
        private static readonly Dictionary<Entity, float> HitProjectiles = new Dictionary<Entity, float>();
        private const float Memory = 1.5f;
        private static int _logged;

        // Objects show no damage numbers and OnTakeDamage carries no attacker, so object dings only
        // count while we're attacking: attack/use button held now or within AttackWindow seconds
        // (arrows still in flight). Keeps drills, explosions and other players from dinging.
        private static float _lastAttackInput = -100f;
        private const float AttackWindow = 1f;
        private const uint AttackButtons = (uint)(CommandInputButtonStateNames.Interact_HeldDown
            | CommandInputButtonStateNames.SecondInteract_HeldDown | CommandInputButtonStateNames.UseOffHand_HeldDown);

        internal static PlayerController LocalPlayer
        {
            get { var m = Manager.main; return m != null ? m.player : null; }
        }

        internal static bool Enabled(SoundBank.Kind kind)
        {
            var s = StimHitsMod.Slot(kind).Sound;
            return s != null && s.Value != "Off";
        }

        internal static bool InRange(Vector3 pos, PlayerController player)
        {
            var range = StimHitsMod.HitRange != null ? StimHitsMod.HitRange.Value : 16;
            var d = pos - player.WorldPosition;
            d.y = 0f;
            return d.sqrMagnitude <= range * range;
        }

        /// <summary>Called every frame: remembers when the local player last held an attack button.</summary>
        internal static void TrackAttackInput()
        {
            var player = LocalPlayer;
            if (player == null) return;
            var world = Manager.ecs.ClientWorld;
            if (world == null || !EntityUtility.EntityExists(player.entity, world)) return;
            if (EntityUtility.TryGetComponentData<ClientInput>(player.entity, world, out var input)
                && (input.buttonSetMask & AttackButtons) != 0)
                _lastAttackInput = Time.unscaledTime;
        }

        internal static bool Attacking => Time.unscaledTime - _lastAttackInput < AttackWindow;

        internal static bool Recent(Dictionary<Entity, float> map, Entity e)
            => map.TryGetValue(e, out var t) && Time.unscaledTime - t < Memory;

        internal static bool RecentTarget(Entity e) => Recent(RecentTargets, e);
        internal static bool HitProjectile(Entity e) => Recent(HitProjectiles, e);

        /// <summary>A damage number appeared on <paramref name="target"/>; ding if we dealt it.</summary>
        internal static void OnDamageNumber(Entity target, Entity attacker)
        {
            var player = LocalPlayer;
            if (player == null || !Enabled(SoundBank.Kind.Hit)) return;
            var world = Manager.ecs.ClientWorld;
            var targetMono = Manager.memory.GetEntityMono(target);
            if (targetMono == null || targetMono == player) return;

            string why = null;
            if (attacker == player.entity) why = "melee";
            else if (attacker != Entity.Null && EntityUtility.EntityExists(attacker, world))
            {
                if (EntityUtility.HasComponentData<ProjectileCD>(attacker, world)
                    && EntityUtility.TryGetComponentData<OwnerReferenceCD>(attacker, world, out var owner)
                    && owner.owner == player.entity)
                {
                    why = "projectile";
                    HitProjectiles[attacker] = Time.unscaledTime;
                }
            }
            else if (InRange(targetMono.WorldPosition, player)) why = "nearby (attacker gone)";
            if (why == null) return;

            RecentTargets[target] = Time.unscaledTime;
            if (RecentTargets.Count > 256) Prune();
            if (_logged < 5)
            {
                _logged++;
                Debug.Log($"[{StimHitsMod.Name}] hit ding ({why}) on {targetMono.name}");
            }
            SoundBank.Play(SoundBank.Kind.Hit, targetMono.transform);
        }

        private static void Prune()
        {
            var now = Time.unscaledTime;
            foreach (var map in new[] { RecentTargets, HitProjectiles })
            {
                var old = new List<Entity>();
                foreach (var kv in map) if (now - kv.Value >= Memory) old.Add(kv.Key);
                foreach (var e in old) map.Remove(e);
            }
        }
    }

    [HarmonyPatch(typeof(EffectEventExtensions), "PlayEffect")]
    public static class PlayEffectPatch
    {
        public static bool Prefix(EffectEventCD effectEvent)
        {
            try
            {
                switch (effectEvent.effectID)
                {
                    case EffectID.WhiteDamageNumber:
                    case EffectID.CritNumber:
                        Hits.OnDamageNumber(effectEvent.entity, effectEvent.entity2);
                        break;
                    case EffectID.HitDamageSound:
                        // Melee impact sound at the hit position. Skip it near us when Hit dings are on.
                        var player = Hits.LocalPlayer;
                        if (player != null && Hits.Enabled(SoundBank.Kind.Hit))
                        {
                            Vector3 p = effectEvent.position1;
                            var d = p - player.WorldPosition;
                            d.y = 0f;
                            if (d.sqrMagnitude <= 16f) return false;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] {e}");
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(EntityMonoBehaviour), "OnTakeDamage")]
    public static class OnTakeDamagePatch
    {
        /// <summary>__state: -1 vanilla, 0 mute only, 1 mute + hurt ding, 2 mute + hit ding (objects).</summary>
        public static void Prefix(EntityMonoBehaviour __instance, out int __state)
        {
            __state = -1;
            Hits.Muting = false;
            try
            {
                __state = Decide(__instance);
                Hits.Muting = __state >= 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] {e}");
            }
        }

        public static Exception Finalizer(EntityMonoBehaviour __instance, int __state, Exception __exception)
        {
            Hits.Muting = false;
            try
            {
                if (__state == 1) SoundBank.Play(SoundBank.Kind.Hurt, __instance.transform);
                else if (__state == 2) SoundBank.Play(SoundBank.Kind.Hit, __instance.transform);
            }
            catch (Exception e) { Debug.LogWarning($"[{StimHitsMod.Name}] {e}"); }
            return __exception;
        }

        private static int Decide(EntityMonoBehaviour entity)
        {
            var player = Hits.LocalPlayer;
            if (player == null || entity == null) return -1;
            if (entity == player) return Hits.Enabled(SoundBank.Kind.Hurt) ? 1 : -1;
            if (entity is PlayerController || !Hits.Enabled(SoundBank.Kind.Hit)) return -1;

            var info = entity.objectInfo;
            if (info == null) return -1;
            var type = info.objectType;
            var creature = type == ObjectType.Creature || type == ObjectType.TrainingDummy || type == ObjectType.Critter;
            var near = Hits.InRange(entity.WorldPosition, player);
            if (creature)
                // The ding itself comes from the damage number; here we only silence the vanilla sound.
                return near || Hits.RecentTarget(entity.entity) ? 0 : -1;
            // Objects show no damage numbers, so they ding here (proximity) when enabled, but only
            // while we're attacking: a drill mining ore next to us is not our hit.
            return StimHitsMod.HitObjects != null && StimHitsMod.HitObjects.Value && near && Hits.Attacking ? 2 : -1;
        }
    }

    /// <summary>
    /// Silences the impact/death sound of our projectiles that just hit something, and plays the
    /// kill sound when a creature we recently hit dies.
    /// </summary>
    [HarmonyPatch(typeof(EntityMonoBehaviour), "OnDeath")]
    public static class OnDeathPatch
    {
        public static void Prefix(EntityMonoBehaviour __instance, out bool __state)
        {
            __state = false;
            Hits.Muting = false;
            try
            {
                if (__instance == null) return;
                var e = __instance.entity;
                Hits.Muting = Hits.Enabled(SoundBank.Kind.Hit) && Hits.HitProjectile(e);
                __state = Hits.Enabled(SoundBank.Kind.Kill) && Hits.RecentTarget(e) && !(__instance is PlayerController);
            }
            catch (Exception ex) { Debug.LogWarning($"[{StimHitsMod.Name}] {ex}"); }
        }

        public static Exception Finalizer(EntityMonoBehaviour __instance, bool __state, Exception __exception)
        {
            Hits.Muting = false;
            if (__state)
            {
                try { SoundBank.Play(SoundBank.Kind.Kill, __instance.transform); }
                catch (Exception ex) { Debug.LogWarning($"[{StimHitsMod.Name}] {ex}"); }
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(AudioManager), "PlayAudioClip")]
    public static class PlayAudioClipPatch
    {
        public static bool Prefix() => !Hits.Muting;
    }
}
