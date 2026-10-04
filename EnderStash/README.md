# Ender Stash

Core Keeper mod: the **Ender Chest**, a personal 40-slot stash per character, like Minecraft's
ender chest.

Buy an Ender Chest from the **Fishing Merchant** (9999 ancient coins, 1 per restock, always first
on his list) and place it anywhere. Opening it shows your stash, not an inventory of its own: every
Ender Chest in every world opens the same 40 slots. The stash belongs to your character (each player
in multiplayer sees their own) and is never dropped on death. Regular chests are untouched. No settings.

While the stash is open: Sort sorts the stash, the quick stack key moves inventory items the stash
already holds into it (hotbar excluded), and shift-click moves items between your inventory and the
stash. Walking away or breaking the chest closes the window; breaking it never touches the stash.

Upgrading from 1.x (the purple button on every chest): your stash contents are kept, the button is
gone; buy an Ender Chest to get at them.

## How it works (for modders)

- **The object** is a real modded object (`ObjectAuthoring.objectName = "EnderStash_EnderChest"`,
  ObjectID assigned by the game above 32767; saves store it by name). Its prefabs, sprites and name
  text live in `Bundles/EnderStash_*.assetbundle`, built with the official Core Keeper Mod SDK by
  `Unity/build_bundle.py` (see below). The logic prefab mirrors the vanilla `ChestEntity` minus the
  inventory; the graphics prefab uses the vanilla `Chest` component, so the bundle has no script
  references of its own and works inside Sid's Overhaul too. Price: sellValue 2000 x 5 x 0.9999 = 9999.
- `StashLayout`: 40 slots appended to the player prefab's `ContainedObjectsBuffer`
  (`API.Authoring.OnObjectTypeAdded`, subscribed on the first `Update` so they always come after
  Loadout Fallback / Five Loadouts / vanity slots). The character save stores the whole buffer.
  Death drops only slots 10..maxSize of `InventoryBuffer[0]`, so the stash is safe.
- `StashGrowSystem` (server) grows older characters' buffers to include the stash.
- `EnderChestMerchantSystem` (server) puts the Ender Chest at index 0 of the Fishing Merchant's
  `MerchantItemInfoBuffer` (prefab and merchants already in saves; forces one restock when added).
- Patches: `Chest.Use` (an Ender Chest opens the chest window on
  `new InventoryHandler(player, world, start, 10, 40)`), `Chest.OnPlayerLeftChest` (closes it),
  `InventoryHandler.Sort` (sorts the stash via swaps), `Create.QuickStack` (quick stack into the
  stash), `InventoryUI.UpdateContainerSize` (force a size refresh).

## Rebuilding the asset bundle

Needs Unity 6000.0.59f2 with Linux Build Support (Mono), and https://github.com/Pugstorm/CoreKeeperModSDK
cloned to `C:\Users\Sid\CoreKeeperModSDK`. Then `python Unity/build_bundle.py` (add `--art` to
regenerate the art from the vanilla black chest). Art: `Unity/Art`, made by `Unity/make_art.py`.

Removing the mod drops the stash slots from future saves (items in it are lost) and removes placed
Ender Chests.
