# Keep Minions On Teleport

Core Keeper mod: your summoned minions no longer vanish when you teleport (portals, waypoints,
recall). They stay alive during the teleport and arrive with you. No settings.

## Why they vanished

The teleport turns off the player's physics for its whole 6-second sequence, and the game's minion
system dismisses every minion whose owner has physics turned off (the same check that clears
minions when you die).

## How it works (for modders)

Server side only, three small managed systems in `RunSimulationSystemGroup`:

1. `HideTeleportFromMinionsSystem` (before `MinionHandlerSystem`): for living players in the
   `Teleporting` state, temporarily switches `DisablePhysicsCD` off.
2. `RestoreTeleportAfterMinionsSystem` (right after `MinionHandlerSystem`): switches it back on, so
   every other system sees the teleport exactly as vanilla.
3. `BringMinionsAlongSystem`: once the owner has landed (physics back on), any minion more than
   32 tiles away is moved next to them.

Dying still dismisses minions as usual. Client + server (`requiredOn: 3`); the host decides in
multiplayer.
