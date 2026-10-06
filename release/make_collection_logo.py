"""1280x720 Steam Workshop listing image for the "Sid's Core Keeper Mods" collection: a grid of twelve
mod tiles (each its own pixel icon), most ticked as kept and two greyed out, next to a big
SUBSCRIBE TO ALL button with a pixel cursor clicking it.
python make_collection_logo.py"""
import os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "collection_logo.png")

PC = dict(P, **{
    'c': (90, 200, 230),   # fish / water
    'C': (50, 130, 170),
    'p': (220, 90, 200),   # potion
    'P': (150, 50, 140),
    'v': (150, 100, 230),  # ender purple
    'V': (90, 60, 160),
    'e': (130, 240, 160),  # ender glow
    'n': (80, 170, 90),    # leaf
    'N': (40, 110, 60),
    'd': (110, 72, 44),    # soil / wood
    'D': (70, 46, 30),
    'b': (236, 228, 208),  # bone
    'B': (170, 156, 136),
    'R': (150, 36, 46),    # dark red
    'a': (150, 160, 180),  # gear steel
    'A': (96, 104, 126),
    'l': (255, 220, 80),   # bolt
})

ICONS = {
    "FISHING": [
        "..............",
        "........kkk...",
        ".......kcck...",
        "kk...kkkcckk..",
        "kck.kccccccck.",
        "kcckcccccwkcck",
        ".kcccccccccccck",
        ".kcccccCcccccck",
        "kcckcCCCCCcck.",
        "kck.kCCCCCck..",
        "kk...kkkkkk...",
        "..............",
    ],
    "POTIONS": [
        ".....kkkk.....",
        ".....kddk.....",
        ".....kkkk.....",
        "......kwk.....",
        "......kwk.....",
        "....kkwwkkk...",
        "...kwwpppppk..",
        "..kwpppppppPk.",
        "..kpppppppPPk.",
        "..kppppppPPPk.",
        "...kPPPPPPPk..",
        "....kkkkkkk...",
    ],
    "LOADOUTS": [
        "...........kk.",
        "..........kwk.",
        ".........kwsk.",
        "........kwsk..",
        ".......kwsk...",
        "..kk..kwsk....",
        "..kgkkwsk.....",
        "...kgwsk......",
        "....kggk......",
        "...kdkGgk.....",
        "..kdk..kk.....",
        ".kdk..........",
        ".kk...........",
    ],
    "FARMING": [
        "......kk......",
        ".....knnk.....",
        "..kk.knNk.kk..",
        ".knnkknNkknnk.",
        ".knnnnnNnnnNk.",
        "..kNnnnNnnNk..",
        "...kkNnNNkk...",
        ".....knNk.....",
        "..kkkknNkkkk..",
        ".kdddddddddDk.",
        "kddDddddDdddDk",
        "kkkkkkkkkkkkkk",
    ],
    "STASH": [
        "..kkkkkkkkkk..",
        ".kvvvvvvvvvvk.",
        "kvvvvvvvvvvvVk",
        "kVVVVVVVVVVVVk",
        "kkkkkkeekkkkkk",
        "kvvvvkeekvvvVk",
        "kvvvvvkkvvvvVk",
        "kvvvvvvvvvvvVk",
        "kVVVVVVVVVVVVk",
        "kkkkkkkkkkkkkk",
    ],
    "GOLDEN": [
        "...kkkkkkkk...",
        "..kyywggggGk..",
        ".kywggggggGGk.",
        "kkkkkkkkkkkkkk",
        ".kgyggggggGk..",
        "..kggggggGk...",
        "...kggggGk....",
        "....kgggk.....",
        ".....kGk......",
        "......k.......",
    ],
    "BOSS LOOT": [
        "...kkkkkkkk...",
        "..kbbbbbbbbk..",
        ".kbwbbbbbbbBk.",
        ".kbbbbbbbbbBk.",
        ".kbkkkbbkkkBk.",
        ".kbkRkbbkRkBk.",
        ".kbkkkbbkkkBk.",
        "..kBbbkkbbBk..",
        "...kbBbBbBk...",
        "...kbkbkbkk...",
        "....kkkkkk....",
    ],
    "SETTINGS": [
        ".....kkkk.....",
        "..kk.kaak.kk..",
        ".kaakkaakkaak.",
        ".kaaaaaaaaaak.",
        "..kaaakkaaak..",
        "kkkaakkkkaakkk",
        "kaaaak..kaaaak",
        "kAAAak..kaAAAk",
        "kkkAAkkkkAAkkk",
        "..kAAAkkAAAk..",
        ".kAAAAAAAAAAk.",
        ".kAAkkAAkkAAk.",
        "..kk.kAAk.kk..",
        ".....kkkk.....",
    ],
    "VEHICLES": [
        "......kk......",
        "......kwk.....",
        "......kwwk....",
        "......kwwwk...",
        "......kwwwwk..",
        "......kkkkkkk.",
        "......kd......",
        "kkkkkkkkkkkkkk",
        "kddddddddddddk",
        ".kdDdDdDdDdDk.",
        "..kkkkkkkkkk..",
        "ccCccCccCccCcc",
    ],
    "HIT SOUNDS": [
        "........k.....",
        ".......kk..k..",
        "......ksk...k.",
        "kkkkkkssk.k.k.",
        "kssssksSk..k.k",
        "kssssksSk..k.k",
        "kSSSSksSk..k.k",
        "kkkkkkSSk.k.k.",
        "......kSk...k.",
        ".......kk..k..",
        "........k.....",
    ],
    "QUICK BUFF": [
        "........kkkk..",
        ".......kllk...",
        "......kllk....",
        ".....kllk.....",
        "....kllkkkkk..",
        "...kllllllk...",
        "..kkkkkllk....",
        ".....kllk.....",
        "....kllk......",
        "...kllk.......",
        "..klk.........",
        "..kk..........",
    ],
    "SELLER": [
        "....kkkkkk....",
        "..kkggggggkk..",
        ".kgyggggggGGk.",
        ".kgygkkkkgGGk.",
        "kgyggkggggGGGk",
        "kgyggkkkkgGGGk",
        "kgyggggggkGGGk",
        "kgyggkkkkgGGGk",
        ".kgGggggggGGk.",
        ".kgGGGGGGGGGk.",
        "..kkGGGGGGkk..",
        "....kkkkkk....",
    ],
}

