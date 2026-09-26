using Unity.Mathematics;

namespace PotionSeller
{
    /// <summary>
    /// The potions the merchant sells and their base buy prices (Ancient Coins).
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
            public Entry(ObjectID id, int buyPrice) { this.id = id; this.buyPrice = buyPrice; }
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
        };

        public static bool TryGetBuyPrice(ObjectID id, out int price)
        {
            for (int i = 0; i < Potions.Length; i++)
            {
                if (Potions[i].id == id) { price = Potions[i].buyPrice; return true; }
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
