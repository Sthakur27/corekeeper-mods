# Boss Bonus Loot

Core Keeper mod: bosses drop guaranteed extra items on top of their normal loot chest.

| Boss | Bonus |
|---|---|
| Ghorm the Devourer | 40 Larva Meat + 40 Golden Larva Meat (always) |
| Azeos the Sky Titan | Chipped Blade (25%), Clear Gemstone (25%), rolled separately |

The items drop at the boss's body and fly to the player who landed the kill (same as vanilla
loot). No settings. Server side; the host decides in multiplayer.

## How it works (for modders)

`BossBonusLootSystem` (server, after `PredictedSimulationSystemGroup`) watches entities with
`BossCD` + enabled `EntityDestroyedCD` and health 0, looks the boss's `ObjectID` up in the `Bonus`
table, rolls each row's chance, and spawns the stacks with `EntityUtility.DropNewEntity`. Each boss entity is handled once.
Add more bosses by adding rows to `Bonus`.
