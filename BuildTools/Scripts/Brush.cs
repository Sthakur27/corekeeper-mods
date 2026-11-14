using System.Collections.Generic;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildTools
{
    public enum BrushShape : byte
    {
        Off,
        Square,
        Row,
        Column,
    }

    /// <summary>Placeable groups that each remember their own brush shape and size.</summary>
    public enum BrushCategory : byte
    {
        Walls,
        Floors,
        Seeds,
        Objects,
    }

    /// <summary>What a wall block places: vanilla (floor on bare ground, else wall), only the floor version, only walls.</summary>
    public enum BlockMode : byte
    {
        Toggle,
        Ground,
        Wall,
    }

    public enum RoofMode : byte
    {
        Toggle,
        AddRoof,
        ClearRoof,
    }

    /// <summary>
    /// Which held placeables the brush works with, and the cells a brush covers. Shared by the client
    /// (preview, memory) and the server (<see cref="Systems.BrushPlaceSystem"/>) so both agree.
    /// </summary>
    public static class Brush
    {
        public const int MaxEdge = 14; // 15 is InputBits.NoSize

        /// <summary>
        /// True for 1x1 placeables the brush can repeat: blocks, floors, seeds and plain objects.
        /// Not for multi-tile objects, wall-mounted items (their side is picked per cell), critters,
        /// cattle and mounts (only PlaceablePrefab qualifies).
        /// </summary>
        public static bool TryClassify(EntityManager em, Entity prefab, ref PugDatabase.EntityObjectInfo info, out BrushCategory category)
        {
            category = BrushCategory.Objects;
            if (prefab == Entity.Null || !em.Exists(prefab)) return false;
            if (info.objectType != ObjectType.PlaceablePrefab) return false;
            if (info.prefabTileSize.x != 1 || info.prefabTileSize.y != 1) return false;
            if (em.HasComponent<DirectionBasedOnVariationCD>(prefab)) return false;

            if (em.HasComponent<TileCD>(prefab))
            {
                category = info.tileType.IsWallTile() ? BrushCategory.Walls : BrushCategory.Floors;
                return true;
            }
            category = em.HasComponent<PlantCD>(prefab) ? BrushCategory.Seeds : BrushCategory.Objects;
            return true;
        }

        public static bool IsOn(BrushShape shape, int edge) => shape != BrushShape.Off && edge > 1;

        /// <summary>Width and height of the brush (x, z); 1x1 when off.</summary>
        public static int2 Extent(BrushShape shape, int edge)
        {
            if (!IsOn(shape, edge)) return new int2(1, 1);
            switch (shape)
            {
                case BrushShape.Row: return new int2(edge, 1);
                case BrushShape.Column: return new int2(1, edge);
                default: return new int2(edge, edge);
            }
        }

        /// <summary>Min corner of the brush, centred on the aimed cell.</summary>
        public static int3 Corner(int3 anchor, int2 extent)
        {
            return anchor - new int3((extent.x - 1) / 2, 0, (extent.y - 1) / 2);
        }

        /// <summary>Extra reach the game gives a placeable beyond its own size (PlacementHandler.GetMaxReachDistance).</summary>
        private const float AimReach = 1.5f;

        /// <summary>
        /// Min corner of a block brush aimed with the mouse, the way the game aims a resized hoe or shovel
        /// (PlacementHandler.FindPlaceablePositionFromMouseOrJoystick with the brush's size): the mouse point,
        /// shifted so it sits in the middle of the brush, is clamped into a box of the brush size plus
        /// <see cref="AimReach"/> centred on the player. A bigger brush can therefore be pushed further out
        /// with the cursor, while a 1x1 block's aimed cell stays next to the player.
        /// </summary>
        public static int3 AimedCorner(int3 anchor, int2 extent, float3 player, float2 mouse)
        {
            var size = new float2(extent.x, extent.y);
            var half = (size - 1f) / 2f;
            var center = player.xz - half;
            var box = (size + AimReach) / 2f;
            var p = math.clamp(mouse - half, center - box, center + box);
            return new int3((int)math.round(p.x), anchor.y, (int)math.round(p.y));
        }

        /// <summary>
        /// Cells of the brush. Mouse-aimed (blocks and floors with keyboard and mouse) it uses
        /// <see cref="AimedCorner"/> and fills nearest the player first; otherwise it is centred on the
        /// game's aimed cell and fills from there (seeds and objects, whose aimed cell the game places itself).
        /// </summary>
        public static List<int3> Cells(int3 anchor, BrushShape shape, int edge, bool aimed, float3 player, float2 mouse)
        {
            var extent = Extent(shape, edge);
            var corner = aimed ? AimedCorner(anchor, extent, player, mouse) : Corner(anchor, extent);
            var from = aimed ? new int3((int)math.round(player.x), anchor.y, (int)math.round(player.z)) : anchor;
            var cells = new List<int3>(extent.x * extent.y);
            for (int x = 0; x < extent.x; x++)
                for (int z = 0; z < extent.y; z++)
                    cells.Add(corner + new int3(x, 0, z));
            cells.Sort((a, b) => DistanceSq(a, from).CompareTo(DistanceSq(b, from)));
            return cells;
        }

        /// <summary>Whether the brush follows the mouse (<see cref="AimedCorner"/>): tile items aimed with keyboard and mouse.</summary>
        public static bool FollowsMouse(bool isTile, in ClientInput input)
        {
            return isTile && (input.prefersKeyboardAndMouse || input.wasAiming);
        }

        private static int DistanceSq(int3 a, int3 b)
        {
            int dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public static string Label(BrushCategory category)
        {
            switch (category)
            {
                case BrushCategory.Walls: return "Blocks";
                case BrushCategory.Floors: return "Floors";
                case BrushCategory.Seeds: return "Seeds";
                default: return "Objects";
            }
        }

        public static string Label(BrushShape shape, int edge)
        {
            if (!IsOn(shape, edge)) return "brush off";
            switch (shape)
            {
                case BrushShape.Row: return $"line across, {edge}";
                case BrushShape.Column: return $"line down, {edge}";
                default: return $"{edge}x{edge}";
            }
        }

        public static string Label(BlockMode mode)
        {
            switch (mode)
            {
                case BlockMode.Ground: return "floor only";
                case BlockMode.Wall: return "walls only";
                default: return "auto (floor first, then walls)";
            }
        }

        public static string Label(RoofMode mode)
        {
            switch (mode)
            {
                case RoofMode.AddRoof: return "close roof";
                case RoofMode.ClearRoof: return "open roof";
                default: return "flip (vanilla)";
            }
        }
    }
}
