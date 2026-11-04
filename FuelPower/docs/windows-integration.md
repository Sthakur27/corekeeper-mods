# Integration contract and Windows validation

## First pass: observe

1. Build the standalone `FuelPowerPrototype` with the same game SDK used by Faster Machines. Keep it out of the combined Overhaul build while experimental.
2. Confirm `FurnaceProbeSystem` compiles and is discovered on the server. Start a disposable world with one ordinary furnace.
3. Insert a recipe and inspect `[FuelPowerPrototype]` logs. Confirm slot/timer values while cooking, idle, blocked, picked up and replaced. The probe samples after crafting and cannot establish a safe pre-craft gate by itself.
4. Repeat with Faster Machines at 1x and 5x. The probe must leave vanilla behavior unchanged and stop logging after twelve reports. No new fuel items should appear.

## Required before gameplay writes

- Inspect current game assemblies for the start, advance and finish paths under `PugAutomationCraftingSystemGroup`. These may be Burst systems; ordinary Harmony patches may not intercept them.
- Find a gate before ingredients are consumed and before output is emitted. Restoring a timer after the finish group is too late. Gate all processing paths for the chosen furnace, including automation.
- Wire the pure `FuelMachine.Advance` transition to a server-owned transaction. Validate inventory/output capacity first; commit consumed fuel, progress, ingredients, output and burn credit together. A plan is not a committed action. Calling Advance twice on the same old snapshot produces two equivalent plans, so the adapter must reject stale/replayed commits.
- Do not let both vanilla and the mod independently advance the same recipe. Supply baseline work from elapsed simulation time and the effective speed once. Define recipe start, cancellation, pickup and destruction refunds. Keep the first release to one slot if necessary; sharing fuel among slots needs explicit allocation.
- Add an explicit wood fuel inventory or adjacent fuel box with an unambiguous ownership/link rule. Do not silently take wood from arbitrary nearby storage. Handle simultaneous multiplayer inventory changes and fuel item removal through supported inventory operations.
- Persist remaining work, burn credit, fuel inventory and schema version using verified save APIs. Associate machine state with durable identity, not transient Entity index/version. Verify chunk unloading, world transitions, pickup/replacement, restart and migrations. Pure snapshot tests do not prove this.
- Replicate status/progress to clients. Fuel checks and drop rolls execute only on the authoritative server. Matching mod content is required on all peers once custom objects exist.

## Coal event adapter

- Register a stable coal object name, icon and stackable item before spawning any coal.
- Locate a server event for a completed wall destruction, not a damage tick. Check eligible material, natural-vs-player-placed provenance and destruction source. If provenance cannot be proven, keep wall drops disabled; do not assume every mined wall is natural.
- Feed one random sample in [0,1) per eligible destruction into `CoalDrops.FromWall`. Example initial tuning: 5%, one coal. This value is not yet a runtime setting. Preserve vanilla wall drops and treasure talent behavior; no talent investment is required for coal.
- Deduplicate events if the hook can be replayed. Reject duplicate callbacks, client callbacks, placed walls, unknown origin and unrelated tile types. Decide explicitly whether automated mining or explosions qualify; both default off in the pure policy.
- For mobs, choose explicit enemy IDs. Invoke `CoalDrops.FromMob` once on server death, preserving other loot. Evaluate mob-farm output before balancing generator costs.

## First playable acceptance checks

| Scenario | Expected |
|---|---|
| Recipe loaded, no wood | No production or ingredient loss |
| Fuel runs out mid-recipe | Progress stops exactly at paid work; refueling resumes |
| Output blocked or no recipe | No additional fuel or burn-credit consumption |
| More fuel than needed | Unused burn credit carries to next recipe |
| Faster Machines 5x | Same energy per equivalent work; faster energy draw |
| Save/reload or chunk unload | No reset, free output, lost fuel, or replayed completion |
| Two clients interact | One authoritative inventory change and output |
| Pickup, destruction, cancellation | Defined refund/drop behavior; no duplication |
| Mine natural / placed / unknown wall | Coal only from verified eligible natural destruction |
| Drill/explosion or duplicate callback | No unintended/repeated coal drops |

Only after these pass should the prototype enforce fuel in ordinary play. Next milestones are cooking/smelter coverage, fueled generators, a supply/demand network, then solar and batteries. Existing `ElectricityCD` powered state does not by itself prove network capacity accounting is available.
