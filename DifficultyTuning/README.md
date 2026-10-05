# Difficulty Tuning

Core Keeper mod (formerly **Hard Mode Tuning**): tune regular enemies (damage, health, move speed,
projectile speed, attack recharge speed) and bosses (damage, health), with one full set of settings
for **hard mode worlds** and a parallel set for **normal worlds**. By default hard mode regular enemies
hit for 1.5x normal damage instead of 2x, bosses keep full hard mode, and normal worlds are vanilla.

## What vanilla hard mode does

- Every enemy attack (melee, ranged, charges, jumps, explosions, beams, mortars) deals **2x** damage.
- Enemies get **1.5x** their normal (level-based) health.
- Boss loot 1.5x.

All of that applies to bosses and regular enemies alike.

## What this mod changes

Two settings pages in Settings > Mod Options: **Difficulty Tuning (Hard)** applies only to hard mode
worlds, **Difficulty Tuning (Normal)** only to normal worlds. They have the same rows:

| Setting | Hard page (vs normal mode) | Normal page (vs vanilla normal) |
|---|---|---|
| Regular enemy damage | 1x to 2x in 0.25 steps, **1.5x** (vanilla hard = 2x) | 0.5x to 2x in 0.25 steps, **1x** |
| Regular enemy health | 1x to 2x, **1.5x** (= vanilla hard) | 0.5x to 2x, **1x** |
| Regular enemy move speed | 0.9x to 1.5x in 0.05 steps, then 1.75x, 2x, 2.5x, 3x; **1x** | same, **1x** |
| Regular enemy projectile speed | same as move speed, **1x** | same, **1x** |
| Regular enemy recharge speed (1.25x = 0.8x cooldown) | same as move speed, **1x** | same, **1x** |
| Boss damage | 0.9x to 1.5x vs vanilla hard, **1x** | 0.9x to 1.5x vs vanilla normal, **1x** |
| Boss health | 0.9x to 1.5x vs vanilla hard, **1x** | 0.9x to 1.5x vs vanilla normal, **1x** |

The speed settings make enemies harder without more damage or health: faster enemies, faster
shots, shorter gaps between attacks.

**Move speed is not a flat multiplier.** A flat 2x would make the already-fast late game enemies
absurdly fast. Instead, `new speed = base + (setting - 1) x min(base, reference)`, where the
reference is the median base speed of all regular enemies (read from the game data and printed
with every enemy's base speed in Player.log). Enemies at or below the median get the full
multiplier; faster ones all get the same flat bonus, so their effective multiplier shrinks as
`1 + (setting - 1) x reference / base` (at 2x: an enemy twice the median speed gets 1.5x, three
times the median about 1.33x). Settings below 1x stay a plain multiplier. They only affect regular enemies; boss attack patterns are
timed to their animations, so bosses only get the damage and health settings.

Bosses, boss parts and boss projectiles are anything with a boss component, an ObjectID containing
"Boss", or Omoroth's tentacles. Loot is untouched. Settings apply the next time a world is loaded
(enemy stats are fixed when the world starts). Upgrading from Hard Mode Tuning inside Sid's Overhaul
keeps your hard mode values.

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Options](https://github.com/Sthakur27/corekeeper-mods/tree/main/ModOptions)

Client + server (`requiredOn: 3`); the host's settings count in multiplayer.

## How it works (for modders)

Enemy stats are baked in when the server world converts its prefabs. Each conversion gets a profile:
`Tuning.Hard` when `ConversionManager.UseHardModeSettings`, `Tuning.Normal` for any other server
conversion (`IsServer`), none for client and startup conversions.

- `ConverterManagerPatch`: prefix on the public `Converter.ConversionManager` setter records each
  converter's profile (the getter is private). `HealthConverter`/`DropLootConverter` publish theirs
  while `ComputeMaxHealth` runs, since that method only receives the hard flag.
- `DamagePatches`: prefix/postfix on each enemy attack converter's `Convert`. For regular enemies
  in a hard conversion the authoring damage and damage multiplier are scaled by `setting / 2`
  before the converter doubles them (all damage formulas are linear in the multiplier), then
  restored afterwards. Bosses (generic attack states plus the Core, Giant Cicada, Hydra, Larva,
  Scarab and Slime boss converters) are scaled by the boss damage setting on top of vanilla hard.
  In normal worlds the converter does not double, so the factor is the Normal page's setting itself.
- `SpeedPatches`: same pattern for regular enemies only: `MovementSpeedAuthoring.speed`,
  `RangeAttackStateAuthoring.speedMultiplier` (projectile speed) and `min/maxCooldown` of the melee,
  ranged, beam, charge, jump and mortar attack states (divided by the recharge setting).
- `HealthPatch`: prefix on `HealthAuthoring.ComputeMaxHealth` passes `useHardModeSettings = false`
  for regular level-scaled enemies (giving the normal level-based value); the postfix multiplies it
  by the health setting. At the default 1.5x this equals vanilla hard mode health (checked in game
  for all 136 regular enemies), so by default only damage changes. Bosses keep the vanilla hard
  value, multiplied by the boss health setting. In normal worlds regular level-based enemies get
  normal health x the Normal page's health setting and bosses normal health x its boss health setting.
