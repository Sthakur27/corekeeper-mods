# Repeatable benchmark protocol

## Prepare a stable test

1. Back up and copy the test character/world through the game's supported save workflow. Use the copy for experiments. Never remove inventory-layout mods from an important save to create a baseline.
2. Choose an exact source snapshot and record its commit plus dirty changes, installed mod list, config copies/fingerprints and whether the test uses separate mods or Overhaul. Do not mix the three WIP mods into a released-mod baseline accidentally.
3. Keep resolution, display mode, VSync/FPS cap, graphics settings, hardware power mode and background workload constant. Keep the game focused. Record single-player, host or client role and player count.
4. Establish a route with a fixed start, end, duration and actions. Restore the same copied world state when harvesting/crafting/combat changes the workload.
5. Warm the loaded world for 30-60 seconds before the first capture. The recorder's additional three-second delay only allows chat to close; it is not a substitute for warming assets and systems.

## Scenario set

| Scenario | Actions | What it exposes |
| --- | --- | --- |
| Quiet idle | Stand in the same quiet location for 60 seconds. | Unnecessary recurring work, allocations and log spam. |
| Busy base | Walk the same loop past a fixed count of plants, machines, merchants and storage. | Entity-count scaling and world scans. |
| Feature burst | Harvest the same plot, craft the same count, fish or swap loadouts in a prescribed sequence. | Event bursts, waits and retries. |
| Combat | Same arena/enemies/weapon; sustained attacks on one target for history-growth checks. | Projectile/event rates and cache retention. |
| UI | Open the same menu/storage and perform the same actions. | UI rebuilding and recipe scans; see UI limitation below. |
| Multiplayer | Repeat on a host and a client with the same player count. | Authority duplication and prediction behavior. |

For long-lived state, add a 10-15 minute session with several 60-second captures and inspect history/queue sizes where targeted counters exist. A stable managed heap alone does not prove a cache is bounded.

**UI limitation:** the initial recorder excludes frames while `Manager.menu.IsAnyMenuActive()` is true. Do not use those captures to declare menu performance good. UI paths need a targeted capture mode/profiler follow-up; retained eligible frames describe gameplay only. Likewise, scope eligibility follows the recorder callback and is not an exact engine phase boundary.

## Diagnostic staging and capture

The staging tool copies an existing mod folder and adds a development-only companion recorder. Use a clean built Overhaul folder or one standalone source folder. See [ModDiagnostics](../../ModDiagnostics/README.md) for the exact CLI and installation instructions. Keep the generated stage report with its captures.

Use three distinct experiments:

1. **Recorder overhead:** same source/config, ordinary build versus recorder-only build. Frame sampling has a cost, so assess it with an independent frame-time measurement if available; the recorder cannot observe a run in which it is absent.
2. **Diagnosis:** same source/config with recorder-only versus instrumented build. This exposes how much scope instrumentation changes the result. Do not subtract measured bookkeeping and call the remainder exact native cost.
3. **Release comparison:** old versus proposed source, both using recorder-only, identical scenario/config. Instrumented runs help locate causes; they should not be the only evidence for a release claim.

After installing a staged build and restarting normally, verify Player.log shows successful compilation and no safety/load errors for the companion and target mod. Do not install duplicate standalone/Overhaul feature implementations.

In chat:

```text
/modperf start base-idle-1 60
/modperf status
/modperf stop
/modperf export
```

Close chat promptly after starting. Captures end automatically. `stop` ends early; `export` retries a failed write. Reports use the loader's config filesystem under `ModDiagnostics/reports/` (normally the active Steam profile's `mods/ModDiagnostics/reports/` directory). Copy them into a local experiment folder; do not commit machine-specific captures by default.

Run at least three captures of each arm, alternating A/B/A/B/A/B where practical. Restore state and restart when changing source or frozen feature switches. Do not count a repeated copy of one report as a repeat. Label captures with scenario, arm and run number.

```powershell
python release/performance/compare.py --baseline "captures/baseline-*.json" --candidate "captures/candidate-*.json" --json captures/comparison.json
```

The tool rejects incompatible recorded environments, insufficient valid frames, truncated buffers and dropped scopes. Fewer than three captures per arm is provisional. Build/config differences are listed for human review: they are allowed because those are often the experimental variable. It cannot automatically validate the route, full graphics settings, world state, third-party source versions, host/client role or background workload.

## Record and interpret

Copy [run-template.json](run-template.json) per experiment. Fill in stage identities, environment details not captured automatically, scenario and expected result before testing. Keep actual correctness observations separate from performance numbers.

Frame time is frame spacing, not GPU or CPU execution time. Scope timings are main-thread elapsed time including waits; an expensive wait may reflect native jobs started elsewhere. Allocation counts cover only the measured current thread and are not retained memory. GC totals and heap changes cover the process, including warmup/excluded intervals. Neither high CPU usage nor a slow scope alone proves that feature is the root cause.

Read the hitch log before the averages. Hitches flagged `gc` point at allocation pressure across the process (look at `allocatedKbPerFrame` and scope `allocatedBytes`), not at whichever scope ran. Hitches without GC and with a large `topScopeMs` point at that scope; with an empty `topScope` the work was outside instrumented code. A hitch whose `topScopeMs` is far below the frame time was spent elsewhere (engine, jobs, rendering). With `frameTimingAvailable`, compare `cpuMainMs` and `gpuMs` p95 to see which side limits the frame. `compare.py` summarizes hitch counts, GC-flagged hitches and top-scope tallies per arm.

If a suspect is confirmed, change one mechanism, repeat the benchmark, and check correctness again. If the difference disappears across repeats, keep it as inconclusive. Do not optimize from a single worst frame.
