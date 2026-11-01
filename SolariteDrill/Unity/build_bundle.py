"""Build SolariteDrill/Bundles with Unity 6000.0.59f2 and the official Core Keeper Mod SDK.

    python SolariteDrill/Unity/build_bundle.py [--sdk PATH] [--skip-import]

Steps: copy Editor/ tools into the SDK, write source assets (gen_assets.py), run Unity twice
(SolariteDrillSetup.ImportGame, SolariteDrillBuilder.BuildAll), copy the bundles, then verify_bundle.py.
The Overhaul's combined bundle (ConnectedStorage/OverhaulBundles) picks up Assets/SolariteDrill when
StorageOverhaulBuilder.Build is run afterwards.
"""
from pathlib import Path
import argparse, shutil, subprocess, sys, tempfile, time

HERE = Path(__file__).resolve().parent
UNITY = r'C:/Program Files/Unity/Hub/Editor/6000.0.59f2/Editor/Unity.exe'


def run(sdk, method, log, *extra):
    process = subprocess.Popen([UNITY, '-batchmode', '-projectPath', str(sdk), '-disable-assembly-updater',
                                '-executeMethod', method, *extra, '-quit', '-logFile', str(log)])
    shutdown = None
    while process.poll() is None:
        text = log.read_text(errors='replace') if log.exists() else ''
        if 'Batchmode quit successfully invoked' in text and 'Cleanup mono' in text:
            shutdown = shutdown or time.monotonic()
            if time.monotonic() - shutdown > 30:
                # Some Unity installs hang after a successful batch shutdown; only then kill the child.
                process.kill(); process.wait(); break
        time.sleep(2)
    return log.read_text(errors='replace')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--sdk', default=r'C:/Users/Sid/CoreKeeperModSDK')
    ap.add_argument('--skip-import', action='store_true')
    args = ap.parse_args()
    sdk = Path(args.sdk)
    for sub in ('Setup', 'Builder'):
        dest = sdk / 'Assets/SolariteDrillTools' / sub / 'Editor'
        dest.mkdir(parents=True, exist_ok=True)
        for f in (HERE / 'Editor' / sub).iterdir():
            shutil.copy2(f, dest / f.name)
    subprocess.run([sys.executable, '-I', str(HERE / 'gen_assets.py'), str(sdk)], check=True)
    work = Path(tempfile.mkdtemp(prefix='solaritedrill_'))
    print('Build logs:', work, flush=True)
    if not args.skip_import:
        text = run(sdk, 'SolariteDrillSetup.ImportGame', work / 'setup.log')
        if '[SolariteDrillSetup] imported' not in text:
            sys.exit('ImportGame failed; see ' + str(work / 'setup.log'))
    text = run(sdk, 'SolariteDrillBuilder.BuildAll', work / 'build.log', '-drillOut', str(work / 'out'))
    if '[SolariteDrillBuilder] Built SolariteDrill into' not in text:
        print('\n'.join(l for l in text.splitlines() if 'error' in l.lower() or 'SolariteDrillBuilder' in l)[-6000:])
        sys.exit('BuildAll failed; see ' + str(work / 'build.log'))
    dest = HERE.parent / 'Bundles'
    dest.mkdir(exist_ok=True)
    for f in (work / 'out' / 'SolariteDrill' / 'Bundles').iterdir():
        shutil.copy2(f, dest / f.name)
        print('  ', f.name)
    subprocess.run([sys.executable, '-I', str(HERE / 'verify_bundle.py')], check=True)


if __name__ == '__main__':
    main()
