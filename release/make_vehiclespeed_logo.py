"""1280x720 mod.io listing image for Vehicle Speed: a boat and a go-kart racing right with speed lines,
fading after-images and the 1x/2x/3x/5x/10x multiplier choices. python make_vehiclespeed_logo.py"""
import os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "vehiclespeed_logo.png")

PV = dict(P, **{
    'f': (240, 200, 160),  # skin
    'h': (170, 120, 74),   # wood light
    'b': (120, 78, 46),    # wood / hair
    'B': (78, 50, 30),     # wood dark
    'c': (80, 120, 210),   # shirt
    'C': (50, 80, 160),
    'r': (214, 60, 70),    # kart paint
    'R': (140, 34, 46),
    'q': (60, 60, 70),     # tyre
})

BOAT = [
    ".......kkkkk..........",
    "......kbbbbbk.........",
    "......kfffkfk.........",
    "......kffffffk........",
    ".....kccccccCk........",
    "kk...kccccccCk......kk",
    "khkkkkkkkkkkkkkkkkkkhk",
    "khhhhhhhhhhhhhhhhhhhhk",
    ".kbbbbbbbbbbbbbbbbbbk.",
    "..kBBBBBBBBBBBBBBBBk..",
    "...kkkkkkkkkkkkkkkk...",
]

KART = [
    ".........kkkkk..........",
    "........kbbbbbk.........",
    "........kfffkfk.........",
    "........kffffffk........",
    ".......kccccccCk........",
    "....kkkkcccccCCkkkkk....",
    "...krrrrrrrrrrrrrrrrrk..",
    "..krrwwrrrrrrrrrrrrrrrk.",
    ".kkrrrrrrrrrrrrrrrrrrrkk",
    ".kRRRRRRRRRRRRRRRRRRRRRk",
    "..kkkkkkkkkkkkkkkkkkkkk.",
    "..kkkk...........kkkk...",
    ".kqqqqk.........kqqqqk..",
    ".kqSSqk.........kqSSqk..",
    ".kqqqqk.........kqqqqk..",
    "..kkkk...........kkkk...",
]


def speed_lines(d, rnd, x0, x1, y0, y1, col, n=9):
    for _ in range(n):
        y = rnd.randint(y0, y1)
        a = rnd.randint(x0, x1 - 160)
        b = min(x1, a + rnd.randint(110, 260))
        d.line([(a, y), (b, y)], fill=col, width=rnd.choice([4, 6, 8]))


def tag(d, x, y, txt, col):
    d.rounded_rectangle([x, y, x + 120, y + 56], radius=12, fill=(24, 18, 40), outline=col, width=4)
    d.text((x + 60, y + 29), txt, font=font(36), fill=col, anchor="mm")


def main():
    img = cave_bg(seed=51, top=(16, 18, 36), bottom=(30, 30, 62)).convert("RGBA")
    d = ImageDraw.Draw(img)
    rnd = random.Random(5)

    # water lane
    wy0, wy1 = 300, 400
    d.rectangle([0, wy0, W, wy1], fill=(36, 84, 140))
    d.rectangle([0, wy0, W, wy0 + 8], fill=(90, 160, 220))
    for _ in range(40):
        x, y = rnd.randrange(0, W, 8), rnd.randint(wy0 + 20, wy1 - 10)
        d.rectangle([x, y, x + rnd.choice([16, 24, 40]), y + 4], fill=(70, 130, 196))
    # road lane
    ry0, ry1 = 470, 590
    d.rectangle([0, ry0, W, ry1], fill=(58, 50, 60))
    d.rectangle([0, ry0, W, ry0 + 6], fill=(96, 86, 96))
    d.rectangle([0, ry1 - 6, W, ry1], fill=(40, 34, 42))
    for x in range(0, W, 120):
        d.rectangle([x, (ry0 + ry1) // 2 + 30, x + 60, (ry0 + ry1) // 2 + 36], fill=(220, 200, 110))

    # boat: speed lines, after-images, boat, foam
    speed_lines(d, rnd, 60, 640, 200, 330, (170, 220, 255), 8)
    boat_full = sprite(BOAT, PV, 9)
    bx, by = 700, wy0 + 8 - boat_full.height + 4 * 9
    for i, (off, a) in enumerate([(420, 50), (280, 90), (140, 140)]):
        g = sprite(BOAT, PV, 9, alpha=a)
        img.paste(g, (bx - off, by), g)
    d = ImageDraw.Draw(img)
    for k in range(6):
        fx = bx - 20 - k * 40
        d.rectangle([fx, wy0 + 2, fx + 26, wy0 + 12], fill=(230, 245, 255))
    img.paste(boat_full, (bx, by), boat_full)

    # kart
    speed_lines(d, rnd, 60, 660, 410, 520, (255, 210, 150), 8)
    kart = sprite(KART, PV, 8)
    kx, ky = 720, ry0 + 50 - kart.height + 4 * 8
    for off, a in [(420, 50), (280, 90), (140, 140)]:
        g = sprite(KART, PV, 8, alpha=a)
        img.paste(g, (kx - off, ky), g)
    d = ImageDraw.Draw(img)
    for k in range(5):
        px = kx - 30 - k * 34
        r = 10 + k * 4
        py = ky + kart.height - 22
        d.ellipse([px - r, py - r, px + r, py + r], fill=(150, 140, 150))
    img.paste(kart, (kx, ky), kart)

    tag(d, 1010, 250, "10x", (255, 220, 110))
    tag(d, 1030, 430, "5x", (255, 170, 110))
    text_shadow(d, (1070, 314), "BOAT", font(24), (200, 230, 255))
    text_shadow(d, (1090, 494), "GO-KART", font(24), (255, 215, 190))

    text_shadow(d, (W // 2, 22), "VEHICLE SPEED", font(84), (240, 244, 255))
    text_shadow(d, (W // 2, 112), "Boat and go-kart speed multipliers in the Mod Settings menu.", font(28), (200, 210, 238))

    # multiplier choices
    opts = ["1x", "2x", "3x", "5x", "10x"]
    pw, gap = 104, 18
    total = len(opts) * pw + (len(opts) - 1) * gap
    x0 = (W - total) // 2
    d.rounded_rectangle([x0 - 24, 618, x0 + total + 24, 702], radius=16, fill=(14, 12, 30, 230),
                        outline=(110, 140, 210), width=3)
    for i, o in enumerate(opts):
        x = x0 + i * (pw + gap)
        hot = o == "10x"
        col = (255, 220, 110) if hot else (170, 180, 220)
        d.rounded_rectangle([x, 634, x + pw, 686], radius=12, fill=(70, 52, 20) if hot else (36, 34, 60),
                            outline=col, width=4 if hot else 3)
        d.text((x + pw // 2, 661), o, font=font(32), fill=col, anchor="mm")

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
