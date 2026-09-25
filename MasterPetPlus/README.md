# Master Pet Plus

Fork of the mod.io mod **Master Pet** (v1.0.2) with one change: the talent
selector offers **every pet talent in the game** (all 37) instead of only the
handful in the equipped pet's own roll pool.

## How to use
1. In the game's mod list, **disable or unsubscribe the mod.io "Master Pet"**
   (both add a button to the same window, so run only one).
2. Run `install-masterpetplus.bat` (one folder up) and start the game.
3. Open the pet talents window, click **Master Pet** (button next to Reset).
4. **Right-click** a talent slot to change which talent it holds; the selector
   now fills the whole window with all talents. Left-click still toggles points.

## Renaming notes
The UI prefab in the asset bundle binds its MonoBehaviours to an assembly name.
The bundles were patched with UnityPy (`m_AssemblyName` "MasterPet" ->
"MasterPetPlus", bundle name -> `MasterPetPlus_*.assetbundle`, re-saved LZ4) so
the mod can be named `MasterPetPlus`. Asset paths inside the bundle are still
`Assets/MasterPet/...`, hence `InternalName` stays "MasterPet" in code.
Originals are kept in `..\MasterPet_original_bundles`.

## What was changed vs. the original
- `ModManifest.json`: new guid, name `MasterPetPlus`, displayName "Master Pet Plus", renamed bundle paths.
- `Scripts/MasterPetMod.cs`: Name/Version.
- `Scripts/Handlers/PetActionHandler.cs`: `TryGetAllPetTalents` (enum values
  filtered to entries that exist in `PetInfosTable`).
- `Scripts/UI/MasterPetUI.cs`: selector clones the prefab's button to reach 37,
  lays them out in a 10-column grid across the whole window (left panel hidden
  while the selector is open), picks from the full list.
- `Scripts/Systems`, `Scripts/Generated`, bundles: untouched (the generated
  Entities.ForEach code must stay in sync with the system source).

## Offline compile check
`dotnet csc.dll @mpp.rsp` with `-r:` for every DLL in `CoreKeeper_Data\Managed`
and all `Scripts/**/*.cs` (including Generated). Last run: 0 errors.
