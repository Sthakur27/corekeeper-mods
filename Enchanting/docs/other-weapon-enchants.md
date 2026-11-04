# Enchants for magic, summoning, throwing and beam weapons (design, 2026-11-04)

Design only. Nothing here is implemented. Code references point at the decompiled game sources
(`...\c4e37e1d-...\scratchpad\dec`, files under `Pug.Other` / `Pug.Base` unless noted).
Effect numbers are `ConditionEffect` indices. The ConditionID-to-effect mapping lives in the ConditionsTable
asset, not in code, so each one below is matched by name. Every enchant still needs the usual
`/enchant stats` check before its numbers are final.

## Top 3 recommendations

1. **Route weapons by damage type, not only by ObjectType.** Magic staffs and scepters are
   `RangeWeapon` (some beams too) with `HasWeaponDamageCD.isMagic = true`. Today they get the **Ranged** pool,
   and **Power does nothing on them**: magic damage uses effect 102 *instead of* 22
   (`EntityUtility.cs:2265-2276`). Add a **Magic** pool for any weapon whose prefab has `isMagic`, and route
   `BeamWeapon` by `HasWeaponDamageCD` (magic → Magic, isRange → Ranged, else → Melee).
2. **Summoning tomes get a minion-stat pool, but only with "carried" semantics.** Minion stats are read
   live from the owner (`MinionHandlerSystem.cs:2844-2862`, `MinionExtensions.cs:52-58`), and minions stay
   out while their tome is anywhere in the inventory (`MinionHandlerSystem.cs:827-850`). A held-only
   +Max Minions would dismiss a minion every time you swap to a sword (`MinionHandlerSystem.cs:1169-1210`).
   Rule: **the last tome you held keeps its enchants while it is still in your inventory.**
3. **Throwing: enchant only non-stackable throwables (Throwing Daggers, Galaxite Chakram, …) with the
   Ranged pool. Skip grenades, bombs and `CastingItem`.** Also add **Piercing** (`PiercingProjectiles`,
   a plain flat count read at shot time, `RangeWeaponSlot.cs:318`) to Ranged, Magic and Throwing. It is the one
   Minecraft bow enchant that works as a plain condition.

## 1. What each class actually reads

How a hit is resolved matters for "held" enchants:

- **Projectiles** (bows, staffs, scepters, throwables) copy the owner's inheritable conditions when they spawn
  (`EntityUtility.InheritAttackData` 2807, called from `RangeWeaponSlot.cs:266/339`). The hit then uses the
  projectile's own summary as the attacker (`AttackSystem.cs:841-849`, `ProjectileSystem.cs:333-350`,
  `isRanged = true`, `isMagic` from the weapon). This is a snapshot, so swapping weapons after a shot keeps the bonus.
  Which IDs get copied is a ConditionsTable flag (`ConditionCanBeInherited`, not in the decompiled sources),
  the same unknown the current Ranged pool already carries.
- **Read live from the player** at use time: mana cost (`EquipmentSlot.cs:533-580`), weapon cooldown
  (`RangeWeaponSlot.cs:55-56`, `BeamWeaponSlot.cs:46-57`), mana pool/regen (`ManaSystem.cs:156-195`), piercing
  count (`RangeWeaponSlot.cs:318`), every minion stat (below), explosives (`ExplosiveSystem.cs:2094-2110`).
- **Beam weapons** are raycasts through the player's own live summary (`PlayerController.TryRayCastAttack` 5215,
  isRange/isMagic from `HasWeaponDamageCD` at 5223-5225). Beams also count as mining tools, except the
  Flamethrower (`PlayerController.cs:5749`).

