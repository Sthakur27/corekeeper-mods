using System;
using HarmonyLib;
using Pug.Conversion;

namespace HardModeTuning.Patches
{
    /// <summary>
    /// Converter.ConversionManager has a private getter, so the hard mode flag of the world being
    /// converted is captured when the manager is assigned (server conversion of a hard world = true,
    /// client conversion = false).
    /// </summary>
    [HarmonyPatch(typeof(Converter), "ConversionManager", MethodType.Setter)]
    public static class ConverterManagerPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Converter __instance, ConversionManager value)
        {
            Tuning.SetConverterMode(__instance, value != null && value.UseHardModeSettings);
        }
    }

    /// <summary>
    /// Vanilla hard mode gives enemies 1.5x their normal (level-based) health. For regular enemies this
    /// computes the normal value and multiplies it by HealthMultiplier (default 1.5x = vanilla hard).
    /// Enemies whose health is not level based are left alone (vanilla hard mode does not scale them either).
    /// Bosses keep the vanilla hard value times BossHealthMultiplier (default 1x).
    /// </summary>
    [HarmonyPatch(typeof(HealthAuthoring), nameof(HealthAuthoring.ComputeMaxHealth))]
    public static class HealthPatch
    {
        private const int None = 0, Regular = 1, Boss = 2;

        [HarmonyPrefix]
        public static void Prefix(HealthAuthoring __instance, ref bool useHardModeSettings, out int __state)
        {
            __state = None;
            if (!useHardModeSettings) return;
            if (!__instance.TryGetComponent(out EnemyAuthoring enemy) || !enemy.enabled) return;
            if (Tuning.IsBoss(__instance.gameObject))
            {
                __state = Boss;
                return;
            }
            if (__instance.dontCalculateHealthFromLevel) return;
            if (!__instance.TryGetComponent(out AreaLevelAuthoring level) || !level.enabled) return;
            __state = Regular;
            useHardModeSettings = false;
        }

        [HarmonyPostfix]
        public static void Postfix(ref int __result, int __state)
        {
            float m = __state == Regular ? Tuning.HealthMultiplier : __state == Boss ? Tuning.BossHealthMultiplier : 1f;
            if (__state == None || (__state == Boss && Tuning.IsOne(m))) return;
            __result = Math.Max(1, (int)Math.Round(__result * m));
        }
    }
}
