# Verified native material colors — 2026-11-12

`native-palettes.json` records native bar/ore/chest evidence for all nine storage tiers. `audit.py` reproduces the extraction from the installed game's `defaultlocalgroup_assets_all.bundle`, verifies every suggested main/accent hex occurs in the corresponding native bar sprite, and makes an equal-scale evidence board. No textures were inferred from material names or web images.

Run from the repository root:

`python ConnectedStorage/ArtConcepts/TierRefresh-2026-11-12/color-audit/audit.py`

Reference PNGs and comparison board are written only to ignored `build/connectedstorage/color-audit-20261112/`. They contain game art and must never be packaged or published as mod artwork.

## Important corrections

| Tier | Verified native identity |
|---|---|
| Copper | Salmon/orange copper |
| Tin | Muted taupe/lavender pewter, not icy blue |
| Iron | Cool blue-gray polished steel |
| Scarlet | Red/coral with pink highlights |
| Octarine | Purple/rose top with vivid cobalt-blue bevels; cyan is only a tiny accent |
| Galaxite | Near-black iridescent metal with tiny multicolor inclusions, not solid purple |
| Solarite | Golden orange with cyan/mint glints |
| Pandorium | Green/emerald with purple shadows, not cobalt blue |
| Relucite | Violet paired with warm orange/gold; magenta is not its primary identity |

These are source sprite colors before game lighting. Main/accent grouping and proposed unit motifs are design judgments. Upgrade gems are original mod objects, so the goal is to carry the material's visual identity into a new crystal/socket design, not claim that a matching vanilla gem exists. Native bars and ores were found for all nine tiers. Exact `chestTin` and `chestPandorium` textures were not found in the inspected bundle; use their bar/ore evidence rather than inventing a chest reference.

No runtime artwork, installed files, bundles, game state or publishing state was changed.
