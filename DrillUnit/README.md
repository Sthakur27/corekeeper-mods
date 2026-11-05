# Drill Pod (Alpha)

An end-game hover craft for Core Keeper. Craft it at the **Rift Statue** for **300 Pandorium Bars and
100 Solarite Bars**, place it on land or water and hop in.

**Alpha:** it works in single player and is still being tuned. Expect changes, and report anything odd.
Formerly called Drill Unit; placed units and saves carry over.

- **Goes anywhere flat:** drives over water, lava, pits and land alike, at 1.6x boat speed (Speeder Boat: 1.3x).
- **Drills what it drives into:** hold **left click** and steer into a wall, and the unit grinds through a 3-wide, 2-deep area ahead plus the
  blocks beside it, so it keeps its speed through rock; the blocks and ore drop to you.
- **Mines from the seat:** you can also hold a pickaxe, drill, laser drill or shovel and use it as usual while riding.
- **Can't attack:** weapons, off-hand items and bare-handed attacks do nothing while you ride. Food still works.
- **Ground mode (G):** while riding, press **G** to cycle **off → lay floor → dig → off**. The mode only acts
  while you hold **left click** (and the unit keeps drilling ahead as you steer).
  - *Lay floor:* water, lava and pit tiles in a 3x3 area around you become floor, using one block from your
    inventory each (the first block that has a floor version: Dirt gives dirt floor). Free in creative worlds.
  - *Dig:* the 3x3 area around you is dug like a shovel does: placed floors, rugs and rails come up first,
    then the ground, leaving pits (water next to them flows in). The dug-up items come to you. The tiles placed
    objects (chests, tables, plants, ...) stand on are left alone.
  - Rebind under Controls → Drill Pod. Getting out turns the mode off.

Get in and out like a boat (interact). You can only get out next to a walkable tile.

## Requirements

- CoreLib

## Limits

- Alpha: tested in single player only. Multiplayer is untested; the weapon lock is applied on the rider's own client.
- Hovering over lava does not hurt the rider.
- Digging and drilling hit hard and fast: dig mode also breaks walls, farmland and rails in its 3x3, so keep it off
  near your base layout.
- Uninstalling: pick the unit up first, or it may be lost with the mod.

## Development

- `Unity/dump_vanilla.py` dumps the vanilla Speeder Boat prefab to `Unity/vanilla_boat.json`.
- `Unity/make_art.py` draws the inventory icon; `Unity/build_bundle.py` builds `Bundles/` with the Mod SDK and
  runs `Unity/verify_bundle.py`.
- `Unity/extract_skin.py` exports the vanilla boat sheets and reference icons; `Unity/make_skin.py` draws the
  in-world sheets in the vanilla layout and `Unity/make_preview.py` writes comparison previews.
- How it works: the unit is a boat to the game. `HoverColliderSystem` gives a rider a collider that ignores the
  water, shoreline and low (pit) layers; `StuckGuardSystem` keeps the game's stuck rescue from pulling the rider
  back; `UnitKeepSystem` stops the not-on-a-valid-tile check from turning the unit back into an item and keeps a
  ridden unit's health full; `DrillMineSystem` damages the wall tiles ahead of and beside a steering rider; `SendInputPatch`
  drops attack input unless a mining tool is held and sends the ground mode (input bits 29 lay / 30 dig);
  `LayGroundSystem` floors and `DigGroundSystem` digs the cells around the rider on the server.
