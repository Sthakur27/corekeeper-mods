# Map markers, nameplates, and test incident — 2026-11-13

## Production behavior

- `MerchantMapMarkers.cs`: client-only marker metadata, native `MapUI.UniqueMarkerInfo` with eight existing 16px idol sprites and localized merchant names. Native `UniqueScene` marker pool owns refresh, placement, visibility and release. No prefab/network/save changes.
- A client presentation system adds `MapMarkerCD` to received merchants. It queries only merchants without a marker, with read-only declarations and a `MerchantCD` component-order gate. No recurring transform snapshots in the mod. Registration retries are limited to 30 per map instance.
- `NpcNameplates.cs`: NPC graphic spawn/despawn hooks, including vanilla NPCs. Revised source clones native text with its serialized settings, guards the language list, and disables labels session-wide after a rendering exception. Reinstalled after combined-mod compile verification at the user's request; corrected rendering is not yet runtime-tested.
- Multiplayer scope chosen by Sid: nearby/synchronized NPCs. Server ghost relevance remains unchanged. This does not implement global player-style NPC synchronization.

## Animation defect

NVIDIA screenshots at 14:50 show Ankhra rendering a horizontal strip of apparent duplicates. Native `SpriteObject.UpdateAtlasRect` uses `skin.GetReplacementAt(m_currentAnimationIndex)`, not the GUID lookup. Native voidMerchant animation order is idle (8 frames), emote (27), move (6); the skin was idle, move, emote. Walking therefore selected a 27-frame sheet with a six-frame divisor, showing 4.5 copies per frame.

Corrected the order in standalone, combined, and installed Windows/Linux bundles. Reopened each output and verified every other serialized object and resource stream unchanged, preserving Wren's concurrently updated artwork. Updated the Unity builder, art updater, and bundle validator to preserve/enforce this order. `fix_skin_order.py` performs the targeted repair reproducibly.

## Test incident and recovery

The first marker test installed a development MapProbe that spawned eight merchants. Its runner lacked automatic cleanup, and interruption left it installed. A subsequent normal session executed the probe in Aethermoor. The fixture was not adequately gated to the test world. This was an agent error, not production marker behavior.

On discovery, moved the entire diagnostic mod out of the game Mods folder and backed up current worlds and Player.log under `build/bossmerchants/recovery-20261113/`. Do not run the old `map_test.py restore`: it would overwrite newer installed work such as Wren's artwork. Production has no MapProbe dependency or spawning fixture.

Sid identified Old Hemlock, Wren and Tilly as the earned merchants. Original-world cleanup must use identity/home evidence and preserve these NPCs; do not reset the whole world or remove arbitrary duplicate entities. Read-only audit uses a named copy of Aethermoor and automatically disables/removes the diagnostic manifest in a finally block.

### Subsequent containment

The audit runner also forced route movement and scheduled a quit, interfering with normal play. No further automated launches are authorized by the current recovery plan. Its log additionally recorded 14,461 NullReferenceExceptions in PugText.ShouldForceLatinFont from the new labels: AddComponent did not initialize the serialized language list. The installed nameplate source and manifest entry were removed, backed up under `nameplate-disabled/`. This is a confirmed error flood; the scheduled quit is recorded, but a separate crash has not been established.

The runner recreated its saved character slot 3 and world slot 4 even after user deletion. Confirmed the current identities (character 710c6797440f6c6485e1c9aa2f6efdbf and Dyseria Bench/fa5404aef4f0a02be6008f5a196e92b4), then moved those exact test slots, related map and their .pugbackup files outside the save profile to `quarantined-test-slots/`. Did not restore older slot snapshots, which could resurrect additional deleted saves. Other save slots remain untouched. Diagnostic mod and bench.txt are absent.

Eight accidental NPCs in Aethermoor are identified but **not yet removed**. The scratch NpcAudit.cs now contains a proposed repair and is no longer read-only; do not rerun audit_run.py. The original three earned merchants must remain. Do not reuse map_test.py or its broad restore operation.

### User-directed cleanup command supersedes targeted repair

The user subsequently requested a command to kill all More NPCs, then fix labels, explicitly cancelling the low-health approach. No low-health changes were made. `/morenpcs killall` is a local host/single-player command, registered with CoreLib (once via the Overhaul registration when combined). It queries server merchants only on invocation, matches the eight catalog IDs, excludes prefabs, and enables native EntityDestroyedCD; it never raw-destroys entities or adjusts health. It reports how many were queued. Includes earned merchants by explicit request; excludes vanilla merchants and idols. Valid idol rooms may summon replacements. Scope is server-loaded entities, not an offline save sweep. No network RPC, recurring scan, automatic execution, or test-runner dependency.

Compiled all currently installed Overhaul scripts with these three staged changes (command, entry-point registration, corrected labels): zero errors. Installed only those scripts and manifest entries, preserving other ongoing work and art. Backups/stage/check response are in `commands-labels/`. No launch, save edits, or command invocation performed. Runtime labels, cleanup behavior and remote multiplayer still need verification in a user-controlled session.

### Proximity labels

Sid confirmed the cleanup command and corrected labels both work in game. Subsequently requested proximity-only labels. Labels now show within eight world units/tiles on the XZ ground plane of the local player. Existing per-graphic LateUpdate does one squared-distance comparison when UI is visible; no new callbacks, ECS queries, network messages or recurring allocations. Out-of-range text objects are inactive and creation/rendering is deferred until first proximity entry; pooled labels are reused. Full-map/hidden-UI/no-local-player paths hide labels and skip the distance test. Feature-off still prevents spawn patches through generated guards. Native map markers remain unaffected.

Combined installed-source compile passed with zero errors; installed only the changed label source (backup under `proximity-labels/`). Running game was left open; restart is required. No benchmark or proximity runtime test performed, so expected small arithmetic cost is a design assessment, not a measured performance claim. Remote clients independently evaluate their own player position.

### Vanilla merchant map markers

Added the six vanilla merchant IDs (Caveling, Slime, Fishing, Seasonal, Crystal, Void) to existing client marker support. Summoning-item associations were checked directly against the installed game's decompiled SpawnMerchantSystem.GetRequiredObjectForMerchant. Uses those item icons and Items/<ObjectID> localization terms. Preserves preexisting native marker metadata/components. Registration shares the existing bounded initialization retries; the same MerchantCD order-version gate and narrow query handle received entities. No new recurring scans, callbacks, server writes or network traffic; additional native marker work scales with synchronized vanilla merchants. Proximity labels do not constrain map markers.

Combined installed-source compile passed with zero errors and only MerchantMapMarkers.cs was installed, backed up under `vanilla-markers/`. No game launch, save mutation or fixture deployment. Functional vanilla marker display and remote multiplayer remain pending user-controlled runtime verification; no measured performance claim.

## Validation limits

Standalone and assembled Overhaul offline compilation passed; loader accepted the marker and nameplate sources with safety checks enabled. First local host/client marker fixture reported eight received entities, eight marked, eight visible, eight distinct icons, and correctly localized hover names. Later fixture counts were unstable, so it does not establish full lifecycle correctness. The single short capture is not a performance comparison and must not be cited as proof of negligible overhead. Remote multiplayer and controlled repeated-run performance measurements remain outstanding.
