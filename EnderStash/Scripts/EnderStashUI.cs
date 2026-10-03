using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EnderStash
{
    /// <summary>
    /// Client UI. Adds a button under the chest window's Quick Stack / Sort buttons (a runtime clone
    /// of the Sort button, so no new assets). Clicking it points the chest window at the stash: the
    /// player's activeInventoryHandler is swapped for a handler over the player's own stash slots
    /// (InventoryHandler's custom-range constructor). Every move the window makes then targets
    /// absolute indices on the player's own entity, which the server accepts like any other move.
    /// Clicking again (or closing the window) returns to the chest.
    /// </summary>
    public static class EnderStashUI
    {
        public static bool Active { get; private set; }
        public static InventoryHandler StashHandler { get; private set; }

        /// <summary>The chest's own handler while the stash is showing.</summary>
        internal static InventoryHandler ChestHandler { get; private set; }

        /// <summary>Makes the chest window recompute its size/buttons on its next update.</summary>
        internal static bool ForceRefresh;

        private static ButtonUIElement _button;
        private static SpriteRenderer _icon;
        private static bool _loggedError;

        private static readonly Color StashTint = new Color(0.75f, 0.45f, 1f);

        public static void Update()
        {
            try
            {
                var player = Manager.main?.player;
                var ui = Manager.ui;
                if (player == null || ui == null || ui.chestInventoryUI == null) return;

                if (Active && (!ui.isChestInventoryUIShowing || player.activeInventoryHandler != StashHandler))
                    Reset();

                EnsureButton(ui.chestInventoryUI);
                if (_button == null) return;

                bool show = StashLayout.Ready && ui.isChestInventoryUIShowing
                            && (Active || player.activeInventoryHandler?.entityMonoBehaviour is Chest { showSortAndQuickStackButtons: true });
                if (_button.gameObject.activeSelf != show) _button.gameObject.SetActive(show);
                if (!show) return;

                var sort = ui.chestInventoryUI.optionalSortButton;
                var quick = ui.chestInventoryUI.optionalQuickStackButton;
                if (sort != null && quick != null)
                {
                    Vector3 step = sort.transform.localPosition - quick.transform.localPosition;
                    _button.transform.localPosition = sort.transform.localPosition + step;
                }
                if (_icon != null) _icon.color = Active ? Color.white : StashTint;
            }
            catch (System.Exception e)
            {
                if (_loggedError) return;
                _loggedError = true;
                Debug.LogError($"[{EnderStashMod.Name}] UI update failed: {e}");
            }
        }

        public static void Toggle()
        {
            var player = Manager.main?.player;
            if (player == null || !StashLayout.Ready) return;
            if (!Active)
            {
                var current = player.activeInventoryHandler;
                if (current == null || !(current.entityMonoBehaviour is Chest)) return;
                ChestHandler = current;
                StashHandler = new InventoryHandler(player, player.world, StashLayout.Start, StashLayout.Columns, StashLayout.Size);
                player.SetActiveInventoryHandler(StashHandler);
                Active = true;
            }
            else
            {
                player.SetActiveInventoryHandler(ChestHandler);
                Reset();
            }
            ForceRefresh = true;
            MarkSlotsDirty();
        }

        internal static void Reset()
        {
            Active = false;
            ChestHandler = null;
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

        private static void EnsureButton(InventoryUI chestUI)
        {
            if (_button != null) return;
            var sort = chestUI.optionalSortButton;
            if (sort == null) return;

            var go = Object.Instantiate(sort.gameObject, sort.transform.parent);
            go.name = "EnderStashButton";
            _button = go.GetComponent<ButtonUIElement>();
            if (_button == null)
            {
                Object.Destroy(go);
                return;
            }
            // New event, not the clone's inspector-wired Sort listener (persistent-listener APIs are
            // rejected by the mod loader's security check; runtime listeners are fine).
            _button.onLeftClick = new UnityEvent();
            _button.onLeftClick.AddListener(Toggle);
            _button.onRightClick = new UnityEvent();
            _button.optionalShortCut = ButtonUIElement.ShortCutBinding.none;
            _button.showHoverDesc = false;
            _button.showHoverTitle = true;
            try
            {
                var title = _button.optionalTitle;
                title.mTerm = "Ender Stash"; // missing localization term: the key itself is shown
                _button.optionalTitle = title;
            }
            catch (System.Exception) { }

            ReplaceIcon(go);
            go.SetActive(false);
            Debug.Log($"[{EnderStashMod.Name}] Chest window button created.");
        }

        /// <summary>Swaps the cloned Sort icon for the chest item icon.</summary>
        private static void ReplaceIcon(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            var names = new List<string>();
            SpriteRenderer best = null;
            bool named = false;
            foreach (var r in renderers)
            {
                names.Add($"{r.gameObject.name}:{(r.sprite != null ? r.sprite.name : "-")}");
                if (r.sprite == null || named) continue;
                string n = (r.gameObject.name + " " + r.sprite.name).ToLowerInvariant();
                if (n.Contains("icon") || n.Contains("sort"))
                {
                    best = r;
                    named = true;
                    continue;
                }
                if (best == null || r.sprite.bounds.size.sqrMagnitude < best.sprite.bounds.size.sqrMagnitude)
                    best = r;
            }
            Debug.Log($"[{EnderStashMod.Name}] Button sprites: {string.Join(", ", names)}; icon = {(best != null ? best.gameObject.name : "none")}");
            if (best == null) return;

            var info = PugDatabase.GetObjectInfo(ObjectID.InventoryChest);
            var sprite = info != null ? (info.smallIcon != null ? info.smallIcon : info.icon) : null;
            if (sprite != null) best.sprite = sprite;
            best.color = StashTint;
            _icon = best;
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
