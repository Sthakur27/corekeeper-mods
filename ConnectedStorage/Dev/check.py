"""Compile, test, verify and optionally install. Never starts/stops Core Keeper."""
import argparse,hashlib,json,pathlib,re,subprocess,sys,time
ROOT=pathlib.Path(__file__).resolve().parents[2];MOD=ROOT/'ConnectedStorage';OUT=ROOT/'build/connectedstorage'
def run(args): subprocess.run([str(x) for x in args],cwd=ROOT,check=True)
def main():
 p=argparse.ArgumentParser();p.add_argument('--install',action='store_true');p.add_argument('--preview',action='store_true');p.add_argument('--compiler',type=pathlib.Path);a=p.parse_args();OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'build-report.json').unlink(missing_ok=True)
 csc=a.compiler or next((pathlib.Path.home()/'AppData/Local/Temp/claude').rglob('bincore/csc.dll'),None)
 if not csc or not csc.exists():raise SystemExit('Pass --compiler <Microsoft.Net.Compilers.Toolset/tasks/netcore/bincore/csc.dll>.')
 managed=pathlib.Path('C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/Managed')
 manifest=json.loads((MOD/'ModManifest.json').read_text());files=[MOD/x['path'] for x in manifest['files']]
 missing=[str(x) for x in files if not x.is_file()]
 if missing:raise SystemExit('Manifest files missing: '+str(missing))
 sources=[x for x in files if x.suffix=='.cs'];unlisted=set((MOD/'Scripts').rglob('*.cs'))-set(sources)
 if unlisted:raise SystemExit('Unlisted runtime sources: '+str(unlisted))
 rsp=OUT/'check.rsp';rsp.write_text('\n'.join(['-nologo','-target:library','-langversion:latest','-unsafe',f'-out:"{OUT}/ConnectedStorage.dll"']+[f'-r:"{x}"' for x in managed.glob('*.dll')]+[f'"{x}"' for x in sources+list((MOD/'Dev/Compile').glob('*.cs'))]))
 run(['dotnet',csc,'@'+str(rsp)])
 runtimes=pathlib.Path('C:/Program Files/dotnet/shared/Microsoft.NETCore.App');runtime=sorted(runtimes.glob('8.*'),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
 rsp=OUT/'model-check.rsp';rsp.write_text('\n'.join(['-nologo','-target:exe',f'-out:"{OUT}/BrowserModelTests.dll"']+[f'-r:"{runtime}/{x}.dll"' for x in ['System.Private.CoreLib','System.Runtime','System.Console','System.Collections']]+[f'"{MOD}/Scripts/StorageBrowserModel.cs"',f'"{MOD}/Tests/BrowserModelTests.cs"']))
 (OUT/'BrowserModelTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':runtime.name}}}))
 run(['dotnet',csc,'@'+str(rsp)]);run(['dotnet',OUT/'BrowserModelTests.dll'])
 for script in ['Dev/test_capture_ui.py','Unity/validate_art.py','Unity/verify_browser_bundle.py','Unity/verify_bundle.py']:run([sys.executable,MOD/script])
 if a.preview:
  sdk=pathlib.Path('C:/Users/Sid/CoreKeeperModSDK');import shutil
  if (sdk/'Temp/UnityLockfile').exists():raise SystemExit('SDK is open; run preview from its Connected Storage menu, then rerun without --preview.')
  for source in (MOD/'Unity/Editor/Builder').glob('StorageUIPreview*.cs'):shutil.copy2(source,sdk/'Assets/ConnectedStorageTools/Builder/Editor'/source.name)
  run(['C:/Program Files/Unity/Hub/Editor/6000.0.59f2/Editor/Unity.exe','-batchmode','-projectPath',sdk,'-executeMethod','StorageUIPreviewFixtures.BuildAll','-quit','-logFile',OUT/'preview-check.log'])
  if 'FIXTURES PASS' not in (OUT/'preview-check.log').read_text(errors='replace'):raise SystemExit('Preview did not confirm success; see preview-check.log')
 version=re.search(r'Version = "([^"]+)"',(MOD/'Scripts/ConnectedStorageMod.cs').read_text()).group(1)
 if a.install:run(['powershell','-NoProfile','-File',MOD/'install.ps1'])
 hashes={str(x.relative_to(MOD)):hashlib.sha256(x.read_bytes()).hexdigest() for x in files}
 if a.install:
  dest=managed.parent/'StreamingAssets/Mods/ConnectedStorage'
  assert all(hashlib.sha256((dest/k).read_bytes()).hexdigest()==v for k,v in hashes.items()),'Installed files differ'
 report={'version':version,'checkedAt':time.time(),'installed':a.install,'preview':a.preview,'files':hashes,'runtimeVerified':False}
 (OUT/'build-report.json').write_text(json.dumps(report,indent=2));print(f'PASS {version}: {len(sources)} sources; '+('installed and hash-verified' if a.install else 'not installed')+'; runtime verification pending')
if __name__=='__main__':main()
