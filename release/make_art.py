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
    """Side view watering can: handle arc on top, body on the right, spout rising to a rose at top-left.
    body/shade/hi are palette keys substituted for b/S/w."""
    rows = [
        "...........kkkkkk.......",
        "..........kbbbbbbk......",
        ".........kbk....kbk.....",
        ".........kbk....kbk.....",
        "kkk.....kkkkkkkkkkkkkk..",
        "kwwk...kbbbbbbbbbbbbbbk.",
        "kwwwk.kbbwwbbbbbbbbbSSk.",
        ".kwwwkbbbbbbbbbbbbbbSSk.",
        "..kwwbbbbbbbbbbbbbbbSSk.",
        "...kbbbbbbbbbbbbbbbbSSk.",
        "....kbbbbbbbbbbbbbbbSSk.",
        ".....kbSSSSSSSSSSSSSSSk.",
        ".....kSSSSSSSSSSSSSSSSk.",
        ".....kkkkkkkkkkkkkkkkkk.",
    ]
    tr = str.maketrans({'b': body, 'S': shade, 'w': hi})
    return [r.translate(tr) for r in rows]

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

    # left: basic watering can over a 2x2 patch (rose of the can sits above the patch, drops fall onto it)
    gx, gy = 200, 330
    gsize = crop_grid(img, gx, gy, 2, 9, seed=2)          # 148 px
    can = sprite(can_sprite('s', 'S', 'w'), PW, 7)          # 168 x 98
    img.paste(can, (gx + gsize // 2 - 14, gy - 130), can)
    drops = sprite(DROPS, PW, 6)
    img.paste(drops, (gx + gsize // 2 - 12, gy - 40), drops)
    d.rounded_rectangle([130, 520, 510, 590], radius=14, fill=(24, 40, 36), outline=(110, 200, 240), width=3)
    text_shadow(d, (320, 534), "WATERING CAN   2 x 2", font(30), (220, 245, 240))

    # right: iron watering can over a 4x4 patch
    gx, gy = 720, 240
    gsize = crop_grid(img, gx, gy, 4, 8, seed=5)          # 268 px
    can = sprite(can_sprite('i', 'I', 'w'), PW, 8)          # 192 x 112
    img.paste(can, (gx + gsize // 2 + 30, gy - 130), can)
    drops = sprite(DROPS, PW, 7)
    img.paste(drops, (gx + gsize // 2 + 34, gy - 36), drops)
    d.rounded_rectangle([650, 520, 1110, 590], radius=14, fill=(24, 40, 36), outline=(110, 200, 240), width=3)
    text_shadow(d, (880, 534), "IRON WATERING CAN   4 x 4", font(30), (220, 245, 240))

    # divider arrow
    d.polygon([(560, 380), (610, 410), (560, 440)], fill=(110, 200, 240))
    text_shadow(d, (W // 2, 640), "Same water per pour. Same preview. Just a bigger patch.", font(26), (170, 210, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\wateringcans_logo.png")
    print("wateringcans_logo.png")

# ---------- Faster Mushrooms ----------
PM = {
    'k': (30, 20, 26),     # outline
    'r': (206, 62, 58),    # cap red
    'R': (240, 120, 100),  # cap highlight
    'd': (140, 36, 44),    # cap shade
    'w': (240, 232, 214),  # cap spots / stem
    'W': (255, 250, 240),
    's': (214, 196, 160),  # stem
    'S': (150, 130, 100),  # stem shade
    'g': (120, 200, 110),  # gills glow
    'y': (255, 230, 120),  # speed lines
}
FM_BIG = [
    "......kkkkkkkk......",
    "....kkrrRRRRrrkk....",
    "...krrRRwwRRRrrrk...",
    "..krrRRRwwRRrrrwwk..",
    ".krrwwRRRRRRrrrwwrk.",
    ".krrwwrrrRRrrrrrrrk.",
    "kdrrrrrrrrrrrwwrrrdk",
    "kddrrwwrrrrrrwwrrddk",
    "kdddrrrrrrrrrrrrdddk",
    ".kkkkkkkggggkkkkkkk.",
    "......kssWWssk......",
    "......kssWWssk......",
    "......ksssWssSk.....",
    "......kssssssSk.....",
    "......kSssssSSk.....",
    ".....kkSSSSSSSkk....",
    ".....kkkkkkkkkkk....",
]
FM_MID = [
    "....kkkkkk....",
    "..kkrrRRrrkk..",
    ".krrRwwRRrrrk.",
    "krrwwrrrrrwwrk",
    "kdrrrrrwwrrrdk",
    "kddrrrrrrrrddk",
    ".kkkkkggggkkk.",
    "....kssWssk...",
    "....kssWssk...",
    "....kSsssSk...",
    "....kkkkkkk...",
]
FM_SMALL = [
    "...kkkk...",
    "..krRRrk..",
    ".krrwrrrk.",
    "kdrrrrrwdk",
    ".kkkkggkk.",
    "...kssk...",
    "...kSsk...",
    "...kkkk...",
]
FM_SPROUT = [
    "..kkk..",
    ".krrrk.",
    "kdrRrdk",
    ".kkkkk.",
    "..ksk..",
    "..kkk..",
]

def mycelium(d, rnd, x0, y0, x1, y1):
    """Ground patch with branching pale threads (the mycelium) and a few root nodes."""
    d.rounded_rectangle([x0, y0, x1, y1], radius=22, fill=(74, 52, 40), outline=(40, 28, 22), width=6)
    for _ in range(90):
        x, y = rnd.randrange(x0 + 12, x1 - 12, 8), rnd.randrange(y0 + 12, y1 - 12, 8)
        d.rectangle([x, y, x + 7, y + 7], fill=(66, 46, 36))
    # threads: random walks in 8px steps, purple-white like the in-game roots
    for _ in range(46):
        x, y = rnd.randrange(x0 + 40, x1 - 40, 8), rnd.randrange(y0 + 30, y1 - 30, 8)
        for _ in range(rnd.randint(8, 22)):
            dx, dy = rnd.choice([(8, 0), (-8, 0), (0, 8), (0, -8), (8, 8), (-8, 8)])
            nx, ny = x + dx, y + dy
            if not (x0 + 16 < nx < x1 - 16 and y0 + 16 < ny < y1 - 16): break
            d.line([(x, y), (nx, ny)], fill=(196, 170, 214), width=4)
            x, y = nx, ny
        d.rectangle([x - 4, y - 4, x + 4, y + 4], fill=(226, 208, 236))
    # a few glowing nodes
    for _ in range(14):
        x, y = rnd.randrange(x0 + 40, x1 - 40, 8), rnd.randrange(y0 + 30, y1 - 30, 8)
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(232, 220, 250))

def fastermushrooms():
    img = cave_bg(seed=21, top=(20, 14, 30), bottom=(52, 30, 44)).convert("RGBA")
    d = ImageDraw.Draw(img)
    rnd = random.Random(42)
    text_shadow(d, (W // 2, 40), "FASTER MUSHROOMS", font(80), (250, 240, 235))
    text_shadow(d, (W // 2, 134), "Mushrooms spread over mycelium and grow up to 100x faster.", font(28), (225, 200, 215))

    # the mycelium bed across the middle
    gy0, gy1 = 220, 600
    mycelium(d, rnd, 70, gy0, W - 70, gy1)

    # growth stages left to right: sprout, small, mid, big, with speed lines between
    stages = [(FM_SPROUT, 8, 150), (FM_SMALL, 9, 340), (FM_MID, 10, 560), (FM_BIG, 12, 840)]
    base_y = gy1 - 50
    for i, (spr, sc, cx) in enumerate(stages):
        s = sprite(spr, PM, sc)
        sx, sy = cx - s.width // 2, base_y - s.height
        # soft shadow on the ground
        d.ellipse([sx - 6, base_y - 12, sx + s.width + 6, base_y + 14], fill=(52, 36, 30))
        img.paste(s, (sx, sy), s)
        if i < len(stages) - 1:
            nx = stages[i + 1][2]
            ax = (cx + nx) // 2
            ay = base_y - 90
            # three speed lines then an arrowhead
            for k in range(3):
                ly = ay - 18 + k * 18
                d.line([(ax - 46 + k * 6, ly), (ax - 6, ly)], fill=(255, 230, 120), width=5)
            d.polygon([(ax, ay - 24), (ax + 34, ay), (ax, ay + 24)], fill=(255, 230, 120))

    # a second big one and a couple of extra small ones so the bed looks populated
    extra = sprite(FM_BIG, PM, 9); img.paste(extra, (1030, base_y - extra.height - 20), extra)
    for (ex, ey, sc) in [(1120, base_y - 6, 6), (960, base_y - 90, 5), (250, base_y - 140, 5), (680, base_y - 160, 5)]:
        e = sprite(FM_SMALL, PM, sc); img.paste(e, (ex, ey - e.height), e)

    # multiplier tag
    d.rounded_rectangle([W - 330, 232, W - 90, 316], radius=16, fill=(28, 22, 40), outline=(255, 230, 120), width=4)
    text_shadow(d, (W - 210, 248), "SPEED  x4", font(44), (255, 230, 120))

    d.rounded_rectangle([W // 2 - 500, 620, W // 2 + 500, 700], radius=16, fill=(16, 12, 24, 230), outline=(160, 120, 190), width=3)
    text_shadow(d, (W // 2, 634), "Pick 1x to 100x in Mod Settings.  Default 4x, 1x = vanilla.", font(28), (240, 230, 245))
    text_shadow(d, (W // 2, 670), "Only mushrooms.  Other crops and root plants stay as they are.", font(22), (180, 160, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\fastermushrooms_logo.png")
    print("fastermushrooms_logo.png")

# ---------- Better Fishing Loot ----------
PF = dict(P)
PF.update({
    'v': (232, 196, 88),   # gold trim
    'V': (170, 132, 50),
    'h': (70, 74, 96),     # dark steel (miner)
    'H': (44, 46, 62),
    'l': (255, 200, 80),   # lamp glow
    'L': (255, 240, 180),
    'e': (52, 52, 70),     # ninja cloth
    'E': (30, 30, 42),
    'x': (214, 60, 70),    # ninja trim
    'q': (90, 200, 230),   # water drip
})
# miner helm: dark steel dome with a lamp on the front
MINER_HELM = [
    ".....kkkkkk.....",
    "...kkhhhhhhkk...",
    "..khhhhhhhhhhk..",
    ".khhhhkkkkhhhhk.",
    ".khhhklLLlkhhhk.",
    "khhhhklLLlkhhhhk",
    "khhhhhkllkhhhhhk",
    "kHHHHHHkkHHHHHHk",
    "kHkkkkkkkkkkkkHk",
    ".kk..........kk.",
]
# ninja hood: dark cloth with an eye slit and red trim
NINJA_HELM = [
    "....kkkkkkkk....",
    "..kkeeeeeeeekk..",
    ".keeeeeeeeeeeek.",
    ".keeeeeeeeeeeek.",
    "keeekkkkkkkkeeek",
    "keekwwwkkwwwkeek",
    "keeekkkkkkkkeeek",
    "kEEEEExxxxEEEEEk",
    ".kEEEEEEEEEEEEk.",
    "..kkkkkkkkkkkk..",
]
NINJA_CHEST = [
    "..kkk......kkk..",
    ".keeekkkkkkeeek.",
    "keeeeeeeeeeeeeek",
    "keeeeexxxxeeeeek",
    ".kkeeeeeeeeeekk.",
    "..keeeexxeeeek..",
    "..kEEEEEEEEEEk..",
    "..kEEEkkkkEEEk..",
    "..kkkk....kkkk..",
]
MINER_PANTS = [
    "kkkkkkkkkkkkkk",
    "khhhhhhhhhhhhk",
    "khhhhhkkhhhhhk",
    "khhhhk..khhhhk",
    "kHHHHk..kHHHHk",
    "kHHHHk..kHHHHk",
    "kkkkkk..kkkkkk",
]
GOLD_RING = [
    "....kkkk....",
    "...kvvvvk...",
    "..kvkccckv..",
    ".kvk.ccc.kv.",
    ".kv......vk.",
    ".kV......Vk.",
    "..kV....Vk..",
    "...kVVVVk...",
    "....kkkk....",
]
DRIP = [
    ".q.",
    "qqq",
    ".q.",
]

def betterfishingloot():
    img = cave_bg(seed=9, top=(12, 20, 38), bottom=(16, 44, 76)).convert("RGBA")
    d = ImageDraw.Draw(img)
    # water from the middle down
    wy = 430
    for y in range(wy, H):
        t = (y - wy) / (H - wy)
        d.line([(0, y), (W, y)], fill=(int(16 + 8 * t), int(76 - 26 * t), int(136 - 46 * t)))
    rnd = random.Random(13)
    for _ in range(70):
        x = rnd.randrange(0, W, 8); y = rnd.randrange(wy + 10, H - 8, 8)
        d.rectangle([x, y, x + rnd.choice([16, 24, 40]), y + 3], fill=(56, 144, 196))
    # rod bending under the weight, from the left bank up and over
    d.line([(120, 420), (330, 150)], fill=(40, 28, 26), width=14)
    d.line([(120, 420), (330, 150)], fill=(120, 80, 60), width=6)
    d.line([(330, 150), (470, 110)], fill=(40, 28, 26), width=12)
    d.line([(330, 150), (470, 110)], fill=(120, 80, 60), width=5)
    # taut line down to the catch
    d.line([(470, 110), (640, 300)], fill=(236, 236, 244), width=3)
    # the catch: a miner helm on the hook, dripping, with the rest of the set surfacing behind it
    helm = sprite(MINER_HELM, PF, 9)
    img.paste(helm, (640 - helm.width // 2, 300), helm)
    for (dx, dy, sc) in [(600, 405, 4), (665, 420, 4), (640, 440, 3)]:
        dr = sprite(DRIP, PF, sc); img.paste(dr, (dx, dy), dr)
    # splash rings where it broke the surface
    for r in (36, 70, 110):
        d.ellipse([640 - r, 470 - r // 4, 640 + r, 470 + r // 4], outline=(200, 232, 255), width=3)
    # more gear bobbing up out of the water to the right, brighter = closer
    chest = sprite(NINJA_CHEST, PF, 8); img.paste(chest, (800, 372), chest)
    hood = sprite(NINJA_HELM, PF, 7); img.paste(hood, (960, 300), hood)
    pants = sprite(MINER_PANTS, PF, 7); img.paste(pants, (1080, 400), pants)
    ring = sprite(GOLD_RING, PF, 6); img.paste(ring, (880, 250), ring)
    for (cx, cy) in [(864, 470), (1016, 470), (1130, 500)]:
        for r in (24, 48):
            d.ellipse([cx - r, cy - r // 4, cx + r, cy + r // 4], outline=(180, 220, 250), width=2)
    # odds tag: vanilla vs boosted
    d.rounded_rectangle([50, 470, 450, 600], radius=16, fill=(20, 18, 34, 235), outline=(232, 196, 88), width=4)
    text_shadow(d, (250, 486), "MINER SET PIECE", font(30), (232, 196, 88))
    text_shadow(d, (250, 528), "1%  ->  4.5%  per bite", font(28), (245, 245, 255))
    text_shadow(d, (250, 566), "at the default 5x", font(22), (170, 180, 210))
    # title + footer
    text_shadow(d, (W // 2, 36), "BETTER FISHING LOOT", font(80), (245, 245, 255))
    d.rounded_rectangle([W // 2 - 500, 618, W // 2 + 500, 700], radius=16, fill=(14, 20, 34, 230), outline=(90, 140, 190), width=3)
    text_shadow(d, (W // 2, 632), "Armor, rings, necklaces and rare finds bite 1x to 25x more often.", font(28), (220, 240, 255))
    text_shadow(d, (W // 2, 668), "Fish, ore and kelp are exactly as in vanilla.", font(24), (160, 200, 230))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\betterfishingloot_logo.png")
    print("betterfishingloot_logo.png")

# ---------- Five Loadouts ----------
def fiveloadouts():
    """A character window with five loadout tabs; tabs 4 and 5 are the new ones."""
    img = cave_bg(seed=13, top=(20, 16, 36), bottom=(48, 30, 66)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "FIVE LOADOUTS", font(84), (245, 240, 255))
    text_shadow(d, (W // 2, 138), "Two more equipment loadouts. Same window, same cycle key.", font(28), (200, 190, 225))

    # window frame
    fx0, fy0, fx1, fy1 = 120, 262, W - 120, 636
    d.rounded_rectangle([fx0, fy0, fx1, fy1], radius=20, fill=(34, 28, 50), outline=(90, 76, 130), width=5)

    # five tabs along the top edge of the window
    tabw, tabh, gap = 170, 56, 20
    total = 5 * tabw + 4 * gap
    tx0 = (W - total) // 2
    ty = fy0 - tabh + 8
    for i in range(5):
        x = tx0 + i * (tabw + gap)
        new = i >= 3
        fill = (76, 62, 108) if new else (52, 44, 74)
        outline = (255, 210, 110) if new else (140, 120, 190)
        d.rounded_rectangle([x, ty, x + tabw, ty + tabh + 16], radius=12, fill=fill, outline=outline, width=4)
        text_shadow(d, (x + tabw // 2, ty + 10), str(i + 1), font(38), (255, 235, 150) if new else (220, 210, 245))
        if new:
            d.rounded_rectangle([x + tabw - 62, ty - 22, x + tabw + 8, ty + 6], radius=8, fill=(214, 60, 70), outline=(28, 22, 30), width=3)
            text_shadow(d, (x + tabw - 27, ty - 19), "NEW", font(20), (255, 245, 245))

    # inside the window: one column of three gear slots per loadout
    slot, sgap = 80, 12
    y0 = fy0 + 36
    icons_by_col = [
        [(HELM, 5), (CHEST, 5), (SWORD, 5)],
        [(HELM, 5), (RING, 6), (BAG, 6)],
        [(CHEST, 5), (PANTS, 6), (LANTERN, 6)],
        [(RING, 6), (SWORD, 5), (BAG, 6)],
        [(HELM, 5), (LANTERN, 6), (PANTS, 6)],
    ]
    for i in range(5):
        new = i >= 3
        cx = tx0 + i * (tabw + gap) + (tabw - slot) // 2
        for r in range(3):
            sy = y0 + r * (slot + sgap)
            slot_panel(d, cx, sy, slot,
                       fill=(66, 54, 96) if new else (58, 50, 78),
                       outline=(255, 210, 110) if new else (140, 120, 190))
            spr, sc = icons_by_col[i][r]
            s = sprite(spr, P, sc)
            img.paste(s, (cx + (slot - s.width) // 2, sy + (slot - s.height) // 2), s)
        text_shadow(d, (cx + slot // 2, y0 + 3 * (slot + sgap) + 4), f"LOADOUT {i + 1}", font(24),
                    (255, 235, 150) if new else (200, 190, 225))

    text_shadow(d, (W // 2, 664), "Pairs with Loadout Fallback: loadouts 4 and 5 inherit from loadout 1 too.", font(26), (170, 160, 200))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\fiveloadouts_logo.png")
    print("fiveloadouts_logo.png")

# ---------- Auto Replant ----------
AR_P = dict(P, **{
    'l': (150, 220, 120),  # leaf light
    'n': (70, 160, 90),    # leaf
    'N': (36, 100, 60),    # leaf shade
    'd': (86, 58, 40),     # soil
    'D': (120, 84, 56),    # soil light
    'g': (250, 210, 90),   # gold leaf light
    'e': (232, 184, 60),   # gold leaf
    'G': (200, 150, 40),   # gold leaf shade
})
AR_RIPE = [
    "....kk...kk...",
    "...kllk.kllk..",
    "..klnnlknnlnk.",
    ".klnnrnnnnrnlk",
    ".knnnnnrnnnnnk",
    ".kNnrnnnnrnnNk",
    "..kNNnrnnnNNk.",
    "...kkNNNNNkk..",
    ".....kNNNk....",
    "......kNk.....",
    "......kkk.....",
]
AR_SPROUT = [
    "..kk...kk.",
    ".kllk.kllk",
    ".klnnknnlk",
    "..knnnnnk.",
    "...kknkk..",
    ".....kNk..",
    "....kNNk..",
    ".....kk...",
]
AR_GOLD_SPROUT = [r.replace('l', 'g').replace('n', 'e').replace('N', 'G') for r in AR_SPROUT]
AR_SEED = [
    "..kk..",
    ".kbBk.",
    "kbBBBk",
    "kBBBbk",
    ".kBBk.",
    "..kk..",
]
AR_BERRY = [
    "..kk..",
    ".krrk.",
    "krrwrk",
    "krrrrk",
    ".krrk.",
    "..kk..",
]
AR_SPARK = [
    "..k..",
    ".kyk.",
    "kyyyk",
    ".kyk.",
    "..k..",
]

def ar_soil_tile(d, x, y, w, h, seed):
    rnd = random.Random(seed)
    d.rounded_rectangle([x, y, x + w, y + h], radius=10, fill=AR_P['d'], outline=(40, 26, 20), width=4)
    for _ in range(26):
        px, py = x + 10 + rnd.randrange(0, w - 26, 6), y + 10 + rnd.randrange(0, h - 26, 6)
        d.rectangle([px, py, px + 6, py + 6], fill=AR_P['D'])

def autoreplant():
    img = cave_bg(seed=11, top=(16, 26, 22), bottom=(30, 56, 40)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 40), "AUTO REPLANT", font(80), (235, 255, 235))
    text_shadow(d, (W // 2, 134), "Harvest a crop and it is planted right back from your seeds.", font(28), (190, 225, 195))

    tile_w, tile_h, gap = 190, 150, 50
    x0 = (W - (4 * tile_w + 3 * gap)) // 2
    y0 = 330
    labels = ["HARVEST", "REPLANTED", "GROWING", "GOLDEN"]
    for i in range(4):
        tx = x0 + i * (tile_w + gap)
        ar_soil_tile(d, tx, y0, tile_w, tile_h, seed=20 + i)
        text_shadow(d, (tx + tile_w // 2, y0 + tile_h + 16), labels[i], font(26), (215, 235, 215))

    # tile 0: ripe bush, crop popping out with an up arrow
    tx = x0
    s = sprite(AR_RIPE, AR_P, 11)
    img.paste(s, (tx + (tile_w - s.width) // 2, y0 + tile_h - s.height - 6), s)
    b = sprite(AR_BERRY, AR_P, 8)
    img.paste(b, (tx + tile_w // 2 - b.width // 2 + 40, y0 - 70), b)
    d.polygon([(tx + 60, y0 - 18), (tx + 84, y0 - 54), (tx + 108, y0 - 18)], fill=(240, 240, 240))
    d.rectangle([tx + 76, y0 - 22, tx + 92, y0 + 10], fill=(240, 240, 240))

    # arrow tile0 -> tile1
    ax = x0 + tile_w + gap // 2
    ay = y0 + tile_h // 2
    d.rectangle([ax - 22, ay - 7, ax + 4, ay + 7], fill=(215, 235, 215))
    d.polygon([(ax + 2, ay - 18), (ax + 26, ay), (ax + 2, ay + 18)], fill=(215, 235, 215))

    # tile 1: seed dropping in, fresh sprout with motion lines
    tx = x0 + (tile_w + gap)
    sd = sprite(AR_SEED, AR_P, 8)
    img.paste(sd, (tx + tile_w // 2 - sd.width // 2 + 46, y0 - 60), sd)
    for k in range(3):
        yy = y0 - 8 + k * 14
        d.line([(tx + tile_w // 2 + 40, yy), (tx + tile_w // 2 + 40, yy + 6)], fill=(230, 230, 200), width=3)
    sp = sprite(AR_SPROUT, AR_P, 9)
    img.paste(sp, (tx + (tile_w - sp.width) // 2 - 10, y0 + tile_h - sp.height - 10), sp)
    for (dx, dy) in [(-26, -30), (26, -30), (-34, -6), (34, -6)]:
        cx, cy = tx + tile_w // 2 - 10 + dx, y0 + tile_h - sp.height - 4 + dy
        d.line([(cx, cy), (cx + (6 if dx > 0 else -6), cy - 8)], fill=(200, 255, 200), width=4)

    # tile 2: growing sprout
    tx = x0 + 2 * (tile_w + gap)
    sp2 = sprite(AR_SPROUT, AR_P, 11)
    img.paste(sp2, (tx + (tile_w - sp2.width) // 2, y0 + tile_h - sp2.height - 8), sp2)

    # tile 3: golden sprout with sparkles
    tx = x0 + 3 * (tile_w + gap)
    gs = sprite(AR_GOLD_SPROUT, AR_P, 11)
    img.paste(gs, (tx + (tile_w - gs.width) // 2, y0 + tile_h - gs.height - 8), gs)
    for (dx, dy, sc) in [(-64, -40, 5), (70, -60, 6), (60, 20, 4), (-70, 30, 4)]:
        sk = sprite(AR_SPARK, AR_P, sc)
        img.paste(sk, (tx + tile_w // 2 + dx - sk.width // 2, y0 + 40 + dy - sk.height // 2), sk)
    d.rounded_rectangle([tx + 18, y0 - 78, tx + tile_w - 18, y0 - 30], radius=12, fill=(40, 34, 20), outline=(220, 180, 70), width=3)
    text_shadow(d, (tx + tile_w // 2, y0 - 70), "5% GOLDEN", font(26), (250, 220, 110))

    d.rounded_rectangle([W // 2 - 500, 590, W // 2 + 500, 690], radius=16, fill=(12, 22, 16, 230), outline=(90, 160, 110), width=3)
    text_shadow(d, (W // 2, 604), "Uses the seed the harvest drops, or one you already carry.", font(28), (215, 240, 220))
    text_shadow(d, (W // 2, 646), "Same XP and growth as planting by hand.  Golden chance is configurable.", font(24), (160, 205, 170))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\autoreplant_logo.png")
    print("autoreplant_logo.png")

# ---------- Quick Buff: a "B" key and a burst of potions / food ----------
PQ = dict(P, **{
    'p': (120, 110, 130),  # keycap side
    'q': (232, 228, 240),  # keycap top
    'Q': (190, 184, 205),  # keycap top shade
    'v': (150, 80, 220),   # violet potion
    'V': (100, 50, 160),
    'e': (80, 210, 120),   # green potion
    'E': (40, 140, 80),
    'a': (230, 70, 80),    # red apple / meat
    'A': (150, 40, 50),
    't': (250, 180, 90),   # bread / tan
    'T': (190, 120, 50),
    'm': (200, 150, 110),  # mushroom cap
    'M': (140, 90, 60),
    'l': (255, 255, 255),  # glass highlight
    'h': (90, 130, 180),   # blue potion
    'H': (50, 80, 130),
})

QB_KEYCAP_B = [
    "..kkkkkkkkkkkkkkkkkk..",
    ".kqqqqqqqqqqqqqqqqqqk.",
    "kqqqqqqqqqqqqqqqqqqqqk",
    "kqqqqkkkkkkkkqqqqqqqqk",
    "kqqqqkkkkkkkkkkqqqqqqk",
    "kqqqqkkkqqqqkkkkqqqqqk",
    "kqqqqkkkqqqqqkkkqqqqqk",
    "kqqqqkkkqqqqkkkkqqqqqk",
    "kqqqqkkkkkkkkkkqqqqqqk",
    "kqqqqkkkkkkkkkkkqqqqqk",
    "kqqqqkkkqqqqqkkkkqqqqk",
    "kqqqqkkkqqqqqqkkkqqqqk",
    "kqqqqkkkqqqqqkkkkqqqqk",
    "kqqqqkkkkkkkkkkkqqqqqk",
    "kqqqqkkkkkkkkkkqqqqqqk",
    "kQQQQQQQQQQQQQQQQQQQQk",
    "kQQQQQQQQQQQQQQQQQQQQk",
    ".kppppppppppppppppppk.",
    ".kppppppppppppppppppk.",
    "..kkkkkkkkkkkkkkkkkk..",
]

def qb_potion(body, shade):
    return [
        "....kkkk....",
        "...kGGGGk...",
        "...kBBBBk...",
        "....kllk....",
        "...kllllk...",
        "..kl" + body * 4 + "lk..",
        ".kl" + body * 6 + "lk.",
        ".k" + body * 8 + "k.",
        ".k" + body * 3 + "l" + body * 4 + "k.",
        ".k" + body * 8 + "k.",
        ".k" + shade + body * 6 + shade + "k.",
        "..k" + shade * 6 + "k..",
        "...kkkkkk...",
    ]

QB_APPLE = [
    "......kk....",
    ".....kBk....",
    "....kBk.....",
    "..kkkakkkk..",
    ".kaaaaaaaak.",
    "kaalaaaaaaak",
    "kaalaaaaaaak",
    "kaaaaaaaaaak",
    "kAaaaaaaaaAk",
    ".kAAaaaaAAk.",
    "..kAAAAAAk..",
    "...kkkkkk...",
]
QB_MUSHROOM = [
    "....kkkkkk....",
    "..kkmmmmmmkk..",
    ".kmmlmmmmmmmk.",
    "kmmmmmmlmmmmmk",
    "kMmmmmmmmmmMMk",
    ".kMMMMMMMMMMk.",
    "..kkkttttkkk..",
    ".....kttk.....",
    ".....kttk.....",
    ".....kTTk.....",
    "......kk......",
]
QB_BREAD = [
    "....kkkkkkk...",
    "..kktttttttkk.",
    ".kttlttttttttk",
    "kttlttttttttTk",
    "kttttttttttTTk",
    "kTtttttttTTTTk",
    ".kTTTTTTTTTTk.",
    "..kkkkkkkkkk..",
]
QB_MEAT = [
    "..kkkkkkk.....",
    ".kaaaaaaakk...",
    "kaalaaaaaaakk.",
    "kaaaaaaAaaaak.",
    "kaaaaAAAaaaak.",
    ".kAAAAaaaaAk..",
    "..kkkAAAAkk...",
    ".....kttk.....",
    "......kttk....",
    ".......kwk....",
    "........k.....",
]

def quickbuff():
    img = cave_bg(seed=11, top=(20, 16, 36), bottom=(52, 26, 70)).convert("RGBA")
    cx, cy = 380, 390
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for r in range(260, 0, -8):
        a = int(90 * (1 - r / 260))
        gd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(200, 150, 255, a))
    img = Image.alpha_composite(img, glow)
    d = ImageDraw.Draw(img)

    key = sprite(QB_KEYCAP_B, PQ, 12)
    img.paste(key, (cx - key.width // 2, cy - key.height // 2), key)

    rnd = random.Random(9)
    for ang in range(-70, 71, 14):
        a = math.radians(ang)
        x0, y0 = cx + 150 * math.cos(a), cy + 150 * math.sin(a)
        ln = rnd.randint(40, 90)
        x1, y1 = cx + (150 + ln) * math.cos(a), cy + (150 + ln) * math.sin(a)
        d.line([(x0, y0), (x1, y1)], fill=(255, 230, 150), width=6)

    items = [
        (qb_potion('v', 'V'), 7), (QB_APPLE, 7), (qb_potion('e', 'E'), 7), (QB_MUSHROOM, 6),
        (qb_potion('h', 'H'), 7), (QB_MEAT, 6), (QB_BREAD, 6), (FISH, 5),
    ]
    n = len(items)
    for i, (spr, sc) in enumerate(items):
        a = math.radians(-30 + i * (60 / (n - 1)))
        radius = 300 if i % 2 == 0 else 385
        x, y = cx + radius * math.cos(a), cy + radius * math.sin(a)
        s = sprite(spr, PQ, sc)
        hr = max(s.width, s.height) // 2 + 14
        d.ellipse([x - hr, y - hr, x + hr, y + hr], fill=(70, 50, 100), outline=(150, 120, 200), width=4)
        img.paste(s, (int(x - s.width / 2), int(y - s.height / 2)), s)

    # four buff tags with timer bars: everything applied at once
    bx, by = 890, 330
    for j, col in enumerate([(150, 80, 220), (80, 210, 120), (90, 130, 180), (230, 70, 80)]):
        x = bx + j * 78
        d.rounded_rectangle([x, by, x + 62, by + 62], radius=10, fill=(40, 30, 60), outline=col, width=5)
        d.rectangle([x + 27, by + 14, x + 35, by + 48], fill=col)
        d.rectangle([x + 14, by + 27, x + 48, by + 35], fill=col)
        d.rectangle([x, by + 74, x + 62, by + 84], fill=(30, 24, 44))
        d.rectangle([x, by + 74, x + 62 - j * 9, by + 84], fill=col)
    text_shadow(d, (bx + 155, by - 44), "ALL BUFFS AT ONCE", font(28), (230, 220, 250))

    text_shadow(d, (W // 2, 22), "QUICK BUFF", font(84), (245, 240, 255))
    text_shadow(d, (W // 2, 112), "Press B.  Eat one of every food, drink one of every potion.", font(28), (210, 200, 235))
    d.rounded_rectangle([W // 2 - 470, 640, W // 2 + 470, 706], radius=16, fill=(14, 12, 30, 230), outline=(140, 110, 200), width=3)
    text_shadow(d, (W // 2, 656), "All your buffs in one key press.  Skips buffs that are still running.", font(26), (230, 220, 250))
    img.convert("RGB").save(r"C:\Users\Sid\CoreKeeperMods\release\quickbuff_logo.png")
    print("quickbuff_logo.png")

if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "all"
    if which in ("fiveloadouts", "all"): fiveloadouts()
    if which in ("betterfishingloot", "all"): betterfishingloot()
    if which in ("fastermushrooms", "all"): fastermushrooms()
    if which in ("loadout", "all"): loadout()
    if which in ("fishing", "all"): fishing()
    if which in ("wateringcans", "all"): wateringcans()
    if which in ("buffduration", "all"): buffduration()
    if which in ("durability", "all"): durability()
    if which in ("autoreplant", "all"): autoreplant()
    if which in ("quickbuff", "all"): quickbuff()
