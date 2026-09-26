# Faster Mushrooms

Core Keeper mod: mycelium roots spread faster, mushrooms grow faster, and picked wild mushrooms
respawn near you instead of almost never. Only mushrooms; every other crop and root plant keeps
its vanilla timing.

## What it does

In Core Keeper the mushroom is a "root plant": each mushroom tile keeps a random countdown
(between the prefab's `minTimeBetweenSpread` and `maxTimeBetweenSpread`) for every neighbouring
mycelium cell it could spread into, and when a countdown ends a new mushroom tile appears there.
This mod divides that spread window by a multiplier you pick, so the whole patch fills in and
regrows after harvesting that many times faster. Mushrooms that grow in stages get their
per-stage time divided the same way.

Settings (Mod Settings menu, section "Faster Mushrooms"):

| Setting | Choices | Default |
|---|---|---|
| Mushroom growth speed | 1x, 1.5x, 2x, 3x, 4x, 5x, 6x, 8x, 10x, 15x, 20x, 30x, 50x, 100x | **4x** |
| Wild mushroom respawn | Off, 15 s, 30 s, 1 min, 2 min, 5 min, 10 min, 30 min | **1 min** |

### Wild mushroom respawn

In vanilla, wild mushrooms come back only through the world's environment respawn, which visits
one 16x16 area at a time (each area roughly once every 4 hours) and skips everything within 200
tiles of a player. Mushrooms you pick near your base essentially never return.

With this setting on, every interval the mod asks the game to run its own respawn roll, for
mushrooms only, in every 16x16 area within 48 tiles of each player. The vanilla rules still decide
where they can appear: the right ground and biome, a free tile, not within 6 tiles of a player,
and fewer new ones when an area already has plenty. Other plants, ores and critters are not
respawned by this.

`1x` is exactly vanilla. Changes apply on the next simulation tick, no restart or reload needed;
countdowns that are already running are shortened to the new maximum immediately.

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Settings Menu](https://mod.io/g/corekeeper/m/mod-settings-menu)

Client + server (`requiredOn: 3`). The speed is applied by the server (host), so in multiplayer
the host's setting is the one that counts.

## How it works (for modders)

The game's `RootPlantGrowSystem` rolls the spread timers inside a Burst-compiled job and
`PugFloraSystem` (Burst `ISystem`) counts them down, so nothing can be patched with Harmony.
Instead `FasterMushroomsSystem` (a managed `PugSimulationSystemBase`, server world, ordered
before `RootPlantGrowSystem`) edits component data every tick:

1. `RootPlantCD.minTimeBetweenSpread / maxTimeBetweenSpread` on every entity that has
   `RootPlantCD` and whose `ObjectDataCD.objectID` name contains "Mushroom" (Mushroom,
   GlowingMushroom, MossMushroom, ...) are set to `vanilla / multiplier`. Vanilla values are
   remembered per ObjectID the first time such an entity is seen.
2. `PugFloraGrowerCD.timer` (ticks) of pending spreads whose source entity is a mushroom is
   capped at `ceil(vanillaMax / multiplier * tickRate)`. Timers rolled from the shortened window
   are already below the cap, so this only touches timers from before the mod / a settings change.
3. `GrowTimerCD.StageTime` of grow timers that belong to a mushroom is set to the mushroom's
   vanilla stage time (`ObjectPropertiesCD`) divided by the multiplier.

Every write is guarded by a compare, so at `1x` nothing is ever written.

## Limitations

- Wild respawn works by briefly zeroing the respawn chance of every non-mushroom entry in the
  environment spawn table while its own requests are processed (a frame or two), then restoring
  it. A vanilla respawn that lands in that same frame skips its non-mushroom objects once.

- Which tiles mycelium can spread onto, how far it spreads, what mushrooms drop and how they are
  harvested are unchanged; only the waiting is shorter.
- Mushroom growth stages only tick on watered ground in vanilla (that is how the game's
  `PlantsGrowingSystem` works); the mod does not change that rule, it only shortens each stage.
- Mushrooms are identified by ObjectID name, so a mushroom-like plant with a differently named
  ObjectID would not be affected.
