// Local art preview using the SDK-built RunicStorageJoinPreview asset pack.
using System;
using System.Collections.Generic;
using HarmonyLib;
using Pug.Sprite;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ConnectedStorage
{
    internal static class StorageJoinPreview
    {
        private sealed class Unit
        {
            public Chest Chest;
            public SpriteObject Sprite;
            public SpriteAsset Original;
            public int Tier;
            public bool FacingFront;
            public World World;
            public int2 Tile;
        }

        private static readonly int2[] Offsets =
        {
            new int2(-1, 0), new int2(1, 0), new int2(0, 1), new int2(0, -1),
            new int2(-1, 1), new int2(1, 1), new int2(-1, -1), new int2(1, -1)
        };
        private static readonly int[] CornerSides = { 5, 6, 9, 10 };
        private static readonly Dictionary<(World, int2), Unit> Units = new();
        private static readonly Dictionary<Chest, Unit> ByChest = new();
        private static readonly Dictionary<SpriteObject, Unit> BySprite = new();
        private static readonly Dictionary<(int, int), SpriteAsset> Art = new();
        private static bool joinLogged;

        private static bool FacesFront(float3 direction) =>
            Mathf.Abs(direction.x) < 0.5f && direction.z < -0.5f;

        [HarmonyPatch(typeof(SpriteAssetManager), nameof(SpriteAssetManager.LoadAndCompile))]
        private static class AssetsPatch
        {
            private static void Postfix()
            {
                const string prefix = "RunicPreview_";
                Art.Clear();
                if (!SpriteAssetManager.isLoaded) return;
                foreach (var asset in SpriteAssetManager.loadedSpriteAssets)
                {
                    if (asset is not SpriteAsset sprite || !sprite.name.StartsWith(prefix, StringComparison.Ordinal))
                        continue;
                    int separator = sprite.name.LastIndexOf('_');
                    if (separator <= prefix.Length || !int.TryParse(sprite.name.Substring(separator + 1), out int mask))
                        continue;
                    string chest = sprite.name.Substring(prefix.Length, separator - prefix.Length);
                    int tier = Array.FindIndex(StorageCatalog.Tiers, name => name + "Chest" == chest);
                    if (tier >= 0 && sprite.staticAtlasRects != null && sprite.staticAtlasRects.Length > 0 &&
                        ReferenceEquals(ScriptableData.GetDataBlock<SpriteAsset>(sprite.address), sprite))
                        Art.Add((tier, mask), sprite);
                }
                Debug.Log("[RunicJoinPreview] Atlas variants resolved by registered ID: " + Art.Count);
            }
        }

        [HarmonyPatch(typeof(Chest), "OnSpawn")]
        private static class SpawnPatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Chest __instance)
            {
                if (Art.Count == 0) return;
                int tier = StorageCatalog.Tier(__instance.objectData.objectID);
                if (tier < 0 || __instance.spriteObjects == null) return;
                var world = __instance.world;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                var entity = __instance.entity;
                if (!em.HasComponent<Unity.Transforms.LocalTransform>(entity)) return;
                foreach (var sprite in __instance.spriteObjects)
                {
                    if (sprite == null || sprite.gameObject.name != "Sprite Object" || sprite.asset == null) continue;
                    Remove(__instance);
                    bool facingFront = !em.HasComponent<DirectionCD>(entity) ||
                        FacesFront(em.GetComponentData<DirectionCD>(entity).direction);
                    var unit = new Unit {
                        Chest = __instance, Sprite = sprite, Original = sprite.asset,
                        Tier = tier, World = world, Tile = StorageNetwork.Tile(em, entity),
                        FacingFront = facingFront,
                    };
                    if (Units.TryGetValue((world, unit.Tile), out var old))
                        Remove(old.Chest);
                    Units.Add((world, unit.Tile), unit);
                    ByChest.Add(__instance, unit);
                    BySprite.Add(sprite, unit);
                    RefreshAround(world, unit.Tile);
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Chest), "OnDespawn")]
        private static class DespawnPatch
        {
            private static void Prefix(Chest __instance) => Remove(__instance);
        }

        [HarmonyPatch(typeof(SpriteVariationFromEntityDirection), nameof(SpriteVariationFromEntityDirection.SetDirection))]
        private static class DirectionPatch
        {
            private static void Prefix(SpriteVariationFromEntityDirection __instance, int2 direction)
            {
                if (BySprite.Count == 0 || __instance.spritesToRotate == null || (direction.x == 0 && direction.y < 0))
                    return;
                foreach (var sprite in __instance.spritesToRotate)
                    if (sprite != null && BySprite.TryGetValue(sprite, out var unit) && unit.FacingFront)
                        sprite.asset = unit.Original;
            }

            private static void Postfix(SpriteVariationFromEntityDirection __instance, int2 direction)
            {
                if (BySprite.Count == 0 || __instance.spritesToRotate == null) return;
                foreach (var sprite in __instance.spritesToRotate)
                {
                    if (sprite == null || !BySprite.TryGetValue(sprite, out var unit)) continue;
                    bool facingFront = direction.x == 0 && direction.y < 0;
                    if (unit.FacingFront == facingFront) continue;
                    unit.FacingFront = facingFront;
                    RefreshAround(unit.World, unit.Tile);
                }
            }
        }

        private static void Remove(Chest chest)
        {
            if (!ByChest.TryGetValue(chest, out var unit)) return;
            ByChest.Remove(chest);
            BySprite.Remove(unit.Sprite);
            Units.Remove((unit.World, unit.Tile));
            if (unit.Sprite != null) unit.Sprite.asset = unit.Original;
            RefreshAround(unit.World, unit.Tile);
        }

        private static void RefreshAround(World world, int2 tile)
        {
            Refresh(world, tile);
            foreach (var offset in Offsets) Refresh(world, tile + offset);
        }

        private static void Refresh(World world, int2 tile)
        {
            if (!Units.TryGetValue((world, tile), out var unit) || unit.Sprite == null) return;
            var art = unit.Original;
            int mask = 0;
            if (unit.FacingFront)
            {
                for (int i = 0; i < Offsets.Length; i++)
                {
                    if (i >= 4 && (mask & CornerSides[i - 4]) != CornerSides[i - 4]) continue;
                    if (Units.TryGetValue((world, tile + Offsets[i]), out var neighbor) && neighbor.FacingFront)
                        mask |= 1 << i;
                }
                if (Art.TryGetValue((unit.Tier, mask), out var variant)) art = variant;
            }
            if (unit.Sprite.asset != art)
            {
                if (art != unit.Original) unit.Sprite.ResetVariant();
                unit.Sprite.asset = art;
            }
            if (!joinLogged && mask != 0 && art != unit.Original && ReferenceEquals(unit.Sprite.asset, art))
            {
                joinLogged = true;
                Debug.Log("[RunicJoinPreview] Renderer resolved joined sprite: " + art.name);
            }
        }
    }
}
