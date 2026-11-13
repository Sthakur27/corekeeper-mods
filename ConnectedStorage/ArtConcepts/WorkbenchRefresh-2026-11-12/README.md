# Workbench revision — hammer removed

User requested removal of the unnecessary hammer. `Workbench-16.png` is the revised actual 16×16 RGBA front/down candidate. The copper braces, gray-blue stone tabletop, crystal-setting socket and wooden base remain. The right half is an uncluttered work area rather than another added prop.

`before-after.png` shows the previous and revised candidates at 1× and exact 12× nearest-neighbor enlargement. Final inspection: hammer and its dark shadow are removed; the crystal socket stays readable and becomes the sole tabletop focal point. The result is intentionally quieter. Whether the empty working area needs any subtle material accent is an aesthetic review choice, not a technical defect.

Built-in image_gen performed the source edit (`source-no-hammer.png`); exact prompt is `prompt.txt`. `make_native.py` deterministically applies the same crop/16×16 normalization and material palette used by the previous native review, with nearest-neighbor sampling, no dithering and alpha threshold 240. `measurements.json` verifies 16 visible colors, 216 opaque pixels and alpha values only 0/255.

No runtime sprite, bundle, installed file or game state changed. This is a front/down review candidate only. Directional views, pivots, emissive masks and in-game integration remain outside this pass.
