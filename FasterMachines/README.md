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

Ore boulders normally weaken every drill hit when they take a lot of drill damage per second, so in vanilla, extra drilling past a point gives less ore, and very fast drills stop doing damage at all. The Drills setting raises that limit by the same multiplier, so 5x drills really get 5x the ore from a boulder. At 1x boulders behave exactly as in vanilla.

## Requirements and limits

Requires **CoreLib and Mod Options** on the host and every client. The host's settings decide the speed; clients with different values only see a different progress bar.
