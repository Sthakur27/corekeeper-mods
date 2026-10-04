"""1280x720 mod.io listing image for Golden Chance: a glowing golden crop sprouting from tilled soil, a
golden cooked dish with sparkles, a x2 multiplier tag and the 1x/1.5x/2x/3x choices.
python make_goldenchance_logo.py"""
import os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "goldenchance_logo.png")

PG = dict(P, **{
    'g': (240, 200, 80),   # gold
    'G': (176, 128, 40),   # gold shade
    'y': (255, 240, 150),  # gold highlight
    'n': (70, 150, 80),    # leaf
    'N': (40, 100, 56),    # leaf shade
    'd': (92, 60, 38),     # soil
    'D': (62, 40, 26),     # soil dark
    'h': (128, 88, 56),    # soil light
    'c': (196, 206, 220),  # bowl rim
    'C': (124, 132, 156),  # bowl shade
    'o': (232, 150, 60),   # food
    'O': (190, 100, 40),
    'w': (250, 250, 240),
})

# golden crop: leafy stem with a big golden bulb on top
PLANT = [
    "........kkkk........",
    "......kkyyggkk......",
    ".....kyyyggggGk.....",
    "....kyywggggggGk....",
    "....kygggggggggk....",
    "....kggggggggGGk....",
    "....kGgggggggGGk....",
    ".....kGGggggGGk.....",
    "......kkGGGGkk......",
    "........kGGk........",
    "..kkk...knNk...kkk..",
    ".knnnk..knNk..knnnk.",
    "knnnnnk.knNk.knnnnnk",
    "kNnnnnnkknNkknnnnnNk",
    ".kNNnnnnknNknnnnNNk.",
    "..kkNNnnnnNnnnNNkk..",
    "....kkNNnnNNNNkk....",
    "......kkknNkkk......",
    "........knNk........",
    "........knNk........",
]

# tilled soil mound
SOIL = [
    "......kkkkkkkkkkkkkk......",
    "...kkkhhhhdhhhhdhhhhkkk...",
    ".kkhhdddhhddddhhdddhhhdkk.",
    "khdddddddddddddddddddddddk",
    "kddDddddDddddddDdddddDdddk",
    "kDDDDdDDDDDdDDDDDDdDDDDDDk",
    "kkkkkkkkkkkkkkkkkkkkkkkkkk",
]

# golden cooked dish: steel bowl heaped with glowing golden food
DISH = [
    "........kkkkkkkk........",
    "......kkyyyggggGkk......",
    "....kkyywyggggggGGkk....",
    "...kyyyygggggggggGGGk...",
    "..kygggoggggOgggggGGGk..",
    ".kkkkkkkkkkkkkkkkkkkkkk.",
    "kccwwcccccccccccccccccCk",
    "kkkkkkkkkkkkkkkkkkkkkkkk",
    ".kcccccccccccccccccccCk.",
    "..kcccccccccccccccccCk..",
    "...kCccccccccccccccCk...",
    "....kkCCCCCCCCCCCCkk....",
    "......kkkkkkkkkkkk......",
    "........kCCCCCCk........",
    ".......kkkkkkkkkk.......",
]

SPARK = [
    "..y..",
    "..g..",
    "ygwgy",
    "..g..",
    "..y..",
]


def glow(img, cx, cy, rad, col, peak=120):
    g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(g)
    for r in range(rad, 0, -6):
        a = int(peak * (1 - r / rad) ** 1.4)
        gd.ellipse([cx - r, cy - r * 0.9, cx + r, cy + r * 0.9], fill=col + (a,))
    return Image.alpha_composite(img, g)


