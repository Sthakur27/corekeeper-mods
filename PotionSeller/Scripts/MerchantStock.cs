using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PotionSeller
{
    /// <summary>
    /// Puts every potion on the Caveling Merchant's list, and every grenade and bomb on the Slime
    /// Merchant's (<see cref="PotionPrices.ForMerchant"/>), and makes room for them.
    ///
    /// MerchantBuyInventorySystem (Burst, server) restocks the merchant every 1500-2100 s: it walks the
    /// merchant's whole ContainedObjectsBuffer and fills slot k with the k-th *available* entry of
    /// MerchantItemInfoBuffer, so entries beyond the buffer length are silently dropped. The merchant's
    /// buffer is sized by its InventoryAuthoring (3x3 in vanilla), so besides appending the potions we
    /// grow ContainedObjectsBuffer and InventoryBuffer[0] (sizeX/sizeY/maxSize) to <see cref="Columns"/> x
    /// <see cref="Rows"/>. A buffer that is already longer (e.g. 25 slots from the 5x5 layout of v1.0.0)
    /// is left alone; the UI only shows sizeX*sizeY slots and the restock just skips the tail. InventoryBuffer.sizeX/sizeY are not replicated (GhostField SendData=false), so
    /// this must run on the prefab in every world (client + server); it is idempotent.
    /// </summary>
    public static class MerchantStock
    {
        /// <summary>
        /// Merchant inventory grid (the buy window grid is patched to the same size). Wide and short on
        /// purpose: the buy window sits at the top of the screen next to the sell window, and extra rows would
        /// grow down over the player inventory, so keep the vanilla 3 rows and add columns (8x3 = 24 slots for
        /// ~9 vanilla items + 13 potions).
        /// </summary>
        public const int Columns = 8;
        public const int Rows = 3;
        public const int Slots = Columns * Rows;

        private static bool _warned;

        public static bool IsTarget(EntityManager em, Entity e)
        {
            return em.HasComponent<ObjectDataCD>(e)
                && PotionPrices.ForMerchant(em.GetComponentData<ObjectDataCD>(e).objectID) != null
                && em.HasBuffer<MerchantItemInfoBuffer>(e)
                && em.HasBuffer<ContainedObjectsBuffer>(e)
                && em.HasBuffer<InventoryBuffer>(e);
        }

        /// <summary>
        /// Appends missing potions (amount = stock, no unlock requirement), syncs the amount of existing
        /// potion entries to <paramref name="stock"/> and grows the inventory. Returns true if anything
        /// changed; <paramref name="addedItems"/> is true only when a potion entry was newly added.
        /// </summary>
        public static bool Apply(EntityManager em, Entity e, int stock, out bool addedItems)
            => Apply(em, e, stock, null, out addedItems);

        /// <summary>
        /// As above; entries with an unlockedBy boss are listed only while <paramref name="isBeaten"/>
        /// says that boss is beaten (null = locked, used at prefab time) and removed from the list otherwise.
        /// </summary>
        public static bool Apply(EntityManager em, Entity e, int stock, System.Func<ObjectID, bool> isBeaten, out bool addedItems)
        {
            addedItems = false;
            bool changed = false;

            var list = PotionPrices.ForMerchant(em.GetComponentData<ObjectDataCD>(e).objectID);
            if (list == null) return false;
            var items = em.GetBuffer<MerchantItemInfoBuffer>(e);
            foreach (var potion in list)
            {
                int idx = -1;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].objectID == potion.id) { idx = i; break; }
                }
                bool locked = potion.unlockedBy != ObjectID.None && (isBeaten == null || !isBeaten(potion.unlockedBy));
                if (locked)
                {
                    if (idx >= 0)
                    {
                        items.RemoveAt(idx);
                        changed = true;
                    }
                    continue;
                }
                int amount = potion.stock > 0 ? potion.stock : stock;
                if (idx < 0)
                {
                    items.Add(new MerchantItemInfoBuffer
                    {
                        objectID = potion.id,
                        amount = amount,
                        requirementToBeAvailable = MerchantItemRequirement.None
                    });
                    addedItems = changed = true;
                }
                else if (items[idx].amount != amount || items[idx].requirementToBeAvailable != MerchantItemRequirement.None)
                {
                    var entry = items[idx];
                    entry.amount = amount;
                    entry.requirementToBeAvailable = MerchantItemRequirement.None;
                    items[idx] = entry;
                    changed = true;
                }
            }
            var inventories = em.GetBuffer<InventoryBuffer>(e);
            int startIndex = 0;
            if (inventories.Length > 0)
            {
                var inv = inventories[0];
                startIndex = inv.startIndex;
                // Columns grow only (another mod may use a wider grid); rows stay at 3 so the window never
                // grows down over the player inventory (v1.0.0 saves had 5 rows). Fish Seller scrolls the rest.
                if (inv.sizeX < Columns || inv.sizeY != Rows || inv.maxSize < math.max(inv.sizeX, Columns) * Rows)
                {
                    inv.sizeX = math.max(inv.sizeX, Columns);
                    inv.sizeY = Rows;
                    inv.maxSize = math.max(inv.maxSize, inv.sizeX * inv.sizeY);
                    inventories[0] = inv;
                    changed = true;
                }
            }

            var contained = em.GetBuffer<ContainedObjectsBuffer>(e);
            int want = startIndex + Slots;
            while (contained.Length < want)
            {
                contained.Add(default);
                changed = true;
            }
            if (items.Length > contained.Length - startIndex && !_warned)
            {
                _warned = true;
                Debug.LogWarning($"[{PotionSellerMod.Name}] merchant list has {items.Length} entries but only {contained.Length - startIndex} slots; the last ones are not stocked (Fish Seller adds room and scrolling).");
            }
            return changed;
        }
    }
}
