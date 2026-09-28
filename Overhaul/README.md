# Sid's Overhaul

All of Sid's Core Keeper mods in one mod, so a multiplayer group only has to install one thing.

Included features (each is also available as its own mod on mod.io):

- **Loadout Fallback** and **Five Loadouts**: loadouts 2-5 inherit loadout 1's gear per slot; two extra loadout tabs.
- **Skill XP Multiplier**: XP multiplier per skill (`/xp` command).
- **Master Pet Plus**: edit any pet's level and talents, all 37 talents selectable.
- **Fast Auto Fishing**: auto fishing with no wait between bites.
- **Durability Multiplier**: tools, weapons and armor wear out slower (default 0.5x).
- **Buff Duration Floor**: short food and potion buffs last at least 3 minutes (healing separately configurable).
- **Faster Mushrooms**: mycelium spreads faster; picked wild mushrooms respawn near you.
- **Bigger Watering Cans**: watering cans water 2x2 or 4x4.
- **Better Fishing Loot**: rare fishing loot drops more often.
- **Auto Replant**: harvesting a crop replants it from a seed in your inventory.
- **Quick Buff**: one key (default B) eats or drinks one of every buff food and potion.
- **Potion Seller**: the Caveling Merchant sells every potion.
- **Hard Mode Tuning**: in hard mode, regular enemies hit for 1.5x normal damage instead of 2x; bosses keep full hard mode.
- **Keep Minions On Teleport**: summoned minions survive teleports and arrive with you.

All options are in the Mod Settings menu under "Sid's Overhaul", each prefixed with its feature
(for example "Mushrooms: Wild mushroom respawn"). In multiplayer the host's values count.

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Settings Menu](https://mod.io/g/corekeeper/m/mod-settings-menu)

Do not install the separate versions of these mods at the same time; every feature would run twice.

## License

Fast Auto Fishing is a fork of mikufish (GPL-3.0-or-later), so this combined mod is distributed under
GPL-3.0-or-later. Master Pet Plus is a fork of Parcew's Master Pet.

## Building (for modders)

The individual mod folders in the repo are the source. `python release/build_overhaul.py` copies
their scripts into `build/SidsOverhaul/Scripts/<Mod>/`, adds the central `SidsOverhaulMod`
(one settings section, one chat-command registration), re-patches the two asset bundles so their
MonoBehaviours bind to the `SidsOverhaul` assembly, and writes one manifest.
`python release/switch_mods.py overhaul|separate` switches a local install between the two forms.
