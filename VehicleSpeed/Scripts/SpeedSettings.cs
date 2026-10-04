using System.Globalization;

namespace VehicleSpeed
{
    /// <summary>The two multipliers. Settings store tokens like "3x"; the system reads the parsed floats.</summary>
    public static class SpeedSettings
    {
        public static readonly string[] Ladder = { "1x", "2x", "3x", "5x", "10x" };
        public const string DefaultToken = "1x";

        public static float Boat { get; set; } = 1f;
        public static float GoKart { get; set; } = 1f;

        /// <summary>Bumped on every change so the system re-applies.</summary>
        public static int Version { get; set; }

        /// <summary>"3x" -> 3. Unparseable input falls back to 1 (vanilla).</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
        }
    }
}
