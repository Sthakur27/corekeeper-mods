# Enchanting (working title) — design spec

Status: prototype 0.1.0 built 2026-11-03, not yet tested in game. Numbers marked *tune* are first
guesses meant to go into Mod Options.

## Prototype 0.1.0 deviations from the plan below

- **Bench**: the Octarine Workbench recipe grid is full (18/18) and vanilla only shows recipes inside its fixed
  6-slot tab windows, so the table sits in the **Galaxite Workbench** stations category (slot 11, empty in
  vanilla). The recipe still costs 20 Octarine Bars + 15 Gold Bars. `EnchantRecipe.Bench/FirstSlot`.
  Sid confirmed this placement (2026-11-03).
- **Cost source**: Gold Bars / Ancient Coins are counted and taken from the player's own inventory only
  (no nearby chests yet). Creative (god mode) characters roll free.
- **Looks**: in the world the table is the vanilla Upgrade Forge sprite (shared graphics); only the item icon
  is the violet recolor. A real skin comes later.
- **Debug**: `/enchant` (show), `list`, `set <key> <lvl>`, `remove <key>`, `clear`, `roll [n]`, `cost`, each with
  an optional trailing `helm|chest|pants` (default: held item). Changing commands need admin privileges.

## Player-facing summary

- New station: **Enchanting Table**, crafted at the **Octarine Workbench**.
- Put a weapon, tool or armor piece in the table's slot, pay **Gold Bars + Ancient Coins**,
  press the button: the item gets a **random enchant** from the pool for its item type.
- Enchants are per item (two identical swords can differ), survive save/reload and upgrades,
  and work in multiplayer.
- Weapon/tool enchants apply while the item is **held**; armor enchants while **equipped**; a summoning tome's
  while it is the last tome held and still in the inventory (carried rule).

## Station

- Recipe (Octarine Workbench, empty recipe slot found at load like SolariteDrill):
  20 Octarine Bar + 15 Gold Bar. *tune*
- Prefab: copy of the vanilla **Upgrade Forge** (`ObjectID.UpgradeForge` 4046) — same vanilla
  MonoBehaviours, its 1-slot inventory and `UpgradeForgeUI` (item slot, materials list, button,
  preview slot). Own name, icon and sprites (recolor of the forge; art pass later).
- Only vanilla MonoBehaviours in the prefab (bundle MonoScripts bind by assembly name; breaks in the
  Overhaul otherwise). Behaviour is redirected with Harmony when the open building is ours.

## Cost per roll *tune*

`gold = 5 × (1 + enchants already on item)`, `coins = 250 × (1 + enchants already on item)`

| Enchants already on item | Gold Bars | Ancient Coins |
|---|---|---|
| 0 | 5 | 250 |
| 1 | 10 | 500 |
| 2 | 15 | 750 |

Materials are taken from the player's inventory and nearby chests, like vanilla crafting
(`CraftingHandler.GetNearbyChests`). Shown in the vanilla materials list under the button.

## Roll rules *tune*

- Max **3 enchants** per item, each level **I–III**.
- A roll picks a random enchant from the item's pool (equal weights):
  - not on the item yet → added at level I (60%), II (30%), III (10%);
  - already on the item → +1 level (capped at III; a capped roll re-picks).
- Item already full (3 enchants): the roll **replaces one random enchant** with a new one
  (gamble to fish for a better set). *Alternative to decide: full items cannot be rolled.*
- The roll happens on the server only.

## Enchant pools (alpha, 2026-11-04 balance rework)

Balance model: `docs/balance-model.md`. Three kinds of value (`EnchantMode`):

- **Multiplier (Model B)**: the enchant multiplies the player's whole bucket ("x1.12 melee damage"). Each server
  tick the raw value is `p × (1000 + others)`, where `others` is the bucket (from the vanilla summary) minus our own
  entry. Rewritten on a change of more than max(2 raw, 2%), at most every 15 ticks (enchant or item changes write at once).
- **Item level (Model A)**: a percent of the vanilla value at the enchanted item's level (`variation`, or the
  prefab `LevelCD` when variation is 0), so it rises when the item is upgraded at the forge.
- **Flat**: fixed displayed value (×10 for tenths conditions, `EnchantUnits.TenthsWithoutUI`).

Costs stay flat per roll (section above). Save ids never change meaning; ids 44, 68, 80-82 are retired.

### Melee weapons (`MeleeWeapon`, non-range beams)
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 1 | Sharpness | `PhysicalMeleeDamageIncrease` | x1.04 / x1.08 / x1.12 damage | multiplier, bucket AllDamage + PhysicalMelee + Melee |
| 2 | Fire Aspect | `ApplyBurning` | 50 / 75 / 100 % of vanilla (10 + 3L) | item level |
| 3 | Knockback | `ChanceOnHitToKnockback` | 10 / 20 / 30 % | flat |
| 4 | Lifesteal | `LifeOnHit` | 50 / 100 / 150 % of vanilla max(1, 0.5L) hp | item level |
| 5 | Venom | `ChanceToApplyPoisoned` | 10 / 20 / 30 % | flat |
| 6 | Stunning | `ChanceOnMeleeHitToStun` | 3 / 6 / 9 % | flat |
| 7 | Swiftblade | `MeleeAttackSpeedIncrease` | x1.03 / x1.06 / x1.10 attack rate | multiplier, bucket MeleeAttackSpeed + AttackSpeed |
| 8 | Keen | `CritChance` | 3 / 6 / 9 % | flat (melee, ranged, magic) |
| 9 | Bossbane | `IncreasedDamageAgainstBosses` | 5 / 10 / 15 % | flat (melee, ranged, magic) |

