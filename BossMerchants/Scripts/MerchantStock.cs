using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BossMerchants
{
    /// <summary>
    /// Keeps a boss merchant's list (MerchantItemInfoBuffer) equal to its <see cref="Catalog.Merchant.Stock"/> and
    /// its inventory big enough to show it. Adapted from Potion Seller's MerchantStock; the difference is that
    /// these merchants are ours, so the list is exactly our stock in our order (anything else is removed, e.g.
    /// vanilla entries left on the copied prefab).
    ///
    /// MerchantBuyInventorySystem (Burst, server) restocks every 1500-2100 s: it walks the merchant's whole
    /// ContainedObjectsBuffer and fills slot k with the k-th available list entry; entries beyond the buffer
    /// length are dropped. Between restocks it compares slot i with entry i and restocks on a mismatch, so all
    /// entries are always-available (requirement None). InventoryBuffer.sizeX/sizeY are not replicated, so
    /// this runs on the prefab in every world (client + server); it is idempotent.
    /// </summary>
    public static class MerchantStock
    {
        /// <summary>Buy window grid, same as Potion Seller / Fish Seller so the shared window layout agrees.</summary>
        public const int Columns = 8;
        public const int Rows = 3;
        public const int Slots = Columns * Rows;

        private static bool _warned;

        public static Catalog.Merchant TargetOf(EntityManager em, Entity e)
        {
            if (!em.HasComponent<ObjectDataCD>(e) || !em.HasBuffer<MerchantItemInfoBuffer>(e)
                || !em.HasBuffer<ContainedObjectsBuffer>(e) || !em.HasBuffer<InventoryBuffer>(e)) return null;
            return Catalog.ByMerchantId(em.GetComponentData<ObjectDataCD>(e).objectID);
        }

        /// <summary>Returns true if anything changed; <paramref name="addedItems"/> when an entry was newly added.</summary>
        public static bool Apply(EntityManager em, Entity e, Catalog.Merchant merchant, int stock, out bool addedItems)
        {
            addedItems = false;
            bool changed = false;
            var list = merchant.Stock;

            if (ListNeedsUpdate(em.GetBuffer<MerchantItemInfoBuffer>(e, true), list, stock))
            {
                var items = em.GetBuffer<MerchantItemInfoBuffer>(e);
                // Rebuild in catalog order; "added" if any of our items was not there before.
                for (int i = 0; i < list.Length; i++)
                    if (IndexOf(items, list[i].id) < 0) { addedItems = true; break; }
                items.Clear();
                foreach (var entry in list)
                    items.Add(new MerchantItemInfoBuffer
                    {
                        objectID = entry.id,
                        amount = entry.stock > 0 ? entry.stock : stock,
                        requirementToBeAvailable = MerchantItemRequirement.None
                    });
                changed = true;
            }

            var inventories = em.GetBuffer<InventoryBuffer>(e, true);
            int startIndex = 0;
            if (inventories.Length > 0)
            {
                var inv = inventories[0];
                startIndex = inv.startIndex;
                if (inv.sizeX < Columns || inv.sizeY != Rows || inv.maxSize < math.max(inv.sizeX, Columns) * Rows)
                {
                    inv.sizeX = math.max(inv.sizeX, Columns);
                    inv.sizeY = Rows;
                    inv.maxSize = math.max(inv.maxSize, inv.sizeX * inv.sizeY);
                    var writable = em.GetBuffer<InventoryBuffer>(e);
                    writable[0] = inv;
                    changed = true;
                }
            }

            int want = startIndex + Slots;
            if (em.GetBuffer<ContainedObjectsBuffer>(e, true).Length < want)
            {
                var contained = em.GetBuffer<ContainedObjectsBuffer>(e);
                while (contained.Length < want) contained.Add(default);
                changed = true;
            }
            if (!_warned && list.Length > Slots)
            {
                _warned = true;
                Debug.LogWarning($"[{BossMerchantsMod.Name}] {merchant.Key} merchant lists {list.Length} items but shows {Slots}; the rest are not stocked.");
            }
            return changed;
        }

        private static int IndexOf(DynamicBuffer<MerchantItemInfoBuffer> items, ObjectID id)
        {
            for (int i = 0; i < items.Length; i++) if (items[i].objectID == id) return i;
            return -1;
        }

        private static bool ListNeedsUpdate(DynamicBuffer<MerchantItemInfoBuffer> items, Catalog.Entry[] list, int stock)
        {
            if (items.Length != list.Length) return true;
            for (int i = 0; i < list.Length; i++)
            {
                int amount = list[i].stock > 0 ? list[i].stock : stock;
                if (items[i].objectID != list[i].id || items[i].amount != amount
                    || items[i].requirementToBeAvailable != MerchantItemRequirement.None) return true;
            }
            return false;
        }
    }
}
