# Native tier candidates — 2026-11-12

Nine front/down units and eight upgrade gems, each an actual 16x16 RGBA PNG with hard alpha. No Copper upgrade gem exists. Files are `<Tier>Chest-16.png` and `<Tier>Gem-16.png`; `tier-lineup.png` shows 8x nearest-neighbor enlargement plus 1x file-pixel views. `measurements.json` records pixel/color checks.

## Direction and evidence

The written parent-authored progression in `../tier-direction.md` preceded these candidates. Material authority is `../color-audit/native-palettes.json`, freshly extracted from native bars/ores/chests. Main corrections: Tin is pewter/taupe, Iron cool steel, Octarine purple/rose with cobalt edge, Galaxite charcoal with sparse iridescence, Pandorium green with violet shadow, and Relucite violet with gold/orange mounting. Neutral stone remains common across the family.

The native Octarine candidate from `../../NativeReview-2026-11-12/OctarineChest-16.png` provides the original shared housing geometry. Its source was built-in imagegen, recorded by that review's prompts and conversion profile. This pass extends the repository's existing palette-keyed native art workflow, preserving the exact common housing while changing semantic material ramps and one compact rune/structural treatment per tier. `make_candidates.py` is the reproducible source; no new imagegen call was used for these deterministic native variants. Gem silhouettes are original pixel geometry based on the liked concept board's squat, squared, diamond and cluster identities, not extracted vanilla shapes.

## Progression and critique

- Copper/Tin deliberately omit a bright top rim and gold stud, using plain brackets and quiet marks.
- Iron has one reinforcing band, Scarlet an angular channel; Octarine retains the liked stepped-rune architecture with corrected purple/blue colors.
- Galaxite uses a dark assembly and sparse native-color inclusions. Its gem needed a restrained steel-gray facet to remain visible on a dark background; avoid adding more speckle.
- Solarite introduces a bright edge and controlled luminous inset. Pandorium interlocks its green corners and uses purple recesses. Relucite integrates a gold bezel, violet seal and balanced high-tier corner highlights.
- The whole set remains recognizable as one family. At 16x16, the motifs are deliberately small; the neutral top plate still occupies much of each unit. Solarite is naturally brighter than Relucite due to native material identity, so final perceived rank remains a user-review question, not an asserted result.
- Gem mounting and facets are constrained by the 16x16 budget; these native candidates simplify the large approved board and need user aesthetic review. Galaxite is intentionally near-black and Pandorium intentionally deep green, but both must remain readable in the game lighting.

## Reproduce and limits

From repository root: `python ConnectedStorage/ArtConcepts/TierRefresh-2026-11-12/tiers/make_candidates.py`.

Validated all 17 sprites as 16x16 with alpha only 0/255 and inspected the ordered lineup. Color identity is grounded in source textures before game lighting. Review-only front/down sprites: no side/up variants, emissive masks, icon differentiation, pivots, transforms, bundled pixels or in-game appearance validated. No production scripts/art/bundles/install changed.
