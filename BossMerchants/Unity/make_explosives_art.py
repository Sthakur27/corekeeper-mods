"""Boss Merchants: Fizzwick the Molten Bombardier (key `explosives`) + the Igneous Fuse Core idol.

    python -I BossMerchants/Unity/make_explosives_art.py

Everything is drawn from code (shape masks + material rules + small hand-authored grids); no vanilla pixels are
copied. Vanilla sheets are only *read* for the side-by-side preview (build/bossmerchants/reference, if present).

Rig: SlimeMerchant (texture `Slime_merchant`, 300x120, 38 cells of 30x30, pivot centre, ground line y=25).
Drawn in Core Keeper's top-down 3/4 view (docs/perspective_rules.md): visible top planes, oval brims/bases.
Clip frame order (from the game's AnimationClips):
  0-9   SlimeMerchantIdleR      10-15 SlimeMerchantMoveR      32-37 SlimeMerchantWaveR (+ Float)
  16-31 SlimeMerchantStand: 16-19 asleep, 20-31 getting up, then 6-9 of the idle
Fizzwick: a lava slime split off from Igneous, wearing an iron blast helmet with a lit fuse (sits where the vanilla
candle glow is), soot goggles, a bomb belt, and always a bomb in hand. Asleep it melts into a cooling puddle
with the helmet and bomb beside it, then pulls itself back together.

Writes:
  Art/merchants/explosives/explosives__Slime_merchant.png + sheets.json
  Art/idols/explosives_icon.png, explosives_smallIcon.png, explosives_placed.png
  Art/previews/explosives.png
Exits 1 if validation fails.
"""
import json, math, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, 'Art')
ROOT = os.path.dirname(os.path.dirname(HERE))
REF = os.path.join(ROOT, 'build', 'bossmerchants')
KEY = 'explosives'
TEX = 'Slime_merchant'
SHEET_W, SHEET_H, CELL, NFRAMES = 300, 120, 30, 38
GROUND = 25


def hx(s):
    return (int(s[1:3], 16), int(s[3:5], 16), int(s[5:7], 16), 255)


# Palette (Igneous / lava slime materials + vanilla bomb steel + rope leather)
LAVA = [hx(c) for c in ('#91210b', '#b43221', '#da6210', '#e2a12c', '#f2df3a', '#f7eec7')]
LAVA_OL = hx('#5e2a28')
CRUST = [hx(c) for c in ('#232930', '#383447', '#484454', '#645d53', '#8f7b77')]
STEEL = [hx(c) for c in ('#2a4e4a', '#47677e', '#677fae', '#a1aedd')]
STEEL_OL = hx('#073c31')
LEATHER = [hx(c) for c in ('#39341f', '#614822', '#915b26', '#b58047')]
SPARK = [hx(c) for c in ('#c5460a', '#da6210', '#f2df3a', '#f7eec7')]
SMOKE = hx('#8f7b77')


def clamp(v, a, b):
    return a if v < a else b if v > b else v


def lerp(a, b, t):
    return a + (b - a) * t


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = {}

    def put(self, x, y, c):
        if c is not None and 0 <= x < self.w and 0 <= y < self.h:
            self.px[(x, y)] = c

    def grid(self, x0, y0, rows, legend):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch != '.':
                    self.put(x0 + i, y0 + j, legend[ch])

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        for (x, y), c in self.px.items():
            im.putpixel((x, y), c)
        return im


def is_border(mask, x, y):
    return any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))


def circle(cx, cy, r):
    return {(x, y) for x in range(int(cx - r) - 1, int(cx + r) + 2) for y in range(int(cy - r) - 1, int(cy + r) + 2)
            if (x + .5 - cx) ** 2 + (y + .5 - cy) ** 2 <= r * r}


# ---------------------------------------------------------------------------------------------- character parts
# Top-down 3/4 view (perspective_rules.md): the camera looks down, so every part shows its top plane (brightest),
# a front face (mid-tone, darker toward the floor) and a curved oval bottom edge. Light comes from above/top-left.

def ellipse(cx, cy, rx, ry):
    return {(x, y) for x in range(int(cx - rx) - 1, int(cx + rx) + 2) for y in range(int(cy - ry) - 1, int(cy + ry) + 2)
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1.0}


