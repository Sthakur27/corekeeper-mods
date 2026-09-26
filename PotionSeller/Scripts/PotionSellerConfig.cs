using System.Globalization;

namespace PotionSeller
{
    /// <summary>
    /// Live tunables. The systems re-read these every few seconds, so a change in the Mod Settings
    /// menu applies without a restart. The constants here are the defaults (and the only place to edit them).
    /// </summary>
    public static class PotionSellerConfig
    {
        /// <summary>Global buy-price multiplier choices, in cycle order. Only the BUY price scales.</summary>
        public static readonly string[] PriceLadder = { "0.25x", "0.5x", "0.75x", "1x", "1.5x", "2x", "3x", "4x" };
        public const string DefaultPriceToken = "1x";

        /// <summary>How many of each potion the merchant carries after a restock.</summary>
        public const int DefaultStock = 20;
        public const int MinStock = 1;
        public const int MaxStock = 99;

        public static float PriceMultiplier { get; private set; } = ParseMultiplier(DefaultPriceToken);
        public static int Stock { get; private set; } = DefaultStock;

        public static void SetPriceMultiplier(float m)
        {
            if (m <= 0f || float.IsNaN(m) || float.IsInfinity(m)) m = 1f;
            PriceMultiplier = m;
        }

        public static void SetStock(int stock)
        {
            if (stock < MinStock) stock = MinStock;
            if (stock > MaxStock) stock = MaxStock;
            Stock = stock;
        }

        /// <summary>"0.5x" -> 0.5. Unparseable input falls back to 1.</summary>
        public static float ParseMultiplier(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
        }
    }
}
