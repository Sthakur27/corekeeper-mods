"""Ender Chest art: the vanilla black chest (chestBlack, chestBlack_side, chestBlack_up from the game's
addressables bundle) recolored to purple obsidian with a glowing teal eye where the lock was, plus emissive
maps for the eye. Writes Art/*.png next to this script. Needs UnityPy + Pillow."""
import os
from PIL import Image
import UnityPy

HERE = os.path.dirname(os.path.abspath(__file__))
BUNDLE = r"C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all.bundle"
SOURCES = [("chestBlack", "EnderChest"), ("chestBlack_side", "EnderChest_side"), ("chestBlack_up", "EnderChest_up")]

# vanilla palette -> ender palette
M = {
    (14, 37, 34): (20, 10, 30), (11, 25, 23): (10, 5, 16), (35, 41, 48): (30, 18, 44),
    (56, 52, 71): (40, 24, 62), (72, 68, 84): (58, 36, 88), (71, 103, 126): (122, 74, 178),
    (7, 60, 49): (74, 26, 116), (42, 78, 74): (112, 52, 166),
    # lock -> eye
    (236, 193, 191): (214, 255, 244), (226, 161, 44): (64, 232, 186), (145, 91, 38): (22, 124, 104),
    (181, 128, 71): (44, 186, 154), (35, 40, 30): (6, 26, 22), (57, 52, 31): (14, 64, 54),
    (94, 42, 40): (40, 14, 52),
}
GLOW = {(214, 255, 244), (64, 232, 186), (22, 124, 104), (44, 186, 154), (14, 64, 54)}


def main():
    vanilla = {}
    for obj in UnityPy.load(BUNDLE).objects:
        if obj.type.name == "Texture2D":
            d = obj.read()
            if d.m_Name in dict(SOURCES):
                vanilla[d.m_Name] = d.image.convert("RGBA")
    out_dir = os.path.join(HERE, "Art")
    os.makedirs(out_dir, exist_ok=True)
    for src, dst in SOURCES:
        im = vanilla[src]
        out = Image.new("RGBA", im.size)
        em = Image.new("RGBA", im.size, (0, 0, 0, 0))
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = im.getpixel((x, y))
                if a == 0:
                    continue
                nc = M.get((r, g, b))
                if nc is None:
                    raise SystemExit(f"unmapped color {(r, g, b)} in {src} (vanilla art changed?)")
                out.putpixel((x, y), nc + (a,))
                if nc in GLOW:
                    em.putpixel((x, y), nc + (255,))
        out.save(os.path.join(out_dir, dst + ".png"))
        em.save(os.path.join(out_dir, dst + "_emissive.png"))
    print("art written to", out_dir)


if __name__ == "__main__":
    main()