def body_mask(cx, T, B, A, r1, flare=1.5):
    """Slime mound seen from above: domed top half (radius r1 down from T), a wider squarish-round lower half
    (superellipse) spreading onto the floor; the bottom row is the flat contact with the ground."""
    cyc = T + r1
    rb = B + 1 - cyc
    m = set()
    for y in range(T, B + 1):
        for x in range(int(cx - A - flare) - 2, int(cx + A + flare) + 3):
            dx = x + .5 - cx
            dy = y + .5 - cyc
            if dy <= 0:
                inside = (dx / A) ** 2 + (dy / r1) ** 2 <= 1.0
            else:
                a = A + flare * (dy / rb) + 0.8 * math.sin(math.pi * min(1.0, dy / rb))
                k = max(0.0, (dy - (rb - 3.0)) / 3.2)      # round off only the last ~3 rows: oval floor contact
                inside = abs(dx) <= a * math.sqrt(max(0.0, 1 - k * k))
            if inside:
                m.add((x, y))
    return m


LIGHT = (-0.25, -0.85, 0.46)   # from above / top-left, camera looking down


def shade(x, y, cx, T, B, A, r1):
    cyc = T + r1
    rb = B + 1 - cyc
    nx = clamp((x + .5 - cx) / (A + .5), -1, 1)
    dy = y + .5 - cyc
    ny = clamp(dy / r1, -1, 1) if dy <= 0 else clamp(0.35 * dy / rb, -1, 1)
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    L = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
    if dy > 0:
        L -= 0.5 * (dy / rb) ** 2.2   # front face darkens toward the floor
    return L


SEAM = [0, 0, 1, 1, 1, 0, 0, -1, -1, 0]   # designed 2-3 px steps for the crust edge, never single-pixel noise


def clean(idx):
    """Remove isolated single pixels in an index map (no speckle): take the majority 4-neighbour."""
    out = dict(idx)
    for (x, y), v in idx.items():
        nb = [idx[p] for p in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)) if p in idx]
        if len(nb) >= 3 and all(n != v for n in nb):
            out[(x, y)] = max(sorted(set(nb)), key=nb.count)
    return out


def draw_body(cv, cx, T, B, A, r1, crust_frac=0.0, glow=True, bubble=None, plates=True, shadow_mask=None):
    m = body_mask(cx, T, B, A, r1)
    cy1 = T + r1
    border = {p for p in m if is_border(m, *p)}
    top_ell = ellipse(cx, cy1, A, r1)
    lava_idx, crust_idx = {}, {}
    seam_row = {x: T + int(round(crust_frac * 2 * r1)) + SEAM[x % len(SEAM)] for x in range(CELL)}
    for (x, y) in m:
        if (x, y) in border:
            continue
        L = shade(x, y, cx, T, B, A, r1)
        i = 4 if L > 0.86 else 3 if L > 0.62 else 2 if L > 0.28 else 1 if L > -0.05 else 0
        if crust_frac > 0 and y < seam_row[x]:
            crust_idx[(x, y)] = {4: 4, 3: 3, 2: 2, 1: 1, 0: 1}[i]
        else:
            lava_idx[(x, y)] = i
    if shadow_mask:                     # occlusion under overhangs (helmet brim)
        for p in shadow_mask:
            if p in lava_idx:
                lava_idx[p] = max(0, min(lava_idx[p], 2) - 1)
            if p in crust_idx:
                crust_idx[p] = max(1, crust_idx[p] - 1)
    lava_idx, crust_idx = clean(lava_idx), clean(crust_idx)
    for p, i in lava_idx.items():
        cv.put(*p, LAVA[i])
    for p, i in crust_idx.items():
        cv.put(*p, CRUST[i])
    if crust_frac > 0:   # glowing seam where crust meets melt: one continuous line
        for (x, y) in lava_idx:
            if (x, y - 1) in crust_idx:
                cv.put(x, y, LAVA[4] if glow else LAVA[3])
    for (x, y) in border:   # outline: lighter on the lit top/left of the top plane, dark maroon elsewhere
        lit = y < cy1 and (x + .5 - cx) < A * 0.3
        if crust_frac > 0 and y < seam_row[x]:
            cv.put(x, y, CRUST[1] if lit else CRUST[0])
        else:
            cv.put(x, y, LAVA[0] if lit else LAVA_OL)
    if plates and crust_frac == 0:
        draw_plates(cv, m, top_ell, cx, cy1, A, r1)
    if bubble:
        bx, by, popped = bubble
        if (bx, by) in m and (bx, by - 1) in m:
            cv.put(bx, by, LAVA[5] if popped else LAVA[4])
            cv.put(bx + (1 if popped else 0), by - (0 if popped else 1), LAVA[4] if popped else LAVA[5])
    return m


