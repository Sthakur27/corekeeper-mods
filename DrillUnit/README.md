# Drill Unit

An end-game hover craft for Core Keeper. Craft it at the **Rift Statue** for **300 Pandorium Bars and
100 Solarite Bars**, place it on land or water and hop in.

- **Goes anywhere flat:** drives over water, lava, pits and land alike, at 1.6x boat speed (Speeder Boat: 1.3x).
- **Drills what it drives into:** steer into a wall and the unit grinds through a 3-wide, 2-deep area ahead plus the
  blocks beside it, so it keeps its speed through rock; the blocks and ore drop to you.
- **Mines from the seat:** you can also hold a pickaxe, drill, laser drill or shovel and use it as usual while riding.
- **Can't attack:** weapons, off-hand items and bare-handed attacks do nothing while you ride. Food still works.
- **Lay ground (G):** while riding, press **G** to toggle laying floor. Water, lava and pit tiles in a 3x3 area around
  you become floor, using one block from your inventory each (the first block that has a floor version: Dirt
  gives dirt floor). Free in creative worlds. Rebind under Controls → Drill Unit.

Get in and out like a boat (interact). You can only get out next to a walkable tile.

## Requirements

- CoreLib

## Limits

- Work in progress. Hovering over lava has not been tested for damage.
- Its look is a gunmetal-and-gold recolor of the Speeder Boat.
- The weapon lock is applied on the rider's own client. Multiplayer is untested.

## Development

- `Unity/dump_vanilla.py` dumps the vanilla Speeder Boat prefab to `Unity/vanilla_boat.json`.
- `Unity/make_art.py` draws the inventory icon; `Unity/build_bundle.py` builds `Bundles/` with the Mod SDK and
  runs `Unity/verify_bundle.py`.
- `Unity/extract_skin.py` exports the vanilla boat sheets; `Unity/make_skin.py` recolors the Speeder Boat's into ours.
- How it works: the unit is a boat to the game. `HoverColliderSystem` gives a rider a collider that ignores the
  water, shoreline and low (pit) layers; `StuckGuardSystem` keeps the game's stuck rescue from pulling the rider
  back; `UnitKeepSystem` stops the not-on-a-valid-tile check from turning the unit back into an item and keeps a
  ridden unit's health full; `DrillMineSystem` damages the wall tiles ahead of and beside a steering rider; `SendInputPatch`
  drops attack input unless a mining tool is held and sends the lay-ground toggle (input bit 29);
  `LayGroundSystem` floors the cell under the rider on the server.
