"""1280x720 listing image for Merchant Restock: a merchant stall whose shelves refill, a clock with a
short countdown, and the restock-time choices. python make_merchantrestock_logo.py"""
import os, sys, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "merchantrestock_logo.png")

PM = dict(P, **{
    'f': (196, 168, 120),  # caveling skin
    'F': (150, 124, 86),
    'e': (250, 230, 120),  # eyes
    'h': (110, 70, 140),   # hood
    'H': (74, 44, 100),
    'g': (230, 190, 80),   # gold trim
})

MERCHANT = [
    "....kkkkkk....",
    "...khhhhhhk...",
    "..khhHHHHhhk..",
    ".khhkffffkhhk.",
    ".khkfeffefkhk.",
    ".khkffffffkhk.",
    ".khhkffffkhhk.",
    "kghhhhhhhhhhgk",
    "khhhhhgghhhhhk",
    "kHhhhhgghhhhHk",
    "kHHhhhhhhhhHHk",
    ".kkkkkkkkkkkk.",
]

POTION = [
    "..kkk..",
    "..kwk..",
    ".kRRRk.",
    "kRRwRRk",
    "kRRRRRk",
    "kRRRRRk",
    ".kkkkk.",
]

FISH = [
    "...kkkk..k",
    "..kBBBBkkB",
    ".kBwkBBBBk",
    "kBBBBBBBBk",
    ".kBBBBBkkB",
    "..kkkkk..k",
]

BOMB = [
    "....yk.",
    "...kk..",
    ".kkkkk.",
    "kqqqqqk",
    "kqwqqqk",
    "kqqqqqk",
    ".kkkkk.",
]


def item_sprite(kind, colour):
    pal = dict(PM, R=colour, B=colour, q=(70, 70, 84), y=(255, 210, 90))
    rows = {"potion": POTION, "fish": FISH, "bomb": BOMB}[kind]
    return sprite(rows, pal, 6)


def clock(d, cx, cy, r, frac):
    d.ellipse([cx - r - 8, cy - r - 8, cx + r + 8, cy + r + 8], fill=(20, 16, 36))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(48, 40, 78), outline=(200, 180, 255), width=6)
    # remaining time wedge
    d.pieslice([cx - r + 14, cy - r + 14, cx + r - 14, cy + r - 14], -90, -90 + 360 * frac, fill=(255, 205, 90))
    for i in range(12):
        a = math.radians(i * 30 - 90)
        x0, y0 = cx + math.cos(a) * (r - 12), cy + math.sin(a) * (r - 12)
        x1, y1 = cx + math.cos(a) * (r - 26), cy + math.sin(a) * (r - 26)
        d.line([(x0, y0), (x1, y1)], fill=(230, 220, 255), width=5)
    a = math.radians(-90 + 360 * frac)
    d.line([(cx, cy), (cx + math.cos(a) * (r - 30), cy + math.sin(a) * (r - 30))], fill=(30, 22, 40), width=8)
    d.ellipse([cx - 10, cy - 10, cx + 10, cy + 10], fill=(30, 22, 40))


def main():
    img = cave_bg(seed=77, top=(20, 16, 36), bottom=(40, 28, 58)).convert("RGBA")
    d = ImageDraw.Draw(img)
    rnd = random.Random(3)

    text_shadow(d, (W // 2, 22), "MERCHANT RESTOCK", font(84), (240, 236, 255))
    text_shadow(d, (W // 2, 112), "Choose how often every merchant refills their shelves.", font(28), (210, 200, 238))

    # stall
    sx0, sy0, sx1, sy1 = 120, 190, 780, 600
    d.rectangle([sx0, sy0, sx1, sy0 + 40], fill=(150, 60, 70))
    for x in range(sx0, sx1, 60):
        d.rectangle([x, sy0, x + 30, sy0 + 40], fill=(232, 220, 200))
    d.rectangle([sx0, sy0 + 40, sx1, sy0 + 52], fill=(90, 40, 50))
    d.rectangle([sx0 + 10, sy0 + 52, sx0 + 30, sy1], fill=(110, 74, 48))
    d.rectangle([sx1 - 30, sy0 + 52, sx1 - 10, sy1], fill=(110, 74, 48))

    shelves = [(sy0 + 150, "potion", [(214, 60, 90), (80, 140, 230), (90, 200, 120), (230, 170, 60), (180, 90, 220)]),
               (sy0 + 260, "fish", [(90, 170, 220), (230, 140, 80), (120, 210, 190), (240, 220, 120), (200, 110, 160)]),
               (sy0 + 370, "bomb", [(255, 90, 60)] * 5)]
    for sy, kind, cols in shelves:
        d.rectangle([sx0 + 30, sy, sx1 - 30, sy + 14], fill=(140, 96, 60))
        d.rectangle([sx0 + 30, sy + 14, sx1 - 30, sy + 20], fill=(90, 60, 38))
        for i, c in enumerate(cols):
            spr = item_sprite(kind, c)
            x = sx0 + 70 + i * 120
            img.paste(spr, (x, sy - spr.height), spr)
            # sparkle: freshly restocked
            if rnd.random() < 0.6:
                px, py = x + spr.width + 6, sy - spr.height - 6
                d.line([(px - 8, py), (px + 8, py)], fill=(255, 250, 200), width=3)
                d.line([(px, py - 8), (px, py + 8)], fill=(255, 250, 200), width=3)
    d = ImageDraw.Draw(img)

    # merchant behind the counter
    m = sprite(MERCHANT, PM, 10)
    img.paste(m, (sx1 - 200, sy1 - m.height - 4), m)
    d = ImageDraw.Draw(img)
    d.rectangle([sx0, sy1 - 20, sx1, sy1 + 20], fill=(120, 80, 52), outline=(70, 46, 30), width=4)

    # clock + arrow "restock"
    clock(d, 1010, 360, 150, 0.2)
    text_shadow(d, (1010, 530), "next restock: 5 min", font(30), (255, 220, 140))
    text_shadow(d, (1010, 570), "vanilla: 25-35 min", font(24), (190, 180, 220))

    # choices
    opts = ["Vanilla", "15", "10", "5", "2", "1 min"]
    widths = [150, 80, 80, 80, 80, 120]
    gap = 16
    total = sum(widths) + gap * (len(opts) - 1)
    x = (W - total) // 2
    d.rounded_rectangle([x - 24, 626, x + total + 24, 706], radius=16, fill=(14, 12, 30, 230),
                        outline=(150, 130, 210), width=3)
    for o, w in zip(opts, widths):
        hot = o == "5"
        col = (255, 220, 110) if hot else (180, 175, 220)
        d.rounded_rectangle([x, 640, x + w, 692], radius=12, fill=(70, 52, 20) if hot else (36, 32, 60),
                            outline=col, width=4 if hot else 3)
        d.text((x + w // 2, 666), o, font=font(30), fill=col, anchor="mm")
        x += w + gap

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
