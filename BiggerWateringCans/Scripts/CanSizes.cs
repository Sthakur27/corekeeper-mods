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

        /// <summary>Applies the mod size to a managed ObjectInfo. Returns true if it changed anything.</summary>
        public static bool Apply(ObjectInfo info)
        {
            if (info == null || !TryGet(info.objectID, out int size)) return false;
            var want = new Vector2Int(size, size);
            if (info.prefabTileSize == want) return false;
            info.prefabTileSize = want;
            return true;
        }

        /// <summary>Applies the mod size to a blob EntityObjectInfo. Returns true if it changed anything.</summary>
        public static bool Apply(ref PugDatabase.EntityObjectInfo info, ObjectID expected)
        {
            if (info.objectID != expected || !TryGet(expected, out int size)) return false;
            var want = new int2(size, size);
            if (math.all(info.prefabTileSize == want)) return false;
            info.prefabTileSize = want;
            return true;
        }
    }
}
