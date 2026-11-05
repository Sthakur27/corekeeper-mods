// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Copyright 2024-2026 Jiamu Sun <barroit@linux.com>
 * Additions Copyright 2026 Sid.
 *
 * Hand-written EntityManager version of the original SystemAPI system (no Scripts/Generated
 * code any more), so OnUpdate is the method that actually runs. That matters for Sid's Overhaul:
 * its "Turn Sub-Mods On/Off" guard is injected into OnUpdate, and with the generated
 * __OnUpdate_* method in place that guard never ran, so the auto-press kept going with the
 * feature switched off. Cheap checks (switch, rod equipped) come first and only read; the
 * player's ClientInputData is read and written only while actually spoofing.
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
EntityQuery time_query;
EntityQuery tick_rate_query;

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
	time_query = GetEntityQuery(new EntityQueryDesc {
		All = new[] { ComponentType.ReadOnly<NetworkTime>() },
		Options = EntityQueryOptions.IncludeSystems,
	});
	tick_rate_query = GetEntityQuery(new EntityQueryDesc {
		All = new[] { ComponentType.ReadOnly<ClientServerTickRate>() },
		Options = EntityQueryOptions.IncludeSystems,
	});
	RequireForUpdate(player_query);
}

protected override void OnUpdate()
{
	if (!EntityManager.Exists(ent))
		return;

	var spoof = EntityManager.GetComponentData<spoof_cd>(ent);
	if (!spoof.enabled)
		return;

	if (player_query.CalculateEntityCount() != 1)
		return;

	var player = player_query.GetSingletonEntity();
	var slot = EntityManager.GetComponentData<EquipmentSlotCD>(player);
	if (slot.slotType != EquipmentSlotType.FishingRodSlot)
		return;

	if (time_query.IsEmptyIgnoreFilter)
		return;
	var tick = time_query.GetSingleton<NetworkTime>().ServerTick;

	if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive()) {
		if (spoof.tmr.isRunning) {
			spoof.tmr.Stop(tick);
			EntityManager.SetComponentData(ent, spoof);
		}

		return;
	}

	uint sim_tps = 60;
	if (!tick_rate_query.IsEmptyIgnoreFilter)
		sim_tps = (uint)tick_rate_query.GetSingleton<ClientServerTickRate>().SimulationTickRate;

	var input_view = EntityManager.GetComponentData<ClientInputData>(player);
	var input = UnsafeUtility.As<ClientInputData, ClientInput>(ref input_view);
	var fishing = EntityManager.GetComponentData<FishingStateCD>(player);
	bool timer_changed = false;

	input.useFishingMiniGame = false;

	if (input.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown))
		goto done;

	if (spoof.tmr.isRunning) {
		if (!spoof.tmr.IsTimerElapsed(tick)) {
			input.SetButtonState(CommandInputButtonStateNames.SecondInteract_HeldDown, true);
			goto done;
		}

		spoof.tmr.Stop(tick);
		timer_changed = true;
	}

	if (fishing.fishIsNibbling && !fishing.isFishingAtOctopusBoss) {
		spoof.tmr.Start(tick, spoof.hold, sim_tps);
		timer_changed = true;
	}

done:
	if (timer_changed)
		EntityManager.SetComponentData(ent, spoof);
	EntityManager.SetComponentData(player, UnsafeUtility.As<ClientInput, ClientInputData>(ref input));
}

protected override void OnDestroy()
{
	if (!EntityManager.Exists(ent) || !EntityManager.HasComponent<spoof_cd>(ent))
		return;

	var spoof = EntityManager.GetComponentData<spoof_cd>(ent);
	var spoof_in = state.load();

	spoof_in.__enabled = spoof.enabled;
	state.save(spoof_in);
}

} /* partial class spoof_system */

}
