# Enchanting: balance model

Status: design proposal (2026-11-04). Nothing here is implemented yet. Numbers marked *tune* are starting points.

**Question (Sid):** Should enchant strength be its own axis, separate from item level? If it is, a flat
"+10%" cannot stay additive. On a level-3 iron bow it is huge, and on a level-20 bow in a level-20 build it hardly matters.

**Short answer:** Yes. The enchant tier should set a **multiplier on the player's final stat**, not points added
to a shared bucket. The game already has a few multiplicative paths (defence, bosses, max HP%, mining%), and
enchants that use them already behave this way. Damage and attack speed have no such path. For those,
recalculate the raw value on the server each tick from the player's current bucket (Model B). Flat-magnitude procs (burn, lifesteal,
thorns) have no bucket to be relative to, so scale them by item level using the vanilla curve (Model A).
Keep costs flat (section 5).

---

## 1. How the game combines bonuses

Source roots:
- `dec/` = `C:\Users\Sid\AppData\Local\Temp\claude\C--Users-Sid\c4e37e1d-8139-4408-aa62-e0e32d08153a\scratchpad\dec`
  (Pug.Other, Pug.Base).
- `dec2/` = `C:\Users\Sid\AppData\Local\Temp\claude\C--Users-Sid\2c248440-3a94-4cb0-8014-5a1df2f02c15\scratchpad\dec`
  (decompiled for this doc: `CondExt/ConditionExtensions.cs` from Pug.ECS.ConditionExtensions.dll,
  `Auth/WeaponDamageAuthoring.cs` from Pug.ECS.Authoring.dll, `Conv/LevelEntitiesBufferConverter.cs`).

### 1.1 Summing

- Every source is summed by plain integer addition into two arrays: per `ConditionID` (`SummarizedConditionsBuffer`) and
  per `ConditionEffect` (`SummarizedConditionEffectsBuffer`).
  `dec/Pug.Other/SummarizeConditionsSystem.cs:437-453`.
  - `ConditionsBuffer` entries (where our enchants live) are added raw. `valueMultiplier` is **not** applied, so
    our `Marker` 0.4242 is harmless (`:136-152`).
  - Equipped gear values come from the item's level entity. Reinforced gear adds +15% (`:288-321`).
- The summary runs only when an input buffer changed (`:115`). It is `[BurstCompile]` and runs on **both server and client**,
  `OrderFirst` in `BeforePredictedFixedStepSimulationSystemGroup` (`:16-18`). So a value we write in tick N is
  summarized at the start of tick N+1. Clients build their own summary from the replicated `ConditionsBuffer`.
- Caps applied after summing (`:389-401`): Dodge 90 (100 with one special condition), DamageTakenPercentage upper 900
  (**no lower cap**), ReducedDamageFromBosses 90, ReducedDamageFromExplosions 90, CritChance 100.

### 1.2 Outgoing damage

`EntityUtility.CalculateDamage`, `dec/Pug.Other/EntityUtility.cs:1625`. The final line is `:2132`:

```
damage = (base + flatIncrease)
       × critMult                         // 1.5 + (CritDmg% + CritDmgVsBurning)/100 on crit   (:2031-2032)
       × (1 + damageIncreasePercentage/1000)   // bucket D (tenths of a percent)
       × (1 + conditional/100)                 // bucket C (whole percent)
       × num3                                  // ×3 on triple-damage proc (:1731), ×2 vs Vulnerable (:1735)
```

- **Bucket D** (`GetAttackerDamageIncrease`, `:2264-2296`) = AllDamage(effect 33) + PhysicalMelee(8) + MeleeDamage(116)
  for melee, or AllDamage + PhysicalRange(22) + RangeDamage(117) for ranged, or + MagicDamage(102).
  **`AllDamageIncrease` is in the same additive bucket. It is not a separate multiplier.** Sharpness, every ring,
  necklace and weapon affix, skill talents, food and potions all go here. In Sid's late build this bucket was +47%.
