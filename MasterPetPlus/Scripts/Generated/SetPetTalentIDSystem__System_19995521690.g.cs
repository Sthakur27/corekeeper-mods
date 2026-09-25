#pragma warning disable 0219
#line 1 "E:/pugsdk/Temp/GeneratedCode/MasterPet//SetPetTalentIDSystem__System_19995521690.g.cs"
using Unity.Entities;
using Unity.NetCode;
namespace MasterPet
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    public partial class SetPetTalentIDSystem
    {
        [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate_T0")]

        void __OnUpdate_450AADF4()
        {
            #line 20 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
            InventoryAuxDataSystemDataCD auxData = __query_102755940_1.GetSingleton<InventoryAuxDataSystemDataCD>();
            #line 21 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
            InventoryAuxDataAccessor accessor = new InventoryAuxDataAccessor(auxData);
            #line 23 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"

            var talentBufferLookup = GetBufferLookup<PetTalentBuffer>(false);
            #line 24 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
            var containedBufferLookup = GetBufferLookup<ContainedObjectsBuffer>(true);
            #line 26 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"

            EntityCommandBuffer ecb = CreateCommandBuffer();
            #line 28 "E:/pugsdk/Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"

            SetPetTalentIDSystem_36D86E65_LambdaJob_0_Execute(ref accessor, ref talentBufferLookup, ref containedBufferLookup, ref ecb);
        }

        #line 32 "E:/pugsdk/Temp/GeneratedCode/MasterPet//SetPetTalentIDSystem__System_19995521690.g.cs"
        struct SetPetTalentIDSystem_36D86E65_LambdaJob_0_Job : global::Unity.Entities.IJobChunk
        {
            public global::InventoryAuxDataAccessor accessor;
            public global::Unity.Entities.BufferLookup<global::PetTalentBuffer> talentBufferLookup;
            public global::Unity.Entities.BufferLookup<global::ContainedObjectsBuffer> containedBufferLookup;
            public global::Unity.Entities.EntityCommandBuffer ecb;
            [global::Unity.Collections.ReadOnly] public global::Unity.Entities.EntityTypeHandle __rpcEntityTypeHandle;
            [global::Unity.Collections.ReadOnly] public global::Unity.Entities.ComponentTypeHandle<global::MasterPet.SetPetTalentIDRPC> __rpcTypeHandle;
            public static readonly global::Unity.Profiling.ProfilerMarker s_ProfilerMarker = new global::Unity.Profiling.ProfilerMarker("SetPetTalentIDSystem_36D86E65_LambdaJob_0");
            
            void OriginalLambdaBody(global::Unity.Entities.Entity rpcEntity, in global::MasterPet.SetPetTalentIDRPC rpc)
            {
#line 33 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (rpc.inventoryEntity == Entity.Null)
                    {
#line 35 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
#line 36 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
return;
                    }
#line 39 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
DynamicBuffer<ContainedObjectsBuffer> containedBuffers;
#line 40 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (!containedBufferLookup.TryGetBuffer(rpc.inventoryEntity, out containedBuffers))
                    {
#line 42 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
#line 43 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
return;
                    }
#line 46 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (rpc.index < 0 || rpc.index >= containedBuffers.Length)
                    {
#line 48 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
#line 49 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
return;
                    }
#line 52 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ContainedObjectsBuffer contained = containedBuffers[rpc.index];
#line 53 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (contained.objectID != rpc.objectID || contained.auxDataIndex == 0)
                    {
#line 55 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
#line 56 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
return;
                    }
#line 59 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
DynamicBuffer<PetTalentBuffer> talentBuffer;
#line 60 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (!accessor.TryGetBuffer<PetTalentBuffer>(contained.auxDataIndex, talentBufferLookup, out talentBuffer))
                    {
#line 62 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
#line 63 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
return;
                    }
#line 66 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
if (talentBuffer.IsCreated && rpc.talentIndex >= 0 && rpc.talentIndex < talentBuffer.Length)
                    {
#line 68 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
PetTalentBuffer element = talentBuffer[rpc.talentIndex];
#line 69 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
element.petTalentID = rpc.newTalentID;
#line 70 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
talentBuffer[rpc.talentIndex] = element;
                    }
#line 73 "E:\pugsdk\Assets/MasterPet/Systems/SetPetTalentIDSystem.cs"
ecb.DestroyEntity(rpcEntity);
                }
            #line 104 "E:/pugsdk/Temp/GeneratedCode/MasterPet//SetPetTalentIDSystem__System_19995521690.g.cs"
            [global::System.Runtime.CompilerServices.CompilerGenerated]
            public void Execute(in global::Unity.Entities.ArchetypeChunk chunk, int batchIndex, bool useEnabledMask, in global::Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                #line 108 "E:/pugsdk/Temp/GeneratedCode/MasterPet//SetPetTalentIDSystem__System_19995521690.g.cs"
                var __rpcEntityArrayPtr = global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetChunkEntityArrayIntPtr(chunk, __rpcEntityTypeHandle);
                var rpcArrayPtr = global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetChunkNativeArrayReadOnlyIntPtr<global::MasterPet.SetPetTalentIDRPC>(chunk, ref __rpcTypeHandle);
                int chunkEntityCount = chunk.Count;
                if (!useEnabledMask)
                {
                    for(var entityIndex = 0; entityIndex < chunkEntityCount; ++entityIndex)
                    {
                        OriginalLambdaBody(global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetCopyOfNativeArrayPtrElement<global::Unity.Entities.Entity>(__rpcEntityArrayPtr, entityIndex), in global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::MasterPet.SetPetTalentIDRPC>(rpcArrayPtr, entityIndex));
                    }
                }
                else
                {
                    int edgeCount = global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong0 ^ (chunkEnabledMask.ULong0 << 1)) + global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong1 ^ (chunkEnabledMask.ULong1 << 1)) - 1;
                    bool useRanges = edgeCount <= 4;
                    if (useRanges)
                    {
                        int entityIndex = 0;
                        int batchEndIndex = 0;
                        while (global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeTryGetNextEnabledBitRange(chunkEnabledMask, batchEndIndex, out entityIndex, out batchEndIndex))
                        {
                            while (entityIndex < batchEndIndex)
                            {
                                OriginalLambdaBody(global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetCopyOfNativeArrayPtrElement<global::Unity.Entities.Entity>(__rpcEntityArrayPtr, entityIndex), in global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::MasterPet.SetPetTalentIDRPC>(rpcArrayPtr, entityIndex));
                                entityIndex++;
                            }
                        }
                    }
                    else
                    {
                        ulong mask64 = chunkEnabledMask.ULong0;
                        int count = global::Unity.Mathematics.math.min(64, chunkEntityCount);
                        for (var entityIndex = 0; entityIndex < count; ++entityIndex)
                        {
                            if ((mask64 & 1) != 0)
                            {
                                OriginalLambdaBody(global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetCopyOfNativeArrayPtrElement<global::Unity.Entities.Entity>(__rpcEntityArrayPtr, entityIndex), in global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::MasterPet.SetPetTalentIDRPC>(rpcArrayPtr, entityIndex));
                            }
                            mask64 >>= 1;
                        }
                        mask64 = chunkEnabledMask.ULong1;
                        for (var entityIndex = 64; entityIndex < chunkEntityCount; ++entityIndex)
                        {
                            if ((mask64 & 1) != 0)
                            {
                                OriginalLambdaBody(global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetCopyOfNativeArrayPtrElement<global::Unity.Entities.Entity>(__rpcEntityArrayPtr, entityIndex), in global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::MasterPet.SetPetTalentIDRPC>(rpcArrayPtr, entityIndex));
                            }
                            mask64 >>= 1;
                        }
                    }
                }
            }
            public static void RunWithoutJobSystem(ref global::Unity.Entities.EntityQuery query, global::System.IntPtr jobPtr)
            {
                try
                {
                    ref var jobData = ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<SetPetTalentIDSystem_36D86E65_LambdaJob_0_Job>(jobPtr);
                    global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface.RunWithoutJobsInternal(ref jobData, ref query);
                }
                finally
                {
                }
            }
        }
        void SetPetTalentIDSystem_36D86E65_LambdaJob_0_Execute(ref global::InventoryAuxDataAccessor accessor,ref global::Unity.Entities.BufferLookup<global::PetTalentBuffer> talentBufferLookup,ref global::Unity.Entities.BufferLookup<global::ContainedObjectsBuffer> containedBufferLookup,ref global::Unity.Entities.EntityCommandBuffer ecb)
        {
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            __TypeHandle.__MasterPet_SetPetTalentIDRPC_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            var __job = new SetPetTalentIDSystem_36D86E65_LambdaJob_0_Job
            {
                accessor = accessor,
                talentBufferLookup = talentBufferLookup,
                containedBufferLookup = containedBufferLookup,
                ecb = ecb,
                __rpcEntityTypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                __rpcTypeHandle = __TypeHandle.__MasterPet_SetPetTalentIDRPC_RO_ComponentTypeHandle
            };
            
            using (SetPetTalentIDSystem_36D86E65_LambdaJob_0_Job.s_ProfilerMarker.Auto())
            {
                if(!__query_102755940_0.IsEmptyIgnoreFilter)
                {
                    this.CheckedStateRef.CompleteDependency();
                    var __jobPtr = global::Unity.Entities.Internal.InternalCompilerInterface.AddressOf(ref __job);
                    SetPetTalentIDSystem_36D86E65_LambdaJob_0_Job.RunWithoutJobSystem(ref __query_102755940_0, __jobPtr);
                }
            }
            accessor = __job.accessor;
            talentBufferLookup = __job.talentBufferLookup;
            containedBufferLookup = __job.containedBufferLookup;
            ecb = __job.ecb;
        }
        
        TypeHandle __TypeHandle;
        global::Unity.Entities.EntityQuery __query_102755940_0;
        global::Unity.Entities.EntityQuery __query_102755940_1;
        struct TypeHandle
        {
            [global::Unity.Collections.ReadOnly] public global::Unity.Entities.EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [global::Unity.Collections.ReadOnly] public Unity.Entities.ComponentTypeHandle<global::MasterPet.SetPetTalentIDRPC> __MasterPet_SetPetTalentIDRPC_RO_ComponentTypeHandle;
            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref global::Unity.Entities.SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __MasterPet_SetPetTalentIDRPC_RO_ComponentTypeHandle = state.GetComponentTypeHandle<global::MasterPet.SetPetTalentIDRPC>(true);
            }
            
        }
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void __AssignQueries(ref global::Unity.Entities.SystemState state)
        {
            var entityQueryBuilder = new global::Unity.Entities.EntityQueryBuilder(global::Unity.Collections.Allocator.Temp);
            __query_102755940_0 = 
                entityQueryBuilder
                    .WithAll<global::MasterPet.SetPetTalentIDRPC>()
                    .WithAll<global::Unity.NetCode.ReceiveRpcCommandRequest>()
                    .Build(ref state);
            entityQueryBuilder.Reset();
            __query_102755940_1 = 
                entityQueryBuilder
                    .WithAll<global::InventoryAuxDataSystemDataCD>()
                    .WithOptions(global::Unity.Entities.EntityQueryOptions.IncludeSystems)
                    .Build(ref state);
            entityQueryBuilder.Reset();
            entityQueryBuilder.Dispose();
        }
        
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref this.CheckedStateRef);
            __TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }
    }
}
