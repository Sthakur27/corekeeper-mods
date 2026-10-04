# Hard Mode Tuning

Core Keeper mod: in hard mode worlds, tune regular enemies (damage, health, move speed, projectile speed, attack
recharge speed) and bosses (damage, health) separately. By default regular enemies hit for 1.5x normal
damage instead of 2x and bosses keep full hard mode.

## What vanilla hard mode does

- Every enemy attack (melee, ranged, charges, jumps, explosions, beams, mortars) deals **2x** damage.
- Enemies get **1.5x** their normal (level-based) health.
- Boss loot 1.5x.

All of that applies to bosses and regular enemies alike.

## What this mod changes

Only regular enemies in hard mode worlds. By default only their damage changes (2x down to
1.5x); their health already matches vanilla hard at 1.5x, and you can lower it. Settings are in Settings > Sid\'s Mods > "Hard Mode Tuning":

| Setting | Choices | Default |
|---|---|---|
| Regular enemy damage (vs normal mode) | 1x, 1.25x, 1.5x, 1.75x, 2x | **1.5x** |
| Regular enemy health (vs normal level-based health) | 1x, 1.25x, 1.5x, 1.75x, 2x | **1.5x** |
| Regular enemy move speed | 0.9x to 1.5x in 0.05 steps, then 1.75x, 2x, 2.5x, 3x | **1x** |
| Regular enemy projectile speed (ranged attacks) | same as move speed | **1x** |
| Regular enemy recharge speed (how fast attacks come back; 1.25x = 0.8x cooldown) | same as move speed | **1x** |
| Boss damage (vs vanilla hard) | 0.9x to 1.5x | **1x** |
| Boss health (vs vanilla hard) | 0.9x to 1.5x | **1x** |

The speed settings make hard mode harder without more damage or health: faster enemies, faster
shots, shorter gaps between attacks. They only affect regular enemies; boss attack patterns are
timed to their animations, so bosses only get the damage and health settings.

Bosses, boss parts and boss projectiles (anything with a boss component, an ObjectID containing
"Boss", or Omoroth's tentacles) keep vanilla hard mode unless you change the boss settings. Loot
and non-hard worlds are untouched. Settings apply the next
time a world is loaded (enemy stats are fixed when the world starts).

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Options](https://github.com/Sthakur27/corekeeper-mods/tree/main/ModOptions)

Client + server (`requiredOn: 3`); the host's settings count in multiplayer.

## How it works (for modders)

Hard mode is baked in when the server world converts its prefabs (`ConversionManager.UseHardModeSettings`).

- `ConverterManagerPatch`: prefix on the public `Converter.ConversionManager` setter records which
  converters belong to a hard mode conversion (the getter is private).
- `DamagePatches`: prefix/postfix on each enemy attack converter's `Convert`. For regular enemies
  in a hard conversion the authoring damage and damage multiplier are scaled by `setting / 2`
  before the converter doubles them (all damage formulas are linear in the multiplier), then
  restored afterwards. Bosses (generic attack states plus the Core, Giant Cicada, Hydra, Larva,
  Scarab and Slime boss converters) are scaled by the boss damage setting on top of vanilla hard.
- `SpeedPatches`: same pattern for regular enemies only: `MovementSpeedAuthoring.speed`,
  `RangeAttackStateAuthoring.speedMultiplier` (projectile speed) and `min/maxCooldown` of the melee,
  ranged, beam, charge, jump and mortar attack states (divided by the recharge setting).
- `HealthPatch`: prefix on `HealthAuthoring.ComputeMaxHealth` passes `useHardModeSettings = false`
  for regular level-scaled enemies (giving the normal level-based value); the postfix multiplies it
  by the health setting. At the default 1.5x this equals vanilla hard mode health (checked in game
  for all 136 regular enemies), so by default only damage changes. Bosses keep the vanilla hard
  value, multiplied by the boss health setting.
