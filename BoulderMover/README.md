# Movable Ore Boulders

![Movable Ore Boulders: mod cover from in-game screenshots](../release/bouldermover_logo.png)

Mine an ore boulder loose and carry it to where your drills are.

## How to use

- Hit an ore boulder with a pickaxe. Your pickaxe needs enough mining damage to break a **Maze Block** (the ancient maze walls). Weaker pickaxes do nothing to boulders.
- Keep hitting. After about three Maze Blocks' worth of mining, the boulder pops out as an item and flies to you.
- Place it anywhere like any other object, for example next to your drill setup.
- Mining a boulder never gives ore. Drills work as before and are still the only way to get ore out of a boulder.

No settings. Covers every ore boulder type, including pandorium and relucite.

## Requirements and limits

No library dependencies. Install on the host and every client in multiplayer; the host decides when a boulder comes loose.

- Only untouched boulders can be picked up. A boulder that drills have already worn down stays where it is (a placed boulder starts fresh, so moving a half-drilled one would refill it). With Infinite Ore Boulders every boulder stays untouched.
- Explosions and weapons don't move boulders and don't drop ore from them.
- There is no progress bar while you mine a boulder loose; you see the hits, and then the boulder pops out.
- **Compatibility:** this mod changes how ore boulders take damage. It may not work with other mods that change ore boulder mining, damage or destruction.
