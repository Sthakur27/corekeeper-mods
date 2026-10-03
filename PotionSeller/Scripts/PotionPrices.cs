using Unity.Mathematics;

namespace PotionSeller
{
    /// <summary>
    /// What the merchants sell: potions + Recall Idol (Caveling Merchant), grenades and bombs (Slime Merchant) and their base buy prices (Ancient Coins).
    ///
    /// The game derives both prices from one database field, EntityObjectInfo.sellValue
    /// (InventoryUtility.GetCoinValue):
    ///   buy  = round(sellValue * 5 * buyValueMultiplier)
    ///   sell = sellValue per unit (x stack size for stackables)
    /// So a buy price P is stored as sellValue = P / 5 with buyValueMultiplier = the global price
    /// multiplier. The sell price becomes P / 5 and does NOT follow the multiplier. All base prices are
    /// multiples of 5 so the division is exact.
    /// </summary>
    public static class PotionPrices
    {
        public struct Entry
        {
            public ObjectID id;
            public int buyPrice;
            /// <summary>Boss that must be beaten before the item is sold (None = always).</summary>
            public ObjectID unlockedBy;
            /// <summary>Stock per restock; 0 = the Stock setting.</summary>
            public int stock;
            public Entry(ObjectID id, int buyPrice, ObjectID unlockedBy = ObjectID.None, int stock = 0)
            {
                this.id = id; this.buyPrice = buyPrice; this.unlockedBy = unlockedBy; this.stock = stock;
            }
        }

        /// <summary>Merchant order: cheap first. Edit prices here.</summary>
        public static readonly Entry[] Potions =
        {
            new Entry(ObjectID.HealingPotion, 50),
            new Entry(ObjectID.ManaPotion, 50),
            new Entry(ObjectID.PoisonAidPotion, 75),
            new Entry(ObjectID.BurnResistancePotion, 75),
            new Entry(ObjectID.KeenPotion, 150),
            new Entry(ObjectID.MagicPotion, 150),
            new Entry(ObjectID.StoneskinPotion, 200),
            new Entry(ObjectID.EnragePotion, 200),
            new Entry(ObjectID.GuardiansPotion, 250),
            new Entry(ObjectID.MinionPotion, 250),
            new Entry(ObjectID.GreaterHealingPotion, 300),
            new Entry(ObjectID.GreaterManaPotion, 300),
            new Entry(ObjectID.UnusualPotion, 500),
            // Not a potion, but sold by the same merchant on request: teleports you home.
            new Entry(ObjectID.RecallIdol, 200),
        };

        /// <summary>
        /// The Slime Merchant sells every grenade and bomb, cheap to end game. Prices must be multiples of 5
        /// (see the class summary). Edit here.
        /// </summary>
        public static readonly Entry[] Explosives =
        {
            new Entry(ObjectID.Grenade, 10),
            new Entry(ObjectID.SmallBomb, 10),
            new Entry(ObjectID.Bomb, 15),
            new Entry(ObjectID.FireGrenade, 20),
            new Entry(ObjectID.PoisonGrenade, 20),
            new Entry(ObjectID.OilGrenade, 25),
            new Entry(ObjectID.StunGrenade, 25),
            new Entry(ObjectID.LargeBomb, 30),
            new Entry(ObjectID.CharmGrenade, 35),
            new Entry(ObjectID.SulfurBomb, 40),
            new Entry(ObjectID.ProximityBomb, 50),
            new Entry(ObjectID.SeekerBomb, 60),
            new Entry(ObjectID.BlunderBomb, 70),
            new Entry(ObjectID.BigGrenade, 80),
            new Entry(ObjectID.VoidBomb, 100),
        };

        /// <summary>
        /// The Fishing Merchant sells each Titan's summoning item once that Titan is beaten (see
        /// MerchantStockSystem: killed in this world, or a connected player holds its soul):
        /// Azeos = Large Shiny Glimmering Object, Omoroth = Expert Lure, Ra-Akar = Thumper.
        /// </summary>
        public static readonly Entry[] TitanSummons =
        {
            new Entry(ObjectID.LargeShinyGlimmeringObject, 2000, ObjectID.BirdBoss, 3),
            new Entry(ObjectID.BaitOctopusBoss, 2000, ObjectID.OctopusBoss, 3),
            new Entry(ObjectID.Thumper, 2000, ObjectID.ScarabBoss, 3),
        };

        /// <summary>The soul a player gets for beating each Titan.</summary>
        public static SoulID SoulFor(ObjectID boss)
        {
            switch (boss)
            {
                case ObjectID.BirdBoss: return SoulID.SoulOfAzeos;
                case ObjectID.OctopusBoss: return SoulID.SoulOfOmoroth;
                case ObjectID.ScarabBoss: return SoulID.SoulOfScarab;
                default: return SoulID.None;
            }
        }

        /// <summary>Every item whose price this mod sets (all merchants).</summary>
        public static readonly Entry[] All = Concat(Concat(Potions, Explosives), TitanSummons);

        /// <summary>The list a merchant sells, or null for merchants this mod leaves alone.</summary>
        public static Entry[] ForMerchant(ObjectID merchant)
        {
            switch (merchant)
            {
                case ObjectID.CavelingMerchant: return Potions;
                case ObjectID.SlimeMerchant: return Explosives;
                case ObjectID.FishingMerchant: return TitanSummons;
                default: return null;
            }
        }

        private static Entry[] Concat(Entry[] a, Entry[] b)
        {
            var r = new Entry[a.Length + b.Length];
            a.CopyTo(r, 0);
            b.CopyTo(r, a.Length);
            return r;
        }

        public static bool TryGetBuyPrice(ObjectID id, out int price)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id) { price = All[i].buyPrice; return true; }
            }
            price = 0;
            return false;
        }

        /// <summary>sellValue that yields the base buy price at 1x.</summary>
        public static int SellValueFor(int buyPrice) => math.max(1, (int)math.round(buyPrice / 5f));

        /// <summary>Applies price + multiplier to a managed ObjectInfo (prefab / PugDatabase.objectsByType). True if changed.</summary>
        public static bool Apply(ObjectInfo info)
        {
            if (info == null || !TryGetBuyPrice(info.objectID, out int price)) return false;
            int sell = SellValueFor(price);
            float mult = PotionSellerConfig.PriceMultiplier;
            if (info.sellValue == sell && info.buyValueMultiplier == mult) return false;
            info.sellValue = sell;
            info.buyValueMultiplier = mult;
            return true;
        }

        /// <summary>Applies price + multiplier to a blob EntityObjectInfo. True if changed.</summary>
        public static bool Apply(ref PugDatabase.EntityObjectInfo info, ObjectID expected)
        {
            if (info.objectID != expected || !TryGetBuyPrice(expected, out int price)) return false;
            int sell = SellValueFor(price);
            float mult = PotionSellerConfig.PriceMultiplier;
            if (info.sellValue == sell && info.buyValueMultiplier == mult) return false;
            info.sellValue = sell;
            info.buyValueMultiplier = mult;
            return true;
        }
    }
}
