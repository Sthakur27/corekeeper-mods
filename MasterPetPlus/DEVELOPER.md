# Master Pet Plus developer notes

Master Pet Plus is a fork of Master Pet v1.0.2 by Parcew. The source currently identifies itself as version 1.1.1. The legacy mod.io listing was taken down in October 2026; repository instructions say not to republish forks without the original author's permission.

## Bundle and assembly names

The UI prefab in the asset bundle binds its MonoBehaviours to an assembly name. The bundles were patched with UnityPy: `m_AssemblyName` changed from `MasterPet` to `MasterPetPlus`, bundle names changed to `MasterPetPlus_*.assetbundle`, and the bundles were re-saved with LZ4 compression.

Asset paths inside the bundles remain `Assets/MasterPet/...`, so `InternalName` stays `MasterPet` in code. Original bundles are kept locally in `MasterPet_original_bundles` beside the mod folder and are excluded from source control.

## Implementation

- `ModManifest.json` uses a new GUID, the name `MasterPetPlus`, display name `Master Pet Plus`, and renamed bundle paths. It declares no dependencies and requires installation on both client and server (`requiredOn: 3`).
- `Scripts/MasterPetMod.cs` defines the display name and version and loads the UI bundle by its contents.
- `Scripts/Handlers/PetActionHandler.cs` builds the full talent selector from enum values with entries in `PetInfosTable`. The original README records 37 talents. The code discovers the entries rather than hardcoding that count.
- `Scripts/UI/MasterPetUI.cs` expands the prefab's selector buttons into a 10-column grid. The left panel is hidden while selecting a talent. The UI also provides level, skin and point controls and on-screen control hints.
- Talent point totals come from `PetExtensions.GetTotalTalentPoints`. Lowering a level clears allocated points; Apply closes the window rather than committing a deferred set of edits.
- Level, skin and point changes use inventory input actions. Talent ID changes use `SetPetTalentIDRPC` and the server system.
- The original fork notes record the system and generated source as unchanged. Keep `Scripts/Generated` in sync with the system source; its generated Entities code is required. The bundles have the assembly-name changes described above.

## Recorded compile check

The previous README records `dotnet csc.dll @mpp.rsp`, referencing every DLL in `CoreKeeper_Data/Managed` and all `Scripts/**/*.cs`, including generated files. Its recorded result was zero errors. No compile was rerun for this documentation edit. This record does not establish current game compatibility, loader acceptance or multiplayer behavior.
