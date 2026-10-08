using System.Globalization;

namespace FishSeller
{
    /// <summary>
    /// What this mod adds to which merchant, and the live tunables (set from Mod Options).
    ///
    /// Prices are the game's own: buy = max(1, sellValue) x 5 x buyValueMultiplier (InventoryUtility.GetCoinValue),
    /// so a fish costs 5x what the Fishing Merchant pays you for it. The price multiplier scales only the
    /// buy price (it multiplies buyValueMultiplier, see <see cref="Systems.ShopPriceSystem"/>); sell prices stay vanilla.
    /// Items the game will not trade (Legendary rarity or CantBeSoldCD, buy price 0) are dropped from the
    /// list at runtime instead of showing an unbuyable slot.
    /// </summary>
    public static class ShopItems
    {
        /// <summary>Every fish in the game (ObjectID 9700..9743), in the game's own order.</summary>
        public static readonly ObjectID[] Fish =
        {
            ObjectID.OrangeCaveGuppy, ObjectID.BlueCaveGuppy, ObjectID.RockJaw, ObjectID.GemCrab, ObjectID.DaggerFin,
            ObjectID.PinkPalaceFish, ObjectID.TealPalaceFish, ObjectID.CrownSquid, ObjectID.YellowBlisterHead,
            ObjectID.GreenBlisterHead, ObjectID.DevilWorm, ObjectID.VampireEel, ObjectID.MoldShark, ObjectID.RotFish,
            ObjectID.BlackSteelUrchin, ObjectID.AzureFeatherFish, ObjectID.EmeraldFeatherFish, ObjectID.SpiritVeil,
            ObjectID.AstralJelly, ObjectID.BottomTracer, ObjectID.SilverTorrentDart, ObjectID.GoldenTorrentDart,
            ObjectID.PinkCoralotl, ObjectID.WhiteCoralotl, ObjectID.SolidSpikeback, ObjectID.SandySpikeback,
            ObjectID.GreyDuneTail, ObjectID.BrownDuneTail, ObjectID.TornisKingfish, ObjectID.DarkLavaEater,
            ObjectID.BrightLavaEater, ObjectID.VerdantDragonfish, ObjectID.ElderDragonfish, ObjectID.StarlightNautilus,
            ObjectID.AngleFish, ObjectID.Deepstalker, ObjectID.CosmicForm, ObjectID.GoldenAngleFish,
            ObjectID.GoldenDeepstalker, ObjectID.TerraTrilobite, ObjectID.LithoTrilobite, ObjectID.PinkhornPico,
            ObjectID.GreenhornPico, ObjectID.RiftianLampfish,
        };

        /// <summary>Shiny Larva Meat (ObjectID.GoldenLarvaMeat), sold by the Caveling Merchant.</summary>
        public static readonly ObjectID[] Caveling = { ObjectID.GoldenLarvaMeat };

        /// <summary>The items this mod adds to <paramref name="merchant"/>, or null.</summary>
        public static ObjectID[] ForMerchant(ObjectID merchant)
        {
            switch (merchant)
            {
                case ObjectID.FishingMerchant: return Fish;
                case ObjectID.CavelingMerchant: return Caveling;
                default: return null;
            }
        }

        public static bool IsOurs(ObjectID id)
        {
            if (id >= ObjectID.OrangeCaveGuppy && id <= ObjectID.RiftianLampfish) return true;
            return id == ObjectID.GoldenLarvaMeat;
        }

        /// <summary>
        /// Legendary items: the game prices every Legendary item at 0 (InventoryUtility.GetCoinValue), so the
        /// vanilla buy path can never sell them and that code is Burst-compiled (not patchable). These get a
        /// fixed price and are bought through our own command instead (<see cref="Patches.SpecialBuy"/>).
        /// Price is in Ancient Coins at 1x; the price multiplier applies.
        /// </summary>
        public static int SpecialBasePrice(ObjectID id)
        {
            switch (id)
            {
                case ObjectID.StarlightNautilus: return 500;
                default: return 0;
            }
        }

        public static bool IsSpecial(ObjectID id) => SpecialBasePrice(id) > 0;

        /// <summary>Current price of a special item (0 if it is not one).</summary>
        public static int SpecialPrice(ObjectID id)
        {
            int p = SpecialBasePrice(id);
            return p > 0 ? System.Math.Max(1, (int)System.Math.Round(p * PriceMultiplier)) : 0;
        }

        /// <summary>Every item whose buy price this mod scales.</summary>
        public static readonly ObjectID[] All = Concat(Fish, Caveling);

        private static ObjectID[] Concat(ObjectID[] a, ObjectID[] b)
        {
            var r = new ObjectID[a.Length + b.Length];
            a.CopyTo(r, 0);
            b.CopyTo(r, a.Length);
            return r;
        }

        // ---------------------------------------------------------------- settings

        public static readonly string[] PriceLadder = { "0.25x", "0.5x", "0.75x", "1x", "1.5x", "2x", "3x", "4x" };
        public const string DefaultPriceToken = "1x";

        public const int DefaultStock = 10;
        public const int MinStock = 1;
        public const int MaxStock = 99;

        public static float PriceMultiplier { get; private set; } = 1f;
        public static int Stock { get; private set; } = DefaultStock;

        public static void SetPriceMultiplier(string token) => PriceMultiplier = ParseMultiplier(token);

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
