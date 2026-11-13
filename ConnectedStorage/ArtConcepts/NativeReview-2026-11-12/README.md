# Native-size workbench and Octarine review

Review candidates, not installed or approved replacements:

- `Workbench-16.png`: 16x16 RGBA, hard alpha, 16 visible colors, 216 opaque pixels.
- `OctarineChest-16.png`: 16x16 RGBA, hard alpha, 13 visible colors, 246 opaque pixels.

Generated isolated source studies with built-in image_gen; exact prompts in `prompts.json` and unmodified output in `sources/`. `make_review.py` crops the sources, normalizes their silhouettes to a 16x16 front/down canvas with nearest-neighbor sampling, thresholds alpha at 240 and maps to an explicit shared material palette without dithering. This normalization slightly changes source aspect ratios; it is a deliberate review profile, not evidence of runtime scaling. Measurements are recorded in `measurements.json`.

The first adaptive palette conversion lost the workbench's rare teal accent. A fixed material palette preserves that distinction. Inspected final actual pixels on the equal-scale comparison: hammer, socket, tier rune and front gem remain distinguishable. Octarine's top is still a large uninterrupted slab compared with the current unit, and its lower gem becomes a small accent. These are aesthetic tradeoffs for user review, not claims of perfect native matching. Both have horizontal top/front geometry and shallow front faces, with no diagonal yaw.

Reproduce from repository root:

`python ConnectedStorage/ArtConcepts/NativeReview-2026-11-12/make_review.py`

Comparison output: `build/connectedstorage/native-review-20261112/comparison.png`. Each sprite is shown at actual 1x file pixels and exact 12x nearest-neighbor scale against identical backdrops and canvas baselines. Client display scaling may resize the whole sheet. Vanilla wooden workbench/electronics table and copper/ancient chest references come from the existing ignored extraction folder. Native reference images and the combined sheet remain in ignored build output and are not packaged as mod art.

Only front/down review candidates exist in this pass. Side/up views, icons, emissive masks, pivot/transform integration, bundle checks and in-game review remain outstanding. No runtime art or bundles changed.
