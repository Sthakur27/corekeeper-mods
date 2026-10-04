"""Publish Sid's Overhaul and every individual mod to the Core Keeper Steam Workshop.

    python release/workshop_publish.py --dry-run          # stage + build the uploader, no upload
    python release/workshop_publish.py                    # upload everything (Core Keeper closed, Steam running)
    python release/workshop_publish.py --only QuickBuff,SidsOverhaul --changelog "Quick Food key"

Uploads go through release/workshop/WorkshopUploader.cs, which uses the game's Facepunch.Steamworks
against the Steam client that is already logged in (no password). Workshop item ids are kept in
release/workshop_ids.json, so later runs update the same items. A Workshop item is a folder with
ModManifest.json at its root (same as a mod.io zip); the game only loads it when a Version tag
matches the game (major.minor.patch).
"""
import argparse, glob, json, os, re, shutil, struct, subprocess, sys

ROOT = r"C:\Users\Sid\CoreKeeperMods"
REL = os.path.join(ROOT, "release")
STAGE = os.path.join(ROOT, "build", "workshop")
TOOL = os.path.join(ROOT, "build", "workshop_tool")
IDS = os.path.join(REL, "workshop_ids.json")
GAME = r"C:\Program Files (x86)\Steam\steamapps\common\Core Keeper"
GITHUB = "https://github.com/Sthakur27/corekeeper-mods/tree/main/"

GAME_VERSION = "1.3.0"  # Version tag; bump (or add) after a game update
CORE_DEPS = {"CoreLib": 3673516180}  # Workshop ids of third-party library mods
# Required items to drop from every item (Mod Settings Menu, replaced by our own Mod Options in overhaul 1.1.0)
STALE_DEPS = ["3791299348"]

# key (= mod folder), title, logo in release/, category tag
MODS = [
    ("ModOptions", "Mod Options", "modoptions_logo.png", "Library"),  # first: other mods require it
    ("LoadoutSharing", "Loadout Fallback", "loadout_logo.png", "Quality of Life"),
    ("FiveLoadouts", "Five Loadouts", "fiveloadouts_logo.png", "Quality of Life"),
    ("SkillXPMultiplier", "Skill XP Multiplier", "skillxp-logo.png", "Quality of Life"),
    ("FastAutoFishing", "Fast Auto Fishing", "fastfish_logo.png", "Quality of Life"),
    ("DurabilityMultiplier", "Durability Multiplier", "durability_logo.png", "Quality of Life"),
    ("BuffDurationFloor", "Buff Duration Floor", "buffduration_logo.png", "Quality of Life"),
    ("FasterMushrooms", "Faster Mushrooms", "fastermushrooms_logo.png", "Quality of Life"),
    ("BiggerWateringCans", "Bigger Watering Cans", "wateringcans_logo.png", "Quality of Life"),
    ("BetterFishingLoot", "Better Fishing Loot", "betterfishingloot_logo.png", "Quality of Life"),
    ("AutoReplant", "Auto Replant", "autoreplant_logo.png", "Quality of Life"),
    ("QuickBuff", "Quick Buff", "quickbuff_logo.png", "Quality of Life"),
    ("PotionSeller", "Potion Seller", "potionseller_logo.png", "Quality of Life"),
    ("DifficultyTuning", "Difficulty Tuning", "difficultytuning_logo.png", "Other"),
    ("KeepMinions", "Keep Minions On Teleport", "keepminions_logo.png", "Quality of Life"),
    ("PetEditor", "Pet Editor", "peteditor_logo.png", "Quality of Life"),
    ("BossBonusLoot", "Boss Bonus Loot", "bossloot_logo.png", "Item"),
    ("EnderStash", "Ender Stash", "enderstash_logo.png", "Quality of Life"),
    ("VehicleSpeed", "Vehicle Speed", "vehiclespeed_logo.png", "Quality of Life"),
    ("InfiniteOreBoulders", "Infinite Ore Boulders", "infiniteore_logo.png", "Quality of Life"),
    ("GoldenChance", "Golden Chance", "goldenchance_logo.png", "Quality of Life"),
]
OVERHAUL = ("SidsOverhaul", "Sid's Overhaul", "overhaul_logo.png", "Overhaul")


# ------------------------------------------------------------------ README -> Steam BBCode

def inline(s):
    s = re.sub(r"\*\*(.+?)\*\*", r"[b]\1[/b]", s)
    s = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"[i]\1[/i]", s)
    s = re.sub(r"`([^`]+)`", r"\1", s)
    s = re.sub(r"\[([^\]]+)\]\((https?://[^)]+)\)", r"[url=\2]\1[/url]", s)
    s = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r"\1", s)  # relative links: keep the text
    return s


