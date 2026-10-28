using System;
using Unity.Entities;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Client side, every frame before the players' LateUpdate: points each player's vanity handlers
    /// (local and remote) at the vanity slots of that player's active loadout. ActiveEquipmentPresetCD is
    /// replicated, so other players see your per-loadout outfit too. The vanity furniture window, our
    /// character-window vanity slots and the character sprite all read through these handlers.
    ///
    /// With <see cref="Fallback"/> on, an unset vanity slot shows the nearest earlier loadout's vanity
    /// piece, like the armor waterfall. Hiding a piece (vanilla stores it as ObjectID.None with a negative
    /// amount) sets that loadout's own slot, so it stops the fallback there, but it is not passed on:
    /// later loadouts skip hidden slots and borrow only real pieces. It is a client display setting:
    /// each player sees everyone's outfits according to their own toggle.
    /// </summary>
    public static class VanitySync
    {
        /// <summary>Set by the "Vanity fallback" toggle.</summary>
        public static bool Fallback = true;

        private static bool _logged;

        public static bool TryKindOf(ItemSlotsUIType slotType, out VanityKind kind)
        {
            switch (slotType)
            {
                case ItemSlotsUIType.HelmVanitySlot: kind = VanityKind.Helm; return true;
                case ItemSlotsUIType.BreastVanitySlot: kind = VanityKind.Breast; return true;
                case ItemSlotsUIType.PantsVanitySlot: kind = VanityKind.Pants; return true;
                default: kind = default; return false;
            }
        }

        /// <summary>The loadout's own slot holds a piece or the hide marker.</summary>
        private static bool IsSet(DynamicBuffer<ContainedObjectsBuffer> contained, int slot)
        {
            if (slot < 0 || slot >= contained.Length) return false;
            var o = contained[slot];
            return o.objectID != ObjectID.None || o.amount < 0;
        }

        private static bool HasPiece(DynamicBuffer<ContainedObjectsBuffer> contained, int slot) =>
            slot >= 0 && slot < contained.Length && contained[slot].objectID != ObjectID.None;

        /// <summary>The preset's own vanity slot if set (or fallback is off), else the nearest earlier loadout's piece.</summary>
        public static int EffectiveSlot(int preset, VanityKind kind, DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            if (preset < 0 || preset >= VanityLayout.Count) preset = 0;
            int own = VanityLayout.Slot(preset, kind);
            if (!Fallback || IsSet(contained, own)) return own;
            for (int p = preset - 1; p >= 0; p--)
            {
                int slot = VanityLayout.Slot(p, kind);
                if (HasPiece(contained, slot)) return slot;
            }
            return own;
        }

        /// <summary>True when the local player's given vanity UI slot is showing an earlier loadout's piece.</summary>
        public static bool IsSlotInherited(ItemSlotsUIType slotType)
        {
            if (!Fallback || !VanityLayout.Ready || !TryKindOf(slotType, out var kind)) return false;
            if (!HandlerSync.TryGetContext(out _, out int preset, out var contained)) return false;
            if (preset <= 0 || preset >= VanityLayout.Count) return false;
            return EffectiveSlot(preset, kind, contained) != VanityLayout.Slot(preset, kind);
        }

        public static void SyncAll()
        {
            if (!VanityLayout.Ready) return;
            try
            {
                var main = Manager.main;
                if (main == null || main.allPlayers == null) return;
                foreach (var pc in main.allPlayers)
                {
                    if (pc == null || pc.vanitySlotsHandler == null || pc.world == null || !pc.world.IsCreated) continue;
                    Entity e = pc.entity;
                    var em = pc.world.EntityManager;
                    if (e == Entity.Null || !em.Exists(e)) continue;
                    if (!em.HasComponent<ActiveEquipmentPresetCD>(e) || !em.HasBuffer<ContainedObjectsBuffer>(e)) continue;
                    int preset = em.GetComponentData<ActiveEquipmentPresetCD>(e).Value;
                    var contained = em.GetBuffer<ContainedObjectsBuffer>(e, true);

                    var h = pc.vanitySlotsHandler;
                    h.helmVanitySlotInventoryHandler.startPosInBuffer = EffectiveSlot(preset, VanityKind.Helm, contained);
                    h.breastVanitySlotInventoryHandler.startPosInBuffer = EffectiveSlot(preset, VanityKind.Breast, contained);
                    h.pantsVanitySlotInventoryHandler.startPosInBuffer = EffectiveSlot(preset, VanityKind.Pants, contained);
                }
            }
            catch (Exception ex)
            {
                if (_logged) return;
                _logged = true;
                Debug.LogError($"[{LoadoutSharingMod.Name}] VanitySync: {ex}");
            }
        }

        /// <summary>
        /// Points the local player's vanity handlers at the active loadout's own slots, so a click or
        /// quick-equip places, removes or hides a piece in this loadout instead of the one it borrows from.
        /// </summary>
        public static void RedirectLocalToPrivate()
        {
            if (!VanityLayout.Ready) return;
            if (!HandlerSync.TryGetContext(out var player, out int preset, out _)) return;
            var h = player.vanitySlotsHandler;
            if (h == null) return;
            h.helmVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Helm);
            h.breastVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Breast);
            h.pantsVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Pants);
        }
    }
}
