#pragma warning disable 0219
#line 1 "/home/barroit/git/ck-mod-sdk/Temp/GeneratedCode/mikufish//spoof__System_12394320590.g.cs"
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using PlayerEquipment;
using PlayerState;
using mikufish;
namespace mikufish
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    public partial class spoof_sys
    {
        [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate_T0")]

void __OnUpdate_450AADF4()
{
	#line 67 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var time = __query_702015382_0.GetSingleton<NetworkTime>();
	#line 68 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var tick = time.ServerTick;
	#line 70 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	var tps = __query_702015382_1.GetSingleton<ClientServerTickRate>();
	#line 71 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var sim_tps = (uint)tps.SimulationTickRate;
	#line 73 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	var player = player_query.GetSingletonEntity();
	#line 75 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	var __spoof = __query_702015382_2.GetSingletonRW<spoof_cd>();
	#line 76 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	ref var spoof = ref __spoof.ValueRW;
	#line 78 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	var __input_view = global::Unity.Entities.Internal.InternalCompilerInterface.GetComponentRWAfterCompletingDependency<global::ClientInputData>(ref __TypeHandle.__ClientInputData_RW_ComponentLookup, ref this.CheckedStateRef, player);
	#line 79 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	ref var input_view = ref __input_view.ValueRW;
	#line 80 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var input = UnsafeUtility.As<ClientInputData, ClientInput>(ref input_view);
	#line 82 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	var slot = global::Unity.Entities.Internal.InternalCompilerInterface.GetComponentAfterCompletingDependency<global::PlayerEquipment.EquipmentSlotCD>(ref __TypeHandle.__PlayerEquipment_EquipmentSlotCD_RO_ComponentLookup, ref this.CheckedStateRef, player);
	#line 83 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var state = global::Unity.Entities.Internal.InternalCompilerInterface.GetComponentAfterCompletingDependency<global::PlayerState.FishingStateCD>(ref __TypeHandle.__PlayerState_FishingStateCD_RO_ComponentLookup, ref this.CheckedStateRef, player);
	#line 85 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (!spoof.enabled)
		#line 86 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		return;
	#line 88 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (slot.slotType != EquipmentSlotType.FishingRodSlot)
		#line 89 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		return;
	#line 91 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	input.useFishingMiniGame = false;
	#line 93 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive()) 
#line 93 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
{
		#line 94 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		if (spoof.tmr.isRunning)
			#line 95 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
			spoof.tmr.Stop(tick);
		#line 97 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

		return;
	}
	#line 100 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (input.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown))
		#line 101 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		goto done;
	#line 103 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (spoof.tmr.isRunning) 
#line 103 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
{
		#line 104 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		if (!spoof.tmr.IsTimerElapsed(tick)) 
#line 104 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
{
			#line 105 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
			input.SetButtonState(CommandInputButtonStateNames.SecondInteract_HeldDown, true);
			#line 106 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
			goto done;
		}
		#line 109 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

		spoof.tmr.Stop(tick);
	}
	#line 112 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	if (state.fishIsNibbling && !state.isFishingAtOctopusBoss)
		#line 113 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
		spoof.tmr.Start(tick, spoof.hold, sim_tps);
#line 115 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

done:
	#line 116 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	__input_view.ValueRW = UnsafeUtility.As<ClientInput, ClientInputData>(ref input);
#line hidden
}

        [global::Unity.Entities.DOTSCompilerPatchedMethod("OnDestroy_T0")]

void __OnDestroy_65239F2F()
{
	#line 121 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var spoof = __query_702015382_3.GetSingleton<spoof_cd>();
	#line 122 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	var spoof_in = state.load();
	#line 124 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"

	spoof_in.__enabled = spoof.enabled;
	#line 125 "/home/barroit/git/ck-mod-sdk/Assets/mikufish/spoof.cs"
	state.save(spoof_in);
#line hidden
}

        
        TypeHandle __TypeHandle;
        global::Unity.Entities.EntityQuery __query_702015382_0;
        global::Unity.Entities.EntityQuery __query_702015382_1;
        global::Unity.Entities.EntityQuery __query_702015382_2;
        global::Unity.Entities.EntityQuery __query_702015382_3;
        struct TypeHandle
        {
            public global::Unity.Entities.ComponentLookup<global::ClientInputData> __ClientInputData_RW_ComponentLookup;
            [global::Unity.Collections.ReadOnly] public global::Unity.Entities.ComponentLookup<global::PlayerEquipment.EquipmentSlotCD> __PlayerEquipment_EquipmentSlotCD_RO_ComponentLookup;
            [global::Unity.Collections.ReadOnly] public global::Unity.Entities.ComponentLookup<global::PlayerState.FishingStateCD> __PlayerState_FishingStateCD_RO_ComponentLookup;
            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref global::Unity.Entities.SystemState state)
            {
                __ClientInputData_RW_ComponentLookup = state.GetComponentLookup<global::ClientInputData>(false);
                __PlayerEquipment_EquipmentSlotCD_RO_ComponentLookup = state.GetComponentLookup<global::PlayerEquipment.EquipmentSlotCD>(true);
                __PlayerState_FishingStateCD_RO_ComponentLookup = state.GetComponentLookup<global::PlayerState.FishingStateCD>(true);
            }
            
        }
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void __AssignQueries(ref global::Unity.Entities.SystemState state)
        {
            var entityQueryBuilder = new global::Unity.Entities.EntityQueryBuilder(global::Unity.Collections.Allocator.Temp);
            __query_702015382_0 = 
                entityQueryBuilder
                    .WithAll<global::Unity.NetCode.NetworkTime>()
                    .WithOptions(global::Unity.Entities.EntityQueryOptions.IncludeSystems)
                    .Build(ref state);
            entityQueryBuilder.Reset();
            __query_702015382_1 = 
                entityQueryBuilder
                    .WithAll<global::Unity.NetCode.ClientServerTickRate>()
                    .WithOptions(global::Unity.Entities.EntityQueryOptions.IncludeSystems)
                    .Build(ref state);
            entityQueryBuilder.Reset();
            __query_702015382_2 = 
                entityQueryBuilder
                    .WithAllRW<global::mikufish.spoof_cd>()
                    .WithOptions(global::Unity.Entities.EntityQueryOptions.IncludeSystems)
                    .Build(ref state);
            entityQueryBuilder.Reset();
            __query_702015382_3 = 
                entityQueryBuilder
                    .WithAll<global::mikufish.spoof_cd>()
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
