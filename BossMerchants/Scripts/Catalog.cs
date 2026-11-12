using PugMod;
using Unity.Mathematics;

namespace BossMerchants
{
    /// <summary>
    /// The eight boss merchants: who they are, which boss(es) drop their idol and what they sell.
    /// Keys and object names of the first five never change (saves store objects by name).
    ///
    /// Each merchant and its idol are our own objects (prefabs copied from vanilla merchants / idols, built in
    /// Unity, see Unity/Editor/Builder). Object IDs are assigned by the mod loader, so everything here is keyed by
    /// object name and resolved at runtime (<see cref="Resolve"/>).
    ///
    /// Prices (Ancient Coins) work like Potion Seller's: the game derives both prices from one database field,
    /// EntityObjectInfo.sellValue (InventoryUtility.GetCoinValue): buy = round(sellValue * 5 * buyValueMultiplier),
    /// sell = sellValue. A buy price P is stored as sellValue = P / 5 with buyValueMultiplier = the price setting.
    /// All base prices are multiples of 5 so the division is exact; the sell price is P / 5 and ignores the
    /// multiplier, so nothing can be bought and resold for profit.
    /// </summary>
    public static class Catalog
    {
        public struct Entry
        {
            public ObjectID id;
            public int buyPrice;
            /// <summary>Stock per restock; 0 = the Stock setting.</summary>
            public int stock;
            public Entry(ObjectID id, int buyPrice, int stock = 0) { this.id = id; this.buyPrice = buyPrice; this.stock = stock; }
        }

        public sealed class Merchant
        {
            public string Key;            // short theme key, also used in object names and art folders
            public string MerchantName;   // ObjectAuthoring.objectName of the merchant
            public string IdolName;       // ObjectAuthoring.objectName of the summoning idol
            public ObjectID[] Bosses;     // each drops the idol when killed
            public Entry[] Stock;
            public ObjectID MerchantId;   // resolved at runtime
            public ObjectID IdolId;
        }

        private static Entry E(ObjectID id, int price, int stock = 0) => new Entry(id, price, stock);

