using Unity.Entities;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Client-side glue between the fallback rule and the game's equipment UI.
    ///
    /// The equipment UI reads each slot through an InventoryHandler whose start position is a
    /// contained-buffer index. Vanilla only re-points those on a loadout switch. We keep them on
    /// the EFFECTIVE slot every frame (so the UI, tooltips, player sprite and achievements all see
    /// the inherited item), but while a click or shift-equip is being processed we point them at
    /// the loadout's OWN slot, so placing an item creates an override instead of overwriting
    /// loadout 1's item, and clicking an inherited item does nothing.
    /// </summary>
    public static class HandlerSync
    {
        private static bool _loggedError;

        public static bool TryKindOf(ItemSlotsUIType slotType, out SlotKind kind)
        {
            switch (slotType)
            {
                case ItemSlotsUIType.HelmSlot: kind = SlotKind.Helm; return true;
                case ItemSlotsUIType.NecklaceSlot: kind = SlotKind.Necklace; return true;
                case ItemSlotsUIType.BreastSlot: kind = SlotKind.Breast; return true;
                case ItemSlotsUIType.PantsSlot: kind = SlotKind.Pants; return true;
                case ItemSlotsUIType.RingSlot1: kind = SlotKind.Ring1; return true;
                case ItemSlotsUIType.RingSlot2: kind = SlotKind.Ring2; return true;
                case ItemSlotsUIType.OffhandSlot: kind = SlotKind.OffHand; return true;
                case ItemSlotsUIType.BagSlot: kind = SlotKind.Bag; return true;
                case ItemSlotsUIType.LanternSlot: kind = SlotKind.Lantern; return true;
                case ItemSlotsUIType.PetSlot: kind = SlotKind.Pet; return true;
                default: kind = default; return false;
            }
        }

        private static InventoryHandler HandlerFor(EquipmentHandler eh, SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Helm: return eh.helmInventoryHandler;
                case SlotKind.Necklace: return eh.necklaceInventoryHandler;
                case SlotKind.Breast: return eh.breastInventoryHandler;
                case SlotKind.Pants: return eh.pantsInventoryHandler;
                case SlotKind.Ring1: return eh.ring1InventoryHandler;
                case SlotKind.Ring2: return eh.ring2InventoryHandler;
                case SlotKind.OffHand: return eh.offHandInventoryHandler;
                case SlotKind.Bag: return eh.bagInventoryHandler;
                case SlotKind.Lantern: return eh.lanternInventoryHandler;
                case SlotKind.Pet: return eh.petInventoryHandler;
                default: return null;
            }
        }

        /// <summary>Local player, its active loadout and its contained-objects buffer, if all are available.</summary>
        public static bool TryGetContext(out PlayerController player, out int preset, out DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            player = null;
            preset = 0;
            contained = default;
            if (!SlotLayout.Ready) return false;

            var main = Manager.main;
            if (main == null) return false;
            player = main.player;
            if (player == null || player.equipmentHandler == null) return false;

            var world = player.world;
            if (world == null || !world.IsCreated) return false;
            var em = world.EntityManager;
            var entity = player.entity;
            if (!em.Exists(entity) || !em.HasBuffer<ContainedObjectsBuffer>(entity)) return false;

            contained = em.GetBuffer<ContainedObjectsBuffer>(entity, true);
            preset = Mathf.Clamp(player.activeEquipmentPreset, 0, SlotLayout.PresetCount - 1);
            return true;
        }

        /// <summary>True when the local player's given UI slot is currently showing an inherited item.</summary>
        public static bool IsSlotInherited(ItemSlotsUIType slotType)
        {
            if (!TryKindOf(slotType, out var kind)) return false;
            if (!TryGetContext(out _, out int preset, out var contained)) return false;
            return SlotLayout.IsInherited(preset, kind, contained);
        }

        /// <summary>Point every equipment handler at the effective (fallback-aware) slot.</summary>
        public static void SyncToEffective()
        {
            try
            {
                if (!TryGetContext(out var player, out int preset, out var contained)) return;
                var eh = player.equipmentHandler;
                for (int k = 0; k < SlotLayout.KindCount; k++)
                {
                    var kind = (SlotKind)k;
                    var handler = HandlerFor(eh, kind);
                    if (handler == null) continue;
                    int slot = SlotLayout.EffectiveSlot(preset, kind, contained);
                    if (slot >= 0 && slot < contained.Length && handler.startPosInBuffer != slot)
                        handler.SetStartPosInBuffer(slot);
                }
            }
            catch (System.Exception ex)
            {
                LogOnce("SyncToEffective", ex);
            }
        }

        /// <summary>Point every equipment handler at the active loadout's own slot (used around clicks).</summary>
        public static void RedirectToPrivate()
        {
            try
            {
                if (!TryGetContext(out var player, out int preset, out var contained)) return;
                var eh = player.equipmentHandler;
                for (int k = 0; k < SlotLayout.KindCount; k++)
                {
                    var kind = (SlotKind)k;
                    var handler = HandlerFor(eh, kind);
                    if (handler == null) continue;
                    int slot = SlotLayout.PrivateSlot(preset, kind);
                    if (slot >= 0 && slot < contained.Length && handler.startPosInBuffer != slot)
                        handler.SetStartPosInBuffer(slot);
                }
            }
            catch (System.Exception ex)
            {
                LogOnce("RedirectToPrivate", ex);
            }
        }

        private static void LogOnce(string where, System.Exception ex)
        {
            if (_loggedError) return;
            _loggedError = true;
            Debug.LogError($"[{LoadoutSharingMod.Name}] {where} failed: {ex}");
        }
    }
}
