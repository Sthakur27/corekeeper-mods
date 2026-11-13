# Ottoline's expanded decor catalog

Expanded from 24 to 423 unique IDs in 29 consecutive theme groups. User requested broad categories guided by the wiki list, not an exhaustive variant-by-variant match. `DecorStock.Create()` supplies a fixed catalog; no runtime discovery or dynamic filtering. Original 24 goods and prices are retained. Other merchants' catalogs are unchanged.

Coverage: home furniture; lights and portable lanterns; potted plants; terrariums/aquariums; decorative architecture; Caveling, coastal, city, temple, lava, alien, library and industrial props; landscaping; graveyards; Easter, Halloween, Christmas, Valentine and Lunar New Year sets; themed storage; rare display pieces; figurines. Theme grouping is stock ordering rather than tab/header UI. Some portable lanterns were part of the original shop and remain.

## Data and validation

Read the installed game's `defaultlocalgroup_assets_all.bundle` with UnityPy and ObjectID/ObjectType enums from Pug.Base.dll metadata. Checked new decorative IDs against non-scene base prefabs with icons and PlaceableObjectAuthoring. Rejected crafting/summoning/indestructible authoring for this selection. Native merchant stock supports object IDs but no explicit cosmetic variation, so the catalog uses base variants. Existing Clay/Caveling pots retain their native breakable-pot behavior. No custom art, prefabs, network types or source generators added.

Verified 423 unique IDs, no lost original stock, prices positive/multiples of five and all enum names compile. New prices are at least native sell value times five; rarity premiums are 1,000/2,500/5,000 for figurines, with 1,500 for rare centerpieces. The existing price multiplier and global buy/sell model apply. Native values and exact selections are recorded in `decor-stock.csv`.

Standalone and the full currently installed Overhaul source compile with zero errors. Installed only Catalog.cs, DecorStock.cs and the new manifest entry. Normalized diff against installed backup confirms only Ottoline's stock assignment changed in Catalog.cs. Backups and extracted inspection data are under `build/bossmerchants/decor-expand/`; no game launch or save edits performed.

## Performance and multiplayer boundaries

Uses the existing 8x3 scrolling window and stock migration. 423 items require 424 allocated inventory slots; only 24 UI slots are visible at once. No new update systems, scans, RPCs, jobs, prefab types or per-frame allocations. Existing periodic merchant stock comparisons, native restock traversal, scroll bounds checks and replicated inventory data scale with the larger catalog: this is not zero additional work. Idle checks remain read-only and stock/buffer writes happen only on mismatches/growth. Feature-off behavior is unchanged.

No runtime benchmark or host/remote-client purchase test was performed. Before publication, check scroll-to-last-item, purchases in each section, sold-out/restock behavior, save/reload with the larger buffer, remote-client ordering and performance with the shop open. Do not reuse the earlier spawning/auto-run fixtures. User will restart and test manually.