        public static readonly Merchant[] Merchants =
        {
            // Grout (Slick Cornerstone): dropped by SlipperySlimeBoss.
            new Merchant
            {
                Key = "Blocks", MerchantName = "BossMerchants_BlocksMerchant", IdolName = "BossMerchants_BlocksIdol",
                Bosses = new[] { ObjectID.SlipperySlimeBoss },
                Stock = new[]
                {
                    E(ObjectID.WallDirtBlock, 5, 99), E(ObjectID.WallStoneBlock, 5, 99),
                    E(ObjectID.WallClayBlock, 5, 99), E(ObjectID.WallWoodBlock, 5, 99),
                    E(ObjectID.WallStoneBricksBlock, 10, 99), E(ObjectID.WallGrassBlock, 5, 99),
                    E(ObjectID.WallMoldBlock, 5, 99), E(ObjectID.WallLimestoneBlock, 10, 99),
                    E(ObjectID.WallSandBlock, 10, 99), E(ObjectID.WallTurfBlock, 10, 99),
                    E(ObjectID.WallMeadowBlock, 10, 99), E(ObjectID.WallMossBlock, 10, 99),
                    E(ObjectID.WallDesertBlock, 10, 99), E(ObjectID.WallSnowBlock, 10, 99),
                    E(ObjectID.WallOasisBlock, 10, 99), E(ObjectID.WallLavaBlock, 15, 99),
                    E(ObjectID.WallGlassBlock, 15, 99), E(ObjectID.WallCoralBlock, 15, 99),
                    E(ObjectID.WallScarletBlock, 20, 99), E(ObjectID.WallObsidianBlock, 20, 99),
                    E(ObjectID.WallCrystalBlock, 25, 99), E(ObjectID.WallGleamWoodBlock, 25, 99),
                    E(ObjectID.WallDarkStoneBlock, 25, 99), E(ObjectID.WallGalaxiteBlock, 30, 99),
                },
            },
            // Ottoline Tidewick (Omoroth Shell Lamp): dropped by OctopusBoss.
            new Merchant
            {
                Key = "Decor", MerchantName = "BossMerchants_DecorMerchant", IdolName = "BossMerchants_DecorIdol",
                Bosses = new[] { ObjectID.OctopusBoss },
                Stock = new[]
                {
                    E(ObjectID.Candle, 25, 10), E(ObjectID.Lantern, 50, 10), E(ObjectID.SmallLantern, 50, 10),
                    E(ObjectID.Lamp, 75, 10), E(ObjectID.OrbLantern, 100, 10), E(ObjectID.StoneLantern, 100, 10),
                    E(ObjectID.CrystalLamp, 150, 10), E(ObjectID.SirenLamp, 150, 10),
                    E(ObjectID.PearlLantern, 150, 10), E(ObjectID.SoulLantern, 200, 10),
                    E(ObjectID.DecorativePot, 50, 10), E(ObjectID.ClayPot, 50, 10), E(ObjectID.CavelingPot, 50, 10),
                    E(ObjectID.Rug, 50, 10), E(ObjectID.Banner, 50, 10), E(ObjectID.LarvaskinRug, 75, 10),
                    E(ObjectID.SmallTempleRug, 150, 10), E(ObjectID.LargeTempleRug, 250, 10),
                    E(ObjectID.Painting, 100, 10), E(ObjectID.CavelingChair, 50, 10),
                    E(ObjectID.CavelingTable, 75, 10), E(ObjectID.DriftwoodStool, 75, 10),
                    E(ObjectID.DriftwoodTable, 100, 10), E(ObjectID.RuinsTable, 150, 10),
                },
            },
            // Fizzwick, the Molten Bombardier (Igneous Fuse Core): dropped by LavaSlimeBoss.
            new Merchant
            {
                Key = "Explosives", MerchantName = "BossMerchants_ExplosivesMerchant", IdolName = "BossMerchants_ExplosivesIdol",
                Bosses = new[] { ObjectID.LavaSlimeBoss },
                Stock = new[]
                {
                    E(ObjectID.Grenade, 10), E(ObjectID.SmallBomb, 10), E(ObjectID.Bomb, 15),
                    E(ObjectID.FireGrenade, 20), E(ObjectID.PoisonGrenade, 20), E(ObjectID.OilGrenade, 25),
                    E(ObjectID.StunGrenade, 25), E(ObjectID.LargeBomb, 30), E(ObjectID.CharmGrenade, 35),
                    E(ObjectID.SulfurBomb, 40), E(ObjectID.ProximityBomb, 50), E(ObjectID.SeekerBomb, 60),
                    E(ObjectID.BlunderBomb, 70), E(ObjectID.BigGrenade, 80), E(ObjectID.VoidBomb, 100),
                },
            },
            // Old Hemlock (Ivy Culture Jar): dropped by PoisonSlimeBoss.
            new Merchant
            {
                Key = "Potions", MerchantName = "BossMerchants_PotionsMerchant", IdolName = "BossMerchants_PotionsIdol",
                Bosses = new[] { ObjectID.PoisonSlimeBoss },
                Stock = new[]
                {
                    E(ObjectID.ManaPotion, 50), E(ObjectID.PoisonAidPotion, 75), E(ObjectID.BurnResistancePotion, 75),
                    E(ObjectID.HealingPotion, 100), E(ObjectID.KeenPotion, 150), E(ObjectID.MagicPotion, 150),
                    E(ObjectID.StoneskinPotion, 200), E(ObjectID.EnragePotion, 200), E(ObjectID.GuardiansPotion, 250),
                    E(ObjectID.MinionPotion, 250), E(ObjectID.GreaterHealingPotion, 300),
                    E(ObjectID.GreaterManaPotion, 300), E(ObjectID.UnusualPotion, 500), E(ObjectID.RecallIdol, 500),
                    // The food and drinks the two vanilla vending machines dispense (VendingMachineAuthoring.items),
                    // priced near what the machines charge (InventoryUtility.GetCoinValue, buy). These prices are
                    // global, so the machines charge them too. The machines' non-food items (Lucky Ring, Core
                    // Figurine, Oracle Card "Metropolis", Pest Egg, Electro-Pest Figurine) are not sold here and
                    // keep vanilla prices (Pest Egg is priced by the Eggs merchant).
                    // Vending Machine:
                    E(ObjectID.CavelingCrunch, 95), E(ObjectID.GlazedHeartberry, 80), E(ObjectID.TentacleJerky, 95),
                    E(ObjectID.CavelingCoffee, 110), E(ObjectID.HeartberrySoda, 100), E(ObjectID.CrystalWater, 85),
                    // Snacks Vending Machine:
                    E(ObjectID.AFDrink1, 40), E(ObjectID.AFDrink2, 40), E(ObjectID.AFDrink3, 40),
                    E(ObjectID.AFDrink4, 40), E(ObjectID.AFDrink5, 40), E(ObjectID.AFDrink6, 40),
                    E(ObjectID.AFBottledAnomaly, 40),
                },
            },
            // Ankhra the Clutch-Keeper (Brood Egg Idol): dropped by ScarabBoss.
            new Merchant
            {
                Key = "Eggs", MerchantName = "BossMerchants_EggsMerchant", IdolName = "BossMerchants_EggsIdol",
                Bosses = new[] { ObjectID.ScarabBoss },
                Stock = new[]
                {
                    E(ObjectID.Egg, 25, 20), E(ObjectID.PetDogEgg, 500, 3), E(ObjectID.PetCatEgg, 500, 3),
                    E(ObjectID.PetBirdEgg, 500, 3), E(ObjectID.PetBunnyEgg, 500, 3),
                    E(ObjectID.PetSlimeBlobEgg, 750, 3), E(ObjectID.PetSlipperySlimeBlobEgg, 750, 3),
                    E(ObjectID.PetPoisonSlimeBlobEgg, 750, 3), E(ObjectID.PetLavaSlimeBlobEgg, 750, 3),
                    E(ObjectID.PetMothEgg, 1000, 3), E(ObjectID.PetTardigradeEgg, 1000, 3),
                    E(ObjectID.PetFrogEgg, 1000, 3), E(ObjectID.PetPrinceSlimeBlobEgg, 1500, 3),
                    E(ObjectID.PetMeleeCatEgg, 1500, 3), E(ObjectID.PetMagicEgg, 2000, 3),
                    E(ObjectID.PetElectricEgg, 2000, 3), E(ObjectID.PetWarlockEgg, 2000, 3),
                    E(ObjectID.PetBirdBossEgg, 5000, 1),
                },
            },
            // Tridra the Haggler (Triune Idol): dropped by HydraBossNature, HydraBossSea, HydraBossDesert.
            new Merchant
            {
                Key = "Hydras", MerchantName = "BossMerchants_HydrasMerchant", IdolName = "BossMerchants_HydrasIdol",
                Bosses = new[] { ObjectID.HydraBossNature, ObjectID.HydraBossSea, ObjectID.HydraBossDesert },
                Stock = new[]
                {
                    E(ObjectID.HydraBossNatureBait, 750, 2), E(ObjectID.HydraBossSeaBait, 1000, 2),
                    E(ObjectID.HydraBossDesertBait, 1250, 2), E(ObjectID.HydraBossVoidBait, 1000, 1),
                    E(ObjectID.HydraBossNatureCraftingItem, 500, 3), E(ObjectID.HydraBossSeaCraftingItem, 600, 3),
                    E(ObjectID.HydraBossFirstCraftingItem, 750, 3), E(ObjectID.HydraBossVoidCraftingItem, 100, 20),
                    E(ObjectID.NatureGemstone, 125, 10), E(ObjectID.SeaGemstone, 125, 10),
                    E(ObjectID.DesertGemstone, 125, 10), E(ObjectID.AncientGemstone, 55, 50),
                    E(ObjectID.HydraBossNatureScanner, 1700, 1), E(ObjectID.HydraBossSeaScanner, 1900, 1),
                    E(ObjectID.HydraBossDesertScanner, 1800, 1), E(ObjectID.HydraBossVoidScanner, 500, 1),
                    E(ObjectID.HydraBossNatureTrophy, 1250, 1), E(ObjectID.HydraBossSeaTrophy, 1250, 1),
                    E(ObjectID.HydraBossDesertTrophy, 1500, 1), E(ObjectID.HydraBossVoidTrophy, 1250, 1),
                    E(ObjectID.HydraJewelryNatureRing, 215, 1), E(ObjectID.HydraJewelrySeaRing, 225, 1),
                    E(ObjectID.HydraJewelryDesertNecklace, 1000, 1), E(ObjectID.HydraWorkbench, 2750, 1),
                },
            },
            // Wren Hayfeather (Skyherd Bell): dropped by BirdBoss.
            new Merchant
            {
                Key = "FarmAnimals", MerchantName = "BossMerchants_FarmAnimalsMerchant", IdolName = "BossMerchants_FarmAnimalsIdol",
                Bosses = new[] { ObjectID.BirdBoss },
                Stock = new[]
                {
                    E(ObjectID.RolyPolyBaby, 100, 2), E(ObjectID.CowBaby, 150, 2), E(ObjectID.GoatBaby, 150, 2),
                    E(ObjectID.DodoBaby, 200, 2), E(ObjectID.CamelBaby, 300, 2), E(ObjectID.TurtleBaby, 300, 2),
                    E(ObjectID.RaptorBaby, 500, 1), E(ObjectID.CattleCage, 130, 5), E(ObjectID.Leash, 55, 3),
                    E(ObjectID.CattleFeedTray, 35, 5), E(ObjectID.Saddle, 315, 2), E(ObjectID.ImprovedSaddle, 970, 1),
                    E(ObjectID.HeartBerry, 10, 20), E(ObjectID.CritterWorm, 10, 20), E(ObjectID.GreenKelp, 10, 20),
                    E(ObjectID.WoodFence, 5, 99), E(ObjectID.WoodFenceGate, 10, 20), E(ObjectID.StoneFence, 5, 99),
                    E(ObjectID.StoneFenceGate, 35, 20), E(ObjectID.WallStrawBlock, 5, 99),
                    E(ObjectID.WovenStrawFloor, 5, 99), E(ObjectID.Milk, 10, 10), E(ObjectID.Wool, 10, 20),
                },
            },
            // Tilly the Seedgrub (Brood Seedpot): dropped by LarvaHiveBoss.
            new Merchant
            {
                Key = "Seeds", MerchantName = "BossMerchants_SeedsMerchant", IdolName = "BossMerchants_SeedsIdol",
                Bosses = new[] { ObjectID.LarvaHiveBoss },
                Stock = new[]
                {
                    E(ObjectID.RootSeed, 5, 20), E(ObjectID.HeartBerrySeed, 10, 20),
                    E(ObjectID.GlowingTulipSeed, 10, 20), E(ObjectID.BombPepperSeed, 15, 20),
                    E(ObjectID.CarrockSeed, 15, 20), E(ObjectID.PuffungiSeed, 15, 20),
                    E(ObjectID.GrubKapokSeed, 20, 20), E(ObjectID.BloatOatSeed, 20, 20),
                    E(ObjectID.CoralRootSeed, 20, 20), E(ObjectID.PewpayaSeed, 25, 20),
                    E(ObjectID.GleamRootSeed, 25, 20), E(ObjectID.PinegrappleSeed, 30, 20),
                    E(ObjectID.GrumpkinSeed, 30, 20), E(ObjectID.SunriceSeed, 30, 20),
                    E(ObjectID.LunacornSeed, 40, 10), E(ObjectID.PlanterBox, 25, 10), E(ObjectID.WaterCan, 50, 1),
                    E(ObjectID.LargeWaterCan, 250, 1), E(ObjectID.StoneSeeder, 300, 1),
                    E(ObjectID.SeedExtractor, 300, 1), E(ObjectID.Sprinkler, 400, 3),
                    E(ObjectID.SeedAndCropsPouch, 250, 1), E(ObjectID.MediumSeedAndCropsPouch, 750, 1),
                    E(ObjectID.LargeSeedAndCropsPouch, 2500, 1),
                },
            },
        };

