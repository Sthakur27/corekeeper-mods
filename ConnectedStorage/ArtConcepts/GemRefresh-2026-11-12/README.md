# Runic Storage concept review — 2026-11-12

## User feedback and native-size status

Sid explicitly requires matching the native slightly angled top-down view so assets do not look out of place. Use native elevated top/front projection: visible top plus shallow front face, not flat overhead or diagonally yawed isometric. Compare candidates with native references at equal pixel magnification, checking top/front proportions, footprint, occupied pixel bounds and ground alignment before integration. Large concept-board resemblance is insufficient.

Sid said "the gems look good tho": preserve the gem board's colors, silhouettes and mounts in subsequent native-size cleanup. This approves gem visual direction; the board itself is not a runtime sprite.

Workbench V2 is a 1254x1254 concept board, not native pixel art. Extracted native workbenchWood down/up and electronicsTable down sprites are 16x16; workbenchWood side is 16x20. V2 corrects yaw/roll to the native front-facing top-visible orientation, but exact top/front proportions, occupied pixels, pivots and 16x16 readability remain unvalidated. Do not describe its angle or pixel density as a proven exact match.

## Workbench perspective correction

Sid liked the workbench design but rejected its diagonal camera angle. `workbench-concept-v2.png` corrects both renditions to front-facing, axis-aligned top/front projection using the native workbench/electronics-table reference board. Materials and props are retained. Generated with built-in image_gen; exact edit prompt in `workbench-v2-prompt.txt`. V1 remains for comparison. V2 is still a concept, with native-resolution cleanup and proportional review outstanding; no production replacement.

Generated with built-in image_gen. Exact prompts and input references: `prompts.json`.

- `gems-concept-v1.png`: all eight upgrades preserve established colors and mounted-crystal identity, with simplified facets and individual socket accents.
- `workbench-concept-v1.png`: stone worktop, copper braces, crystal-setting socket and hammer; complements the existing terminal.

User likes the terminal and overall upgrade progression, wants more individual tier flavor, and dislikes the existing workbench. These boards are review concepts only. No runtime art, bundles or installed files were replaced. Storage-unit accent redesign remains a possible later pass; no unit candidate is included here.

Review: gem silhouettes and tier colors read clearly; heavy near-black outlines and residual fine shading need cleanup. Workbench has clearer purpose and top/front depth, but the generated small rendition is not a certified 16x16 sprite; its hammer/socket and front rune must survive native-size simplification. Neither board demonstrates exact pixel grids or validated game scale.

After direction approval, prepare native-size sprites with hard alpha, compare occupied pixels against native references, inspect full/small icons and dropped sprites, then rebuild and verify packed pixels before installation. Preserve source concepts. Do not simply package these large boards or assume point filtering makes them native pixel art.
