using Inventory;
using Unity.Collections;
using Unity.Entities;

namespace PetEditor
{
    /// <summary>
    /// Server-side edits of a player's equipped pet. The pet is the item in the player's pet slot
    /// (PetOwnerCD.SlotIndex): its XP is the slot's amount (PetHandlerSystem adds XP the same way) and
    /// its talents are the item's PetTalentBuffer, stored as inventory aux data under the slot's
    /// auxDataIndex. Both are replicated, so clients update on their own.
    /// </summary>
    public static class PetServer
    {
        public static string SetLevel(EntityManager em, Entity player, int level)
        {
            if (!TryGetPetSlot(em, player, out int slot, out string err)) return err;
            if (level < 1) level = 1;
            if (level > PetExtensions.maxLevel) level = PetExtensions.maxLevel;

            var inventory = em.GetBuffer<ContainedObjectsBuffer>(player);
            var pet = inventory[slot];
            int xp = PetExtensions.GetXPFromLevel(level);
            pet.objectData.amount = xp;
            inventory[slot] = pet;

            // Going down a level can leave more points placed than the pet has; clear them then.
            int total = PetExtensions.GetTotalTalentPoints(xp);
            if (TryGetTalents(em, pet, out var talents))
            {
                int spent = 0;
                for (int i = 0; i < talents.Length; i++) spent += talents[i].points;
                if (spent > total)
                {
                    for (int i = 0; i < talents.Length; i++)
                    {
                        var t = talents[i];
                        t.points = 0;
                        talents[i] = t;
                    }
                    return $"Pet level {level} ({total} talent points); points were reset.";
                }
            }
            return $"Pet level {level} ({total} talent points).";
        }

        /// <summary>
        /// Queues the game's own SetPetSkin inventory action on the server. Done server-side (instead of the
        /// client's predicted input action) so the change arrives once, without the predicted value being
        /// rolled back and re-applied (a visible flicker).
        /// </summary>
        public static string SetColor(EntityManager em, Entity player, int skin)
        {
            if (!TryGetPetSlot(em, player, out int slot, out string err)) return err;
            var pet = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            if (query.IsEmptyIgnoreFilter) return "Inventory system not ready.";
            var changes = em.GetBuffer<InventoryChangeBuffer>(query.GetSingletonEntity());
            changes.Add(new InventoryChangeBuffer
            {
                playerEntity = player,
                inventoryChangeData = new InventoryChangeData
                {
                    inventoryAction = InventoryAction.SetPetSkin,
                    inventory1 = player,
                    index1 = slot,
                    objectID = pet.objectID,
                    index2 = skin < 0 ? 0 : skin
                }
            });
            return $"Pet color {skin + 1}.";
        }

        public static string SetTalent(EntityManager em, Entity player, int index, PetTalent talent)
        {
            if (!TryGetPetSlot(em, player, out int slot, out string err)) return err;
            var pet = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
            if (!TryGetTalents(em, pet, out var talents)) return "This pet has no talents.";
            if (index < 0 || index >= talents.Length) return $"Talent slot must be 1-{talents.Length}.";
            var t = talents[index];
            t.petTalentID = talent;
            talents[index] = t;
            return $"Talent {index + 1} set to {talent}.";
        }

        public static string RemovePoint(EntityManager em, Entity player, int index)
        {
            if (!TryGetPetSlot(em, player, out int slot, out string err)) return err;
            var pet = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
            if (!TryGetTalents(em, pet, out var talents)) return "This pet has no talents.";
            if (index < 0 || index >= talents.Length) return $"Talent slot must be 1-{talents.Length}.";
            var t = talents[index];
            t.points = 0;
            talents[index] = t;
            return $"Removed the point from talent {index + 1}.";
        }

        private static bool TryGetPetSlot(EntityManager em, Entity player, out int slot, out string error)
        {
            slot = -1;
            error = null;
            if (player == Entity.Null || !em.Exists(player) || !em.HasComponent<PetOwnerCD>(player) || !em.HasBuffer<ContainedObjectsBuffer>(player))
            {
                error = "No player found.";
                return false;
            }
            slot = em.GetComponentData<PetOwnerCD>(player).SlotIndex;
            var inventory = em.GetBuffer<ContainedObjectsBuffer>(player);
            if (slot < 0 || slot >= inventory.Length || inventory[slot].objectID == ObjectID.None)
            {
                error = "Equip a pet first.";
                return false;
            }
            return true;
        }

        private static bool TryGetTalents(EntityManager em, ContainedObjectsBuffer pet, out DynamicBuffer<PetTalentBuffer> talents)
        {
            talents = default;
            if (pet.auxDataIndex == 0) return false;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<InventoryAuxDataSystemDataCD>());
            if (query.IsEmptyIgnoreFilter) return false;
            var aux = query.GetSingleton<InventoryAuxDataSystemDataCD>();
            if (!aux.TryGetEntity<PetTalentBuffer>(pet.auxDataIndex, out Entity e, out _, out _)) return false;
            if (e == Entity.Null || !em.Exists(e) || !em.HasBuffer<PetTalentBuffer>(e)) return false;
            talents = em.GetBuffer<PetTalentBuffer>(e);
            return true;
        }
    }
}
