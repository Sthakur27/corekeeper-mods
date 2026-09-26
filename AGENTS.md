# Core Keeper modding notes (for agents and humans)

Everything learned while building `LoadoutSharing` (Sept 2026, game version with Roslyn mod loader
1.x, Unity Entities 1.x + NetCode). Read this before starting any new Core Keeper mod.

## 1. Where things are

| Thing | Path |
|---|---|
| Game | `C:\Program Files (x86)\Steam\steamapps\common\Core Keeper` |
| Game assemblies (reference these) | `CoreKeeper_Data\Managed\*.dll` |
| Local mod install folder (side-loader) | `CoreKeeper_Data\StreamingAssets\Mods\<ModName>\` |
| mod.io mods Sid has installed (all shipped as source) | `C:\Users\Public\mod.io\5289\mods\<id>_<version>\` |
| Loader's compiled/patched copies of every mod | `%LOCALAPPDATA%\Temp\Pugstorm\Core Keeper\ModLoader\<ModName>\` |
| Game log | `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` |
| Per-mod configs written by CoreLib | `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Steam\138826622\mods\<ModName>\` |
| Mod loader state (unsupported-version allow list, load order) | `...\Steam\138826622\modloader\config.json` |
| Saves (plain JSON) | `...\Steam\138826622\saves\`, backups in `backups\` |
| Our mod sources | `C:\Users\Sid\CoreKeeperMods\<ModName>\` + `install.bat` |
| Decompiler + compiler downloads | scratch only; re-download as in section 4 |

Steam app id is 1621690 (`steam://rungameid/1621690`). Mods here come from **mod.io**, not the Steam
Workshop (`steamapps\workshop\content\1621690` does not exist).

## 2. How the official mod loader works

- A mod is a folder with `ModManifest.json` plus loose `.cs` files. **No DLL, no Unity project
  needed.** The game compiles the scripts at startup with RoslynCSharp (`PugMod.Loader.LoadScripts`)
  and runs a "safety check" sandbox (blocks `System.IO` etc.; use CoreLib for files).
- `PugMod.SideLoader` scans `StreamingAssets/Mods/*/ModManifest.json` **at startup only**
  (its `Update()` is never called at runtime). Restart the game after installing/editing.
  Side-loaded mods are always treated as version-compatible.
- Manifest template (copy from `LoadoutSharing/ModManifest.json`). Fields: `guid` (any 32 hex),
  `name` (the mod id; also used as namespace by convention), `displayName`, `accessesExtraAssemblies:
  true` (lets you reference every game assembly), `requiredOn: 3` (client+server), `files[]` with
  `path` + `guid` (guid can be anything, `""` is fine), `dependencies[{modName, required}]`.
  Every `.cs` must be listed in `files`.
- Entry point: a class implementing `PugMod.IMod` (`EarlyInit`, `Init`, `Shutdown`,
  `ModObjectLoaded`, `Update`). `Update` runs every frame (client), handy for UI syncing.
  `TypeManager` is not ready in `EarlyInit` (some things throw there); use `Init`.
- Harmony (`0Harmony.dll`) is available: `[HarmonyPatch(typeof(X), "Method")]` static classes are
  auto-applied. Overloads: `[HarmonyPatch(typeof(X), "M", new[] { typeof(bool), typeof(int) })]`.
- Player.log lines to look for: `loaded mod <name> at ...`, `__Roslyn Compile Output__`
  (errors follow), `Successfully compiled <name> safetyCheck=True`, `mod <name> load error: CompileFailed`.
  `grep -a` the log; it contains binary junk.

## 3. ECS rules that matter

- The game is Unity DOTS. Player, inventory, equipment are entities; the UI is MonoBehaviour.
  Single player runs a client world and a server world in one process. Multiplayer clients only
  get what NetCode replicates (`[GhostField]`); anything else must be derived deterministically on
  both sides (e.g. from the prefab) or it will disagree between client and server.
- **Do not use `Entities.ForEach`, `SystemAPI`, `IJobEntity`, `[BurstCompile]` ISystems, or
  `[GhostComponent]` types in a mod.** Those need Unity source generators the runtime compiler
  doesn't run (mod.io mods that use them ship `Scripts/Generated/*.g.cs` produced by the official
  Unity SDK). Instead write `public partial class X : PugSimulationSystemBase` with
  `GetEntityQuery(ComponentType.ReadOnly<A>(), ...)`, `query.ToEntityArray(Allocator.Temp)` and
  `EntityManager.GetBuffer/GetComponentData/SetComponentData` in `OnUpdate`. Works fine, see
  `FastSmelter1215` and our systems.
