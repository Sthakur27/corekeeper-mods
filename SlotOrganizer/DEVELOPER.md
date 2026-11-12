# World & Character Manager developer notes

The internal mod name and folder remain `SlotOrganizer`; the public name is World & Character Manager.

A slot number is the save filename. Moving swaps the slot files through the game's filesystem and reloads the list. World files include `worlds`, `worldinfos`, `worldgenparams` and `servermaps`; character files include `saves` and `maps/<slot>/`. Characters associate their world progress with world IDs, rather than slot numbers.

Copies duplicate these files, then assign a new identity. For worlds, `SlotCopy` replaces the world GUID in the Brotli- or GZip-compressed save and updates `worldinfo`. If replacement fails, the copy retains the old GUID and a warning is logged. This fallback means a blanket promise of independent world identity or map tracking would be incorrect. Character copies receive a new `characterGuid`.

Character editing uses the game's customization screen and `SaveManager`, preserving saved armor customization fields. The role picker is hidden. Done writes the character and returns to the list; leaving the screen clears the editing state.

The arrow controls are clones of the trash-can button. A Harmony prefix handles their activation. The mod update returns outside the main-menu world and character lists, and excludes `SelectWorldFromModMenu`.

The manifest uses `requiredOn: 0` so joining players are not required to install the mod. Version 1.0.1 corrected the original requirement. It declares no dependencies and sets `skipSafetyChecks: true` because the sandbox blocks the filesystem APIs needed here, including `Pug.Platform`.

This documentation review checked the current source and manifest. It did not run the game, test Steam Cloud conflicts or verify copied saves in multiplayer. It does not establish compatibility beyond the stated game version.
