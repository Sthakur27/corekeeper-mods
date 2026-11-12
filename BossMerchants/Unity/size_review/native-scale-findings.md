# Native NPC and idol scale review — 2026-11-12

This is a read-only asset/source audit, not a measurement from the running game. No images, runtime code, production sheets or installed bundles were changed.

## Result

All four native merchant bodies and all four native placed-idol main sprites have effective prefab XY scale **(1, 1)** and use **16 pixels per world unit**. Their complete main-renderer transform chains have identity rotation and scale; differences are translation/pivot placement. One occupied source pixel is therefore 0.0625 world units in each sprite-plane axis at rest. There is no hidden per-rig scale multiplier that would compensate for a mod body occupying substantially more pixels than a native body.

This means a fair native-size comparison should use actual occupied **body** pixels, excluding tall staffs, lanterns and packs when defining the body, and display each pixel at the same scale. Atlas/cell dimensions alone are not body dimensions. Do not scale an entire character to match native overall staff height and infer that its torso is native-sized.

| Native graphics prefab | Main component | Cumulative XY scale | PPU | World units / source pixel XY |
| --- | --- | --- | --- | --- |
| CavelingMerchant | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| SlimeMerchant | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| FishingMerchant | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| VoidMerchant | SpriteObject /SO | 1, 1 | 16 | .0625, .0625 |
| MysteriousIdol | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| SlimeOil | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| PileOfChum | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |
| VoidMerchantIdol | SpriteRenderer /SR | 1, 1 | 16 | .0625, .0625 |

Main paths are `<prefab>/XScaler/AnimPositionRotation/AnimScale/SRPivot/SR` (or `/SO` for Void). All eight corresponding entity-prefab roots also have scale `(1,1,1)` and identity rotation. All legacy main SpriteRenderers use simple draw mode (`m_DrawMode=0`), so their `m_Size` is not an extra sliced/tiled size factor.

## Mod inheritance

The builder copies the selected native entity data and reuses its native graphical reference (`BossMerchantsBuilder.cs`, `AddObjectAuthoring`, lines around 178–200). `Visuals.Replacement` preserves the original sprite's `pixelsPerUnit`; the sprite swap does not rescale transforms. Void uses a skin on the same SpriteObject. The four rig scales in the table therefore apply to the mod's respective merchants too.

Every mod idol uses `IDOL_BASE = 'MysteriousIdolEntity'` in `gen_assets.py`, so all mod placed idols use the Mysterious Idol graphical rig, not their boss's associated native merchant idol rig. This still gives scale 1 and PPU16. Native item-icon PPU is an asset property; inventory UI scaling is a separate presentation path and is not inferred from world sprite transforms here.

## Animation and code checks

- Native merchant classes `CavelingMerchant`, `SlimeMerchant`, `FishingMerchant`, `VoidMerchant`, and their `NPC` base do not change body scale. The four idol classes are empty `EntityMonoBehaviour` subclasses.
- `EntityMonoBehaviour.SetOrientation` sets `XScaler.localScale` to `(sign,1,1)`, where sign is +1 or -1. This mirrors facing without changing absolute width/height. Its optional random sprite flips also only change X sign; these prefabs have no randomly flipped sprites configured.
- Native idle, move and wave clips for the three legacy merchants have **no compiled scale bindings**. They animate sprite references and positions (plus some particle/activation bindings). Empty `m_ScaleCurves` alone would be insufficient evidence: player-build animation curves are packed in `m_MuscleClip`.
- `MerchantSpawn` contains scale bindings for `XScaler/AnimPositionRotation/AnimScale` and `Shadow/Shadow`. Body keyframes include X ≈0.498 and Y=1.5, returning to `(1,1,1)` by 0.18333 seconds. Those are keyframe values, not a claim about all interpolated extrema.
- The shared `objectTakeDamage` clip also contains packed scale curves, including the main `SRPivot` path; its keyframes similarly squash/stretch around X≈0.498 and Y=1.5 before returning to 1. Other shared-clip paths do not exist on these particular hierarchies; the report retains unresolved hashes instead of inventing matches.
- Void's SpriteObject can also apply shader-side `TransformAnimation` data because `useSharedTransformAnimations` is enabled. Native `voidSpawn` runs for 0.5 s and goes from roughly `(0.501,0)` to `(1,1)`. `takeDamage` runs for 0.2 s and returns to `(1,1)`. The extracted transform-animation collection has no idle/move/wave scale animation. Do not use spawn or damage frames for resting size comparison.
- `UpdateGraphicalObjectTransformSystem.TransformUpdateJob.Execute` updates rounded **position only** from ECS LocalToWorld, not rotation or scale. `CreateGraphicalObjectSystem` obtains the graphical prefab from the pool or instantiates its fallback; the inspected spawn path does not copy entity scale onto the graphical transform.

## SpriteObject conversion evidence

The decompiled `Pug.Sprite.SpriteObject` declares `PixelsPerUnit = 16f` (line 45), divides positional data by it (lines 600–601), and sends its cached `transform.localToWorldMatrix` to instancing (lines 983, 1012).

The SDK's `Packages/dev.pugstorm.sprite/Shaders/SpriteObjectPugRP.shader` defines `PPU 16` (line 27); `SpriteObjectSimple.shader` does the same (line 14). `SpriteObjectInput.hlsl` subtracts the pivot, multiplies vertex XY by `rect.zw / PPU` (line 167), applies optional transient transform animation, then multiplies by `localToWorld` (line 179). Its one-pixel outline padding is not additional opaque body area. The shader is SDK source evidence corroborated by native SpriteObject code; the installed compiled shader was not decompiled here.

## Reproduce / evidence

Run from the repository root:

```powershell
python -I BossMerchants/Unity/size_review/extract_native_scales.py
```

Inputs are installed `defaultlocalgroup_assets_all.bundle` plus the native monoscripts bundle. Output is `build/bossmerchants/size-review-native/native-prefab-scales.json` and an optional redirected extraction log. JSON records complete selected hierarchies, every local transform, cumulative matrices, renderer PPU, component data, controller clip mappings, packed clip data, decoded streamed scale keyframes, matching SpriteAssets, and shared TransformAnimations. The script filters for the actual named graphics MonoBehaviour, excluding a loose `SlimeMerchant` sprite object with the same GameObject name.

Relevant local source evidence is under `build/perf-fixes/cs-skin/decomp/Pug.Objects/`, `Pug.Other/EntityMonoBehaviour.cs`, `Pug.Other/Pug.ECS.Hybrid/UpdateGraphicalObjectTransformSystem.cs`, `Pug.Other/Pug.ECS.Hybrid/CreateGraphicalObjectSystem.cs`, and `PugSprite/Pug.Sprite/`.

## Limits

These are prefab-relative world units, not monitor pixels. Camera projection, zoom, render scale and live parent/pool transforms were not measured. They can change absolute screen size, but no per-rig resting size adjustment was found in the inspected native asset/code paths. A live screenshot at settled idle remains the final visual check. Shadow and light nodes have different rotations or scales (e.g. Caveling/Fishing glow .4, Slime glow .25, Void indirect-light X≈1.8768); those are effects, not body scaling, and must be excluded from body measurements.
