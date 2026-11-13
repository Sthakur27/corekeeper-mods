# Workbench tabletop alternatives — iteration 2

User liked the structure but rejected the teal blob. These two **actual 16×16** front/down alternatives remove it and keep the hammer absent.

- `Workbench-A-16.png`: neutral recessed assembly setting. Quieter, with a pale upper rim and dark interior. The generated amber speck does not survive native sampling; the final candidate relies on physical recessed geometry rather than a colored blob. It can also read as a recess/tray, so function needs user review.
- `Workbench-B-16.png`: inset rectangular assembly plate with two copper retaining rails. More directly connected to the bench's copper structure and more legible at 1×; my preferred option for review.
- `before-after.png`: old top and A/B at 1× and exact nearest-neighbor 12×.

Built-in image_gen edited the original source twice. Unmodified sources and exact prompts are retained. `make_native.py` uses the previous source normalization and a restricted neutral/copper palette, then composites **only** the inner tabletop mask x=3..12, y=3..8 onto the existing native candidate. Option A additionally normalizes its generated inset crop to the intended 5×4 pixel footprint because whole-object reduction collapsed its rim.

Assertions verify all pixels outside the mask are byte-for-byte unchanged and the full alpha channel is unchanged. The silhouette, braces, wood front, legs and ground alignment are therefore exact. A changes 27 inner pixels and uses 13 visible colors; B changes 29 and uses 12. Both have hard alpha. Measurements and changed coordinates are in `measurements.json`.

These are review candidates only, not runtime replacements. No production sprites, bundles or installed assets changed.
