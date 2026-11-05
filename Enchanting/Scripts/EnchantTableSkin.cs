using System.Collections.Generic;
using HarmonyLib;
using Pug.Sprite;

namespace Enchanting
{
    /// <summary>
    /// The Enchanting Table draws with the vanilla Upgrade Forge graphics prefab (shared pool with real forges).
    /// Its sprites carry the vanilla "upgradeForge" skin; for our ObjectID we swap in Enchanting_TableSkin (the
    /// same sprites with the glow recolored violet, built in the bundle), and put the original skin back when a
    /// pooled graphic is reused by a vanilla forge.
    /// </summary>
    [HarmonyPatch(typeof(EntityMonoBehaviour), nameof(EntityMonoBehaviour.UpdateGraphicsFromObjectInfo))]
    public static class EnchantTableSkinPatch
    {
        public const string SkinName = "Enchanting_TableSkin";
        internal static SpriteAssetSkin Skin;

        // Original skin per sprite we changed, restored when the graphic shows a vanilla forge again.
        private static readonly Dictionary<SpriteObject, DataBlockRef<SpriteAssetSkin>> Originals =
            new Dictionary<SpriteObject, DataBlockRef<SpriteAssetSkin>>();

        private static int _logged;

        public static void Postfix(EntityMonoBehaviour __instance, ObjectInfo info)
        {
            if (!(__instance is UpgradeForge) || info == null) return;
            bool table = Skin != null && EnchantRecipe.TableId != ObjectID.None && info.objectID == EnchantRecipe.TableId;
            if (_logged < 6 && (table || info.objectID == EnchantRecipe.TableId))
            {
                _logged++;
                UnityEngine.Debug.Log($"[{EnchantingMod.Name}] table graphics: skin {(Skin != null ? "loaded" : "MISSING")}, objectID {info.objectID}, applying violet: {table}");
            }
            if (table) PruneDestroyed();
            foreach (var sprite in __instance.GetComponentsInChildren<SpriteObject>(true))
            {
                if (sprite == null) continue;
                if (table)
                {
                    if (!Originals.ContainsKey(sprite)) Originals[sprite] = sprite.skinRef;
                    sprite.skinRef = Skin.address;
                    sprite.ApplyVisualChange();
                }
                else if (Originals.TryGetValue(sprite, out var original))
                {
                    sprite.skinRef = original;
                    Originals.Remove(sprite);
                    sprite.ApplyVisualChange();
                }
            }
        }

        // Graphics destroyed with a closed world leave dead keys behind; drop them before adding new ones.
        private static readonly List<SpriteObject> Dead = new List<SpriteObject>();

        private static void PruneDestroyed()
        {
            foreach (var key in Originals.Keys)
                if (key == null) Dead.Add(key);
            if (Dead.Count == 0) return;
            foreach (var key in Dead) Originals.Remove(key);
            Dead.Clear();
        }
    }
}
