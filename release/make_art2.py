"""Listing images for Hard Mode Tuning, Keep Minions On Teleport and Sid's Overhaul (1280x720).
python make_art2.py [hardmode|keepminions|overhaul|all]"""
import sys, math, random
from PIL import Image, ImageDraw
from make_art import (W, H, font, sprite, cave_bg, text_shadow, slot_panel, P,
                                        HELM, CHEST, RING, SWORD, BAG, LANTERN, FISH, POTION, MUSHROOM, PICKAXE, PB)

OUT = r"C:\Users\Sid\CoreKeeperMods\release"

SLIME = [
    "....kkkkkk....",
    "..kkggggggkk..",
    ".kggwwggggggk.",
    ".kgwwgggggggk.",
    "kgggggkggkgggk",
    "kgggggkggkgggk",
    "kggggggggggggk",
    "kGgggggggggggk",
    ".kGGGGGGGGGGk.",
    "..kkkkkkkkkk..",
]
SLIME_P = {'k': (20, 30, 24), 'g': (110, 200, 110), 'G': (60, 140, 70), 'w': (220, 255, 220)}

BOSS = [
    "..k...kk...k..",
    ".kyk.kyyk.kyk.",
    ".kyykyyyykyyk.",
    "kkyyyyyyyyyykk",
    "krrrrrrrrrrrrk",
    "krrwwrrrrwwrrk",
    "krrwkrrrrwkrrk",
    "krrrrrrrrrrrrk",
    "krRRrkkkkrRRrk",
    "krRRkwkwkkRRrk",
    ".krRRRRRRRRrk.",
    "..kkkkkkkkkk..",
]
BOSS_P = {'k': (30, 16, 20), 'r': (190, 60, 70), 'R': (130, 36, 46), 'w': (250, 245, 235), 'y': (255, 210, 80)}

PLAYER = [
    "...kkkkkk...",
    "..kbbbbbbk..",
    ".kbbbbbbbbk.",
    ".kffffffffk.",
    ".kfkffffkfk.",
    ".kffffffffk.",
    "..kffffffk..",
    ".kcccccccck.",
    "kccCccccCcck",
    "kfkccccccKfk",
    "..kccccccK..",
    "..kBBkkBBk..",
    "..kkk..kkk..",
]
PLAYER_P = {'k': (24, 18, 26), 'b': (120, 70, 40), 'f': (240, 200, 160), 'c': (80, 120, 210),
            'C': (50, 80, 160), 'K': (40, 60, 120), 'B': (60, 40, 30)}

MINION = [
    "...kkkk...",
    "..kppppk..",
    ".kpwwpppk.",
    "kppwkppwpk",
    "kpppppwkpk",
    "kppppppppk",
    "kpPppPppPk",
    ".kk.kk.kk.",
]
MINION_P = {'k': (30, 20, 50), 'p': (170, 140, 255), 'P': (110, 90, 200), 'w': (250, 250, 255)}


