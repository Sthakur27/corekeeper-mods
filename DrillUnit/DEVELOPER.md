# Drill Pod development notes


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
