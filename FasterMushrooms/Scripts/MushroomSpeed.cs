using System;
using System.Collections.Generic;
using System.Globalization;

namespace FasterMushrooms
{
    /// <summary>
    /// The one tunable: how many times faster mushrooms spread and grow. Settings store a token
    /// such as "4x"; the ECS system reads <see cref="Multiplier"/> every tick, so that path is a
    /// plain static float.
    /// </summary>
    public static class MushroomSpeed
    {
        /// <summary>Choices in the Mod Settings menu, in cycle order. 1x is vanilla.</summary>
        public static readonly string[] Ladder =
        {
            "1x", "1.5x", "2x", "3x", "4x", "5x", "6x", "8x", "10x", "15x", "20x", "30x", "50x", "100x"
        };

        public const string DefaultToken = "4x";

        /// <summary>Current speed multiplier (1 = vanilla). Never below 1.</summary>
        public static float Multiplier { get; private set; } = Parse(DefaultToken);

        public static void Set(string token)
        {
            Multiplier = Parse(token);
        }

        /// <summary>"4x" to 4, "1.5x" to 1.5. Anything unparseable or below 1 falls back to 1 (vanilla).</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v >= 1f ? v : 1f;
        }

        /// <summary>
        /// Mushrooms that grow wild and respawn from the environment spawn table (not enemies,
        /// armor or trophies that merely have "Mushroom" in their ObjectID name).
        /// </summary>
        public static bool IsWildMushroom(ObjectID id)
        {
            if (!IsMushroom(id)) return false;
            string n = id.ToString();
            foreach (string bad in NotWild)
                if (n.IndexOf(bad, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static readonly string[] NotWild = { "Enemy", "Brute", "Trophy", "Helm", "Armor", "Pants", "Seed" };

        private static readonly Dictionary<int, bool> _isMushroomCache = new Dictionary<int, bool>();

        /// <summary>
        /// True for the mushroom flora only (Mushroom, GlowingMushroom, MossMushroom, ...): any
        /// ObjectID whose name contains "Mushroom". The result is cached per id; the enum name is
        /// only built once. Combined with a RootPlantCD requirement by the caller, this leaves every
        /// other root plant and crop at vanilla speed.
        /// </summary>
        public static bool IsMushroom(ObjectID id)
        {
            int key = (int)id;
            if (_isMushroomCache.TryGetValue(key, out bool cached)) return cached;
            bool result;
            switch (id)
            {
                case ObjectID.Mushroom:
                case ObjectID.GlowingMushroom:
                case ObjectID.MossMushroom:
                    result = true;
                    break;
                default:
                    result = id.ToString().IndexOf("Mushroom", StringComparison.OrdinalIgnoreCase) >= 0;
                    break;
            }
            _isMushroomCache[key] = result;
            return result;
        }
    }

    /// <summary>Wild mushroom respawn near players (see Systems.MushroomRespawnSystem).</summary>
    public static class MushroomRespawn
    {
        public static readonly string[] Ladder = { "Off", "15 s", "30 s", "1 min", "2 min", "5 min", "10 min", "30 min" };
        public const string DefaultToken = "1 min";

        /// <summary>Tiles around each player whose 16x16 areas get respawn rolls.</summary>
        public const int Radius = 48;

        /// <summary>Seconds between respawn passes; 0 = off.</summary>
        public static float IntervalSeconds { get; private set; } = Parse(DefaultToken);

        public static void Set(string token) { IntervalSeconds = Parse(token); }

        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token) || token == "Off") return 0f;
            string[] parts = token.Split(' ');
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return 0f;
            return parts.Length > 1 && parts[1] == "min" ? v * 60f : v;
        }
    }
}
