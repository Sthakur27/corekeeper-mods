# Hard Mode Tuning

Core Keeper mod: in hard mode worlds, regular enemies hit for 1.5x normal damage (instead of 2x)
and have 1.5x their normal level-based health, while bosses keep full hard mode.

## What vanilla hard mode does

- Every enemy attack (melee, ranged, charges, jumps, explosions, beams, mortars) deals **2x** damage.
- Level-scaled enemies get **1.5x their prefab's raw base health** instead of the level-based
  health they have in normal mode, which can differ a lot from a clean 1.5x.
- Boss loot 1.5x.

All of that applies to bosses and regular enemies alike.

## What this mod changes

Only regular enemies in hard mode worlds (settings in the Mod Settings menu, section "Hard Mode Tuning"):

| Setting | Choices | Default |
|---|---|---|
| Regular enemy damage (vs normal mode) | 1x, 1.25x, 1.5x, 1.75x, 2x | **1.5x** |
| Regular enemy health (vs normal level-based health) | 1x, 1.25x, 1.5x, 1.75x, 2x | **1.5x** |

Bosses, boss parts and boss projectiles (anything with a boss component, an ObjectID containing
"Boss", or Omoroth's tentacles), loot and non-hard worlds are untouched. Settings apply the next
time a world is loaded (enemy stats are fixed when the world starts).

## Requirements

- [CoreLib](https://mod.io/g/corekeeper/m/corelib)
- [Mod Settings Menu](https://mod.io/g/corekeeper/m/mod-settings-menu)

Client + server (`requiredOn: 3`); the host's settings count in multiplayer.

## How it works (for modders)

Hard mode is baked in when the server world converts its prefabs (`ConversionManager.UseHardModeSettings`).

- `ConverterManagerPatch`: prefix on the public `Converter.ConversionManager` setter records which
  converters belong to a hard mode conversion (the getter is private).
- `DamagePatches`: prefix/postfix on each enemy attack converter's `Convert`. For regular enemies
  in a hard conversion the authoring damage and damage multiplier are scaled by `setting / 2`
  before the converter doubles them (all damage formulas are linear in the multiplier), then
  restored afterwards.
- `HealthPatch`: prefix on `HealthAuthoring.ComputeMaxHealth` passes `useHardModeSettings = false`
  for regular level-scaled enemies (giving the level-based value); the postfix multiplies it by
  the health setting and logs `[HardModeTuning] <prefab>: health X (normal Y, vanilla hard Z)`.
