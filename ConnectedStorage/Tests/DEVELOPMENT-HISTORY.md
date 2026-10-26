# Connected Storage

A separate Core Keeper mod with physically connected storage units and one searchable terminal. Items stay in real world inventories. Ender Stash is independent.

## Build and upgrade

1. At the Wooden Workbench (or the wooden tier selected on a higher-tier workbench), open **Base** and craft a **Storage Workbench** for 20 wood. Place it and open it to craft the Storage Terminal (10 copper bars + 20 wood), Copper Storage Unit (30 copper bars), and all seven gems.
2. Place units edge-to-edge with the terminal. Diagonals and gaps do not connect. There is no remote access.
3. Open the terminal to search, scroll and move items using native inventory controls. Units cannot be opened directly.
4. Craft the next tier gem, select it in your hotbar, and interact with the placed unit. Upgrades consume one gem and preserve contents, slot positions and auxiliary data. No skipping tiers or downgrades.
5. Terminal crafting is deferred; this build focuses on storage.

| Craftable item | Cost | Resulting capacity | Craft at |
|---|---|---:|---|
| Copper Storage Unit | 30 copper bars | 20 | Storage Workbench |
| Tin Upgrade Gem | 30 tin bars | 40 | Storage Workbench |
| Iron Upgrade Gem | 30 iron bars | 60 | Storage Workbench |
| Scarlet Upgrade Gem | 30 scarlet bars | 80 | Storage Workbench |
| Octarine Upgrade Gem | 30 octarine bars | 100 | Storage Workbench |
| Galaxite Upgrade Gem | 30 galaxite bars | 120 | Storage Workbench |
| Solarite Upgrade Gem | 30 solarite bars | 140 | Storage Workbench |
| Pandorium Upgrade Gem | 30 pandorium bars | 160 | Storage Workbench |

Follow the table in order. Only copper units are crafted directly. A wrong-tier or locked gem is not consumed. An upgraded unit can be mined and placed again at its existing tier.

All nine recipes are visible together at the Storage Workbench. Higher-tier gems still require their matching bars and the preceding unit tier. Old prototype recipes are removed from metal workbenches.

## Terminal controls

The terminal now uses an adapted Item Browser layout: search, scrolling, grid/list views, sorting direction, and basic stackable/non-stackable filters. Its real player inventory appears alongside it. For this test build, click a stored stack to withdraw it, right-click to withdraw one, and click a player inventory stack to deposit it. Vanilla cursor dragging is not implemented in this new interface. Crafting remains deferred.
## Removing units

Mining a unit moves what fits into units still connected to the terminal, then vanilla drops leftovers and the unit itself. An isolated unit drops all its contents. Breaking a bridge leaves the disconnected units and their contents intact. Reconnect them to restore terminal access. Breaking the terminal never removes stored items.

## Install and verification

Requires CoreLib on host and clients. Run install.ps1 and restart Core Keeper. This is local test build 0.6.0, not a published release. Offline compilation, planner tests and bundle inspection are automated. The previous UI passed basic transfer/search/persistence tests. The native UI rewrite is undergoing separate live validation in Test; see Tests/BROWSER-UI-2026-10-14.md. Use a test world first.

The terminal uses mouse/keyboard and English labels. Units and stations must be loaded; networks beyond the client's replication range are unsupported. CoreLib command security must permit connectedstorage for participating players. Empty storage before uninstalling.

## Development

Unity/ contains original pixel art, the official SDK builder and bundle inspection. Tests/ contains planner checks and live acceptance steps. Stable object keys retain the older Chest suffix for compatibility, while player-facing names are Storage Units. Upgrades instantiate the next-tier prefab, swap inventories through vanilla helpers, consume a validated gem and suppress loot from the replaced unit. No custom ghost serializers or Burst patches are used.

Version 0.3.2 adds the workbench recipe before vanilla conversion builds copied tiers and prerequisite lists. This prevents unlock refreshes from restoring the copper pickaxe into the wooden Base slot.

Version 0.3.3 uses the existing localized Crafting heading for both native recipe panels. The nine recipes share one list; vanilla displays up to six recipes per panel.

