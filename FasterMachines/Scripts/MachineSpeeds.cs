using System.Globalization;

namespace FasterMachines
{
    /// <summary>The machine groups that get their own speed setting.</summary>
    public enum Machine
    {
        Furnace,
        SmelterKiln,
        GlassSmelter,
        FuryForge,
        CookingPot,
        TableSaw,
        EggIncubator,
        Drill,
        Other,
    }

    /// <summary>
    /// Speed multiplier per machine group. Settings store tokens like "3x"; the systems and the progress-bar
    /// patch read the parsed floats.
    /// </summary>
    public static class MachineSpeeds
    {
        /// <summary>Timed machines (anything with a crafting timer).</summary>
        public static readonly string[] Ladder = { "1x", "1.5x", "2x", "3x", "5x", "10x", "20x", "50x", "100x", "Instant" };
        /// <summary>Drills: no "Instant" (a drill hit every tick with huge damage would just delete walls).</summary>
        public static readonly string[] DrillLadder = { "1x", "1.5x", "2x", "3x", "5x", "10x", "20x", "50x", "100x" };
        public const string DefaultToken = "5x";

        /// <summary>"Instant": one simulation tick per item (the timer is clamped to a single tick).</summary>
        public const float Instant = 100000f;

        private static readonly float[] Mult = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        /// <summary>Bumped on every change so the drill system re-applies.</summary>
        public static int Version { get; private set; }

        public static float Get(Machine m) => Mult[(int)m];

        /// <summary>False while every timed machine is at 1x (the timer system then does nothing).</summary>
        public static bool AnyTimedChanged
        {
            get
            {
                for (int i = 0; i < Mult.Length; i++)
                    if (i != (int)Machine.Drill && Mult[i] != 1f) return true;
                return false;
            }
        }

        public static void Set(Machine m, float value)
        {
            Mult[(int)m] = value;
            Version++;
        }

        public static Machine Of(ObjectID id)
        {
            switch (id)
            {
                case ObjectID.Furnace: return Machine.Furnace;
                case ObjectID.SmelterKiln: return Machine.SmelterKiln;
                case ObjectID.GlassSmelter: return Machine.GlassSmelter;
                case ObjectID.FuryForge: return Machine.FuryForge;
                case ObjectID.CookingPot: return Machine.CookingPot;
                case ObjectID.TableSaw: return Machine.TableSaw;
                case ObjectID.EggIncubator: return Machine.EggIncubator;
                default: return Machine.Other;
            }
        }

        /// <summary>Multiplier for a timed machine.</summary>
        public static float For(ObjectID id) => Mult[(int)Of(id)];

        /// <summary>"3x" -> 3, "Instant" -> <see cref="Instant"/>. Unparseable input falls back to 1 (vanilla).</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim();
            if (s.Equals("Instant", System.StringComparison.OrdinalIgnoreCase)) return Instant;
            s = s.TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
        }

        public static string Describe(float m) => m >= Instant ? "instant" : m.ToString("0.##", CultureInfo.InvariantCulture) + "x";
    }
}
