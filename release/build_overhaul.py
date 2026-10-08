"""Assemble Sid's Overhaul (one mod containing every Sid mod) from the individual mod folders.

    python release/build_overhaul.py            # build into build/SidsOverhaul
    python release/build_overhaul.py --install  # ...and copy it into the game's Mods folder

The individual folders stay the source of truth (and keep working as standalone mods). This script
copies their Scripts/ into Scripts/<Mod>/, adds Overhaul/Scripts (the central SidsOverhaulMod),
re-patches the two asset bundles so their MonoBehaviours bind to the SidsOverhaul assembly, and writes
one manifest with every file. Features detect the overhaul via their InOverhaul(this) check.
"""
import json, os, re, shutil, sys

ROOT = r"C:\Users\Sid\CoreKeeperMods"
OUT = os.path.join(ROOT, "build", "SidsOverhaul")
GAME_MODS = r"C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods"
NAME = "SidsOverhaul"
GUID = "5d1a0c3e9b7f4e21a8c6d4b2f0e19a73"  # fixed forever, identifies the mod

MODS = [
    "ModOptions", "LoadoutSharing", "FiveLoadouts", "SkillXPMultiplier", "FastAutoFishing",
    "DurabilityMultiplier", "BuffDurationFloor", "FasterMushrooms", "BiggerWateringCans",
    "BetterFishingLoot", "AutoReplant", "QuickBuff", "PotionSeller", "FishSeller", "MerchantRestock", "DifficultyTuning", "KeepMinions", "PetEditor", "BossBonusLoot", "EnderStash", "VehicleSpeed", "GoldenChance", "InfiniteOreBoulders", "StimHits", "ArmorDye",
]
# Bundles whose MonoScripts bind to the old assembly name.
# MasterPetPlus is NOT included: it is a fork of Parcew's Master Pet and mod.io took it down;
# it stays a separate, side-loaded mod.
# EnderStash's bundle (Ender Chest prefab) has no MonoScripts of its own (vanilla Chest only), so
# patching is a no-op copy; it is listed so the bundle ships with the overhaul.
# ORDER MATTERS: the loader registers ONE data-block loader per mod guid (ScriptableData.AddDataBlocksLoader
# rejects the second bundle: "Data block loader already added"), so only the first bundle's data blocks
# (SpriteAsset, TextDataBlock, Entity/GraphicalObjectDataBlock) are seen. EnderStash's bundle has them,
# FastAutoFishing's has none, so EnderStash goes first. A third bundle with data blocks would need merging.
BUNDLE_MODS = {"EnderStash": "EnderStash", "FastAutoFishing": "mikufish"}


def patch_bundle(src, dst, old_assembly):
    import UnityPy
    env = UnityPy.load(src)
    n = 0
    for obj in env.objects:
        if obj.type.name == "MonoScript":
            tree = obj.read_typetree()
            if tree.get("m_AssemblyName") in (old_assembly, old_assembly + ".dll"):
                tree["m_AssemblyName"] = tree["m_AssemblyName"].replace(old_assembly, NAME)
                obj.save_typetree(tree)
                n += 1
    with open(dst, "wb") as f:
        f.write(env.file.save(packer="lz4"))
    return n


# ---------------------------------------------------------------- feature switches
# Every feature listed in Overhaul/Scripts/Features.cs (Features.All) gets guards so it can be switched
# off from Mod Options ("Features On/Off"). They are added here, at build time, so the standalone mods
# stay untouched:
#   - Harmony patch classes: static bool Prepare() => Features.On(key)   (Harmony skips the class)
#   - IMod EarlyInit/Init/Update/ModObjectLoaded: return early
#   - system OnUpdate: disable the system
#   - CoreLib chat commands: answer "switched off"
GUARD = 'global::SidsOverhaul.Features.On("{key}")'


def read_switchable():
    text = open(os.path.join(ROOT, "Overhaul", "Scripts", "Features.cs"), encoding="utf-8").read()
    return dict(re.findall(r'\{\s*"(\w+)",\s*"([^"]+)"\s*\}', text))