- System placement that works: `[UpdateInGroup(typeof(SimulationSystemGroup))]
  [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
  [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]`.
  `World.IsServer()` (needs `using Unity.NetCode;`) tells which world you're in.
- Harmony cannot patch Burst-compiled jobs/ISystems. Managed `SystemBase.OnUpdate` can be patched,
  but only after `BurstDisabler.DisableBurstForSystem<T>()` (see `DisableDurability` mod). Some
  behaviour (e.g. hotbar right-click equip in `EquipmentLateUpdateSystem`) is simply unreachable.
- Changing inventories: never poke `ContainedObjectsBuffer` directly for moves (aux data, locks,
  replication). Enqueue into the `InventoryChangeBuffer` singleton on the **server**:
  `changes.Add(new InventoryChangeBuffer { playerEntity = e, inventoryChangeData = Inventory.Create.Swap(e, e, i, j) })`.
  Useful factories in `Inventory.Create`: `Swap`, `MoveOrDropItem(from, idx, to, hint 0, end 0, pos)`
  (end 0 = search all of the target's inventories), `MoveOrDropAllItems`, `DropItem`, `ClaimInventory`
  (forces `UpdateInventorySpace`). Indices are absolute positions in `ContainedObjectsBuffer`.
- Modifying prefabs: `API.Authoring.OnObjectTypeAdded += (Entity, GameObject authoring, EntityManager)`
  fires for every object type in every world; test `authoring.TryGetComponent<PlayerAuthoring>`.
  Appending to buffers there gives deterministic indices on client and server. Existing saved
  characters keep their old buffers, so also run a server-side "migration" system that fixes them.
- Equipment model (for reference): `EquipmentPresetsBuffer[3].equipment` is an `EquipmentCD` of slot
  indices per loadout; `ActiveEquipmentPresetCD.Value` is the active one; a Burst job copies
  `presets[active]` into the entity's `EquipmentCD` every tick. Vanilla shares bag/lantern/pet across
  loadouts by pointing all three presets at one slot. `PetOwnerCD.SlotIndex` and
  `InventoryBuffer[0].extraInventorySizeSlot` (bag) must be kept in sync by the mod if you move them.
  UI slots read through `PlayerController.equipmentHandler.<x>InventoryHandler.startPosInBuffer`,
  which vanilla only re-points in `EquipmentHandler.UpdateEquipmentPreset`. Slot UI types are in
  `ItemSlotsUIType`; `InventorySlotUI.UpdateSlot()` runs every LateUpdate and resets `icon.color`.
- Save/deserialize is by stable type hash; unknown mod components in a save are ignored (the
  "Loadout Plus" leftovers didn't break anything).

## 4. Tooling (no .NET SDK installed, only runtimes 6/8/10)

Decompile game assemblies (ILSpy CLI, framework-dependent, run with dotnet):
```bash
curl -sL -o ilspycmd.nupkg https://www.nuget.org/api/v2/package/ilspycmd && unzip -q ilspycmd.nupkg -d ilspy
dotnet ilspy/tools/net10.0/any/ilspycmd.dll -p -o out/Pug.Other "C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/Managed/Pug.Other.dll"
```
Most gameplay code is in `Pug.Other.dll`; components in `Pug.ECS.Components.dll`; prefab
conversion in `Pug.ECS.Conversion.dll`; loader in `PugMod.Loader.dll`; `Assembly-CSharp.dll` is thin.
`grep -l -a TypeName Managed/*.dll` finds which assembly defines a type.

Compile-check a mod **before** launching the game (catches API mistakes in seconds):
```bash
curl -sL -o toolset.nupkg https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset && unzip -q toolset.nupkg -d csc
# refs.rsp: one -r:"<Managed dll>" line per DLL, plus -nologo -target:library -langversion:latest -unsafe
dotnet csc/tasks/netcore/bincore/csc.dll @refs.rsp -out:Mod.dll @files.rsp
```
For dependencies that are source-only mods (CoreLib, ModSettingsMenu) write tiny stub classes for
the API surface you use instead of compiling them (CoreLib's `Entities.ForEach` code fails on new csc).
Ignore warnings; only `error CS` matters. Then install, launch via
`Start-Process "steam://rungameid/1621690"` and poll Player.log for `Successfully compiled <name>`.

## 5. Config/settings for mods

- `ModSettingsMenu` (mod.io) gives an in-game settings section:
  `ModSettings.Section(this).Hint("...").Toggle(out SettingHandle<bool> h, "Label", false).Build()`
  in `Init`; read `h.Value` live; `h.OnChanged` event. Depends on `CoreLib`. Labels are the keys
  unless you register localization terms. Declare both as `dependencies` in the manifest.
- CoreLib `ConfigFile` is the sandbox-safe way to persist anything.

## 6. Workflow that worked

1. Find a mod.io mod that does something adjacent; read its `Scripts/` (they're all source).
2. Decompile the relevant game types; grep for the component/system names.
3. Write the mod in `C:\Users\Sid\CoreKeeperMods\<Name>\`, list files in the manifest.
4. Offline compile (section 4), fix errors.
5. `install.bat`-style copy into `StreamingAssets\Mods\<Name>` (never edit there directly).
6. Launch, grep the log for compile success and your own `Debug.Log` lines, then test in a world.
7. Don't run two mods that fight over the same data (e.g. Loadout Plus + LoadoutSharing).

## 7. Sid's preferences

- Keep the game running if it's already open; ask/wait rather than killing it.
- Prefer implicit "just works" behaviour over settings toggles when the semantics are clear.
- Save editing scripts for skills/pets live in `C:\Users\Sid\CoreKeeperSkills` (see that folder).

## 8. Publishing to mod.io (full flow, done once for LoadoutSharing on 2026-09-25)

**Standing permission from Sid:** he is fine with agents creating mod pages, uploading files,
ticking the mod.io terms box, and publishing his mods on mod.io. Do not stop to double-check
those steps; just do the flow below and report the link at the end. (The only things an agent
still cannot do are account creation and sign-in, which Sid handles himself in Chrome.)

Prereqs: Sid is logged in to mod.io in his real Chrome (Discord login). Use the
**Claude in Chrome** tools (`mcp__claude-in-chrome__*`), not the built-in browser pane, so the
session is reused. Game id on mod.io is `corekeeper` (numeric 5289).

1. **Package.** From the mod source folder build a zip with `ModManifest.json` at the zip root:
   `python -c "import zipfile,os; z=zipfile.ZipFile('release/<Name>-<ver>.zip','w',zipfile.ZIP_DEFLATED); [z.write(os.path.join(r,f),os.path.relpath(os.path.join(r,f),'.')) for r,_,fs in os.walk('.') for f in fs]"`
   (run inside the mod folder). Keep the `release/` folder next to the mod for zip + logo + description.
2. **Logo.** mod.io needs a 16:9 image, min 512x288, max 8 MB. Pillow is installed; generate one
   with `release/make_logo.py "TITLE" "tagline" "subline" logo.png` (dark gradient, three slot
   boxes, Consolas Bold text) or adapt it. mod.io shows a crop dialog after upload; click **Select**
   to accept the default crop before doing anything else on the page.
   The generic three-slot `make_logo.py` looks off-topic for most mods; write a purpose-built
   script instead (see `release/make_skillxp_logo.py`: title + skill XP bars with multiplier tags,
   Segoe UI Bold, soft radial glow). Sid rejected the generic one for Skill XP Multiplier.
3. **Game version tag.** Get the running game version from Player.log
   (`grep -a -o -m1 "version: [0-9.]*"`), e.g. `1.3.0.2` → tick tag **1.3.0** (major.minor.patch).
   Without a matching tag players get the "not compatible with current version" prompt.
4. **Create the page:** `https://mod.io/g/corekeeper` → top-right **Add ▾ → Mod**
   (`/g/corekeeper/m/add/`). Step 1 fields:
   - Name, Summary (≤250 chars), Description (TinyMCE editor: click into it and `type`; hyphen
     lines auto-convert to bullets), listing image (file input inside the "Mod listing image" label
     → `file_upload`).
   - Tags: Game Version (see 3), Type = Quality of Life (or fitting), Application Type = Client +
     Server (for `requiredOn: 3` mods), Access Type = Script.
   - Visibility: Public (or Private to test first). Click **Create Mod**.
   - **Gotcha:** `form_input` on the name/summary fields does NOT register with the page's
     framework; the submit fails with "Field is required". Click the field and `type` instead
     (triple-click + Backspace to clear). Radios/checkboxes: verify with
     `javascript_tool`: `[...document.querySelectorAll('input:checked')].map(i=>i.closest('label').innerText)`.
5. Step 2 (media gallery): **Save & next** (skip). Step 3 (dependencies): none → **Save & next**.
6. Step 4 (file): `file_upload` the zip to the file input, type Version (`2.0.0`), Changelog (one
   change per line), tick "I agree to the terms and privacy policy", then **Upload & publish**
   (or **Upload & keep as draft** to test via in-game subscribe first, then flip to Public in the
   mod's edit page).
7. Report the mod URL (`https://mod.io/g/corekeeper/m/<slug>`), and remember: after a game
   update, edit the mod and add the new version tag (no re-upload needed unless the code broke).

Updating an existing mod: open `https://mod.io/g/corekeeper/m/<slug>/edit` (or the mod page →
edit), go to the Files tab, upload a new zip with a bumped version + changelog.

Published mods: **Loadout Fallback** → https://mod.io/g/corekeeper/m/loadout-fallback (mod id 6403817).
**Master Pet Plus** → https://mod.io/g/corekeeper/m/master-pet-plus (fork of Parcew's Master Pet; published 2026-09-25).
Gotchas seen on the second publish (2026-09-25): the first click on **Create Mod** can silently do
nothing (page re-renders at a huge zoom) — re-find the button by `find` and click it by ref; the
success toast "The Mod ... has been created" confirms. After creation the page shows an empty Step 1
form again; do NOT refill it. `/m/<slug>/edit` is a 404 — the edit page is
`/m/<slug>/admin/settings`, with a **Files** tab (left menu) that has the zip file input, Version,
Changelog, terms checkbox and **Upload & publish**. Right after upload the status reads "Pending /
requires game admin approval" for ~30 s, then flips to Live on its own with Visibility = Public.
Forks: credit the original author in summary, description and changelog.
**Skill XP Multiplier** → https://mod.io/g/corekeeper/m/skill-xp-multiplier (mod id 6403827, deps CoreLib + Mod Settings Menu added in step 3; the dependency search box works, type the name and click the result).
Gotcha seen on first publish: after step 4 the page can jump to the admin settings before you click
upload; the file may already be attached under **Files**. If "Mod status: Pending" remains, the
cause is Visibility = Private → Mod profile → Visibility → Public → **Save**. Status then shows Live.

Published: **Fast Auto Fishing** → https://mod.io/g/corekeeper/m/fast-auto-fishing (mod id 6403836).
Source `C:\Users\Sid\CoreKeeperMods\FastAutoFishing` (fork of mod.io "Autofish"/mikufish, GPL-3.0-or-later).
Gotchas learned: (1) the loader names the compiled assembly `<manifest name>.dll`, and prefabs inside an
asset bundle reference MonoBehaviours by that assembly name, so a forked mod that reuses someone's
bundle must keep their manifest `name` (use `displayName` for the new title). (2) Never write a zip
into the folder being zipped (os.walk picks up the growing zip → runaway 5 GB file); build zips in
`CoreKeeperMods\release\`. (3) Typing into the form while the image-crop dialog is open loses the
name/summary; click Select on the crop first, then re-check fields with JS before Create Mod.
(4) Timer squash pattern for Burst state machines: per-tick system in both worlds that caps
`TickTimer.targetTicks` on the replicated state component (see fastfish.cs).

## 9. Team rules (Sid is the owner; a manager agent coordinates sub-agents)

**Central git repo.** All mods live in ONE repo: `C:\Users\Sid\CoreKeeperMods` = GitHub
`Sthakur27/corekeeper-mods`. One folder per mod (`<ModName>/` with `ModManifest.json`, `Scripts/`,
`README.md`), shared `release/` (zips, art, scripts), shared `AGENTS.md`. Sub-agents commit only inside
their own mod folder (`git add <ModName>` then commit); the manager pushes. Never commit the mod.io
token, game DLLs, decompiled sources, zips or the `MasterPet_original_bundles` folder (see .gitignore).

**mod.io: use the REST API, not the browser.** Base URL `https://g-5289.modapi.io/v1` (api.mod.io is deprecated for writes), game id **5289**
(Core Keeper). Writes need an OAuth access token: `Authorization: Bearer <token>` (Sid generates it at
https://mod.io/me/access → "OAuth Access" → create token with read+write; it is stored OUTSIDE the
repo history (gitignored) at `C:\Users\Sid\CoreKeeperMods\.modio_token`, one line). Reads can use an API key (`?api_key=`), but just use
the bearer token for everything. All write requests are `multipart/form-data` (or
`application/x-www-form-urlencoded`) with `Accept: application/json`.
```bash
TOK=$(cat /c/Users/Sid/CoreKeeperMods/.modio_token); API=https://g-5289.modapi.io/v1; GAME=5289
# find tag options (game version tags etc.)
curl -s "$API/games/$GAME/tags" -H "Authorization: Bearer $TOK" -H "Accept: application/json"
# create mod (logo required, 16:9 >=512x288). visible: 0 hidden, 1 public
curl -s -X POST "$API/games/$GAME/mods" -H "Authorization: Bearer $TOK" -H "Accept: application/json" \
  -F "name=My Mod" -F "summary=one paragraph, <=250 chars" -F "description=<p>HTML ok</p>" \
  -F "logo=@release/mymod_logo.png" -F "visible=1"          # -> {"id":<mod_id>,...}
# tags
curl -s -X POST "$API/games/$GAME/mods/$MOD/tags" -H "Authorization: Bearer $TOK" -H "Accept: application/json" \
  -F "tags[]=1.3.0" -F "tags[]=Quality of Life" -F "tags[]=Client" -F "tags[]=Server" -F "tags[]=Script"
# upload file (zip with ModManifest.json at root). active=1 makes it the live file
curl -s -X POST "$API/games/$GAME/mods/$MOD/files" -H "Authorization: Bearer $TOK" -H "Accept: application/json" \
  -F "filedata=@release/MyMod-1.0.0.zip" -F "version=1.0.0" -F "changelog=Initial release" -F "active=1"
# edit (e.g. new logo, description, visibility)
curl -s -X PUT "$API/games/$GAME/mods/$MOD" -H "Authorization: Bearer $TOK" -H "Accept: application/json" -F "visible=1"
curl -s -X POST "$API/games/$GAME/mods/$MOD/media" -H "Authorization: Bearer $TOK" -H "Accept: application/json" -F "logo=@release/new_logo.png"
# dependencies
curl -s -X POST "$API/games/$GAME/mods/$MOD/dependencies" -H "Authorization: Bearer $TOK" -H "Accept: application/json" -F "dependencies[]=<CoreLib mod id>"
```
Verify with `GET $API/games/$GAME/mods/$MOD` (check `status`, `visible`, `modfile.version`, `tags`).
Standing permission from Sid covers creating, uploading, tagging and publishing via the API too.
Known mod.io ids: CoreLib 3177992, ModSettingsMenu 6211950, our mods: Loadout Fallback 6403817,
Skill XP Multiplier 6403827, Fast Auto Fishing 6403836.

**Listing images must be original art.** Generate them with Pillow from `release/make_art.py`
(pixel-sprite helper: string grids scaled up). Draw a scene that shows what the mod does; never reuse
another mod's image, game screenshots of other people's work, or a previous mod's art with the text
swapped. Each mod gets its own `release/<mod>_logo.png`.

**Sub-agent contract (what a mod-building agent must do):**
1. Read this file. Work only in `C:\Users\Sid\CoreKeeperMods\<ModName>\`.
2. Research in the decompiled game sources first (section 4); confirm the exact system/component
   names you touch and whether they are Burst (unpatchable) before designing.
3. Build, then run the offline csc compile check until 0 errors. Do NOT launch or kill the game; the
   manager launches once for all mods and reads Player.log.
4. Write `README.md` (what it does, settings, limits) and `release/<mod>_logo.png` via make_art.py.
5. Report back: mechanism, files, compile result, defaults, anything you could not do and why.
Prefer ModSettingsMenu toggles/sliders for tunables (deps CoreLib + ModSettingsMenu), 1x/vanilla defaults
unless the spec says otherwise, and `PugSimulationSystemBase` systems + Harmony only where needed.

## 10. Mod pack = a mod.io Collection

The "pack" is a mod.io **Collection** (mod.io/g/corekeeper/c), not a merged mod: players subscribe
once, every mod stays independently updatable, and no third-party code is redistributed.
`GET /games/5289/collections` works with the API key; creating/editing needs the OAuth token
(`POST /games/5289/collections`, then add mods; if the write endpoints are missing from API v1,
fall back to the browser at /g/corekeeper/c → Add). Contents = all of Sid's mods (section 8 list +
the 2026-09-25 batch) + the third-party mods Sid keeps active, EXCLUDING ones his mods replace:
Loadout Plus (→ Loadout Fallback), Autofish/mikufish (→ Fast Auto Fishing), Master Pet
(→ Master Pet Plus), XP Multiplier (→ Skill XP Multiplier), Disable Durability (→ Durability
Multiplier), and broken-on-1.3 mods (MinerKart fails to compile). Active third-party list as of
2026-09-25 (mod.io id): CoreLib 3177992, ModSettingsMenu 6211950, AutoDoor 3342278, PlacementPlus
3400322, HealthBars 4164578, AllSkills 4297529, ExpandedChestUI 5079247, Double Chest Inventory
5128201, MoreMapReveal 5476385, EternalOreBoulders 6042718, RoofingGadgetPlus 6124697,
MapTeleport1215 6161255, FastSmelter1215 6163010, BoatTurbo 6265625, CraftingReach 6312780,
SkipIntro 6363825. Refresh the list from Player.log (`loaded mod X from mod.io`) before editing.

### 9b. What actually works with a personal access token (learned 2026-09-25)
- Host must be `https://g-5289.modapi.io/v1`. Non-file writes MUST be `application/x-www-form-urlencoded`
  (`--data-urlencode k@file`); only `/files` and `/media` take multipart.
- `POST /games/5289/mods` (create) and `POST /games/5289/collections` return 403 for personal tokens.
  Create the page in the browser (name, summary, logo → click **Select** on the crop dialog — take a
  screenshot first or the click misses — tags, Public, Create Mod), then `python release/publish.py setup
  <mod_id> <Folder> <version> [dep ids]` does description, tags, dependencies and the file upload.
  `python release/publish.py file <mod_id> <Folder> <version> "<changelog>"` for updates.
  New mod id: `GET /me/mods?game_id=5289` (public search skips mods without a file).
- Our mod ids: Loadout Fallback 6403817, Skill XP Multiplier 6403827, Master Pet Plus 6403829,
  Fast Auto Fishing 6403836, Durability Multiplier 6405262, Buff Duration Floor 6405266,
  Faster Mushrooms 6405268, Bigger Watering Cans 6405271, Five Loadouts 6405273,
  Better Fishing Loot 6405276, Auto Replant 6405281, Quick Buff 6405282.

### 10b. Collection facts (2026-09-25)
- Live: **Sid's Core Keeper Pack** https://mod.io/g/corekeeper/c/sids-core-keeper-pack (collection id 116637, 22 mods).
- mod.io refuses to put any mod that declares dependencies into a collection (error 29609) and only
  reports one offender per save. So our mods' mod.io dependency records were DELETED (the in-game
  manifest deps are untouched; the loader still enforces them). Do not re-add dependencies via the API.
- Excluded because they declare deps: PlacementPlus, Mod Settings Menu, AutoDoors, Higher Crafting
  distance, Double Chest Inventory, Boat Turbo. **Mod Settings Menu is required by 8 of our mods**, so the
  collection summary tells players to subscribe to it (and CoreLib is inside the collection).
- Collections API: GET works, POST/collection edits are browser-only for personal tokens. Dialog
  "Add mods": type a name, wait 3 s, it stages automatically; repeat; then **Add mod**, then **Save**.
- **Potion Seller** → https://mod.io/g/corekeeper/m/potion-seller (mod id 6405806, v1.0.1, in the collection; collection now 23 mods).
  Collection "Save" gotcha: the Save button click can silently do nothing if an "unsaved changes" modal
  is open behind the Add-mods dialog; check `document.querySelectorAll('[role=dialog]').length == 0`,
  then click Save by ref and confirm the "Collection updated successfully" toast before navigating.