Version 0.3.4 gives the terminal wood-and-gold panels, framed inventory slots, and an explicit system cursor while open. It restores the prior cursor state on close and no longer displays tool durability as stack counts. This remains a custom mouse/keyboard interface rather than the native inventory screen. Live click and appearance checks are pending.

Version 0.4.0 replaces the IMGUI overlay with a native UIelement hierarchy, vanilla inventory sprites, PugText, and TextInputField. Uses the game cursor and UI click routing; world interaction is blocked separately so UI input stays enabled. Search, sorting, storage/crafting tabs, visible-row scrolling, bag deposits, stack withdrawals, quick-stack, bulk deposit and craft collection retain the existing server commands. Mouse wheel and arrow buttons scroll each grid. Slot hover shows item names. This is our own runtime layout, with no Item Browser source or assets included. Controller navigation and full vanilla item-stat tooltips are not implemented. Live appearance and transfer checks remain necessary.

Version 0.4.1 initializes PugText language/style lists required by native text rendering (fixes the reported ShouldForceLatinFont null exception on terminal opening). Storage-unit graphics now reapply their mod-owned crystal SpriteAsset, clear skins and reset animation/variant when spawned from the graphics pool. The cause of the reported chest appearance is still unconfirmed; Unit art log entries record the previous asset/skin and selected asset for diagnosis. Both bundles contain the expected visible pixels for all 48 storage-unit textures. Live terminal opening and visual checks remain pending.

Version 0.4.2 refreshes all runtime artwork: faceted crystal storage units with tier-specific hardware, distinct upgrade-gem cuts, a shaded terminal housing and detailed timber storage workbench. All placed objects have front/side/back views. Artwork is original pixel-grid code, informed by native game proportions and shading; no extracted game or third-party artwork is shipped. Preview: `Unity/Art/preview.png`; directional/emissive sheet: `Unity/Art/directions.png`.

Version 0.4.3 adds opaque panel/control backings (the vanilla border sprite has a transparent center), square item slots, uniform UI scaling and ten-column inventory rows. Grids show30 slots at a time with wheel/arrow scrolling. Artwork has been redesigned around native top/front perspective: solid storage housings with inset crystals, asymmetric material details, a sloped console terminal and broader timber workbench. In-game appearance and mouse/transfer checks remain pending after this update.

Version 0.4.4 follows an independent art critique/revision loop using native sprites at actual size and4x. Revised storage-unit sockets, seated crystals, rear structures and high-tier silhouettes; terminal rear/side now show casing rather than repeating the front display. Workbench and gems passed the initial comparison. Asset review accepted the final set; in-game rendering remains to be checked.


Version 0.4.5 fixes terminal text clipping by keeping glyphs at native scale, uses wooden UI frames with opaque backings, and fixes node discovery after world reload using replicated object IDs. Hover labels no longer show missing localization errors. See Tests/LIVE-2026-10-14.md for verified live checks and remaining coverage.


## UI attribution

Browser layout, theme atlas, and adapted list/search logic derive from moorowl/ItemBrowser (MIT), revision 9b9df228428f212681873e875409fc33003cb3f7. See ThirdParty/ItemBrowser/LICENSE. The Item Browser mod itself is not modified or required.


Storage tiers have fixed capacities unaffected by Double Chest Inventory. Existing excess slots are retained for withdrawal only, preserving saved items; empty excess slots are hidden. Withdraw legacy overflow before upgrading if it exceeds the new tier.

### Stable terminal slots (0.7.7-dev)
The terminal displays physical storage slots. Deposits stay where placed; pressing Sort sorts the whole connected network by name; reopening the terminal preserves the arrangement. The direction button sorts ascending/descending. Search and categories only filter the view, retaining each slot address. Empty cells beyond capacity are hidden. Existing occupied overflow remains withdrawable and prevents new stacks until space is available.

Nearby native crafting stations can consume materials from storage units using the game’s normal nearby-chest crafting rules. Existing units are reconciled on both client and server; this does not add remote crafting through the terminal.

Interacting with any storage unit opens its connected inventory. Holding an upgrade gem instead attempts the sequential upgrade.


Connected unit order uses world tile coordinates on both client and server, so manual sorting fills the first displayed slots consistently across units and reloads.

Access from a node uses that node as the proximity anchor and includes the full connected component, even without a separate terminal. Manual sort, search, categories and transfers work identically from either entry point.

