# Buff Duration Floor (Core Keeper mod)

Food and potion buffs in Core Keeper are short (the mushroom speed boost from cooking, most
potions). This mod sets a **minimum duration**: any buff you get from eating or drinking that
would last less than the floor lasts the floor instead. Buffs that are already longer are not
changed, and debuffs are never extended.

## Settings

Both are in **Mod Settings** (needs the Mod Settings Menu mod) and use the same ladder:
**Off**, 30 s, 45 s, 1:00, 1:30, 2:00, 3:00, 5:00, 10:00. **Off** means that group is left
exactly at vanilla.

| Setting | Applies to | Default |
|---|---|---|
| **Minimum buff duration** | every other positive timed food/potion buff (speed, damage, armor, glow, ...) | **3:00** |
| **Minimum healing duration** | heal-over-time and mana-regen conditions (e.g. cooked food's "4.2 health every second", potion heal-over-time) | **Off** (vanilla) |

Healing defaults to Off because a 3-minute regen tick is far too strong; raise it deliberately.
Changes apply immediately and independently: the next thing you eat uses the new floors, buffs
already running keep their timer, and setting either back to Off restores that group's vanilla
numbers without a restart. Persisted in CoreLib's config for this mod
(`BuffDurationFloor/config.cfg`).

In multiplayer the server (host) decides the actual duration; a client with a different setting
mispredicts for a moment and is then corrected by the server.

## What is affected (exactly)

The game stores each consumable's buff list on the item prefab in the
`GivesConditionsWhenConsumedBuffer` component. Every element has two entries: `conditionData`
(used when the item is eaten raw) and `conditionDataWhenCooked` (used when the item is one of the
two ingredients of a cooked meal). When a player eats or drinks, the Burst job
`EatableSlotConsumeResultEvaluationSystem` calls `ConditionUIExtensions.GetConditionsOnConsume`,
which reads those entries (both ingredients' cooked entries for cooked food, the item's own raw
entry for raw food and potions), scales the value for rarity, and adds the conditions to the
player's `ConditionsBuffer` with `removeTick = now + duration`.

This mod raises `duration` on those prefab entries, in both the client and the server world, to
`max(vanilla, floor)`, where the floor is the buff floor or the healing floor depending on the
condition's group (Off = no change). An entry is extended only if all of the following hold:

- its condition is not `None` and its vanilla duration is > 0 and finite (instant effects such as
  healing, mana, hunger, and the golden-plant permanent max-health increase have duration 0);
- the game's `ConditionsTable` does not flag the condition `isPermanent`;
- the game's `ConditionsTable` does not flag the condition `isNegative` (so no poison, slow,
  weakness or any other debuff a food or potion applies gets longer);
- the floor for its group is not Off.

Which group a condition belongs to is read from the game's condition data, not a hand-written
list: it is **healing** when its `ConditionEffect` in the `ConditionsTable` is `HealOverTime`,
`HealOverTimePercentage` or `ManaRegen`, or (fallback for ids without a table entry) when its
`ConditionID` name contains `HealOverTime`, `HealingOverTime`, `HealthRegen` or `ManaRegen`;
otherwise it is an ordinary **buff**. In vanilla data the healing group covers
  `HealOverTime` (the food "X health every second" buff), `HealOverTimePercentage`,
  `HealOverTimeFromPotion`, `HealingPotionAddHealOverTime`, `AuraHealingOverTime`,
  `HealthRegenFromBeingBelowHalfHealth`, `IncreasedHealthRegenEffectiveness`,
  `IncreasedManaRegen`, `IncreasedManaRegenEffectiveness`, `WellFedManaRegenerationPercentage`
  and `IncreasedManaRegenPercentageWhileCasting` (most of those never appear on a consumable; the
  ones that matter are `HealOverTime` from cooked food and the potion heal-over-time).

So: raw eatables (mushrooms, plants, fish, meat, berries...), cooked food (via the ingredients'
cooked entries, including every mushroom-based speed buff), and potions (they carry the same
buffer) are covered.

Not touched, because they come from other data or code:

- equipment conditions (`GivesConditionsWhenEquippedBuffer`), held-item conditions, set bonuses;
- skill and talent conditions, souls, auras, environmental and movement conditions, minion buffs,
  random condition affixes;
- three bonuses that talents inject at consume time with durations hard-coded inside the Burst
  job (`IncreasedBossDamageFromEatingFish` 60 s, `MeleeAttackSpeedFromCookedFood` 30 s,
  `HealOverTimeFromPotion` 20 s). They keep their vanilla length.

**"+15% damage against bosses for 1 min" is not extended.** That buff is the fishing talent's
`IncreasedBossDamageFromEatingFish`: `ConditionUIExtensions.GetConditionsOnConsume` adds it with
`duration = 60f` written in code whenever the eaten item (or an ingredient of a cooked meal) has
`FishCD` and the talent value is non-zero. No prefab carries it, so the prefab patch cannot see
it, and it shows up in the item tooltip with the same hard-coded 60 s. (The only boss-damage
condition that exists in food data, `IncreasedDamageAgainstBosses`, is used by minions; the
Player.log line described below lists every timed condition id found on consumables, so if a food
does carry a boss-damage entry it will appear there and would already be extended.) Extending
this talent buff would need a per-tick system rewriting the player's `ConditionsBuffer` entry
after the fact; that was deliberately left out.

## Why prefab data instead of editing the player's condition timers

Both approaches were evaluated. Editing `ConditionsBuffer.removeTick` after the fact would need a
per-tick system in both worlds, a "seen" set to avoid re-extending every frame, and it cannot tell a
food buff from the same condition id applied by a talent or piece of gear. Patching the prefab
entries is a one-off write (re-done only when the setting changes or the number of consumable
prefabs changes), the game itself hands out the longer buff so client prediction and the server
agree, no per-tick work, and item tooltips (which read the same buffer) show the real duration.
The original values are remembered, so lowering the floor restores vanilla numbers without a
restart.

Files: `Scripts/BuffDurationFloorMod.cs` (settings), `Scripts/FloorSettings.cs` (ladder/parsing,
both floors), `Scripts/ConditionFilter.cs` (skip / buff / healing classification),
`Scripts/Systems/ConsumableDurationFloorSystem.cs` (the patch system).

## Changelog

- **1.2.0**: the **Also extend healing** toggle is replaced by a second independent floor,
  **Minimum healing duration** (same ladder, default Off). Ladder is now Off, 30 s, 45 s, 1:00,
  1:30, 2:00, 3:00, 5:00, 10:00 (the old 30-second steps are gone); old "3m"-style config values
  are still parsed. Off = vanilla for that group.
- **1.1.0**: regeneration-over-time buffs (health and mana) are no longer extended by default;
  new **Also extend healing** toggle restores the 1.0.0 behaviour. The apply log line now lists
  the timed condition ids found on consumables and which were excluded.
- 1.0.0: initial release.

## Install

Run `..\install.bat BuffDurationFloor` (copies into
`CoreKeeper_Data\StreamingAssets\Mods\BuffDurationFloor`), then restart the game. Requires
CoreLib and ModSettingsMenu. Check `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log`
for `Successfully compiled BuffDurationFloor` and
`[BuffDurationFloor] server: buff floor 3:00, healing floor Off applied to N consumable prefabs (...)`.
That line ends with `Timed condition ids seen: ...` (every condition id with a positive finite
duration on any consumable) and `Excluded (permanent/negative): ...` (the subset neither floor
may ever touch).
