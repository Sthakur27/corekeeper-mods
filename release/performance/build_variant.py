"""Build an Overhaul variant without some features' code, for benchmarking only (never install or publish).

    python release/performance/build_variant.py no-cs ConnectedStorage
    python release/performance/build_variant.py no-loadouts LoadoutSharing,FiveLoadouts,EnderStash
    python release/performance/build_variant.py no-bundles "" --no-bundles

Output: build/bench/variants/<name>/ (same layout as build/SidsOverhaul). The asset bundles are kept,
so the features' placed objects stay in the world and only their scripts are missing: the difference
to the full build is the code's cost, not the cost of drawing the objects. --no-bundles is the reverse:
all code, no asset bundles, so the mod-added objects (storage units, Ender Chest, Magic Mirror) are
missing from the world and their code finds no object types.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import build_overhaul as bo  # noqa: E402  (release/build_overhaul.py)


def main():
    no_bundles = "--no-bundles" in sys.argv
    args = [a for a in sys.argv[1:] if a != "--no-bundles"]
    if len(args) != 2:
        sys.exit(__doc__)
    name, drop = args[0], [m for m in args[1].split(",") if m]
    if no_bundles:
        bo.BUNDLE_MODS = {}
    unknown = [m for m in drop if m not in bo.MODS]
    if unknown:
        sys.exit("not in build_overhaul.MODS: " + ", ".join(unknown))
    bo.MODS = [m for m in bo.MODS if m not in drop]
    bo.OUT = os.path.join(bo.ROOT, "build", "bench", "variants", name)
    sys.argv = [sys.argv[0]]  # never --install
    bo.main()


if __name__ == "__main__":
    main()
