using System;

namespace BoulderMover
{
    /// <summary>
    /// The pickup requirement is the Maze Block's (ObjectID.WallDesertTempleBlock2, "an extremely durable block used
    /// to build ancient mazes"): a pickaxe hit only counts if its mining damage beats the block's damage reduction, and
    /// the counted damage must add up to the block's health. The values are read from the prefab when it is converted;
    /// the defaults are the vanilla ones (read in game 2026-11-01) in case the boulders convert first.
    /// </summary>
    internal static class MazeBlock
    {
        public static int Reduction = 895;
        public static int Health = 988;

        /// <summary>A boulder takes this many Maze Blocks' worth of counted damage to pick up (Sid: "a few more hits").</summary>
        public const int EffortMultiplier = 3;

        public static int PickupEffort => Health * EffortMultiplier;

        private static readonly System.Collections.Generic.Dictionary<int, bool> IsBoulderCache =
            new System.Collections.Generic.Dictionary<int, bool>();

        /// <summary>Ore boulders by ObjectID name ("...OreBoulder"), so new boulder types are covered automatically.</summary>
        public static bool IsOreBoulder(ObjectID id)
        {
            int key = (int)id;
            if (!IsBoulderCache.TryGetValue(key, out bool yes))
            {
                yes = id.ToString().EndsWith("OreBoulder", StringComparison.Ordinal);
                IsBoulderCache[key] = yes;
            }
            return yes;
        }
    }
}
