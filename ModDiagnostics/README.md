# Sid's Mod Diagnostics (development only)

An opt-in local performance recorder for investigating Core Keeper mods. It is not included in Workshop releases or the Overhaul build. Requires CoreLib. Normal gameplay sources remain unchanged; instrumentation is added only to separate staged copies.

Since 0.2.0 each report also has a hitch log: up to 200 frames over 33.33 ms with their time after warmup, whether a GC was observed in that interval, main-thread allocation, and (when instrumented) the single slowest scope in that frame. Scopes gain `hitchTop`/`hitchTopMs` (how often and how long they were the slowest scope in a hitch). `cpuMainMs`/`gpuMs` come from Unity's FrameTimingManager when the build enables frame timing stats (`frameTimingAvailable`), and `allocatedKbPerFrame` summarizes main-thread allocation per frame.

The recorder collects frame-time mean/median/p95/p99/max and stutter counts, process GC/heap observations, bounded warning/error patterns, environment/config identities and optional per-method scopes. Scopes report calls, inclusive elapsed time, maximum call time and current-thread allocation bytes. `Recorder.Work` can record examined/changed counts in a deliberately instrumented existing loop; automatic staging does not invent those counts.

## Build an isolated diagnostic copy

From the repository root, choose an existing standalone mod folder or a clean built Overhaul folder:

```powershell
python release/performance/stage_diagnostics.py --source GoldenChance --output build/performance/golden-recorder --no-instrument
python release/performance/stage_diagnostics.py --source GoldenChance --output build/performance/golden-scopes
```

Each output must be new. Keep the staging report: it identifies source hashes and instrumented/skipped methods. The automatic scope selection covers managed block-bodied Update/OnUpdate/LateUpdate/OnGUI methods, managed `ISystem.OnUpdate(ref SystemState)` and static Harmony `Prefix`/`Postfix` methods in HarmonyPatch files, not every event hook (timers, attribute-named patches and multi-line signatures are reported as skipped). Golden Chance's event-driven work therefore needs targeted scopes for detailed attribution. Generated/Burst/job code is skipped deliberately and must be reviewed separately.

Install the staged target and companion `ModDiagnostics` folders through the normal local side-loader workflow, with Core Keeper closed and CoreLib available. Back up the prior target install first, avoid duplicate subscribed/local feature implementations, and restart normally. This tool does not copy into the game or stop it for you. Verify both mods compile/load in Player.log. Restore the normal target and remove the companion after diagnostics.

## Capture

```text
/modperf start base-idle-1 60
/modperf status
/modperf mark harvest
/modperf stop
/modperf export
```

Start is opt-in for each capture, accepts 5-300 seconds and waits three seconds before eligible sampling. Close chat and keep the game focused in a loaded world. Captures finish automatically; data is written once at the end through the game's sandbox-safe config filesystem to `ModDiagnostics/reports/`. Failed exports remain in memory for `/modperf export`; the recorder refuses to overwrite that pending report with a new capture.

Frame storage is capped at 120,000 samples, scopes at 512, markers at 128 and repeated warning/error patterns at 64. Reports flag capacity limits. While off, the recorder has only its inactive Update check; uninstrumented release code has no scope calls. Staged diagnostic code still checks the recorder when inactive.

## Interpret carefully

- Use [the benchmark protocol](../docs/performance/BENCHMARK.md), including at least three runs per comparison arm.
- Menus, loss of focus and absence of a local player exclude frame samples. This version is not a UI-menu benchmark or a dedicated-server profiler.
- Scopes are inclusive main-thread elapsed time, including waits. Never add nested scopes to estimate total mod cost. Automatic names do not distinguish client/server worlds.
- Allocated bytes describe the current thread inside a scope. Heap change and GC counts are process-wide and cannot assign blame to a mod.
- Native/Burst/GPU work and unwrapped hooks are not directly attributed. A zero or missing scope is not proof of zero cost.
- Profiling adds overhead. Compare recorder-only and instrumented copies of the same source before interpreting small differences.
- Config fingerprints do not record every effective setting or source version of third-party mods. Keep the experiment worksheet and stage report.
- Warning/error samples may contain text from other mods. Review captured messages before sharing reports.

Offline compile and automated checks are necessary, but loader sandbox acceptance and real-world behavior still require an in-game trial. No benchmark numbers are claimed until those captures exist.
