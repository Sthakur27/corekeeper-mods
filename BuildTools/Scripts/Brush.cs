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

        /// <summary>
        /// Min corner of the brush. The game keeps the aimed cell next to the player, so a brush centred on
        /// it covered the player once it was 3x3 or bigger. Instead the brush sits in front of the player in
        /// one of four directions, whichever way the mouse is furthest (<paramref name="aim"/> = mouse minus
        /// player): its near edge is the row or column next to the player. Sideways it follows the mouse, but
        /// only up to half the brush (at least one tile) off the player's row or column, so a diagonal aim
        /// moves it one step aside instead of the two the old centred-on-the-aimed-cell brush jumped. With
        /// no aim it is centred on the aimed cell.
        /// </summary>
        public static int3 Corner(int3 anchor, int2 extent, int2 playerCell, float2 aim)
        {
            int2 half = (extent - 1) / 2;
            if (aim.x == 0f && aim.y == 0f)
                return anchor - new int3(half.x, 0, half.y);
            if (math.abs(aim.x) >= math.abs(aim.y))
            {
                int x = aim.x > 0f ? playerCell.x + 1 : playerCell.x - extent.x;
                return new int3(x, anchor.y, Sideways(playerCell.y, aim.y, extent.y) - half.y);
            }
            int z = aim.y > 0f ? playerCell.y + 1 : playerCell.y - extent.y;
            return new int3(Sideways(playerCell.x, aim.x, extent.x) - half.x, anchor.y, z);
        }

        /// <summary>Centre of the brush across the aim: the mouse's row or column, at most max(1, half the brush) off the player's.</summary>
        private static int Sideways(int player, float aim, int extent)
        {
            int reach = math.max(1, (extent - 1) / 2);
            return math.clamp((int)math.round(aim), -reach, reach) + player;
        }

        /// <summary>The tile cell a world position is on (tiles are centred on whole coordinates).</summary>
        public static int2 CellOf(float x, float z)
        {
            return new int2((int)math.round(x), (int)math.round(z));
        }

        /// <summary>Cells of the brush, nearest to the player first (so a short stack fills the near edge first).</summary>
        public static List<int3> Cells(int3 anchor, BrushShape shape, int edge, int2 playerCell, float2 aim)
        {
            var extent = Extent(shape, edge);
            var corner = Corner(anchor, extent, playerCell, aim);
            var near = new int3(playerCell.x, anchor.y, playerCell.y);
            var cells = new List<int3>(extent.x * extent.y);
            for (int x = 0; x < extent.x; x++)
                for (int z = 0; z < extent.y; z++)
                    cells.Add(corner + new int3(x, 0, z));
            cells.Sort((a, b) => DistanceSq(a, near).CompareTo(DistanceSq(b, near)));
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
