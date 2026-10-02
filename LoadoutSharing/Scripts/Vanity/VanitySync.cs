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
    /// </summary>
    public static class VanitySync
    {
        private static bool _logged;

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
                    if (e == Entity.Null || !pc.world.EntityManager.Exists(e)) continue;
                    if (!pc.world.EntityManager.HasComponent<ActiveEquipmentPresetCD>(e)) continue;
                    int preset = pc.world.EntityManager.GetComponentData<ActiveEquipmentPresetCD>(e).Value;

                    var h = pc.vanitySlotsHandler;
                    h.helmVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Helm);
                    h.breastVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Breast);
                    h.pantsVanitySlotInventoryHandler.startPosInBuffer = VanityLayout.Slot(preset, VanityKind.Pants);
                }
            }
            catch (Exception ex)
            {
                if (_logged) return;
                _logged = true;
                Debug.LogError($"[{LoadoutSharingMod.Name}] VanitySync: {ex}");
            }
        }
    }
}
