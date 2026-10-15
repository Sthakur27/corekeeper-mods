"""Rebuilds ConnectedStorage/Bundles (terminal and eight storage chest tiers) with the official Core Keeper Mod SDK.

Needs: Unity 6000.0.59f2 (+ Linux Build Support (Mono)) via Unity Hub, and a clone of
https://github.com/Pugstorm/CoreKeeperModSDK (default C:\\Users\\Sid\\CoreKeeperModSDK).

    python build_bundle.py [--sdk PATH] [--art]

  --art  regenerate original procedural pixel art first (make_art.py); otherwise Art/*.png is used as is.

Steps: copy Editor/ tools into the SDK project, write the source assets (gen_assets.py), run Unity in
batch mode twice (ConnectedStorageSetup.ImportGame: game assemblies + USE_PUG_OTHER define, then
ConnectedStorageBuilder.BuildAll: prefabs, data blocks, ModBuilder), copy the bundles into ../Bundles.
"""
import argparse, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
UNITY = r"C:\Program Files\Unity\Hub\Editor\6000.0.59f2\Editor\Unity.exe"


def unity(sdk, method, log, *extra):
    cmd = [UNITY, "-batchmode", "-projectPath", sdk, "-disable-assembly-updater", "-executeMethod", method, *extra, "-quit", "-logFile", log]
    print(">", method)
    r = subprocess.run(cmd)
    if r.returncode != 0:
        with open(log, encoding="utf-8", errors="replace") as f:
            tail = [l for l in f if "error" in l.lower() or "[ConnectedStorageBuilder]" in l]
        sys.exit(f"{method} failed (exit {r.returncode}); see {log}\n" + "".join(tail[-20:]))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sdk", default=r"C:\Users\Sid\CoreKeeperModSDK")
    ap.add_argument("--art", action="store_true")
    a = ap.parse_args()

    if a.art:
        subprocess.run([sys.executable, os.path.join(HERE, "make_art.py")], check=True)

    tools = os.path.join(a.sdk, "Assets", "ConnectedStorageTools")
    for sub in ("Setup", "Builder"):
        dst = os.path.join(tools, sub, "Editor")
        os.makedirs(dst, exist_ok=True)
        for fn in os.listdir(os.path.join(HERE, "Editor", sub)):
            shutil.copy2(os.path.join(HERE, "Editor", sub, fn), dst)
    subprocess.run([sys.executable, os.path.join(HERE, "gen_assets.py"), a.sdk], check=True)

    work = tempfile.mkdtemp(prefix="connectedstorage_")
    unity(a.sdk, "ConnectedStorageSetup.ImportGame", os.path.join(work, "setup.log"))
    out = os.path.join(work, "out")
    unity(a.sdk, "ConnectedStorageBuilder.BuildAll", os.path.join(work, "build.log"), "-storageOut", out)

    src = os.path.join(out, "ConnectedStorage", "Bundles")
    dst = os.path.join(MOD, "Bundles")
    os.makedirs(dst, exist_ok=True)
    for fn in os.listdir(src):
        shutil.copy2(os.path.join(src, fn), dst)
        print("  ", fn)
    print("bundles copied to", dst)


if __name__ == "__main__":
    main()
