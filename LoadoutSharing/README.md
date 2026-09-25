# Loadout Fallback (Core Keeper mod)

Loadout 1 is your base set. In loadouts 2 and 3, every equipment slot (helmet, chest, pants,
necklace, both rings, off-hand, bag, lantern, pet) uses its own item if it holds one, otherwise
it falls through to loadout 1's item. No settings.

- Inherited items show **dimmed** in the equipment window, with their real durability.
- Drop an item onto a dimmed slot to give that loadout its own item. Take it out again and the
  slot falls back to loadout 1's.
- Clicking a dimmed slot does nothing: the item lives in loadout 1, change it there.
- Stats, player sprite, bag capacity and pet all follow the same rule.

Install: run `..\install.bat` (copies into `CoreKeeper_Data\StreamingAssets\Mods\LoadoutSharing`),
then restart the game. Keep **Loadout Plus** disabled; this mod replaces it and reuses its slots.
Check `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` for
`Successfully compiled LoadoutSharing` and `[LoadoutSharing] Player layout: ...`.

Known limit: right-clicking gear on the hotbar while in loadout 2/3 equips it through the server's own path and will replace loadout 1's item if that slot is currently inherited. Use drag-and-drop or shift-click from the inventory to give the loadout its own item.