### Magic (any weapon with `HasWeaponDamageCD.isMagic`)
| Condition (ID → effect) | Where read | Notes |
|---|---|---|
| IncreasedMagicDamagePercentage (261 → 102) | `EntityUtility.cs:2269-2272`, applied `/1000` at 2132 | tenths of a percent (also `CharacterStatsWindow.cs:357`). Replaces the Physical*Damage bonus for magic |
| RangeDamageIncrease (290 → 117), AllDamage (33) | `EntityUtility.cs:2266, 2279` | generic; still applies to magic |
| GainMagicDamagePercentageBoostAfterCrit (268) | `EntityUtility.cs:2049-2058` | on crit: applies `MagicDamagePercentageBoostAfterCrit = value×10` for 6 s, so the value is a **whole %** |
| GainManaCostOnCrit (264 → 106) | `EntityUtility.cs:2042-2047` | on crit: refunds `value% × shot mana cost` |
| GainRemainingManaAsMagicDamagePercentage (273 → 110) | `EntityUtility.cs:1702-1706` | +value% × (mana / maxMana), magic only, whole % |
| ReduceManaCostByPercentage (367) | `EquipmentSlot.cs:576-577` | raw `/100`, whole %. Read live |
| IncreasedManaRegenEffectiveness (255) | `ManaSystem.cs:179` | regen × (1 + v/100). Read live |
| IncreasedMaxMana (256 → 100, flat) | `ManaSystem.cs:159-160` | base 100 mana. Read live |
| ApplySlowedByCold (369 → 148) | `EntityUtility.cs:1862-1876` | **must be negative**. Applies on *every* hit (no roll), 4 s, unless the target is immune (149) |
| CritChance, IncreasedDamageAgainstBosses, ChanceOnRangeShotToStun (59), ChanceOnRangeShotToApplySlowedBySlime (56, negative), ChanceOnRangeHitToKnockback (364 → 144), ApplyBurning (17) | `EntityUtility.cs` 2031, 1693-1696, 1902-1917, 1834-1846, 2011-2017, 1803-1817 | all work for magic projectiles (isRanged = true) |
| RangeAttackSpeedIncrease (41) | `RangeWeaponSlot.cs:56` | staffs use the **range** cooldown even though they are magic |
| **Dead for magic:** PhysicalRangeDamageIncrease (22), LifeOnHit, ManaOnHit (348 → 141) | `EntityUtility.cs:2273-2276, 2209-2229` | the last two sit in the `!isRanged` branch (melee only) |

### Summoning (`SummoningWeapon`, the Tomes)
All of these are read from the **owner** while minions are out, so they are player-wide and live:
| Condition (ID → effect) | Where read | Units |
|---|---|---|
| IncreasedMaxMinions (259 → 113) | `MinionExtensions.cs:127-130` (1 + v), `MinionHandlerSystem.cs:1169, 1190-1210` (dismisses extras) | flat count |
| IncreasedMinionDamagePercentage (260 → 105) | `MinionExtensions.cs:55` `/1000` | tenths % |
| IncreasedMagicDamageToMinionDamage (279) | `MinionExtensions.cs:56` | % of magic damage given to minions |
| IncreasedMinionAttackSpeed (277 → 114) | `MinionExtensions.cs:65-75` → minion `AttackSpeed` (`MinionHandlerSystem.cs:2844-2849`), `/1000` (`TouchAttackStateSystem.cs:167`) | tenths % |
| IncreasedMinionCritChance (287 → 115) | `MinionHandlerSystem.cs:2850-2855` → minion CritChance | whole % |
| IncreasedMinionCritDamagePercentage (326 → 128) | `MinionHandlerSystem.cs:2856-2861` → crit multiplier 1.5 + v/100 (`EntityUtility.cs:2032`) | whole % |
| IncreasedMinionBossDamagePercentage (327 → 129) | `MinionHandlerSystem.cs:2862-2867` → minion IncreasedDamageAgainstBosses | whole % |
| ReducedManaReservationPercentage (257 → 150) | `MinionHandlerSystem.cs:1187`, `MinionSummonAndDismissSystem.cs:311` | whole %, clamp 0-100 |
| MinionsExplodeAsTheyDespawn (317) | `MinionHandlerSystem.cs:1543-1572` | v/100 × minion damage as an explosion on despawn |
| Per-minion owner buffs: GainRangeAttackSpeedPerMinion (275), IncreasedMinionCountAsMagicDamagePercentage (278), …AsMagicBarrier (280), …AsArmor (295) | `ConditionsFromMinionSystem.cs:136-195` | value × minion count (×10 for 275/278/295) |
| LifeOnMinionHit (324 → 126) | `EntityUtility.cs:2187-2194` | read from the **minion's** summary, not the owner's, so a condition on the player probably does nothing (see creative idea B) |

The player's own on-hit conditions (burn, poison, stun) do **not** reach minions. A minion's on-hit comes from its own
prefab `ConditionsBuffer`, which `SetConditionsJob` re-levels (`MinionHandlerSystem.cs:2868-2883`).

### Throwing (`ThrowingWeapon`) and explosives
- Throwables fire through `RangeWeaponSlot` (same slot as bows, `PlayerController.cs:3907-3909`). They spawn
  projectiles that inherit conditions, so the whole **Ranged** pool applies (Power included, unless isMagic).
- Grenades are `ThrowingWeapon` stacks consumed per throw (`RangeWeaponSlot.cs:32-43`). Their explosion reads the
  owner's IncreasedExplosivesDamage (57 → 46) and radius (307 → 120) **at detonation**
  (`ExplosiveSystem.cs:2094-2102`, `ExplosionDamageSystem.cs:205-211`), after the last grenade may already be gone.
  Placed bombs are `PlaceablePrefab` and snapshot the explosive conditions on placement (`InventoryUtility.cs:359-368`).

## 2. Proposed pools

