# Ender Stash

Core Keeper mod: a personal 40-slot stash per character, like Minecraft's ender chest.

Open any chest and click the new purple chest button under Quick Stack / Sort to switch the
window to your stash; click it again to go back to the chest. The stash is the same from every
chest in every world, belongs to your character (each player in multiplayer has their own), and
is never dropped on death. No new items, no settings.

While the stash is showing: Sort sorts the stash, the quick stack key moves inventory items the
stash already holds into it (hotbar excluded), and shift-click moves items between your inventory
and the stash.

## How it works (for modders)

- `StashLayout`: 40 slots appended to the player prefab's `ContainedObjectsBuffer`
  (`API.Authoring.OnObjectTypeAdded`, subscribed on the first `Update` so they always come after
  Loadout Fallback / Five Loadouts / vanity slots). The character save stores the whole buffer.
  Death drops only slots 10..maxSize of `InventoryBuffer[0]`, so the stash is safe.
- `StashGrowSystem` (server) grows older characters' buffers to include the stash.
- `EnderStashUI` clones the chest window's Sort button and, on click, swaps
  `player.activeInventoryHandler` for `new InventoryHandler(player, world, start, 10, 40)`.
- Patches: `Chest.OnPlayerLeftChest` (still closes when you walk away), `InventoryHandler.Sort`
  (sorts the stash via swaps), `Create.QuickStack` (quick stack into the stash),
  `InventoryUI.UpdateContainerSize` (force a size refresh on toggle).

Removing the mod drops the stash slots from future saves (items in it are lost).