        /// <summary>
        /// Items from Sid's other mods, sold only when that mod is installed (looked up by object name, so
        /// neither mod depends on the other). Prices must be multiples of 5.
        /// </summary>
        private static readonly (string merchantKey, string name, int buyPrice)[] Optional =
        {
            ("Potions", "WormholePotion_Potion", 2500), // Potion of Return
        };

        private static bool _resolved, _optionalResolved;

        /// <summary>
        /// Resolves merchant/idol IDs and optional items. Safe to call often. With <paramref name="final"/> false
        /// (during prefab conversion) a missing optional mod is retried later; once a world exists every mod's
        /// IDs are assigned, so a miss then means the mod isn't installed.
        /// </summary>
        public static void Resolve(bool final = true)
        {
            if (!_resolved)
            {
                bool all = true;
                foreach (var m in Merchants)
                {
                    if (m.MerchantId == ObjectID.None) m.MerchantId = API.Authoring.GetObjectID(m.MerchantName);
                    if (m.IdolId == ObjectID.None) m.IdolId = API.Authoring.GetObjectID(m.IdolName);
                    all &= m.MerchantId != ObjectID.None && m.IdolId != ObjectID.None;
                }
                _resolved = all;
            }
            if (_optionalResolved) return;
            int found = 0;
            var added = new System.Collections.Generic.List<string>();
            foreach (var (key, name, price) in Optional)
            {
                var id = API.Authoring.GetObjectID(name);
                if (id == ObjectID.None) continue;
                found++;
                var m = ByKey(key);
                if (m == null || Contains(m.Stock, id)) continue;
                var list = new Entry[m.Stock.Length + 1];
                m.Stock.CopyTo(list, 0);
                list[m.Stock.Length] = new Entry(id, price);
                m.Stock = list;
                added.Add(name);
            }
            if (found < Optional.Length && !final) return;
            _optionalResolved = true;
            _all = null;
            if (added.Count > 0)
                UnityEngine.Debug.Log($"[{BossMerchantsMod.Name}] optional items from other installed mods: {string.Join(", ", added)}.");
        }

