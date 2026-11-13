# Iteration 2: fitted top details

Sid liked Iron, Galaxite and Relucite units, also liked Pandorium's top, and approved the upgrade gems. Those four complete units and all eight gems are copied byte-for-byte from `../tiers/`. Only the tops of Copper, Tin, Scarlet, Octarine and Solarite change.

## Revised construction

- Copper: plain small metal plate, upper lip and dark lower seam; practical starter hardware with no luminous mark.
- Tin: fitted pewter socket rim with a recessed dark center.
- Scarlet: beveled red inset with a shaded inner plane and contact edge.
- Octarine: rose-purple lens in a dark fitting with cobalt lower bevel, using the native material identity.
- Solarite: compact golden frame around a luminous inset, with a pale upper highlight and a cool inner glint.

The purpose is to make these read as things physically fitted into the top plane, rather than unexplained floating colored blocks. Each uses the same upper-left light direction and lower contact shadow. Shapes stay deliberately quiet at 16x16; Copper has the simplest surface, while Solarite has an integrated multi-material inset. The approved unit architecture and all lower halves remain unchanged.

## Deliverables and checks

`before-after.png` compares the five affected units at exact 10x nearest-neighbor enlargement and 1x file-pixel size. `unit-lineup.png` shows all nine units in order at 8x plus 1x. Candidate filenames remain `<Tier>Chest-16.png` and `<Tier>Gem-16.png`, so the parent can compose the combined review.

`make_iteration.py` extends the existing code-native generator as requested. It reproduces the patch grids and performs validation; no new image-generation call was needed. Every changed pixel is within x=5..10, y=3..7. Copper/Scarlet/Octarine change 10 pixels each, Tin 13 and Solarite 14. Alpha and bottom-half pixels are identical to the earlier version. `metrics.json` records all change coordinates and SHA-256 comparisons proving the four locked units and eight gems remain byte-identical.

Visual review: the five revised surfaces read more clearly as small fitted objects with rim/highlight and shadow cues on the enlarged board. They may read as raised or recessed fittings; these few pixels do not prove physical depth. At 1x they remain compact accents, not literal detailed mechanisms. The interpretation of each fitting and the overall hierarchy are still aesthetic review questions. Actual images are 16x16 hard-alpha front/down candidates, not complete in-game validated assets. No runtime art, scripts, bundles or installed mod files changed.

Reproduce from repo root:

`python ConnectedStorage/ArtConcepts/TierRefresh-2026-11-12/iteration-2/make_iteration.py`