- **Bucket C** is built at `:1691-1720`. It holds DamagePercentageAgainstBosses(55) (only when the target is a boss),
  DamageBasedOnTargetRemainingHealth(87), DamageAgainstPoisoned(61), DamageAgainstStunned(85),
  DamageAgainstBurning(72), plus two magic-only terms. Most builds have this bucket near zero, so anything added
  here is already close to a true multiplier. **Bossbane uses it.**
- **Crit:** chance = CritChance(10), a flat percent capped at 100 (`:1739`, `:2031`). The multiplier is a separate factor.
- **Triple damage:** `rnd < ChanceToDealTripleDamage(86)/100` gives ×3 (`:1731-1734`). This is an independent
  multiplicative path. Expected value is ×(1 + 2c).
- Attack speed: cooldown × 1/(1 + (MeleeAttackSpeed(40) + AttackSpeed(65))/1000)
  (`dec/Pug.Other/MeleeAttackStateSystem.cs:384`, ranged `RangeAttackStateSystem.cs:407` with effect 41).
  This is one additive bucket. The attack rate scales by (1 + sum).
- Mining (tiles): `(base + Mining(7)) × (1 + MiningPercentage(38)/100)` (`EntityUtility.cs:1686-1690`). Here
  MiningPercentage is a separate multiplier.

### 1.3 Incoming damage (receiver)

`EntityUtility.cs:1658-1667`, `:2133-2152`:

```
armor   = Armor(9) × (1 + ArmorPercentage(37)/1000)        (+ movement-speed armor term)
dmg    -= 0.75 × min(dmg, armor)                            // flat subtraction, nonlinear in value
dmg    ×= (1000 + DamageTakenPercentage(97)) / 1000         // separate multiplier; negative = less damage
dmg    ×= (100 − ReducedDamageFromBosses(29)) / 100         // separate multiplier, boss attackers only
dodge  = DodgeChance(13) % (cap 90)                         // before everything (:1675)
```

- Max HP = (100 + MaxHealth(5) + MaxHealthPermanent(68)) × (1 + MaxHealthPercentage(34)/100)
  (`dec/Pug.Other/CharacterStatsWindow.cs:276-282`). Vanilla gear gives a **constant 15** for MaxHealthPercentage
  at every level (`dec2/CondExt/ConditionExtensions.cs:372`), so this bucket is small and percentage HP is already
  close to multiplicative.

### 1.4 How vanilla scales stats with item level

- Item base damage: `(10 + 10·L) × dmgMult × cooldownFactor × (1.7 melee | 1.2 ranged) × rand(0.95-1)`
  (`dec2/Auth/WeaponDamageAuthoring.cs:63-71`). From level 3 to level 20 it goes 40 to 210, a 5.25× increase.
- Gear condition values per level: `value = round(f(L) × valueMultiplier × rand(0.9-1.1) / divider)`
  (`dec2/CondExt/ConditionExtensions.cs:187`, final scaling `:824-836`). The divider is the number of
  non-negative conditions on the item, with exceptions (`:155-185`). Each of the 21 levels (0-20) gets a precomputed level entity
  (`dec2/Conv/LevelEntitiesBufferConverter.cs:78-140`). The max level is Excavation(16) + Legendary(4) = 20
  (`dec/Pug.Base/LevelScaling.cs:57-60`).

| Effect | f(L) (raw units) | L3 | L20 | Line |
|---|---|---|---|---|
| PhysicalMelee/RangeDamage % | 30 + 20L (tenths) | 9.0% | 43.0% | `:248` |
| MeleeDamage / RangeDamage % | 25 + 18L | 7.9% | 38.5% | `:252` |
| AllDamage % | 20 + 15L | 6.5% | 32.0% | `:259` |
| Melee/Range attack speed % | 20 + 4L (tenths) | 3.2% | 10.0% | `:406` |
| ArmorPercentage % | 10 + 50L (tenths) | 16% | 101% | `:280` |
| CritChance % | 3 + 0.7L | 5 | 17 | `:283` |
| DodgeChance % | 3 + 0.7L | 5 | 17 | `:300` |
| DamageTakenPercentage | −10·round(5 + 0.7L) | −7% | −19% | `:357` |
| ReducedDamageFromBosses % | 5 + 0.7L | 7 | 19 | `:351` |
| DamagePercentageAgainstBosses % | 2L | 6 | 40 | `:431` |
| MaxHealthPercentage % | 15 (constant) | 15 | 15 | `:372` |
| ApplyBurning | 10 + 3L | 19 | 70 | `:308`, `:838` |
| LifeOnHit (hp) | max(1, 0.5L) | 2 | 10 | `:324` |
| ThornsDamage | 1 + 2L | 7 | 41 | `:336` |
| MiningPercentage % | 1 + 2L | 7 | 41 | `:245` |
| Mining (flat) | GetMiningDamage(L) | ~22 | ~1268 | `:239`, `:853` |

