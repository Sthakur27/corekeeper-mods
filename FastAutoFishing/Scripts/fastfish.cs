// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Fast Auto Fishing - timer squash.
 * Derived from mikufish, Copyright 2024-2026 Jiamu Sun <barroit@linux.com> (GPL-3.0-or-later).
 * Additions Copyright 2026 Sid.
 *
 * The fishing state machine (PlayerState.Fishing) is Burst-compiled, so its hard-coded waits
 * cannot be patched. Instead, every tick we look at each fishing player's FishingStateCD and
 * shorten any running timer that is longer than our cap. Runs identically in the client and
 * server worlds so prediction stays in agreement. Hit chance, loot tables, bait use, XP and
 * shoal behaviour are untouched; only the waiting goes away.
 */

using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

using mikufish; namespace mikufish {

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PlayerStateSystemGroup), OrderLast = true)]
public partial class fastfish_sys : PugSimulationSystemBase {

	// Seconds. The nibble window must stay long enough for the auto-press (0.2 s hold) to land.
	const float WAIT_CAP    = 0.05f;   // wait for a bite / retry after a fake nibble
	const float NIBBLE_CAP  = 0.6f;    // window in which the bite must be answered
	const float ANIM_CAP    = 0.25f;   // cast, throw and pull-up animations

	EntityQuery players;
	EntityQuery tickRateQuery;

	protected override void OnCreate()
	{
		base.OnCreate();
		players = GetEntityQuery(ComponentType.ReadOnly<PlayerGhost>(), ComponentType.ReadWrite<FishingStateCD>());
		tickRateQuery = GetEntityQuery(ComponentType.ReadOnly<ClientServerTickRate>());
		RequireForUpdate(players);
	}

	static bool Cap(ref TickTimer t, float seconds, uint tickRate)
	{
		if (!t.isRunning) return false;
		uint target = NetworkTimeUtilities.SecondsToTicks(seconds, tickRate);
		if (target < 1) target = 1;
		if (t.targetTicks <= target) return false;
		t.targetTicks = target;
		return true;
	}

	protected override void OnUpdate()
	{
		uint tickRate = 60;
		if (!tickRateQuery.IsEmptyIgnoreFilter)
			tickRate = (uint)tickRateQuery.GetSingleton<ClientServerTickRate>().SimulationTickRate;

		var ents = players.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < ents.Length; i++) {
			var e = ents[i];
			var s = EntityManager.GetComponentData<FishingStateCD>(e);
			bool changed = false;

			// Do not shorten the wait while fishing the octopus boss; that fight has its own pacing.
			if (!s.isFishingAtOctopusBoss)
				changed |= Cap(ref s.fishBiteTimer, s.fishIsNibbling ? NIBBLE_CAP : WAIT_CAP, tickRate);

			changed |= Cap(ref s.castTimer, ANIM_CAP, tickRate);
			changed |= Cap(ref s.throwTimer, ANIM_CAP, tickRate);
			changed |= Cap(ref s.pullUpTimer, ANIM_CAP, tickRate);

			if (changed)
				EntityManager.SetComponentData(e, s);
		}
		ents.Dispose();
	}

} /* class fastfish_sys */

}
