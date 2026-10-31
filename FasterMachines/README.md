# Faster Machines

![Faster Machines cover: in-game screenshot with an animated speed dial](../release/fastermachines_logo.gif)

Speed up every timed machine on its own dial: **furnace, smelter kiln, glass smelter, fury forge, cooking pot, table saw, egg incubator (hatching) and drills**, plus one setting for any other timed machine. Everything starts at 5x so you can see it working right away; set any machine back to 1x for normal speed.

## Choose your speeds

Open **Settings > Mod Options > Faster Machines**.

| Setting | Choices | Default |
|---|---|---|
| Furnace | 1x, 1.5x, 2x, 3x, 5x, 10x, 20x, 50x, 100x, Instant | 5x |
| Smelter Kiln | same | 5x |
| Glass Smelter | same | 5x |
| Fury Forge | same | 5x |
| Cooking Pot | same | 5x |
| Table Saw (wood into planks) | same | 5x |
| Egg Incubator (hatching) | same | 5x |
| Drills | 1x to 100x | 5x |
| Other timed machines | same as Furnace | 5x |

Changes apply while you play. An item that is already in progress finishes at the old speed; the next one uses the new speed. Progress bars follow the new speed.

**Instant** finishes one item every game tick. Drills get faster by hitting more often, and past the fastest hit rate by hitting harder.

## Requirements and limits

Requires **CoreLib and Mod Options** on the host and every client. The host's settings decide the speed; clients with different values only see a different progress bar.

Disable **Fast Smelter** or other mods that also change machine timers so they don't fight over the same timers.
