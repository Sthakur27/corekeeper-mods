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
| Copper Storage Unit | 30 copper bars | 40 | Storage Workbench |
| Tin Upgrade Gem | 30 tin bars | 80 | Storage Workbench |
| Iron Upgrade Gem | 30 iron bars | 120 | Storage Workbench |
| Scarlet Upgrade Gem | 30 scarlet bars | 180 | Storage Workbench |
| Octarine Upgrade Gem | 30 octarine bars | 240 | Storage Workbench |
| Galaxite Upgrade Gem | 30 galaxite bars | 320 | Storage Workbench |
| Solarite Upgrade Gem | 30 solarite bars | 400 | Storage Workbench |
| Pandorium Upgrade Gem | 30 pandorium bars | 500 | Storage Workbench |

Follow the table in order. Only copper units are crafted directly. A wrong-tier or locked gem is not consumed. An upgraded unit can be mined and placed again at its existing tier.

All nine recipes are visible together at the Storage Workbench. Higher-tier gems still require their matching bars and the preceding unit tier. Old prototype recipes are removed from metal workbenches.

## Terminal controls

The terminal reuses the native chest panel and the real player inventory. Left-click picks up or places a stack; right-click and modifier interactions use the game cursor. Quick transfer routes bag items into the connected network. Search filters occupied slots by name; the mouse wheel, scrollbar and arrow buttons navigate storage. Empty slots remain in physical order when search is clear. Terminal crafting is not exposed.

## Removing units

Mining a unit moves what fits into units still connected to the terminal, then vanilla drops leftovers and the unit itself. An isolated unit drops all its contents. Breaking a bridge leaves the disconnected units and their contents intact. Reconnect them to restore terminal access. Breaking the terminal never removes stored items.

## Install and verification

Requires CoreLib on host and clients. Run install.ps1 and restart Core Keeper. This is local test build 0.5.0, not a published release. Offline compilation, planner tests and bundle inspection are automated. The previous UI passed basic transfer/search/persistence tests. The native UI rewrite is undergoing separate live validation in Test; see Tests/NATIVE-UI-2026-10-14.md. Use a test world first.

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

