// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Fast Auto Fishing - clean exit.
 * Copyright 2026 Sid.
 *
 * The auto-press holds Second Interact for 0.2 s, which outlasts 20% of the (squashed) pull-up,
 * so the game queues another cast after every catch and fishing never ends on its own. A click
 * landing between casts is ignored, and walking away only works once allowedToLeaveStateTimer
 * runs out, which the fast loop keeps restarting.
 *
 * So when the player clicks Second Interact or moves while fishing, we finish the cast for
 * them: tap the button for a single tick, every PULSE_GAP ticks, until the line comes up (that
 * also lands a nibbling fish), and never during the pull-up, so no recast is queued and the
 * state ends. Single-tick taps keep this safe even when the predicted state we read lags the
 * input by a tick or two; holding until we see the pull-up was not.
 */

using PlayerEquipment;
using PlayerState;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

using mikufish; namespace mikufish {

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(RunSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(spoof_sys))]
public partial class exit_sys : SystemBase {

const int PULSE_GAP = 8;

EntityQuery player_query;
EntityQuery time_query;
bool exiting;
bool first_cast = true;
bool saw_pull_up;
NetworkTick pulse_tick;

protected override void OnCreate()
{
	player_query = GetEntityQuery(ComponentType.ReadWrite<ClientInputData>(),
				      ComponentType.ReadOnly<EquipmentSlotCD>(),
				      ComponentType.ReadOnly<PlayerStateCD>(),
				      ComponentType.ReadOnly<FishingStateCD>(),
				      ComponentType.ReadOnly<GhostOwnerIsLocal>());
	time_query = GetEntityQuery(new EntityQueryDesc {
		All = new[] { ComponentType.ReadOnly<NetworkTime>() },
		Options = EntityQueryOptions.IncludeSystems,
	});
	RequireForUpdate(player_query);
}

void stop_exit(string why)
{
	if (exiting)
		Debug.Log($"[FastAutoFishing] exit finished: {why}");
	exiting = false;
	saw_pull_up = false;
	pulse_tick = NetworkTick.Invalid;
}

protected override void OnUpdate()
{
	if (player_query.CalculateEntityCount() != 1)
		return;

	var player = player_query.GetSingletonEntity();
	var spoof = EntityManager.GetComponentData<spoof_cd>(spoof_sys.ent);
	var slot = EntityManager.GetComponentData<EquipmentSlotCD>(player);
	var pstate = EntityManager.GetComponentData<PlayerStateCD>(player);

	if (!spoof.enabled || slot.slotType != EquipmentSlotType.FishingRodSlot ||
	    !pstate.HasAnyState(PlayerStateEnum.Fishing)) {
		stop_exit("left fishing");
		first_cast = true;
		return;
	}

	if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive())
		return;

	var state = EntityManager.GetComponentData<FishingStateCD>(player);
	var input_view = EntityManager.GetComponentData<ClientInputData>(player);
	var input = UnsafeUtility.As<ClientInputData, ClientInput>(ref input_view);

	/*
	 * The click that started fishing can still be in the input mask, and holding it during
	 * the first cast sets the throw distance; only listen once that line is out.
	 */
	if (first_cast) {
		if (state.castTimer.isRunning)
			return;
		first_cast = false;
	}

	/* spoof only ever sets HeldDown, so Pressed and movement are the player's own */
	if (!exiting) {
		bool clicked = input.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_Pressed);
		if (!clicked && math.lengthsq(input.movementDirection) <= 0.01f)
			return;
		exiting = true;
		Debug.Log($"[FastAutoFishing] exit requested by {(clicked ? "right-click" : "movement")}");
	}

	var tick = time_query.GetSingleton<NetworkTime>().ServerTick;
	bool press = false;

	if (state.IsPullingUp) {
		saw_pull_up = true;
	} else if (saw_pull_up) {
		/*
		 * Pull-up ended but we still read Fishing: either the state pops next tick or a
		 * recast slipped through. Wait a full gap before tapping again; a tap after the
		 * state pops would start a new cast.
		 */
		Debug.Log("[FastAutoFishing] pull-up done, still fishing; waiting");
		saw_pull_up = false;
		pulse_tick = tick;
	} else if (!pulse_tick.IsValid || tick.TicksSince(pulse_tick) >= PULSE_GAP) {
		pulse_tick = tick;
		press = true;
	} else {
		press = tick.Equals(pulse_tick);
	}

	input.SetButtonState(CommandInputButtonStateNames.SecondInteract_HeldDown, press);
	EntityManager.SetComponentData(player, UnsafeUtility.As<ClientInput, ClientInputData>(ref input));
}

} /* partial class exit_sys */

}
