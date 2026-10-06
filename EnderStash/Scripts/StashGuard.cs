using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace EnderStash
{
    /// <summary>
    /// Keeps the stash in place when other mods that add player slots (Pouch Lite, Five Loadouts...) are
    /// installed or removed. The stash lives at fixed indices in the player's ContainedObjectsBuffer, after
    /// the other mods' slots, so such a change moves where the stash should be while the saves still have
    /// the items at the old indices.
    ///
    /// The slot after the stash holds a marker (objectID None, variationUpdateCount = <see cref="Tag"/>,
    /// variation = the stash start). The character save keeps it; the world save does not (it drops data
    /// in empty slots), so the character save is the record of the old layout. A player's slots come from
    /// one of two places when they join, and both are handled:
    ///  - from the character save (first visit to a world, multiplayer): <see cref="Relocate(CharacterData)"/>
    ///    moves the stash inside the decoded data before the game copies it into the player (it copies
    ///    only up to the current slot count, so a stash that moved down would otherwise be cut off);
    ///  - from the world save (rejoining the world you were last in): the player entity keeps its old
    ///    slots. The decode step remembers the stash items it saw; <see cref="Relocate(DynamicBuffer{ContainedObjectsBuffer}, Unity.Entities.Hash128)"/>
    ///    then moves them only if they are still found at the old position (if they are already at the new
    ///    one, the first path did it).
    /// Saves from before the marker existed have none and are left as they are.
    /// </summary>
    public static class StashGuard
    {
        public const int Tag = 0x45535448; // "ESTH"

        private struct Pending
        {
            public int OldStart;
            public ObjectDataCD[] Items;
        }

        private static readonly Dictionary<Unity.Entities.Hash128, Pending> _pending = new Dictionary<Unity.Entities.Hash128, Pending>();

        public static bool IsMarker(ObjectDataCD data) =>
            data.objectID == ObjectID.None && data.variationUpdateCount == Tag && data.variation >= 0;

        public static ObjectDataCD Marker(int start) =>
            new ObjectDataCD { objectID = ObjectID.None, amount = 0, variation = start, variationUpdateCount = Tag };

        private static bool Same(ObjectDataCD a, ObjectDataCD b) =>
            a.objectID == b.objectID && a.amount == b.amount && a.variation == b.variation;

        /// <summary>Server: fixes a decoded character's slots before they are applied to the player.</summary>
        public static void Relocate(CharacterData data)
        {
            if (data == null || data.inventory == null || !StashLayout.Ready) return;
            List<ObjectDataCD> inventory = data.inventory;
            int markerAt = -1;
            for (int i = inventory.Count - 1; i >= 0; i--)
                if (IsMarker(inventory[i])) { markerAt = i; break; }
            if (markerAt < 0) return; // saved before the guard existed: same layout as before

            int oldStart = inventory[markerAt].variation;
            int newStart = StashLayout.Start;
            if (oldStart == newStart) return;

            List<string> names = data.inventoryObjectNames;
            List<CharacterInventoryAuxData> aux = data.inventoryAuxData;
            while (inventory.Count < StashLayout.TotalLength) inventory.Add(default);
            if (names != null) while (names.Count < inventory.Count) names.Add(null);
            if (aux != null) while (aux.Count < inventory.Count) aux.Add(default);

            // Copy the old stash out first (old and new ranges can overlap), clear it, then write it back.
            var items = new ObjectDataCD[StashLayout.Size];
            var itemNames = new string[StashLayout.Size];
            var itemAux = new CharacterInventoryAuxData[StashLayout.Size];
            int moved = 0;
            for (int k = 0; k < StashLayout.Size; k++)
            {
                int from = oldStart + k;
                if (from < 0 || from >= inventory.Count) continue;
                items[k] = inventory[from];
                if (names != null) itemNames[k] = names[from];
                if (aux != null) itemAux[k] = aux[from];
                if (items[k].objectID != ObjectID.None) moved++;
                inventory[from] = default;
                if (names != null) names[from] = null;
                if (aux != null) aux[from] = default;
            }
            inventory[markerAt] = default;
            for (int k = 0; k < StashLayout.Size; k++)
            {
                int to = newStart + k;
                inventory[to] = items[k];
                if (names != null) names[to] = itemNames[k];
                if (aux != null) aux[to] = itemAux[k];
            }
            inventory[StashLayout.MarkerIndex] = Marker(newStart);

            if (moved > 0 && !string.IsNullOrEmpty(data.characterGuid))
            {
                Unity.Entities.Hash128 guid = UnityEngine.Hash128.Parse(data.characterGuid);
                _pending[guid] = new Pending { OldStart = oldStart, Items = items };
            }
            Debug.Log($"[{EnderStashMod.Name}] Player slot layout changed: stash moved from slot {oldStart} to {newStart} ({moved} items) in the saved character {data.characterGuid}.");
        }

        /// <summary>Server, every tick: grows old buffers, finishes a pending move (world-save path) and keeps the marker in place.</summary>
        public static void Relocate(DynamicBuffer<ContainedObjectsBuffer> contained, Unity.Entities.Hash128 guid)
        {
            if (!StashLayout.Ready) return;
            while (contained.Length < StashLayout.TotalLength) contained.Add(default);
            int newStart = StashLayout.Start;

            if (_pending.TryGetValue(guid, out Pending pending))
            {
                _pending.Remove(guid);
                if (!Matches(contained, newStart, pending.Items))
                {
                    if (Matches(contained, pending.OldStart, pending.Items))
                        Move(contained, pending.OldStart, newStart);
                    else
                        Debug.LogWarning($"[{EnderStashMod.Name}] Stash layout changed but the stash items were found neither at slot {pending.OldStart} nor {newStart}; left as is.");
                }
            }

            int markerIndex = StashLayout.MarkerIndex;
            ObjectDataCD current = contained[markerIndex].objectData;
            if (IsMarker(current) && current.variation == newStart) return;
            for (int i = 0; i < contained.Length; i++)
                if (i != markerIndex && IsMarker(contained[i].objectData)) contained[i] = default;
            // Claim the marker slot only when it is free (never overwrite an item).
            if (contained[markerIndex].objectData.objectID == ObjectID.None)
                contained[markerIndex] = new ContainedObjectsBuffer { objectData = Marker(newStart) };
        }

        private static bool Matches(DynamicBuffer<ContainedObjectsBuffer> contained, int start, ObjectDataCD[] items)
        {
            for (int k = 0; k < items.Length; k++)
            {
                int i = start + k;
                ObjectDataCD live = i >= 0 && i < contained.Length ? contained[i].objectData : default;
                if (!Same(live, items[k])) return false;
            }
            return true;
        }

        private static void Move(DynamicBuffer<ContainedObjectsBuffer> contained, int oldStart, int newStart)
        {
            var items = new ContainedObjectsBuffer[StashLayout.Size];
            int moved = 0;
            for (int k = 0; k < StashLayout.Size; k++)
            {
                int from = oldStart + k;
                if (from < 0 || from >= contained.Length) continue;
                items[k] = contained[from];
                if (items[k].objectData.objectID != ObjectID.None) moved++;
                contained[from] = default;
            }
            for (int k = 0; k < StashLayout.Size; k++) contained[newStart + k] = items[k];
            Debug.Log($"[{EnderStashMod.Name}] Player slot layout changed: moved the stash from slot {oldStart} to {newStart} ({moved} items).");
        }
    }
}