def draw_plates(cv, m, top_ell, cx, cy1, A, r1):
    """Cooled Igneous crust plates lying on the top plane (left and right shoulders), each with a glowing crack."""
    y0 = int(cy1 - r1 * 0.35)
    # left shoulder plate: 3 rows, catches the most light
    for j, (dx0, w) in enumerate(((2, 3), (1, 4), (2, 3))):
        y = y0 + j
        xs = [x for (x, yy) in m if yy == y]
        if not xs:
            continue
        xl = min(xs)
        for k in range(w):
            x = xl + dx0 + k
            c = CRUST[4] if (j == 0 and k < 2) else CRUST[3] if j < 2 else CRUST[2]
            if k == w - 1 and j > 0:
                c = CRUST[2]
            cv.put(x, y, c)
        if j == 1:
            cv.put(xl + dx0 + w, y, LAVA[4])
    # right shoulder plate: 2 rows, darker (away from light)
    for j, (dx0, w) in enumerate(((3, 3), (2, 3))):
        y = y0 + 1 + j
        xs = [x for (x, yy) in m if yy == y]
        if not xs:
            continue
        xr = max(xs)
        for k in range(w):
            cv.put(xr - dx0 - k + 1 - 1, y, CRUST[2] if j == 0 else CRUST[1])
        if j == 0:
            cv.put(xr - dx0 - w, y, LAVA[3])


def draw_helmet(cv, hxc, hy, spark=None, lit=True):
    """Blast helmet seen from above: oval brim (top surface lit) around a domed crown with a riveted band.
    hxc/hy = brim centre. Returns the brim mask (for the shadow it casts)."""
    BR, CR = 7.0, 3.6
    brim = ellipse(hxc, hy, BR, 2.5)
    dome_top = {p for p in ellipse(hxc - 0.5, hy - 1.0, CR, 4.6) if p[1] + .5 <= hy - 1.0}
    dome_low = ellipse(hxc - 0.5, hy - 1.0, CR, 1.3)
    dome = dome_top | dome_low
    for (x, y) in brim:
        if (x, y) in dome:
            continue
        nx = (x + .5 - hxc) / BR
        if is_border(brim, x, y):
            c = STEEL[0] if (y + .5 < hy and nx < 0.3) else STEEL_OL
        elif y + .5 < hy + 0.8:
            c = STEEL[3] if nx < -0.1 else STEEL[2]
        else:
            c = STEEL[1]                   # front edge of the brim
        cv.put(x, y, c)
    band_y = {}
    for (x, y) in dome:
        band_y[x] = max(band_y.get(x, -99), y)
    for (x, y) in dome:
        nx = (x + .5 - (hxc - 0.5)) / CR
        if is_border(dome, x, y) and not (y == band_y[x]):
            c = STEEL[0] if (y + .5 < hy - 2.5 and nx < 0.3) else STEEL_OL
        elif y >= band_y[x] - 1:           # riveted band where the crown meets the brim (curves with the oval)
            c = STEEL_OL if y == band_y[x] else (STEEL[3] if x % 3 == 0 else STEEL[1])
        elif nx > 0.45:
            c = STEEL[1]
        elif y + .5 < hy - 2.5 and nx < 0.25:
            c = STEEL[3]                   # crown top: brightest plane
        else:
            c = STEEL[2]
        cv.put(x, y, c)
    top = min(y for (_, y) in dome)
    sx = int(hxc - 2.0)
    cv.put(sx, top + 1, hx('#d7dcfb'))
    # short wick on the crown, tilted back, then the spark
    wx = int(hxc) + 1
    cv.put(wx, top, LEATHER[1])
    cv.put(wx, top - 1, LEATHER[2])
    tip = (wx + 1, top - 2)
    if not lit:
        cv.put(*tip, CRUST[1])
        if spark is not None and spark % 2 == 0:
            cv.put(tip[0] + 1, tip[1] - 1, SMOKE)
            cv.put(tip[0] + 1, tip[1] - 2, SMOKE)
    else:
        for dx, dy, c in SPARKS[spark % len(SPARKS)]:
            cv.put(tip[0] + dx, tip[1] + dy, c)
    # shadow the brim casts on the slime: the rows just below its front edge
    shadow = set()
    for (x, y) in brim:
        if (x, y + 1) not in brim:
            shadow.add((x, y + 1))
    return brim | dome, shadow


