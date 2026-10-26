"""Downsample the approved high-resolution runtime art to a sane texture size.

    python ConnectedStorage/Unity/downscale_art.py [--width 96]

The approved art (import_approved_art.py) was packaged at 840-1220 px per one-tile object and drawn
with point filtering and no mipmaps; benchmarks showed those textures cost ~2 ms per frame in a base
full of storage units. One tile is only ~60-100 screen pixels even at 4K, so every view of an object
is resampled to TARGET px wide with the same factor (main, side, up, emissive, icon stay aligned).
World size is unchanged: ConnectedStorageBuilder and the runtime derive ArtScale and pivot from the
texture width. The full-resolution originals stay in git history (before this script's commit).
"""
import argparse
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).parent
NAMES = ["Workbench", "TinChest", "IronChest", "ScarletChest", "OctarineChest", "GalaxiteChest", "SolariteChest",
         "PandoriumChest", "CopperChest", "Terminal", "TinGem", "IronGem", "ScarletGem", "OctarineGem", "GalaxiteGem",
         "SolariteGem", "PandoriumGem"]  # same list ConnectedStorageBuilder resizes
SUFFIXES = ["", "_side", "_up", "_emissive", "_side_emissive", "_up_emissive", "_icon"]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--width", type=int, default=96)
    target = ap.parse_args().width
    for name in NAMES:
        main_path = ROOT / "Art" / (name + ".png")
        if not main_path.exists():
            continue
        width = Image.open(main_path).width
        if width <= target:
            print(f"{name}: {width}px, unchanged")
            continue
        factor = target / width
        for suffix in SUFFIXES:
            path = ROOT / "Art" / (name + suffix + ".png")
            if not path.exists():
                continue
            image = Image.open(path).convert("RGBA")
            size = (max(1, round(image.width * factor)), max(1, round(image.height * factor)))
            # Pillow resamples RGBA with premultiplied alpha, so edges do not pick up dark fringes.
            image.resize(size, Image.Resampling.LANCZOS, reducing_gap=3.0).save(path)
        print(f"{name}: {width}px -> {target}px (x{factor:.4f})")


if __name__ == "__main__":
    main()
