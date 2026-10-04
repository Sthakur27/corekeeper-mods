using System;
using HarmonyLib;
using Pug.Conversion;

namespace DifficultyTuning.Patches
{
    /// <summary>
    /// Converter.ConversionManager has a private getter, so the world being converted is captured when
    /// the manager is assigned: server conversion of a hard world = Hard profile, server conversion of
    /// any other world = Normal profile, client and startup conversions = not tuned.
    /// </summary>
    [HarmonyPatch(typeof(Converter), "ConversionManager", MethodType.Setter)]
    public static class ConverterManagerPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Converter __instance, ConversionManager value)
        {
            Tuning.SetConverterMode(__instance, value != null && value.IsServer, value != null && value.UseHardModeSettings);
        }
    }

    /// <summary>
    /// HealthAuthoring.ComputeMaxHealth only gets the hard flag, which is false both for normal worlds
    /// and client conversions. The two converters that call it (health, loot) publish their world's
    /// profile while it runs.
    /// </summary>
    [HarmonyPatch(typeof(HealthConverter), "Convert", new[] { typeof(HealthAuthoring) })]
    public static class HealthConverterProfilePatch
    {
        [HarmonyPrefix]
        public static void Prefix(HealthConverter __instance) => Tuning.CurrentHealthProfile = Tuning.ProfileFor(__instance);

        [HarmonyFinalizer]
        public static void Finalizer() => Tuning.CurrentHealthProfile = null;
    }

    [HarmonyPatch(typeof(DropLootConverter), "Convert", new[] { typeof(DropLootAuthoring) })]
    public static class DropLootConverterProfilePatch
    {
        [HarmonyPrefix]
        public static void Prefix(DropLootConverter __instance) => Tuning.CurrentHealthProfile = Tuning.ProfileFor(__instance);

        [HarmonyFinalizer]
        public static void Finalizer() => Tuning.CurrentHealthProfile = null;
    }

    /// <summary>
    /// Enemy max health. Hard worlds: vanilla gives enemies 1.5x their normal (level-based) health; for
    /// regular enemies this computes the normal value and multiplies it by the Hard profile's Health
    /// (default 1.5x = vanilla hard); bosses keep the vanilla hard value x BossHealth. Normal worlds:
    /// regular level-based enemies get normal health x the Normal profile's Health, bosses normal
    /// health x BossHealth (both default 1x = vanilla). Enemies whose health is not level based are
    /// left alone (vanilla hard mode does not scale them either).
    /// </summary>
    [HarmonyPatch(typeof(HealthAuthoring), nameof(HealthAuthoring.ComputeMaxHealth))]
    public static class HealthPatch
    {
        public struct State
        {
            public Profile Profile;
            public bool Boss;
        }

        [HarmonyPrefix]
        public static void Prefix(HealthAuthoring __instance, ref bool useHardModeSettings, out State __state)
        {
            __state = default;
            Profile p = useHardModeSettings ? Tuning.Hard : Tuning.CurrentHealthProfile;
            if (p == null) return;
            if (!__instance.TryGetComponent(out EnemyAuthoring enemy) || !enemy.enabled) return;
            if (Tuning.IsBoss(__instance.gameObject))
            {
                __state = new State { Profile = p, Boss = true };
                return;
            }
            if (__instance.dontCalculateHealthFromLevel) return;
            if (!__instance.TryGetComponent(out AreaLevelAuthoring level) || !level.enabled) return;
            __state = new State { Profile = p, Boss = false };
            useHardModeSettings = false; // normal level-based value; the postfix applies the multiplier
        }

        [HarmonyPostfix]
        public static void Postfix(ref int __result, State __state)
        {
            if (__state.Profile == null) return;
            float m = __state.Boss ? __state.Profile.BossHealth : __state.Profile.Health;
            if (Tuning.IsOne(m)) return;
            __result = Math.Max(1, (int)Math.Round(__result * m));
        }
    }
}