SPARKS = [
    [(0, 0, SPARK[3]), (0, -1, SPARK[2]), (1, 0, SPARK[1]), (-1, 0, SPARK[1])],
    [(0, 0, SPARK[2]), (0, -1, SPARK[3]), (1, -1, SPARK[1]), (0, -2, SPARK[0])],
    [(0, 0, SPARK[3]), (1, 0, SPARK[2]), (1, -1, SPARK[1]), (-1, 0, SPARK[0])],
]

GOGGLE = ['.ab.', 'aLkb', '.bb.']          # foreshortened: seen from above the lenses are short
GOGGLE_LEG = {'a': STEEL[2], 'b': STEEL_OL, 'L': STEEL[3], 'k': CRUST[0]}
SHUT = ['....', 'oooo', '....']


def draw_face(cv, body, cx, gy, open_eyes=True, mouth=True):
    gx = int(cx) - 1
    for x in list(range(gx - 2, gx)) + [gx + 4] + list(range(gx + 9, gx + 11)):
        if (x, gy + 1) in body and not is_border(body, x, gy + 1):
            cv.put(x, gy + 1, LEATHER[1])
    g = GOGGLE if open_eyes else SHUT
    leg = GOGGLE_LEG if open_eyes else {'o': LAVA_OL}
    cv.grid(gx, gy, g, leg)
    cv.grid(gx + 5, gy, g, leg)
    if mouth:
        my = gy + 4
        cv.put(gx + 2, my, LAVA_OL)
        cv.put(gx + 7, my, LAVA_OL)
        for x in range(gx + 3, gx + 7):
            cv.put(x, my + 1, LAVA_OL)
        cv.put(gx + 4, my + 1, LAVA[5])
        cv.put(gx + 5, my + 1, LAVA[5])


MINI = ['o3o', '321', 'o1o']
MINI_LEG = {'o': STEEL_OL, '3': STEEL[3], '2': STEEL[2], '1': STEEL[1]}


def draw_belt(cv, body, cx, A, B):
    """Bomb belt round the base; seen from above it curves DOWN at the front like the bottom oval."""
    for x in range(int(cx - A) - 1, int(cx + A) + 2):
        nx = (x + .5 - cx) / (A + .5)
        if abs(nx) > 1:
            continue
        y = B - 3 + int(round(1.5 * (1 - nx * nx)))
        for dy, c in ((0, LEATHER[2]), (1, LEATHER[1])):
            if (x, y + dy) in body and not is_border(body, x, y + dy):
                cv.put(x, y + dy, c)
    x = int(cx) - 5
    nx = (x + .5 - cx) / (A + .5)
    y = B - 3 + int(round(1.5 * (1 - nx * nx))) - 1
    cv.grid(x - 1, y, MINI, MINI_LEG)


BOMB = [                      # sphere lit from above: bright top, dark underside
    '..ooo..',
    '.ps33o.',
    'p33332o',
    'o22221o',
    'o21110o',
    '.o100o.',
    '..ooo..',
]
BOMB_LEG = {'o': STEEL_OL, 'p': STEEL[0], 's': hx('#d7dcfb'), '3': STEEL[3], '2': STEEL[2], '1': STEEL[1], '0': STEEL[0]}


def draw_bomb(cv, x0, y0, spark=0, lit=True):
    cv.grid(x0, y0, BOMB, BOMB_LEG)
    cv.put(x0 + 3, y0 - 1, LEATHER[1])    # fuse stands up from the top, tilted back
    cv.put(x0 + 4, y0 - 2, LEATHER[2])
    if lit:
        cv.put(x0 + 4, y0 - 3, SPARK[3] if spark % 2 == 0 else SPARK[2])
        cv.put(x0 + 5, y0 - 3, SPARK[1])
        if spark % 3 == 1:
            cv.put(x0 + 4, y0 - 4, SPARK[0])
    else:
        cv.put(x0 + 4, y0 - 3, CRUST[1])