def to_bbcode(md):
    out, lines, i = [], md.replace("\r\n", "\n").split("\n"), 0
    while i < len(lines):
        line = lines[i]
        if line.startswith("```"):
            block = []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                block.append(lines[i])
                i += 1
            out.append("[code]" + "\n".join(block) + "[/code]")
            i += 1
            continue
        m = re.match(r"^(#{1,3})\s+(.*)", line)
        if m:
            n = len(m.group(1))
            out.append(f"[h{n}]{inline(m.group(2))}[/h{n}]")
            i += 1
            continue
        if line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-{2,}:?", c) for c in cells):
                    rows.append(cells)
                i += 1
            t = ["[table]"]
            for r, cells in enumerate(rows):
                tag = "th" if r == 0 else "td"
                t.append("[tr]" + "".join(f"[{tag}]{inline(c)}[/{tag}]" for c in cells) + "[/tr]")
            t.append("[/table]")
            out.append("\n".join(t))
            continue
        if re.match(r"^\s*([-*]|\d+\.)\s+", line):
            ordered = bool(re.match(r"^\s*\d+\.", line))
            items = []
            while i < len(lines) and (re.match(r"^\s*([-*]|\d+\.)\s+", lines[i]) or (lines[i].startswith("  ") and items)):
                if re.match(r"^\s*([-*]|\d+\.)\s+", lines[i]):
                    items.append(re.sub(r"^\s*([-*]|\d+\.)\s+", "", lines[i]))
                else:
                    items[-1] += " " + lines[i].strip()
                i += 1
            tag = "olist" if ordered else "list"
            out.append(f"[{tag}]" + "".join(f"[*]{inline(x)}" for x in items) + f"[/{tag}]")
            continue
        out.append(inline(line))
        i += 1
    return "\n".join(out).strip()


def description(key, folder, readme_path):
    md = open(readme_path, encoding="utf-8").read() if os.path.exists(readme_path) else ""
    md = re.sub(r"^#\s+.*\n", "", md, count=1)  # Steam shows the title already
    if key == OVERHAUL[0]:
        note = ("[b]All of Sid's mods in one.[/b] Do not also subscribe to the individual mods (each feature "
                "would run twice). Requires CoreLib. Settings: Settings > Mod Options (built in).")
    elif key == "ModOptions":
        note = ("[b]Library.[/b] Mods that use it list it as a required item, so Steam installs it for you. "
                "Works alongside Mod Settings Menu.")
    else:
        note = ("Also included in [url=https://steamcommunity.com/workshop/filedetails/?id={overhaul}]Sid's Overhaul[/url]; "
                "use one or the other, not both.")
    footer = f"\n\nSource and full docs: [url={GITHUB}{folder}]{GITHUB}{folder}[/url]"
    body = to_bbcode(md)
    text = note + "\n\n" + body
    if len(text) + len(footer) > 7900:
        text = text[: 7900 - len(footer) - 20].rsplit("\n", 1)[0] + "\n[i](continued on GitHub)[/i]"
    return text + footer


# ------------------------------------------------------------------ staging

def stage_mod(key):
    src = os.path.join(ROOT, key)
    dst = os.path.join(STAGE, key)
    shutil.rmtree(dst, ignore_errors=True)
    man = json.load(open(os.path.join(src, "ModManifest.json"), encoding="utf-8-sig"))
    paths = ["ModManifest.json", "README.md"] + [f["path"] for f in man["files"]]
    for rel in paths:
        s = os.path.join(src, rel)
        if os.path.exists(s):
            os.makedirs(os.path.dirname(os.path.join(dst, rel)), exist_ok=True)
            shutil.copy2(s, os.path.join(dst, rel))
    return dst, man


def stage_overhaul():
    subprocess.run([sys.executable, os.path.join(REL, "build_overhaul.py")], check=True)
    dst = os.path.join(STAGE, OVERHAUL[0])
    shutil.rmtree(dst, ignore_errors=True)
    shutil.copytree(os.path.join(ROOT, "build", "SidsOverhaul"), dst)
    man = json.load(open(os.path.join(dst, "ModManifest.json"), encoding="utf-8-sig"))
    return dst, man


# ------------------------------------------------------------------ uploader build

def is_managed(path):
    try:
        with open(path, "rb") as f:
            data = f.read(4096)
        pe = struct.unpack_from("<I", data, 0x3C)[0]
        magic = struct.unpack_from("<H", data, pe + 24)[0]
        dd = pe + 24 + (112 if magic == 0x20B else 96)
        clr_rva = struct.unpack_from("<I", data, dd + 14 * 8)[0]
        return clr_rva != 0
    except Exception:
        return False