def gate_source(text, key, title, counts):
    guard = GUARD.format(key=key)
    nl = "\r\n" if "\r\n" in text else "\n"
    prepare = f"static bool Prepare() => {guard};"

    # Harmony patch classes (class-level [HarmonyPatch], possibly several attribute lines).
    out, pending, inject = [], False, False
    for line in text.split(nl):
        out.append(line)
        stripped = line.strip()
        indent = re.match(r"\s*", line).group(0)
        if inject:
            if "{" in stripped:
                out.append(indent + "    " + prepare)
                counts["patches"] += 1
                inject = False
            continue
        if stripped.startswith("[HarmonyPatch"):
            pending = True
        elif pending and re.search(r"\bclass\s+\w+", stripped):
            pending = False
            if "{" in stripped:  # brace on the class line
                out.append(indent + "    " + prepare)
                counts["patches"] += 1
            else:
                inject = True
        elif stripped and not stripped.startswith(("[", "//")):
            pending = False
    text = nl.join(out)

    def sub(pattern, repl, name):
        nonlocal text
        text, n = re.subn(pattern, repl, text)
        counts[name] += n

    if re.search(r":\s*IMod\b", text):
        sig = r"(public void (?:EarlyInit|Init|Update|ModObjectLoaded)\([^)]*\))"
        sub(sig + r"\s*=>\s*([^;]+);", lambda m: f"{m.group(1)} {{ if (!{guard}) return; {m.group(2)}; }}", "imod")
        sub(sig + r"(\s*\{)(?! if \(!global::)", lambda m: f"{m.group(1)}{m.group(2)} if (!{guard}) return;", "imod")
    sub(r"(protected override void OnUpdate\(\)\s*\{)",
        lambda m: f"{m.group(1)} if (!{guard}) {{ Enabled = false; return; }}", "systems")
    sub(r"(public CommandOutput Execute\([^)]*\)\s*\{)",
        lambda m: f'{m.group(1)} if (!{guard}) return new CommandOutput("{title} is switched off (Mod Options > Features On/Off).", CommandStatus.Warning);',
        "commands")
    return text


def copy_script(src, dst, mod, switchable, counts):
    if mod not in switchable or not src.endswith(".cs"):
        shutil.copy2(src, dst)
        return
    text = open(src, encoding="utf-8-sig", newline="").read()
    c = counts.setdefault(mod, {"patches": 0, "imod": 0, "systems": 0, "commands": 0})
    open(dst, "w", encoding="utf-8", newline="").write(gate_source(text, mod, switchable[mod], c))


def main():
    if os.path.exists(OUT):
        shutil.rmtree(OUT)
    os.makedirs(OUT)
    files = []
    switchable = read_switchable()
    counts = {}
    unknown = [m for m in switchable if m not in MODS]
    if unknown:
        sys.exit(f"Features.cs lists mods that are not in MODS: {unknown}")

    def add(rel, guid=""):
        files.append({"path": rel.replace("\\", "/"), "guid": guid})

    for mod in MODS:
        man = json.load(open(os.path.join(ROOT, mod, "ModManifest.json"), encoding="utf-8"))
        for f in man["files"]:
            path = f["path"]
            src = os.path.join(ROOT, mod, path)
            if path.startswith("Scripts/"):
                rel = f"Scripts/{mod}/{path[len('Scripts/'):]}"
            elif path.startswith("Bundles/"):
                continue  # handled below
            else:
                rel = path  # e.g. Localization/Localization.csv (only FastAutoFishing has one)
            dst = os.path.join(OUT, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            copy_script(src, dst, mod, switchable, counts)
            add(rel, f.get("guid", ""))

    os.makedirs(os.path.join(OUT, "Bundles"), exist_ok=True)
    for mod, old in BUNDLE_MODS.items():
        bdir = os.path.join(ROOT, mod, "Bundles")
        for fn in sorted(os.listdir(bdir)):
            src = os.path.join(bdir, fn)
            dst = os.path.join(OUT, "Bundles", fn)
            if fn.endswith(".assetbundle"):
                n = patch_bundle(src, dst, old)
                print(f"  bundle {fn}: {n} MonoScripts -> {NAME}")
            else:
                shutil.copy2(src, dst)
            add(f"Bundles/{fn}")

    for r, _, fs in os.walk(os.path.join(ROOT, "Overhaul", "Scripts")):
        for fn in fs:
            src = os.path.join(r, fn)
            rel = "Scripts/Overhaul/" + os.path.relpath(src, os.path.join(ROOT, "Overhaul", "Scripts"))
            dst = os.path.join(OUT, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)
            add(rel, "")
    shutil.copy2(os.path.join(ROOT, "Overhaul", "README.md"), os.path.join(OUT, "README.md"))
    for mod in switchable:
        c = counts.get(mod, {})
        print(f"  switch {mod}: {c.get('patches', 0)} patch classes, {c.get('imod', 0)} IMod methods, "
              f"{c.get('systems', 0)} systems, {c.get('commands', 0)} commands")
        if not c.get("imod"):
            sys.exit(f"feature switch for {mod}: no IMod method guarded, check gate_source")

    manifest = {
        "guid": GUID, "name": NAME, "displayName": "Sid's Overhaul",
        "skipSafetyChecks": False, "disableScripts": False, "accessesExtraAssemblies": True,
        "disableHarmonyPatching": False, "requiredOn": 3,
        "files": files,
        "dependencies": [{"modName": "CoreLib", "required": True}],
    }
    json.dump(manifest, open(os.path.join(OUT, "ModManifest.json"), "w", encoding="utf-8"), indent=4)
    print(f"built {OUT}: {len(files)} files from {len(MODS)} mods")

    if "--install" in sys.argv:
        dst = os.path.join(GAME_MODS, NAME)
        if os.path.exists(dst):
            shutil.rmtree(dst)
        shutil.copytree(OUT, dst)
        print("installed", dst)


if __name__ == "__main__":
    main()
