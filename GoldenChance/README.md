# Golden Chance

Adds separate percentage-point bonuses to golden plants and the rarity upgrade roll for bonus cooked food. Requires **CoreLib** and **Mod Options**. Included in Sid's Overhaul; install either the standalone mod or the overhaul.

Open **Options → Mod Options → Golden Chance**.

| Setting | Options | Default |
|---|---|---|
| Golden plant chance bonus | +0%, +5%, +10%, …, +100% | +0% |
| Golden food chance bonus | +0%, +5%, +10%, …, +100% | +0% |

These are additive bonuses: a 20% roll with +5% becomes 25%. Bonuses apply even with zero points in the relevant talent. +0% preserves vanilla behavior. The modified talent condition is capped at 100%; a plant roll including its base chance can exceed 100%, which simply guarantees a golden plant.

## Vanilla maximums

- **Golden plants:** 3% base plus up to 15% from Expert Gardener (3% per point, five points), for **18% total**. At full talent, +5% gives 23%; with no talent points, +5% gives 8%. Already planted seeds keep the outcome rolled when they were planted.
- **Bonus cooked-food rarity:** Master Chef gives up to **25%** (5% per point, five points). At full talent, +5% gives 30%. This roll applies only to **additional food** produced by the Cooking skill; it upgrades that food's rarity by one tier. It does not affect every cooked dish or the chance of receiving additional food. Cooking level 100's **20%** is the separate chance for additional food.

The Mod Options page includes these maximums and an additive example. Talent tooltips retain vanilla per-point values. Settings apply immediately when a character is active, and after joining a world. A talent reset is detected within half a second and the bonus is restored.

In multiplayer each player's local settings are sent through their talent-condition commands, following the existing game's replicated condition path. Install the mod and its dependencies on client and server. Live multiplayer behavior still needs an in-game check.

Works with Auto Replant: its base chance is normally 3%; enabling its base override replaces that 3%, and the Gardening talent plus this mod's bonus still add on top. Automated seeders that do not read a player's talent conditions are unaffected.

## Implementation

A Harmony postfix on the managed `SkillTalentsTable.GetConditionDataForSkillTalent` adds the selected bonus to `ChanceToGainRarePlant` or `ChanceForExtraCookedFoodToBeRare`, including zero-point talents. The existing `SkillTalentConditionsBuffer` → `SummarizedConditionsBuffer` path supplies the game's Burst rolls. No Burst systems are patched and no player component types are added.

The local player re-sends golden conditions on joining, settings changes, and talent-point changes. Every refresh starts from the raw talent table, so bonuses do not accumulate. New additive config keys deliberately leave old multiplier settings unused.

## 2.0.0

Replaces the old 1x–3x talent multipliers with separate +0%–+100% bonuses, defaults to vanilla odds, explains vanilla maximums in the settings menu, and restores bonuses after talent resets.
