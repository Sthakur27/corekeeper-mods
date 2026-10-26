"""Run pure diagnostics metrics/sampling tests: python ModDiagnostics/Tests/run_tests.py --csc PATH."""
import argparse
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--csc", required=True)
args = parser.parse_args()
sys.path.insert(0, str(root / "release"))
from workshop_publish import is_managed

out = root / "build" / "diagnostics-tests"
out.mkdir(parents=True, exist_ok=True)
runtime = max(Path("C:/Program Files/dotnet/shared/Microsoft.NETCore.App").glob("8.*"),
              key=lambda p: tuple(map(int, p.name.split("."))))
assembly = out / "MetricsTests.dll"
sources = [root / ("ModDiagnostics/Scripts/" + name) for name in ("Metrics.cs", "Report.cs", "ReportJson.cs")]
sources.append(Path(__file__).with_name("MetricsTests.cs"))
rsp = out / "tests.rsp"
rsp.write_text("\n".join(["-nologo", "-target:exe", f'-out:"{assembly}"']
    + [f'-r:"{p}"' for p in runtime.glob("*.dll") if is_managed(str(p))]
    + [f'"{p}"' for p in sources]))
assembly.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
    "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": runtime.name}}}))
subprocess.run(["dotnet", args.csc, "@" + str(rsp)], check=True)
result = subprocess.run(["dotnet", str(assembly)], check=True, capture_output=True, text=True)
lines = result.stdout.splitlines()
print(lines[0])
report = json.loads(lines[-1])
assert report["frames"]["count"] == 3 and report["frames"]["p99Ms"] == 20
assert report["mods"] == [{"name": "example", "configFingerprint": "1234", "configReadError": None}]
assert report["scopes"][0]["allocatedBytes"] == 99 and report["scopes"][0]["inclusiveMs"] == 1.25
assert report["markers"] == [{"seconds": 2.5, "label": "marker"}]
assert report["repeatedLogs"][0]["message"] == "tab\tcontrol\x01"
assert report["label"] == 'quote" slash\\ newline\n snow\u2603'
assert report["sampledSeconds"] == 60.25 and report["featureSwitches"] == ["Feature: example = true"]
assert report["sampleCapacityReached"] is False
assert report["hitches"] == [{"seconds": 1.5, "ms": 80, "gc": True, "allocatedBytes": 2048, "topScope": "Update", "topScopeMs": 5}]
assert report["hitchesWithGc"] == 1 and report["cpuMainMs"]["p99"] == 4 and report["gpuMs"] is None
assert report["scopes"][0]["hitchTop"] == 0 and report["frameTimingAvailable"] is False
print("11 JSON export round-trip checks passed (nested fields, escaping, non-English locale, hitches).")