### Ranged weapons (`RangeWeapon`, non-stackable `ThrowingWeapon`, range beams; not magic)
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 20 | Power | `PhysicalRangeDamageIncrease` | x1.04 / x1.08 / x1.12 | multiplier, bucket AllDamage + PhysicalRange + Range |
| 21 | Quick Draw | Range- or `MeleeAttackSpeedIncrease` (by the weapon's cooldown) | x1.03 / x1.06 / x1.10 | multiplier (ranged and magic) |
| 22 | Frost | `ChanceOnRangeShotToApplySlowedBySlime` | −10 / −20 / −30 (tenths) | flat |
| 23 | Stunshot | `ChanceOnRangeShotToStun` | 3 / 6 / 9 % | flat |
| 8, 9 | Keen, Bossbane | | as melee | |

Stackable throwables (grenades, bombs) are not enchantable (aux data is shared per stack).

### Magic weapons (`RangeWeapon` / `BeamWeapon` / `ThrowingWeapon` with `HasWeaponDamageCD.isMagic`)
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 100 | Arcane Power | `IncreasedMagicDamagePercentage` | x1.04 / x1.08 / x1.12 | multiplier, bucket AllDamage + Magic + Range/Melee |
| 101 | Frugality | `ReduceManaCostByPercentage` | 5 / 10 / 15 % less mana | flat |
| 102 | Spellsurge | `GainMagicDamagePercentageBoostAfterCrit` | +10 / 20 / 30 % magic damage for 6 s after a crit | flat |
| 103 | Clarity | `GainManaCostOnCrit` | 25 / 50 / 75 % of the shot's mana back on crit | flat |
| 104 | Frostbite | `ApplySlowedByCold` | −5 / −10 / −15 % move speed for 4 s, every hit (tenths, must be negative) | flat |
| 21, 8, 9 | Quick Draw, Keen, Bossbane | | as ranged | |

Existing magic items that rolled Ranged enchants keep them (Power is inert on magic).

### Summoning tomes (`SummoningWeapon`), carried rule
The last tome the player held keeps applying its enchants while it is anywhere in the player's inventory
(tracked per player on the server; forgotten when it leaves the inventory).
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 120 | Pack Leader | `IncreasedMinionDamagePercentage` | x1.04 / x1.08 / x1.12 minion damage | multiplier, bucket MinionDamagePercentage (+ magic share) |
| 121 | Frenzy | `IncreasedMinionAttackSpeed` | x1.03 / x1.06 / x1.10 minion attack rate | multiplier, bucket MinionAttackSpeed |
| 122 | Keen Fangs | `IncreasedMinionCritChance` | 5 / 10 / 15 % | flat |
| 123 | Slayer | `IncreasedMinionBossDamagePercentage` | 10 / 20 / 30 % | flat |

No Legion (max minions): extra minions are dismissed whenever the bonus drops.

### Tools (`MiningPick`, `Shovel`, `Sledge`, `DrillTool`, `Hoe`)
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 40 | Efficiency | `MiningPercentageIncrease` | ×1.10 / 1.20 / 1.30 mining damage (separate factor) | flat |
| 41 | Unbreaking | `ToolDurabilityLastsLonger` | 15 / 30 / 45 % | flat |
| 42 | Fortune | `ChanceForRandomLootFromWall` | 2 / 4 / 6 % | flat |
| 43 | Mending | `DurabilityIncreaseAfterMiningOre` | 1 / 2 / 3 | flat |

### Armor (`Helm`, `BreastArmor`, `PantsArmor`)
| Id | Enchant | Condition | I / II / III | Kind |
|---|---|---|---|---|
| 60 | Resilience (key `protection`) | `ReducedDamageTakenPercentage` | −3 / −5 / −8 % damage taken per piece (tenths); our total clamped at −25 % | flat |
| 61 | Thorns | `ThornsDamage` | 100 / 200 / 300 % of vanilla (1 + 2L) | item level (per piece) |
| 62 | Vitality | `IncreasedMaxHealthPercentage` | 3 / 6 / 10 % max HP | flat |
| 63 | Regeneration | `IncreasedHealthRegenEffectiveness` | 10 / 20 / 30 % | flat |
| 64 | Evasion | `DodgeChance` | 2 / 4 / 6 % | flat |
| 65 | Bosswarden | `ReducedDamageFromBosses` | 3 / 6 / 9 % | flat |
| 66 | Unbreaking | `EquipmentDurabilityLastsLonger` | 15 / 30 / 45 % | flat |
| 67 | Swiftness (pants only) | `MovementSpeedIncrease` | 3 / 6 / 9 % | flat |
| 69 | Blast Protection | `ReducedDamageFromExplosions` | 10 / 20 / 30 % less damage from friendly (own/allied) explosions; vanilla cap 90 | flat |

Removed 2026-11-04 (Sid): Lure, Luck of the Sea, Reach (so fishing rods take no enchants), Magnetism, Prospector.

Not yet: fishing rods, rings, necklaces, offhands, Legion, Piercing/Punch, minion on-hit enchants, Minecraft
enchants that need Burst-only logic (Multishot, Infinity, Silk Touch, Looting, Loyalty, Vanishing).

## How it works (implementation)

- **Storage**: vanilla aux data `PetTalentBuffer` on the item (list of `(id, points)`, `[GhostField]`,
  saved generically). Encoding: `petTalentID = 1000 + enchantId` (out of vanilla talent range),
  `points = level`. Written server-side via
  `InventoryAuxDataSystemDataCD.SetOrAllocate…` like ArmorDye's `MealsEatenCD`, so it coexists with dyes.
  Vanilla only reads `PetTalentBuffer` for pets (PetExtensions, PetInitSystem, PetTalentsWindow,
  SummarizeConditions pet slot) — **must verify** nothing chokes on unknown talent IDs.
- **Effects**: server system (`PugSimulationSystemBase`, server world) each tick reads the held item and
  the equipped armor, sums enchant conditions per `ConditionID`, and keeps one entry per ID in the
  player's `ConditionsBuffer` (refreshing `removeTick`; removing entries when the item changes).
  `SummarizeConditionsSystem` (Burst, unpatched) then folds them into stats, and NetCode replicates
  `ConditionsBuffer` to clients.
- **Enchant button**: Harmony on `UpgradeForgeButton.GetRequiredMaterials` / `ShouldBeActive` and
  `UpgradeForgeUI.Upgrade` / `LateUpdate` when the open building is the Enchanting Table: show our cost,
  hide the level-up preview (or show the enchanted preview), send a CoreLib server command
  (`/enchant roll`, like ArmorDye's `DyeCommand`) instead of the vanilla Upgrade craft action.
- **Server roll**: validate the player has the table open/in range and the cost (inventory + nearby
  chests), consume materials, roll, write aux data.
- **UI**: item tooltip lines ("Sharpness II") via a hover-stats patch; hide our entries from the buff
  bar (or show one "Enchanted" icon) via a UI patch.
- Mod Options: costs, max enchants, level weights, full-item rule.
- requiredOn: 3 (server rolls + applies; clients need tooltips and the station prefab).

## Risks to check first (prototype order)

1. `PetTalentBuffer` on a sword: allocate, save/reload, no pet-system errors.
2. Permanent `ConditionsBuffer` entry: stat actually changes in the character window; survives
   death/respawn handling (`ConditionsSystem` shortens timers on death unless `isPermanent`).
3. Condition value units per ID (percent vs. tenths) against vanilla items.
4. Upgrade Forge prefab copy: UI opens, slot works, our Harmony redirect fires.
5. On-hit conditions (`ApplyBurning` etc.) trigger from conditions on the player, not only from gear.

## Verification matrix (how to prove each enchant works)

Setup: `/enchant debug on`, two copies of the same item at the same level, one enchanted with
`/enchant set <key> 3`. `/enchant reset` between runs, `/enchant report` to read results (also in Player.log).
`/enchant stats` shows the raw value each enchant adds and the player's total for that stat.
Every enchant first needs `/enchant stats` to list its condition while the item is held/worn.

| Enchant | Proof | Pass when |
|---|---|---|
| Sharpness, Power, Arcane Power | report: hit avg per item on the training dummy (20+ non-crit hits each) | enchanted avg ≈ plain avg × 1.04/1.08/1.12 (any buff level) |
| Bossbane (vs boss) | same, against a boss | enchanted avg ≈ plain avg × (1 + bonus) |
| Keen | report: crit rate per item (50+ hits) | crit rate higher by ≈ the bonus |
| Swiftblade, Quick Draw | character stats window attack speed; hits per 10 s in Player.log | stat window number rises |
| Unbreaking (tools, armor) | report: durability lost per drop over 30+ uses | fewer points lost / fewer drops |
| Mending | Player.log durability lines while mining ore | durability goes up after ore |
| Efficiency | character stats window mining damage; mining hits in log | mining number rises |
| Vitality, Evasion, Regeneration, Swiftness | character stats window (max HP, dodge, regen, speed) | number rises with the piece on |
| Resilience | take the same hit with/without the piece | damage × 0.97/0.95/0.92 |
| Pack Leader, Frenzy, Keen Fangs, Slayer | character stats window (minion damage/speed/crit) with the tome carried | number rises |
| Thorns, Bosswarden | take hits from an enemy/boss, compare damage numbers on you | reflected damage / less damage |
| Fire Aspect, Venom, Stunning, Frost, Stunshot, Knockback | visual: burn/poison/stun/slow icon or knockback on the target | effect seen on hits |
| Lifesteal | your HP number rising on hits | heal per hit |
| Fortune | mine walls with/without, count bonus drops | more drops (noisy, needs many walls) |
