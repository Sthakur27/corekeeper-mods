"""Pixel-art style mod.io listing images (1280x720). python make_art.py [loadout|fishing|all]"""
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

which = sys.argv[1] if len(sys.argv) > 1 else "all"
if which in ("loadout", "all"): loadout()
if which in ("fishing", "all"): fishing()
