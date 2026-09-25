using System.Globalization;

namespace BetterFishingLoot
{
    /// <summary>
    /// The one tunable: how much the weight of every "rare" entry in the fishing (non-fish) loot
    /// tables is multiplied. Settings store a token like "5x"; systems read <see cref="Multiplier"/>
    /// and re-apply whenever <see cref="Version"/> changes.
    /// </summary>
    public static class FishingLootConfig
    {
        /// <summary>Choices offered in the Mod Settings menu, in cycle order. 1x = vanilla.</summary>
        public static readonly string[] Ladder = { "1x", "2x", "3x", "5x", "10x", "25x" };

        public const string DefaultToken = "5x";

        public static float Multiplier { get; private set; } = Parse(DefaultToken);

        /// <summary>Bumped on every change so per-world systems know to re-apply.</summary>
        public static int Version { get; private set; }

        public static void Set(float value)
        {
            if (value <= 0f) value = 1f;
            if (value == Multiplier) return;
            Multiplier = value;
            Version++;
        }

        /// <summary>"10x" → 10. Unparseable input falls back to 1 (vanilla).</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
        }
    }
}
