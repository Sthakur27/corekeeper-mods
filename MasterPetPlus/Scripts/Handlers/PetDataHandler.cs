using Unity.Entities;

namespace MasterPet.Handlers
{
    public static class PetDataHandler
    {
        public static bool TryGetEquippedPet(PlayerController player, out ContainedObjectsBuffer contained)
        {
            contained = default;

            if (player == null)
                return false;

            InventoryHandler petInventory = player.equipmentHandler?.petInventoryHandler;
            if (petInventory == null)
                return false;

            contained = petInventory.GetContainedObjectData(0);
            return contained.objectID != ObjectID.None;
        }

        public static bool TryGetPetInventory(PlayerController player, out InventoryHandler petInventory)
        {
            petInventory = player?.equipmentHandler?.petInventoryHandler;
            return petInventory != null;
        }

        public static bool TryGetPetCD(ContainedObjectsBuffer contained, out PetCD petCD)
        {
            return PugDatabase.TryGetComponent(contained.objectData, out petCD);
        }

        public static bool TryGetPetSkin(ContainedObjectsBuffer contained, out PetSkinCD skin)
        {
            return InventoryHandler.TryGetExtraInventoryData(contained, out skin);
        }

        public static bool TryGetPetTalents(ContainedObjectsBuffer contained, out DynamicBuffer<PetTalentBuffer> talents)
        {
            return InventoryHandler.TryGetExtraInventoryBuffer(contained, out talents);
        }

        public static int GetPetLevel(ContainedObjectsBuffer contained)
        {
            return PetExtensions.GetLevelFromXP(contained.amount);
        }
    }
}