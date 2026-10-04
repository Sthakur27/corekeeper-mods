"""1280x720 mod.io listing image for Infinite Ore Boulders: a pickaxe striking an ore boulder whose health
bar stays full, ore chunks flying onto an ever-growing pile under an infinity sign. python make_infiniteore_logo.py"""
import os, sys, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageOps
from make_art import W, H, font, sprite, cave_bg, text_shadow, P, PICKAXE, SPARK

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "infiniteore_logo.png")

PO = dict(P, **{
    'm': (120, 112, 124),  # rock
    'M': (84, 78, 92),     # rock shade
    'l': (162, 154, 168),  # rock highlight
    'o': (214, 112, 58),   # copper ore
    'O': (255, 184, 112),  # ore shine
    'i': (255, 214, 100),  # infinity gold
    'I': (196, 140, 40),
})

BOULDER = [
    ".......kkkkkkkkkkk........",
    ".....kkmmmmmmmmmmmkk......",
    "....kmmllmmmmoommmmmkk....",
    "...kmmllmmmmoOOommmmmmk...",
    "..kmmmmmmmmoOookmmmmmmmk..",
    "..kmmmoommmmkkkmmmmmMMmk..",
    ".kmmmoOOommmmmmmmmmmMMmmk.",
    ".kmmmmoOkmmmmlmmmmmmmmmmk.",
    "kmmmmmmkmmmmmmmmmmoommmmmk",
    "kmmllmmmmmmmmmmmmoOOommmMk",
    "kmmmmmmmMMmmmmmmmmoOkmmMMk",
    "kmmmmoommMMmmmmmmmmkmmmMMk",
    "kMmmoOOommmmmmmooommmmmMMk",
    "kMMmmoOkmmmmmmoOOOkmmmMMMk",
    ".kMMmmkmmmmmmmmkkkmmMMMMk.",
    "..kMMMMMMMMMMMMMMMMMMMMk..",
    "...kkkkkkkkkkkkkkkkkkkk...",
]

ORE = [
    "..kk..",
    ".kOok.",
    "kOoook",
    "kooOok",
    ".kkkk.",
]

INFINITY = [
    "..kkkkk.....kkkkk..",
    ".kiiiiik...kiiiiik.",
    "kiikkkiik.kiikkkiik",
    "kik...kiikiik...kik",
    "kik....kiiik....kIk",
    "kik...kiikiik...kIk",
    "kiikkkiik.kiikkkIIk",
    ".kiiiiik...kIIIIIk.",
    "..kkkkk.....kkkkk..",
]


def main():
    img = cave_bg(seed=61, top=(20, 16, 30), bottom=(48, 32, 40)).convert("RGBA")

    # warm glow behind the boulder
    cx, top = 430, 262
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for r in range(280, 0, -8):
        a = int(80 * (1 - r / 280))
        gd.ellipse([cx - r, 370 - r * 0.8, cx + r, 370 + r * 0.8], fill=(255, 150, 80, a))
    img = Image.alpha_composite(img, glow)
    d = ImageDraw.Draw(img)

    boulder = sprite(BOULDER, PO, 12)
    bx = cx - boulder.width // 2
    img.paste(boulder, (bx, top), boulder)

    # pickaxe striking the top-right of the boulder
    pick = ImageOps.mirror(sprite(PICKAXE, P, 8))
    px, py = bx + boulder.width - 66, top - 30
    img.paste(pick, (px, py), pick)
    sp = sprite(SPARK, P, 5)
    for ox, oy in [(-34, 10), (-6, 66), (-52, 58)]:
        img.paste(sp, (px + ox, py + oy), sp)

    # ore chunks arcing from the boulder to the pile
    ore = sprite(ORE, PO, 7)
    sx, sy = bx + boulder.width - 20, top + 60
    ex, ey = 990, 470
    for t in [0.15, 0.35, 0.55, 0.75]:
        x = sx + (ex - sx) * t
        y = sy + (ey - sy) * t - 170 * math.sin(math.pi * t)
        img.paste(ore, (int(x), int(y)), ore)
    d = ImageDraw.Draw(img)
    for t in [i / 30 for i in range(2, 28, 2)]:
        x = sx + (ex - sx) * t + 20
        y = sy + (ey - sy) * t - 170 * math.sin(math.pi * t) + 18
        d.rectangle([x, y, x + 5, y + 5], fill=(255, 200, 140))

    # growing ore pile
    pile_cx, pile_base = 1040, 556
    rnd = random.Random(3)
    rows = [7, 6, 5, 4, 3, 2]
    for r, n in enumerate(rows):
        for j in range(n):
            x = pile_cx + (j - (n - 1) / 2) * 40 + rnd.randint(-4, 4)
            y = pile_base - ore.height - r * 28
            img.paste(ore, (int(x - ore.width / 2), int(y)), ore)

    inf = sprite(INFINITY, PO, 12)
    img.paste(inf, (pile_cx - inf.width // 2, 222), inf)

    # boulder health bar: always full
    d = ImageDraw.Draw(img)
    hx0, hx1, hy = cx - 150, cx + 150, top + boulder.height + 24
    d.rectangle([hx0 - 4, hy - 4, hx1 + 4, hy + 24], fill=(20, 14, 20))
    d.rectangle([hx0, hy, hx1, hy + 20], fill=(90, 210, 110))
    d.rectangle([hx0, hy, hx1, hy + 6], fill=(150, 240, 160))
    text_shadow(d, (cx, hy + 34), "BOULDER HP STAYS FULL", font(24), (190, 240, 190))

    text_shadow(d, (W // 2, 22), "INFINITE ORE BOULDERS", font(84), (255, 244, 236))
    text_shadow(d, (W // 2, 112), "Ore boulders never break.  Keep mining one forever.", font(28), (236, 210, 196))
    d.rounded_rectangle([W // 2 - 470, 640, W // 2 + 470, 706], radius=16, fill=(16, 10, 16, 230),
                        outline=(220, 140, 90), width=3)
    text_shadow(d, (W // 2, 656), "Normal drop rate.  Every boulder, copper to pandorium.  No settings.", font(24),
                (255, 225, 190))

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
