# Native size review — 2026-11-12

Review-only output. No bundle rebuild, installation, or production-art replacement was performed for this size correction.

The comparison boards show native, currently installed art, and proposed art at a shared pixel magnification. NPCs share a visible foot baseline. Teal rectangles are manually annotated body envelopes excluding projecting props; they are design judgments, not automatic anatomical segmentation. Proposed guides are proportional mappings of the original annotations.

Native prefab inspection confirms scale (1,1,1) and 16 pixels per world unit for all four merchant and all four placed summoning-item graphics. Entity roots also use unit scale. Facing mirrors X only. Spawn/hit animations temporarily squash/stretch; these comparisons describe resting size. Evidence: `../../Unity/size_review/native-scale-findings.md` and `../../../build/bossmerchants/size-review-native/native-prefab-scales.json`.

Six NPCs are reduced: Ottoline, Hemlock, Ankhra, Tridra, Wren, and Tilly. Grout and Fizzwick already occupy a native-sized envelope and retain their squat proportions. Ankhra's shell and Tridra's three heads deliberately remain wider than humanoid bodies. Staff height is not used to determine body size.

All eight spawn items were checked as placed art, full inventory icons, and small icons. Fizzwick and Hemlock change from 12x16 to 11x15 visible pixels; Tilly changes from 15x16 to 14x15. The other five already fit native item sizes. Small icons preserve aspect ratio within their configured budgets, so rounding can leave a one-pixel difference from the reference.

`measurements.json` records all before/after bounds, occupied pixel counts, world-unit dimensions, body guides, small-icon sizes, and reasons. `validation.txt` confirms all eight NPC sets and 24 item PNGs pass metadata, alpha, palette, animation-cell, floor, animation-diversity, and overlay-alignment checks.

Reproduce from the repository root:

```powershell
python -I BossMerchants/Unity/import_approved_art.py --profile BossMerchants/Unity/size_review/native_size_profile.json --out BossMerchants/ArtConcepts/NativeSizeReview-2026-11-12/sprites
python -I BossMerchants/Unity/size_review/make_comparison.py
python -I BossMerchants/Unity/validate_approved_art.py --root BossMerchants/ArtConcepts/NativeSizeReview-2026-11-12/sprites
```

Do not apply or rebuild until Sid reviews the comparisons.
