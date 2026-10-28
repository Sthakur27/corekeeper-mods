# Runic Storage art exploration: shared brief

Core Keeper mod "Runic Storage": connected storage like Terraria's Magic Storage. We need three
placed objects that look like they ship with the vanilla game. You draw ONE theme (given in your
task) and produce three sprites:

| File | Object | What it is |
|---|---|---|
| `chest.png` | Storage Unit (base/copper tier) | A storage container. Later tiers will be recolours of one feature (e.g. a gem/core/accent), so give it ONE clear tier-colour feature; for this base tier use copper orange for it. |
| `terminal.png` | Storage Terminal | The access point for the whole network (search/withdraw). Should read as "the brain/heart" of the storage family. |
| `workstation.png` | Storage Workbench | 1x1 crafting station where units, terminal and upgrade gems are made. Must read as a crafting bench, distinct from the chest. |

All three: front-facing (vanilla "down") view, **exactly 16x16 px, RGBA, hard alpha (0 or 255 only)**,
one texel = one game pixel. Same family: shared materials/palette so they look like a set.

## References (study these first; Read the PNGs, they are images)
Folder: `C:\Users\Sid\CoreKeeperMods\build\artwork-review\2026-10-28\style-refs\`
- `chests_sheet.png`, `stations_sheet.png`: vanilla chests and crafting stations at 8x with 1x beside.
- `ingame_scale_3x.png`: how 16 px objects read side by side.
- `pixel_grids.txt`: exact pixels + hex palettes of 15 key sprites (chests, workbenches, electronics table, anvil, automation table, furnace...).
- `png/`: the individual vanilla sprites.

Vanilla conventions to match (verify against the refs yourself):
- Object fills the tile: 16 px wide, top surface seen from above (roughly the top 8-10 rows), then the
  front face; benches end in a dark apron row and 3-4 rows of legs with gaps; chests sit on a base row.
- Coloured dark outline per material (dark teal/brown/navy), not black. Light from top-left.
- 3-5 shades per material, 1 px highlights on top edges and corners, small 1-2 px detail clusters
  (rivets, grain, tools), slight mottled wear rather than flat fills or noise.
- Glowing parts (gems, screens, runes) are a few bright pixels with a ramp, not big flat blobs.

## Rules
- Original pixels: use vanilla for style, proportions and palette relationships. You may reuse vanilla
  hex colours, but do NOT copy or trace a vanilla sprite's pixel layout.
- Work only in your own output folder `C:\Users\Sid\CoreKeeperMods\build\artwork-review\2026-10-28\styles\<theme>\`.
  Do not edit any other file in the repo, the game folder, or the Unity SDK. Do not launch the game.
- Tooling: Python 3 + Pillow (installed; numpy is not). Write `make.py` in your folder that draws the
  sprites from palette-keyed string grids (one char per pixel + a palette dict), like
  `C:\Users\Sid\CoreKeeperMods\ConnectedStorage\Unity\make_native_art.py` (read it for the pattern).
- Also write `emissive_chest.png` / `emissive_terminal.png` / `emissive_workstation.png` (16x16, glowing pixels only, rest transparent).

## Process (required)
1. Study the references. Note the vanilla outline colours, shade counts and layout rows you will follow.
2. Draw all three. Render `review.png` in your folder: your three sprites at 8x next to 3-4 of the most
   relevant vanilla refs at 8x, plus a 1x and 3x row mixing yours with vanilla on a dark (#1d2026) background.
3. Read `review.png` and critique it honestly against vanilla: does it look native at 1x? silhouette,
   outline, shading, readability of the tier feature, chest vs bench distinct? Revise. Do at least two
   critique/revise rounds; stop when another round would not clearly improve it.
4. Report back (short): theme summary in one sentence per object, palette (hex), what you changed
   across rounds, and remaining weaknesses. Confirm the three files are 16x16 RGBA with hard alpha.
