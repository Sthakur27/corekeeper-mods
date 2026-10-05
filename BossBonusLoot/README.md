# Boss Bonus Loot

Core Keeper mod: bosses drop extra items on top of their normal loot chest, and Hydras drop more gems.

| Boss | Bonus (each row rolled separately) |
|---|---|
| Ghorm the Devourer | 80 Larva Meat + 80 Golden Larva Meat (always) |
| Azeos the Sky Titan | Jungle Emerald (always), Chipped Blade 25%, Clear Gemstone 25%, Large Seed and Crop Pouch 40%, Large Critter Pouch 40% |
| Omoroth the Sea Titan | Ocean Sapphire (always), Large Fish Pouch 40%, Large Potion Pouch 40% |
| Ra-Akar the Sand Titan | Desert Ruby (always), Large Valuable Pouch 40%, Large Ore and Block Pouch 40% |
| Mimite, Orbital Turret (regular enemies) | Jungle Emerald 10%, Ocean Sapphire 10%, Desert Ruby 10% (their vanilla gem drops are replaced by these rolls) |
| Void Larva (regular enemy) | Oblivion Fragment 20% (its vanilla fragment drop is replaced by this roll) |
| The three biome Hydras (HydraBossNature, Sea, Desert) | Jungle Emerald / Ocean Sapphire / Desert Ruby amounts x2 |

Bonus items drop at the boss's body and fly to the player who landed the kill (same as vanilla
loot). No settings. Host decides in multiplayer.

## How it works (for modders)

- `BossBonusLootSystem` (server, after `PredictedSimulationSystemGroup`) watches entities with
  `BossCD` + enabled `EntityDestroyedCD` and health 0, looks the boss's `ObjectID` up in the `Bonus`
  table, rolls each row's chance, and spawns the stacks with `EntityUtility.DropNewEntity`. Each
  boss entity is handled once. Add bosses by adding rows to `Bonus`.
- `HydraGemSystem` (client + server) doubles the min/max amount of the three biome gemstone entries
  in the `HydraBossNature/Sea/Desert` loot tables inside the `LootTableBankCD` blob (vanilla ranges
  remembered, so it never stacks). Player.log lists every entry it changed.
