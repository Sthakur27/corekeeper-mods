using System.Globalization;

namespace BossMerchants
{
    /// <summary>Live tunables (Mod Options). The systems re-read these, so changes apply without a restart.</summary>
    public static class BossMerchantsConfig
    {
        /// <summary>Global buy-price multiplier choices, in cycle order. Only the BUY price scales.</summary>
        public static readonly string[] PriceLadder = { "0.25x", "0.5x", "0.75x", "1x", "1.5x", "2x", "3x", "4x" };
        public const string DefaultPriceToken = "1x";

        /// <summary>Stock per restock for items without their own fixed amount (potions, explosives).</summary>
        public const int DefaultStock = 20;
        public const int MinStock = 1;
        public const int MaxStock = 99;

        /// <summary>NPC max-health choices. Vanilla merchants (and these copies of them) have 100,000.</summary>
        public const string VanillaHealthToken = "Default (100,000)";
        public static readonly string[] HealthLadder = { "1,000", "10,000", VanillaHealthToken, "1,000,000" };

        /// <summary>Max health for the merchants, 0 = default (the prefab's own value).</summary>
        public static int HealthTarget { get; private set; }

        public static void SetHealthTarget(int hp) => HealthTarget = hp > 0 ? hp : 0;

        /// <summary>"10,000" -> 10000; "Default (100,000)" or unknown -> 0.</summary>
        public static int ParseHealth(string token)
        {
            if (string.IsNullOrEmpty(token) || token == VanillaHealthToken) return 0;
            return int.TryParse(token.Replace(",", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v > 0 ? v : 0;
        }

        public static float PriceMultiplier { get; private set; } = ParseMultiplier(DefaultPriceToken);
        public static int Stock { get; private set; } = DefaultStock;

        public static void SetPriceMultiplier(float m)
        {
            if (m <= 0f || float.IsNaN(m) || float.IsInfinity(m)) m = 1f;
            PriceMultiplier = m;
        }

        /// <summary>Entries with a fixed stock below this are premium goods and ignore the floor.</summary>
        public const int RareBelow = 4;

        /// <summary>Minimum stock per item, 0 = off. Set by Sid's Overhaul's "Minimum stock per item" (Merchant Restock page); the standalone mod never sets it.</summary>
        public static int StockFloor { get; private set; }

        public static void SetStockFloor(int floor) => StockFloor = floor < 0 ? 0 : floor;

        /// <summary>
        /// Stock for one catalog entry: its fixed stock (or the Stock setting when it has none), raised to the
        /// floor unless it is a premium entry with a fixed stock below <see cref="RareBelow"/>. Boss merchants
        /// only sell stackable items or one-per-slot items listed with a fixed stock of 1.
        /// </summary>
        public static int AmountFor(int entryStock, int stock)
        {
            int amount = entryStock > 0 ? entryStock : stock;
            if (StockFloor <= 0 || (entryStock > 0 && entryStock < RareBelow)) return amount;
            return amount >= StockFloor ? amount : StockFloor;
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
