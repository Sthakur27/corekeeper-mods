# Boss Merchants: character designer brief (shared by all 8 designers)

Mod: Core Keeper "Boss Merchants" (C:\Users\Sid\CoreKeeperMods\BossMerchants). Each merchant is a NEW NPC that moves
into a player's base when its summoning idol (dropped by a boss) is placed in a room with a free bed, exactly like
vanilla merchants (Caveling Merchant / Mysterious Idol, Slime Merchant / Slime Oil, Fishing Merchant / Pile of Chum,
Void Merchant / Void Merchant Idol, Crystal Merchant, Seasonal Merchant).

You own ONE merchant. You design a brand-new character: name, personality, look, and its summoning idol, themed
on what it sells and the boss that drops the idol. Sid (the mod author) rejected the first alpha pass because:
1. the merchants were recolours of existing merchants. He wants NEW characters, not palette swaps.
2. the idols did not match the game's item aesthetic. They must look like they shipped with Core Keeper.

## Study first (mandatory)

- Look at EVERY vanilla merchant and EVERY vanilla merchant idol/summoning item before drawing: Caveling,
  Slime, Fishing, Void, Crystal, Seasonal merchants and Mysterious Idol, Slime Oil, Pile of Chum, Void
  Merchant Idol, Crystal Merchant Spawn Item, Calendar. Already extracted (art reference only, never shipped):
  - C:\Users\Sid\CoreKeeperMods\build\bossmerchants\reference (merchant sheets + reference.json with rects/pivots)
  - C:\Users\Sid\CoreKeeperMods\build\bossmerchants\idol_reference (idols + reference.json with sizes/pivots)
  - BossMerchants\Unity\Art\merchants\skins.json: the frame layout of the four rig merchants (see below).
  Missing references (Crystal, Seasonal merchants, boss/biome items for your theme)? Extract them yourself with
  UnityPy, following BossMerchants\Unity\extract_reference.py, into
  C:\Users\Sid\CoreKeeperMods\build\bossmerchants\ref_<yourkey> (game bundles in C:\Program Files (x86)\Steam\
  steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64; run Python with `python -I`).
- Also study 10+ vanilla item icons from your theme (what the merchant sells, the boss's trophy/drops) to match
  item-icon conventions: 16 px grid, light from top-left, 3-5 step colour ramps per material, a bright specular
  pixel or two, dark coloured outline (never pure black; Core Keeper has no black outlines), hard alpha, no
  anti-aliasing, chunky readable silhouette at 1x. Compare your idol side by side with vanilla idols at 1x
  and 4x; if it doesn't look like it belongs in the same set, iterate.

## Hard technical constraint (merchants)

The NPC reuses a vanilla merchant's animation rig, so your sheets MUST keep that rig's exact sheet size, frame
grid (cell rects), frame count and pivots. Inside each frame cell the drawing is entirely yours (new silhouette,
new character); only the cell layout is fixed. Pick the rig that fits your character's body and size:
- SlimeMerchant rig: texture `Slime_merchant` 300x120, 30x30 cells (legacy SpriteRenderer + Animator).
- CavelingMerchant rig: `Merchant` + `Merchant_Light` + `Merchant_Eye` (legacy; the light/eye sheets are glow overlays).
- FishingMerchant rig: `Fisher_merchant` (legacy; its glow `Fisher_merchant_light` lives in a shared atlas and
  cannot be replaced, so the character must look right with that small vanilla glow on, or avoid the glow area).
- VoidMerchant rig: SpriteAsset `voidMerchant` with idle (128x35, 8 frames), move (96x34, 6) and emote (621x37,
  27) sheets (skinned via SpriteAssetSkin).
Every frame must animate sensibly (idle bob, walk, wave/emote per the vanilla frame order). Keep the character
inside each cell with the same ground line/pivot as vanilla so it stands on the floor correctly.
Two designers may pick the same rig; that is fine as long as the characters look different.

## Idol

16x16 like the vanilla idols, pivot centre (0.5, 0.5); icon sits on the bottom edge. Deliver icon (16x16),
smallIcon (the vanilla smallIcons are about 10x11 centred on 16x16), and placed (the in-world sprite; vanilla
uses the icon pixels for it). It should read as an object you'd place in a room to attract that merchant.

## Deliverables (only write inside your own paths, never another designer's)

- `BossMerchants\Unity\Art\merchants\<key>\`: your sheets, named `<key>__<vanilla texture name>.png`, and
  `sheets.json` = {"key", "rig" (vanilla prefab e.g. "SlimeMerchantEntity"), "kind" ("legacy" or "spriteAsset"),
  "textures": [ same entry schema as that rig's entries in Art\merchants\skins.json, with "output" pointing to your file ]}.
- `BossMerchants\Unity\Art\idols\<key>_icon.png`, `<key>_smallIcon.png`, `<key>_placed.png`.
- `BossMerchants\Unity\make_<key>_art.py`: deterministic (`python -I`), draws everything from code / hand-authored
  pixel grids (no copying vanilla pixels), with a validation step: sheet sizes equal the rig's, pixels only inside
  cells, hard alpha, idol sizes 16x16. Exit 1 on failure.
- `BossMerchants\docs\designs\<key>.json`: {"key", "merchantName", "merchantBlurb" (one in-game line),
  "idolName", "idolDescription" (one line, mention the boss), "boss" (ObjectID name), "rig", "stock": [list of
  ObjectID names, max 24, with "price" coins (multiple of 5) and optional "stock" per restock]}.
- `BossMerchants\Unity\Art\previews\<key>.png`: a review sheet for Sid: your character's idle frame at 6x next to
  the vanilla merchants at the same scale; the walk + emote frames in a strip; your idol icon at 1x and 8x beside
  the vanilla idols; the idol placed on a dark stone floor tile mock.
- Final report (under 250 words): name and concept, rig and why, boss, stock summary, anything you could not do,
  and the absolute preview path. Don't touch Scripts/, gen_assets.py, the builder, or other keys' files.

Existing code (for awareness only, don't edit): Scripts\Catalog.cs (current stock lists), Scripts\Visuals.cs (runtime
swap: each vanilla sprite's rect is cut from our sheet of the same name, so layout must match).
ObjectID names: see the game's ObjectID enum (dump: C:\Users\Sid\AppData\Local\Temp\claude\9baedba92acb1708\
2af2000c-41a7-45b9-9f02-1b97ee309256\scratchpad\objectids_u.txt, "Name=Number" per line).
