# Enchanting: combo and stacking audit

Status: analysis, 2026-11-04. Nothing in `Scripts/` was changed. Numbers come from
[`balance_calc.py`](balance_calc.py) (`python docs/balance_calc.py` prints every table below).

**Bottom line.** Offence is fine: the best melee build is +41-44% DPS over vanilla, +53% against bosses,
and the gain is the same early and late because Model B works. **Defence is not fine.** Armor sums
Resilience, Vitality and Evasion over 3 pieces, and the offhand adds a 4th copy. The best all-round build
gets **+134% to +142% effective HP** (+175% to +215% against bosses), 2.3x the +60% threshold. Magic also
goes over (+59-63% DPS) because of Spellsurge. Six per-condition caps on our own total, plus two value
changes, bring every build under threshold. The one exception is EHP against bosses at +57-62%, which I'd accept
(4.3). Typical builds barely change: late EHP goes from +36% to +31%, and DPS is unchanged.

## 1. Method

### 1.1 What the model computes

`balance_calc.py` uses the game's own combine rules (citations from `docs/balance-model.md` and the
decompiled sources):

| Stat | Formula | Source |
|---|---|---|
| Hit damage | `base x (1 + D/1000) x crit x (1 + C/100)`, base = (10 + 10L) x 1.7 | `EntityUtility.cs:2132`, `WeaponDamageAuthoring.cs:63-71` |
| Crit factor | `1 + min(cc,100)/100 x (0.5 + cd/100)` | `EntityUtility.cs:2031-2032`, cap `SummarizeConditionsSystem.cs:403` |
| Attack rate | `hits/s x (1 + AS/1000)` | `MeleeAttackStateSystem.cs:384` |
| DPS | hit x rate | |
| Max HP | `flatHP x (1 + HP%/100)` | `CharacterStatsWindow.cs:276-282` |
| Damage taken | `x (1000 + DT)/1000`, DT upper cap 900, **no lower cap** | `EntityUtility.cs:2133-2152`, `SummarizeConditionsSystem.cs:400` |
| Boss damage taken | `x (100 - BW)/100`, BW cap 90 | `:401` |
| Dodge | flat %, cap 90 (100 with condition 320) | `:393-399` |
| Effective HP | `maxHP / dmgTaken / (1 - dodge) / bossFactor` (armor is untouched by enchants, so it cancels out of every ratio) | |
| Move speed | `1 + MS/1000`, **only a lower cap (-900)** | `:398` |
| Regen | `(HoT + maxHP x pct/1000) x (1 + RegenEff/100)` per second | `HealFromConditionsSystem.cs:173-175` |
| Lifesteal | `LifeOnHit` hp per **melee hit, per target** | `EntityUtility.cs:2211-2223` |
| Spellsurge | on crit, +value x 10 raw magic damage for 6 s (refreshes) | `EntityUtility.cs:2049-2057` |

Model B (Relative) enchants are simulated the way `EnchantEffectSystem` does it: one entry per write
condition, `raw = p x (1000 + bucket - own entry)`, iterated to the fixed point. Sharpness III alone comes out at
exactly x1.1200. Sharpness III + Might III gives x1.1578, slightly above the product (x1.1536), because each entry
counts the other as part of its bucket. That difference is negligible.

