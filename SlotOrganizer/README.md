# World & Character Manager

Move your worlds and characters into the order you want, make a copy before trying something, or change a character's name and appearance from the main menu.

## Using the menu buttons

Open **Load World** or the **character list**. Filled slots have extra buttons beside the trash can:

- **▲ / ▼** move the save one slot up or down. If that slot is occupied, the two saves swap places; if it is empty, your save moves into the gap.
- **Copy** (two sheets) puts a copy in the first empty slot of the same list and adds " copy" to its name. Long names are shortened to fit.
- **Edit** (**...**, characters only) opens the character creation screen with your character's name and appearance filled in. **Done** saves the changes and returns to the list without starting a game. **Back** leaves the character unchanged. This edits name and appearance, not the character's starting role.

To move a save with the keyboard, select or hover over its slot and press **Ctrl+Up** or **Page Up**, or **Ctrl+Down** or **Page Down**. The first slot has no up button and the last has no down button. Normal and creative characters stay in their own lists.

To rename a world, use the game's existing **...** world settings button.

## Requirements

For **Core Keeper 1.3.0**. No libraries or other mods are required, and there are no settings to configure. The buttons are available when the mod is enabled.

World & Character Manager works only in the main-menu save lists. It manages saves on your own PC; players joining your world do not need it. It is a separate download and is **not included in Sid's Overhaul**.

It requires **Script (Elevated Access)** because it needs access to the game's save-file functions, which the normal mod sandbox blocks.

## Copies, saves and backups

Moving a world or character keeps its identity, progress and maps. Copying needs an empty slot: there are 30 world slots, 30 normal character slots and 30 creative character slots.

A copied character keeps its inventory and maps but gets a new identity. Worlds treat it as a new player, starting at the world's spawn point. A copied world normally gets a new identity too, with fresh map reveal for each character. If the mod cannot update a world's identity in its save file, it keeps the original identity and writes a warning to the game log; in that case, do not assume the copy has separate map tracking.

Moving, copying and editing write to your save files. **Back up your save folder before using these tools on saves you want to keep.** The game also keeps its own `.pugbackup` files and `backups` folder. A copy in another slot is useful for trying things, but is not a replacement for a separate backup.

World & Character Manager does not provide cloud recovery or resolve cloud-sync conflicts. Keep a separate backup if you use cloud saves. Uninstalling leaves your saves in their current order and keeps any copies or edits you made.

[Developer notes](https://github.com/Sthakur27/corekeeper-mods/blob/main/SlotOrganizer/DEVELOPER.md)
