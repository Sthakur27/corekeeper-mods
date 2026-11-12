using System.Collections.Generic;
using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// One-time cleanup after Potion Seller is removed (More NPCs replaces it). Potion Seller appended its
    /// items to the vanilla Caveling, Slime and Fishing Merchants' MerchantItemInfoBuffer, which is saved
    /// with each merchant, so without this those merchants would keep selling them forever.
    ///
    /// Runs only while Potion Seller is NOT loaded: no loaded mod is named "PotionSeller" (standalone) and no
    /// loaded mod ships "Scripts/PotionSeller/..." in its manifest file list (an Overhaul build that still
    /// bundles it). Uses the loader's metadata only; System.Reflection is blocked by the sandbox.
    ///
    /// Removes only entries whose objectID is on Potion Seller's lists (PotionSeller/Scripts/PotionPrices.cs),
    /// only from those three vanilla merchants, and never an item that the merchant's prefab lists (vanilla's
    /// own stock or another loaded mod's; none of these items is vanilla stock for those three merchants).
    /// Fish Seller's fish, Ender Stash's Ender Chest and vanilla items are never touched, and the inventory
    /// size is left as is (Fish Seller relies on it). A stripped merchant's restock timer is zeroed so his
    /// shelves refill from the cleaned list.
    ///
    /// Read-only in steady state (GetBuffer(e, true)); write access only when an entry is removed. Timer
    /// staggered against the other merchant systems. Logs once per merchant.
    ///
    /// OnUpdate is expression-bodied on purpose: the Overhaul build only adds its feature-switch guard to
    /// "OnUpdate() {" bodies, and this cleanup should also run while More NPCs is switched off.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class LegacyPotionSellerCleanup : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 5f;
        // Staggered against MerchantStockSystem (2.1 s / 3 s), Fish Seller and Merchant Restock.
        private float _timer = 3.7f;

        private EntityQuery _merchants;
        private EntityQuery _prefabs;
        private int _potionSellerState; // 0 unknown, 1 loaded (do nothing), 2 not loaded
        private readonly HashSet<Entity> _logged = new HashSet<Entity>();

        private static readonly ObjectID[] CavelingItems =
        {
            ObjectID.HealingPotion, ObjectID.ManaPotion, ObjectID.PoisonAidPotion, ObjectID.BurnResistancePotion,
            ObjectID.KeenPotion, ObjectID.MagicPotion, ObjectID.StoneskinPotion, ObjectID.EnragePotion,
            ObjectID.GuardiansPotion, ObjectID.MinionPotion, ObjectID.GreaterHealingPotion,
            ObjectID.GreaterManaPotion, ObjectID.UnusualPotion, ObjectID.RecallIdol,
        };
        /// <summary>Potion Seller's optional Caveling item from another mod, looked up by object name.</summary>
        private const string CavelingOptionalName = "WormholePotion_Potion";

        private static readonly ObjectID[] SlimeItems =
        {
            ObjectID.Grenade, ObjectID.SmallBomb, ObjectID.Bomb, ObjectID.FireGrenade, ObjectID.PoisonGrenade,
            ObjectID.OilGrenade, ObjectID.StunGrenade, ObjectID.LargeBomb, ObjectID.CharmGrenade,
            ObjectID.SulfurBomb, ObjectID.ProximityBomb, ObjectID.SeekerBomb, ObjectID.BlunderBomb,
            ObjectID.BigGrenade, ObjectID.VoidBomb,
        };

        private static readonly ObjectID[] FishingItems =
        {
            ObjectID.LargeShinyGlimmeringObject, ObjectID.BaitOctopusBoss, ObjectID.Thumper,
        };

        private ObjectID _wormholeId = ObjectID.None;
        private bool _wormholeResolved;

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(
                ComponentType.ReadOnly<MerchantCD>(),
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<MerchantItemInfoBuffer>());
            _prefabs = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadOnly<MerchantItemInfoBuffer>()
                },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
        }

        protected override void OnUpdate() => Tick();

        private void Tick()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmpty) return;
            if (_potionSellerState == 0)
            {
                _potionSellerState = PotionSellerLoaded() ? 1 : 2;
                if (_potionSellerState == 1)
                    Debug.Log($"[{BossMerchantsMod.Name}] Potion Seller is loaded; its merchant items are left alone.");
            }
            if (_potionSellerState != 2) { Enabled = false; return; }

            if (!_wormholeResolved)
            {
                _wormholeResolved = true; // a world exists, so every mod's object IDs are assigned
                _wormholeId = API.Authoring.GetObjectID(CavelingOptionalName);
            }

            var entities = _merchants.ToEntityArray(Allocator.Temp);
            var objects = _merchants.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var merchant = objects[i].objectID;
                var list = LegacyItems(merchant);
                if (list == null) continue;
                var e = entities[i];
                bool optional = merchant == ObjectID.CavelingMerchant && _wormholeId != ObjectID.None;

                // Read-only check first: steady state takes no write access.
                var items = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e, true);
                bool found = false;
                for (int k = 0; k < items.Length && !found; k++)
                    found = IsLegacy(items[k].objectID, list, optional) && !PrefabSells(merchant, items[k].objectID);
                if (!found) continue;

                var writable = EntityManager.GetBuffer<MerchantItemInfoBuffer>(e);
                int removed = 0;
                for (int k = writable.Length - 1; k >= 0; k--)
                {
                    var id = writable[k].objectID;
                    if (!IsLegacy(id, list, optional) || PrefabSells(merchant, id)) continue;
                    writable.RemoveAt(k);
                    removed++;
                }
                int left = writable.Length;
                var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                od.amount = 0; // restock from the cleaned list on the next MerchantBuyInventorySystem tick
                EntityManager.SetComponentData(e, od);
                if (_logged.Add(e))
                    Debug.Log($"[{BossMerchantsMod.Name}] Removed {removed} leftover Potion Seller item(s) from {merchant} {e} ({left} items left, restock forced).");
            }
            entities.Dispose();
            objects.Dispose();
        }

        private static ObjectID[] LegacyItems(ObjectID merchant)
        {
            switch (merchant)
            {
                case ObjectID.CavelingMerchant: return CavelingItems;
                case ObjectID.SlimeMerchant: return SlimeItems;
                case ObjectID.FishingMerchant: return FishingItems;
                default: return null;
            }
        }

        private bool IsLegacy(ObjectID id, ObjectID[] list, bool optional)
        {
            if (id == ObjectID.None) return false;
            if (optional && id == _wormholeId) return true;
            for (int i = 0; i < list.Length; i++) if (list[i] == id) return true;
            return false;
        }

        /// <summary>
        /// True if this merchant type's prefab lists the item, i.e. vanilla or a mod that is loaded now
        /// sells it there. Potion Seller is not loaded, so it can't be one of its own entries. Only called
        /// when a candidate entry is found, so the steady state never reads the prefabs.
        /// </summary>
        private bool PrefabSells(ObjectID merchant, ObjectID id)
        {
            if (_prefabs.IsEmptyIgnoreFilter) return false;
            var prefabs = _prefabs.ToEntityArray(Allocator.Temp);
            bool sells = false;
            for (int p = 0; p < prefabs.Length && !sells; p++)
            {
                if (EntityManager.GetComponentData<ObjectDataCD>(prefabs[p]).objectID != merchant) continue;
                var items = EntityManager.GetBuffer<MerchantItemInfoBuffer>(prefabs[p], true);
                for (int k = 0; k < items.Length; k++)
                    if (items[k].objectID == id) { sells = true; break; }
            }
            prefabs.Dispose();
            return sells;
        }

        /// <summary>
        /// Standalone Potion Seller (manifest name), or any loaded mod (an Overhaul build) whose manifest
        /// lists Potion Seller's scripts.
        /// </summary>
        private static bool PotionSellerLoaded()
        {
            foreach (var mod in API.ModLoader.LoadedMods)
            {
                var meta = mod.Metadata;
                if (meta.name == "PotionSeller") return true;
                if (meta.files == null) continue;
                foreach (var f in meta.files)
                {
                    if (f.path == null) continue;
                    if (f.path.Replace('\\', '/').StartsWith("Scripts/PotionSeller/")) return true;
                }
            }
            return false;
        }
    }
}
