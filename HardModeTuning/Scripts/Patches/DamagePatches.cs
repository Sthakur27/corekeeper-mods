using HarmonyLib;

namespace HardModeTuning.Patches
{
    /// <summary>Original authoring values, restored after the converter ran.</summary>
    public sealed class SavedDamage
    {
        public int Value;
        public float Multiplier;
    }

    // Every enemy attack converter computes damage from the authoring fields (raw value, or a
    // level formula that is linear in the multiplier field) and then doubles it in hard mode.
    // For regular enemies in a hard world each prefix scales both fields by DamageMultiplier / 2
    // so the doubled result is normal damage x DamageMultiplier; the postfix restores the
    // authoring (the same prefab is also converted for the client world and future worlds).

    [HarmonyPatch(typeof(MeleeAttackStateConverter), "Convert", new[] { typeof(MeleeAttackStateAuthoring) })]
    public static class MeleeAttackStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(MeleeAttackStateConverter __instance, MeleeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.meleeDamage, Multiplier = authoring.meleeDamageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.meleeDamage = Tuning.Scale(authoring.meleeDamage, f);
            authoring.meleeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(MeleeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.meleeDamage = __state.Value;
            authoring.meleeDamageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(BeamAttackStateConverter), "Convert", new[] { typeof(BeamAttackStateAuthoring) })]
    public static class BeamAttackStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(BeamAttackStateConverter __instance, BeamAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.damage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(BeamAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(ChargeAttackStateConverter), "Convert", new[] { typeof(ChargeAttackStateAuthoring) })]
    public static class ChargeAttackStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ChargeAttackStateConverter __instance, ChargeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.damage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ChargeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(DamageWallStateConverter), "Convert", new[] { typeof(DamageObjectStateAuthoring) })]
    public static class DamageWallStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(DamageWallStateConverter __instance, DamageObjectStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.meleeDamage, Multiplier = authoring.meleeDamageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.meleeDamage = Tuning.Scale(authoring.meleeDamage, f);
            authoring.meleeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(DamageObjectStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.meleeDamage = __state.Value;
            authoring.meleeDamageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(ExplodeOnImpactConverter), "Convert", new[] { typeof(ExplodeOnImpactAuthoring) })]
    public static class ExplodeOnImpactPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplodeOnImpactConverter __instance, ExplodeOnImpactAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.explodeDamage, Multiplier = authoring.explodeDamageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.explodeDamage = Tuning.Scale(authoring.explodeDamage, f);
            authoring.explodeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplodeOnImpactAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.explodeDamage = __state.Value;
            authoring.explodeDamageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(ExplodeStateConverter), "Convert", new[] { typeof(ExplodeStateAuthoring) })]
    public static class ExplodeStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplodeStateConverter __instance, ExplodeStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.damage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplodeStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(ExplosiveConverter), "Convert", new[] { typeof(ExplosiveAuthoring) })]
    public static class ExplosivePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplosiveConverter __instance, ExplosiveAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.damage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplosiveAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(JumpAttackStateConverter), "Convert", new[] { typeof(JumpAttackStateAuthoring) })]
    public static class JumpAttackStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(JumpAttackStateConverter __instance, JumpAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.jumpDamage, Multiplier = authoring.jumpDamageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.jumpDamage = Tuning.Scale(authoring.jumpDamage, f);
            authoring.jumpDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(JumpAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.jumpDamage = __state.Value;
            authoring.jumpDamageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(RangeAttackStateConverter), "Convert", new[] { typeof(RangeAttackStateAuthoring) })]
    public static class RangeAttackStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(RangeAttackStateConverter __instance, RangeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.rangeDamage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.rangeDamage = Tuning.Scale(authoring.rangeDamage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(RangeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.rangeDamage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(ShootMortarProjectileStateConverter), "Convert", new[] { typeof(ShootMortarProjectileStateAuthoring) })]
    public static class ShootMortarProjectileStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ShootMortarProjectileStateConverter __instance, ShootMortarProjectileStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.mortarDamage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.mortarDamage = Tuning.Scale(authoring.mortarDamage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ShootMortarProjectileStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.mortarDamage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(SnakeMovementStateConverter), "Convert", new[] { typeof(SnakeMovementStateAuthoring) })]
    public static class SnakeMovementStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(SnakeMovementStateConverter __instance, SnakeMovementStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            __state = new SavedDamage { Value = authoring.damage, Multiplier = authoring.damageMultiplier };
            float f = Tuning.DamageFactor;
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(SnakeMovementStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Value;
            authoring.damageMultiplier = __state.Multiplier;
        }
    }

    [HarmonyPatch(typeof(HealOtherEntityStateConverter), "Convert", new[] { typeof(HealOtherEntityStateAuthoring) })]
    public static class HealOtherEntityStatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(HealOtherEntityStateConverter __instance, HealOtherEntityStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            if (!Tuning.ShouldTuneDamage(__instance, authoring)) return;
            if (authoring.donCalculateHealingFromLevel) return;
            __state = new SavedDamage { Value = authoring.healPerSecond, Multiplier = authoring.healMultiplier };
            float f = Tuning.DamageFactor;
            authoring.healPerSecond = Tuning.Scale(authoring.healPerSecond, f);
            authoring.healMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(HealOtherEntityStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.healPerSecond = __state.Value;
            authoring.healMultiplier = __state.Multiplier;
        }
    }
}