These are before valueMultiplier and divider. The "+48.8% physical melee" on Sid's level-19 Battle Axe fits
(30 + 20·19) = 410 × valueMultiplier × deviation, × 1.15 if reinforced.

### 1.5 What this means for enchants

- An enchant in a **summed bucket** gives a relative gain of `Δ / (1 + B)`, where B is everything else in the bucket.
  B grows with level because every vanilla % stat grows about 5× from L3 to L20 (table above). A fixed Δ therefore
  shrinks in value as the game goes on. Measured: Sharpness III (+15 points) with B = +47% gave 1.62/1.47 = **+10.2%**
  (Battle Axe 1331.5 vs 1208.4 average).
- Compared with what an item level gives, a fixed +15% is about 1.7 vanilla affixes at L3 and about 0.35 at L20.
  That is why it feels huge early and small late.
- **Flat magnitudes** (burn 30, lifesteal 6 hp, thorns 30, Efficiency +30 flat mining) are worse. They lose against
  stats that grow 5-60× (Mining goes 22 to 1268).
- Enchants that already sit in an empty or separate bucket keep their value: Bossbane (bucket C),
  Bosswarden (boss-damage multiplier), Vitality (MaxHealthPercentage is small in vanilla), Evasion and Keen (flat chances).

---

## 2. Reference scenarios used below

| Scenario | Item level | Damage bucket D | Attack-speed bucket | Notes |
|---|---|---|---|---|
| **E**: iron bow | 3 | +5% (50 raw) | 0 | early skill points only |
| **L1**: Sid's level-20 build | 20 | +47% (470 raw), measured | ~+40% (assumed) | gear + skills + food |
| **L2**: L1 + buff potions/food | 20 | +100% (1000 raw), assumed | ~+60% | burst window |

The Enchanting Table is crafted at the Galaxite Workbench and costs Octarine, so real enchanting starts late in the
game. Scenario E matters only for players who enchant old gear, or if the table moves earlier.

---

## 3. Candidate models

### Model A: scale enchant values by item level (vanilla curve)

**Formula:** `raw(L, tier) = round(k[tier] × f_vanilla(L))`, where f is the vanilla curve for that effect
(section 1.4) and k is a tier fraction, e.g. 0.15 / 0.30 / 0.45 of a vanilla affix.

**Examples (damage, III, k = 0.45):**

| | E (L3, B 5%) | L1 (L20, B 47%) | L2 (L20, B 100%) |
|---|---|---|---|
| Today, flat +150 raw | +14.3% | +10.2% (measured) | +7.5% |
| Model A, 0.45 × (30 + 20L) | 41 raw → **+3.9%** | 194 raw → **+13.2%** | **+9.7%** |

- **Hook:** `EnchantUnits.Raw(def, level)` gains an item-level argument. `EnchantEffectSystem.AddFrom` already has
  `item.objectData`. The item level is `variation > 0 ? min(variation, 20) : LevelCD.level`, the same lookup as
  `SummarizeConditionsSystem.cs:292`. It is deterministic, reads nothing back, and cannot loop.
- **Multiplayer:** no new risk. The value changes only when the held or worn item changes.
- **Tooltip:** a concrete number such as "Sharpness III: +19.4% physical melee damage", using the game's own condition text.
  Upgrading the item at the forge raises it.
- **Downside:** it ties enchant strength **to item level**, which is the coupling Sid wants to avoid. The relative gain
  still depends on the rest of the bucket (+13% vs +9.7% between L1 and L2). Low-level items get almost nothing.
