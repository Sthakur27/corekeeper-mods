# Solarite Drill development


- `python -I SolariteDrill/Unity/make_art.py`: regenerate the sprites (and `Art/preview.png`) from the vanilla Tin/Scarlet drill sheets and Solarite palette exported by `extract_reference.py`.
- `python -I SolariteDrill/Unity/dump_vanilla.py`: re-dump the vanilla Scarlet Drill prefabs into `vanilla_drill.json` after a game update.
- `python SolariteDrill/Unity/build_bundle.py`: build the Windows/Linux bundles with Unity 6000.0.59f2 and the official Mod SDK, then run `verify_bundle.py`.
- `python SolariteDrill/Unity/compile_check.py`: compile the runtime scripts against the local game assemblies.

How it is put together: four entity prefabs (`SolariteDrill_<Direction>Entity`, variations 0-3) copy the vanilla Scarlet Drill prefabs and use the vanilla drill graphics. Two `SpriteAssetSkin` data blocks target the vanilla drill sprite assets the same way the Tin Drill's skins do, and `DrillReskinPatch` applies them for this object. Every networked prefab carries the `SolariteDrill_` prefix so its ghost type never collides with a vanilla prefab's.
