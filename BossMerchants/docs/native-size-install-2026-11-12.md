# Approved native-size corrections — 2026-11-12

Sid approved the NativeSizeReview comparison and requested rebuild/install, with publication deferred until in-game verification.

Applied `Unity/size_review/native_size_profile.json` through `import_approved_art.py --profile ... --apply`. All 50 production PNG/metadata files exactly match the approved staged review. All eight NPC animation sets and 24 spawn-item images pass `validate_approved_art.py`. No runtime scripts or prefab scales changed.

Updated both Windows/Linux combined Overhaul bundles, independently preserving the repo and installed bundle baselines. Each output reopens with 42 exact source textures, unchanged unrelated serialized objects (2,423) and resource stream, and passes the 35-ghost-prefab uniqueness check. Installed bundle hashes match the verified outputs. No standalone duplicate was installed.

Rollback: `build/bossmerchants/native-size-install-20261112/before.zip` and its `inventory.json` contain 139 pre-change production/bundle files. Verification output and installed hashes are beside them.

Unity standalone build log: `C:/Users/Sid/AppData/Local/Temp/bossmerchants_jy_exw1r/build.log`.

Both standalone Windows/Linux outputs pass prefab verification (16 unique ghost prefabs, 42 textures, Ankhra skin). All 42 textures in each output match source alpha and visible RGB; transparent-edge RGB dilation is excluded. The SDK logs `InventoryAuthoring.OnValidate` exceptions while components are initially added; the build completes and the serialized outputs pass the checks above. No runtime script changes required a new Roslyn compile.

The review boards remain historical before/after records. Their review-only status describes when they were generated; this approval supersedes the hold on applying them. Future art imports must use the native-size profile to retain these sizes.

No game launch, publication, or upload was performed. The installed assets take effect on the next launch. In-game verification remains with Sid.
