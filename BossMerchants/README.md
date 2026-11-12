# Boss Merchants (Alpha)

Eight new merchants, each tied to a boss. Defeat the boss and it drops an idol; place the idol in a room with a free bed and its merchant moves in, just like the vanilla merchants.

| Boss | Idol | Merchant | Sells |
|---|---|---|---|
| Morpha the Aquatic Mass | Slick Cornerstone | Grout | 24 wall block types from every biome (5–30 coins, 99 per restock) |
| Omoroth the Sea Titan | Omoroth Shell Lamp | Ottoline Tidewick | Candles, lanterns, lamps, pots, rugs, banners, a painting, tables and chairs (25–250 coins, 10 per restock) |
| Igneous the Molten Mass | Igneous Fuse Core | Fizzwick, the Molten Bombardier | Every grenade and bomb (10–100 coins) |
| Ivy the Poisonous Mass | Ivy Culture Jar | Old Hemlock | Every potion and the Recall Idol (50–500 coins) |
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

Open **Settings > Mod Options > Boss Merchants**.

- **Price multiplier**: 1x by default, 0.25x to 4x. Scales every buy price this mod sets.
- **Potions and explosives per restock**: 20 by default, 1–99. Every other merchant has fixed amounts.

Items sold here have fixed sell prices of one fifth of their 1x buy price, so nothing can be resold for a profit.

## Alpha status and limits

- Every merchant and idol has its own pixel art, drawn over the vanilla merchant animations. Ottoline's lantern glow still uses the vanilla colours.
- Not yet tested in a world or in multiplayer.
- Each merchant shows up to 24 items (8x3 shop).

## Requirements

Requires **CoreLib** and **Mod Options** on the host/server and every client.

## Development

- `Scripts/`: catalog and prices (`Catalog.cs`), shop list (`MerchantStock.cs`), idol drops, merchant spawning, stock and price systems, runtime art swap (`Visuals.cs`), shared 8x3 buy-window layout.
- `Unity/`: `dump_vanilla.py` (vanilla merchant/idol prefabs → JSON), `extract_reference.py` / `extract_idol_reference.py` + `make_<key>_art.py` and `finish_art.py` (merchant sheets and idols in `Art/final`, designs in `docs/designs`), `gen_assets.py` + `Editor/Builder` (prefabs, text, skins) and `build_bundle.py` (Unity 6000.0.59f2 + Mod SDK) followed by `verify_bundle.py`.
