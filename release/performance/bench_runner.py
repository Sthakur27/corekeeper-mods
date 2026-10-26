"""Unattended Core Keeper benchmark runs (pilot stage: one mod setup, a few runs).

    python release/performance/bench_runner.py --runs 2 --label full
    python release/performance/bench_runner.py --runs 2 --label full --dry-run

Each run: refuse if the game is open, restore the pristine benchmark save (build/bench/pristine,
character slot 3 + world slot 4, cloned from Sid's top character and world), write the
ModDiagnostics control file, launch the game through Steam, wait for ModDiagnostics to report
done/error and for the game to quit, then copy the capture to build/bench/results/<session>/.
The control file is always removed afterwards so a normal launch never starts a benchmark.
"""
import argparse
import datetime
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(r"C:\Users\Sid\CoreKeeperMods")
PROFILE = Path.home() / "AppData/LocalLow/Pugstorm/Core Keeper/Steam/138826622"
DIAG = PROFILE / "mods" / "ModDiagnostics"
CONTROL = DIAG / "bench.txt"
STATUS = DIAG / "bench-status.txt"
PRISTINE = ROOT / "build" / "bench" / "pristine"
RESULTS = ROOT / "build" / "bench" / "results"
CHARACTER, WORLD = 3, 4
# pristine file -> live save path
SAVE_FILES = {
    "3.json": PROFILE / "saves" / "3.json",
    "4.world.gzip": PROFILE / "worlds" / "4.world.gzip",
    "4.worldinfo": PROFILE / "worldinfos" / "4.worldinfo",
    "4.json": PROFILE / "worldgenparams" / "4.json",
    "4.mapparts.gzip": PROFILE / "maps" / str(CHARACTER) / "4.mapparts.gzip",
}


def game_running():
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq CoreKeeper.exe"], capture_output=True, text=True).stdout
    return "CoreKeeper.exe" in out


def restore_save():
    for name, target in SAVE_FILES.items():
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(PRISTINE / name, target)
        # The game keeps .pugbackup twins; drop them so it cannot fall back to a played state.
        backup = target.with_name(target.name + ".pugbackup")
        if backup.exists():
            backup.unlink()


def read_status():
    if not STATUS.exists():
        return {}
    return dict(line.split("=", 1) for line in STATUS.read_text(encoding="utf-8").splitlines() if "=" in line)


def run_once(label, route, seconds, settle, config_name, session_dir, dry_run):
    if game_running():
        sys.exit("Core Keeper is running; close it first (the runner never kills the game).")
    restore_save()
    DIAG.mkdir(parents=True, exist_ok=True)
    STATUS.unlink(missing_ok=True)
    CONTROL.write_text(f"world={WORLD}\ncharacter={CHARACTER}\nroute={route}\nlabel={label}\n"
                       f"seconds={seconds}\nsettle={settle}\nconfig={config_name}\nquit=1\n", encoding="utf-8")
    if dry_run:
        print("dry run: wrote", CONTROL)
        CONTROL.unlink()
        return None
    started = time.time()
    try:
        subprocess.run(["cmd", "/c", "start", "", "steam://rungameid/1621690"], check=True)
        deadline = started + seconds + settle + 600
        status = {}
        while time.time() < deadline:
            time.sleep(5)
            status = read_status()
            if status.get("state") in ("done", "error"):
                break
            if time.time() - started > 120 and not game_running():
                status = {"state": "error", "detail": "game exited before finishing"}
                break
        else:
            status = {"state": "error", "detail": "timed out (game left running)"}
        while game_running() and time.time() < deadline + 60:
            time.sleep(3)
    finally:
        CONTROL.unlink(missing_ok=True)
    result = {"label": label, "config": config_name, "state": status.get("state"), "detail": status.get("detail", ""),
              "seconds": round(time.time() - started)}
    report = status.get("report", "")
    if report:
        src = PROFILE / "mods" / report
        if src.exists():
            shutil.copy2(src, session_dir / src.name)
            result["report"] = str(session_dir / src.name)
    print(json.dumps(result))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--runs", type=int, default=2)
    parser.add_argument("--label", default="full")
    parser.add_argument("--config-name", default="current")
    parser.add_argument("--route", default="base")
    parser.add_argument("--seconds", type=int, default=60)
    parser.add_argument("--settle", type=int, default=20)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    if not all((PRISTINE / name).exists() for name in SAVE_FILES):
        sys.exit("Missing pristine benchmark save in " + str(PRISTINE))
    session_dir = RESULTS / datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    session_dir.mkdir(parents=True, exist_ok=True)
    results = [run_once(f"{args.label}-{i + 1}", args.route, args.seconds, args.settle, args.config_name, session_dir, args.dry_run)
               for i in range(args.runs)]
    (session_dir / "runs.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    print("session:", session_dir)


if __name__ == "__main__":
    main()
