# Performance design and release checklist

Use this for new features and changes to existing hot paths. Put the completed notes in the change description or benchmark record.

## Before implementation

- State the native event or system that owns the behavior. Confirm the exact current game method/component and whether it is managed or Burst compiled.
- State the trigger, expected frequency, authority (server/client/both), and maximum data touched. Include idle, disabled/default, multiplayer and worst-case behavior.
- Prefer native authoring, inventory, harvest, cooking, settings-change or lifecycle events when accessible. Cache stable metadata; invalidate on the event that changes it.
- If no safe native hook exists, document that limit and choose a smaller feature or a bounded workaround. Do not silently disable Burst to make a patch reachable.
- For inherently timed behavior, explain why a timer is needed. Bound the batch and query only relevant entities. A slower timer does not make a whole-world scan cheap.
- Treat per-frame callbacks, full entity snapshots, nested searches, texture readbacks, reflection, global job completion, structural changes and native-system replacement as measurement-required decisions.
- Avoid diagnostic string construction, LINQ materialization, repeated lookups and temporary managed collections in hot paths. A logger that discards a message still pays for arguments constructed before the call.
- Bound queues, caches, retry windows and retained entity/projectile histories. Specify removal and world-unload behavior.

## During implementation

- Check disabled/default conditions before queries, snapshots, waits and allocations wherever semantics allow. Verify generated entry points too; a guard in an unused method is not a guard.
- Avoid duplicate work across client/server worlds and prediction replays. Make one-time effects idempotent at the correct authority boundary.
- Reuse the game's mutation pathways to preserve inventory metadata, replication and save behavior.
- Keep normal releases free of profiling instrumentation. Diagnostic scopes should wrap existing work, not perform extra queries to count it.
- Record examined and changed item counts at the loop that already performs the work. Distinguish inclusive scope time from exclusive cost; nested scopes cannot be summed.
- Count errors and warnings; do not print once per entity or once per frame. Logging itself can become the bottleneck.
- Define the expected scaling: per action, per player, per nearby object, or per world object. Explain any multiplicative behavior.

## Before release

- Offline compile, loader acceptance and functional behavior all pass. A compile success is not a runtime test.
- Run the [benchmark protocol](BENCHMARK.md) with a fixed world/route, settings, mod set and host/client role. Include idle, ordinary use and the feature's stress case.
- Compare at least three captures per arm. Use a recorder-only comparison for the release decision; use instrumented copies for diagnosis and quantify their overhead separately.
- Review p95/p99 frame time, stutters per 1,000 frames, individual scope cost/call count, allocation trends, errors and work counts. Average FPS alone does not establish smoothness.
- Test the actual off/default path. Removing a mod, changing its settings and disabling an Overhaul feature are different experiments.
- Check sustained use for growing queues/caches and memory. Short captures will miss slow growth.
- Preserve source/stage identities and a brief correctness result alongside the reports. Do not claim a speedup from mismatched settings or a single favorable run.

## Initial investigation thresholds

These are project policies to trigger investigation, not universal hardware limits or statistical significance tests:

- Investigate if repeated-run median p95 worsens by **both more than 10% and more than 1 ms**, or p99 by **both more than 15% and more than 2 ms**.
- Investigate increased stutter frequency, recurring errors, growing retained state or unexpectedly nonzero idle work even if the frame thresholds pass.
- At 60 FPS the entire frame has about 16.67 ms. Establish a feature budget from measured baseline headroom; do not give every mod that entire budget.
- If frame time is capped and unchanged, inspect headroom/cost with a second consistently uncapped test. Keep capped and uncapped results in separate comparisons.

A result below these thresholds means no large regression was detected in those scenarios. It does not prove the mod is cost-free.
