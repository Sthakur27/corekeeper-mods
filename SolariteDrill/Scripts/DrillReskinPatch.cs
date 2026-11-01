using HarmonyLib;
using Pug.Sprite;

namespace SolariteDrill
{
    /// <summary>
    /// The Solarite Drill uses the vanilla drill graphics prefab (shared pool with Tin and Scarlet drills).
    /// Vanilla <c>SpriteSkinFromEntityAndSeason</c> picks a skin per ObjectID from a list baked into that prefab,
    /// which can't name a modded ID, so for our ID we apply our skins the same way it applies the Tin Drill's.
    /// Our skins target the vanilla drill SpriteAssets, so a pooled sprite never gets a skin for another asset,
    /// and vanilla's own UpdateSkin resets skinRef on every spawn, so reuse by a vanilla drill stays correct.
    /// </summary>
    [HarmonyPatch(typeof(SpriteSkinFromEntityAndSeason), "UpdateSkin")]
    public static class DrillReskinPatch
    {
        public static bool Prefix(SpriteSkinFromEntityAndSeason __instance, ObjectID objectID)
        {
            var drill = SolariteDrillMod.Id;
            if (drill == ObjectID.None || objectID != drill) return true;
            var sprites = __instance.spritesToReskin;
            if (sprites == null || sprites.Count != 2 || SolariteDrillMod.DrillSkin == null || SolariteDrillMod.BaseSkin == null)
                return true;
            Apply(sprites[0], SolariteDrillMod.DrillSkin);
            Apply(sprites[1], SolariteDrillMod.BaseSkin);
            return false;
        }

        private static void Apply(SpriteObject sprite, SpriteAssetSkin skin)
        {
            if (sprite == null) return;
            sprite.skinRef = skin.address;
            sprite.primaryGradientMap = default;
            sprite.ApplyVisualChange();
        }
    }
}
