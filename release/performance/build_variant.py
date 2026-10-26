"""Build an Overhaul variant without some features' code, for benchmarking only (never install or publish).

    python release/performance/build_variant.py no-cs ConnectedStorage
    python release/performance/build_variant.py no-loadouts LoadoutSharing,FiveLoadouts,EnderStash

Output: build/bench/variants/<name>/ (same layout as build/SidsOverhaul). The asset bundles are kept,
so the features' placed objects stay in the world and only their scripts are missing: the difference
to the full build is the code's cost, not the cost of drawing the objects.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import build_overhaul as bo  # noqa: E402  (release/build_overhaul.py)


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    name, drop = sys.argv[1], [m for m in sys.argv[2].split(",") if m]
    unknown = [m for m in drop if m not in bo.MODS]
    if unknown:
        sys.exit("not in build_overhaul.MODS: " + ", ".join(unknown))
    bo.MODS = [m for m in bo.MODS if m not in drop]
    bo.OUT = os.path.join(bo.ROOT, "build", "bench", "variants", name)
    sys.argv = [sys.argv[0]]  # never --install
    bo.main()


if __name__ == "__main__":
    main()
