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

        /// <summary>Cells of the brush, nearest to the aimed cell first (so a short stack fills the middle).</summary>
        public static List<int3> Cells(int3 anchor, BrushShape shape, int edge)
        {
            var extent = Extent(shape, edge);
            var corner = Corner(anchor, extent);
            var cells = new List<int3>(extent.x * extent.y);
            for (int x = 0; x < extent.x; x++)
                for (int z = 0; z < extent.y; z++)
                    cells.Add(corner + new int3(x, 0, z));
            cells.Sort((a, b) => DistanceSq(a, anchor).CompareTo(DistanceSq(b, anchor)));
            return cells;
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
                case BrushShape.Row: return $"row of {edge}";
                case BrushShape.Column: return $"column of {edge}";
                default: return $"{edge}x{edge}";
            }
        }

        public static string Label(BlockMode mode)
        {
            switch (mode)
            {
                case BlockMode.Ground: return "ground only";
                case BlockMode.Wall: return "walls only";
                default: return "toggle (vanilla)";
            }
        }

        public static string Label(RoofMode mode)
        {
            switch (mode)
            {
                case RoofMode.AddRoof: return "add roof only";
                case RoofMode.ClearRoof: return "remove roof only";
                default: return "toggle (vanilla)";
            }
        }
    }
}
