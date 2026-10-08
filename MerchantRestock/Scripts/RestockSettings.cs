using System.Collections.Generic;

namespace MerchantRestock
{
    /// <summary>Choices, the merchant list and the live per-merchant caps the system reads.</summary>
    public static class RestockSettings
    {
        public const string Vanilla = "Vanilla (25-35 min)";
        public const string SameAsAll = "Same as all";

        public static readonly string[] Ladder = { Vanilla, "15 min", "10 min", "5 min", "2 min", "1 min" };
        public static readonly string[] MerchantLadder = { SameAsAll, Vanilla, "15 min", "10 min", "5 min", "2 min", "1 min" };

        /// <summary>Upper end of the game's random restock time (MerchantBuyInventorySystem: 1500-2100 s).</summary>
        public const int VanillaMaxSeconds = 2100;

        public static readonly (ObjectID id, string label)[] Merchants =
        {
            (ObjectID.CavelingMerchant, "Caveling Merchant"),
            (ObjectID.SlimeMerchant, "Slime Merchant"),
            (ObjectID.FishingMerchant, "Fishing Merchant"),
            (ObjectID.CrystalMerchant, "Crystal Merchant"),
            (ObjectID.VoidMerchant, "Void Merchant"),
            (ObjectID.SeasonalMerchant, "Seasonal Merchant"),
        };

        private static readonly Dictionary<ObjectID, int> _caps = new Dictionary<ObjectID, int>();

        /// <summary>Longest restock time in seconds for that merchant, 0 = vanilla.</summary>
        public static int CapFor(ObjectID merchant) => _caps.TryGetValue(merchant, out int s) ? s : 0;

        public static void SetCap(ObjectID merchant, int seconds) => _caps[merchant] = seconds;

        /// <summary>"5 min" -> 300; vanilla or unknown -> 0.</summary>
        public static int ParseSeconds(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            int space = token.IndexOf(' ');
            if (space <= 0 || !int.TryParse(token.Substring(0, space), out int minutes) || !token.EndsWith(" min")) return 0;
            return minutes * 60;
        }
    }
}