def find_csc():
    for p in glob.glob(os.path.expandvars(r"%LOCALAPPDATA%\Temp\claude\*\*\scratchpad\csc\pkg\tasks\netcore\bincore\csc.dll")):
        return p
    tools = os.path.join(ROOT, "build", "tools")
    p = os.path.join(tools, "csc", "tasks", "netcore", "bincore", "csc.dll")
    if not os.path.exists(p):
        os.makedirs(tools, exist_ok=True)
        nupkg = os.path.join(tools, "toolset.nupkg")
        subprocess.run(["curl", "-sL", "-o", nupkg, "https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset"], check=True)
        shutil.unpack_archive(nupkg, os.path.join(tools, "csc"), "zip")
    return p


def build_tool():
    fw = sorted(glob.glob(r"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.*"))[-1]
    facepunch = os.path.join(GAME, "CoreKeeper_Data", "Managed", "Facepunch.Steamworks.Win64.dll")
    os.makedirs(TOOL, exist_ok=True)
    refs = [p for p in glob.glob(os.path.join(fw, "*.dll")) if is_managed(p)] + [facepunch]
    rsp = os.path.join(TOOL, "build.rsp")
    with open(rsp, "w", encoding="utf-8") as f:
        f.write("-nologo -nostdlib -target:exe -langversion:latest -nowarn:CS8632\n")
        for r in refs:
            f.write(f'-r:"{r}"\n')
        f.write(f'-out:"{os.path.join(TOOL, "WorkshopUploader.dll")}"\n')
        f.write(f'"{os.path.join(REL, "workshop", "WorkshopUploader.cs")}"\n')
    r = subprocess.run(["dotnet", find_csc(), "@" + rsp], capture_output=True, text=True)
    errors = [l for l in (r.stdout + r.stderr).splitlines() if "error CS" in l]
    if errors or r.returncode != 0:
        print("\n".join(errors) or r.stdout + r.stderr)
        sys.exit("uploader build failed")
    major = os.path.basename(fw).split(".")[0]
    json.dump({"runtimeOptions": {"tfm": f"net{major}.0", "framework": {"name": "Microsoft.NETCore.App", "version": f"{major}.0.0"}}},
              open(os.path.join(TOOL, "WorkshopUploader.runtimeconfig.json"), "w"))
    shutil.copy2(facepunch, TOOL)
    shutil.copy2(os.path.join(GAME, "CoreKeeper_Data", "Plugins", "x86_64", "steam_api64.dll"), TOOL)
    open(os.path.join(TOOL, "steam_appid.txt"), "w").write("1621690")
    print(f"uploader built in {TOOL}")


# ------------------------------------------------------------------ main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--changelog", default="")
    a = ap.parse_args()
    only = set(x for x in a.only.split(",") if x)

    ids = json.load(open(IDS)) if os.path.exists(IDS) else {}
    entries = [OVERHAUL] + MODS
    items = []
    for key, title, logo, category in entries:
        if only and key not in only:
            continue
        dst, man = stage_overhaul() if key == OVERHAUL[0] else stage_mod(key)
        deps = []
        for d in man.get("dependencies", []):
            if not d.get("required"):
                continue
            if d["modName"] in CORE_DEPS:
                deps.append(str(CORE_DEPS[d["modName"]]))
            elif any(m[0] == d["modName"] for m in MODS):
                deps.append(d["modName"])  # resolved by the uploader from workshop_ids.json (uploaded earlier in MODS order)
            else:
                print(f"  note: {key} requires {d['modName']}, which is not one of our Workshop items")
        folder = "Overhaul" if key == OVERHAUL[0] else key
        desc = description(key, folder, os.path.join(dst, "README.md"))  # "{overhaul}" filled in by the uploader
        preview = os.path.join(REL, logo)
        if not os.path.exists(preview):
            print(f"  WARNING: {key} has no preview image ({logo})")
            preview = ""
        items.append({
            "key": key, "title": title, "description": desc, "content": dst, "preview": preview,
            "tags": [category, GAME_VERSION, "Client", "Server", "Script"],
            "changelog": a.changelog, "dependencies": deps, "removeDependencies": STALE_DEPS,
        })
        print(f"staged {key}: {sum(len(fs) for _, _, fs in os.walk(dst))} files, {len(desc)} chars, deps {deps}")

    items_path = os.path.join(STAGE, "items.json")
    json.dump(items, open(items_path, "w", encoding="utf-8"), indent=2)
    build_tool()
    if a.dry_run:
        print(f"dry run: {len(items)} items staged, see {items_path}")
        return
    if "CoreKeeper.exe" in subprocess.run(["tasklist"], capture_output=True, text=True).stdout:
        sys.exit("Close Core Keeper first (the uploader registers with Steam as the game).")
    r = subprocess.run(["dotnet", os.path.join(TOOL, "WorkshopUploader.dll"), items_path, IDS], cwd=TOOL)
    sys.exit(r.returncode)


if __name__ == "__main__":
    main()