Values are in displayed units (EnchantUnits applies ×10 where `showDecimal`), I / II / III, all *tune*.
New save ids: Magic 100-119, Summon 120-139, shared/ranged additions 24+.

### Magic (new `EnchantSlot.Magic`; RangeWeapon or BeamWeapon with `isMagic`)
| Enchant | Condition | I / II / III | Sign / unit notes |
|---|---|---|---|
| Arcane Power | IncreasedMagicDamagePercentage | 5 / 10 / 15 % | tenths internally. Check the tooltip shows "+5%" and not "+0.5%" |
| Spellsurge | GainMagicDamagePercentageBoostAfterCrit | 10 / 20 / 30 % for 6 s after a crit | whole %, the game ×10s it. Pairs with Keen |
| Clarity | GainManaCostOnCrit | 25 / 50 / 75 % of the shot's mana refunded on crit | whole % |
| Frugality | ReduceManaCostByPercentage | 5 / 10 / 15 % | whole %. Stacks additively with the vanilla temporary one from explosions (`ExplosiveSystem.cs:2063-2072`) |
| Overcharge | GainRemainingManaAsMagicDamagePercentage | 10 / 20 / 30 % at full mana | scales with current mana |
| Frostbite | ApplySlowedByCold | −5 / −10 / −15 | **negative**, every hit (no chance roll). Slow units unverified, start small |
| shared | Keen, Bossbane, Quick Draw, Piercing | as Ranged | Quick Draw works because staffs use the range cooldown |

Leave Wellspring (IncreasedManaRegenEffectiveness 15/30/45 %) and Capacity (IncreasedMaxMana +10/20/30 flat)
as reserves. They are weak as held-only stats.

### Summoning (new `EnchantSlot.Summon`; carried semantics, see recommendation 2)
| Enchant | Condition | I / II / III | Notes |
|---|---|---|---|
| Legion | IncreasedMaxMinions | +1 / +2 (two levels only, `Values = {1,2}`) | strongest one. Weight it lower or cap it at II |
| Pack Leader | IncreasedMinionDamagePercentage | 10 / 20 / 30 % | tenths internally |
| Frenzy | IncreasedMinionAttackSpeed | 10 / 20 / 30 % | tenths internally |
| Keen Fangs | IncreasedMinionCritChance | 5 / 10 / 15 % | whole % |
| Savagery | IncreasedMinionCritDamagePercentage | 15 / 30 / 45 % | whole % |
| Slayer | IncreasedMinionBossDamagePercentage | 10 / 20 / 30 % | whole % |
| Soulbond | ReducedManaReservationPercentage | 10 / 20 / 30 % | whole %. Lets more minions fit in your mana |

Optional 8th: **Martyrdom**, MinionsExplodeAsTheyDespawn 50 / 100 / 150 (= 0.5-1.5× minion damage).

### Throwing (`ThrowingWeapon` where `!ObjectInfo.isStackable`)
Reuse the **Ranged** pool as-is (`EnchantSlot.Ranged` from `SlotOf`). No new enchants are needed.

### Additions to Ranged (and Magic/Throwing via shared flags)
| Enchant | Condition | I / II / III | Notes |
|---|---|---|---|
| Piercing | PiercingProjectiles (225) | +1 / +2 / +3 enemies | flat. Read live at shot time (`RangeWeaponSlot.cs:318`). The `SetComponent` at 320-323 has no guard, so watch Player.log for an ECB error on a projectile without the piercing component (vanilla gear grants this player-wide, so it is probably fine). Not for beams |
| Punch | ChanceOnRangeHitToKnockback (364 → 144) | 10 / 20 / 30 % | whole %, ranged only (`EntityUtility.cs:2011-2017`) |
| Flame | ApplyBurning | 10 / 20 / 30 | no isRanged gate (`EntityUtility.cs:1803-1817`). Needs ApplyBurning to be inheritable onto projectiles. Test it |

## 3. Creative ideas (beyond plain held buffs)

