using HarmonyLib;

namespace DifficultyTuning.Patches
{
    /// <summary>Original authoring values, restored after the converter ran.</summary>
    public sealed class SavedDamage
    {
        public int[] Values;
        public float[] Multipliers;
    }

    // Every enemy attack converter computes damage from the authoring fields (raw value, or a
    // level formula that is linear in the multiplier field) and then doubles it in hard mode.
    // In a hard world each prefix scales both fields by Tuning.DamageFactorFor (regular enemies:
    // DamageMultiplier / 2, so the doubled result is normal damage x DamageMultiplier; bosses:
    // BossDamageMultiplier on top of vanilla hard); the postfix restores the authoring (the same
    // prefab is also converted for the client world and future worlds).
    // Generated: one class per converter.

    [HarmonyPatch(typeof(MeleeAttackStateConverter), "Convert", new[] { typeof(MeleeAttackStateAuthoring) })]
    public static class MeleeAttackStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(MeleeAttackStateConverter __instance, MeleeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.meleeDamage }, Multipliers = new[] { authoring.meleeDamageMultiplier } };
            authoring.meleeDamage = Tuning.Scale(authoring.meleeDamage, f);
            authoring.meleeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(MeleeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.meleeDamage = __state.Values[0];
            authoring.meleeDamageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(BeamAttackStateConverter), "Convert", new[] { typeof(BeamAttackStateAuthoring) })]
    public static class BeamAttackStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(BeamAttackStateConverter __instance, BeamAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(BeamAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ChargeAttackStateConverter), "Convert", new[] { typeof(ChargeAttackStateAuthoring) })]
    public static class ChargeAttackStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ChargeAttackStateConverter __instance, ChargeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ChargeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(DamageWallStateConverter), "Convert", new[] { typeof(DamageObjectStateAuthoring) })]
    public static class DamageWallStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(DamageWallStateConverter __instance, DamageObjectStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.meleeDamage }, Multipliers = new[] { authoring.meleeDamageMultiplier } };
            authoring.meleeDamage = Tuning.Scale(authoring.meleeDamage, f);
            authoring.meleeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(DamageObjectStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.meleeDamage = __state.Values[0];
            authoring.meleeDamageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ExplodeOnImpactConverter), "Convert", new[] { typeof(ExplodeOnImpactAuthoring) })]
    public static class ExplodeOnImpactDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplodeOnImpactConverter __instance, ExplodeOnImpactAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.explodeDamage }, Multipliers = new[] { authoring.explodeDamageMultiplier } };
            authoring.explodeDamage = Tuning.Scale(authoring.explodeDamage, f);
            authoring.explodeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplodeOnImpactAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.explodeDamage = __state.Values[0];
            authoring.explodeDamageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ExplodeStateConverter), "Convert", new[] { typeof(ExplodeStateAuthoring) })]
    public static class ExplodeStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplodeStateConverter __instance, ExplodeStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplodeStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ExplosiveConverter), "Convert", new[] { typeof(ExplosiveAuthoring) })]
    public static class ExplosiveDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ExplosiveConverter __instance, ExplosiveAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ExplosiveAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(JumpAttackStateConverter), "Convert", new[] { typeof(JumpAttackStateAuthoring) })]
    public static class JumpAttackStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(JumpAttackStateConverter __instance, JumpAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.jumpDamage }, Multipliers = new[] { authoring.jumpDamageMultiplier } };
            authoring.jumpDamage = Tuning.Scale(authoring.jumpDamage, f);
            authoring.jumpDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(JumpAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.jumpDamage = __state.Values[0];
            authoring.jumpDamageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(RangeAttackStateConverter), "Convert", new[] { typeof(RangeAttackStateAuthoring) })]
    public static class RangeAttackStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(RangeAttackStateConverter __instance, RangeAttackStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.rangeDamage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.rangeDamage = Tuning.Scale(authoring.rangeDamage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(RangeAttackStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.rangeDamage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ShootMortarProjectileStateConverter), "Convert", new[] { typeof(ShootMortarProjectileStateAuthoring) })]
    public static class ShootMortarProjectileStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ShootMortarProjectileStateConverter __instance, ShootMortarProjectileStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.mortarDamage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.mortarDamage = Tuning.Scale(authoring.mortarDamage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ShootMortarProjectileStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.mortarDamage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(SnakeMovementStateConverter), "Convert", new[] { typeof(SnakeMovementStateAuthoring) })]
    public static class SnakeMovementStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(SnakeMovementStateConverter __instance, SnakeMovementStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(SnakeMovementStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(HealOtherEntityStateConverter), "Convert", new[] { typeof(HealOtherEntityStateAuthoring) })]
    public static class HealOtherEntityStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(HealOtherEntityStateConverter __instance, HealOtherEntityStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            if (authoring.donCalculateHealingFromLevel || Tuning.IsBoss(authoring.gameObject)) return;
            __state = new SavedDamage { Values = new[] { authoring.healPerSecond }, Multipliers = new[] { authoring.healMultiplier } };
            authoring.healPerSecond = Tuning.Scale(authoring.healPerSecond, f);
            authoring.healMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(HealOtherEntityStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.healPerSecond = __state.Values[0];
            authoring.healMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(CoreBossConverter), "Convert", new[] { typeof(CoreBossAuthoring) })]
    public static class CoreBossDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(CoreBossConverter __instance, CoreBossAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.whirlwindProjectileDamage, authoring.homingTriangleProjectileDamage }, Multipliers = new[] { authoring.whirlwindProjectileDamageMultiplier, authoring.homingTriangleProjectileDamageMultiplier } };
            authoring.whirlwindProjectileDamage = Tuning.Scale(authoring.whirlwindProjectileDamage, f);
            authoring.whirlwindProjectileDamageMultiplier *= f;
            authoring.homingTriangleProjectileDamage = Tuning.Scale(authoring.homingTriangleProjectileDamage, f);
            authoring.homingTriangleProjectileDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(CoreBossAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.whirlwindProjectileDamage = __state.Values[0];
            authoring.whirlwindProjectileDamageMultiplier = __state.Multipliers[0];
            authoring.homingTriangleProjectileDamage = __state.Values[1];
            authoring.homingTriangleProjectileDamageMultiplier = __state.Multipliers[1];
        }
    }

    [HarmonyPatch(typeof(GiantCicadaBossConverter), "Convert", new[] { typeof(GiantCicadaBossAuthoring) })]
    public static class GiantCicadaBossDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(GiantCicadaBossConverter __instance, GiantCicadaBossAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.armSlamDamage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.armSlamDamage = Tuning.Scale(authoring.armSlamDamage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(GiantCicadaBossAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.armSlamDamage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(HydraBossConverter), "Convert", new[] { typeof(HydraBossAuthoring) })]
    public static class HydraBossDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(HydraBossConverter __instance, HydraBossAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.buriedAppearDamage, authoring.beamDamage, authoring.stalactiteMortarDamage, authoring.shockwaveDamage, authoring.iceShardMortarDamage, authoring.lavaMortarDamage, authoring.nilipedeMortarDamage }, Multipliers = new[] { authoring.buriedAppearDamageMultiplier, authoring.beamDamageMultiplier, authoring.stalactiteMortarDamageMultiplier, authoring.shockwaveDamageMultiplier, authoring.iceShardMortarDamageMultiplier, authoring.lavaMortarDamageMultiplier, authoring.nilipedeMortarDamageMultiplier } };
            authoring.buriedAppearDamage = Tuning.Scale(authoring.buriedAppearDamage, f);
            authoring.buriedAppearDamageMultiplier *= f;
            authoring.beamDamage = Tuning.Scale(authoring.beamDamage, f);
            authoring.beamDamageMultiplier *= f;
            authoring.stalactiteMortarDamage = Tuning.Scale(authoring.stalactiteMortarDamage, f);
            authoring.stalactiteMortarDamageMultiplier *= f;
            authoring.shockwaveDamage = Tuning.Scale(authoring.shockwaveDamage, f);
            authoring.shockwaveDamageMultiplier *= f;
            authoring.iceShardMortarDamage = Tuning.Scale(authoring.iceShardMortarDamage, f);
            authoring.iceShardMortarDamageMultiplier *= f;
            authoring.lavaMortarDamage = Tuning.Scale(authoring.lavaMortarDamage, f);
            authoring.lavaMortarDamageMultiplier *= f;
            authoring.nilipedeMortarDamage = Tuning.Scale(authoring.nilipedeMortarDamage, f);
            authoring.nilipedeMortarDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(HydraBossAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.buriedAppearDamage = __state.Values[0];
            authoring.buriedAppearDamageMultiplier = __state.Multipliers[0];
            authoring.beamDamage = __state.Values[1];
            authoring.beamDamageMultiplier = __state.Multipliers[1];
            authoring.stalactiteMortarDamage = __state.Values[2];
            authoring.stalactiteMortarDamageMultiplier = __state.Multipliers[2];
            authoring.shockwaveDamage = __state.Values[3];
            authoring.shockwaveDamageMultiplier = __state.Multipliers[3];
            authoring.iceShardMortarDamage = __state.Values[4];
            authoring.iceShardMortarDamageMultiplier = __state.Multipliers[4];
            authoring.lavaMortarDamage = __state.Values[5];
            authoring.lavaMortarDamageMultiplier = __state.Multipliers[5];
            authoring.nilipedeMortarDamage = __state.Values[6];
            authoring.nilipedeMortarDamageMultiplier = __state.Multipliers[6];
        }
    }

    [HarmonyPatch(typeof(LarvaBossConverter), "Convert", new[] { typeof(BossLarvaAuthoring) })]
    public static class LarvaBossDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(LarvaBossConverter __instance, BossLarvaAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(BossLarvaAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(ScarabBossConverter), "Convert", new[] { typeof(ScarabBossAuthoring) })]
    public static class ScarabBossDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ScarabBossConverter __instance, ScarabBossAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.chargeDamage }, Multipliers = new[] { authoring.chargeDamageMultiplier } };
            authoring.chargeDamage = Tuning.Scale(authoring.chargeDamage, f);
            authoring.chargeDamageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(ScarabBossAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.chargeDamage = __state.Values[0];
            authoring.chargeDamageMultiplier = __state.Multipliers[0];
        }
    }

    [HarmonyPatch(typeof(SlimeBossJumpStateConverter), "Convert", new[] { typeof(SlimeBossJumpStateAuthoring) })]
    public static class SlimeBossJumpStateDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(SlimeBossJumpStateConverter __instance, SlimeBossJumpStateAuthoring authoring, out SavedDamage __state)
        {
            __state = null;
            float f = Tuning.DamageFactorFor(__instance, authoring);
            if (Tuning.IsOne(f)) return;
            __state = new SavedDamage { Values = new[] { authoring.damage }, Multipliers = new[] { authoring.damageMultiplier } };
            authoring.damage = Tuning.Scale(authoring.damage, f);
            authoring.damageMultiplier *= f;
        }

        [HarmonyPostfix]
        public static void Postfix(SlimeBossJumpStateAuthoring authoring, SavedDamage __state)
        {
            if (__state == null) return;
            authoring.damage = __state.Values[0];
            authoring.damageMultiplier = __state.Multipliers[0];
        }
    }
}
