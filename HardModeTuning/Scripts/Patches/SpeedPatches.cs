using HarmonyLib;

namespace HardModeTuning.Patches
{
    /// <summary>Original authoring floats, restored after the converter ran.</summary>
    public sealed class SavedFloats
    {
        public float A, B, C;
    }

    // Speed settings, regular enemies in hard worlds only. Same pattern as the damage patches: scale the
    // authoring fields the converter copies, then restore them. Vanilla never changes these per mode.

    /// <summary>Movement speed (MovementSpeedCD.originalSpeed; slows, enrage etc. multiply on top).</summary>
    [HarmonyPatch(typeof(MovementSpeedConverter), "Convert", new[] { typeof(MovementSpeedAuthoring) })]
    public static class MovementSpeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MovementSpeedConverter __instance, MovementSpeedAuthoring authoring, out SavedFloats __state)
        {
            __state = null;
            if (Tuning.IsOne(Tuning.MoveSpeedMultiplier) || !Tuning.IsRegularInHardWorld(__instance, authoring)) return;
            __state = new SavedFloats { A = authoring.speed };
            authoring.speed *= Tuning.MoveSpeedMultiplier;
        }

        [HarmonyPostfix]
        public static void Postfix(MovementSpeedAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.speed = __state.A;
        }
    }

    /// <summary>Ranged attacks: projectile speed (RangeAttackStateCD.speedMultiplier) and cooldown.</summary>
    [HarmonyPatch(typeof(RangeAttackStateConverter), "Convert", new[] { typeof(RangeAttackStateAuthoring) })]
    public static class RangeAttackSpeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(RangeAttackStateConverter __instance, RangeAttackStateAuthoring authoring, out SavedFloats __state)
        {
            __state = null;
            if (Tuning.IsOne(Tuning.ProjectileSpeedMultiplier) && Tuning.IsOne(Tuning.RechargeSpeedMultiplier)) return;
            if (!Tuning.IsRegularInHardWorld(__instance, authoring)) return;
            __state = new SavedFloats { A = authoring.speedMultiplier, B = authoring.minCooldown, C = authoring.maxCooldown };
            authoring.speedMultiplier *= Tuning.ProjectileSpeedMultiplier;
            authoring.minCooldown /= Tuning.RechargeSpeedMultiplier;
            authoring.maxCooldown /= Tuning.RechargeSpeedMultiplier;
        }

        [HarmonyPostfix]
        public static void Postfix(RangeAttackStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.speedMultiplier = __state.A;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }

    /// <summary>Shared cooldown scaling for the other attack states (recharge speed setting).</summary>
    public static class Cooldowns
    {
        public static SavedFloats Scale(Pug.Conversion.Converter converter, UnityEngine.MonoBehaviour authoring, ref float min, ref float max)
        {
            if (Tuning.IsOne(Tuning.RechargeSpeedMultiplier) || !Tuning.IsRegularInHardWorld(converter, authoring)) return null;
            var saved = new SavedFloats { B = min, C = max };
            min /= Tuning.RechargeSpeedMultiplier;
            max /= Tuning.RechargeSpeedMultiplier;
            return saved;
        }
    }

    [HarmonyPatch(typeof(MeleeAttackStateConverter), "Convert", new[] { typeof(MeleeAttackStateAuthoring) })]
    public static class MeleeAttackCooldownPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MeleeAttackStateConverter __instance, MeleeAttackStateAuthoring authoring, out SavedFloats __state)
            => __state = Cooldowns.Scale(__instance, authoring, ref authoring.minCooldown, ref authoring.maxCooldown);

        [HarmonyPostfix]
        public static void Postfix(MeleeAttackStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }

    [HarmonyPatch(typeof(BeamAttackStateConverter), "Convert", new[] { typeof(BeamAttackStateAuthoring) })]
    public static class BeamAttackCooldownPatch
    {
        [HarmonyPrefix]
        public static void Prefix(BeamAttackStateConverter __instance, BeamAttackStateAuthoring authoring, out SavedFloats __state)
            => __state = Cooldowns.Scale(__instance, authoring, ref authoring.minCooldown, ref authoring.maxCooldown);

        [HarmonyPostfix]
        public static void Postfix(BeamAttackStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }

    [HarmonyPatch(typeof(ChargeAttackStateConverter), "Convert", new[] { typeof(ChargeAttackStateAuthoring) })]
    public static class ChargeAttackCooldownPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ChargeAttackStateConverter __instance, ChargeAttackStateAuthoring authoring, out SavedFloats __state)
            => __state = Cooldowns.Scale(__instance, authoring, ref authoring.minCooldown, ref authoring.maxCooldown);

        [HarmonyPostfix]
        public static void Postfix(ChargeAttackStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }

    [HarmonyPatch(typeof(JumpAttackStateConverter), "Convert", new[] { typeof(JumpAttackStateAuthoring) })]
    public static class JumpAttackCooldownPatch
    {
        [HarmonyPrefix]
        public static void Prefix(JumpAttackStateConverter __instance, JumpAttackStateAuthoring authoring, out SavedFloats __state)
            => __state = Cooldowns.Scale(__instance, authoring, ref authoring.minCooldown, ref authoring.maxCooldown);

        [HarmonyPostfix]
        public static void Postfix(JumpAttackStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }

    [HarmonyPatch(typeof(ShootMortarProjectileStateConverter), "Convert", new[] { typeof(ShootMortarProjectileStateAuthoring) })]
    public static class ShootMortarCooldownPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ShootMortarProjectileStateConverter __instance, ShootMortarProjectileStateAuthoring authoring, out SavedFloats __state)
            => __state = Cooldowns.Scale(__instance, authoring, ref authoring.minCooldown, ref authoring.maxCooldown);

        [HarmonyPostfix]
        public static void Postfix(ShootMortarProjectileStateAuthoring authoring, SavedFloats __state)
        {
            if (__state == null) return;
            authoring.minCooldown = __state.B;
            authoring.maxCooldown = __state.C;
        }
    }
}