- **Best for:** flat-magnitude procs (burn, lifesteal, thorns), where no bucket exists to be relative to.

### Model B: enchant as a true multiplier on the current bucket (recommended for damage and attack speed)

**Formula:** we want the final factor to be `(1 + B/1000) × (1 + p)`. Write one raw value into the same bucket:

```
others = Σ effects in bucket (from SummarizedConditionEffectsBuffer) − ourValueAtSummaryTime
others = max(0, others)                         // a debuff must not make the enchant smaller than ×(1+p) on 1000
raw    = round(p × (1000 + others))            // tenths-of-a-percent buckets
```

Then `1 + (others + raw)/1000 = (1 + others/1000)(1 + p)`. This is exact apart from rounding to 1 raw unit (0.1%).

**Examples (damage, III, p = 0.12):**

| | E | L1 | L2 |
|---|---|---|---|
| raw written | 126 | 176 | 240 |
| actual gain | **+12.0%** | **+12.0%** | **+12.0%** |

Attack speed III (p = 0.10): E raw 100 → +10%. L1 raw 140 → +10%. Today's flat +150 gives +15% at E and +10.7% at L1.

**Hook (fits `EnchantEffectSystem`):**
- Add `Mode = Additive | Relative` and a bucket definition to `EnchantDef`:
  - melee damage → effects {33, 8, 116}, written as `PhysicalMeleeDamageIncrease`
  - ranged damage → {33, 22, 117}, written as `PhysicalRangeDamageIncrease`
  - melee attack speed → {40, 65}, written as `MeleeAttackSpeedIncrease`; ranged → {41, 65}, written as `RangeAttackSpeedIncrease`
  - armor percent (only if Protection stays on armor) → {37}
- In `Collect`, sum p per bucket (not raw). In `Apply`, read `SummarizedConditionEffectsBuffer` for the player
  (server world) and compute `raw`.
- **Feedback loop:** the summary already includes our own entry. Subtract the value that was in our
  `ConditionsBuffer` entry **when the summary was built**. The summary runs OrderFirst in
  `BeforePredictedFixedStepSimulationSystemGroup`, and we run after `PredictedSimulationSystemGroup`, so on a normal
  tick it reflects our previous write. That group is fixed-step and can skip or repeat on a frame, so keep a
  per-player `lastWriteTick` and recalculate only when `serverTick > lastWriteTick`. Then
  `ourValueAtSummaryTime = lastWrittenRaw`. This settles in one step: the new raw value does not depend on our own old value.
- **Hysteresis:** rewrite only when `|newRaw − currentRaw| ≥ max(2, 2% of currentRaw)`, at most every ~0.25 s
  per player. Stacking buffs such as `MeleeDamageIncreaseFromHitting` (one stack per 20 raw) change B on every hit,
  and each `ConditionsBuffer` write triggers a new summary plus a ghost update.
- Rewrite only the entry that changed. Today `Apply` removes and re-adds all of our entries.
- Do **not** Harmony-patch `CalculateDamage` or the summarizer. Both are Burst-compiled and a patch would never run.

**Multiplayer and sync risks:**
- All logic stays on the server. Clients receive the raw value through `ConditionsBuffer` and summarize it themselves.
  After a buff changes, the client's attack-speed prediction (`MeleeAttackStateSystem`) can be one tick behind:
  a tiny mispredict, which is acceptable.
- Bandwidth: one small buffer change per bucket change, throttled. Not a concern for 1-4 players.
- Each player is calculated from their own summary, so one player never affects another.

**Tooltip:** do not show the raw number, because it changes with buffs. Show the multiplier:
"Sharpness III: ×1.12 melee damage (multiplies with all other bonuses)". Build it client-side from p, not from the
condition. The Character Stats window keeps showing the true summed % (for example 47% → 64.6%), which is correct
because `(1 + B)(1 + p) = 1 + B + p(1 + B)`.

### Model C: use a multiplicative path the game already has

Use a different `ConditionID` whose effect is applied as its own factor (section 1.3):

