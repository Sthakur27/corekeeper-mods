using Unity.Mathematics;
using UnityEngine;

namespace BiggerWateringCans
{
    /// <summary>
    /// The watered area of each can. A watering can's area is simply its prefab tile size
    /// (PugDatabase.EntityObjectInfo.prefabTileSize / ObjectInfo.prefabTileSize): WaterCanSlot.PlaceItem
    /// loops over prefabTileSize.x * prefabTileSize.y tiles from PlacementCD.bestPositionToPlaceAt, and
    /// PlacementHandler uses the same size for the aim preview. So the whole mod is "change that number".
    /// </summary>
    public static class CanSizes
    {
        public const int BasicCanSize = 2;  // Watering Can: 2x2
        public const int IronCanSize = 4;   // Iron Watering Can (ObjectID.LargeWaterCan): 4x4

        /// <summary>Highest size-variation index we ever need (index n = (n+1)x(n+1), clamped per can).</summary>
        public const byte MaxVariation = IronCanSize - 1;

        public static bool TryGet(ObjectID id, out int size)
        {
            switch (id)
            {
                case ObjectID.WaterCan: size = BasicCanSize; return true;
                case ObjectID.LargeWaterCan: size = IronCanSize; return true;
                default: size = 0; return false;
            }
        }

        // Both Apply overloads only ever grow the size. Brush mods (Placement Plus, Tool Resizer) raise
        // prefabTileSize to their own max as a size ceiling; shrinking it back made the server and client
        // disagree (whichever world patched last won), so the aim preview flickered between the two sizes.

        /// <summary>Applies the mod size to a managed ObjectInfo. Returns true if it changed anything.</summary>
        public static bool Apply(ObjectInfo info)
        {
            if (info == null || !TryGet(info.objectID, out int size)) return false;
            if (info.prefabTileSize.x >= size && info.prefabTileSize.y >= size) return false;
            info.prefabTileSize = new Vector2Int(size, size);
            return true;
        }

        /// <summary>Applies the mod size to a blob EntityObjectInfo. Returns true if it changed anything.</summary>
        public static bool Apply(ref PugDatabase.EntityObjectInfo info, ObjectID expected)
        {
            if (info.objectID != expected || !TryGet(expected, out int size)) return false;
            if (math.all(info.prefabTileSize >= size)) return false;
            info.prefabTileSize = new int2(size, size);
            return true;
        }
    }
}
