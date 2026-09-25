"""Pixel-art style mod.io listing images (1280x720). python make_art.py [loadout|fishing|buffduration|all]"""
import sys, math, random
from PIL import Image, ImageDraw, ImageFont

W, H = 1280, 720

def font(sz):
    for f in ["C:/Windows/Fonts/consolab.ttf", "C:/Windows/Fonts/arialbd.ttf"]:
        try: return ImageFont.truetype(f, sz)
        except Exception: pass
    return ImageFont.load_default()

def sprite(rows, palette, scale, alpha=255):
    """rows: list of strings, each char a palette key ('.' = transparent)."""
    h, w = len(rows), max(len(r) for r in rows)
    img = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == '.' or ch == ' ': continue
            c = palette[ch]
            d.rectangle([x * scale, y * scale, (x + 1) * scale - 1, (y + 1) * scale - 1], fill=(c[0], c[1], c[2], alpha))
    return img

def cave_bg(seed=1, top=(22, 18, 34), bottom=(44, 30, 60)):
    img = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(img)
    for y in range(H):
        t = y / H
        d.line([(0, y), (W, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    rnd = random.Random(seed)
    # scattered dim "tiles"
    for _ in range(140):
        x, y = rnd.randrange(0, W, 16), rnd.randrange(0, H, 16)
        s = rnd.choice([16, 16, 32])
        v = rnd.randint(4, 12)
        d.rectangle([x, y, x + s - 1, y + s - 1], fill=(top[0] + v, top[1] + v, top[2] + v + 4))
    return img

def text_shadow(d, xy, txt, f, fill, anchor="mt", shadow=(0, 0, 0)):
    x, y = xy
    d.text((x + 4, y + 4), txt, font=f, fill=shadow, anchor=anchor)
    d.text((x, y), txt, font=f, fill=fill, anchor=anchor)

# ---------- pixel sprites ----------
P = {
    'k': (28, 22, 30),    # outline
    'g': (232, 196, 88),  # gold
    'G': (170, 132, 50),  # gold shade
    's': (196, 206, 220), # steel
    'S': (120, 132, 156), # steel shade
    'b': (96, 64, 40),    # leather
    'B': (60, 40, 26),
    'r': (214, 60, 70),   # red gem
    'c': (90, 200, 230),  # cyan
    'w': (245, 245, 250),
    'o': (255, 150, 60),  # orange
    'n': (60, 140, 90),   # green
    'y': (255, 230, 120), # highlight
}

HELM = [
    "...kkkkkkkk...",
    "..kssssssssk..",
    ".kssswwsssssk.",
    ".kssssssssssk.",
    "kSSSSSSSSSSSSk",
    "kSkkkkkkkkkkSk",
    "kSk.kkkkkk.kSk",
    ".kk........kk.",
]
CHEST = [
    "..kkk....kkk..",
    ".kSSSkkkkSSSk.",
    "kSSsssssssSSSk",
    "kSSssswwssSSSk",
    ".kksssssssskk.",
    "..ksssssssk...",
    "..kSSSSSSSk...",
    "..kSSkkSSSk...",
    "..kkk..kkk....",
]
PANTS = [
    "kkkkkkkkkkkk",
    "kbbbbbbbbbbk",
    "kbbbbkkbbbbk",
    "kbbbk..kbbbk",
    "kBBBk..kBBBk",
    "kBBBk..kBBBk",
    "kkkkk..kkkkk",
]
RING = [
    "....kkkk....",
    "...kggggk...",
    "..kgkrrkgk..",
    ".kgk.rr.kgk.",
    ".kg......gk.",
    ".kG......Gk.",
    "..kG....Gk..",
    "...kGGGGk...",
    "....kkkk....",
]
SWORD = [
    "..........kk",
    ".........kwk",
    "........kwsk",
    ".......kwsk.",
    "......kwsk..",
    "kk...kwsk...",
    "kgk.kwsk....",
    ".kgkwsk.....",
    "..kggk......",
    "..kBkGk.....",
    ".kBk.kk.....",
    "kBk.........",
    "kk..........",
]
BAG = [
    "...kkkkk....",
    "..kbBBBbk...",
    ".kbbbbbbbk..",
    "kbbbGGGbbbk.",
    "kbbbGgGbbbk.",
    "kbbbbbbbbbk.",
    "kBbbbbbbbBk.",
    ".kBBBBBBBk..",
    "..kkkkkkk...",
]
LANTERN = [
    "....kk....",
    "...kggk...",
    "..kkkkkk..",
    ".kyyyyyyk.",
    ".kyooooyk.",
    ".kyoooyyk.",
    ".kyyyyyyk.",
    "..kkkkkk..",
    "...kGGk...",
]
FISH = [
    "........kkk.....",
    ".......kcckk....",
    "kk...kkccccck...",
    "kck.kcccwkcccck.",
    "kcckcccccccccck.",
    "kccccccccccccck.",
    "kcckcccccccccck.",
    "kck.kcccccccck..",
    "kk...kkcccck....",
    ".......kkkk.....",
]
BOBBER = [
    "..kkkk..",
    ".krrrrk.",
    "krrrrrrk",
    "kkkkkkkk",
    "kwwwwwwk",
    ".kwwwwk.",
    "..kkkk..",
]

def slot_panel(d, x, y, size, fill=(58, 50, 78), outline=(140, 120, 190)):
    d.rounded_rectangle([x, y, x + size, y + size], radius=14, fill=fill, outline=outline, width=5)

def loadout():
    img = cave_bg(seed=3).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 44), "LOADOUT FALLBACK", font(78), (245, 240, 255))
    text_shadow(d, (W // 2, 138), "Set your armor once. Only what changes goes in loadouts 2 and 3.", font(28), (200, 190, 225))

    cols = [("1", 1.0), ("2", 0.33), ("3", 0.33)]
    slot, gap, colw = 92, 14, 3 * 92 + 2 * 14
    total = 3 * colw + 2 * 70
    x0 = (W - total) // 2
    y0 = 215
    items = [(HELM, 6), (CHEST, 6), (PANTS, 7), (RING, 8), (SWORD, 7), (BAG, 8), (LANTERN, 9)]
    # per loadout which slots hold their own item
    own = [set(range(7)), {4}, {3, 4}]
    for ci, (label, _) in enumerate(cols):
        cx = x0 + ci * (colw + 70)
        d.rounded_rectangle([cx - 18, y0 - 18, cx + colw + 18, y0 + 2 * (slot + gap) + slot + 18], radius=18, fill=(34, 28, 50), outline=(90, 76, 130), width=4)
        text_shadow(d, (cx + colw // 2, y0 + 3 * (slot + gap) + 4), f"LOADOUT {label}", font(30), (220, 210, 245))
        for si, (spr, sc) in enumerate(items):
            if si >= 9: break
            r, c = divmod(si, 3)
            sx, sy = cx + c * (slot + gap), y0 + r * (slot + gap)
            slot_panel(d, sx, sy, slot)
            a = 255 if si in own[ci] else 95
            s = sprite(spr, P, sc, alpha=a)
            img.paste(s, (sx + (slot - s.width) // 2, sy + (slot - s.height) // 2), s)
        # empty 8th/9th slots
        for si in (7, 8):
            r, c = divmod(si, 3)
            slot_panel(d, cx + c * (slot + gap), y0 + r * (slot + gap), slot)
        if ci > 0:
            # arrow from previous
            ax = cx - 52
            ay = y0 + (slot + gap) * 1 + slot // 2
            d.polygon([(ax - 8, ay - 14), (ax + 22, ay), (ax - 8, ay + 14)], fill=(140, 120, 190))
    text_shadow(d, (W // 2, 668), "Dimmed = inherited from loadout 1.   Bright = this loadout's own item.", font(26), (170, 160, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\loadout_logo.png")
    print("loadout_logo.png")

def fishing():
    img = cave_bg(seed=7, top=(14, 24, 40), bottom=(20, 60, 90)).convert("RGBA")
    d = ImageDraw.Draw(img)
    # water
    wy = 400
    for y in range(wy, H):
        t = (y - wy) / (H - wy)
        d.line([(0, y), (W, y)], fill=(int(18 + 10 * t), int(80 - 30 * t), int(140 - 50 * t)))
    rnd = random.Random(5)
    for i in range(60):
        x = rnd.randrange(0, W, 8); y = rnd.randrange(wy + 8, H - 8, 8)
        d.rectangle([x, y, x + rnd.choice([16, 24, 40]), y + 3], fill=(60, 150, 200))
    # rod
    d.line([(160, 380), (520, 130)], fill=(40, 28, 26), width=14)
    d.line([(160, 380), (520, 130)], fill=(120, 80, 60), width=6)
    d.line([(520, 130), (700, 330)], fill=(230, 230, 240), width=3)
    bob = sprite(BOBBER, P, 8); img.paste(bob, (700 - bob.width // 2, 330), bob)
    # splash rings
    for r in (30, 60, 95):
        d.ellipse([700 - r, 372 - r // 4, 700 + r, 372 + r // 4], outline=(200, 230, 255), width=3)
    # fish leaping, with speed lines
    for i, (fx, fy, sc, a) in enumerate([(860, 250, 9, 255), (1000, 300, 7, 170), (1110, 340, 5, 110)]):
        f = sprite(FISH, P, sc, alpha=a)
        img.paste(f, (fx, fy), f)
        for k in range(3):
            d.line([(fx - 30 - k * 14, fy + 20 + k * 18), (fx - 4 - k * 14, fy + 20 + k * 18)], fill=(200, 240, 255, a), width=4)
    # counter
    d.rounded_rectangle([880, 96, 1190, 190], radius=16, fill=(28, 24, 44), outline=(140, 120, 190), width=4)
    text_shadow(d, (1035, 112), "CAUGHT  x999", font(40), (255, 230, 120))
    text_shadow(d, (W // 2, 40), "FAST AUTO FISHING", font(80), (245, 245, 255))
    d.rounded_rectangle([W // 2 - 470, 618, W // 2 + 470, 700], radius=16, fill=(14, 20, 34, 230), outline=(90, 140, 190), width=3)
    text_shadow(d, (W // 2, 632), "Auto-answers every bite.  Removes the waiting.", font(30), (220, 240, 255))
    text_shadow(d, (W // 2, 668), "Same odds, loot, bait and XP as vanilla.  Just faster.", font(24), (160, 200, 230))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\fishing_logo.png")
    print("fishing_logo.png")

PICKAXE = [
    ".......kkkkkk.....",
    ".....kksssssskk...",
    "....kssswwsssssk..",
    "...kssSk...kSsssk.",
    "..kssSk.....kSSsk.",
    ".kssSk.......kSSk.",
    ".ksSk...kk....kSk.",
    "kssk...kbbk...kkk.",
    "kSk...kbBbk.......",
    "kk...kbBbk........",
    "....kbBbk.........",
    "...kbBbk..........",
    "..kbBbk...........",
    ".kbBbk............",
    "kbBbk.............",
    "kBBk..............",
    "kkk...............",
]
SPARK = [
    "..y..",
    ".yyy.",
    "yywyy",
    ".yyy.",
    "..y..",
]

def durability_bar(d, img, x, y, w, h, fill_frac, color, dim=False):
    """Pixel-segmented durability bar in a dark frame."""
    frame = (70, 62, 90) if dim else (140, 120, 190)
    d.rounded_rectangle([x, y, x + w, y + h], radius=8, fill=(24, 20, 36), outline=frame, width=4)
    seg, gap = 22, 6
    inner_w = w - 16
    n = inner_w // (seg + gap)
    lit = int(round(n * fill_frac))
    hi = tuple(min(255, c + 60) for c in color)
    lo = tuple(int(c * 0.55) for c in color)
    for i in range(n):
        sx = x + 8 + i * (seg + gap)
        if i < lit:
            d.rectangle([sx, y + 8, sx + seg - 1, y + h - 8], fill=color)
            d.rectangle([sx, y + 8, sx + seg - 1, y + 13], fill=hi)
            d.rectangle([sx, y + h - 13, sx + seg - 1, y + h - 8], fill=lo)
        else:
            d.rectangle([sx, y + 8, sx + seg - 1, y + h - 8], fill=(40, 34, 56))

def durability():
    img = cave_bg(seed=11, top=(20, 24, 30), bottom=(34, 48, 52)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "DURABILITY MULTIPLIER", font(74), (245, 245, 250))
    text_shadow(d, (W // 2, 130), "Tools, weapons and armor wear out slower.  Or never.", font(28), (200, 220, 215))

    # big pickaxe on the left with a few sparks off the head
    pick = sprite(PICKAXE, P, 18)
    px, py = 90, 215
    img.paste(pick, (px, py), pick)
    for (sx, sy, sc) in [(px + 300, py - 30, 6), (px + 350, py + 40, 4), (px + 260, py - 55, 3)]:
        s = sprite(SPARK, P, sc); img.paste(s, (sx, sy), s)

    # right side: vanilla bar (short, dim) vs the long bar from this mod
    bx, bw = 520, 660
    d.rounded_rectangle([bx - 30, 205, bx + bw + 30, 600], radius=20, fill=(26, 24, 40, 235), outline=(90, 100, 130), width=4)

    text_shadow(d, (bx, 232), "1x  vanilla", font(30), (170, 170, 190), anchor="lt")
    durability_bar(d, img, bx, 272, 300, 54, 0.30, (170, 90, 80), dim=True)
    text_shadow(d, (bx + 320, 282), "worn out", font(26), (150, 130, 140), anchor="lt")

    text_shadow(d, (bx, 372), "0.5x  this mod", font(30), (220, 240, 225), anchor="lt")
    durability_bar(d, img, bx, 412, bw, 54, 0.65, (96, 200, 120))

    text_shadow(d, (bx, 500), "0x  never breaks", font(30), (220, 240, 225), anchor="lt")
    durability_bar(d, img, bx, 540, bw, 40, 1.0, (90, 190, 230))

    text_shadow(d, (W // 2, 655), "Pick 0x, 0.1x, 0.25x, 0.5x, 0.75x or 1x in Mod Settings.  Repairs untouched.", font(26), (170, 190, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\durability_logo.png")
    print("durability_logo.png")


# ---------- Buff Duration Floor ----------
POTION = [
    "....kkkk....",
    "....kbbk....",
    "....kbbk....",
    "...kkkkkk...",
    "..kwwwwwwk..",
    ".kwwmmmmwwk.",
    ".kwmmMMmmwk.",
    "kwmmMMMMmmwk",
    "kwmMMMMMMmwk",
    "kwmMMyyMMmwk",
    "kwmmMMMMmmwk",
    ".kwmmMMmmwk.",
    "..kkkkkkkk..",
]
BOWL = [
    "......kkkk......",
    ".....kppppk.....",
    "....kpPPPPpk....",
    "...kppPPPPppk...",
    "..kkkkppppkkkk..",
    ".ktttkkkkkkttk..",
    ".kttttkkkkttttk.",
    "kttttttttttttttk",
    "kTtttttttttttTk.",
    ".kTTttttttttTk..",
    "..kTTTTTTTTTk...",
    "...kkkkkkkkk....",
]
MUSHROOM = [
    "...kkkkkk...",
    "..kppPPppk..",
    ".kpPPppPPpk.",
    "kpppPPPPpppk",
    "kkkkkkkkkkkk",
    "...kwwwwk...",
    "...kwwwwk...",
    "...kwWWwk...",
    "...kkkkkk...",
]
CLOCK = [
    "....kkkk....",
    "..kkwwwwkk..",
    ".kwwwwwwwwk.",
    ".kwwwwkwwwk.",
    "kwwwwwkwwwwk",
    "kwwwwwkkkwwk",
    "kwwwwwwwwwwk",
    ".kwwwwwwwwk.",
    ".kwwwwwwwwk.",
    "..kkwwwwkk..",
    "....kkkk....",
]
PB = dict(P)
PB.update({
    'm': (120, 90, 220),   # potion liquid
    'M': (170, 130, 255),  # potion liquid bright
    'p': (232, 110, 90),   # mushroom cap
    'P': (250, 160, 130),  # cap highlight
    't': (110, 74, 44),    # bowl wood
    'T': (70, 46, 28),
    'W': (210, 210, 220),
})

def timer_bar(d, x, y, w, h, frac, colour, label, f, ghost_frac=None):
    d.rounded_rectangle([x, y, x + w, y + h], radius=h // 2, fill=(30, 26, 44), outline=(120, 104, 168), width=4)
    if ghost_frac is not None:
        gw = int((w - 8) * ghost_frac)
        d.rounded_rectangle([x + 4, y + 4, x + 4 + gw, y + h - 4], radius=(h - 8) // 2, fill=(70, 60, 96))
    fw = int((w - 8) * frac)
    d.rounded_rectangle([x + 4, y + 4, x + 4 + fw, y + h - 4], radius=(h - 8) // 2, fill=colour)
    d.text((x + w + 26, y + h // 2), label, font=f, fill=(245, 240, 255), anchor="lm")

def buffduration():
    img = cave_bg(seed=11, top=(18, 16, 36), bottom=(40, 28, 66)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "BUFF DURATION FLOOR", font(78), (245, 240, 255))
    text_shadow(d, (W // 2, 134), "Every food and potion buff lasts at least as long as you choose.", font(28), (200, 190, 225))

    # left: the consumables
    panel_x, panel_y = 90, 210
    d.rounded_rectangle([panel_x, panel_y, panel_x + 330, panel_y + 400], radius=18, fill=(34, 28, 50), outline=(90, 76, 130), width=4)
    pot = sprite(POTION, PB, 9); img.paste(pot, (panel_x + 40, panel_y + 40), pot)
    bowl = sprite(BOWL, PB, 9); img.paste(bowl, (panel_x + 170, panel_y + 60), bowl)
    mush = sprite(MUSHROOM, PB, 8); img.paste(mush, (panel_x + 190, panel_y + 235), mush)
    small_pot = sprite(POTION, PB, 6, alpha=230); img.paste(small_pot, (panel_x + 60, panel_y + 230), small_pot)
    text_shadow(d, (panel_x + 165, panel_y + 355), "POTIONS + FOOD", font(28), (220, 210, 245))

    # right: timer bars, vanilla ghost vs floored
    bx, by, bw, bh = 500, 230, 520, 54
    f = font(34)
    clock = sprite(CLOCK, PB, 5); img.paste(clock, (bx - 70, by - 4), clock)
    d.text((bx, by - 30), "before", font=font(22), fill=(150, 140, 180), anchor="lb")
    timer_bar(d, bx, by, bw, bh, 0.12, (150, 90, 90), "0:20", f)
    d.text((bx, by + 95), "after (floor 3:00)", font=font(22), fill=(150, 140, 180), anchor="lb")
    timer_bar(d, bx, by + 105, bw, bh, 1.0, (110, 220, 130), "3:00", f, ghost_frac=0.12)
    d.text((bx, by + 215), "already longer stays as is", font=font(22), fill=(150, 140, 180), anchor="lb")
    timer_bar(d, bx, by + 225, bw, bh, 1.0, (90, 190, 240), "5:00", f)

    # floor slider illustration
    sx, sy, sw = 500, 560, 520
    d.rounded_rectangle([sx, sy, sx + sw, sy + 14], radius=7, fill=(70, 60, 96))
    for i, t in enumerate(["0:30", "3:00", "10:00"]):
        px = sx + int(sw * (0, 0.263, 1)[i])
        d.rectangle([px - 3, sy - 8, px + 3, sy + 22], fill=(160, 140, 210))
        d.text((px, sy + 40), t, font=font(22), fill=(200, 190, 225), anchor="mm")
    kx = sx + int(sw * 0.263)
    d.ellipse([kx - 16, sy - 9, kx + 16, sy + 23], fill=(232, 196, 88), outline=(28, 22, 30), width=3)
    text_shadow(d, (W // 2, 665), "Set the minimum in Mod Settings, 30 s steps up to 10 min.  Debuffs untouched.", font(26), (170, 160, 200))

    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\buffduration_logo.png")
    print("buffduration_logo.png")

# ---------- Bigger Watering Cans ----------
PW = {
    'k': (28, 22, 30),    # outline
    's': (196, 206, 220), # steel
    'S': (120, 132, 156), # steel shade
    'w': (245, 245, 250), # highlight
    'i': (150, 160, 178), # iron
    'I': (84, 92, 112),   # iron shade
    'r': (214, 60, 70),   # red trim
    'c': (110, 200, 240), # water
    'C': (60, 140, 210),  # water shade
    'd': (110, 74, 44),   # tilled soil
    'D': (74, 48, 30),    # soil furrow
    'm': (70, 52, 44),    # wet soil
    'M': (46, 34, 30),    # wet furrow
    'n': (70, 170, 80),   # sprout
    'N': (40, 110, 55),   # sprout shade
    'y': (255, 230, 120),
}

def can_sprite(body, shade, hi):
    """Watering can pointing down-left, spout at top-left, handle on top. body/shade/hi are palette keys."""
    b, S, w = body, shade, hi
    rows = [
        "..........kkkkkk......",
        ".........k" + b*6 + "k.....",
        "........k" + b + "kkkk" + b + "k....",
        ".kk.....k" + b + "k..k" + b + "k....",
        "k" + b + b + "k...k" + b + "k..k" + b + "k....",
        "k" + b + b + b + "kkk" + b + "kkkk" + b + "kkkk.",
        "k" + b + w + b + b + b + b + b + b + b + b + b + b + b + b + b + b + "k",
        ".k" + b + b + b + b + "k" + b + b + w + w + b + b + b + b + S + S + b + "k",
        "..k" + b + b + "k.k" + b + b + b + b + b + b + b + S + S + S + b + "k",
        "...kkk..k" + b + b + b + b + b + b + S + S + S + S + b + "k",
        "........k" + S + b + b + b + b + S + S + S + S + S + b + "k",
        "........k" + S + S + S + S + S + S + S + S + S + S + S + "k",
        ".........kkkkkkkkkkkkk.",
    ]
    return rows

DROPS = [
    "c.c.c",
    ".c.c.",
    "C...C",
    ".c.c.",
    "c.C.c",
]

def soil_tile(wet, sprout, rnd):
    """8x8 pixel tile of tilled ground, optionally watered and with a sprout."""
    g, f = ('m', 'M') if wet else ('d', 'D')
    rows = []
    for y in range(8):
        row = ''.join(f if (y % 3 == 1) else g for _ in range(8))
        rows.append(row)
    if sprout:
        sx = rnd.choice([2, 3])
        rows[2] = rows[2][:sx+1] + 'n' + rows[2][sx+2:]
        rows[3] = rows[3][:sx] + 'nNn' + rows[3][sx+3:]
        rows[4] = rows[4][:sx+1] + 'N' + rows[4][sx+2:]
    return rows

def crop_grid(img, x, y, n, scale, seed, wet_all=True):
    """n x n grid of soil tiles, each 8px * scale, with a 2px gap, watered ones marked with a bright frame."""
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    tile = 8 * scale
    for r in range(n):
        for c in range(n):
            tx, ty = x + c * (tile + 4), y + r * (tile + 4)
            t = sprite(soil_tile(wet_all, rnd.random() < 0.7, rnd), PW, scale)
            img.paste(t, (tx, ty), t)
    # frame around the whole watered area
    d.rectangle([x - 6, y - 6, x + n * (tile + 4) - 4 + 5, y + n * (tile + 4) - 4 + 5], outline=(110, 200, 240), width=4)
    return n * (tile + 4) - 4

def wateringcans():
    img = cave_bg(seed=11, top=(16, 30, 26), bottom=(30, 60, 44)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 36), "BIGGER WATERING CANS", font(76), (235, 250, 245))
    text_shadow(d, (W // 2, 126), "Water more crops per pour.", font(30), (180, 225, 215))

    # left: basic watering can over a 2x2 patch
    lx = 150
    gsize = crop_grid(img, lx + 40, 330, 2, 9, seed=2)
    can = sprite(can_sprite('s', 'S', 'w'), PW, 7)
    img.paste(can, (lx + 40 + gsize - 60, 170), can)
    drops = sprite(DROPS, PW, 7)
    img.paste(drops, (lx + 40 + gsize - 40, 262), drops)
    d.rounded_rectangle([lx - 20, 520, lx + 360, 590], radius=14, fill=(24, 40, 36), outline=(110, 200, 240), width=3)
    text_shadow(d, (lx + 170, 534), "WATERING CAN   2 x 2", font(30), (220, 245, 240))

    # right: iron watering can over a 4x4 patch
    rx = 680
    gsize = crop_grid(img, rx + 20, 205, 4, 9, seed=5)
    can = sprite(can_sprite('i', 'I', 'w'), PW, 8)
    img.paste(can, (rx + 20 + gsize - 110, 60), can)
    drops = sprite(DROPS, PW, 8)
    img.paste(drops, (rx + 20 + gsize - 80, 165), drops)
    d.rounded_rectangle([rx - 30, 520, rx + 430, 590], radius=14, fill=(24, 40, 36), outline=(110, 200, 240), width=3)
    text_shadow(d, (rx + 200, 534), "IRON WATERING CAN   4 x 4", font(30), (220, 245, 240))

    # divider arrow
    d.polygon([(560, 380), (610, 410), (560, 440)], fill=(110, 200, 240))
    text_shadow(d, (W // 2, 640), "Same water per pour. Same preview. Just a bigger patch.", font(26), (170, 210, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\wateringcans_logo.png")
    print("wateringcans_logo.png")

which = sys.argv[1] if len(sys.argv) > 1 else "all"
if which in ("loadout", "all"): loadout()
if which in ("fishing", "all"): fishing()
if which in ("wateringcans", "all"): wateringcans()
if which in ("buffduration", "all"): buffduration()
if which in ("durability", "all"): durability()
