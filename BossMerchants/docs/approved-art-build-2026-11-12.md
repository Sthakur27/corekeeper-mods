# Approved art build — 2026-11-12

Implemented all eight approved NPC concepts and their eight summoning items. Source artwork and exact image-generation prompts are preserved in `Unity/Art/approved_sources/`; the concept boards remain in `ArtConcepts/StylePass-2026-11-12/`.

## Implementation

- Built 18 merchant texture sheets and 24 idol textures (icon, small icon and placed image for each item). Runtime art has hard alpha, point filtering and no mipmaps. NPC palettes contain 37–44 visible colors, with reserved highlight colors to preserve small lanterns and eyes.
- Legacy atlas dimensions, sprite rectangles, pivots, timing and ground rows remain native. Ankhra uses the same Void Merchant animation GUIDs and 8/6/27 frame counts, with 32px-wide frames and explicit bottom-centre pivots so her broad scarab shape is retained.
- Animations use pixel-aligned breathing/compression, walking sway, greeting nods and slime arrival reveals. They are modest procedural motions, not separately illustrated articulated animation poses.
- Ottoline suppresses the native fishing rig's misaligned lantern sprite/flare only while her pooled graphic is in use. Cached renderer references and their prior `forceRenderingOff` values are restored on despawn. No change to the per-frame swap loop; lookup happens once per pooled graphic and subsequent flag changes happen only on spawn/despawn.

## Verification completed

- Independent `validate_approved_art.py`: eight NPC sets and 24 idol images pass native layout, binary alpha, palette, ground, cell containment, animation variation and overlay alignment checks. All eight actual-pixel preview boards inspected against the approved concepts and native reference scale.
- Offline Roslyn compile: zero errors; one existing unused stub-event warning.
- Unity standalone Windows and Linux bundle build succeeded. Both verify with 16 uniquely named ghost prefabs, 42 textures and the Ankhra skin.
- All 42 textures in each standalone bundle have exact source alpha and visible RGB; Unity's normal transparent-edge RGB dilation is excluded from visible-pixel comparison. Ankhra's pivots verified in serialized skin data.
- Repo and installed Overhaul Windows/Linux bundles updated with `update_combined_art.py`. All 42 texture images are byte-exact after re-opening; 2,423 unrelated serialized objects and the existing shared resource stream remain byte-for-byte unchanged. Both platform variants pass combined-bundle verification (35 unique ghost prefabs).
- Existing installed `Visuals.cs` was verified to match the pre-task source after Overhaul feature-gate injection before replacing that one script. Other installed scripts were not rebuilt or overwritten.
- Launched Core Keeper after installation. Player.log reports `Successfully compiled SidsOverhaul safetyCheck=True`, stages the updated Windows combined bundle and reports `[BossMerchants] v0.1.0 loaded: 8 boss merchants, grid 8x3.` Game left running.
- Standalone zip contains the manifest at its root, every manifest-listed file and README; archive integrity verified. No diagnostics or concept/source-art files included.

## Outputs and rollback

- Installed: existing `StreamingAssets/Mods/SidsOverhaul` received its two art bundles and the gated BossMerchants `Visuals.cs` only. No duplicate standalone mod installed.
- Standalone package: repository `release/MoreNPCs-approved-art-2026-11-12.zip`.
- Built-pixel comparison: `Unity/Art/approved_build/previews/lineup.png`; individual boards and walking GIFs beside it.
- Pre-change backup: repository `build/bossmerchants/approved-art-backup-20261112/before.zip`, with file hashes and archive mappings in `inventory.json` (404 files, including the installed Overhaul). These files are local, not release artifacts.
- Unity build log: `C:/Users/Sid/AppData/Local/Temp/bossmerchants_jrrtqy3u/build.log`.

## Remaining validation

Loader acceptance and source/bundle checks are complete. In-world visual playtesting, pooled vanilla-merchant reuse, Ankhra's animation pivot, and multiplayer behavior have not been manually verified in this pass. No runtime performance benchmark or performance improvement is claimed. Publication was not requested and no upload was performed.