Stacking rules modelled:
- **held** weapon: Sum, one item.
- **armor** (helm, chest, pants): Sum per condition (today's `AddItem`).
- **accessories** (ring1, ring2, necklace, offhand): the planned `Max` per enchant id, so only the highest copy counts.
- **offhand** Vitality, Resilience and Swiftness: summed with the armor copies (the implementing agent's
  stated plan: "accessory Max is separate from armor Sum").
- Today's clamp: our `ReducedDamageTakenPercentage` total >= -250 (-25%).

### 1.2 Vanilla baselines

The late stage is anchored on Sid's measured L20 build (+47% damage bucket). The other values are a
reasonable mid-roll vanilla kit from the curves in balance-model.md section 1.4 (gear, skills, food). Only the
ratios matter, and the conclusions survive +/-50% changes to these inputs (see 4.3).

| Stage | Item L | Dmg bucket | Atk-speed bucket | Crit % | Crit dmg | Boss C | Flat HP | HP% | Dmg taken | Dodge | Move | Bosswarden | Regen hp/s | Lifesteal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| early (L5) | 5 | +10% | +5% | 5 | +0% | +0% | 300 | +0% | +0% | 3% | +0% | 0% | 5 + 0.0%max | 0/hit |
| mid (L12) | 12 | +30% | +20% | 12 | +10% | +10% | 600 | +15% | -10% | 8% | +5% | 8% | 20 + 0.3%max | 3/hit |
| late (L20) | 20 | +47% | +40% | 20 | +25% | +20% | 1000 | +30% | -19% | 12% | +10% | 19% | 40 + 0.5%max | 6/hit |

### 1.3 Builds

- **typical**: 1-2 enchants per item, all at II. Weapon Sharpness + Swiftblade; helm Vitality + Evasion;
  chest Resilience + Regeneration; pants Swiftness + Resilience; ring1 Might + Gourmet; ring2 Precision +
  Angler; necklace Hardiness; offhand Resilience.
- **best**: every slot has 3 enchants at III. The weapon is brute-forced over all 3-of-pool picks for DPS, and
  armor over every helm, chest and pants combination for EHP. Accessories carry every Max-stacked combat
  stat once, and the offhand carries Resilience, Vitality and Swiftness. The optimizer converges on the same
  build at every stage:
  - weapon Sharpness/Swiftblade/Keen (vs boss: Bossbane instead of Keen; magic: Arcane/Quick Draw/Spellsurge)
  - Resilience/Vitality/Evasion III on **all three** armor pieces (vs boss: Bosswarden replaces Evasion on 1-3 pieces)
  - ring1 Might/Haste/Precision, ring2 Ferocity/Hardiness/Fleetfoot, necklace utility, offhand Resilience/Vitality/Swiftness
- **degenerate**: the named single-stat stacks from the brief, with the rest of the kit vanilla.

Thresholds (flags): DPS > +60%, EHP > +60%, move > +25%, dodge >= 60% or at cap, crit at 100%, and
sustain > 5% max HP/s **and** more than 2x vanilla.

## 2. Results with the current rules (+ planned accessory Max)

#### Current rules, early (L5)

| Build | DPS | EHP | Move | Crit % | Dodge % | Dmg taken | Max HP | Sustain hp/s (%max/s) | Flags |
|---|---|---|---|---|---|---|---|---|---|
| vanilla | +0% | +0% | +0% | 5 | 3 | +0% | 300 | 5 (1.7%) | - |
| typical (II, 1-2 per item) | +18% | +33% | +6% | 7 | 7 | -15% | 324 | 6 (1.9%) | - |
| best all-round (III x3, optimized) | +41% | +134% | +12% | 17 | 21 | -25% | 429 | 5 (1.2%) | EHP |
| best vs boss (III x3, optimized) | +54% | +175% | +12% | 8 | 9 | -24% | 429 | 5 (1.2%) | EHP |
| best magic (III x3, optimized) | +59% | +134% | +12% | 17 | 21 | -25% | 429 | 5 (1.2%) | EHP |
| Evasion III x3 armor | +0% | +23% | +0% | 5 | 21 | +0% | 300 | 5 (1.7%) | - |
| Evasion III x3, dodge-built vanilla (40%) | +0% | +43% | +0% | 5 | 58 | +0% | 300 | 5 (1.7%) | - |
| Resilience III x3 + offhand | +0% | +33% | +0% | 5 | 3 | -25% | 300 | 5 (1.7%) | - |
| Vitality III x3 + offhand + Hardiness | +0% | +43% | +0% | 5 | 3 | +0% | 429 | 5 (1.2%) | - |
| Tank: Res+Vit+Eva III on all 3 + offhand Res/Vit + Hardiness | +0% | +134% | +0% | 5 | 21 | -25% | 429 | 5 (1.2%) | EHP |
| Boss tank: Res+Vit+Bosswarden x3 + offhand (vs boss) | +0% | +161% | +0% | 5 | 3 | -25% | 429 | 5 (1.2%) | EHP |
| Speed: Swiftness pants + offhand + Fleetfoot | +0% | +0% | +21% | 5 | 3 | +0% | 300 | 5 (1.7%) | - |
| Crit: Keen + Precision + Ferocity | +7% | +0% | +0% | 17 | 3 | +0% | 300 | 5 (1.7%) | - |
| Crit cap probe: same, vanilla crit 85% | +8% | +0% | +0% | 97 | 3 | +0% | 300 | 5 (1.7%) | - |
| Melee DPS: Sharp+Swift+Keen + Might/Haste/Prec/Fero | +41% | +0% | +0% | 17 | 3 | +0% | 300 | 5 (1.7%) | - |
| Boss DPS: Sharp+Swift+Bossbane + accessories (vs boss) | +54% | +0% | +0% | 8 | 3 | +0% | 300 | 5 (1.7%) | - |
| Magic: Arcane+QuickDraw+Spellsurge + accessories | +58% | +0% | +0% | 8 | 3 | +0% | 300 | 5 (1.7%) | - |
| Sustain: Lifesteal III + Regeneration III x3 | +14% | +0% | +0% | 5 | 3 | +0% | 300 | 19 (6.3%) | SUSTAIN |

#### Current rules, mid (L12)

| Build | DPS | EHP | Move | Crit % | Dodge % | Dmg taken | Max HP | Sustain hp/s (%max/s) | Flags |
|---|---|---|---|---|---|---|---|---|---|
| vanilla | +0% | +0% | +0% | 12 | 8 | -10% | 690 | 29 (4.2%) | - |
| typical (II, 1-2 per item) | +18% | +34% | +6% | 14 | 12 | -25% | 738 | 34 (4.6%) | - |
| best all-round (III x3, optimized) | +42% | +137% | +11% | 24 | 26 | -35% | 948 | 31 (3.3%) | EHP |
| best vs boss (III x3, optimized) | +53% | +188% | +11% | 15 | 26 | -34% | 828 | 31 (3.7%) | EHP |
| best magic (III x3, optimized) | +63% | +137% | +11% | 15 | 26 | -35% | 948 | 31 (3.3%) | DPS, EHP |
| Evasion III x3 armor | +0% | +24% | +0% | 12 | 26 | -10% | 690 | 29 (4.2%) | - |
| Evasion III x3, dodge-built vanilla (40%) | +0% | +43% | +0% | 12 | 58 | -10% | 690 | 29 (4.2%) | - |
| Resilience III x3 + offhand | +0% | +38% | +0% | 12 | 8 | -35% | 690 | 29 (4.2%) | - |
| Vitality III x3 + offhand + Hardiness | +0% | +37% | +0% | 12 | 8 | -10% | 948 | 30 (3.2%) | - |
| Tank: Res+Vit+Eva III on all 3 + offhand Res/Vit + Hardiness | +0% | +137% | +0% | 12 | 26 | -35% | 948 | 30 (3.2%) | EHP |
| Boss tank: Res+Vit+Bosswarden x3 + offhand (vs boss) | +0% | +169% | +0% | 12 | 8 | -35% | 948 | 30 (3.2%) | EHP |
| Speed: Swiftness pants + offhand + Fleetfoot | +0% | +0% | +20% | 12 | 8 | -10% | 690 | 29 (4.2%) | - |
| Crit: Keen + Precision + Ferocity | +8% | +0% | +0% | 24 | 8 | -10% | 690 | 29 (4.2%) | - |
| Crit cap probe: same, vanilla crit 85% | +9% | +0% | +0% | 97 | 8 | -10% | 690 | 29 (4.2%) | - |
| Melee DPS: Sharp+Swift+Keen + Might/Haste/Prec/Fero | +42% | +0% | +0% | 24 | 8 | -10% | 690 | 30 (4.4%) | - |
| Boss DPS: Sharp+Swift+Bossbane + accessories (vs boss) | +53% | +0% | +0% | 15 | 8 | -10% | 690 | 30 (4.4%) | - |
| Magic: Arcane+QuickDraw+Spellsurge + accessories | +63% | +0% | +0% | 15 | 8 | -10% | 690 | 30 (4.4%) | DPS |
| Sustain: Lifesteal III + Regeneration III x3 | +14% | +0% | +0% | 12 | 8 | -10% | 690 | 75 (10.8%) | SUSTAIN |

#### Current rules, late (L20)

| Build | DPS | EHP | Move | Crit % | Dodge % | Dmg taken | Max HP | Sustain hp/s (%max/s) | Flags |
|---|---|---|---|---|---|---|---|---|---|
| vanilla | +0% | +0% | +0% | 20 | 12 | -19% | 1300 | 63 (4.9%) | - |
| typical (II, 1-2 per item) | +18% | +36% | +5% | 22 | 16 | -34% | 1380 | 74 (5.4%) | - |
| best all-round (III x3, optimized) | +44% | +142% | +11% | 32 | 30 | -44% | 1730 | 68 (3.9%) | EHP |
| best vs boss (III x3, optimized) | +53% | +215% | +11% | 23 | 30 | -43% | 1530 | 67 (4.4%) | EHP |
| best magic (III x3, optimized) | +63% | +142% | +11% | 23 | 30 | -44% | 1730 | 68 (3.9%) | DPS, EHP |
| Evasion III x3 armor | +0% | +26% | +0% | 20 | 30 | -19% | 1300 | 63 (4.9%) | - |
| Evasion III x3, dodge-built vanilla (40%) | +0% | +43% | +0% | 20 | 58 | -19% | 1300 | 63 (4.9%) | - |
| Resilience III x3 + offhand | +0% | +45% | +0% | 20 | 12 | -44% | 1300 | 63 (4.9%) | - |
| Vitality III x3 + offhand + Hardiness | +0% | +33% | +0% | 20 | 12 | -19% | 1730 | 65 (3.8%) | - |
| Tank: Res+Vit+Eva III on all 3 + offhand Res/Vit + Hardiness | +0% | +142% | +0% | 20 | 30 | -44% | 1730 | 65 (3.8%) | EHP |
| Boss tank: Res+Vit+Bosswarden x3 + offhand (vs boss) | +0% | +189% | +0% | 20 | 12 | -44% | 1730 | 65 (3.8%) | EHP |
| Speed: Swiftness pants + offhand + Fleetfoot | +0% | +0% | +19% | 20 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Crit: Keen + Precision + Ferocity | +9% | +0% | +0% | 32 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Crit cap probe: same, vanilla crit 85% | +9% | +0% | +0% | 97 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Melee DPS: Sharp+Swift+Keen + Might/Haste/Prec/Fero | +44% | +0% | +0% | 32 | 12 | -19% | 1300 | 66 (5.0%) | - |
| Boss DPS: Sharp+Swift+Bossbane + accessories (vs boss) | +53% | +0% | +0% | 23 | 12 | -19% | 1300 | 66 (5.0%) | - |
| Magic: Arcane+QuickDraw+Spellsurge + accessories | +63% | +0% | +0% | 23 | 12 | -19% | 1300 | 66 (5.0%) | DPS |
| Sustain: Lifesteal III + Regeneration III x3 | +14% | +0% | +0% | 20 | 12 | -19% | 1300 | 155 (11.9%) | SUSTAIN |

## 3. Red flags (current rules)

Ranked by size. Percentages are against the vanilla build of the same stage.

1. **Stacked defence, +134% to +142% EHP (+175% to +215% against bosses).** The optimizer puts
   Resilience/Vitality/Evasion III on all three armor pieces plus Resilience/Vitality on the offhand. Each part
   on its own (late stage):
   - **Resilience** x3 + offhand = -32%, clamped to our -25%. It shares an additive bucket with vanilla's -19%,
     so the total reaches -44% damage taken: **+45% EHP from Resilience alone**. Even one Resilience III gives
     +11% late, more than its nominal 8%, because the bucket is additive (`(1000 + DT)/1000`) and vanilla has
     already used part of it.
   - **Vitality** x3 + offhand + Hardiness = +43% max HP on top of vanilla's +30%: **+33%** (+43% early).
   - **Evasion** x3 = +18 dodge: **+23-26%**. On a dodge-built vanilla kit (40%) the total reaches 58% dodge
     and +43% EHP. Each point is worth more as dodge rises, so stacking is the problem, not the per-piece value.
   - **Bosswarden** x3 = +27, plus vanilla 19 = 46% less boss damage: the boss tank build reaches +189% EHP.

   Cause: armor `Sum` over 3 pieces (`EnchantEffectSystem.AddItem`, `_flat[c] += value`), the offhand adding a
   4th copy, and no cap except the -25% Resilience clamp.
2. **Magic DPS +59% to +63%.** Spellsurge III (+30% magic damage for 6 s after a crit) is up about 95% of the
   time at 2+ casts/s with 20% crit, so it is effectively a permanent +30 points in bucket D. Arcane Power
   (Model B) then multiplies that larger bucket too. Arcane III + Spellsurge III = x1.34 on its own; Spellsurge
   accounts for about +20% of it.
3. **Sustain: Lifesteal III + Regeneration III x3 = 11.9% max HP/s late (vanilla 4.9%)**, 6.3% early (vanilla
   1.7%). Lifesteal III is 1.5 vanilla affixes (15 hp/hit at L20). `LifeOnHit` pays out per melee hit **per
   target** (`EntityUtility.cs:2211-2223`), so AoE swings multiply it beyond these numbers. Regeneration x3
   = x1.9 regen.
4. **Move speed: Swiftness pants + offhand Swiftness + Fleetfoot = +21% of our own** (+19-21% over vanilla).
   That is under the +25% threshold, but `MovementSpeed` has no upper cap (`SummarizeConditionsSystem.cs:398`,
   lower cap only), and gear-slot-enchants.md already planned a 15 total.
5. Not a problem:
   - **Crit**: Keen III + Precision III adds at most +12 crit chance, and the 100 cap is unreachable unless
     vanilla is already at 88% or more. Ferocity +6 crit damage is worth about +1% DPS.
   - **Melee DPS**: the best build is +41-44%, and against bosses (Bossbane in bucket C) +53-54%. Both are
     under +60% and flat across stages, as Model B intends.
   - **Typical builds**: +18% DPS, +33-36% EHP, +5% move.
   - **Might, Haste and Hardiness under Max**: one copy only, so they can't multiply across ring1, ring2,
     necklace and offhand.

## 4. Recommended fixes (ranked by impact)

### 4.1 Changes

| # | Change | Numbers | Effect (late, best build) |
|---|---|---|---|
| 1 | Cap our **Resilience** total (`ReducedDamageTakenPercentage`) | -25% → **-12%** (raw -250 → -120) | Resilience stack +45% → +17% EHP |
| 2 | Cap our **max HP%** total (`IncreasedMaxHealthPercentage`, Vitality + offhand Vitality + Hardiness) | **+15%** | Vitality stack +33% → +12% |
| 3 | Cap our **Dodge** total (`DodgeChance`) | **8** | Evasion stack +26% → +10%. Dodge-built kit 58% → 48% |
| 4 | Cap our **Bosswarden** total (`ReducedDamageFromBosses`) | **9** (one piece at III) | Boss tank +189% → +47% |
| 5 | **Spellsurge** values | 10/20/30 → **5/10/15** | Best magic +63% → +49% |
| 6 | Cap our **move speed** (`MovementSpeedIncrease`: Swiftness, offhand Swiftness, Fleetfoot) | **+12%** (raw 120) | +21% → +12% (+11% over vanilla) |
| 7 | Cap **Regeneration** (`IncreasedHealthRegenEffectiveness`) and retune **Lifesteal** | regen cap **+60%**; Lifesteal **50/100/150 → 50/75/100** % of the vanilla curve (III = one vanilla affix, 10 hp/hit at L20) | sustain 11.9 → 9.6 % max HP/s |

Note on 7: sustain stays the softest flag after the fix (9.6%/s against 4.9% vanilla, mostly regen from food,
which only Regeneration multiplies). The game has no per-swing limit for `LifeOnHit`, so if AoE leech still
feels strong in game, lower the values again (e.g. 35/50/75). Don't add a mechanic for it.

### 4.2 Result with all fixes

The full model output follows. With caps in place, the armor + offhand rule makes no difference: the same
numbers come out whether offhand copies sum with armor or only count when higher, because the caps bind first.
Keep the planned Sum.

#### Proposed rules, late (L20)

| Build | DPS | EHP | Move | Crit % | Dodge % | Dmg taken | Max HP | Sustain hp/s (%max/s) | Flags |
|---|---|---|---|---|---|---|---|---|---|
| vanilla | +0% | +0% | +0% | 20 | 12 | -19% | 1300 | 63 (4.9%) | - |
| typical (II, 1-2 per item) | +18% | +31% | +5% | 22 | 16 | -31% | 1380 | 74 (5.4%) | - |
| best all-round (III x3, optimized) | +44% | +44% | +11% | 32 | 20 | -31% | 1450 | 66 (4.6%) | - |
| best vs boss (III x3, optimized) | +53% | +62% | +11% | 23 | 20 | -31% | 1450 | 66 (4.6%) | EHP |
| best magic (III x3, optimized) | +49% | +44% | +11% | 23 | 20 | -31% | 1450 | 66 (4.6%) | - |
| Evasion III x3 armor | +0% | +10% | +0% | 20 | 20 | -19% | 1300 | 63 (4.9%) | - |
| Evasion III x3, dodge-built vanilla (40%) | +0% | +15% | +0% | 20 | 48 | -19% | 1300 | 63 (4.9%) | - |
| Resilience III x3 + offhand | +0% | +17% | +0% | 20 | 12 | -31% | 1300 | 63 (4.9%) | - |
| Vitality III x3 + offhand + Hardiness | +0% | +12% | +0% | 20 | 12 | -19% | 1450 | 64 (4.4%) | - |
| Tank: Res+Vit+Eva III on all 3 + offhand Res/Vit + Hardiness | +0% | +44% | +0% | 20 | 20 | -31% | 1450 | 64 (4.4%) | - |
| Boss tank: Res+Vit+Bosswarden x3 + offhand (vs boss) | +0% | +47% | +0% | 20 | 12 | -31% | 1450 | 64 (4.4%) | - |
| Speed: Swiftness pants + offhand + Fleetfoot | +0% | +0% | +11% | 20 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Crit: Keen + Precision + Ferocity | +9% | +0% | +0% | 32 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Crit cap probe: same, vanilla crit 85% | +9% | +0% | +0% | 97 | 12 | -19% | 1300 | 63 (4.9%) | - |
| Melee DPS: Sharp+Swift+Keen + Might/Haste/Prec/Fero | +44% | +0% | +0% | 32 | 12 | -19% | 1300 | 66 (5.0%) | - |
| Boss DPS: Sharp+Swift+Bossbane + accessories (vs boss) | +53% | +0% | +0% | 23 | 12 | -19% | 1300 | 66 (5.0%) | - |
| Magic: Arcane+QuickDraw+Spellsurge + accessories | +49% | +0% | +0% | 23 | 12 | -19% | 1300 | 66 (5.0%) | - |
| Sustain: Lifesteal III + Regeneration III x3 | +14% | +0% | +0% | 20 | 12 | -19% | 1300 | 125 (9.6%) | - |

#### Summary across rule sets

| Rules | Stage | best all-round | best vs boss | best magic | Lifesteal+Regen sustain (%max/s, vanilla) |
|---|---|---|---|---|---|
| current + planned accessory Max | early (L5) | +41% / +134% | +54% / +175% | +59% / +134% | 6.3 (1.7) |
| current + planned accessory Max | mid (L12) | +42% / +137% | +53% / +188% | +63% / +137% | 10.8 (4.2) |
| current + planned accessory Max | late (L20) | +44% / +142% | +53% / +215% | +63% / +142% | 11.9 (4.9) |
| proposed | early (L5) | +41% / +42% | +54% / +57% | +46% / +42% | 4.3 (1.7) |
| proposed | mid (L12) | +42% / +43% | +53% / +58% | +49% / +43% | 8.7 (4.2) |
| proposed | late (L20) | +44% / +44% | +53% / +62% | +49% / +44% | 9.6 (4.9) |
| alternative: Max across armor for Resilience/Vitality/Evasion/Bosswarden | early (L5) | +41% / +31% | +54% / +44% | +59% / +31% | 6.3 (1.7) |
| alternative: Max across armor for Resilience/Vitality/Evasion/Bosswarden | mid (L12) | +42% / +31% | +53% / +45% | +63% / +31% | 10.8 (4.2) |
| alternative: Max across armor for Resilience/Vitality/Evasion/Bosswarden | late (L20) | +44% / +31% | +53% / +47% | +63% / +31% | 11.9 (4.9) |

### 4.3 Robustness

Scaling every late-stage vanilla input (damage, speed, crit, boss %, HP%, damage taken, dodge, move, Bosswarden)
by 0.5x or 1.5x does not change the conclusions:

| Vanilla inputs | Current: all-round / boss / magic (DPS/EHP) | Proposed: all-round / boss / magic |
|---|---|---|
| x0.5 | +42/+135, +53/+188, +63/+135 | +42/+42, +53/+58, +49/+42 |
| x1.0 | +44/+142, +53/+215, +63/+142 | +44/+44, +53/+62, +49/+44 |
| x1.5 | +45/+155, +52/+259, +60/+155 | +45/+47, +52/+68, +48/+47 |

The proposed boss-tank EHP ends up slightly over +60% (+62-68%) on high-vanilla kits. It applies only
against bosses, which is where Bosswarden is meant to matter, so I'd accept it. Bosswarden cap 6 instead of 9
brings it to +52-56% if Sid wants it strictly under.

### 4.4 Alternative considered: `Max` across armor per enchant

Making Resilience, Vitality, Evasion and Bosswarden `Max` across the three pieces (like accessories) gives
+31% EHP (+44-47% against bosses), well under the threshold. **Not recommended.** Rolls are random and an item
can hold only 3 enchants, so a second Resilience on chest or pants would be a dead roll that the player paid for.
Caps keep every copy useful up to a ceiling (two pieces at II already reach about the cap), and they are
also the combined armor + accessory limit the new pool needs (4.5). It does not fix magic or sustain either.

### 4.5 Interaction with the accessory Max rule

Accessory `Max` (per enchant id over ring1, ring2, necklace and offhand) and armor `Sum` are separate groups, so
on their own they do **not** bound one stat fed from both groups: Swiftness on pants + Swiftness on the
offhand + Fleetfoot on a ring gives +21%, and Vitality x3 + offhand Vitality + Hardiness gives +43%. The
per-ConditionID caps in 4.1 are applied **after** both groups are folded into `_flat`, so they are the
combined cap. This only works if the accessory enchants write the **same ConditionID** as their armor
counterparts:

| Accessory enchant | Must write | Shares the cap with |
|---|---|---|
| Hardiness | `IncreasedMaxHealthPercentage` | Vitality |
| Fleetfoot | `MovementSpeedIncrease` (tenths) | Swiftness |
| Precision | `CritChance` | Keen (no cap needed: +12 max) |
| Ferocity | crit damage condition | none (no cap needed) |
| offhand Vitality / Resilience / Swiftness | same defs as armor (ids 62 / 60 / 67) | themselves |

If Hardiness ends up on flat `MaxHealth` instead, add a separate cap for it.

## 5. Exact code changes for the main session

### 5.1 `Scripts/Enchants.cs` (values only)

```csharp
Lvl(4, "lifesteal", "Lifesteal", EnchantSlot.Melee, ConditionID.LifeOnHit, ItemCurve.LifeOnHit, 50, 75, 100);   // was 50, 100, 150
Add(102, "spellsurge", "Spellsurge", EnchantSlot.Magic, ConditionID.GainMagicDamagePercentageBoostAfterCrit, 5, 10, 15); // was 10, 20, 30
```
Update the SPEC.md pool rows for ids 4 and 102 to match.

### 5.2 `Scripts/EnchantMath.cs`: replace `MinDamageTaken` with a cap table (our own total, raw units)

```csharp
/// <summary>Caps on our own folded total per condition (raw units). Vanilla gear, food and skills come on top.</summary>
public static readonly Dictionary<ConditionID, int2> Caps = new Dictionary<ConditionID, int2>
{
    { ConditionID.ReducedDamageTakenPercentage,      new int2(-120, int.MaxValue) }, // tenths: -12 %
    { ConditionID.IncreasedMaxHealthPercentage,      new int2(int.MinValue, 15) },   // whole %: Vitality + Hardiness
    { ConditionID.DodgeChance,                       new int2(int.MinValue, 8) },
    { ConditionID.MovementSpeedIncrease,             new int2(int.MinValue, 120) },  // tenths: Swiftness + Fleetfoot
    { ConditionID.IncreasedHealthRegenEffectiveness, new int2(int.MinValue, 60) },
    { ConditionID.ReducedDamageFromBosses,           new int2(int.MinValue, 9) },
};
```
(`int2` from Unity.Mathematics avoids relying on ValueTuple in the mod sandbox. A two-field struct works too.)
The gear-slot doc's planned caps for Provisions, Second Wind and so on go in the same table.

### 5.3 `Scripts/EnchantEffectSystem.cs` `Collect`: apply the table after **all** slots are folded

Replace

```csharp
if (_flat.TryGetValue(ConditionID.ReducedDamageTakenPercentage, out int dt) && dt < EnchantMath.MinDamageTaken)
    _flat[ConditionID.ReducedDamageTakenPercentage] = EnchantMath.MinDamageTaken;
```

with

```csharp
foreach (var cap in EnchantMath.Caps)
    if (_flat.TryGetValue(cap.Key, out int v))
        _flat[cap.Key] = Mathf.Clamp(v, cap.Value.x, cap.Value.y);
```

It must run after the new necklace, ring and offhand `AddFrom` calls and after the accessory `Max` fold,
so it caps armor and accessory contributions together. Keep the armor `Sum` and the offhand rule as planned.
Nothing outside `EnchantMath`/`Collect` references `MinDamageTaken`. Consider a tooltip or
`/enchant stats` hint such as "(capped at 12%)" so a player who stacks past the cap can see why.

### 5.4 Correctness notes for the accessory Relative enchants (Might, Haste)

These are not balance changes, but the model assumes they deliver exactly x1.03:

- `EnchantEffectSystem.AddItem` sets `RelWant.Weapon = item.objectID`. For a ring that is the **ring**, so:
  - `EnchantMath.WriteCondition` would always choose `MeleeAttackSpeedIncrease` for Haste
    (`UsesRangeCooldown(ring)` is false), which does nothing with a bow or staff held.
  - `BucketSum` would use the melee class effect for Might.

  Fix: either pass the **held** item's ObjectID as `Weapon` for accessory Relative enchants, or have Haste
  write `AttackSpeedIncrease` (effect 65, read by both cooldowns) with its bucket = held weapon's 40/41 + 65.
- If Might writes `AllDamageIncrease`, `EffectOf` returns `None`, and `BucketSum`'s Damage case then drops the
  weapon-type effect (PhysicalMelee 8, PhysicalRange 22 or Magic 102). Might would then multiply only part of the
  bucket, around x1.025 late instead of x1.03. Give it the held weapon's type effect.
- Might and Sharpness written as **separate** entries (different write conditions) compound to x1.158, not
  x1.154, because each counts the other as part of its bucket. That's harmless. If they wrote the **same**
  condition, the permilles would add instead (x1.15), which is also fine.
- Might does not reach minions: the minion bucket is MinionDamage plus the magic share, not AllDamage.

## 6. Re-running

```
python docs/balance_calc.py           # every table in this doc
python docs/balance_calc.py --brief   # only the best/tank/magic rows
```
Edit `STAGES` for other vanilla assumptions, `PROPOSED` for other caps or values, and `degenerate()` for
new stacks. When the accessory pool lands, check its real values and ConditionIDs against the `d(...)` lines
at the top.
