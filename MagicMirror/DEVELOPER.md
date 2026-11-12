# Magic Mirror development

Run `install.ps1` from the source checkout for a local install. The mod includes original pixel art, an inventory icon and item text.

- `python MagicMirror/Unity/make_art.py`: regenerate the in-game sprites and a procedural reference image.
- `python MagicMirror/Unity/build_bundle.py`: build with Unity 6000.0.59f2 and the official Core Keeper Mod SDK. Requires EnderStash assets already built in that SDK. `--skip-import` skips reimporting game assemblies.
- `python MagicMirror/Unity/compile_check.py`: compile runtime scripts against local game assemblies.
- `python MagicMirror/Tests/check_recipe.py`: regression checks for master-list persistence, inherited recipes, idempotency, and coexistence with other mods' recipes.
- `python MagicMirror/Tests/check_assets.py`: validate the actual Windows/Linux bundles and their artwork.
- `python release/build_overhaul.py`: include the feature in Sid's Overhaul.

`Bundles/` is standalone. `OverhaulBundles/` combines Magic Mirror and Ender Stash data blocks, because the game registers only one data-block loader per mod GUID. Both builds use vanilla prefab components, with no assembly-name-dependent mod MonoBehaviours.

The cast system runs before vanilla's Burst player-state systems in both predicted worlds. It uses the existing replicated casting state and requests vanilla's teleport state without enqueuing an inventory consumption. No custom components are added to players. The authoring hook inserts the recipe before vanilla caches its unfiltered recipes and prerequisites; the recipe updater also handles already-placed workbenches.

See [the manual checklist](https://github.com/Sthakur27/corekeeper-mods/blob/main/MagicMirror/Tests/MANUAL.md) and [recorded validation](https://github.com/Sthakur27/corekeeper-mods/blob/main/MagicMirror/Tests/VALIDATION.md).

## Earlier changes

### 1.0.3

- Crafting now requires 50 Solarite Bars and 10 Recall Idols. Existing mirrors and the 20% Omoroth drop are unchanged.

### 1.0.2

- Add an independent 20% chance for Omoroth to drop one Magic Mirror. The crafting recipe remains available.

### 1.0.1

- Register the recipe in the workbench's original recipe list so the game's periodic prerequisite refresh cannot erase it and cause flickering.
- Replace the dark purple icon outline with shaded gold edges and lighter blue glass to better match Solarite equipment.
