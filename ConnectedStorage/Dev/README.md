# Connected Storage UI development loop

## One-time startup

Restart Core Keeper once after installing 0.7.0-dev. Use character **Test** and world **Test**. The development file is created at:
`C:\Users\Sid\AppData\LocalLow\Pugstorm\Core Keeper\Steam\138826622\mods\ConnectedStorage\ui-dev.cfg`.
Only this development build defaults diagnostics on; set `[Development] Enabled = false` to disable captures and use the default layout. Configuration polling continues so it can be enabled again without restarting.

## Capture without screenshots

Press **F8** with a normal chest open, with Item Browser open, or with our terminal open. Terminal opening also captures frames 0, 1 and 11. Run:

```powershell
python ConnectedStorage/Dev/capture_ui.py
```

JSON goes to `build/connectedstorage/ui-captures/`. Captures include resolution, UI camera clipping planes/size, transforms and visible sprite bounds of our browser, native bag, hotbar and chest. The active Item Browser panel is captured too, when its installed component matches the inspected version. Source logs retain each capture for comparison. Opening/closing reason logs remain enabled.

## Edit spacing without restart

Edit the five coordinates in `ui-dev.cfg` and save. The game polls once per second; changes apply while the terminal remains open. These coordinates use gameplay UI scale, not screen pixels. Invalid/nonfinite or out-of-range values fall back to defaults. Do not change resolution/UI scale during a baseline comparison.

## Unity preview

Open `C:\Users\Sid\CoreKeeperModSDK` with Unity 6000.0.59f2. Choose **Connected Storage > UI Development Preview**. Use **Open / create preview scene**, then **Load latest game capture** after running the capture script. Scene view overlays measured bounds; browser visuals come from the actual prefab. The window can write live layout values to the same config file.

This is an edit-mode visual fixture, NOT a game simulation. Gameplay MonoBehaviours are removed from the scene copy to avoid missing Manager singletons; runtime-generated text/slots and native inventory sprites are not simulated. The original prefab is untouched. Native inventory is shown as labeled captured bounds in Scene view. Do not press Play expecting functional inventories.

## Repeatable acceptance run

Keep resolution and UI scale unchanged. Record one F8 capture for each baseline: normal inventory, native chest, Item Browser, storage terminal. Run capture script after the sequence.

1. Open/close terminal five times: no flash, drift, clipped panel, or stuck input. Compare first and last captures.
2. Move one disposable stack from bag into storage, then withdraw it. Verify total count before/after is identical. Test one-item withdrawal separately.
3. Deposit at least 64 distinct stacks to exceed one viewport. Scroll to bottom/top; search for the last stack; clear search; change sort/direction and grid/list. Verify the physical item withdrawn matches the clicked result.
4. Test empty storage, no search matches, full storage, and filtered results.
5. Close terminal; open native chest and Item Browser again. Verify positions, mouse input and tooltips remain normal.
6. Repeat the layout checks at a second supported resolution/UI scale. Record settings with results.

Do not call a test passed from compilation, preview rendering, or a successful capture alone. Runtime transfers, input response and loader safety checks need a live run. User currently drives the game; these tools do not inject input or modify saves.


## Streamlined commands (0.7.3-dev)

From the repository root:

```powershell
# Compile every manifest source, test models/captures, verify all bundles,
# render Unity fixtures, then install and compare installed SHA-256 hashes.
python ConnectedStorage/Dev/check.py --preview --install

# Faster code-only pass: retains bundle validation, skips Unity startup.
python ConnectedStorage/Dev/check.py --install

# Watch loader messages, exceptions, opening/closing events and captures for 60s.
python ConnectedStorage/Dev/watch.py --seconds 60
```

The watcher is bounded and must be running during a test session; it is not a permanent background service. It writes `build/connectedstorage/log-watch.json`. The check command writes `build-report.json` containing version, file hashes and explicit runtime-verification status. Failure stops before installation; preview mode requires the SDK project to be closed. Compiler defaults to the existing local Toolset download; `--compiler <path-to-csc.dll>` overrides it. Requires Python packages UnityPy and Pillow, .NET 8 runtime, installed game assemblies and the configured Unity SDK.

Fixture PNGs and their verification result are in `build/connectedstorage/previews/`. They use the actual slot artwork and imported item icons with a SUBSTITUTE font for labels/counts. They are geometry fixtures, not the actual PugText renderer or an interactive inventory. Five cases: empty, counts, full-grid, long-names, no-results. `replay-contract.png` checks a synthetic captured transform/resolution through Unity; it does not mean the latest live game capture has passed. Incomplete/old captures are rejected, and `latest.json` is removed rather than left stale. The runtime node serializer now explicitly writes positions/scales/bounds as primitive vectors.

Live config adds `PanelScale` (0.6–1.4, scales browser dimensions/contents together) and `TextScale` (0.75–1.5, heading and empty hint only). These do not independently change grid spacing or search/count font size. Inventory stays at native scale. `HotbarY` is obsolete because the inventory contains its own hotbar row. One restart is needed for these new code/config keys; subsequent value edits reload while the terminal stays open.
