using System.Linq;
using ModOptions;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossMerchants
{
    /// <summary>
    /// More NPCs (alpha, internal id BossMerchants): eight new merchant NPCs, each summoned by an idol that a boss drops.
    ///
    ///  - Objects (bundle): per merchant an NPC copied from a vanilla merchant prefab and an idol copied from a
    ///    vanilla merchant idol (Unity/Editor/Builder). Graphics reuse the vanilla merchant / idol graphics
    ///    with our own sheets swapped in (<see cref="Visuals"/>).
    ///  - Drops: <see cref="Systems.IdolDropSystem"/> (boss dies -> its idol).
    ///  - Moving in: <see cref="Systems.MerchantSpawnSystem"/> (idol in a room with a free bed -> merchant), and
    ///    <see cref="Patches.RequiredIdolPatch"/> so vanilla re-homes them like its own merchants.
    ///  - Shop: <see cref="MerchantStock"/> on the prefab (here, OnObjectTypeAdded) and on live merchants
    ///    (<see cref="Systems.MerchantStockSystem"/>); prices from <see cref="Catalog"/> written into
    ///    the prefab ObjectInfo, SDK authoring (Harmony) and the database blob (<see cref="Systems.PriceSystem"/>).
    ///    The buy window is widened to 8x3 with the same shared layout code as Potion Seller / Fish Seller.
    /// </summary>
    public sealed class BossMerchantsMod : IMod
    {
        public const string Name = "BossMerchants";
        public const string Version = "0.1.1";

        private static bool _appliedViaDatabase;
        private static Setting<string> _price;
        private static Setting<int> _stock;

        public const string SettingsHint = "Bosses drop a merchant idol: Morpha (blocks), Omoroth (decor), Igneous (explosives), Ivy (potions), Ra-Akar (pet eggs). Place it in a room with a free bed and the merchant moves in. Price multiplier scales buy prices; stock applies to potions and explosives (blocks, decor and eggs have fixed amounts). In multiplayer the host's values count.";

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded: {Catalog.Merchants.Length} boss merchants, grid {MerchantStock.Columns}x{MerchantStock.Rows}.");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "More NPCs").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _price, "Price multiplier", BossMerchantsConfig.PriceLadder, BossMerchantsConfig.DefaultPriceToken)
                .Stepper(out _stock, "Potions and explosives per restock", BossMerchantsConfig.MinStock, BossMerchantsConfig.MaxStock, BossMerchantsConfig.DefaultStock);

            BossMerchantsConfig.SetPriceMultiplier(BossMerchantsConfig.ParseMultiplier(_price.Value));
            BossMerchantsConfig.SetStock(_stock.Value);
            _price.OnChanged += token =>
            {
                BossMerchantsConfig.SetPriceMultiplier(BossMerchantsConfig.ParseMultiplier(token));
                ApplyViaDatabase();
                Debug.Log($"[{Name}] price multiplier set to {BossMerchantsConfig.PriceMultiplier:0.##}x");
            };
            _stock.OnChanged += v =>
            {
                BossMerchantsConfig.SetStock(v);
                Debug.Log($"[{Name}] stock per restock set to {BossMerchantsConfig.Stock}");
            };
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) => Visuals.OnAssetLoaded(obj);

        public void Update() { }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (authoring == null) return;

            if (authoring.TryGetComponent<EntityMonoBehaviourData>(out var data) && Catalog.Apply(data.objectInfo))
                Debug.Log($"[{Name}] {data.objectInfo.objectID}: sellValue {data.objectInfo.sellValue} (authoring).");

            // PugDatabase.objectsByType holds the same ObjectInfo instances and is populated before conversion;
            // fix them on the first callback and again right before the database blob is baked.
            if (!_appliedViaDatabase || authoring.TryGetComponent<PugDatabaseAuthoring>(out _))
            {
                _appliedViaDatabase = true;
                Catalog.Resolve(final: false);
                ApplyViaDatabase();
            }

            // Our merchant prefab entity (already converted: ModPostConverter is a PostConverter). Keyed by object
            // name, so it works before the ID lookup succeeds.
            if (entity == Entity.Null || !em.Exists(entity)) return;
            if (!authoring.TryGetComponent<ObjectAuthoring>(out var oa)) return;
            var merchant = Catalog.ByMerchantName(oa.objectName);
            if (merchant == null || !em.HasBuffer<MerchantItemInfoBuffer>(entity)) return;
            Catalog.Resolve(final: false);
            if (merchant.MerchantId == ObjectID.None && em.HasComponent<ObjectDataCD>(entity))
                merchant.MerchantId = em.GetComponentData<ObjectDataCD>(entity).objectID;
            if (MerchantStock.Apply(em, entity, merchant, BossMerchantsConfig.Stock, out _))
                Debug.Log($"[{Name}] {merchant.Key} merchant prefab: {em.GetBuffer<MerchantItemInfoBuffer>(entity, true).Length} items ({em.World.Name}).");
        }

        private static void ApplyViaDatabase()
        {
            if (PugDatabase.objectsByType == null) return;
            foreach (var item in Catalog.All)
                if (PugDatabase.TryGetObjectInfo(item.id, out var info)) Catalog.Apply(info);
        }
    }
}