ORDER = ["LOADOUTS", "FISHING", "FARMING", "QUICK BUFF",
         "SELLER", "STASH", "VEHICLES", "SETTINGS",
         "GOLDEN", "BOSS LOOT", "HIT SOUNDS", "POTIONS"]
GREYED = {"VEHICLES", "HIT SOUNDS"}

CURSOR = [
    "k.........",
    "kk........",
    "kwk.......",
    "kwwk......",
    "kwwwk.....",
    "kwwwwk....",
    "kwwwwwk...",
    "kwwwwwwk..",
    "kwwwwwwwk.",
    "kwwwwkkkkk",
    "kwwkwwk...",
    "kwk.kwwk..",
    "kk...kwwk.",
    "......kwwk",
    ".......kk.",
]


def glow(img, cx, cy, rad, col, peak=110):
    g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(g)
    for r in range(rad, 0, -6):
        a = int(peak * (1 - r / rad) ** 1.4)
        gd.ellipse([cx - r, cy - r * 0.8, cx + r, cy + r * 0.8], fill=col + (a,))
    return Image.alpha_composite(img, g)


def grey(spr):
    r, g, b, a = spr.split()
    l = Image.merge("RGB", (r, g, b)).convert("L").point(lambda v: 40 + v // 3)
    return Image.merge("RGBA", (l, l, l, a.point(lambda v: v * 3 // 4)))


def tile(img, x, y, size, name, kept):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x + 5, y + 6, x + size + 5, y + size + 6], radius=14, fill=(6, 4, 10))
    if kept:
        d.rounded_rectangle([x, y, x + size, y + size], radius=14, fill=(48, 40, 70), outline=(150, 130, 210), width=4)
    else:
        d.rounded_rectangle([x, y, x + size, y + size], radius=14, fill=(30, 28, 36), outline=(78, 74, 88), width=4)
    rows = ICONS[name]
    scale = 6 if max(len(r) for r in rows) <= 14 and len(rows) <= 14 else 5
    s = sprite(rows, PC, scale)
    if not kept:
        s = grey(s)
    img.paste(s, (x + (size - s.width) // 2, y + (size - s.height) // 2), s)
    d = ImageDraw.Draw(img)
    # badge in the top-right corner
    bx, by, br = x + size - 6, y + 6, 19
    if kept:
        d.ellipse([bx - br - 2, by - br - 2, bx + br + 2, by + br + 2], fill=(10, 8, 14))
        d.ellipse([bx - br, by - br, bx + br, by + br], fill=(80, 196, 110), outline=(200, 255, 210), width=3)
        d.line([(bx - 9, by + 1), (bx - 2, by + 8), (bx + 10, by - 7)], fill=(250, 255, 250), width=5, joint="curve")
    else:
        d.ellipse([bx - br - 2, by - br - 2, bx + br + 2, by + br + 2], fill=(10, 8, 14))
        d.ellipse([bx - br, by - br, bx + br, by + br], fill=(40, 38, 46), outline=(110, 106, 120), width=3)
    text_shadow(d, (x + size // 2, y + size + 10), name, font(18),
                (226, 220, 240) if kept else (120, 116, 130))


def main():
    img = cave_bg(seed=27, top=(18, 14, 30), bottom=(40, 28, 50)).convert("RGBA")
    img = glow(img, 380, 390, 360, (140, 110, 230), peak=60)
    img = glow(img, 1000, 390, 260, (90, 220, 130), peak=70)

    d = ImageDraw.Draw(img)
    rnd = random.Random(9)
    for _ in range(30):
        x, y = rnd.randint(640, W - 20), rnd.randint(160, 600)
        s = rnd.choice([4, 6, 8])
        d.rectangle([x, y, x + s, y + s], fill=(rnd.randint(150, 220), 200, 255, 120))

    # grid of mod tiles: 4 columns x 3 rows
    size, gx, gy = 106, 30, 44
    x0, y0 = 70, 168
    for i, name in enumerate(ORDER):
        cx, cy = i % 4, i // 4
        tile(img, x0 + cx * (size + gx), y0 + cy * (size + gy), size, name, name not in GREYED)

    # right side: counter + SUBSCRIBE TO ALL button
    d = ImageDraw.Draw(img)
    rx = 980
    text_shadow(d, (rx, 186), "22", font(120), (190, 250, 200), shadow=(10, 30, 16))
    text_shadow(d, (rx, 304), "MODS IN ONE COLLECTION", font(24), (220, 230, 220))

    bx0, by0, bx1, by1 = 760, 360, 1200, 500
    d.rounded_rectangle([bx0 + 8, by0 + 10, bx1 + 8, by1 + 10], radius=24, fill=(6, 4, 10))
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=24, fill=(52, 150, 80), outline=(200, 255, 200), width=6)
    d.rounded_rectangle([bx0 + 12, by0 + 10, bx1 - 12, by0 + 40], radius=14, fill=(84, 186, 110))
    # plus sign
    px, py = bx0 + 70, (by0 + by1) // 2
    d.rectangle([px - 28, py - 8, px + 28, py + 8], fill=(250, 255, 250))
    d.rectangle([px - 8, py - 28, px + 8, py + 28], fill=(250, 255, 250))
    text_shadow(d, (bx0 + 270, py - 30), "SUBSCRIBE", font(52), (250, 255, 250), anchor="mm", shadow=(20, 70, 36))
    text_shadow(d, (bx0 + 270, py + 30), "TO ALL", font(52), (250, 255, 250), anchor="mm", shadow=(20, 70, 36))

    cur = sprite(CURSOR, PC, 6)
    img.paste(cur, (bx1 - 60, by1 - 22), cur)
    d = ImageDraw.Draw(img)
    text_shadow(d, (rx - 40, 556), "Drop any you don't want.", font(22), (200, 196, 214))

    text_shadow(d, (W // 2, 22), "SID'S CORE KEEPER MODS", font(80), (236, 228, 252))
    text_shadow(d, (W // 2, 112), "Every mod on its own. Subscribe to all, keep what you like.", font(28), (214, 206, 226))

    # caption box
    bx0, bx1 = 230, W - 230
    d.rounded_rectangle([bx0, 614, bx1, 698], radius=16, fill=(14, 10, 22, 230), outline=(170, 150, 220), width=3)
    text_shadow(d, (W // 2, 626), "22 mods. Each updates on its own.", font(25), (225, 215, 255))
    text_shadow(d, (W // 2, 660), "Or get them all in one: Sid's Overhaul.", font(25), (190, 236, 210))

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
