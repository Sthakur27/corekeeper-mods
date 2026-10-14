"""Run the isolated event tests: python GoldenChance/Tests/run_tests.py --csc PATH_TO_CSC_DLL."""
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

out = root / "build" / "golden-events"
out.mkdir(parents=True, exist_ok=True)
runtime = max(Path("C:/Program Files/dotnet/shared/Microsoft.NETCore.App").glob("8.*"),
              key=lambda p: tuple(map(int, p.name.split("."))))
assembly = out / "TalentEventTests.dll"
sources = [root / "GoldenChance/Scripts/Patches/TalentValuePatch.cs", Path(__file__).with_name("TalentEventTests.cs")]
rsp = out / "tests.rsp"
rsp.write_text("\n".join(["-nologo", "-target:exe", f'-out:"{assembly}"']
    + [f'-r:"{p}"' for p in runtime.glob("*.dll") if is_managed(str(p))]
    + [f'"{p}"' for p in sources]))
assembly.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
    "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": runtime.name}}}))
subprocess.run(["dotnet", args.csc, "@" + str(rsp)], check=True)
subprocess.run(["dotnet", str(assembly)], check=True)

scripts = root / "GoldenChance/Scripts"
source = "\n".join(p.read_text() for p in scripts.rglob("*.cs"))
for removed in ("DisableBurst", "Time.unscaledTime", "RegularCooking", "_nextRefresh"):
    assert removed not in source, f"Continuous inventory/talent mechanism returned: {removed}"
assert "public void Update() { }" in source
declared = {f["path"] for f in json.loads((scripts.parent / "ModManifest.json").read_text())["files"] if f["path"].endswith(".cs")}
actual = {p.relative_to(scripts.parent).as_posix() for p in scripts.rglob("*.cs")}
assert declared == actual, "Runtime sources and manifest disagree"
print("Performance guards and runtime manifest passed.")
