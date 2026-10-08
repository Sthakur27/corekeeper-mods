using Unity.Entities;
using Unity.Mathematics;

namespace FishSeller
{
    /// <summary>
    /// Edits a merchant's list (MerchantItemInfoBuffer) and makes room for it. Everything here only ADDS or
    /// GROWS, so it composes with other mods that edit the same merchants (Potion Seller appends Titan
    /// summons to the Fishing Merchant, Ender Stash inserts the Ender Chest at index 0).
    ///
    /// How the game restocks (MerchantBuyInventorySystem, Burst, server, every 1500-2100 s): it walks the
    /// merchant's WHOLE ContainedObjectsBuffer and fills slot k with the k-th available list entry; entries
    /// beyond the buffer length are dropped. So the buffer is grown to fit the list, and the buy window
    /// scrolls through it (<see cref="Patches.BuyScroll"/>); the window itself stays sizeX x sizeY.
    ///
    /// Restock gotcha: between restocks it also compares slot i with entry i for every AVAILABLE entry and
    /// restocks immediately on a mismatch. An unavailable entry (MerchantItemRequirement not met, or amount 0)
    /// shifts every later item one slot up, so anything listed after it would mismatch and the merchant would
    /// restock every tick (endless stock). <see cref="KeepGatedLast"/> therefore keeps such entries behind
    /// all the always-available ones.
    /// </summary>
    public static class MerchantRoom
    {
        /// <summary>Grid for merchants this mod adds items to: same as Potion Seller's, so the two agree.</summary>
        public const int Columns = 8;
        public const int Rows = 3;

        public static bool HasBuffers(EntityManager em, Entity e)
        {
            return em.HasComponent<ObjectDataCD>(e)
                && em.HasBuffer<MerchantItemInfoBuffer>(e)
                && em.HasBuffer<ContainedObjectsBuffer>(e)
                && em.HasBuffer<InventoryBuffer>(e);
        }

        /// <summary>
        /// Puts our items on the list with <paramref name="amountFor"/>(id) each (a value &lt;= 0 removes the
        /// item instead, e.g. when the game would not sell it). Returns true if the list changed;
        /// <paramref name="added"/> is true when an item was newly listed.
        /// </summary>
        public static bool ApplyItems(EntityManager em, Entity e, ObjectID[] ours, System.Func<ObjectID, int> amountFor, out bool added)
        {
            added = false;
            bool changed = false;
            var items = em.GetBuffer<MerchantItemInfoBuffer>(e);
            foreach (var id in ours)
            {
                int idx = IndexOf(items, id);
                int amount = amountFor(id);
                if (amount <= 0)
                {
                    if (idx >= 0) { items.RemoveAt(idx); changed = true; }
                    continue;
                }
                if (idx < 0)
                {
                    items.Add(new MerchantItemInfoBuffer { objectID = id, amount = amount, requirementToBeAvailable = MerchantItemRequirement.None });
                    added = changed = true;
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
            return changed;
        }

        /// <summary>Stable partition: always-available entries first, gated ones (requirement or amount 0) after. See the class summary.</summary>
        public static bool KeepGatedLast(EntityManager em, Entity e)
        {
            var items = em.GetBuffer<MerchantItemInfoBuffer>(e);
            int firstGated = -1;
            bool needed = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (IsGated(items[i])) { if (firstGated < 0) firstGated = i; }
                else if (firstGated >= 0) { needed = true; break; }
            }
            if (!needed) return false;

            var copy = items.ToNativeArray(Unity.Collections.Allocator.Temp);
            int k = 0;
            for (int i = 0; i < copy.Length; i++) if (!IsGated(copy[i])) items[k++] = copy[i];
            for (int i = 0; i < copy.Length; i++) if (IsGated(copy[i])) items[k++] = copy[i];
            copy.Dispose();
            return true;
        }

        private static bool IsGated(MerchantItemInfoBuffer entry)
            => entry.amount == 0 || entry.requirementToBeAvailable != MerchantItemRequirement.None;

        /// <summary>Widens/heightens the merchant grid to at least <see cref="Columns"/> x <see cref="Rows"/> (never shrinks). Not replicated: run in every world.</summary>
        public static bool EnsureGrid(EntityManager em, Entity e)
        {
            var inventories = em.GetBuffer<InventoryBuffer>(e);
            if (inventories.Length == 0) return false;
            var inv = inventories[0];
            if (inv.sizeX >= Columns && inv.sizeY >= Rows && inv.maxSize >= inv.sizeX * inv.sizeY) return false;
            inv.sizeX = math.max(inv.sizeX, Columns);
            inv.sizeY = math.max(inv.sizeY, Rows);
            inv.maxSize = math.max(inv.maxSize, inv.sizeX * inv.sizeY);
            inventories[0] = inv;
            return true;
        }

        /// <summary>
        /// Grows ContainedObjectsBuffer so every list entry has a slot, rounded up to whole rows and at least
        /// one full window. Never shrinks (another mod may keep data past our end).
        /// </summary>
        public static bool EnsureRoom(EntityManager em, Entity e)
        {
            var inventories = em.GetBuffer<InventoryBuffer>(e);
            if (inventories.Length == 0) return false;
            var inv = inventories[0];
            int cols = math.max(1, inv.sizeX);
            int entries = em.GetBuffer<MerchantItemInfoBuffer>(e).Length;
            int slots = math.max(inv.sizeX * inv.sizeY, (entries + cols - 1) / cols * cols);
            int want = inv.startIndex + slots;

            var contained = em.GetBuffer<ContainedObjectsBuffer>(e);
            if (contained.Length >= want) return false;
            while (contained.Length < want) contained.Add(default);
            return true;
        }

        private static int IndexOf(DynamicBuffer<MerchantItemInfoBuffer> items, ObjectID id)
        {
            for (int i = 0; i < items.Length; i++) if (items[i].objectID == id) return i;
            return -1;
        }
    }
}