def bar(d, x, y, w, h, frac, colour, label, value, f, fv):
    d.rounded_rectangle([x, y, x + w, y + h], radius=8, fill=(30, 24, 44), outline=(90, 76, 130), width=3)
    d.rounded_rectangle([x + 4, y + 4, x + 4 + int((w - 8) * frac), y + h - 4], radius=6, fill=colour)
    d.text((x, y - 34), label, font=f, fill=(210, 200, 235))
    d.text((x + w + 16, y + h // 2), value, font=fv, fill=(255, 235, 160), anchor="lm")


def hardmode():
    img = cave_bg(seed=11, top=(30, 14, 22), bottom=(58, 24, 36)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "HARD MODE TUNING", font(80), (255, 240, 240))
    text_shadow(d, (W // 2, 134), "Regular enemies calmed down. Bosses stay brutal.", font(30), (235, 200, 205))

    # left: regular enemy panel
    d.rounded_rectangle([70, 200, 620, 610], radius=22, fill=(40, 26, 40), outline=(150, 110, 170), width=5)
    text_shadow(d, (345, 222), "REGULAR ENEMIES", font(36), (220, 240, 220))
    s = sprite(SLIME, SLIME_P, 11)
    img.paste(s, (345 - s.width // 2, 280), s)
    f, fv = font(24), font(30)
    bar(d, 110, 430, 330, 34, 1.0, (120, 60, 70), "vanilla hard: damage", "2x", f, fv)
    d.line([(110, 447), (440, 447)], fill=(250, 250, 250), width=4)
    bar(d, 110, 530, 330, 34, 0.75, (110, 200, 120), "this mod (default)", "1.5x", f, fv)

    # right: boss panel
    d.rounded_rectangle([660, 200, 1210, 610], radius=22, fill=(48, 20, 28), outline=(220, 90, 100), width=5)
    text_shadow(d, (935, 222), "BOSSES", font(36), (255, 220, 200))
    b = sprite(BOSS, BOSS_P, 11)
    img.paste(b, (935 - b.width // 2, 272), b)
    bar(d, 700, 530, 330, 34, 1.0, (210, 70, 80), "unchanged: full hard mode", "2x", f, fv)

    text_shadow(d, (W // 2, 650), "Damage and health for regular enemies set in Mod Settings.  Normal worlds untouched.", font(24), (215, 185, 195))
    img.convert("RGB").save(OUT + r"\hardmode_logo.png")
    print("hardmode_logo.png")


def portal(d, cx, cy, r, phase):
    for i in range(7, 0, -1):
        t = i / 7
        col = (int(70 + 120 * t), int(40 + 60 * t), int(150 + 100 * t))
        rr = r * t
        d.ellipse([cx - rr, cy - rr * 1.25, cx + rr, cy + rr * 1.25], outline=col, width=6)
    for k in range(10):
        a = phase + k * 0.63
        x, y = cx + math.cos(a) * r * 0.8, cy + math.sin(a) * r


def keepminions():
    img = cave_bg(seed=21, top=(16, 16, 36), bottom=(34, 26, 64)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "KEEP MINIONS ON TELEPORT", font(70), (240, 238, 255))
    text_shadow(d, (W // 2, 128), "Portals, waypoints and recall no longer dismiss your summons.", font(30), (200, 195, 235))

    # two portals
    for cx in (330, 950):
        d.ellipse([cx - 120, 250, cx + 120, 560], fill=(40, 28, 80))
        for i in range(6, 0, -1):
            t = i / 6
            col = (int(80 + 120 * t), int(50 + 80 * t), int(170 + 80 * t))
            rx, ry = 120 * t, 155 * t
            d.ellipse([cx - rx, 405 - ry, cx + rx, 405 + ry], outline=col, width=5)
    # arrow between
    d.polygon([(560, 385), (700, 385), (700, 360), (750, 405), (700, 450), (700, 425), (560, 425)], fill=(170, 140, 255))
    text_shadow(d, (655, 470), "TELEPORT", font(28), (220, 210, 255))

    # player + minions at the destination
    pl = sprite(PLAYER, PLAYER_P, 9)
    img.paste(pl, (950 - pl.width // 2, 340), pl)
    m = sprite(MINION, MINION_P, 7)
    for (dx, dy) in [(-150, 300), (100, 280), (-120, 470), (95, 480)]:
        img.paste(m, (950 + dx, dy), m)
    # faded minions at the source
    mg = sprite(MINION, MINION_P, 7, alpha=110)
    for (dx, dy) in [(-110, 300), (60, 290), (-90, 470), (55, 470)]:
        img.paste(mg, (330 + dx, dy), mg)

    d.rounded_rectangle([250, 600, 1030, 680], radius=16, fill=(24, 20, 44), outline=(120, 100, 190), width=3)
    text_shadow(d, (W // 2, 612), "Minions stay alive and arrive with you.", font(30), (255, 235, 160))
    text_shadow(d, (W // 2, 648), "Dying still dismisses them.  No settings.", font(22), (190, 185, 220))
    img.convert("RGB").save(OUT + r"\keepminions_logo.png")
    print("keepminions_logo.png")


def overhaul():
    img = cave_bg(seed=31).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 36), "SID'S OVERHAUL", font(92), (250, 244, 255))
    text_shadow(d, (W // 2, 142), "15 quality-of-life mods.  One install for your whole group.", font(30), (205, 195, 235))

    icons = [(HELM, P, 6), (CHEST, P, 6), (RING, P, 8), (SWORD, P, 7), (BAG, P, 8),
             (LANTERN, P, 9), (FISH, P, 7), (POTION, PB, 7), (MUSHROOM, PB, 8), (PICKAXE, P, 6),
             (SLIME, SLIME_P, 6), (BOSS, BOSS_P, 5), (MINION, MINION_P, 7), (PLAYER, PLAYER_P, 6), (None, None, 0)]
    slot, gap, cols = 118, 22, 5
    x0 = (W - (cols * slot + (cols - 1) * gap)) // 2
    y0 = 208
    for i, (spr, pal, sc) in enumerate(icons):
        r, c = divmod(i, cols)
        x, y = x0 + c * (slot + gap), y0 + r * (slot + gap)
        slot_panel(d, x, y, slot)
        if spr is None:
            text_shadow(d, (x + slot // 2, y + 26), "+", font(64), (160, 140, 210))
            continue
        try:
            s = sprite(spr, pal or P, sc)
        except KeyError:
            continue
        if s.width > slot - 16 or s.height > slot - 16:
            k = min((slot - 16) / s.width, (slot - 16) / s.height)
            s = s.resize((int(s.width * k), int(s.height * k)), Image.NEAREST)
        img.paste(s, (x + (slot - s.width) // 2, y + (slot - s.height) // 2), s)

    d.rounded_rectangle([140, 640, 1140, 700], radius=16, fill=(24, 20, 40), outline=(140, 120, 190), width=3)
    text_shadow(d, (W // 2, 652), "Loadouts, farming, fishing, buffs, pets, XP, hard mode, minions", font(28), (255, 225, 140))
    img.convert("RGB").save(OUT + r"\overhaul_logo.png")
    print("overhaul_logo.png")


if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "all"
    if which in ("hardmode", "all"): hardmode()
    if which in ("keepminions", "all"): keepminions()
    if which in ("overhaul", "all"): overhaul()
