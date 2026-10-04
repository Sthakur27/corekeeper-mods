"""1280x720 mod.io listing image for Ender Stash: an open chest glowing purple, items falling into it,
the purple stash button, and the 40-slot stash grid. python make_enderstash_logo.py"""
import os, sys, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P, SWORD, RING, FISH, POTION, PB, LANTERN

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "enderstash_logo.png")

PE = dict(PB, **{
    'v': (130, 60, 210),   # portal deep
    'V': (185, 120, 255),  # portal mid
    'p': (240, 215, 255),  # portal core
    'd': (82, 54, 34),     # wood dark
    'D': (122, 82, 50),    # wood
    'h': (160, 112, 70),   # wood highlight
})

# open chest: lid tilted back, glowing purple interior, wooden body with gold trim and lock
CHEST_OPEN = [
    "...kkkkkkkkkkkkkkkkkk...",
    "..kDDDDDDDDDDDDDDDDDDk..",
    "..kDhhhhhhhhhhhhhhhhDk..",
    "..kDDDDDDDDDDDDDDDDDDk..",
    "..kddddddddddddddddddk..",
    ".kgggggggggggggggggggGk.",
    "kkkkkkkkkkkkkkkkkkkkkkkk",
    "kvvvvvVVVVVVVVVVVVvvvvvk",
    "kvvVVVVppppppppppVVVVvvk",
    "kvVVpppppwwwwwwpppppVVvk",
    "kkkkkkkkkkkkkkkkkkkkkkkk",
    "kgggggggggggggggggggggGk",
    "kDDDDDDDDDDDDDDDDDDDDDDk",
    "kDhDDDDDDDDkkDDDDDDDDhDk",
    "kDhDDDDDDDkggkDDDDDDDhDk",
    "kDhDDDDDDDkgGkDDDDDDDhDk",
    "kDhDDDDDDDkGGkDDDDDDDhDk",
    "kDhDDDDDDDDkkDDDDDDDDhDk",
    "kDDDDDDDDDDDDDDDDDDDDDDk",
    "kgggggggggggggggggggggGk",
    "kddddddddddddddddddddddk",
    "kkkkkkkkkkkkkkkkkkkkkkkk",
]

# small chest icon for the purple button
BTN_CHEST = [
    ".kkkkkkkkkk.",
    "kwwwwwwwwwwk",
    "kwVVVVVVVVwk",
    "kkkkkkkkkkkk",
    "kwwwwkkwwwwk",
    "kwVVVkkVVVwk",
    "kwVVVVVVVVwk",
    "kkkkkkkkkkkk",
]

SPARK = [
    "..p..",
    ".pVp.",
    "pVwVp",
    ".pVp.",
    "..p..",
]


def main():
    img = cave_bg(seed=41, top=(18, 14, 34), bottom=(44, 24, 66)).convert("RGBA")

    cx, chest_top = 360, 272
    scale = 13
    chest = sprite(CHEST_OPEN, PE, scale)
    mouth_y = chest_top + 8 * scale  # middle of the glowing interior

    # purple aura around the chest
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for r in range(300, 0, -6):
        a = int(110 * (1 - r / 300) ** 1.4)
        gd.ellipse([cx - r, mouth_y - r * 0.85, cx + r, mouth_y + r * 0.85], fill=(170, 90, 255, a))
    # light column rising from the interior
    for i in range(14):
        w = 120 - i * 7
        a = 10 + i * 4
        gd.rectangle([cx - w, 165, cx + w, mouth_y], fill=(200, 150, 255, a))
    img = Image.alpha_composite(img, glow)
    d = ImageDraw.Draw(img)

    img.paste(chest, (cx - chest.width // 2, chest_top), chest)

    # items being pulled down into the stash, with little purple trails
    items = [(SWORD, P, 5, -80, 222), (POTION, PB, 5, 15, 190), (RING, P, 6, 105, 226), (FISH, P, 4, -185, 196),
             (LANTERN, P, 5, 190, 186)]
    for spr, pal, sc, dx, y in items:
        s = sprite(spr, pal, sc)
        x = cx + dx - s.width // 2
        for t in range(1, 4):
            d.line([(x + s.width // 2, y - 8 - t * 12), (x + s.width // 2, y - 2 - t * 12 + 6)],
                   fill=(200, 150, 255), width=max(1, 6 - t * 2))
        img.paste(s, (x, y), s)
    sp = sprite(SPARK, PE, 4)
    rnd = random.Random(7)
    for _ in range(9):
        x = cx + rnd.randint(-190, 190)
        y = rnd.randint(180, mouth_y + 20)
        img.paste(sp, (x, y), sp)

    # the purple stash button under the chest
    bx0, by0, bx1, by1 = cx - 130, 590, cx + 130, 642
    d.rounded_rectangle([bx0 + 4, by0 + 5, bx1 + 4, by1 + 5], radius=12, fill=(10, 6, 20))
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=12, fill=(132, 66, 210), outline=(220, 180, 255), width=4)
    bc = sprite(BTN_CHEST, PE, 4)
    img.paste(bc, (bx0 + 18, by0 + (by1 - by0 - bc.height) // 2), bc)
    text_shadow(d, (bx0 + 158, by0 + 12), "STASH", font(30), (250, 240, 255), shadow=(60, 20, 100))

    # 40-slot stash grid on the right
    cols, rows, slot, gap = 8, 5, 52, 8
    gx0, gy0 = 735, 262
    gw, gh = cols * slot + (cols - 1) * gap, rows * slot + (rows - 1) * gap
    d.rounded_rectangle([gx0 - 18, gy0 - 62, gx0 + gw + 18, gy0 + gh + 18], radius=16,
                        fill=(30, 20, 52), outline=(170, 110, 240), width=4)
    text_shadow(d, (gx0 + gw // 2, gy0 - 48), "YOUR STASH  -  40 SLOTS", font(26), (225, 200, 255))
    filled = {0: (SWORD, P), 1: (POTION, PB), 2: (RING, P), 3: (FISH, P), 4: (LANTERN, P),
              9: (POTION, PB), 10: (RING, P), 17: (FISH, P)}
    for i in range(cols * rows):
        r, c = divmod(i, cols)
        x, y = gx0 + c * (slot + gap), gy0 + r * (slot + gap)
        d.rectangle([x, y, x + slot - 1, y + slot - 1], fill=(56, 40, 86), outline=(120, 86, 176), width=3)
        if i in filled:
            spr, pal = filled[i]
            s = sprite(spr, pal, 1)
            k = max(1, min((slot - 14) // s.width, (slot - 14) // s.height))
            s = sprite(spr, pal, k)
            img.paste(s, (x + (slot - s.width) // 2, y + (slot - s.height) // 2), s)

    # arrow from chest to grid
    d.polygon([(560, 380), (650, 380), (650, 360), (695, 400), (650, 440), (650, 420), (560, 420)], fill=(185, 120, 255))

    text_shadow(d, (W // 2, 22), "ENDER STASH", font(84), (245, 238, 255))
    text_shadow(d, (W // 2, 112), "A personal 40-slot stash per character, opened from any chest.", font(28), (210, 198, 238))
    mid = gx0 + gw // 2
    d.rounded_rectangle([gx0 - 18, 606, gx0 + gw + 18, 690], radius=16, fill=(14, 10, 30, 230),
                        outline=(160, 110, 230), width=3)
    text_shadow(d, (mid, 618), "Same stash in every chest and world.", font(23), (240, 225, 255))
    text_shadow(d, (mid, 652), "Never dropped on death.", font(23), (255, 225, 150))

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
