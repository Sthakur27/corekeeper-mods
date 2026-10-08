"""Armor Dye listing image (1280x720): one pixel-art chestplate in three dyes, a palette strip and a sword.
python make_armordye_logo.py  ->  armordye_logo.png"""
import colorsys
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow

CHEST = [
    "...OOO....OOO...",
    "..OLLLO..OLLLO..",
    ".OLMMMMOOMMMMLO.",
    "OLMMMMMMMMMMMMDO",
    "OLMMHMMMMMMHMMDO",
    "OLMMMMMDDMMMMMDO",
    ".OLMMMMDDMMMMDO.",
    "..OLMMMMMMMMDO..",
    "..OLMMHMMHMMDO..",
    "..OLMMMMMMMMDO..",
    "..OLMMMDDMMMDO..",
    "...OLMMMMMMDO...",
    "...OOOOOOOOOO...",
]

SWORD = [
    ".........OO",
    "........OLO",
    ".......OLMO",
    "......OLMO.",
    ".....OLMO..",
    "....OLMO...",
    ".O.OLMO....",
    ".OOLMO.....",
    "..OOO......",
    ".OHOOO.....",
    "OHO..O.....",
    "OO.........",
]


def shades(rgb):
    """outline / light / mid / dark / highlight from one base color."""
    h, l, s = colorsys.rgb_to_hls(*[c / 255 for c in rgb])
    def at(lv, sv=s):
        r, g, b = colorsys.hls_to_rgb(h, max(0, min(1, lv)), max(0, min(1, sv)))
        return (int(r * 255), int(g * 255), int(b * 255))
    return {"O": (28, 20, 30), "L": at(l + 0.22), "M": rgb, "D": at(l - 0.18), "H": at(l + 0.35, s * 0.6)}


def main():
    img = cave_bg(seed=7, top=(20, 16, 30), bottom=(40, 26, 52)).convert("RGBA")
    d = ImageDraw.Draw(img)

    dyes = [(214, 52, 52), (44, 186, 196), (150, 76, 220)]
    for i, base in enumerate(dyes):
        spr = sprite(CHEST, shades(base), 16)
        x = 260 + i * 300 - spr.width // 2 + 150
        img.alpha_composite(spr, (x, 250))

    sword = sprite(SWORD, shades((240, 196, 56)), 14)
    img.alpha_composite(sword, (1060, 420))

    # palette strip
    strip = [(220, 40, 40), (240, 130, 30), (240, 200, 50), (60, 190, 70), (40, 190, 200),
             (50, 100, 230), (150, 70, 220), (240, 110, 190), (235, 235, 235), (60, 60, 70)]
    sx, sy, sz = 1280 // 2 - len(strip) * 52 // 2, 520, 44
    for i, c in enumerate(strip):
        x = sx + i * 52
        d.rectangle([x - 3, sy - 3, x + sz + 2, sy + sz + 2], fill=(28, 20, 30))
        d.rectangle([x, sy, x + sz - 1, sy + sz - 1], fill=c)

    text_shadow(d, (W // 2, 70), "ARMOR DYE", font(108), (255, 236, 200))
    text_shadow(d, (W // 2, 610), "Per-item colors for armor, weapons and tools", font(36), (220, 210, 235))
    img.convert("RGB").save("armordye_logo.png")
    print("wrote armordye_logo.png")


if __name__ == "__main__":
    main()