def draw_hand(cv, hx_, hy):
    m = ellipse(hx_, hy, 2.4, 1.9)
    for (x, y) in m:
        if is_border(m, x, y):
            c = LAVA[0] if y < hy - .5 else LAVA_OL
        else:
            c = LAVA[3] if y < hy else LAVA[1]
        cv.put(x, y, c)


CX = 14.5
A0 = 9.0
R1 = 7.0
TOP = 10          # body top row; helmet crown reaches ~y=6, spark ~y=3 (where the vanilla candle glow is)
BOMB_X = 21


def character(T=TOP, B=GROUND, A=A0, r1=R1, cx=CX, crust=0.0, glow=True, spark=0, bubble=None,
              eyes=True, mouth=True, bomb=(BOMB_X, None), hand=True, hand_dy=0, helmet=None, helmet_lit=True,
              belt=True, extras=()):
    cv = Canvas(CELL, CELL)
    cy1 = T + r1
    hxc, hy = helmet if helmet else (cx + 0.5, T + 2.0)
    rest_y = int(cy1)
    by = bomb[1] if bomb[1] is not None else rest_y
    # pre-compute the brim shadow so the body can be shaded with it
    probe = Canvas(CELL, CELL)
    _, shadow = draw_helmet(probe, hxc, hy, spark=spark, lit=helmet_lit)
    body = draw_body(cv, cx, T, B, A, r1, crust, glow, bubble=bubble, shadow_mask=shadow)
    if eyes is not None:
        draw_face(cv, body, cx, min(int(hy) + 4, B - 4), open_eyes=eyes, mouth=mouth)
    if belt:
        draw_belt(cv, body, cx, A, B)
    draw_helmet(cv, hxc, hy, spark=spark, lit=helmet_lit)
    if hand:
        draw_hand(cv, bomb[0] + 3.5, min(rest_y + 6.5, B - 2.5) + hand_dy)
    if bomb[0] is not None:
        draw_bomb(cv, bomb[0], by, spark=spark + 1)
    for x, y, c in extras:
        cv.put(x, y, c)
    return cv.image()


def frames():
    out = []
    # 0-9 idle: breathe (top sinks 1px, spreads), fuse flickers, one bubble rises on the front and pops
    dh = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0]
    bub = {4: (8, 21, False), 5: (8, 20, False), 6: (8, 19, False), 7: (8, 18, False), 8: (8, 17, True)}
    for i in range(10):
        out.append(character(T=TOP + dh[i], A=A0 + 0.5 * dh[i], spark=i % 3, bubble=bub.get(i)))
    # 10-15 move: squash, stretch, hop, drips left on the ground
    mv = [(0, 0.0, 0), (1, 0.5, 0), (2, 1.0, 0), (-1, -0.5, -1), (-2, -1.0, -2), (0, 0.0, -1)]
    drips = {3: [(4, 25, LAVA[2]), (5, 25, LAVA[1])], 4: [(2, 25, LAVA[1]), (3, 25, LAVA[1])], 5: [(1, 25, LAVA[0])]}
    for i, (dT, dA, lift) in enumerate(mv):
        out.append(character(T=TOP + dT + lift, B=GROUND + lift, A=A0 + dA, spark=i % 3, extras=drips.get(i, ())))
    # 16-19 asleep: a flat cooling crust puddle (all top plane), helmet + bomb set down beside it, fuse smoking
    for i in range(4):
        T = GROUND - 6 - (i % 2)
        out.append(character(T=T, A=11.5, r1=2.5 + 0.5 * (i % 2), crust=0.55, glow=i % 2 == 0, spark=i, eyes=False,
                             mouth=False, bomb=(BOMB_X + 1, GROUND - 6), hand=False, helmet=(9.5, GROUND - 4.0),
                             helmet_lit=False, belt=False))
    # 20-31 getting up: the puddle gathers into Fizzwick, helmet rides up onto him, goggles on, bomb picked up
    for k in range(12):
        t = (k + 1) / 12.0
        e = t * t * (3 - 2 * t)
        T = int(round(lerp(GROUND - 6, TOP, e)))
        r1 = lerp(2.5, R1, e)
        holding = t > 0.6
        tgt = (CX + 0.5, T + 2.0)
        hel = (lerp(9.5, tgt[0], e), lerp(GROUND - 4.0, tgt[1], e))
        out.append(character(T=T, A=round(lerp(11.5, A0, e) * 2) / 2, r1=r1, crust=0.55 * (1 - e) if t < 0.85 else 0.0,
                             glow=k % 2 == 0, spark=k % 3, eyes=(t >= 0.5) if t >= 0.4 else None, mouth=t >= 0.7,
                             bomb=(BOMB_X, None) if holding else (BOMB_X + 1, GROUND - 6), hand=holding,
                             helmet=hel, helmet_lit=t > 0.45, belt=t >= 0.5))
    # 32-37 wave: tosses the bomb up and catches it
    toss = [0, -3, -6, -8, -4, 0]
    hand = [0, -2, -3, -3, -2, 0]
    sq = [0, 1, 0, 0, 0, 1]
    for i in range(6):
        T = TOP + sq[i]
        out.append(character(T=T, A=A0 + 0.5 * sq[i], spark=i % 3,
                             bomb=(BOMB_X, int(T + R1) + toss[i]), hand_dy=hand[i]))
    return out


