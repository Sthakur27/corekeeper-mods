# Loadout Fallback (Core Keeper mod)

Gear waterfalls down through your loadouts. In loadouts 2 and 3, every equipment slot (helmet,
chest, pants, necklace, both rings, off-hand, bag, lantern, pet) uses its own item if it holds one,
otherwise the nearest lower loadout's item: loadout 3 falls through to 2, then to 1. No settings.

- Inherited items show **dimmed** in the equipment window, with their real durability.
- Drop an item onto a dimmed slot to give that loadout its own item. Take it out again and the
  slot falls back to the loadout below.
- Clicking a dimmed slot does nothing: the item lives in a lower loadout, change it there.
- Stats, player sprite, bag capacity and pet all follow the same rule.

## Per-loadout vanity

Every loadout also has its own vanity outfit (helmet, chest, pants). Three vanity slots sit to the left
of your armor in the character window, marked with an eye; whatever you put there is what your
character looks like in that loadout. An empty vanity slot shows that loadout's real armor (like
vanilla), and clicking an empty vanity slot toggles "hide this piece", per loadout. The vanity
furniture edits the current loadout's outfit too. Other players see your active loadout's outfit.
(Idea for the in-window slots from the mod.io mod "Vanity Slots"; this is a separate implementation.
If Vanity Slots is installed and adds its slots first, this mod uses those instead of adding its own.)

Install: run `..\install.bat` (copies into `CoreKeeper_Data\StreamingAssets\Mods\LoadoutSharing`),
then restart the game. Keep **Loadout Plus** disabled; this mod replaces it and reuses its slots.
Check `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` for
`Successfully compiled LoadoutSharing` and `[LoadoutSharing] Player layout: ...`.

Known limit: right-clicking gear on the hotbar while in loadout 2/3 equips it through the server's own path and will replace the lower loadout's item if that slot is currently inherited. Use drag-and-drop or shift-click from the inventory to give the loadout its own item.

Works with **Five Loadouts**: loadouts 4 and 5 waterfall the same way (5 -> 4 -> 3 -> 2 -> 1).