When a previously viewed network gains a storage node, the next terminal/node view automatically sorts once to fill from the top-left. Reopening an unchanged network does not sort. Network tracking lasts for the current world session; the first visit after loading preserves the saved layout.

Art direction: Runic Kiln — terracotta terminal and sealed ceramic storage vessels, amber rune lighting, and tier-specific metal bands. Runtime pixel maps live in Unity/make_kiln_art.py; previews in ArtConcepts/RunicKiln.

Runic Kiln sprites use straight-on faces and shallow top surfaces. Each storage tier has a complete body, highlight, metal and glow palette; the ivory/green terminal has its own palette.

0.7.20 art trial: Copper Storage Unit and Terminal use the approved copper/ivory prototype designs redrawn at native pixel density (16x16 and 16x20). Other tier textures retain the installed 0.7.18 art while these two are evaluated. Final sprite source: Unity/make_review_art.py. No native game textures are distributed.

0.7.21 art trial replaces the 16px redraws with the actual approved copper-node and ivory-terminal PNGs. import_approved_art.py crops outer padding without resampling or repainting the object. The visual child is scaled to a one-tile canvas width; collider and gameplay footprint are unchanged. All directions use the approved front image during this trial. Emissive masks are empty pending a separate lighting pass. Bundle checks compare source pixels and verify physical scale.

0.7.22: all seven upgrade gems use individual generated crystal designs with distinct tier colours and cuts, dark metal settings, and brass rivets. Original pixels are preserved through crop-and-pad import; costs and upgrade rules are unchanged. Sources and preview: ArtConcepts/UpgradeGems.

0.7.23 art completion: all seven remaining storage tiers and the workbench now use individually generated PNGs, preserved without resampling into runtime textures. Full body palettes distinguish tiers. Sources and prompts: ArtConcepts/StorageFamily; review sheet: preview.png. All rotation views currently share the front sprite. Mesh scaling keeps the original one-tile footprint; bundle checks verify source pixels and scale.

0.7.24: high-resolution art sizing lives on an ArtScale parent. The native direction handler can reset or mirror the Sprite Object child without making the artwork enormous. Bundle validation checks the hierarchy and both direction-reset scales.

0.7.25: the storage workbench presents its nine recipes in one wide native crafting frame with one title. Native recipe handlers and costs are unchanged; other stations retain their usual panels.

0.7.26: shared chest pools may supply vanilla graphics instead of the authored prefab. The spawn handler now creates or reuses ArtScale and sizes from the active asset dimensions, including terminal graphics. It resets the wrapper for subsequent vanilla chest reuse. Spawn logs include source width and applied scale.

0.7.27: storage-unit hover descriptions read capacity directly from StorageCatalog (20 through 160), avoiding outdated localized capacity text.

0.7.28: right-click modifiers match native inventory (one, ten, or half rounded up). After cursor transfers, a one-second bounded watcher reads only the clicked client slot and refreshes on its replicated change instead of waiting for the normal 100 ms cadence. No extra network scans, mutation, Burst changes, or job completion. Watcher replaces the prior pending action and clears on close/timeout. Idle/closed path has no watcher reads. Runtime responsiveness and host/client performance remain unmeasured; no speedup claim.

0.7.29: both sort controls consolidate compatible unlocked stacks across connected units before ordering by name. Stack capacity remains 9999; differing variations or auxiliary-data identities stay separate. Native InventoryUtility.MoveAmount performs merges and Swap orders the result. Consolidation is server-only per sort action (including existing expansion sorts), O(N) dictionary work over eligible slots plus existing O(N log N) ordering; no idle scan, new job completion, or Burst changes. Host/client timing remains unmeasured.

0.7.30: while the terminal is open, the native inventory quick-stack button sends matching bag items into the connected network. Right-click that same button to deposit all eligible bag items; its tooltip explains both actions. Hotbar and locked items are excluded, capacity is enforced, and leftovers stay in your bag. Inventory sort still sorts the bag, and native locking/trash behavior is unchanged. Closing the terminal restores ordinary nearby-chest quick stacking. Managed click-event hooks only; no recurring work added. Bulk actions reuse the existing server transfer path (including its existing per-transfer job completion); runtime timing and host/client checks remain pending.
