# Wren's critter catalog — 2026-11-13

Added 24 entries to Wren's existing worm listing: all 25 distinct native bug-net critter ObjectIDs. Source and installed Catalog.cs matched before editing. The design JSON and README stock table have been synchronized.

## Native evidence

Inspected the installed game's `defaultlocalgroup_assets_all.bundle`, extracting all objectType 801 prefabs and their authoring components. All 25 distinct IDs have CritterAuthoring. Persistent variation 1 prefabs share the same IDs and are not separate shop items. ObjectID names/values were checked against Pug.Base.dll metadata.

`PlayerAttackSystem.TryCatchAnyCritters` tests CritterCD and uses the native inventory pickup request. `CritterConverter` adds CritterCD from CritterAuthoring. Fireflies lack CritterCatcherCatchableAuthoring (automated catcher eligibility), but have CritterAuthoring and are net-catchable; filtering by automated-catcher eligibility would incorrectly omit all five colors. No native systems were patched.

Local evidence and backup: `build/bossmerchants/critters/all-items.json`, `native.json`, `prices.json`, `Catalog-before.cs`, and `installed-Catalog-before.cs`. Decompiled source evidence: `build/perf-fixes/cs-skin/decomp/Pug.Other/PlayerAttackSystem.cs` and `build/perf-fixes/storage-cost/decomp/Pug.ECS.Conversion/CritterConverter.cs`.

## Behavior and validation

- Wren has 47 distinct wares, fitting 48 inventory slots; existing 8-column scrolling handles six rows.
- Existing MerchantStockSystem runs on the server every three seconds, migrates saved merchants, and resets restock when entries are added. Existing prefab authoring sizes client/server inventories. No new timer, scan, allocation site, job synchronization or runtime mechanism added. Existing steady-state list comparisons cover 24 more entries per Wren; no performance improvement or benchmark result claimed.
- Offline Roslyn compile passed with zero errors and one existing unused stub-event warning.
- Checked exact set equality against the 25 native critter IDs, no duplicate Wren entries, catalog/design JSON parity, positive stock and prices divisible by five.
- Prices are deliberate balance choices (10–150 coins, 3–20 stock), not inferred rarity values. All species are available at Wren's existing Azeos unlock. Existing global price/resale behavior applies to these new entries too.
- Catalog-only update: no asset-bundle rebuild is needed, and the approved NPC/item art remains unchanged. The loader compiles installed loose C# on the next launch.

In-game purchase, release/recapture, restock, scrolling, and multiplayer checks remain pending. No publication or upload authorized in this step.