def main():
    img = cave_bg(seed=63, top=(20, 16, 30), bottom=(46, 34, 40)).convert("RGBA")

    # cave floor strip
    d = ImageDraw.Draw(img)
    d.rectangle([0, 520, W, 600], fill=(40, 30, 36))
    d.rectangle([0, 520, W, 526], fill=(64, 50, 56))

    px, dx = 300, 980          # scene centres
    img = glow(img, px, 330, 230, (255, 200, 80))
    img = glow(img, dx, 400, 220, (255, 190, 70))
    d = ImageDraw.Draw(img)

    # golden plant on soil
    soil = sprite(SOIL, PG, 12)
    plant = sprite(PLANT, PG, 13)
    sy = 520 - soil.height + 12
    img.paste(plant, (px - plant.width // 2, sy - plant.height + 30), plant)
    img.paste(soil, (px - soil.width // 2, sy), soil)

    # golden dish with steam
    dish = sprite(DISH, PG, 14)
    dy = 520 - dish.height + 14
    for i, ox in enumerate([-70, 0, 70]):
        for k in range(5):
            x = dx + ox + (8 if (k + i) % 2 else -8)
            y = dy - 30 - k * 26
            d.rectangle([x - 6, y - 10, x + 6, y + 10], fill=(220, 210, 200, 0))
            d.rectangle([x - 5, y - 9, x + 5, y + 9], fill=(150 + k * 8, 140 + k * 8, 140 + k * 6))
    img.paste(dish, (dx - dish.width // 2, dy), dish)

    # sparkles around both
    sp_big, sp_small = sprite(SPARK, PG, 6), sprite(SPARK, PG, 4)
    rnd = random.Random(11)
    for cx, cy in [(px, 300), (dx, 380)]:
        for _ in range(8):
            s = rnd.choice([sp_big, sp_small])
            x = cx + rnd.randint(-210, 190)
            y = cy + rnd.randint(-130, 110)
            if abs(x - cx) < 120 and abs(y - cy) < 90:
                x += 150 if x >= cx else -150
            if (cx == px and x > 470) or (cx == dx and x < 800):
                continue  # keep clear of the centre tag/chips
            img.paste(s, (x, y), s)
    d = ImageDraw.Draw(img)

    # labels
    text_shadow(d, (px, 540), "GARDENING", font(30), (200, 240, 190))
    text_shadow(d, (dx, 540), "COOKING", font(30), (255, 215, 180))

    # centre: x2 tag and setting choices
    tx, ty, tw, th = W // 2 - 95, 250, 190, 110
    d.rounded_rectangle([tx + 5, ty + 6, tx + tw + 5, ty + th + 6], radius=18, fill=(8, 6, 14))
    d.rounded_rectangle([tx, ty, tx + tw, ty + th], radius=18, fill=(70, 50, 16), outline=(255, 220, 110), width=5)
    text_shadow(d, (W // 2, ty + th // 2 + 2), "x2", font(76), (255, 228, 120), anchor="mm", shadow=(40, 24, 0))
    text_shadow(d, (W // 2, ty + th + 18), "GOLDEN ODDS", font(22), (240, 220, 160))
    opts = ["1x", "1.5x", "2x", "3x"]
    cw, gap = 62, 8
    total = len(opts) * cw + (len(opts) - 1) * gap
    x0 = W // 2 - total // 2
    for i, o in enumerate(opts):
        x = x0 + i * (cw + gap)
        hot = o == "2x"
        col = (255, 220, 110) if hot else (170, 170, 200)
        d.rounded_rectangle([x, 420, x + cw, 458], radius=9, fill=(70, 52, 20) if hot else (36, 32, 52),
                            outline=col, width=3)
        d.text((x + cw // 2, 439), o, font=font(20), fill=col, anchor="mm")

    text_shadow(d, (W // 2, 22), "GOLDEN CHANCE", font(84), (255, 236, 170))
    text_shadow(d, (W // 2, 112), "Multiplies your golden plant and golden cooking talents.", font(28), (226, 214, 190))

    # caption box
    bx0, bx1 = 250, W - 250
    d.rounded_rectangle([bx0, 614, bx1, 698], radius=16, fill=(16, 12, 20, 230), outline=(210, 170, 90), width=3)
    text_shadow(d, (W // 2, 626), "Scales your talent bonus only.", font(25), (255, 236, 190))
    text_shadow(d, (W // 2, 660), "No points = vanilla odds.", font(25), (200, 230, 200))

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
