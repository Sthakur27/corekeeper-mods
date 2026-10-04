# Sid's Core Keeper mods

## Download (for friends)

**[Download the latest Sid's Overhaul](https://github.com/Sthakur27/corekeeper-mods/releases/latest)**: one zip with all of
Sid's mods. Install steps are on the release page and in the zip's `README.txt`. In short, copy the
`SidsOverhaul` folder into `Core Keeper/CoreKeeper_Data/StreamingAssets/Mods/`, then subscribe to
**CoreLib** in the in-game mod browser. Settings are under Settings > Mod Options.

## The mods

Each folder is one mod (also published separately on [mod.io](https://mod.io/g/corekeeper)):

| Mod | What it does |
|---|---|
| LoadoutSharing (Loadout Fallback) | Empty loadout slots waterfall to the next lower loadout; per-loadout vanity outfits |
| FiveLoadouts | Two extra loadout tabs |
| SkillXPMultiplier | XP multiplier per skill |
| FastAutoFishing | Auto fishing without the waiting (fork of mikufish, GPL-3.0) |
| DurabilityMultiplier | Gear wears out slower |
| BuffDurationFloor | Short food/potion buffs last at least a few minutes |
| FasterMushrooms | Faster mycelium spread, wild mushrooms respawn near you |
| BiggerWateringCans | Watering cans water 2x2 / 4x4 |
| BetterFishingLoot | Rare fishing loot bites more often |
| AutoReplant | Harvesting replants from your seeds |
| QuickBuff | One key eats/drinks every buff item |
| PotionSeller | Caveling Merchant sells potions + Recall Idols, Slime Merchant sells explosives |
| HardModeTuning | Hard mode: regular enemies 1.5x damage instead of 2x, bosses untouched |
| KeepMinions | Minions survive teleports |
| PetEditor | Pet level, any talent in any slot, color |
| BossBonusLoot | Bosses drop guaranteed extras |
| EnderStash | Personal 40-slot stash from any chest, same in every world, kept on death |
| ModOptions | The Mod Options settings menu (library used by the other mods; built into the overhaul) |
| InfiniteOreBoulders | Ore boulders never break |

`Overhaul/` + `release/build_overhaul.py` combine them into Sid's Overhaul. Notes for modders are in
`AGENTS.md`.