# --------------------------------------------------------------------------------------------------- idol
IDOL_LEG = {
    'K': CRUST[0], 'g': CRUST[1], '1': CRUST[2], '2': CRUST[3], '3': CRUST[4], '4': hx('#a69298'),
    'O': LAVA_OL, 'r': LAVA[0], 'R': LAVA[1], 'o': LAVA[2], 'y': LAVA[3], 'Y': LAVA[4], 'W': LAVA[5],
    's': STEEL_OL, 'a': STEEL[0], 'b': STEEL[1], 'd': STEEL[2], 'e': STEEL[3],
    'w': LEATHER[2], 'k': LEATHER[1], 'F': SPARK[0], 'f': SPARK[1],
}
# Igneous Fuse Core in 3/4 view: crusted orb (lit crown), iron band curving down across the front, glowing molten
# underside sitting in a lava-filled stone dish whose top is an oval (back rim behind the orb, lit front rim, dark
# front face and curved bottom edge on the icon's bottom row).
IDOL_ICON = [
    '...........Y....',
    '..........YWf...',
    '......seesw.....',
    '.....14433K.....',
    '...1444Y3332K...',
    '..1444Y333221K..',
    '..133Y33222Y1K..',
    '..se32222111bs..',
    '..rydeddebbaRO..',
    '..ryWYyooooRRO..',
    '..OyyoooooRRrO..',
    '.K4OooooRRRrO3K.',
    '.K4YyOOOOOOyo3K.',
    '.K444433333222K.',
    '..Kg2222111ggK..',
    '...KKKKKKKKKK...',
]
IDOL_SMALL = [
    '................',
    '..........Y.....',
    '.........YWf....',
    '......seew......',
    '......143K......',
    '.....14Y32K.....',
    '....14Y3321K....',
    '....se3221bs....',
    '....rydebbRO....',
    '....ryWoooRO....',
    '....OyoooRrO....',
    '.....OOOOOO.....',
    '................',
    '................',
    '................',
    '................',
]


def idol(small=False):
    cv = Canvas(16, 16)
    cv.grid(0, 0, IDOL_SMALL if small else IDOL_ICON, IDOL_LEG)
    return cv.image()


# --------------------------------------------------------------------------------------------------- layout
def sprite_rects():
    rects = []
    for i in range(NFRAMES):
        r, c = divmod(i, 10)
        rects.append({'name': f'{TEX}_{i}', 'rect': [float(c * CELL), float(SHEET_H - CELL - r * CELL), float(CELL), float(CELL)],
                      'pivot': [0.5, 0.5], 'pixelsPerUnit': 16.0})
    return rects


def png_box(rect):
    x, y, w, h = rect
    return int(x), int(SHEET_H - y - h), int(x + w), int(SHEET_H - y)


def build_sheet(fr, rects):
    sheet = Image.new('RGBA', (SHEET_W, SHEET_H), (0, 0, 0, 0))
    for im, r in zip(fr, rects):
        x0, y0, _, _ = png_box(r['rect'])
        sheet.alpha_composite(im, (x0, y0))
    return sheet


