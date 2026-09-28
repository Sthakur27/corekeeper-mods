"""Switch the local game between Sid's Overhaul and the separate mods (never both: every feature would load twice).

    python release/switch_mods.py overhaul   # rebuild + install SidsOverhaul, park the separate mods
    python release/switch_mods.py separate   # remove SidsOverhaul, restore the separate mods
    python release/switch_mods.py status

Parked mods go to CoreKeeper_Data/StreamingAssets/Mods_parked. Restart the game after switching
(mods are only scanned at startup). mod.io subscriptions (CoreLib, Mod Settings Menu, ...) are untouched.
"""
import os, shutil, subprocess, sys

sys.path.insert(0, os.path.dirname(__file__))
import build_overhaul as b

MODS_DIR = b.GAME_MODS
PARKED = os.path.join(os.path.dirname(MODS_DIR), "Mods_parked")
# folder names as installed in the game (FastAutoFishing installs as its own folder name)
SEPARATE = b.MODS


def installed(name):
    return os.path.isdir(os.path.join(MODS_DIR, name))


def status():
    print("overhaul installed:", installed(b.NAME))
    print("separate installed:", [m for m in SEPARATE if installed(m)])
    print("parked:", sorted(os.listdir(PARKED)) if os.path.isdir(PARKED) else [])


def to_overhaul():
    os.makedirs(PARKED, exist_ok=True)
    for m in SEPARATE:
        src = os.path.join(MODS_DIR, m)
        if os.path.isdir(src):
            dst = os.path.join(PARKED, m)
            if os.path.exists(dst):
                shutil.rmtree(dst)
            shutil.move(src, dst)
    subprocess.run([sys.executable, os.path.join(os.path.dirname(__file__), "build_overhaul.py"), "--install"], check=True)


def to_separate():
    """Remove the overhaul and install fresh copies of the separate mods from the repo."""
    dst = os.path.join(MODS_DIR, b.NAME)
    if os.path.isdir(dst):
        shutil.rmtree(dst)
    for m in SEPARATE:
        target = os.path.join(MODS_DIR, m)
        if os.path.exists(target):
            shutil.rmtree(target)
        shutil.copytree(os.path.join(b.ROOT, m), target, ignore=shutil.ignore_patterns("release", "*.pugbackup"))
    if os.path.isdir(PARKED):
        shutil.rmtree(PARKED)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "status"
    {"overhaul": to_overhaul, "separate": to_separate}.get(cmd, lambda: None)()
    status()
