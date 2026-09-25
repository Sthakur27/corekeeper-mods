# Five Loadouts (Core Keeper mod)

Raises the number of equipment loadouts from 3 to 5. Loadouts 4 and 5 get their own helmet,
necklace, chest, pants, both rings and off-hand slots; the four pouch slots stay shared, exactly
like vanilla. Two more tabs appear in the character window and the "cycle loadout" key wraps at 5.
No settings.

Optional companion: **Loadout Fallback** (`LoadoutSharing`). With it installed **and updated
per `LoadoutSharing.patch.md`**, loadouts 4 and 5 also get a private bag, lantern and pet slot
and fall back to loadout 1 the same way loadouts 2 and 3 do. Without it, loadouts 4 and 5 share
the bag, lantern and pet with the other loadouts (vanilla behaviour for 2 and 3).

Install: copy this folder to `CoreKeeper_Data\StreamingAssets\Mods\FiveLoadouts` and restart
the game. In `Player.log` look for `Successfully compiled FiveLoadouts`,
`[FiveLoadouts] Player layout: presets=5 ...` and
`[FiveLoadouts] Character window extended to 5 preset tabs.`

## How it works

- **Prefab time.** `API.Authoring.OnObjectTypeAdded` fires for the player prefab in every world.
  For presets 4 and 5 the mod appends 10 slots each to `ContainedObjectsBuffer` (helm, necklace,
  chest, pants, ring1, ring2, off-hand, bag, lantern, pet, in that order) and adds two
  `EquipmentPresetsBuffer` entries copied from preset 1 with the gear indices rewired. Client and
  server run the same code on the same prefab, so the indices agree without any network sync.
  `EquipmentPresetsBuffer` is not saved by the game (loaded characters are rebuilt from the
  prefab), so existing characters get the five entries automatically.
- **Burst.** The game's `SelectedEquipmentChangeSystem` clamps the requested preset to the buffer
  length and copies `presets[active]` into `EquipmentCD`; `EquipmentHandler` clamps the same way.
  `const MaxEquipmentPresets = 3` and `[InternalBufferCapacity(3)]` are never used as a limit at
  runtime, so five entries work without touching Burst code. Nothing else in the game
  hard-codes 3 on the preset index except the hotkey cycle (patched).
- **Existing saves.** `ContainedObjectsBuffer` IS saved, at its old length. A server-side system
  (`PresetMigrationSystem`) grows it to cover the new slots; the client receives the buffer
  through replication. Items already in the buffer are never moved.
- **UI.** `CharacterWindowUI` drives the tabs entirely from its `presetTabs` list, so the mod
  clones the third `CharacterWindowTab` twice at runtime, offsets the clones by the tab spacing,
  turns off the cloned inspector `onClick` (which would select preset 3), adds a runtime listener
  for the right index, replaces the icon with a generated pixel "4"/"5", renames the hover term,
  wires left/right controller navigation and appends them to the list. Re-done automatically if
  the UI is rebuilt.
- **Hotkey.** Harmony prefix on `PlayerController.SetActiveEquipmentPreset`: when the cycle key
  was pressed this frame and the requested index equals vanilla's `(active + 1) % 3`, it becomes
  `(active + 1) % 5`. Keys for loadouts 1-3 still work. There are no vanilla input actions for
  loadouts 4 and 5 (Rewired actions cannot be added by a script mod), so reach them by tab click
  or cycling.
- **Loadout Fallback bridge.** Looked up by name (`AccessTools.TypeByName`), never referenced at
  compile time. Five Loadouts declares Loadout Fallback as an optional dependency so the loader
  runs Loadout Fallback's prefab handler first; our 20 slots always land after its 6, and we then
  call `SlotLayout.RegisterPreset(3|4, ...)` so its fallback, UI sync and bag/pet sync cover all
  five presets.

## What works / what doesn't

Works (by construction; needs the manager's in-game run to confirm): five presets on client and
server, switching by tab or cycle key, own gear per loadout, migration of existing characters,
save/load of the active preset 4 or 5, multiplayer (no extra replication needed).

Not done:
- Dedicated hotkeys for loadouts 4 and 5 (no input actions to bind; would need a Rewired
  action injected into the input map, out of scope).
- Tab icons: tabs 4 and 5 show Roman numerals IV (gold) and V (purple) generated at runtime in
  the vanilla pixel style. The vanilla "I" sprite is read back (blit to a RenderTexture) to copy
  its canvas size, pixels-per-unit, stroke width, glyph height, letter gap and shadow/outline
  shade; the I pixels are reused recoloured and the V is drawn to match. If the sprite cannot be
  read, a plain 1px, 5-high style is used instead. The style found is logged as
  `[FiveLoadouts] Numeral style (...)`.
- Tab placement: the five tabs keep vanilla spacing and the column is shifted up (or, if that
  is not enough, compressed) so all tabs stay inside the vertical extent of the window frame
  (largest sprite under the window root). Logged as `[FiveLoadouts] Tab column: ...`.

## Save-safety notes

- Slots are appended at the end of the contained-objects buffer, after Loadout Fallback's six
  (when installed). Their indices are stable as long as the set of mods that append player slots
  and their relative order do not change. Both mods log their layout on startup; compare the
  numbers if something looks off.
- **Switch to loadout 1, 2 or 3 before uninstalling this mod.** `ActiveEquipmentPresetCD` is
  saved; with the mod removed the vanilla Burst copy job would index a 3-entry buffer with 3 or
  4 (no bounds check in Burst). Items in loadout 4/5 slots stay in the save (unknown slots are
  ignored, not deleted) but are unreachable until the mod is back.
- The layout is the same with or without Loadout Fallback (the private bag/lantern/pet slots
  are always allocated, only wired in when the bridge is available), so toggling Loadout
  Fallback does not shift any index.
- Death/grave: vanilla only moves the main inventory range (`InventoryBuffer[0]`, slots 10 and
  up for non-hardcore) into the grave, the same as for vanilla loadouts 2 and 3; the appended
  slots are outside that range.

## Files

- `ModManifest.json`: guid `5f1d0a7c9e3b4d2a8c6f0b1e2d3a4c55`, `requiredOn: 3`, optional
  dependency on `LoadoutSharing`.
- `Scripts/FiveLoadoutsMod.cs`: entry point.
- `Scripts/PresetLayout.cs`: prefab-time slot allocation and layout table.
- `Scripts/LoadoutSharingBridge.cs`: reflection bridge to Loadout Fallback.
- `Scripts/PresetTabsUI.cs`: tab cloning.
- `Scripts/Systems/PresetMigrationSystem.cs`: buffer growth for existing saves.
- `Scripts/Patches/PresetCyclePatch.cs`: cycle key wraps at 5.
- `LoadoutSharing.patch.md`: exact edits Loadout Fallback needs (validated to compile).
- `release/fiveloadouts_logo.png`: listing image (from `..\release\make_art.py fiveloadouts`).
