# Sid's Overhaul

All of Sid's Core Keeper mods in one mod, so a multiplayer group only has to install one thing.

Included features (each is also available as its own mod on mod.io):

- **Loadout Fallback** and **Five Loadouts**: empty slots waterfall down to the next lower loadout; per-loadout vanity outfits; two extra loadout tabs.
- **Skill XP Multiplier**: XP multiplier per skill (`/xp` command).
- **Fast Auto Fishing**: auto fishing with no wait between bites.
- **Durability Multiplier**: tools, weapons and armor wear out slower (default 0.5x).
- **Buff Duration Floor**: short food and potion buffs last at least 3 minutes (healing separately configurable).
- **Faster Mushrooms**: mycelium spreads faster; picked wild mushrooms respawn near you.
- **Bigger Watering Cans**: watering cans water 2x2 or 4x4.
- **Better Fishing Loot**: rare fishing loot drops more often.
- **Auto Replant**: harvesting a crop replants it from a seed in your inventory.
- **Quick Buff**: one key (default B) eats or drinks one of every buff food and potion; F does the same for buff food only; H drinks the first healing potion. Pouches included.
- **Potion Seller**: the Caveling Merchant sells every potion and Recall Idols; the Slime Merchant sells every grenade and bomb.
- **Difficulty Tuning** (was Hard Mode Tuning): separate enemy damage, health, speed and boss settings for hard mode worlds and normal worlds. Defaults: hard mode regular enemies hit for 1.5x normal damage instead of 2x; normal worlds vanilla.
- **Keep Minions On Teleport**: summoned minions survive teleports and arrive with you.
- **Pet Editor**: set your pet's level and put any talent in any slot from the pet talent window.
- **Boss Bonus Loot**: bosses drop guaranteed extras (Ghorm: larva meat; Titans: their biome gem and large pouches; Azeos: chance of Chipped Blade and Clear Gemstone; Hydras: double gems; Mimites and Orbital Turrets: 10% each biome gem).
- **Ender Stash**: a personal 40-slot stash per character, opened from any chest (purple button under Sort); same stash in every chest and world, never dropped on death.
- **Vehicle Speed**: boat and go-kart speed multipliers (1x, 2x, 3x, 5x, 10x). Replaces the Boat Turbo mod; disable that one.
- **Golden Chance**: multiplies what your golden plant (Gardening) and golden cooking (Cooking) talents give (1x, 1.5x, 2x, 3x; default 2x). No talent points = vanilla odds.
- **Infinite Ore Boulders**: ore boulders never break; keep mining for ore forever.
- **Stim Hits**: crisp metallic hit feedback: a ting when you hit, a coin chime when something you hit dies, a clang when you get hit (custom sounds supported; Off = vanilla).

Settings: Settings > **Mod Options** (main menu or pause menu). Pick a feature to see just its options;
left/right or click the arrows to change a value, and each page has a "Reset to defaults" button
(click twice to confirm). In multiplayer the host's values count. The menu is built in (Mod Options);
Mod Settings Menu is not needed, and it is fine to keep it installed for other mods.

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Options](https://github.com/Sthakur27/corekeeper-mods/tree/main/ModOptions)

Do not install the separate versions of these mods at the same time; every feature would run twice.

## License

Fast Auto Fishing is a fork of mikufish (GPL-3.0-or-later), so this combined mod is distributed under
GPL-3.0-or-later.

## Building (for modders)

The individual mod folders in the repo are the source. `python release/build_overhaul.py` copies
their scripts into `build/SidsOverhaul/Scripts/<Mod>/`, adds the central `SidsOverhaulMod`
(one settings section, one chat-command registration), re-patches the Fast Auto Fishing asset bundle so their
MonoBehaviours bind to the `SidsOverhaul` assembly, and writes one manifest.
`python release/switch_mods.py overhaul|separate` switches a local install between the two forms.