| Use | Condition (effect) | Path | Formula |
|---|---|---|---|
| Defence (replaces Protection) | `ReducedDamageTakenPercentage` (DamageTakenPercentage 97) | `×(1000 + v)/1000` after armor | v = −30/−50/−80 per piece |
| Boss defence (Bosswarden) | `ReducedDamageFromBosses` (29) | `×(100 − v)/100` | already used |
| Boss damage (Bossbane) | `IncreasedDamageAgainstBosses` (55) | bucket C | already used |
| Burst damage (optional new enchant) | `ChanceToDealTripleDamage` (86) | ×3 on proc | expected ×(1 + 2c) |
| Mining (Efficiency) | `MiningPercentageIncrease` (38) | `×(1 + v/100)` | replaces flat `MiningIncrease` |

**Defence example:** an enemy hit of 600 against 300 armor deals 600 − 225 = 375. Today's Protection III (+15% armor)
makes that 341 (−9%). Against a 2000 hit it goes 1775 → 1741 (−1.9%). The value swings with enemy damage, and late
gear can carry +100% ArmorPercentage, which dilutes it further. `ReducedDamageTakenPercentage` −8% gives −8% in every case.

- **Hook:** change the `ConditionID` in `Enchants.cs`. The values stay additive and static, so there is no feedback
  and nothing new to sync.
- **Risks:**
  - DamageTakenPercentage has **no lower cap** in vanilla (`:389-401`, upper 900 only). Clamp our total to ≥ −250
    (−25%) in `Collect`.
  - **Verify in game:** ID 248 must map to effect 97, and the sign must be negative = less damage
    (`/enchant stats` while taking hits). The conditions-table dump failed this session (UnityPy TypeTreeGenerator could
    not load the DLL), so the mapping comes from names and `ConditionExtensions.cs:357`, not from the table.
  - Triple damage is high-variance and some vanilla legendaries use it. Keep it as an optional extra enchant,
    not a replacement for Sharpness.
- **Tooltip:** the game's own text for these conditions reads naturally ("−8% damage taken").

### Model D: keep additive values and tier by game stage

Keep the flat values, but gate higher tiers or raise the numbers by bench or biome (for example the III tier requires
the table plus Scarab/Galaxite materials).

- **Hook:** only roll and cost rules change.
- **Effect:** it does not solve the problem. A +15 that is fair at one stage is still wrong at the next, and
  enchanted gear carried into a later stage still loses value. It also makes enchanting an extra progression track to tune.
  **Rejected** as the main model. Section 5 explains why costs stay flat.

---

## 4. Recommendation

Enchant tier is its own axis and should act as a **multiplier**:

| Category | Enchants | Model | I / II / III *tune* | Notes |
|---|---|---|---|---|
| **Damage %** | Sharpness, Power | **B** (relative) | **×1.04 / ×1.08 / ×1.12** | Only one held weapon, so max ×1.12. Today III gives +10.2% late, so this is a small buff late and a small nerf early. |
| **Attack speed** | Swiftblade, Quick Draw | **B** | **×1.03 / ×1.06 / ×1.10** rate | Bucket {40 or 41, 65}. |
| **Crit** | Keen | flat chance | **3 / 6 / 9 %** (unchanged) | A chance is not diluted the way a bucket is. Cap 100. Vanilla L20 affix about 17. |
| **Boss damage** | Bossbane | flat, bucket C (already multiplicative) | **5 / 10 / 15 %** (unchanged) | Bucket C is near-empty in most builds. |
| **Defence** | Protection → rename **Resilience** | **C** `ReducedDamageTakenPercentage` | **−3 / −5 / −8 %** damage taken per piece | 3 pieces max −24%. Clamp our total at −25%. Verify mapping and sign first. |
| | Vitality (MaxHP%) | flat (bucket already small) | **3 / 6 / 10 %** per piece (was 5/10/15) | 3 × III = +30% HP, which is plenty next to vanilla's 15 per item. |
| | Bosswarden | flat, multiplicative path | **3 / 6 / 9 %** (unchanged) | Cap 90 in vanilla. |
| | Evasion | flat chance | **2 / 4 / 6 %** (unchanged) | Cap 90. Each point is worth more as dodge rises, so keep it small. |
| | Regeneration | flat | 10 / 20 / 30 % (unchanged) | Already a % of regeneration. |
| **Flat procs** | Fire Aspect (ApplyBurning) | **A**, k × (10 + 3L) | **0.5× / 0.75× / 1.0×** vanilla → L20: 35 / 53 / 70; L3: 10 / 14 / 19 | Burn value is applied as is (`EntityUtility.cs:1805-1817`). |
| | Lifesteal (LifeOnHit) | **A**, k × max(1, 0.5L) | **0.5× / 1.0× / 1.5×** → L20: 5 / 10 / 15 hp; L3: 1 / 2 / 3 | |
| | Thorns | **A**, k × (1 + 2L) | **1× / 2× / 3×** → L20: 41 / 82 / 123; L3: 7 / 14 / 21 | Melee attackers only (`:1939-1943`). |
| **Chance procs** | Knockback, Venom, Stunning, Frost, Stunshot | flat chance | Knockback 10/20/30; Venom 10/20/30; **Stunning 3/6/9, Stunshot 3/6/9** (were 5/10/15); Frost unchanged | Vanilla stun affix is a flat 5. 15% stun on every hit can stun-lock. Poison and burn damage come from the game. |
| **Tools** | Efficiency | **C** → `MiningPercentageIncrease` | **10 / 20 / 30 %** | Flat +30 against ~1268 L20 mining damage is nothing (`:853`). |
| | Unbreaking, Mending, Fortune | flat utility | unchanged | No dilution. |
| **Movement** | Swiftness (pants) | flat | 3 / 6 / 9 % (unchanged) | One piece only. The movement bucket rarely gets large. |
| *Optional new* | "Brutal" (melee/ranged) | **C** `ChanceToDealTripleDamage` | 2 / 4 / 6 % → expected ×1.04 / 1.08 / 1.12 | Only if Sid wants a spiky alternative to Sharpness. |

**Fallback:** if the B feedback logic seems too fiddly for v1, ship damage and attack speed on **Model A** with
k = 0.15 / 0.30 / 0.45 (L20 III ≈ +13%). Later, swap in B without changing saves: only raw values change, and
the stored data is just `(id, level)`.

### Implementation order

1. Model C swaps (Protection → `ReducedDamageTakenPercentage`, Efficiency → `MiningPercentageIncrease`) and
   value retunes. These only touch `Enchants.cs`. Verify each with `/enchant stats`.
2. Model A for Fire Aspect, Lifesteal and Thorns: item level in `EnchantUnits.Raw`.
3. Model B for Sharpness, Power, Swiftblade and Quick Draw in `EnchantEffectSystem`, plus multiplier tooltips.
   Re-run the Battle Axe dummy test. Pass if the average ratio is 1.12 ± 0.01 with and without a food buff.

### Unrelated bug risk found while reading

`EnchantUnits.ScaleOf` (`Scripts/EnchantStorage.cs:133-152`) reads `showDecimal` from `Manager.ui`. On a
**dedicated server** there is no UI, so it returns 1. The tenths-based conditions (damage %, attack speed, armor %,
movement, DamageTaken) would then be **10× too small** on dedicated servers. Hard-code the scale per enchant
(`EnchantDef.Scale`), or read `showDecimal` from the server's conditions table blob.

---

## 5. Should costs scale with item level?

**No. Keep costs flat per roll**, scaled by how many enchants the item already has, as now:
5/10/15 Gold Bars + 250/500/750 Ancient Coins.

- Under Models B and C the enchant's value does not depend on item level, so a level-scaled price has no value to
  follow.
- Enchants survive forge upgrades (SPEC "survive... upgrades"). If price grew with level, players would enchant at
  level 1 and then upgrade, so the scaling would only punish people who don't know the trick. The same applies under
  Model A. The forge already charges for levels, so level scaling is already priced in there.
- The table is a Galaxite-bench, Octarine-cost station, so early-game abuse is already limited by access.
- If late-game gold feels too cheap, raise the **coin** part across the board or add one late material at tier
  III. Do not tie price to item level.
