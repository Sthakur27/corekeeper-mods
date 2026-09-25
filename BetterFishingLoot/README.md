# Better Fishing Loot

Rare fishing loot (Miner / Ninja / Diver / Rambo / Swamp Mage / Desert Guardian / Moss armor, rings,
necklaces, pouches, lanterns, tools and every Rare-or-better item) bites more often. Fish, ore, kelp
and the rest of the junk are exactly as in vanilla.

## Setting (Mod Settings menu)

| Setting | Values | Default |
|---|---|---|
| Rare loot multiplier | 1x, 2x, 3x, 5x, 10x, 25x | **5x** |

1x is vanilla. Changes apply instantly, also while you are in a world (both the client and server
world re-apply on the next tick). The value is stored in this mod's CoreLib config file.

Dependencies: CoreLib, Mod Settings Menu. `requiredOn: 3` (client + server). In multiplayer the
host's/server's setting decides the actual catch; a client with a different value only mispredicts
for a tick before the server result arrives.

## How it works

When you fish, the game first decides whether a fish or "loot" bites (base 40 % fish, modified by the
`IncreasedChanceToGetFish` / `IncreasedChanceToGetFishLoot` conditions). A fish bite rolls one of the
`*Fishes` loot tables, a loot bite rolls one of the ten `*FishingLoot` tables
(`DirtFishingLoot`, `LarvaFishingLoot`, `StoneFishingLoot`, `NatureFishingLoot`, `MoldFishingLoot`,
`SeaFishingLoot`, `DesertFishingLoot`, `LavaFishingLoot`, `CrystalFishingLoot`, `PassageFishingLoot`),
picked by the water tileset / biome. That roll is `PugDatabase.GetRandomLoot`, a plain weighted pick
over the `LootTableBank` blob, executed inside a Burst job (`PlayerState.Fishing`), so it cannot be
patched. The blob it reads is a singleton (`LootTableBankCD`) that every world holds.

`FishingLootWeightSystem` (a `PugSimulationSystemBase`, runs in the client and the server world)
therefore edits the data instead: as soon as the world's `LootTableBankCD` exists it walks the ten
`*FishingLoot` tables and sets `weight = vanilla weight x multiplier` for every rare entry, leaving all
other entries alone. Vanilla weights are remembered per table slot the first time they are seen, so
re-applying (setting change, second world, both worlds sharing one deduplicated blob) is idempotent.
The `*Fishes` tables, the fish-vs-loot split, bait, the minigame and the legendary-fish logic are not
touched; rare gear never shares a table entry with a fish, so nothing about fish had to change.

### What counts as rare

An entry is boosted if the item is equipment or the game marks it Rare, Epic or Legendary:

- object type Helm, BreastArmor, PantsArmor, Necklace, Ring, Offhand, Bag, Lantern, Pouch,
  MeleeWeapon, RangeWeapon, SummoningWeapon, any tool (Shovel .. BeamWeapon, e.g. sledges and drills),
  Pet; or
- `rarity >= Rare`.

The exact list the game resolved is printed once per multiplier to Player.log
(`[BetterFishingLoot] Applied 5x to N rare entries ...`, one line per table with every boosted item,
its rarity/type and old -> new weight, plus the table's total rare chance before and after).

Equipment in the vanilla tables (from the shipped `LootTableBank` asset):

| Table (where) | Equipment entries |
|---|---|
| DirtFishingLoot (Dirt Biome) | Cave Guppy Necklace, Diver Pants, Fish Pouch |
| LarvaFishingLoot (Clay Caves) | Rusted Ring, Diver Necklace, Tamer Necklace |
| StoneFishingLoot (Forgotten Ruins) | Bubble Pearl Necklace, Diver Ring, Trenchcoat, Grappling Hook, Moss Helm / Chest / Pants |
| NatureFishingLoot (Azeos' Wilderness) | Sea Foam Ring, Trenchcoat, Rambo Helm / Chest / Pants, Swamp Mage Helm / Chest / Pants |
| MoldFishingLoot (Mold Dungeon) | Diver Armor (chest), Plague Doctor Helm |
| SeaFishingLoot (Sunken Sea) | Diver Helm, Octarine Sledge, Tamer Pants, Pearl Lantern, Crescent Necklace, Large Fish Pouch, Pet Frog Egg, Sea Scepter |
| DesertFishingLoot (Desert of Beginnings) | Noble Ring, Double Ring, Puppet Ring, Desert Guardian Helm / Chest / Pants, Large Fish Pouch |
| LavaFishingLoot (Molten Quarry / lava) | Fusioned Chunk Necklace, **Miner Helm / Chest / Pants** |
| CrystalFishingLoot (Shimmering Frontier) | **Ninja Helm / Chest / Pants** |
| PassageFishingLoot (The Passage) | none (only ore, fossils, kelp) |

Non-equipment items with Rare+ rarity (e.g. Core Eye, Golden Starfish, Enhydro Crystal, Data Slate,
Oracle Card) are boosted too when the game flags them; the log shows which.

### The math

For a table with total weight `T` and rare weight `R`, a multiplier `m` gives
`P(rare) = m*R / (T - R + m*R)`; each rare item's chance is `m*w / (T - R + m*R)`. Per loot bite
(equipment only, exact numbers for anything the game additionally marks Rare+ are in the log):

| Table | 1x | 2x | 3x | 5x | 10x | 25x |
|---|---|---|---|---|---|---|
| DirtFishingLoot | 6.6 % | 12.3 % | 17.4 % | 26.0 % | 41.3 % | 63.7 % |
| LarvaFishingLoot | 3.0 % | 5.7 % | 8.4 % | 13.2 % | 23.4 % | 43.3 % |
| StoneFishingLoot | 15.7 % | 27.2 % | 35.9 % | 48.3 % | 65.1 % | 82.4 % |
| NatureFishingLoot | 13.6 % | 24.0 % | 32.1 % | 44.1 % | 61.2 % | 79.8 % |
| MoldFishingLoot | 4.5 % | 8.6 % | 12.4 % | 19.0 % | 32.0 % | 54.0 % |
| SeaFishingLoot | 14.5 % | 25.3 % | 33.7 % | 45.8 % | 62.8 % | 80.9 % |
| DesertFishingLoot | 9.6 % | 17.6 % | 24.2 % | 34.7 % | 51.6 % | 72.7 % |
| LavaFishingLoot | 4.2 % | 8.1 % | 11.6 % | 18.0 % | 30.5 % | 52.3 % |
| CrystalFishingLoot | 4.6 % | 8.9 % | 12.7 % | 19.6 % | 32.7 % | 54.9 % |

Examples per single piece: Miner Helm 1.05 % -> 4.5 % (5x) -> 13.1 % (25x); Ninja Helm 1.55 % ->
6.5 % (5x) -> 18.3 % (25x); Rambo Helm 1.58 % -> 5.1 % (5x). Multiply by the loot-bite chance
(about 60 % without fishing accessories) for per-cast odds.

## Limits

- Only the ten fishing loot tables are changed. Fish tables, chests, enemy drops and the fishing
  minigame are untouched.
- Rare item weights are scaled, not the number of drops, so a loot bite still yields exactly one item.
- The blob is edited in memory only; nothing is written to saves or game files. Removing the mod
  restores vanilla.
- Both worlds must run the mod (`requiredOn: 3`); the server's setting is authoritative.
