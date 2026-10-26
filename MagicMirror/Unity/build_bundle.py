"""Build standalone and combined-overhaul item bundles with Unity 6000.0.59f2.
Requires the SDK and EnderStash source assets in that SDK (build EnderStash first).
"""
from pathlib import Path
import argparse, subprocess, shutil, tempfile, sys, time
HERE=Path(__file__).resolve().parent
ap=argparse.ArgumentParser();ap.add_argument('--sdk',default=r'C:/Users/Sid/CoreKeeperModSDK');ap.add_argument('--skip-import',action='store_true');args=ap.parse_args()
sdk=Path(args.sdk)
for sub in ('Setup','Builder'):
 dest=sdk/'Assets/MagicMirrorTools'/sub/'Editor';dest.mkdir(parents=True,exist_ok=True)
 for f in (HERE/'Editor'/sub).iterdir():shutil.copy2(f,dest/f.name)
subprocess.run([sys.executable,str(HERE/'gen_assets.py'),str(sdk)],check=True)
work=Path(tempfile.mkdtemp(prefix='magicmirror_'));print('Build logs:',work,flush=True)
# ModBuilder removes processor-added paths if it fails before bundling them.
# Preserve the shared EnderStash source assets on that failure path.
ender=sdk/'Assets/EnderStash'
backup={p:p.read_bytes() for p in ender.rglob('*') if p.is_file()}
for method in ('MagicMirrorSetup.ImportGame','MagicMirrorBuilder.BuildAll'):
 if args.skip_import and method.endswith('ImportGame'): continue
 log=work/(method+'.log')
 process=subprocess.Popen([r'C:/Program Files/Unity/Hub/Editor/6000.0.59f2/Editor/Unity.exe','-batchmode','-projectPath',str(sdk),'-disable-assembly-updater','-executeMethod',method,'-mirrorOut',str(work/'out'),'-quit','-logFile',str(log)])
 shutdown=None
 while process.poll() is None:
  text=log.read_text(errors='replace') if log.exists() else ''
  if 'Batchmode quit successfully invoked' in text and 'Cleanup mono' in text:
   shutdown=shutdown or time.monotonic()
   if time.monotonic()-shutdown>30:
    # Some Unity installations hang after a successful batch shutdown. Only
    # terminate this child after the completed task and shutdown are logged.
    process.kill();process.wait();break
  time.sleep(2)
 text=log.read_text(errors='replace')
 for path,data in backup.items():
  if not path.exists():path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
 success=('[MagicMirrorSetup] imported' in text if method.endswith('ImportGame') else '[MagicMirrorBuilder] Built SidsItemAssets into' in text)
 if not success:
  print('\n'.join(l for l in text.splitlines() if 'error' in l.lower() or 'MagicMirrorBuilder' in l));sys.exit(1)
for name,directory in [('MagicMirror','Bundles'),('SidsItemAssets','OverhaulBundles')]:
 dest=HERE.parent/directory;dest.mkdir(exist_ok=True)
 for f in (work/'out'/name/'Bundles').iterdir():shutil.copy2(f,dest/f.name)
print('Standalone and combined bundles built.')
