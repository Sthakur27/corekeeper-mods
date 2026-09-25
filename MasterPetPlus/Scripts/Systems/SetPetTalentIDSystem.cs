using Unity.Entities;
using Unity.NetCode;

namespace MasterPet
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    public partial class SetPetTalentIDSystem : PugSimulationSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            UpdatesInRunGroup();
            RequireForUpdate<SetPetTalentIDRPC>();
            RequireForUpdate<ReceiveRpcCommandRequest>();
        }

        protected override void OnUpdate()
        {
            InventoryAuxDataSystemDataCD auxData = SystemAPI.GetSingleton<InventoryAuxDataSystemDataCD>();
            InventoryAuxDataAccessor accessor = new InventoryAuxDataAccessor(auxData);

            var talentBufferLookup = GetBufferLookup<PetTalentBuffer>(false);
            var containedBufferLookup = GetBufferLookup<ContainedObjectsBuffer>(true);

            EntityCommandBuffer ecb = CreateCommandBuffer();

            Entities
                .WithAll<SetPetTalentIDRPC>()
                .WithAll<ReceiveRpcCommandRequest>()
                .ForEach((Entity rpcEntity, in SetPetTalentIDRPC rpc) =>
                {
                    if (rpc.inventoryEntity == Entity.Null)
                    {
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    DynamicBuffer<ContainedObjectsBuffer> containedBuffers;
                    if (!containedBufferLookup.TryGetBuffer(rpc.inventoryEntity, out containedBuffers))
                    {
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    if (rpc.index < 0 || rpc.index >= containedBuffers.Length)
                    {
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    ContainedObjectsBuffer contained = containedBuffers[rpc.index];
                    if (contained.objectID != rpc.objectID || contained.auxDataIndex == 0)
                    {
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    DynamicBuffer<PetTalentBuffer> talentBuffer;
                    if (!accessor.TryGetBuffer<PetTalentBuffer>(contained.auxDataIndex, talentBufferLookup, out talentBuffer))
                    {
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    if (talentBuffer.IsCreated && rpc.talentIndex >= 0 && rpc.talentIndex < talentBuffer.Length)
                    {
                        PetTalentBuffer element = talentBuffer[rpc.talentIndex];
                        element.petTalentID = rpc.newTalentID;
                        talentBuffer[rpc.talentIndex] = element;
                    }

                    ecb.DestroyEntity(rpcEntity);
                })
                .WithoutBurst()
                .Run();
        }
    }
}