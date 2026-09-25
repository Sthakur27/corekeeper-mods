using Unity.Entities;
using Unity.NetCode;

namespace MasterPet
{
    public struct SetPetTalentIDRPC : IRpcCommand
    {
        public Entity inventoryEntity;
        public int index;
        public ObjectID objectID;
        public int talentIndex;
        public PetTalent newTalentID;
    }
}