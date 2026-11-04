# Drill Unit (Terraria Drill Containment Unit style) — research notes

Researched 2026-11-03 against the 2026-10-31 decompile (`Pug.Other`, `Pug.ECS.Components`, `Pug.Base`);
every type/field named below was re-checked by name in the 2026-11-02 game DLLs.

Goal: late-game ride that drives on land AND water, cannot attack, mines, and can optionally lay ground
tiles under itself (toggle key).

## 1. Water vs land is a physics filter, not movement code

- `PhysicsLayerID` (Pug.Base): `WaterCollider = 0x20000` (deep water and lava, lava = water tileset 3),
  `ShorelineCollider = 0x40000` (keeps boats off land), `DefaultLowCollider = 0x100` (pits etc.).
- `PlayerColliderVariationPostConverter` bakes three blobs into `PlayerColliderCD`:
  `defaultCollider` (collides with water), `boatRidingCollider` (= default minus Water plus Shoreline),
  `grapplingHookCollider` (= default minus Water minus DefaultLow).
- `SelectPlayerColliderSystem` (Burst ISystem, `BeforePhysicsSystemGroup`, server + client) writes
  `PhysicsCollider.Value` every tick: grappling -> grappling blob, BoatRiding -> boat blob, else default.
- **Plan:** managed system `[UpdateInGroup(BeforePhysicsSystemGroup)] [UpdateAfter(SelectPlayerColliderSystem)]`
  that, for players riding a Drill Unit, overwrites `PhysicsCollider.Value` with our own blob =
  default minus Water (and NOT plus Shoreline). Build it once per world by cloning `defaultCollider`
  and changing the filter, exactly like the post converter does. Run in both worlds (client predicts).
  Optional: also drop `DefaultLowCollider` to hover over pits.

## 2. Things that fight a rider over the "wrong" terrain

- `ResolveStuckPositionSystem` main job (Burst): sphere test at the player with filter
  `0x20101` (Default | DefaultLow | Water; boat: minus Water plus Shoreline). Blocked for 0.1 s ->
  teleport to a last-walked tile (boats: water tiles only); still stuck 2 s later -> **kills the player**.
  Skipped for NoClip/Death/invincible/god mode only.
  **Fix:** managed system `[UpdateAfter(ResolveStuckPositionSystem)]` that `ClearStart()`s
  `PlayerUnstuckCD.stuckTimer` and `killIfStuckTimer` every tick while riding the Drill Unit, so the
  0.1 s timer never elapses. (Re-check ordering: the job is scheduled, so our system must complete
  its dependency or run in a later group.)
- `AutoClaimBoatIfInWaterJob` (same system): a non-boat-riding player standing on a water tile with a
  free vanilla boat within 0.95 tiles is switched into that boat. Only matters if the Drill Unit is
  NOT itself a boat; with the boat approach below it is skipped (BoatRiding returns early).
- Lava is `TileType.water` tileset 3 and shares `WaterCollider`, so hovering also crosses lava.
  Untested whether lava damage applies to a rider; test early. Could be a feature or need a block.

## 3. Which riding state to use

| State | Movement | Held tool use | Notes |
|---|---|---|---|
| `BoatRiding` (`BoatCD`) | WASD, `BoatCD.speedMultiplier` | **Allowed** (QueueHitSystem, `CurrentStateAllowInteractions`); attacks from a boat hit walls only (PlayerController ~6486) | `Boat` MonoBehaviour: skins per ObjectID, water particles, motorboat loop |
| `VehicleRiding` (`VehicleCD`) | tank controls (accelerate/turn), drift, honk, rams destructibles | **Blocked** (only eating) | Aim UI hidden |
| `MountRiding` | WASD | Blocked | creature mounts with saddles |

**Recommendation: build it as a boat.** WASD movement like the Terraria DCU, and primary use of the
held tool already works while riding, so mining with the player's own pickaxe/drill/`LaserDrillTool`
(id 91) comes for free. Things to add on top:
- **No attacking:** while riding, allow primary use only for mining tool slots (pickaxe/drill/laser drill).
  Find the earliest managed or patchable point in the attack path (Burst jobs can't be patched); fallback:
  zero creature damage server-side for hits by a Drill Unit rider, or force-unequip weapons on boarding.
- **Land side:** collider override (section 1), stuck-timer clear (section 2). `BoatRiding.AttemptToLeaveBoat`
  already looks for a walkable neighbour tile, so dismounting on land works.
- **Placement:** placement rules come from `ObjectPropertiesCD` flags read in `PlacementHandler.Activate`
  (`canPlaceOnWalkableTiles`, `canPlaceOnWater`, ...). Give the item both flags so it can be placed on
  land or water (a single placement can't straddle both, which is fine). `HasLimitedPlacementRange` only
  special-cases vanilla boat ids.
- **Visuals:** `Boat.UpdateBoatSkin` picks textures from `boatSkins` by ObjectID; add our skin at runtime
  (Harmony on `OnSpawn`/`UpdateBoatSkin`). Postfix `ManagedLateUpdate` to stop water particles off water.
  Keep using the vanilla `Boat` MonoBehaviour in the prefab (bundle MonoScript rule, AGENTS.md section 14).
- Speed: `BoatCD.speedMultiplier` (Vehicle Speed already touches every BoatCD: make it skip the Drill Unit
  or treat it as its own entry).

## 4. Lay-ground toggle

Reuse Build Tools: `InputBits` for a client key -> server, then `EntityUtility.AddTile(tileset, TileType.ground,
pos, creative, TileUpdateBuffer)` on the tile under/ahead of the rider when it is water/pit/lava. Adding ground
over water removes the water (see `BrushPlaceSystem.IsAddSideEffect`). Consume a ground block from the rider's
inventory (Build Tools' inventory-change path), stop when out of blocks, and never touch Core cells or
tileset 2 (`BrushPlaceSystem.AddAllowed`).

## 5. New object

Needs the Mod SDK bundle pipeline (Ender Stash / Solarite Drill): logic prefab with `ObjectAuthoring`,
`InventoryItemAuthoring`, the vanilla boat authoring components copied from the `SpeederBoat` prefab
(dump with UnityPy), `BoatCD` values, object property flags for land + water placement, recipe at a
late-game workbench (e.g. Solarite workbench), plus a graphics prefab reusing the boat sprite sheet.

## 6. Open questions to test in game

1. Does the collider override alone let the boat cross shoreline onto land smoothly, or does the
   boat entity's own collider (it follows the player via `ControllableLocalToWorldSystem`) snag?
2. Lava damage while hovering.
3. Does anything else (sound, wake, `WaterSim`) misbehave on land?
4. Multiplayer: the override and timer clear must run identically on server and predicting client.
