"""Listing images for Pet Editor and Boss Bonus Loot (1280x720). python make_art3.py [peteditor|bossloot|all]"""
import sys, random
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, slot_panel, P
from make_art2 import BOSS, BOSS_P

OUT = r"C:\Users\Sid\CoreKeeperMods\release"

PET = [
    "..kk......kk..",
    ".kbbk....kbbk.",
    ".kbbbkkkkbbbk.",
    "kbbbbbbbbbbbbk",
    "kbbwkbbbbwkbbk",
    "kbbkkbbbbkkbbk",
    "kbbbbbppbbbbbk",
    ".kbbbbbbbbbbk.",
    "..kbbbbbbbbk..",
    "..kbk.kk.kbk..",
    "..kk......kk..",
]


def pet_palette(body, shade):
    return {'k': (30, 22, 34), 'b': body, 'B': shade, 'w': (250, 250, 255), 'p': (240, 120, 150)}


STAR = [
    "...k...",
    "..kyk..",
    "kkyyykk",
    ".kyyyk.",
    "..kyk..",
    ".kk.kk.",
]
HEART = [
    ".kk.kk.",
    "krrkrrk",
    "krrrrrk",
    ".krrrk.",
    "..krk..",
    "...k...",
]
SWORDI = [
    ".....kk",
    "....kwk",
    "...kwk.",
    "k.kwk..",
    ".kgk...",
    "kgk....",
    "kk.....",
]
SHIELD = [
    "kkkkkkk",
    "kcccwck",
    "kccccck",
    ".kcccck",
    ".kccck.",
    "..kck..",
    "...k...",
]
ICON_P = {'k': (28, 22, 30), 'y': (255, 220, 90), 'r': (230, 70, 90), 'w': (240, 240, 250), 'g': (232, 196, 88), 'c': (90, 200, 230)}


def peteditor():
    img = cave_bg(seed=41, top=(24, 18, 40), bottom=(46, 30, 66)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 36), "PET EDITOR", font(92), (250, 244, 255))
    text_shadow(d, (W // 2, 142), "Level, any talent in any slot, and color. Right in the pet window.", font(28), (205, 195, 235))

    # left: pet in three colors
    colors = [((240, 200, 120), (190, 150, 80)), ((150, 200, 255), (100, 150, 210)), ((200, 140, 240), (150, 90, 200))]
    for i, (body, shade) in enumerate(colors):
        s = sprite(PET, pet_palette(body, shade), 9 if i == 1 else 7, alpha=255 if i == 1 else 150)
        x = 110 + i * 150 + (0 if i != 1 else -10)
        y = 250 if i == 1 else 275
        img.paste(s, (x, y), s)
    text_shadow(d, (330, 400), "COLOR  < 2/3 >", font(30), (255, 225, 140))
    # level row
    d.rounded_rectangle([110, 460, 560, 520], radius=12, fill=(34, 28, 52), outline=(140, 120, 190), width=4)
    for k in range(10):
        d.rectangle([130 + k * 32, 476, 152 + k * 32, 504], fill=(120, 230, 140) if k < 10 else (60, 60, 80))
    text_shadow(d, (500, 470), "LV 10", font(32), (255, 235, 160))

    # right: 3x3 talent grid with a picker arrow
    icons = [STAR, HEART, SWORDI, SHIELD, STAR, HEART, SWORDI, SHIELD, STAR]
    gx, gy, slot, gap = 690, 220, 112, 18
    for i, ic in enumerate(icons):
        r, c = divmod(i, 3)
        x, y = gx + c * (slot + gap), gy + r * (slot + gap)
        slot_panel(d, x, y, slot, fill=(64, 52, 88), outline=(255, 220, 110) if i == 4 else (140, 120, 190))
        s = sprite(ic, ICON_P, 9)
        img.paste(s, (x + (slot - s.width) // 2, y + (slot - s.height) // 2), s)
    text_shadow(d, (gx + (3 * slot + 2 * gap) // 2, 620), "Right-click: pick any talent", font(26), (220, 210, 245))
    text_shadow(d, (W // 2, 672), "Written from scratch. Works for every player in multiplayer.", font(22), (175, 165, 205))
    img.convert("RGB").save(OUT + r"\peteditor_logo.png")
    print("peteditor_logo.png")


MEAT = [
    "..kkkk..",
    ".kmmmmk.",
    "kmmMmmmk",
    "kmmmmMmk",
    ".kmmmmk.",
    "..kwwk..",
    "...kk...",
]
GEM = [
    "..kkk..",
    ".kwcck.",
    "kwcccck",
    "kcccCck",
    ".kcCck.",
    "..kck..",
    "...k...",
]
BLADE = [
    "......kk",
    ".....kwk",
    "....kwSk",
    "...kwSk.",
    "k.kwSk..",
    ".kgSk...",
    "kgk.....",
    "kk......",
]
LOOT_P = {'k': (28, 22, 30), 'm': (230, 120, 90), 'M': (180, 80, 60), 'w': (245, 245, 250), 'c': (150, 230, 255),
          'C': (90, 170, 220), 'S': (150, 160, 180), 'g': (232, 196, 88)}
GOLD_P = dict(LOOT_P, m=(255, 215, 90), M=(210, 160, 50))


def bossloot():
    img = cave_bg(seed=51, top=(34, 16, 22), bottom=(60, 28, 34)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 36), "BOSS BONUS LOOT", font(84), (255, 240, 230))
    text_shadow(d, (W // 2, 134), "Bosses drop guaranteed extras on top of their loot chest.", font(28), (235, 205, 200))

    b = sprite(BOSS, BOSS_P, 18)
    img.paste(b, (W // 2 - b.width // 2, 220), b)
    rnd = random.Random(3)
    drops = [(MEAT, LOOT_P), (MEAT, GOLD_P), (GEM, LOOT_P), (BLADE, LOOT_P), (MEAT, LOOT_P), (MEAT, GOLD_P)]
    spots = [(250, 300), (330, 470), (880, 300), (960, 460), (200, 520), (1040, 540)]
    for (spr, pal), (x, y) in zip(drops, spots):
        s = sprite(spr, pal, 9)
        img.paste(s, (x, y), s)
        for _ in range(4):
            sx, sy = x + rnd.randint(-30, 90), y + rnd.randint(-30, 80)
            d.rectangle([sx, sy, sx + 5, sy + 5], fill=(255, 230, 140))

    d.rounded_rectangle([190, 610, 1090, 690], radius=16, fill=(30, 18, 26), outline=(220, 120, 110), width=3)
    text_shadow(d, (W // 2, 622), "Ghorm: 40 Larva Meat + 40 Golden Larva Meat", font(26), (255, 225, 150))
    text_shadow(d, (W // 2, 656), "Azeos: 25% Chipped Blade, 25% Clear Gemstone", font(22), (230, 200, 200))
    img.convert("RGB").save(OUT + r"\bossloot_logo.png")
    print("bossloot_logo.png")


if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "all"
    if which in ("peteditor", "all"): peteditor()
    if which in ("bossloot", "all"): bossloot()
