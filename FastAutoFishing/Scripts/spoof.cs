// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Copyright 2024-2026 Jiamu Sun <barroit@linux.com>
 */

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

using PlayerEquipment;
using PlayerState;

using mikufish; namespace mikufish {

public struct spoof_cd : IComponentData {
	public TickTimer tmr;
	public float hold;
	public bool enabled;
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(RunSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(SendClientInputSystem))]
public partial class spoof_sys : SystemBase {

public static Entity ent;
public static EntityManager entctl;

EntityQuery player_query;

protected override void OnCreate()
{
	var spoof_in = state.load();

	if (spoof_in.hold == 0f) {
		spoof_in.__enabled = true;
		spoof_in.hold = 0.2f;
		state.save(spoof_in);
	}

	entctl = EntityManager;
	ent = EntityManager.CreateSingleton<spoof_cd>();

	var spoof = new spoof_cd {
		tmr = new TickTimer(39),
		hold = spoof_in.hold,
		enabled = spoof_in.__enabled,
	};

	EntityManager.SetComponentData(ent, spoof);

	var hw_input_rw = ComponentType.ReadWrite<ClientInputData>();
	var hotbar_slot_ro = ComponentType.ReadOnly<EquipmentSlotCD>();
	var fishing_state_ro = ComponentType.ReadOnly<FishingStateCD>();
	var local_ghost_ro = ComponentType.ReadOnly<GhostOwnerIsLocal>();

	player_query = GetEntityQuery(hw_input_rw, hotbar_slot_ro,
				      fishing_state_ro, local_ghost_ro);
	RequireForUpdate(player_query);
}

protected override void OnUpdate()
{
	var time = SystemAPI.GetSingleton<NetworkTime>();
	var tick = time.ServerTick;

	var tps = SystemAPI.GetSingleton<ClientServerTickRate>();
	var sim_tps = (uint)tps.SimulationTickRate;

	var player = player_query.GetSingletonEntity();

	var __spoof = SystemAPI.GetSingletonRW<spoof_cd>();
	ref var spoof = ref __spoof.ValueRW;

	var __input_view = SystemAPI.GetComponentRW<ClientInputData>(player);
	ref var input_view = ref __input_view.ValueRW;
	var input = UnsafeUtility.As<ClientInputData, ClientInput>(ref input_view);

	var slot = SystemAPI.GetComponent<EquipmentSlotCD>(player);
	var state = SystemAPI.GetComponent<FishingStateCD>(player);

	if (!spoof.enabled)
		return;

	if (slot.slotType != EquipmentSlotType.FishingRodSlot)
		return;

	input.useFishingMiniGame = false;

	if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive()) {
		if (spoof.tmr.isRunning)
			spoof.tmr.Stop(tick);

		return;
	}

	if (input.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown))
		goto done;

	if (spoof.tmr.isRunning) {
		if (!spoof.tmr.IsTimerElapsed(tick)) {
			input.SetButtonState(CommandInputButtonStateNames.SecondInteract_HeldDown, true);
			goto done;
		}

		spoof.tmr.Stop(tick);
	}

	if (state.fishIsNibbling && !state.isFishingAtOctopusBoss)
		spoof.tmr.Start(tick, spoof.hold, sim_tps);

done:
	__input_view.ValueRW = UnsafeUtility.As<ClientInput, ClientInputData>(ref input);
}

protected override void OnDestroy()
{
	var spoof = SystemAPI.GetSingleton<spoof_cd>();
	var spoof_in = state.load();

	spoof_in.__enabled = spoof.enabled;
	state.save(spoof_in);
}

} /* partial class spoof_system */

}
