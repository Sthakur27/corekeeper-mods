# Mod performance workflow

Performance is a release requirement. A feature should cost almost nothing when its relevant gameplay is idle, and its disabled path should be measured too. Native hooks are preferred, but an event handler can still cause lag by scanning the world, allocating heavily, or forcing native jobs to finish.

This workflow has three parts:

1. [Design and release checklist](CHECKLIST.md): establish what triggers work and what it can cost before adding it.
2. [Development recorder](../../ModDiagnostics/README.md) and the source audits below: identify expensive paths, then measure them in an isolated diagnostic build.
3. [Repeatable benchmark protocol](BENCHMARK.md): compare repeated runs of the same scenario and preserve the evidence with the release.

## Existing-mod audit, 2026-10-14

Three independent sub-agent reviews covered all 29 existing root mod manifests, the Overhaul integration, and the legacy asset archive. ModDiagnostics is new tooling, not part of the existing-mod count. The reviews are **static hypotheses, not measured causes of lag**. Gameplay code was not changed by this audit.

| Review | Coverage |
| --- | --- |
| [Simulation](audit-simulation.md) | Auto Replant, Faster Mushrooms, Fast Auto Fishing, Better Fishing Loot, Bigger Watering Cans, Buff Duration Floor, Boss Bonus Loot, Infinite Ore Boulders, Keep Minions, Vehicle Speed, Durability Multiplier |
| [Inventory and UI](audit-inventory-ui.md) | Loadout Fallback, Five Loadouts, Ender Stash, Armor Dye, Stim Hits, Mod Options, Pet Editor, legacy Master Pet Plus and its asset archive |
| [Settings, integration and WIP](audit-settings-wip.md) | Difficulty Tuning, Skill XP Multiplier, Golden Chance, Potion Seller, Fish Seller, Merchant Restock, Quick Buff, Overhaul, Connected Storage, Magic Mirror, Brute Armor |

## Measurement order

| Priority | Candidate | Reason to investigate | First experiment |
| --- | --- | --- | --- |
| 1 | Armor Dye | Diagnostic strings are constructed every frame even when a once-only logger discards them. | Idle/movement capture with allocation timing around the affected path. |
| 1 | Faster Mushrooms | Several entity snapshots each tick, including vanilla-rate operation. | Busy base with default, enabled and safely disabled configurations. |
| 1 | Fast Auto Fishing | Generated code can complete jobs before checking whether work is enabled; timer work is separate from the auto toggle. | No rod equipped, auto off, then active fishing; inspect generated-path coverage explicitly. |
| 1 | Auto Replant | Repeated dropped-item searches during pending harvest retries. | Dense harvest and a failed/pending replant scenario. |
| 2 | Stim Hits | Projectile-history cleanup depends on a different collection's size. | Sustained attacks on one target; inspect history counts and memory trend. |
| 2 | Loadout Fallback / Five Loadouts / Ender Stash | Repeated player-buffer migration/guard scans even when nothing changes. | Idle with inventories full, then switch loadouts and reconnect. Never remove slot mods from a live save to test. |
| 2 | Merchant mods / Skill XP | Recurring scans/snapshots may remain at default or unchanged settings. | Defaults versus active modifications, with equal entity counts. |
| WIP | Connected Storage | Repeated storage graph/recipe scans and explicit global job completion. | Large connected network, UI closed/open, crafting bursts. Keep separate from released-mod captures. |

Golden Chance 2.2 removed the known invasive inventory/Burst path and uses native plant/talent events. The player's improved experience is valuable evidence, but it is not a quantified baseline for the remaining mods.

## Deliverables and current limits

See [initial validation results](VALIDATION.md) for automated checks and staged-build compilation evidence.

- The recorder is opt-in, bounded and local. Normal release sources are not instrumented.
- The staging tool creates separate diagnostic copies and a companion recorder. It reports what it could instrument; absence from a report does not mean a feature is free.
- The comparison tool validates capture compatibility and compares repeated-run medians rather than combining all frames into one run.
- Generated jobs, Burst jobs, native engine work, GPU work and unwrapped event handlers require targeted follow-up. The first automatic pass is intentionally conservative.
- Runtime loader acceptance and actual in-game captures must be checked before treating this tooling as a validated measurement system. An offline compile cannot prove loader sandbox acceptance or correct scheduling.

Fix one measured issue at a time, rerun the same route, and keep both the correctness result and the performance evidence. Do not silently trade away a feature to hit a number; record any scope change.
