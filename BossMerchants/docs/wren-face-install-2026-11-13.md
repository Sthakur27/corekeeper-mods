# Wren face installed — 2026-11-13

Sid approved the WrenFace review and requested rebuild/install, with publication held for in-game checking.

Applied the approved NPC source as `Unity/Art/approved_sources/farmanimals/npc_source.png`, preserving the original paired source for the bell. The importer now accepts this optional NPC-only source override. Its exact prompt is beside it. Use the native-size profile when importing: Wren remains 23x36 visible pixels.

Regenerated Wren's animation sheets through the existing importer and verified all four sheet/metadata files exactly match the approved review. The complete production-art validator passes all eight NPC sets and 24 item textures.

Rebuilt Windows/Linux standalone, repository Overhaul, and installed Overhaul bundles by replacing only the three Wren texture objects in each existing bundle. Reopened all six outputs and verified exact source pixels, unchanged object identities, and byte-identical unrelated objects/resource streams. All six pass prefab uniqueness and bundle-content checks. Installed file hashes match verified output. No runtime scripts, spawn items, other NPCs or stock lists were replaced.

This was a targeted serialized asset-bundle rebuild using UnityPy, not a full Unity SDK rebuild. Existing prefab layouts and dimensions are unchanged.

Backup, rebuild script, validation output and installed hashes: `build/bossmerchants/wren-face-install-20261113/`. No game launch, publication or upload performed. New artwork loads on the next game launch; in-game confirmation remains pending.
