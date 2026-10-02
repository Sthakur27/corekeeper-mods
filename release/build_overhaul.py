"""Assemble Sid's Overhaul (one mod containing every Sid mod) from the individual mod folders.

    python release/build_overhaul.py            # build into build/SidsOverhaul
    python release/build_overhaul.py --install  # ...and copy it into the game's Mods folder

The individual folders stay the source of truth (and keep working as standalone mods). This script
copies their Scripts/ into Scripts/<Mod>/, adds Overhaul/Scripts (the central SidsOverhaulMod),
re-patches the two asset bundles so their MonoBehaviours bind to the SidsOverhaul assembly, and writes
one manifest with every file. Features detect the overhaul via their InOverhaul(this) check.
"""
import json, os, shutil, sys

ROOT = r"C:\Users\Sid\CoreKeeperMods"
OUT = os.path.join(ROOT, "build", "SidsOverhaul")
GAME_MODS = r"C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods"
NAME = "SidsOverhaul"
GUID = "5d1a0c3e9b7f4e21a8c6d4b2f0e19a73"  # fixed forever, identifies the mod

MODS = [
    "LoadoutSharing", "FiveLoadouts", "SkillXPMultiplier", "FastAutoFishing",
    "DurabilityMultiplier", "BuffDurationFloor", "FasterMushrooms", "BiggerWateringCans",
    "BetterFishingLoot", "AutoReplant", "QuickBuff", "PotionSeller", "HardModeTuning", "KeepMinions", "PetEditor", "BossBonusLoot",
]
# Bundles whose MonoScripts bind to the old assembly name.
# MasterPetPlus is NOT included: it is a fork of Parcew's Master Pet and mod.io took it down;
# it stays a separate, side-loaded mod.
BUNDLE_MODS = {"FastAutoFishing": "mikufish"}


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


def main():
    if os.path.exists(OUT):
        shutil.rmtree(OUT)
    os.makedirs(OUT)
    files = []

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
            shutil.copy2(src, dst)
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

    manifest = {
        "guid": GUID, "name": NAME, "displayName": "Sid's Overhaul",
        "skipSafetyChecks": False, "disableScripts": False, "accessesExtraAssemblies": True,
        "disableHarmonyPatching": False, "requiredOn": 3,
        "files": files,
        "dependencies": [{"modName": "CoreLib", "required": True}, {"modName": "ModSettingsMenu", "required": True}],
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
