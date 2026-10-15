# Item Browser adaptation: 0.6.0 test build

The previous UI is checkpointed at `5b458ea`. This build switches the terminal
to a separate exported variant of Item Browser's authored UI prefab and classic
atlas. Item Browser does not need to be installed; its executable catalogue and
spawning code is not in the exported bundles. MIT attribution is included.

Implemented: the authored search field, grid/list templates, scroll window and
scrollbar, layout/sort/direction/filter controls, per-slot item identity, and the
real player inventory beside the browser. Sorts are physical position, name,
quantity. The filter button cycles all, stackable, non-stackable. It does not yet
expose Item Browser's full category filter panel or navigation history.

First-pass transfer controls: click a stored item to withdraw its stack to the
bag; right-click or modified left-click withdraws one. Click a player inventory
stack to deposit it. The established server commands validate proximity,
network membership, slot identity and locks. This is not yet vanilla cursor
pickup/drag/drop behavior. The failed old UI deposit path was not repaired.

Automated checks passed:
- Full mod compile with game references (zero errors).
- Browser model executable tests: sorting, normalized search, scroll bounds,
  duplicate stacks retaining distinct physical addresses.
- Both platform bundles: required controls, native UI scripts and theme textures
  present; no Item Browser assembly references in MonoScript records.

Not live-verified: loader safety checks, rendering/scale, cursor input, transfer
round trips, responsiveness, save/reload, or compatibility with ExpandedChestUI.
The user now drives testing. No game restart or automated input was performed.

## User test sequence

1. Restart Core Keeper and open the terminal in Test.
2. Check the panel, font, slots, and inventory alignment against Item Browser.
3. Deposit a disposable stack. Search for it; clear search with right-click.
4. Try grid/list, each sort, both directions and each filter.
5. Withdraw the stack, then one item. Check total quantities are conserved.
6. With enough different stacks, scroll through the list and use the scrollbar.
7. Close the terminal and check that ordinary inventory/chests still align and work.

Report screenshots and the first failing step. Do not treat compilation as a
successful in-game acceptance test.

## 0.6.1 rendering correction
User reported the browser flashing and disappearing while the inventory stayed offset. The controller put the camera child at local z=0 each LateUpdate. It now uses the native inventory depth and applies layout before activation. Horizontal and vertical offsets follow the gameplay UI scale. Native close hooks respect unsuccessful close requests; open/close diagnostics identify any remaining lifecycle issue. Offline compile passed; live rendering awaits user restart/test.


## 0.6.2 spacing and empty state
User confirmed the panel remains visible, with an opening flash. Empty storage now explains how to deposit; the heading is shorter and lower, and browser/inventory spacing leaves more room for native inventory controls. Layout runs after ordinary UI updates to reduce ordering conflicts. Offline compile passed. The opening flash is not yet verified resolved; user drives live testing.


## 0.7.1-dev cursor grid

Storage now includes physical empty slots as insertion targets (search and category filters can hide them). Stack amount labels clone native inventory count text because imported browser slots had null amountNumber references. Left-click a bag item to use the native cursor, then click a storage slot to place/merge/swap; click an occupied storage slot with an empty cursor to pick up. Right-click a storage slot moves one item; Shift-click retains whole-stack quick transfer. Closing returns held items through the native mouse inventory handler.

Server cursor actions validate terminal proximity, membership, live unit state, slot bounds, both displayed item identities, locks and guest permissions, then queue native Swap/MoveAmount operations. No direct item-buffer writes. Runtime cursor interaction and count rendering await user verification. Offline compile, browser-model address/empty-slot/sort/search tests and diagnostic parser tests passed. Snapshot node arrays are now serialized individually because Unity omitted the mod-defined array in the observed game capture.

Test with disposable stacks: bag -> cursor -> empty storage; storage -> cursor -> bag; same-type merge; unlike-type swap; right-click one item; close while holding; verify item totals after each action. Check counts at 1, 2 and a large stack size. Clear search/filter to see all empty insertion targets. These are click-pickup/click-place controls; continuous held-button dragging has not been verified.

## 0.7.2-dev duplicate hotbar opening frame

Native ItemSlotsBarUI.Update hides the gameplay bar when any inventory is open. Terminal Use can run after that Update, while InventoryUI.ShowContainerUI immediately enables its own first (hotbar) row. Hide the gameplay bar synchronously before opening InventoryUI, and keep it hidden during terminal layout. Stop repositioning the separate gameplay bar; the inventory already includes the visible hotbar. Native Update restores gameplay visibility on close. Legacy ui-dev HotbarY is now unused.

Offline compile passed. Live check pending: open/close five times, check no duplicate opening row, then verify gameplay hotbar returns on closing and ordinary chests still behave normally.

## 0.7.4-dev counts, held-item layers, quick move

Count labels explicitly use the browser icon sorting layer at order 60, outside sprite masking, and force-render the stack count. Browser bundle sprites occupy orders 41–48. While open, the cursor item underlay/icon/overlay and its count render above the browser; original render settings are restored on close. Native inventory Shift-click is intercepted at TryToSendItemToOtherInventoryOrEquip (UIMouse bypasses OnLeftClicked for QUICK_MOVE_ITEMS). Browser slots read the same configured QUICK_MOVE_ITEMS action instead of assuming mod1 means Shift (it actually means PICK_UP_10).

Manifest-driven compile, model/capture tests and both platform bundle checks passed; installed hashes match. Live acceptance pending: store 46 dirt and check 46 appears; pick it up and move across the panel, checking both held sprite/count remain visible; Shift-left-click bag -> storage and storage -> bag; close and check normal cursor/chest rendering. No preview rerender: these fixes concern runtime PugText/native input, which substitute-font fixtures cannot validate.

## 0.7.5-dev categories and text

Filter button now opens a category chooser inside the browser (All, Weapons, Tools, Armor, Accessories, Food, Building, Materials, Pets, Valuables, Other). Categories use ObjectInfo.objectType, so modded items with those types participate. One category at a time, combined with search/sorting; All restores empty insertion targets. This is a broad type chooser, not Item Browser's full multi-include/exclude filter system.

Search hint is literal, nonlocalized `Search`. Node upgrade guidance retains only the upgrade instructions, removing `Access items at the terminal.` Existing compile/model/capture/bundle checks passed and install hashes matched. Category chooser rendering/input and wording await live restart verification.