        public static Merchant ByKey(string key)
        {
            foreach (var m in Merchants) if (m.Key == key) return m;
            return null;
        }

        public static Merchant ByMerchantId(ObjectID id)
        {
            if (id == ObjectID.None) return null;
            foreach (var m in Merchants) if (m.MerchantId == id) return m;
            return null;
        }

        public static Merchant ByMerchantName(string name)
        {
            foreach (var m in Merchants) if (m.MerchantName == name) return m;
            return null;
        }

        public static Merchant ByIdolId(ObjectID id)
        {
            if (id == ObjectID.None) return null;
            foreach (var m in Merchants) if (m.IdolId == id) return m;
            return null;
        }

        public static Merchant ByBoss(ObjectID boss)
        {
            if (boss == ObjectID.None) return null;
            foreach (var m in Merchants)
                foreach (var b in m.Bosses)
                    if (b == boss) return m;
            return null;
        }

        private static bool Contains(Entry[] list, ObjectID id)
        {
            foreach (var e in list) if (e.id == id) return true;
            return false;
        }

        private static Entry[] _all;

        /// <summary>Every item whose price this mod sets (first price wins if two merchants list an item).</summary>
        public static Entry[] All
        {
            get
            {
                if (_all != null) return _all;
                var seen = new System.Collections.Generic.HashSet<ObjectID>();
                var list = new System.Collections.Generic.List<Entry>();
                foreach (var m in Merchants)
                    foreach (var e in m.Stock)
                        if (seen.Add(e.id)) list.Add(e);
                return _all = list.ToArray();
            }
        }

        public static bool TryGetBuyPrice(ObjectID id, out int price)
        {
            var all = All;
            for (int i = 0; i < all.Length; i++)
                if (all[i].id == id) { price = all[i].buyPrice; return true; }
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
            float mult = BossMerchantsConfig.PriceMultiplier;
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
            float mult = BossMerchantsConfig.PriceMultiplier;
            if (info.sellValue == sell && info.buyValueMultiplier == mult) return false;
            info.sellValue = sell;
            info.buyValueMultiplier = mult;
            return true;
        }
    }
}
