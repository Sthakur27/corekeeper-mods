using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace FasterMachines.Patches
{
    /// <summary>
    /// The machine window's progress bar is (fullTime - timeLeft) / fullTime with the vanilla full time. With the
    /// timer divided by m the real full time is fullTime / m, so the bar becomes 1 - m * (1 - vanillaResult).
    /// </summary>
    [HarmonyPatch(typeof(CraftingHandler), nameof(CraftingHandler.GetNormalizedElapsedCraftingTime))]
    public static class CraftProgressPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CraftingHandler __instance, ref float __result)
        {
            if (__result <= 0f || __result >= 1f) return;
            World world = Manager.ecs.ClientWorld;
            if (world == null) return;
            if (!EntityUtility.TryGetComponentData<ObjectDataCD>(__instance.craftingEntity, world, out var data)) return;
            float m = MachineSpeeds.For(data.objectID);
            if (m == 1f) return;
            __result = Mathf.Clamp01(1f - m * (1f - __result));
        }
    }
}
