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
    /// Vanilla hard mode gives level-scaled enemies 1.5x their prefab's raw base health instead of the
    /// level-based health. For regular enemies this computes the normal (level-based) value and
    /// multiplies it by HealthMultiplier. Enemies whose health is not level based are left alone
    /// (vanilla hard mode does not scale them either).
    /// </summary>
    [HarmonyPatch(typeof(HealthAuthoring), nameof(HealthAuthoring.ComputeMaxHealth))]
    public static class HealthPatch
    {
        [HarmonyPrefix]
        public static void Prefix(HealthAuthoring __instance, ref bool useHardModeSettings, out bool __state)
        {
            __state = false;
            if (!useHardModeSettings || __instance.dontCalculateHealthFromLevel) return;
            if (!__instance.TryGetComponent(out AreaLevelAuthoring level) || !level.enabled) return;
            if (!Tuning.IsRegularEnemy(__instance.gameObject)) return;
            __state = true;
            useHardModeSettings = false;
        }

        [HarmonyPostfix]
        public static void Postfix(HealthAuthoring __instance, ref int __result, bool __state)
        {
            if (!__state) return;
            int levelBased = __result;
            __result = Math.Max(1, (int)Math.Round(levelBased * Tuning.HealthMultiplier));
            int vanillaHard = (int)Math.Round(__instance.maxHealth * 1.5f);
            UnityEngine.Debug.Log($"[HardModeTuning] {__instance.gameObject.name}: health {__result} (normal {levelBased}, vanilla hard {vanillaHard})");
        }
    }
}
