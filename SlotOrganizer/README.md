# Slot Manager

Reorder, copy and edit your worlds and characters on the main menu. Move a world from slot 4 up to
slot 3, swap two characters, close a gap, duplicate a world before a risky build, or rename a
character, without touching the save folder.

## How to use

On **Load World** and on the **character list**, every filled slot gets buttons to the right of the
trash can:

- **▲** moves that world/character up one slot
- **▼** moves it down one slot
- **Copy** (two sheets) copies it into the first empty slot, named "<name> copy"
- **Edit** (**...**, characters only) opens the character creation screen for that character, with its
  name and looks filled in. Change the name or appearance and press **Done** to save, or **Back** to
  leave it unchanged; no game starts. Worlds are renamed in the game's own world settings (their own
  **...** button).

If the neighbouring slot has a world or character, the two swap places; if it is empty, the slot just
moves there. The top slot has no ▲ and the bottom slot has no ▼. Normal and creative characters are
each reordered within their own list.

Keyboard: select (or hover) a slot and press **Ctrl+Up / Page Up** or **Ctrl+Down / Page Down**.

## Requirements and multiplayer

- No other mods needed. No settings.
- Only you need it: it reorders the saves on your own PC and does nothing in game, so players joining you
  don't need it (1.0.1 fixes 1.0.0, which made the game ask joining players for it).
- Not part of Sid's Overhaul; subscribe to it on its own (works fine next to the Overhaul).
- Uses **elevated script access** ("Script (Elevated Access)" on the Workshop): the game's mod sandbox
  blocks the save-file functions this mod needs, so it runs without the sandbox.

## Limits

- A copied world or character gets its own new ID, so it is a separate save from then on: playing the
  copy doesn't change the original. A copied world starts with a fresh map reveal for every character;
  a copied character keeps its maps and inventory, and appears in each world as a new player (it
  starts at the world's spawn point).
- Copying needs an empty slot in the same list (30 worlds, 30 normal and 30 creative characters).

- Characters find their worlds by an ID inside the save, not by slot number, so moving either one keeps
  every character's progress and map in each world.
- A move rewrites the files of the two slots involved. The game keeps its own backups
  (`.pugbackup` files and the `backups` folder), but if you are nervous, copy your save folder first.
- Uninstalling changes nothing: your saves stay in the order you left them.

## How it works

A slot number is only the save file name, so the mod swaps the slot's files through the game's own
file system and reloads the save list: for a world `worlds`, `worldinfos`, `worldgenparams` and
`servermaps`; for a character `saves` and `maps/<slot>/`. Copies duplicate the same files and then
give the copy a new GUID (worlds: the world GUID inside the Brotli-compressed world save and its
`worldinfo`; characters: `characterGuid`). Renaming a character sets the name in the character save
through the game's `SaveManager`. The arrows are copies of the slot's trash-can
button with an arrow icon; a Harmony prefix on its `OnActivated` moves the slot instead of opening the
delete prompt. The manifest sets `skipSafetyChecks: true` because the sandbox bans the game's
file-system namespace (`Pug.Platform`).
