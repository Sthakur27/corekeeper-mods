using System.Collections.Generic;
using UnityEngine;

namespace EnderStash
{
    /// <summary>
    /// Client UI. Using an Ender Chest opens the vanilla chest window with the player's
    /// activeInventoryHandler set to a handler over the player's own stash slots (InventoryHandler's
    /// custom-range constructor). Every move the window makes then targets absolute indices on the
    /// player's own entity, which the server accepts like any other move. Regular chests are untouched.
    /// </summary>
    public static class EnderStashUI
    {
        public static bool Active { get; private set; }
        public static InventoryHandler StashHandler { get; private set; }

        /// <summary>The Ender Chest the window was opened from.</summary>
        internal static Chest OpenedFrom { get; private set; }

        /// <summary>Makes the chest window recompute its size/buttons on its next update.</summary>
        internal static bool ForceRefresh;

        private static bool _loggedError;

        /// <summary>Called instead of <see cref="Chest.Use"/> for an Ender Chest.</summary>
        public static void Open(Chest chest)
        {
            var player = Manager.main?.player;
            if (player == null) return;
            if (!StashLayout.Ready)
            {
                Debug.LogWarning($"[{EnderStashMod.Name}] Ender Chest used before the stash slots exist.");
                return;
            }
            StashHandler = new InventoryHandler(player, player.world, StashLayout.Start, StashLayout.Columns, StashLayout.Size);
            OpenedFrom = chest;
            Active = true;
            ForceRefresh = true;
            player.SetActiveWorldLabel(null);
            player.SetActiveInventoryHandler(StashHandler);
            Manager.ui.OnChestInventoryOpen();
            MarkSlotsDirty();
        }

        /// <summary>Closes the window if it is showing the stash opened from <paramref name="chest"/> (walked away / chest broken).</summary>
        public static void CloseIfFrom(Chest chest)
        {
            if (!Active || OpenedFrom != chest) return;
            var player = Manager.main?.player;
            if (player != null && player.activeInventoryHandler == StashHandler)
            {
                Manager.ui.TryHideAllInventoryAndCraftingUI();
                player.SetActiveWorldLabel(null);
            }
            Reset();
        }

        public static void Update()
        {
            try
            {
                if (!Active) return;
                var player = Manager.main?.player;
                var ui = Manager.ui;
                if (player == null || ui == null || ui.chestInventoryUI == null
                    || !ui.isChestInventoryUIShowing || player.activeInventoryHandler != StashHandler)
                    Reset();
            }
            catch (System.Exception e)
            {
                if (_loggedError) return;
                _loggedError = true;
                Debug.LogError($"[{EnderStashMod.Name}] UI update failed: {e}");
            }
        }

        internal static void Reset()
        {
            Active = false;
            OpenedFrom = null;
            StashHandler = null;
            ForceRefresh = true;
        }

        private static void MarkSlotsDirty()
        {
            var slots = Manager.ui?.chestInventoryUI?.itemSlots;
            if (slots == null) return;
            foreach (var s in slots)
                if (s is InventorySlotUI slot) slot.dirty = true;
        }

        // ------------------------------------------------------------------ stash actions

        /// <summary>Sorts the stash on the client by queueing swaps (by item id, variation, then bigger stacks first).</summary>
        public static void SortStash(PlayerController player)
        {
            var h = StashHandler;
            if (h == null) return;
            int n = h.size;
            var cur = new ContainedObjectsBuffer[n];
            for (int i = 0; i < n; i++) cur[i] = h.GetContainedObjectData(i);

            var order = new List<int>();
            for (int i = 0; i < n; i++) if (cur[i].objectID != ObjectID.None) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = ((int)cur[a].objectID).CompareTo((int)cur[b].objectID);
                if (c != 0) return c;
                c = cur[a].variation.CompareTo(cur[b].variation);
                if (c != 0) return c;
                c = cur[b].amount.CompareTo(cur[a].amount);
                return c != 0 ? c : a.CompareTo(b);
            });
            var desired = new ContainedObjectsBuffer[n];
            for (int p = 0; p < order.Count; p++) desired[p] = cur[order[p]];

            for (int p = 0; p < order.Count; p++)
            {
                if (Same(cur[p], desired[p])) continue;
                int from = -1;
                for (int j = p + 1; j < n; j++)
                    if (Same(cur[j], desired[p])) { from = j; break; }
                if (from < 0) continue;
                h.Swap(player, p, h, from);
                var t = cur[p]; cur[p] = cur[from]; cur[from] = t;
            }
        }

        private static bool Same(ContainedObjectsBuffer a, ContainedObjectsBuffer b)
        {
            return a.objectID == b.objectID && a.variation == b.variation && a.amount == b.amount && a.auxDataIndex == b.auxDataIndex;
        }

        /// <summary>Quick stack into the stash: every non-hotbar inventory item that the stash already holds moves in.</summary>
        public static void QuickStackIntoStash(PlayerController player)
        {
            var stash = StashHandler;
            var inv = player.playerInventoryHandler;
            if (stash == null || inv == null) return;

            var held = new HashSet<long>();
            for (int i = 0; i < stash.size; i++)
            {
                var o = stash.GetContainedObjectData(i);
                if (o.objectID != ObjectID.None) held.Add(Key(o));
            }
            for (int i = 10; i < inv.size; i++)
            {
                var o = inv.GetContainedObjectData(i);
                if (o.objectID == ObjectID.None || !held.Contains(Key(o))) continue;
                inv.TryMoveTo(player, i, stash);
            }
        }

        private static long Key(ContainedObjectsBuffer o) => ((long)(int)o.objectID << 32) | (uint)o.variation;
    }
}
