using HarmonyLib;
using UnityEngine;

namespace DrillUnit.Patches
{
    /// <summary>
    /// The Drill Unit draws with the vanilla boat graphics, which pick their sprite sheets from Boat.boatSkins by
    /// ObjectID. Our ID has no entry, so it gets our own sheets (a recolor of the Speeder Boat's, same layout;
    /// loaded from the bundle in <see cref="DrillUnitMod.ModObjectLoaded"/>), or the Speeder Boat's if those are
    /// missing.
    /// </summary>
    [HarmonyPatch(typeof(Boat), "UpdateBoatSkin")]
    public static class UnitSkinPatch
    {
        internal static Texture2D Front, Back;

        [HarmonyPostfix]
        public static void Postfix(Boat __instance)
        {
            if (DrillUnitMod.Id == ObjectID.None || __instance.objectData.objectID != DrillUnitMod.Id) return;
            if (Front != null && Back != null)
            {
                __instance.spriteSheetSkinBack.SetSkin(Back);
                __instance.spriteSheetSkinFront.SetSkin(Front);
                return;
            }
            foreach (var skin in __instance.boatSkins)
            {
                if (skin.boatID != ObjectID.SpeederBoat) continue;
                __instance.spriteSheetSkinBack.SetSkin(skin.textureBack);
                __instance.spriteSheetSkinFront.SetSkin(skin.textureFront);
                return;
            }
        }
    }

    /// <summary>It hovers: no wake particles, also not when crossing water.</summary>
    [HarmonyPatch(typeof(Boat), nameof(Boat.ManagedLateUpdate))]
    public static class UnitWakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Boat __instance)
        {
            if (DrillUnitMod.Id == ObjectID.None || __instance.waterParticles == null) return;
            if (__instance.objectData.objectID != DrillUnitMod.Id) return;
            var emission = __instance.waterParticles.emission;
            emission.rateOverTime = 0f;
        }
    }
}
