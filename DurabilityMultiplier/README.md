# Durability Multiplier (Core Keeper mod)

Tools, weapons and armor lose durability at a fraction of the vanilla rate.

- Set the rate in **Mod Settings** (needs the Mod Settings Menu mod): **Durability loss rate**
  cycles through 0x, 0.1x, 0.25x, 0.5x, 0.75x, 1x. 0x = equipment never wears out, 1x = vanilla.
  **Default is 0.5x** (gear lasts twice as long).
- Changes apply instantly and are saved by CoreLib (`DurabilityMultiplier/config.cfg`).
- Repairs are never touched. In multiplayer the host/server's setting is the one that counts.

## How it works

Every durability loss in the game is a one-tick request left on the player entity that the game's
Burst `ChangeDurabilitySystem` consumes: `ReduceDurabilityOfEquippedTriggerCD` (held tool/weapon,
integer loss amount) and `ReduceDurabilityOfAllEquipmentTriggerCD` (helm/chest/pants when you take a
hit). This mod's `ScaleDurabilityLossSystem` runs one system earlier in the same group and rewrites
those requests before the game sees them. No Harmony patch, no Burst disabling.

Durability is an integer, so fractions are handled by skipping losses probabilistically:

- **Held item:** the loss amount is multiplied by the rate with stochastic rounding. The whole part is
  always applied and the fractional remainder is applied with that probability, so over time the
  item loses exactly `rate` times the vanilla durability (at 0.5x, each swing has a 50% chance of
  costing 1 point). When the result is 0 the request is cancelled.
- **Armor on hit:** the game turns the request (hit damage + a percentage) into a non-linear loss
  (minimum 1, plus a "hits under 3% of max HP are free" threshold), so the number cannot be scaled
  directly. Instead each hit's armor durability loss is applied in full with probability `rate`
  and otherwise skipped. Averaged over many hits this matches the rate; a single hit is all-or-nothing.

Rolls use the player's own replicated `RandomCD`, the same RNG the vanilla job rolls on for its
"no durability loss" condition, so client prediction and server agree. At 1x the system does
nothing at all (byte-for-byte vanilla, no random numbers consumed).

## Limits

- Armor loss per hit is skipped or applied whole (see above); expected loss matches, variance is higher.
- Do not combine with the mod.io "Disable Durability" mod: it skips the whole durability system, so
  this mod would have nothing to scale (set this mod to 0x instead for the same effect).
- The setting is per installation; clients joining a server get the server's behaviour regardless of
  their own value (their local prediction may briefly disagree until the next server snapshot).

## Install

Run `..\install.bat DurabilityMultiplier` (copies into
`CoreKeeper_Data\StreamingAssets\Mods\DurabilityMultiplier`), then restart the game.
Requires CoreLib and ModSettingsMenu. Check
`%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` for
`Successfully compiled DurabilityMultiplier` and `[DurabilityMultiplier] Loaded. Durability loss rate: ...`.
