"""Compile SolariteDrill/Scripts against the local game assemblies (offline check; the game's own
safety check still runs at load). Uses the Roslyn toolset already in build/tools/csc.

    python SolariteDrill/Unity/compile_check.py
"""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[2]
managed = Path(r'C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/Managed')
work = root / 'build/solaritedrill'
work.mkdir(parents=True, exist_ok=True)
csc = root / 'build/tools/csc/tasks/netcore/bincore/csc.dll'
if not csc.exists():
    raise SystemExit(f'missing {csc} (see AGENTS.md section 4)')
args = ['-nologo', '-target:library', '-langversion:latest', '-unsafe', '-nowarn:CS0618,CS0649,CS0169']
args += ['-r:"' + str(p) + '"' for p in managed.glob('*.dll')]
args += ['-out:"' + str(work / 'SolariteDrill.dll') + '"']
args += ['"' + str(p) + '"' for p in (root / 'SolariteDrill/Scripts').rglob('*.cs')]
rsp = work / 'compile.rsp'
rsp.write_text('\n'.join(args))
raise SystemExit(subprocess.run(['dotnet', str(csc), '@' + str(rsp)]).returncode)
