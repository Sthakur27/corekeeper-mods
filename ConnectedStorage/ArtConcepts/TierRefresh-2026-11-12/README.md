# Combined tier iteration — 2026-11-12

Sid requested agent fan-out to remove the workbench hammer, correct material colors, give each storage tier a coherent identity, then review actual native-size candidates together. Sid additionally required a written progression before art so early tiers do not look more majestic than late tiers.

## Direction and evidence

- `tier-direction.md`: parent-authored concept progression, supplied to the artist before tier changes.
- `color-audit/native-palettes.json`: native bar/ore and available chest evidence, freshly extracted from installed game assets. Palettes are source-sprite colors before lighting, not a claim about final in-game appearance.
- `tiers/README.md`: artist's reproduction and review notes for nine units/eight upgrade gems.
- `../WorkbenchRefresh-2026-11-12/README.md`: hammer-free workbench revision.

Key corrected identities: Tin taupe/lavender pewter, Octarine purple/rose with cobalt, Galaxite charcoal with sparse colored flecks, Pandorium green with purple shadows, Relucite violet with orange/gold. Unit accents and gems share each tier's material identity. The workbench crystal remains a general teal crafting accent; it is not labeled as Octarine.

## Combined review

Run `python ConnectedStorage/ArtConcepts/TierRefresh-2026-11-12/make_combined_review.py` from repository root. Output: `build/connectedstorage/combined-review-20261112/combined-review.png`. This board stays in ignored build output because it includes native references. Candidates are 16x16 RGBA, shown at 8x nearest-neighbor and 1x; native material bars are explicitly labeled 4x. Workbench comparisons are all 8x plus 1x. Client display scaling can resize the entire board.

Validated all 18 candidates for actual 16x16 size and hard alpha; details in `combined-validation.json`. Parent visually inspected the finished combined sheet. Separate audit, tier-design and workbench agents contributed; parent defined progression and assembled/reviewed the results.

## Remaining review judgments

- Matching the game's material identities is a clear improvement over the earlier invented palette assignments.
- Hammer removal creates calmer workbench space while preserving the crystal socket and supporting structure.
- The common housing remains very consistent; tier distinctions are restrained color/rune/mount changes at 16x16, not radically different structures.
- Solarite naturally commands attention with its golden palette. Relucite has integrated violet/gold accents, but whether it feels sufficiently above Solarite is still a subjective review question. Do not claim the progression is aesthetically approved yet.
- Galaxite was revised once to preserve charcoal identity while separating facets against a dark background. It remains intentionally subdued; check it under game lighting later.
- Gem facets are necessarily simpler at native size than on the earlier approved large board.

No production art, manifest, bundle or installed mod changed. Only front/down review candidates exist; side/up variants, icons/emissive masks, runtime sizing/pivots, bundle verification and in-game review remain before release.