def validate(sheet, rects, idols):
    errs = []
    if sheet.size != (SHEET_W, SHEET_H):
        errs.append(f'sheet size {sheet.size}')
    inside = set()
    for r in rects:
        x0, y0, x1, y1 = png_box(r['rect'])
        for y in range(y0, y1):
            for x in range(x0, x1):
                inside.add((x, y))
        bb = sheet.crop((x0, y0, x1, y1)).getbbox()
        if not bb:
            errs.append(f'{r["name"]} empty')
        elif bb[3] > GROUND + 1:
            errs.append(f'{r["name"]} below ground line ({bb})')
    skins = os.path.join(ART, 'merchants', 'skins.json')
    if os.path.exists(skins):
        try:
            ref = json.load(open(skins))['merchants']['igneous']['textures'][0]['legacySprites']
            if [(s['name'], s['rect'], s['pivot']) for s in ref] != [(s['name'], s['rect'], s['pivot']) for s in rects]:
                errs.append('rects differ from skins.json SlimeMerchant layout')
        except (KeyError, IndexError):
            pass
    W, H = sheet.size
    for y in range(H):
        for x in range(W):
            p = sheet.getpixel((x, y))
            if p[3] not in (0, 255):
                errs.append(f'soft alpha at {x},{y}')
            if p[3] and (x, y) not in inside:
                errs.append(f'pixel outside cells at {x},{y}')
            if p[3] and p[:3] == (0, 0, 0):
                errs.append(f'pure black at {x},{y}')
    for gname, g in (('IDOL_ICON', IDOL_ICON), ('IDOL_SMALL', IDOL_SMALL)):
        if len(g) != 16 or any(len(r) != 16 for r in g):
            errs.append(f'{gname} grid is not 16x16')
    for gname, g, w in (('BOMB', BOMB, 7), ('GOGGLE', GOGGLE, 4)):
        if any(len(r) != w for r in g):
            errs.append(f'{gname} grid rows not {w} wide')
    for name, im in idols.items():
        if im.size != (16, 16):
            errs.append(f'{name} size {im.size}')
        if any(a not in (0, 255) for a in im.tobytes()[3::4]):
            errs.append(f'{name} soft alpha')
        bb = im.getbbox()
        if name != 'smallIcon' and (not bb or bb[3] != 16):
            errs.append(f'{name} not on bottom edge ({bb})')
    return errs


