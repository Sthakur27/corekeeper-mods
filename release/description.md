**Loadout 1 is your base set. Loadouts 2 and 3 only need the pieces that differ.**

In loadouts 2 and 3, every equipment slot uses its own item if it has one, otherwise it falls through to whatever loadout 1 is wearing. That covers helmet, chest, pants, necklace, both rings, off-hand, bag, lantern and pet.

**How it looks in game**
- An inherited item is drawn dimmed in the equipment window, with its real durability and tooltip.
- Drag or shift-click an item onto a dimmed slot to give that loadout its own. Take it out again and the slot falls back to loadout 1's.
- Clicking a dimmed slot does nothing. The item lives in loadout 1, so change it there.
- Stats, your character sprite, bag capacity and pet all follow the same rule.

**Why**
Vanilla loadouts are fully separate, so a second loadout that only swaps a weapon still needs a full second set of armor. With this mod, you set your armor once in loadout 1 and only put the weapon, off-hand or ring that changes into loadouts 2 and 3.

**Notes**
- No settings. Works for all three loadouts out of the box.
- Vanilla shares the bag, lantern and pet between all loadouts. This mod gives each loadout its own bag, lantern and pet slot (with fallback), using the same slot layout as Loadout Plus, so a save that used Loadout Plus carries over. Do not run both mods at once.
- Known limit: right-clicking gear on the hotbar equips it through the game's own server path and will replace loadout 1's item if that slot is currently inherited. Use drag-and-drop or shift-click from the inventory instead.
- Multiplayer: install on the host and every client.
- Source ships in the mod (plain C#). Feel free to learn from it or fork it.
