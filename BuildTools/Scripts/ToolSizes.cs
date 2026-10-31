using PlayerEquipment;

namespace BuildTools
{
    /// <summary>
    /// Which tools Build Tools resizes, and how a tool's area is stored.
    ///
    /// Vanilla keeps one size per tool type in the player's PlacementSizeByEquipmentTypeBuffer
    /// (shovel, watering can, hoe, roofing gadget, seeder, paint tool), as a "variation" index:
    /// area = min(variation, prefabTileSize.x - 1) + 1. prefabTileSize in the database blob is the
    /// ceiling, so raising it is all it takes to allow bigger areas; the dig/water/roof code then
    /// handles any size on its own. The buffer is ghost-replicated but NOT saved, which is why the
    /// game forgets sizes on rejoin (<see cref="SizeMemory"/> fixes that).
    /// </summary>
    public static class ToolSizes
    {
        public const int MinCeiling = 3;
        public const int MaxCeiling = 15;
        public const int DefaultCeiling = 10;

        /// <summary>Highest variation index anything can hold (a 15x15 area).</summary>
        public const int MaxVariation = MaxCeiling - 1;

        /// <summary>Ceiling for shovels, hoes, the roofing gadget and paint tools. Set from Mod Options.</summary>
        public static int Ceiling = DefaultCeiling;

        /// <summary>Bumped whenever <see cref="Ceiling"/> changes so the ceiling system re-applies.</summary>
        public static int Version;

        /// <summary>
        /// True when watering cans get the Build Tools ceiling too: Bigger Watering Cans is not running,
        /// or its sizes are overridden in the settings. Otherwise its 2x2 / 4x4 stay the cap and the size
        /// keys can only shrink a can. Set from BuildToolsMod; changes bump <see cref="Version"/>.
        /// </summary>
        public static bool RaiseWateringCans;

        /// <summary>Object types Build Tools may resize (raised or put back to their own size).</summary>
        public static bool IsToolType(ObjectType type)
        {
            return type == ObjectType.Shovel ||
                   type == ObjectType.Hoe ||
                   type == ObjectType.RoofingTool ||
                   type == ObjectType.PaintTool ||
                   type == ObjectType.WaterCan;
        }

        /// <summary>Object types whose ceiling is raised right now.</summary>
        public static bool IsRaisedType(ObjectType type)
        {
            return IsToolType(type) && (type != ObjectType.WaterCan || RaiseWateringCans);
        }

        /// <summary>Tool slots that have a size (same list as the game's HasSizeVariationForEquipment).</summary>
        public static bool HasSize(EquipmentSlotType slot)
        {
            return PlacementSizeByEquipmentTypeBufferExtensions.HasSizeVariationForEquipment(slot);
        }

        /// <summary>Area edge length the game uses for a variation under a given ceiling.</summary>
        public static int EdgeFor(int variation, int ceiling)
        {
            if (ceiling < 1) ceiling = 1;
            int v = variation < ceiling - 1 ? variation : ceiling - 1;
            return v + 1;
        }

        public static string Label(EquipmentSlotType slot)
        {
            switch (slot)
            {
                case EquipmentSlotType.ShovelSlot: return "Shovel";
                case EquipmentSlotType.WaterCanSlot: return "Watering can";
                case EquipmentSlotType.HoeSlot: return "Hoe";
                case EquipmentSlotType.RoofingToolSlot: return "Roofing gadget";
                case EquipmentSlotType.SeederSlot: return "Seeder";
                case EquipmentSlotType.PaintToolSlot: return "Paint tool";
                default: return slot.ToString();
            }
        }
    }
}
