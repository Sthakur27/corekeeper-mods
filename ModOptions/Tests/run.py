"""Run against installed CoreLib config source (not a simulated configuration implementation).

python ModOptions/Tests/run.py --csc <path/to/csc.dll> [--corelib <ConfigFile source directory>]
No game files or user configs are modified; output goes to ignored build/modoptions-tests.
"""
import argparse
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--csc', required=True, type=pathlib.Path)
p.add_argument('--corelib', type=pathlib.Path, default=pathlib.Path(
    r'C:\Users\Public\mod.io\5289\mods\3177992_8237090\Scripts\Scripts\Util\Data\ConfigFile'))
p.add_argument('--placement', type=pathlib.Path, default=pathlib.Path(
    r'C:\Users\Public\mod.io\5289\mods\3400322_8291416\Scripts\Scripts\PlacementPlusMod.cs'))
args = p.parse_args()
out = ROOT / 'build' / 'modoptions-tests'
out.mkdir(parents=True, exist_ok=True)
runtimes = subprocess.check_output(['dotnet', '--list-runtimes'], text=True)
version, folder = re.findall(r'Microsoft.NETCore.App (\S+) \[([^\]]+)\]', runtimes)[-1]
runtime = pathlib.Path(folder) / version
sources = list(args.corelib.glob('*.cs')) + list((ROOT / 'ModOptions' / 'Tests').glob('*.cs'))
sources += [ROOT / 'ModOptions' / 'Scripts' / f for f in ('ConfigDiscovery.cs', 'Setting.cs', 'SettingsPage.cs')]
# Execute the installed mod's actual Bind declarations, not a hand-transcribed fixture.
placement = args.placement.read_text(encoding='utf-8-sig')
block = placement.split('Config = new ConfigFile("PlacementPlus/PlacementPlus.cfg", true, modInfo);', 1)[1]
block = block.split('//RegisterMenuNames();', 1)[0]
block, count = re.subn(r'(?m)^(\s*)(\w+) = Config.Bind', r'\1var \2 = Config.Bind', block)
assert count == 6, 'Placement Plus settings changed: inspect the installed declarations before updating the fixture'
fixture = out / 'InstalledFixture.cs'
fixture.write_text('using CoreLib.Data.Configuration;\nclass InstalledFixture { public static ConfigFile Create() { '
    'var Config = new ConfigFile("PlacementPlus/PlacementPlus.cfg", false);\n' + block + '\nreturn Config; } }', encoding='utf-8')
sources.append(fixture)
assert len(list(args.corelib.glob('*.cs'))) >= 10, 'CoreLib config source not found'
rsp = out / 'tests.rsp'
rsp.write_text('\n'.join(['-nologo', '-target:exe', '-langversion:latest', '-nostdlib+',
    f'-out:"{out / "DiscoveryTests.dll"}"'] + [f'-r:"{f}"' for f in runtime.glob('*.dll')
        if (f.name.startswith('System.') and '.Native.' not in f.name) or f.name in ('mscorlib.dll', 'netstandard.dll')] +
    [f'"{f}"' for f in sources]), encoding='utf-8')
subprocess.run(['dotnet', str(args.csc), '@' + str(rsp)], check=True)
(out / 'DiscoveryTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
    'tfm': 'net' + version.split('.')[0] + '.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': version}}}))
subprocess.run(['dotnet', str(out / 'DiscoveryTests.dll')], check=True)