| Idea | Hook | Rating |
|---|---|---|
| **A. Carried tome** (required for Summon) | `EnchantEffectSystem.Collect`: keep `Dictionary<Entity, (slot, ObjectID)>` of the last held SummoningWeapon per player. While that slot still holds that tome, `AddFrom` it with `heldSlot` ignored. Optionally require `MinionCountTrackerCD.count > 0` | **feasible**, managed EntityManager only |
| **B. Minion on-hit enchants**: Static Familiars (ChanceOnHitToStun 318 → 124, any hit), Soul Siphon (LifeOnMinionHit 324: heals the owner, `EntityUtility.cs:2187-2194`), Bully (ChanceOnHitToKnockback for melee minions) | Server system: for each entity with `MinionCD` + `OwnerReferenceCD.owner == player`, keep marker entries (same 0.4242/∞ trick) in the **minion's** `ConditionsBuffer` (the buffer exists, `MinionHandlerSystem.cs:2836`). **Never** ApplyBurning / ChanceToApplyPoisoned / AuraApplyRadioactive, because `SetConditionsJob` rewrites those by level (`MinionExtensions.cs:106-125`) | **risky**: needs an in-game check that minion summaries pick it up and that ranged-minion projectiles inherit it. Re-add on every minion spawn |
| **C. Enthrall** (magic/summon): ChanceToApplyCharmed (214 → 122) | plain condition. Charms non-boss, non-player targets for 20 s (`EntityUtility.cs:1788-1800`) | **feasible**, balance risk. Keep it at 3 / 6 / 9 % |
| **D. Kraken's Call** (ranged/magic): ChanceOnRangeHitToSpawnOctopusBossProjectile (177 → 67) | plain, vanilla legendary proc (`EntityUtility.cs:2061-2067`) | **feasible**. Needs an in-game look at the visuals |
| **E. Scarab's Curse**: ChanceOnHitToSpawnScarabBossProjectile (197 → 70) | plain, any hit (`EntityUtility.cs:2102-2110`) | **feasible** |
| **F. Martyrdom**: MinionsExplodeAsTheyDespawn | plain, owner-side (`MinionHandlerSystem.cs:1543`) | **feasible** |
| **G. Pack Tactics**: IncreasedMinionCountAsArmor (295) / …AsMagicDamagePercentage (278) on the tome | plain. Vanilla turns it into per-minion owner buffs (`ConditionsFromMinionSystem.cs:166-195`) | **feasible**, units unclear (×10 inside) |
| H. Mana on hit for staffs | ManaOnHit is applied only in the melee branch (`EntityUtility.cs:2209-2229`). A replacement would need a post-damage hook, and damage runs in Burst (AttackSystem/ProjectileSystem) | **infeasible** (AGENTS.md §2-3) |
| I. Multishot / Infinity / bounce | shot count and mana spend live in Burst equipment updates. Bounce only at max windup (`RangeWeaponSlot.cs:324`) | **infeasible** |
| J. Summon-on-hit (effect 104 ChanceToSpawnMinionOnHit) | `ClientSystem.cs:393-403` spawns from the *equipped* tome on direct player hits. No ConditionID name in the enum maps to effect 104; it needs a ConditionsTable dump | **risky** (unknown ID) |
| K. Execute (ChanceOnHitToKillTargetWithLowerHealth 155 → 62) | kills outright when the target's HP < yours, **bosses included** (`EntityUtility.cs:2019-2023`) | feasible but **don't** (breaks bosses) |
| L. Blast Protection on **armor** (ReducedDamageFromExplosions 309 → 123) | plain, worn | **feasible**. Gives explosives a home without enchanting stacks |

## 4. Skip, and why

- **`CastingItem` (602): not a weapon.** It shares the slot with `UniqueCraftingComponent` (`PlayerController.cs:3928-3930`)
  and is the parchment/scroll "cast to use a recipe" item (`CastingItemSlot.cs:14-60`, `ParchmentRecipe`). Magic staffs are `RangeWeapon`.
- **Stackable throwables (grenades) and placed bombs.** Aux data on a stack is shared, and it is lost or duplicated on
  split, merge and consume. The last grenade's explosion reads stats after the slot is empty. Put explosive
  stats on armor instead (idea L), if anywhere.
- **Offhand, rings, necklaces.** These are not weapons. They are easy later as "worn" pools: the indexes are on `EquipmentCD`
  (`SummarizeConditionsSystem.cs:222-237`), using the same path as armor.
- **Do not skip `BeamWeapon`.** Classify it via `HasWeaponDamageCD` as in recommendation 1. Exclude Piercing (beams are raycasts) and
  LifeOnHit/ManaOnHit when `isRange`.

## Implementation notes (for later)

- `Enchants.SlotOf`: for `RangeWeapon`/`BeamWeapon`/`ThrowingWeapon`, use
  `PugDatabase.TryGetComponent<HasWeaponDamageCD>(objectData, out var w)`. `w.isMagic` → Magic; else
  `w.isRange` → Ranged; else Melee. `ThrowingWeapon` with `isStackable` → None. `SummoningWeapon` → Summon.
- `IsHeldSlot`: Magic is held. Summon follows idea A.
- Existing magic items that already rolled Ranged enchants: keep them (they still read Keen, Quick Draw and Stunshot). Power stays
  inert on them. A one-time `/enchant` reroll is enough; no migration.
- Verify with `/enchant stats` + the character window, which shows IncreasedMaxMana, IncreasedManaRegen, IncreasedMaxMinions,
  IncreasedMinionDamagePercentage, ReducedManaReservationPercentage, IncreasedMinionAttackSpeed and
  IncreasedMinionCritChance (`CharacterStatsWindow.cs:89-125`).
