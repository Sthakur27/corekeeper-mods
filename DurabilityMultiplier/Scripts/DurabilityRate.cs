using System.Globalization;

namespace DurabilityMultiplier
{
    /// <summary>
    /// The configured durability-loss rate. Settings store a token like "0.5x"; the ECS system
    /// reads the parsed float from <see cref="Value"/> every tick, so keep that path trivial.
    /// </summary>
    public static class DurabilityRate
    {
        /// <summary>Choices offered in the Mod Settings menu, in cycle order. 0x = never loses durability.</summary>
        public static readonly string[] Ladder = { "0x", "0.1x", "0.25x", "0.5x", "0.75x", "1x" };

        public const string DefaultToken = "0.5x";

        /// <summary>Fraction of vanilla durability loss to apply (0..1). 1 = vanilla, untouched.</summary>
        public static float Value { get; private set; } = Parse(DefaultToken);

        public static void Set(float rate)
        {
            if (rate < 0f) rate = 0f;
            if (rate > 1f) rate = 1f;
            Value = rate;
        }

        /// <summary>"0.5x" → 0.5. Unparseable input falls back to 1 (vanilla).</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v >= 0f ? v : 1f;
        }
    }
}