# --------------------------------------------------------------------------------------------------- preview
def scaled(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def floor_tile(s):
    t = Canvas(48, 48)
    base = [hx('#1b1d24'), hx('#23262f'), hx('#2b2e38'), hx('#33363f')]
    for y in range(48):
        for x in range(48):
            tx, ty = x % 16, y % 16
            if tx == 15 or ty == 15:
                c = hx('#14161b')
            elif tx == 0 or ty == 0:
                c = base[3]
            else:
                c = base[(x * 7 + y * 13 + (x // 16) * 5) % 3]
            t.put(x, y, c)
    return scaled(t.image(), s)


def preview(fr, idols, path):
    S = 6
    bg = (36, 38, 46, 255)
    W, H = 1500, 1080
    im = Image.new('RGBA', (W, H), bg)
    d = ImageDraw.Draw(im)
    d.text((16, 10), 'Fizzwick, the Molten Bombardier (explosives) - SlimeMerchant rig - idol: Igneous Fuse Core', fill=(240, 220, 160))
    # row 1: idle at 6x vs vanilla merchants at 6x (first idle frame, cropped to one cell)
    x = 16
    d.text((x, 34), 'Perspective self-check: idle frame at 6x next to the vanilla merchants at the same scale (1x at right)', fill=(200, 200, 210))
    entries = [('Fizzwick (ours)', fr[0])]
    vm = os.path.join(REF, 'reference', 'merchants')
    for label, f, w, h in [('Slime Merchant', 'SlimeMerchant/Slime_merchant.png', 30, 30),
                           ('Caveling Merchant', 'CavelingMerchant/Merchant.png', 64, 64),
                           ('Fishing Merchant', 'FishingMerchant/Fisher_merchant.png', 26, 40),
                           ('Void Merchant', 'VoidMerchant/voidMerchant_idle.png', 16, 35)]:
        p = os.path.join(vm, f)
        if os.path.exists(p):
            entries.append((label, Image.open(p).convert('RGBA').crop((0, 0, w, h))))
    for label, sp in entries:
        big = scaled(sp, S)
        im.alpha_composite(big, (x, 52 + (240 - big.height)))
        d.text((x, 300), label, fill=(220, 220, 220))
        x += big.width + 24
    # the same merchants at 1x
    d.text((1180, 200), '1x', fill=(200, 200, 210))
    xx = 1200
    for _, sp in entries:
        im.alpha_composite(sp, (xx, 292 - sp.height))
        xx += sp.width + 6
    # row 2: strips
    y = 330
    for title, idx in [('Idle 0-9', range(0, 10)), ('Move 10-15 / Wave 32-37', list(range(10, 16)) + list(range(32, 38))),
                       ('Stand (sleep 16-19, get up 20-31)', range(16, 32))]:
        d.text((16, y), title, fill=(200, 200, 210))
        xx = 16
        for i in idx:
            big = scaled(fr[i], 3)
            im.alpha_composite(big, (xx, y + 14))
            d.text((xx + 2, y + 14), str(i), fill=(120, 120, 130))
            xx += 92
        y += 112
    # row 3: idols 1x + 8x next to vanilla idols
    y = 680
    d.text((16, y), 'Perspective self-check: idols at 1x and 8x (ours first, then vanilla Mysterious Idol, Slime Oil, Pile of Chum, Void Idol, Lava trophy)', fill=(200, 200, 210))
    ir = os.path.join(REF, 'idol_reference')
    vids = [('Fuse Core', idols['icon']), ('small', idols['smallIcon'])]
    for label, f in [('Myst. Idol', '3930_MysteriousIdol_icon.png'), ('Slime Oil', '3931_SlimeOil_icon.png'),
                     ('Pile of Chum', '3932_PileOfChum_icon.png'), ('Void Idol', '3937_VoidMerchantIdol_icon.png'),
                     ('Lava Trophy', '5165_LavaSlimeTrophy_icon.png')]:
        p = os.path.join(ir, f)
        if os.path.exists(p):
            vids.append((label, Image.open(p).convert('RGBA')))
    xx = 16
    for label, ic in vids:
        im.alpha_composite(ic, (xx, y + 20))
        im.alpha_composite(scaled(ic, 8), (xx, y + 44))
        d.text((xx, y + 178), label, fill=(220, 220, 220))
        xx += 140
    # 1x row of all idols side by side
    xx = 16
    for _, ic in vids:
        im.alpha_composite(ic, (xx, y + 200))
        xx += 18
    # placed on a dark stone floor mock
    fl = floor_tile(4)
    fx, fy = 1080, 690
    im.alpha_composite(fl, (fx, fy))
    shadow = Image.new('RGBA', (16 * 4, 16 * 4), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((12, 52, 52, 62), fill=(10, 10, 14, 255))
    im.alpha_composite(shadow, (fx + 64, fy + 64 + 4))
    im.alpha_composite(scaled(idols['placed'], 4), (fx + 64, fy + 64))
    d.text((fx, fy + 200), 'placed on stone floor (4x)', fill=(220, 220, 220))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.convert('RGB').save(path)


def main():
    rects = sprite_rects()
    fr = frames()
    assert len(fr) == NFRAMES
    sheet = build_sheet(fr, rects)
    icon = idol()
    idols = {'icon': icon, 'smallIcon': idol(small=True), 'placed': icon.copy()}
    errs = validate(sheet, rects, idols)
    if errs:
        print('VALIDATION FAILED:')
        for e in errs[:40]:
            print('  ', e)
        sys.exit(1)
    mdir = os.path.join(ART, 'merchants', KEY)
    os.makedirs(mdir, exist_ok=True)
    out_name = f'{KEY}__{TEX}.png'
    sheet.save(os.path.join(mdir, out_name))
    json.dump({'key': KEY, 'rig': 'SlimeMerchantEntity', 'kind': 'legacy', 'textures': [{
        'output': f'{KEY}/{out_name}', 'replacesTexture': TEX, 'size': [SHEET_W, SHEET_H],
        'legacySprites': rects, 'renderer': '/SlimeMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SR'}]},
        open(os.path.join(mdir, 'sheets.json'), 'w'), indent=1)
    idir = os.path.join(ART, 'idols')
    for k, v in idols.items():
        v.save(os.path.join(idir, f'{KEY}_{k}.png'))
    preview(fr, idols, os.path.join(ART, 'previews', f'{KEY}.png'))
    print('ok: sheet', sheet.size, 'frames', len(fr), '-> ', mdir)


if __name__ == '__main__':
    main()
