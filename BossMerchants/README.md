# More NPCs (Alpha)

Eight new merchants, each tied to a boss. Defeat the boss and it drops an idol; place the idol in a room with a free bed and its merchant moves in, just like the vanilla merchants.

| Boss | Idol | Merchant | Sells |
|---|---|---|---|
| Morpha the Aquatic Mass | Slick Cornerstone | Grout | 24 wall block types from every biome (5–30 coins, 99 per restock) |
| Omoroth the Sea Titan | Omoroth Shell Lamp | Ottoline Tidewick | Candles, lanterns, lamps, pots, rugs, banners, a painting, tables and chairs (25–250 coins, 10 per restock) |
| Igneous the Molten Mass | Igneous Fuse Core | Fizzwick, the Molten Bombardier | Every grenade and bomb (10–100 coins) |
| Ivy the Poisonous Mass | Ivy Culture Jar | Old Hemlock | Every potion, the Recall Idol and the food and drinks both vending machines dispense: Cave Coffee, sodas and snacks (40–500 coins; 2,500 for the Potion of Return) |
| Ra-Akar the Sand Titan | Brood Egg Idol | Ankhra the Clutch-Keeper | Pet eggs (500–5,000 coins, 1–3 per restock) and cooking eggs |
| Druidra, Crydra or Pyrdra (any of the three hydras) | Triune Idol | Tridra the Haggler | Hydra baits, hydra crafting drops, gemstones, scanners, trophies, hydra jewellery and the Hydra Workbench (55–2,750 coins, 1–50 per restock) |
| Azeos the Sky Titan | Skyherd Bell | Wren Hayfeather | Baby farm animals, cattle cages, leashes, feed trays, saddles, animal food, fences and gates, straw blocks, milk and wool (5–970 coins) |
| Hive Mother | Brood Seedpot | Tilly the Seedgrub | Every crop seed, planter boxes, watering cans, a seeder, the seed extractor, sprinklers and seed pouches (5–2,500 coins) |

## How it works

- Every kill of one of these bosses drops one idol at the boss, so a lost idol is one fight away.
- The room rules are the vanilla ones: an enclosed room, an unclaimed bed and the idol placed inside. One of each merchant per world.
- If a merchant's bed is destroyed, it moves to another free bed in a room with its idol, like the vanilla merchants.
- When you install Sid's Potion of Return mod, Old Hemlock sells that potion too.

## Settings

Open **Settings > Mod Options > More NPCs**.

- **Price multiplier**: 1x by default, 0.25x to 4x. Scales every buy price this mod sets.
- **Potions and explosives per restock**: 20 by default, 1–99 (also Old Hemlock's vending snacks and drinks). Every other item has a fixed amount.

Items sold here have fixed sell prices of one fifth of their 1x buy price, so nothing can be resold for a profit. These prices apply everywhere, so the vending machines charge Old Hemlock's prices for their food and drinks too. Their other items keep vanilla prices.

## Alpha status and limits

- Every merchant and idol uses the approved Hemlock-style art direction: overhead shapes, compact silhouettes and shaded pixel clusters. Concepts and transparent source art were made with AI image generation, then imported into native-size, hard-alpha sprites with a limited palette.
- Animations use the vanilla clip timing with subtle idle breathing, walking sway, greeting nods and slime arrival reveals. These are modest pixel motions rather than individually redrawn articulated poses. Ottoline's misplaced vanilla lantern overlay is hidden while her graphic is in use and restored when it returns to the pool.
- Not yet tested in a world or in multiplayer.
- The shop window shows 24 items (8x3). A merchant with more wares (Old Hemlock) scrolls: use the mouse wheel over the window. Scrolling needs a mouse; controllers only see the first 24.

## Requirements

Requires **CoreLib** and **Mod Options** on the host/server and every client.

## Development

- `Scripts/`: catalog and prices (`Catalog.cs`), shop list (`MerchantStock.cs`), idol drops, merchant spawning, stock and price systems, runtime art swap (`Visuals.cs`), shared 8x3 buy-window layout.
- `Unity/Art/approved_sources/`: generated source cutouts and exact prompts. `import_approved_art.py` builds native-size sprites and inspection previews under `Art/approved_build`; add `--apply` to copy validated output to `Art/merchants`, `Art/idols`, and `Art/final`. `validate_approved_art.py` independently checks native layouts, transparency, palettes, ground contact, animation variation and overlay alignment.
- `gen_assets.py` + `Editor/Builder` create prefabs, text and skins; `build_bundle.py` builds Windows/Linux bundles with Unity 6000.0.59f2 and the Mod SDK; `verify_bundle.py` checks them. Ankhra retains her vanilla animation GUIDs/counts with wider 32px replacement frames and explicit bottom-centre pivots.
- `update_combined_art.py INPUT OUTPUT` updates only the 42 More NPCs textures and Ankhra's skin pivots in an existing Overhaul bundle. It verifies exact source pixels and that all unrelated serialized objects/resource streams are unchanged before writing the output.
- The older `make_<key>_art.py` scripts and `finish_art.py` document the previous procedural art pipeline; do not run them over this approved pass. Native reference extraction remains in `dump_vanilla.py`, `extract_reference.py` and `extract_idol_reference.py`.
