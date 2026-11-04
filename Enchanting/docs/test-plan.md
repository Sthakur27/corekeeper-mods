# Enchanting — test plan

Goal: every enchant has an objective pass/fail, produced by the mod itself, readable from Player.log.
Three layers, cheapest first. Layers 1–2 exist (0.1.x); layer 3 and the new weapon classes are to do.

## Layer 1 — stat check (`/enchant selftest`, ~30 s, automatic)

For every enchant in the pools of the held item and worn helm/chest/pants, at levels I and III:
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
