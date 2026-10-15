"""Rebuild the adapted browser visuals from a pinned MIT-licensed upstream.

Requires the game-imported Core Keeper SDK and Unity used by build_bundle.py.
Run while that SDK project is closed. Does not touch or restart Core Keeper.
"""
import json
from pathlib import Path
import shutil
import subprocess

REPO = Path(__file__).resolve().parents[2]
SDK = Path('C:/Users/Sid/CoreKeeperModSDK')
UNITY = 'C:/Program Files/Unity/Hub/Editor/6000.0.59f2/Editor/Unity.exe'
REV = '9b9df228428f212681873e875409fc33003cb3f7'
upstream = REPO / 'build/itembrowser-upstream'
if not upstream.exists():
    subprocess.run(['git', 'clone', 'https://github.com/moorowl/ItemBrowser.git', str(upstream)], check=True)
actual = subprocess.check_output(['git', '-C', str(upstream), 'rev-parse', 'HEAD'], text=True).strip()
if actual != REV:
    raise SystemExit(f'Expected upstream {REV}; found {actual}. Use a pinned checkout.')
target = SDK / 'Assets/ItemBrowserPackage'
shutil.copytree(upstream / 'ItemBrowserPackage', target, dirs_exist_ok=True)
asm_path = target / 'ItemBrowser.asmdef'
asm = json.loads(asm_path.read_text(encoding='utf-8-sig'))
asm['precompiledReferences'] = sorted({p.name for p in (SDK / 'Assets/Plugins').rglob('*.dll')
    if not p.name.startswith(('Unity', 'System', 'mscorlib', 'netstandard', 'Microsoft', 'Mono.'))})
asm_path.write_text(json.dumps(asm, indent=2), encoding='utf-8')
shutil.copy2(Path(__file__).parent / 'Editor/Builder/StorageBrowserBuilder.cs',
             SDK / 'Assets/ConnectedStorageTools/Builder/Editor/StorageBrowserBuilder.cs')
log = REPO / 'build/storage-browser-build.log'
subprocess.run([UNITY, '-batchmode', '-projectPath', str(SDK), '-disable-assembly-updater',
    '-executeMethod', 'StorageBrowserBuilder.Build', '-quit', '-logFile', str(log)], check=True)
if '[StorageBrowserBuilder] Exported browser visuals' not in log.read_text(errors='replace'):
    raise SystemExit(f'Export was not confirmed; see {log}')
for bundle in (SDK / 'Library/ConnectedStorageBrowserBuild').glob('*.assetbundle'):
    name = 'ConnectedStorageBrowser_Windows.assetbundle' if '_windows.' in bundle.name else 'ConnectedStorageBrowser_Linux.assetbundle'
    shutil.copy2(bundle, Path(__file__).parents[1] / 'Bundles' / name)
print('Rebuilt browser visuals; run verify_browser_bundle.py before installing.')
