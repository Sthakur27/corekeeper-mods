# Enchanting — test plan

Goal: every enchant has an objective pass/fail, produced by the mod itself, readable from Player.log.
Three layers, cheapest first. Layers 1–2 exist (0.1.x); layer 3 and the new weapon classes are to do.

## Layer 1 — stat check (`/enchant selftest`, ~30 s, automatic)

For every enchant in the pools of the held item, worn helm/chest/pants and worn ring1/ring2/necklace/offhand, at
levels I and III:
put only that enchant on the item, let the game re-sum stats, compare the player's summed condition value
with the same item enchant-free. PASS = it rose by exactly the enchant's raw value, computed by the same
function the effect system uses (`EnchantMath.Raw`; item-level procs use the item's level). Multiplier
(Model B) enchants: our entry must equal `EnchantEffectSystem.RelativeRaw` (player's bucket without our entry,
within the 2% hysteresis), and both the condition total and the effect bucket must rise by exactly our entry.
Tomes: hold the tome and run it (held), then switch to another item with the tome still in the inventory and run
again: the carried tome is tested while not held (carried rule).

- Proves: the enchant reaches the game's stat system with the right ID, sign and units.
- Doesn't prove: that the stat does something in combat (Frost passed layer 1 while the game ignored it).
- Coverage: run once per weapon class held (melee, ranged, tool, + new classes) with armor worn.
- Result 2026-11-04: 62/62 PASS (Battle Axe + Miner helm/chest/pants). Bow pending.

### Accessories (2026-11-04, accessory pool ids 140-152)

Wear two rings, a necklace and a (non-bait) offhand, hold a melee weapon, `/enchant selftest`:
- Each slot's baseline step clears the enchants of **every** tested item (so a copy on another accessory can't hide
  a Max enchant under test); originals are restored at the end.
- Every accessory runs the shared pool (10 enchants × I/III); the offhand also runs offhand Vitality/Resilience/
  Swiftness. Might/Haste are multipliers on the held weapon's bucket: PASS as for Sharpness (`AllDamage` / `AttackSpeed`
  effect rises by exactly our entry). Radiance: the glow colour our entry went to rises by 1/3.
- **Max rule** (only with both rings worn): Precision III on ring1 + I on ring2 → CritChance +3 (not +4);
  III + III → +3 (not +6); Might III + III → our `AllDamageIncrease` entry = the x1.03 value (not x1.06). Lines start
  with "Max rule" and always go to `/enchant selftest status`.
- Then hold a bow and run Might/Haste again (`/enchant set might 3 ring1`): Haste must show "attack speed" and the
  stats window ranged attack speed must rise (bucket follows the held weapon).
- Player.log `[Enchanting] units:` must list no mismatches for AllDamageIncrease, AttackSpeed, CritChance,
  CriticalDamagePercentageIncrease, IncreasedMaxHealthPercentage, ExtraHarvestChance, IncreasedFishing,
  ChanceToGainExtraCookedFood or the glow conditions.
- Manual: tooltips on accessories end with "Doesn't stack: only your highest copy counts"; Radiance gives a small
  light without a lantern and a larger one with a lantern (same colour); Gourmet/Green Thumb/Angler by use.

## Layer 2 — combat check (`/enchant combattest [seconds]`, ~10 min per weapon at 20 s, hands off)

Stand next to a training dummy, hold the weapon. Runs plain → each pool enchant at III → plain again.
Each run: apply enchant, set player to half HP, wait until the dummy has no burn/poison/stun/slow
(min 6 s, max 25 s), then fake aim + attack for N seconds. Report per run: hits, average/min/max damage,
crit rate, % of hits with the target burning / poisoned / stunned / slowed, HP regained.

| Enchant | Pass when (vs the plain runs) |
|---|---|
| Sharpness, Power, Arcane Power | avg hit ≈ plain × 1.12 at III, with or without food buffs (Model B) |
| Bossbane | only vs a boss (manual, see layer 4) |
| Keen | crit rate +≈9 points (needs 60 s runs: `/enchant combattest 60`) |
| Swiftblade, Quick Draw | ≈ 10% more hits in the same time at III |
| Fire Aspect, Venom, Stunning, Frost, Stunshot | burning / poisoned / stunned / slowed % clearly above plain |
| Lifesteal | HP regained clearly above plain (regen is in both) |
| Knockback | not measurable on a dummy (manual) |
| plain first vs plain last | within ~5%; otherwise drift (stacking buffs) — rerun |

### Experimental 0.3.0 (Thunderstrike, Piercing, Flame, Static Familiars, Soul Siphon)

- Layer 1: all five are in the selftest. Static Familiars / Soul Siphon are checked on every summoned minion
  (our entry = expected, minion summary includes it); with no minions out they report SKIP: summon, then rerun.
- Layer 2 (`/enchant combattest` on a dummy): Flame on a bow/staff = the open question; PASS when burning % is
  clearly above plain (beams should always pass). Thunderstrike: Soul of Azeos enabled, 60 s runs; more hits /
  damage than plain. Piercing can't show on one dummy.
- Manual: Piercing, shoot through a line of 2+ enemies (each gets a damage number). Static Familiars: minions on
  a live enemy, look for stun stars. Soul Siphon: take some damage, let minions hit, HP climbs per minion hit.
  Ranged minions only stun if the game passes the stat on to their projectiles.

## Layer 3 — unattended runner (to do, reuses ModDiagnostics bench mode)

`python Enchanting/Unity/../test/run_tests.py` (planned): restore a pristine test save (world with a training
dummy next to the spawn point, character with a melee weapon, bow, pickaxe, staff, summoning weapon,
explosive and armor in fixed slots), launch the game, auto-start the world, run layer 1 for each held slot,
layer 2 for each weapon slot (slot switching via faked EQUIPSLOT input), write `Enchanting/test-results.txt`
through API.ConfigFilesystem, quit. The runner prints the PASS/FAIL table. Est. 8–10 min per full run.

Sid prepares once: the test world/character (any world; it is cloned so runs start identical).

## Layer 4 — manual (per release, ~10 min)

- Looks: violet table on the ground (2x1 placement preview), table UI with two separated buttons (right one a
  red down arrow), violet slot borders, enchant names in tooltips without "missing:".
- Table flow: roll costs 5/10/15 Gold + 250/500/750 coins; 4th roll replaces one; remove takes the top one.
- Persistence: upgrade an enchanted item, dye it, save + quit + reload — enchants stay.
- Knockback (enemy), Bossbane/Bosswarden (any boss), Thorns (let an enemy hit you).
- Mending, Unbreaking (tools): `/enchant debug on`, mine 30+ blocks per pickaxe, `/enchant report`.
- Multiplayer (when possible): a joining client sees tooltips/borders, enchant bonuses apply for them.

## New weapon classes (alpha 2026-11-04: magic, summoning, throwing)

- Magic staffs: layer 1 + layer 2 (combattest refills mana every tick while attacking, so Frugality/Clarity are
  verified by layer 1 only). Tomes: layer 1 only (combattest refuses them: minions ignore the dummy).
  Non-stackable throwables: layer 1 + layer 2 like a bow.

Earlier plan:

Each new pool must pass layer 1 (held) before shipping. Layer 2 needs the matching fake input:
staff/beam = hold attack like the bow; summoning = summon then let minions hit the dummy (count damage from
entities owned by the player's minions); explosives = throw at the dummy. Until layer 2 supports them,
they ship as "alpha: stat-verified only".
