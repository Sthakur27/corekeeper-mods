# Validation — 2026-10-14

## Release candidate

Magic Mirror 1.0.2 and Sid's Overhaul 1.3.7 both compile with zero errors against the installed game assemblies. The isolated release is based on published Overhaul 1.3.6, retaining Golden Chance 2.2.0, and excludes unfinished Brute Armor, Connected Storage, and diagnostic tooling. All four bundle checks and 27 recipe assertions pass. Both distribution archives pass manifest coverage and ZIP integrity checks. These checks do not replace the in-world checks below.

## 1.0.2 Omoroth drop

Added a server-only system with one independent 20% roll per dead `ObjectID.OctopusBoss`, spawning one mirror at the boss position. Both successes and misses enter the handled-entity set before subsequent ticks, so the death animation cannot reroll misses or duplicate successful drops. Uses the same enabled destruction flag/health guard and drop API as the existing Boss Bonus Loot system, without a dependency on that mod. Standalone and integrated Overhaul scripts compile against the game assemblies. In-world boss-drop behavior has not yet been verified; the running game needs a restart to load the update.

## 1.0.1 correction

The user reported a recipe-icon flicker roughly every seven seconds. The running 1.0.0 log showed repeated recipe re-registration. Inspection of `AvailableRecipesFromContentBundlesSystem.UpdateAvailableRecipesJob` confirmed that vanilla restores live slots from `Crafting/unfilteredRecipes`, overwriting the old buffer-only insertion.

The fix inserts the recipe in `InventoryConverter.SetupCanCraftBuffer` before vanilla caches recipes and prerequisites. The production authoring hook passes 27 regression assertions, including 20 simulated master-list refreshes, repeated conversion, higher-tier inheritance, preservation of gated recipes, and occupied slots. Runtime scripts compile against the game assemblies. The actual running game must be restarted to load 1.0.1; disappearance of the reported flicker has not yet been confirmed in-world.

## Original 1.0.0 checks

- Standalone runtime scripts: offline compiler passed with zero errors against the installed game assemblies.
- Integrated Overhaul runtime scripts: offline compiler passed with zero errors, using the existing CoreLib configuration source/dependency stubs for the compile check.
- Windows and Linux standalone bundles, and Windows and Linux combined Overhaul item bundles: `check_assets.py` passed. Verified recipe cost, cast type/time, item identity, non-stackability, exact visible icon pixels, 16x16 sprite sizing, localization, prefab references, unique data-block addresses, manifest coverage, and preservation of Ender Chest assets.
- Installed standalone files match the checked source and bundles byte-for-byte.
- Game startup: `Successfully compiled MagicMirror safetyCheck=True`; Windows bundle staged; both Magic Mirror data blocks loaded; item mapped as `magicmirror_mirror`; Solarite Workbench recipe registration logged.
- Distribution zip integrity checked. Overhaul integration is staged, not installed over the user's existing Overhaul.

The game was launched and left running. Crafting, teleporting, cancellation, save/reload, and remote multiplayer behavior have **not** yet been manually verified in a world; use `MANUAL.md` for those checks. The combined Overhaul bundle was inspected offline, not separately game-loaded.
