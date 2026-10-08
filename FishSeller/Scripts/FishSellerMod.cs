using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Command;
using ModOptions;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FishSeller
{
    /// <summary>
    /// Fish Seller: the Fishing Merchant sells every fish and the Caveling Merchant sells Shiny Larva Meat,
    /// and every merchant's buy window scrolls (mouse wheel) when he has more wares than fit in it.
    ///
    ///  1. Lists (<see cref="MerchantRoom"/>): our items are added to the merchant prefabs in every world
    ///     (API.Authoring.OnObjectTypeAdded, after conversion), and to merchants living in a save by
    ///     <see cref="Systems.ShopStockSystem"/> (server), which also drops items the game will not trade,
    ///     grows every merchant's slot buffer to fit his list and keeps gated entries last.
    ///  2. Prices (<see cref="Systems.ShopPriceSystem"/>): vanilla, times the price multiplier (buy only).
    ///  3. UI (<see cref="Patches.BuyScroll"/>): the buy window shows sizeX x sizeY slots of a longer buffer;
    ///     scrolling moves the window's InventoryHandler.startPosInBuffer, which is also what a click buys
    ///     (InventoryHandler.Buy sends startPosInBuffer + slot), so no server change is needed.
    ///     <see cref="Patches.BuyUILayout"/> widens the window to 8 columns unless Potion Seller already does.
    /// </summary>
    public sealed class FishSellerMod : IMod
    {
        public const string Name = "FishSeller";
        public const string Version = "1.1.0";

        private static Setting<string> _price;
        private static Setting<int> _stock;

        public const string SettingsHint = "The Fishing Merchant sells every fish and the Caveling Merchant sells Shiny Larva Meat. "
            + "Prices are the game's own (5x what he pays you for one); the multiplier scales buying only. Stock is how many of each per restock. "
            + "Scroll a merchant's wares with the mouse wheel when they do not fit. In multiplayer the host's values count.";

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            if (!InOverhaul(this)) // the overhaul registers all commands once
            {
                try
                {
                    // Command RPC = client -> server channel for buying Legendary items (Patches.SpecialBuy).
                    var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
                    CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(CommandModule));
                    if (info != null) CommandModule.AddCommands(info.ModId, Name);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{Name}] could not register /{Patches.SpecialBuy.Trigger}; Legendary fish can't be bought: {e}");
                }
            }
            Debug.Log($"[{Name}] v{Version} loaded: {ShopItems.Fish.Length} fish on the Fishing Merchant, Shiny Larva Meat on the Caveling Merchant, scrollable buy window.");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Fish Seller").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _price, "Price multiplier", ShopItems.PriceLadder, ShopItems.DefaultPriceToken)
                .Stepper(out _stock, "Stock per restock", ShopItems.MinStock, ShopItems.MaxStock, ShopItems.DefaultStock);

            ShopItems.SetPriceMultiplier(_price.Value);
            ShopItems.SetStock(_stock.Value);
            _price.OnChanged += token =>
            {
                ShopItems.SetPriceMultiplier(token);
                Debug.Log($"[{Name}] price multiplier set to {ShopItems.PriceMultiplier:0.##}x");
            };
            _stock.OnChanged += v =>
            {
                ShopItems.SetStock(v);
                Debug.Log($"[{Name}] stock per restock set to {ShopItems.Stock}");
            };
            Debug.Log($"[{Name}] price multiplier {ShopItems.PriceMultiplier:0.##}x, stock {ShopItems.Stock}.");
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

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<MerchantCD>(entity) || !MerchantRoom.HasBuffers(em, entity)) return;

            var merchant = em.GetComponentData<ObjectDataCD>(entity).objectID;
            var ours = ShopItems.ForMerchant(merchant);
            bool changed = false;
            if (ours != null)
            {
                // The database is not built yet, so list everything; the server system drops what the game will not sell.
                changed |= MerchantRoom.ApplyItems(em, entity, ours, _ => ShopItems.Stock, out _);
                changed |= MerchantRoom.EnsureGrid(em, entity);
            }
            changed |= MerchantRoom.KeepGatedLast(em, entity);
            changed |= MerchantRoom.EnsureRoom(em, entity);
            if (changed)
            {
                Debug.Log($"[{Name}] {merchant} prefab: {em.GetBuffer<MerchantItemInfoBuffer>(entity).Length} items, {em.GetBuffer<ContainedObjectsBuffer>(entity).Length} slots ({em.World.Name}).");
            }
        }
    }
}
