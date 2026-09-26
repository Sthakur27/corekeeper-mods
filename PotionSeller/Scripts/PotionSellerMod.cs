using ModSettingsMenu.Settings;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PotionSeller
{
    /// <summary>
    /// Potion Seller: the Caveling Merchant stocks every potion (20 each per restock) at fixed coin prices.
    ///
    /// Two independent changes:
    ///  1. Merchant list + inventory size (<see cref="MerchantStock"/>): applied to the CavelingMerchant
    ///     prefab entity in every world via API.Authoring.OnObjectTypeAdded (fires from ModPostConverter,
    ///     after conversion, so the buffers exist), and to merchants already living in a save by
    ///     <see cref="Systems.MerchantStockSystem"/> (server). The buy window grid is patched to match
    ///     (<see cref="Patches.BuyUIPatches"/>).
    ///  2. Prices (<see cref="PotionPrices"/>): sellValue / buyValueMultiplier on each potion, written into
    ///     the prefab ObjectInfo (authoring hook + PugDatabase.objectsByType, so the database blob bakes them),
    ///     into SDK-style ObjectAuthoring results (Harmony), and straight into the built blob by
    ///     <see cref="Systems.PotionPriceSystem"/> (both worlds, re-applied when the multiplier changes).
    /// </summary>
    public sealed class PotionSellerMod : IMod
    {
        public const string Name = "PotionSeller";
        public const string Version = "1.0.0";

        private static bool _appliedViaDatabase;

        private SettingHandle<string> _price;
        private SettingHandle<int> _stock;

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded: {PotionPrices.Potions.Length} potions on the Caveling Merchant, grid {MerchantStock.Columns}x{MerchantStock.Rows}.");
        }

        public void Init()
        {
            ModSettings.Section(this)
                .Hint("The Caveling Merchant sells every potion. Price multiplier scales the buy prices (1x = 50..500 coins); stock is how many of each he carries per restock. Both apply live; stock changes show up at the next restock. In multiplayer the host's values count.")
                .Choice(out _price, "Potion price multiplier", PotionSellerConfig.PriceLadder, PotionSellerConfig.DefaultPriceToken)
                .Stepper(out _stock, "Potions per restock", PotionSellerConfig.MinStock, PotionSellerConfig.MaxStock, PotionSellerConfig.DefaultStock)
                .Build();

            PotionSellerConfig.SetPriceMultiplier(PotionSellerConfig.ParseMultiplier(_price.Value));
            PotionSellerConfig.SetStock(_stock.Value);
            _price.OnChanged += token =>
            {
                PotionSellerConfig.SetPriceMultiplier(PotionSellerConfig.ParseMultiplier(token));
                ApplyViaDatabase();
                Debug.Log($"[{Name}] price multiplier set to {PotionSellerConfig.PriceMultiplier:0.##}x");
            };
            _stock.OnChanged += v =>
            {
                PotionSellerConfig.SetStock(v);
                Debug.Log($"[{Name}] stock per restock set to {PotionSellerConfig.Stock}");
            };

            Debug.Log($"[{Name}] price multiplier {PotionSellerConfig.PriceMultiplier:0.##}x, stock {PotionSellerConfig.Stock}.");
        }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (authoring == null) return;

            // Potion prefabs (old-style authoring keeps one ObjectInfo instance; edit it in place).
            if (authoring.TryGetComponent<EntityMonoBehaviourData>(out var data) && PotionPrices.Apply(data.objectInfo))
            {
                Debug.Log($"[{Name}] {data.objectInfo.objectID}: sellValue {data.objectInfo.sellValue}, buy x{data.objectInfo.buyValueMultiplier:0.##} (authoring).");
            }

            // PugDatabase.objectsByType holds the same ObjectInfo instances and is populated before conversion;
            // fix them on the first callback and again right before the database blob is baked.
            if (!_appliedViaDatabase || authoring.TryGetComponent<PugDatabaseAuthoring>(out _))
            {
                _appliedViaDatabase = true;
                ApplyViaDatabase();
            }

            // The merchant prefab entity (already converted: ModPostConverter is a PostConverter).
            if (entity != Entity.Null && em.Exists(entity) && MerchantStock.IsTarget(em, entity))
            {
                if (MerchantStock.Apply(em, entity, PotionSellerConfig.Stock, out _))
                {
                    Debug.Log($"[{Name}] CavelingMerchant prefab: {em.GetBuffer<MerchantItemInfoBuffer>(entity).Length} items, {em.GetBuffer<ContainedObjectsBuffer>(entity).Length} slots ({em.World.Name}).");
                }
            }
        }

        private static void ApplyViaDatabase()
        {
            if (PugDatabase.objectsByType == null) return;
            foreach (var potion in PotionPrices.Potions)
            {
                if (PugDatabase.TryGetObjectInfo(potion.id, out var info)) PotionPrices.Apply(info);
            }
        }
    }
}
