"""Compile/run the real armor rule with data-only doubles; no game process needed."""
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'build' / 'mushroomslayer' / 'tests'
OUT.mkdir(parents=True, exist_ok=True)
compilers = list((Path(os.environ['LOCALAPPDATA']) / 'Temp').glob('claude/**/csc.dll'))
compilers += list((ROOT / 'build' / 'tools').glob('**/csc.dll'))
if not compilers:
    raise SystemExit('Install the Roslyn toolset described in AGENTS.md first.')
runtime = sorted(Path('C:/Program Files/dotnet/shared/Microsoft.NETCore.App').glob('8.*'))[-1]
rsp = ['-nologo', '-target:exe', '-langversion:latest', '-out:"' + str(OUT / 'RuleTests.dll') + '"']
rsp += ['-r:"' + str(runtime / p) + '"' for p in ['System.Private.CoreLib.dll', 'System.Runtime.dll', 'System.Console.dll']]
rsp += ['"' + str(ROOT / p) + '"' for p in ['MushroomSlayer/Scripts/ArmorRule.cs', 'MushroomSlayer/Tests/RuleTests.cs']]
(OUT / 'tests.rsp').write_text('\n'.join(rsp))
subprocess.run(['dotnet', str(compilers[0]), '@' + str(OUT / 'tests.rsp')], check=True)
(OUT / 'RuleTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime.name}}}))
subprocess.run(['dotnet', str(OUT / 'RuleTests.dll')], check=True)
