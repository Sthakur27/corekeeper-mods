# Initial tooling validation, 2026-10-14

## First live-loader trial

Installed the recorder-only companion alongside the actual installed Overhaul; gameplay and
WIP mods were not replaced. The loader rejected `Exception.GetType().Name` in the config
read-error path because it references reflection. Replaced it with a fixed error label and
staged/installed the correction for the next restart. Live loader success, command execution
and report export are still pending. The initial offline evidence below describes the earlier
snapshot; it did not detect this sandbox restriction.

Fresh launch after the correction: Player.log confirms `passed code security verification`
and `Successfully compiled ModDiagnostics safetyCheck=True`. The game remained running.
Command execution and capture/export are the next live checks.

## Capture/export trial and correction

The command and filesystem write succeeded in game. First capture had only 2.7425 eligible
seconds; second (`20261014-231056-610-idle-1.json`) had 59.9977 eligible seconds, 12,026
frames and a logged p95 of 9.35 ms, with zero recorded warnings/errors. However, Unity's
JSON export omitted nested frame statistics and custom-type lists, including the mod list.
Both files are incomplete and must not be used in comparisons. Full p99/stutter statistics
cannot be recovered from their saved JSON.

Recorder 0.1.1 replaces that export with an explicit reflection-free schema writer.
Eight JSON round-trip checks cover nested objects/lists, locale-independent numbers and
escaping; the original 21 metrics checks still pass. Game-assembly compilation passes.
The comparator now requires >=20 eligible seconds and >=80% coverage for duration-limited
captures. Its 20 tests plus 10 staging tests pass.

Corrected recorder-only stage `build/performance/installed-baseline-20261014-v3/` was copied
to the local recorder install for the next restart; gameplay sources were untouched.
Live loader/export verification of this correction remains pending.

Completed offline:

- 15 comparison-tool tests passed: repeated runs, metadata/quality rejection, different frame counts, normalized stutters, missing/nested scopes, practical thresholds, safe JSON and duplicate-input rejection.
- 10 staging tests passed: C# comment/string masking, body wrapping, unsupported/generated/job exclusions, manifest/path safety, companion dependency and deterministic identities.
- 21 C# metrics/sampling checks passed: percentiles, partial/empty buffers, bounds, invalid values, warmup/focus/menu gaps and long frame stalls.
- ModDiagnostics compiled against the installed game assemblies with CoreLib compile stubs: zero errors.
- Clean released Overhaul from the isolated release worktree was staged with 60 instrumented lifecycle scopes and 85 skipped coverage entries. Both the staged recorder and staged Overhaul compiled separately with the recorder assembly reference: zero errors.

Integration stage: `build/performance/overhaul-scopes-check/` (local, gitignored).
Build identity: `c332a31ddaf4a97712964d548b2ad0f049bfa1e9f5b2e1bf03c12341192895c9:instrumented`.
The stage report preserves the full input/output hashes and skipped coverage. Compiler artifacts at the stage root are validation outputs, not runtime mod files.

Not yet validated: loader safety-check acceptance, CoreLib command registration in game, actual export permissions, live sampling behavior or profiler overhead. No mods were installed, game processes changed, benchmarks captured or performance gains claimed by this tooling work.

Re-run Python checks from the repository root:

```powershell
python -m unittest discover -s release/performance -p 'test_*.py' -v
python ModDiagnostics/Tests/run_tests.py --csc "<path to Roslyn csc.dll>"
```

See AGENTS.md tooling notes for obtaining the compiler. The C# test runner uses the installed .NET 8 runtime; the runtime compile check must separately use the actual game assemblies and dependency API.
