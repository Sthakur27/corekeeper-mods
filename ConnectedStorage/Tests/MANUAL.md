# In-game acceptance checks (pending)

Use a new test world and spare character. Install CoreLib and Connected Storage on both host and joining client for multiplayer checks.

- Loader: `Successfully compiled ConnectedStorage safetyCheck=True`, bundles staged, no Harmony or ECS exceptions.
- Recipes: Wooden Workbench > Base shows Storage Workbench for 20 wood, preserving the five vanilla Base recipes. Place it and open: all nine mod recipes are visible together in native crafting windows, with localized Crafting titles and correct costs. Metal workbenches no longer show mod recipes. Check both existing and newly placed benches, and reload.
- Station: mine/re-place Storage Workbench; connect it beside the terminal and check its recipes appear in the terminal Crafting tab.
- Connectivity: adjacent chests join; diagonals and gaps do not. Workbench footprint touching any node unlocks recipes. Disconnecting removes recipes.
- Terminal: open, search by localized name, sort name/count, withdraw stack/one, deposit stack, quick stack, deposit bag, close/Escape/distance. Mouse clicks must not swing tools; search must not trigger gameplay keys.
- Full inventory: withdrawal leaves overflow in the original chest; crafting output stays available for collection; no item disappears.
- Capacity: each chest exposes its configured slot count. Mixed tiers sum correctly.
- Removal: fill a chest, add spare capacity, mine source; all items move and the chest drops once. Verify partial stack merges, partial capacity with overflow drops, full destinations dropping all contents, isolated chest drops, occupied bridge removal leaving disconnected inventories intact (including an empty bridge), multiple players mining simultaneously and deposits during evacuation.
- Special items: named pets, dyed/damaged equipment, food variants and locked slots retain their attributes. Ender Stash/equipment slots are never used as a withdrawal destination.
- Persistence: save/quit/reload with connected and disconnected chests; break and replace terminal; verify contents and capacity.
- Multiplayer: two users deposit/withdraw/craft same item concurrently; reject stale item selection; no duplicates or wrong-player output. Guest restrictions and CoreLib security remain respected.
- Damage: explosions use the same transfer-then-drop path without loss or duplication.

- Upgrades: select Tin Gem in hotbar and interact with a copper unit; consume exactly one gem, 20 to 40 slots, all item data and positions retained. Repeat all seven transitions to 160 slots. Reject skipped tiers, downgrades, wrong items, locked gems, guests, distant requests and stale duplicate requests without consumption.
- Sealed units: interacting without the correct gem never opens a chest inventory; terminal still opens shared inventory.
- Replacement safety: upgrade an occupied unit while another client uses the terminal; save/reload, mine and replace the upgraded unit, confirm retained tier and correct item totals. Test pending transfers and broken units.

- Tier selector regression: on Solarite and other higher-tier workbenches, select wooden tier and verify Storage Workbench in Base. Check every adjacent tier page for intact recipes, existing and newly placed benches, then save/reload.

- Unlock refresh regression: on an existing Solarite bench select wood, wait at least ten seconds, close/reopen and switch tiers repeatedly. Base must keep Storage Workbench, while Copper keeps its pickaxe. Check again after world reload and on a newly placed higher-tier bench.

## Terminal input and appearance (0.4.0)

- Open terminal: native game pointer, vanilla slot frames and pixel text, no IMGUI overlay. Test windowed and fullscreen.
- Click search and type; switch tabs; sort; scroll inventory. Verify pointer targets match controls at different resolutions/UI scales.
- Deposit a known stack, withdraw it, Shift-click one, quick-stack, and deposit bag. Confirm counts and no world tool actions while open.
- Close with button and Escape; reopen, then open the pause menu. Pointer/game input must return to normal after closing.
- Tools show icons without durability masquerading as stack counts; stackable items retain counts.

- Search focus: type spaces, Backspace, arrows and Enter; click a slot after searching. Escape must release the text field and restore gameplay.
- Scroll each grid using wheel and arrow buttons; verify final rows, empty search results, and search/sort after scrolling.
- Open with an item on the mouse; verify it returns to the bag. Close/reopen after world change; no duplicate controls or stale slots.
- Item Browser coexistence: close the terminal, open Item Browser with Z, then return to the terminal. Both retain their own input and visuals.

## 0.4.1 regressions

- Open terminal with E: status text, empty and populated slots, search and tooltips must render without ShouldForceLatinFont exceptions. Close/reopen repeatedly.
- Check placed copper units before and after save/reload and walking out of render range. They must show the copper crystal/pedestal; inspect `[ConnectedStorage] Unit art` log lines if not. Upgrade once and verify the new tier art.

## Artwork 0.4.2

- Compare terminal, crafting workbench and each storage tier with Unity/Art/preview.png. Rotate placed objects to inspect front, side and rear; verify they meet the floor and remain within the tile.
- Inspect in bright and dark areas: crystal/screen emissions should not light the entire housing. Check inventory icons and gem silhouettes at normal game zoom.
- Reload existing units, then upgrade one: the new tier should keep its distinct crystal shape/material palette.

## UI 0.4.3

- Open over a bright floor: panel, buttons, search and slots must have opaque backgrounds, with legible text.
- Inventory uses ten square slots per row; the hotbar occupies the first row. Check wheel/arrows expose all50 bag slots without duplicates or omission.
- Change resolution and UI scale: icons retain proportions and the footer/Close button stay within the viewport.

- Right click: 24 bars -> half modifier gives 12; 25 -> 13; one item -> 1. Compare modifier 1 (ten) and ordinary (one) with a native chest, including depositing from the cursor and full/locked destinations.
- Cursor transfer: compare drop-to-cell visibility with native inventory on host and remote client; rejected/full slots must preserve cursor contents. Close/reopen during a pending transfer and verify no stale updates. Performance follow-up: benchmark three matched UI captures per build; watcher examines one slot per frame for at most one second per action, zero while closed.

- Consolidation: put copper bars 12+24 in one node and another 8 in a second node. Each sort control should produce one 44 stack at the appropriate sorted position. Check total counts before/after. Check 9990+20 gives 9999+11, three partial stacks, full network, locked stacks, different variations/aux data, and nonstackable tools. Repeated ascending/descending sorts must preserve all items and metadata.

- Quick actions (0.7.30): open terminal, left-click the native green quick-stack button with matching and unmatched bag stacks. Only matching types should move; right-click deposits both. Hotbar/locked items stay put. Test partial/full capacity, multiple nodes, auxiliary-data items, host and remote client; totals must be conserved with no chat spam. Inventory sort still only sorts your bag. Close terminal and verify nearby-chest quick stacking returns to vanilla behavior. Compare idle and repeated bulk-action frame times before/after; no runtime performance claim yet.
