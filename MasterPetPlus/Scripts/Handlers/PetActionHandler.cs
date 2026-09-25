using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Inventory;

namespace MasterPet.Handlers
{
    public static class PetActionHandler
    {
        private static readonly Dictionary<ObjectID, PetTalent[]> TalentPoolCache = new();

        public static bool TrySetPetXP(PlayerController player, int targetXP)
        {
            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained))
                return false;

            if (!PetDataHandler.TryGetPetInventory(player, out InventoryHandler petInventory))
                return false;

            player.QueueInputAction(new UIInputActionData
            {
                action = UIInputAction.InventoryChange,
                inventoryChangeData = Create.SetAmount(
                    petInventory.inventoryEntity,
                    petInventory.startPosInBuffer + 0,
                    contained.objectID,
                    targetXP
                )
            });

            return true;
        }

        public static bool TrySetPetSkin(PlayerController player, int skinIndex)
        {
            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained))
                return false;

            if (!PetDataHandler.TryGetPetInventory(player, out InventoryHandler petInventory))
                return false;

            player.QueueInputAction(new UIInputActionData
            {
                action = UIInputAction.InventoryChange,
                inventoryChangeData = new InventoryChangeData
                {
                    inventoryAction = InventoryAction.SetPetSkin,
                    inventory1 = petInventory.inventoryEntity,
                    index1 = petInventory.startPosInBuffer + 0,
                    objectID = contained.objectID,
                    index2 = skinIndex
                }
            });

            return true;
        }

        public static bool TrySetPetTalentPoints(PlayerController player, int slotIndex, int points)
        {
            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained))
                return false;

            if (!PetDataHandler.TryGetPetInventory(player, out InventoryHandler petInventory))
                return false;

            player.QueueInputAction(new UIInputActionData
            {
                action = UIInputAction.InventoryChange,
                inventoryChangeData = new InventoryChangeData
                {
                    inventoryAction = InventoryAction.SetPetTalentPoints,
                    inventory1 = petInventory.inventoryEntity,
                    index1 = petInventory.startPosInBuffer + 0,
                    objectID = contained.objectID,
                    index2 = slotIndex,
                    amount = points
                }
            });

            return true;
        }

        public static bool TryGetPetTalentPool(PlayerController player, out List<PetTalent> talents)
        {
            talents = null;

            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained))
                return false;

            if (!TalentPoolCache.TryGetValue(contained.objectID, out PetTalent[] cached))
            {
                DynamicBuffer<PetTalentPoolBuffer> pool =
                    PugDatabase.GetBuffer<PetTalentPoolBuffer>(contained.objectData);

                if (!pool.IsCreated)
                    return false;

                cached = new PetTalent[pool.Length];

                for (int i = 0; i < pool.Length; i++)
                    cached[i] = pool[i].petTalentID;

                TalentPoolCache[contained.objectID] = cached;
            }

            talents = new List<PetTalent>(cached);
            return true;
        }

        private static List<PetTalent> _allTalentsCache;

        // Master Pet Plus: every talent the game knows about (not just this pet's roll pool).
        // Filtered to talents that actually have an entry in PetInfosTable so lookups never throw.
        public static bool TryGetAllPetTalents(out List<PetTalent> talents)
        {
            talents = null;

            if (_allTalentsCache == null)
            {
                PetInfosTable table = Manager.ui?.petInfosTable;
                if (table == null || table.petTalents == null)
                    return false;

                var known = new HashSet<PetTalent>();
                foreach (PetInfosTable.PetTalentInfo info in table.petTalents)
                    known.Add(info.petTalentID);

                var list = new List<PetTalent>();
                foreach (PetTalent talent in (PetTalent[])Enum.GetValues(typeof(PetTalent)))
                {
                    if (known.Contains(talent))
                        list.Add(talent);
                }

                if (list.Count == 0)
                    return false;

                _allTalentsCache = list;
            }

            talents = new List<PetTalent>(_allTalentsCache);
            return true;
        }

        public static bool TrySetPetTalentID(PlayerController player, int slotIndex, PetTalent newTalentID)
        {
            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained))
                return false;

            if (!PetDataHandler.TryGetPetInventory(player, out InventoryHandler petInventory))
                return false;

            EntityManager entityManager = Manager.ecs.ClientWorld.EntityManager;

            Entity rpcEntity = entityManager.CreateEntity(typeof(SetPetTalentIDRPC), typeof(SendRpcCommandRequest));
            entityManager.SetComponentData(rpcEntity, new SetPetTalentIDRPC
            {
                inventoryEntity = petInventory.inventoryEntity,
                index = petInventory.startPosInBuffer + 0,
                objectID = contained.objectID,
                talentIndex = slotIndex,
                newTalentID = newTalentID
            });

            return true;
        }
    }
}