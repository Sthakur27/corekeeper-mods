using System.Collections.Generic;

namespace MerchantRestock
{
    /// <summary>Choices, the merchant list and the live per-merchant caps the system reads.</summary>
    public static class RestockSettings
    {
        public const string Vanilla = "Vanilla (25-35 min)";
        public const string SameAsAll = "Same as all";

        public static readonly string[] Ladder = { Vanilla, "15 min", "10 min", "5 min", "2 min", "1 min", "30 sec" };
        public static readonly string[] MerchantLadder = { SameAsAll, Vanilla, "15 min", "10 min", "5 min", "2 min", "1 min", "30 sec" };

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

        // ---------------------------------------------------------------- minimum stock

        public const string FloorOff = "Off (vanilla)";
        public static readonly string[] FloorLadder = { FloorOff, "10", "25", "50", "99" };

        /// <summary>Entries the game stocks fewer than this per restock are premium goods and are left alone.</summary>
        public const int RareBelow = 4;

        /// <summary>Highest stock a stackable item is raised to (the largest stock any of our merchants uses).</summary>
        public const int MaxFloor = 99;

        /// <summary>Minimum stock per stackable item, 0 = off (vanilla amounts).</summary>
        public static int StockFloor { get; private set; }

        /// <summary>Raised when <see cref="StockFloor"/> changes; Sid's Overhaul forwards it to the other merchant mods.</summary>
        public static event System.Action FloorChanged;

        public static void SetFloor(int floor)
        {
            if (floor < 0) floor = 0;
            if (floor > MaxFloor) floor = MaxFloor;
            if (floor == StockFloor) return;
            StockFloor = floor;
            FloorChanged?.Invoke();
        }

        /// <summary>"25" -> 25; "Off (vanilla)" or unknown -> 0.</summary>
        public static int ParseFloor(string token) => int.TryParse(token, out int n) && n > 0 ? n : 0;

        /// <summary>Items Fish Seller owns on the Fishing / Caveling Merchant: it manages their amounts itself.</summary>
        public static bool IsFishSellerItem(ObjectID id)
            => (id >= ObjectID.OrangeCaveGuppy && id <= ObjectID.RiftianLampfish) || id == ObjectID.GoldenLarvaMeat;

        /// <summary>
        /// The amount a list entry should have: its vanilla amount, raised to the floor when it is a regular
        /// stackable entry (not premium, not gated at 0, not unstackable).
        /// </summary>
        public static int TargetAmount(int vanilla, bool stackable, int floor)
        {
            if (floor <= 0 || !stackable || vanilla < RareBelow) return vanilla;
            return vanilla >= floor ? vanilla : floor;
        }

        private static readonly Dictionary<ObjectID, int> _caps = new Dictionary<ObjectID, int>();

        /// <summary>Longest restock time in seconds for that merchant, 0 = vanilla.</summary>
        public static int CapFor(ObjectID merchant) => _caps.TryGetValue(merchant, out int s) ? s : 0;

        /// <summary>True when at least one merchant has a cap; false (the default, all vanilla) lets the system idle.</summary>
        public static bool AnyCap { get; private set; }

        public static void SetCap(ObjectID merchant, int seconds)
        {
            _caps[merchant] = seconds;
            bool any = false;
            foreach (var cap in _caps.Values) if (cap > 0) { any = true; break; }
            AnyCap = any;
        }

        /// <summary>"5 min" -> 300, "30 sec" -> 30; vanilla or unknown -> 0.</summary>
        public static int ParseSeconds(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            int space = token.IndexOf(' ');
            if (space <= 0 || !int.TryParse(token.Substring(0, space), out int n)) return 0;
            if (token.EndsWith(" min")) return n * 60;
            if (token.EndsWith(" sec")) return n;
            return 0;
        }
    }
}
