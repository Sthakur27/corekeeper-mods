"""Boss Merchants: art for the `seeds` merchant (Tilly the Seedgrub) and its idol (Brood Seedpot).

    python -I BossMerchants/Unity/make_seeds_art.py

Character: a plump larva from the Hive Mother's brood that never pupated and took up gardening instead. Straw sun
hat with a Glow Tulip tucked in the band, a wicker seed basket on its back overflowing with sprouts and a Bloat Oat
ear, a tin watering can hanging from a strap at the hip, stubby larva legs. Greeting = tips its hat.

Rig: CavelingMerchant (legacy SpriteRenderer + Animator). Sheets keep the vanilla size, 64x64 cells and pivots:
  Merchant        640x192  frames 0-9 idle (MerchantIdleR), 10-17 move (MerchantMoveR), 18-23 wave (MerchantWaveR)
  Merchant_Light  640x192  same frames; glow overlay = the Glow Tulip blooms (hat + basket)
  Merchant_Eye    640x128  frames 0-9 idle, 10-17 move (wave reuses Eye_10); glow overlay = eye glints
Ground line = cell row 53 (top-down), as vanilla (Merchant_* bbox bottom 54). Everything is drawn from code and
hand-authored grids; no vanilla pixels are copied. Colours come from the game's shared palette (larva, hive,
straw, wicker, tin and leaf ramps found in vanilla icons/sprites).

Idol (Art/idols/seeds_*.png): a terracotta seed pot whose rim is overgrown with pink Hive Mother brood flesh and
larva eggs, with a seedling sprouting from it. 16x16 icon, ~10x11 smallIcon centred on 16x16, placed = icon pixels.

Outputs (only this designer's paths):
  Art/merchants/seeds/seeds__Merchant.png, seeds__Merchant_Light.png, seeds__Merchant_Eye.png, sheets.json
  Art/idols/seeds_icon.png, seeds_smallIcon.png, seeds_placed.png
  Art/previews/seeds.png   (review sheet; reads vanilla references from build/bossmerchants for comparison only)
Validation (exit 1 on failure): sheet sizes = rig sizes, opaque pixels only inside the rig's sprite rects, hard
alpha (0 or 255 only), idol sizes 16x16, every frame non-empty and standing on the vanilla ground row.
"""
import json
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
ART = HERE / 'Art'
OUT_M = ART / 'merchants' / 'seeds'
OUT_I = ART / 'idols'
OUT_P = ART / 'previews'
REF = ROOT / 'build' / 'bossmerchants'
KEY = 'seeds'
CELL = 64
GROUND = 53  # last opaque row (top-down) inside a cell, as on the vanilla Caveling Merchant


def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


# ------------------------------------------------------------------------------------------- palette (vanilla)
BODY = [hx(c) for c in ('bb803f', 'd69776', 'eebebc', 'f9efc3')]          # larva_idle
BODY_OUT = hx('5e2a28')
BODY_SEAM = hx('bb803f')
HEAD = [hx(c) for c in ('23281e', '23281e', '4e3c6d', 'a555a4')]  # larva head capsule + mandibles
HEAD_OUT = hx('0b1917')
COLLAR = [hx('c5460a'), hx('da6210')]
LEG = [hx('23281e'), hx('4e3c6d'), hx('4e3c6d')]
CHEEK = hx('c76369')
EYE = hx('23281e')
STRAW = [hx(c) for c in ('915b26', 'b58047', 'e2a12c', 'f2df3a')]         # FarmerHelm
STRAW_OUT = hx('614822')
BAND = [hx('824223'), hx('b43221'), hx('c5460a')]
WICKER = [hx(c) for c in ('614822', '915b26', 'b58047', 'e2a12c')]        # Wood icon
WICKER_OUT = hx('39341f')
TIN = [hx(c) for c in ('47677e', '677fae', 'a1aedd', 'd6dbfa')]           # LargeWaterCan / Bucket
TIN_OUT = hx('2a4e4a')
LEAF = {'o': hx('0f5630'), 'V': hx('0a6d27'), 'v': hx('239029'), 'G': hx('5ca537'), 'g': hx('a3ce4a')}
TULIP = {'U': hx('143eab'), 'T': hx('165dd9'), 't': hx('1885d8'), 'c': hx('19bdc6'), 'C': hx('4ad8ae'),
         'X': hx('94f7dd')}
OAT = {'n': hx('614822'), 'h': hx('b58047'), 'y': hx('e2a12c'), 'Y': hx('f2df3a'), 'W': hx('f9efc3')}
BERRY = {'R': hx('5e2a28'), 'r': hx('b43221'), 'e': hx('c76369')}
GLOW = {'1': hx('4ad8ae'), '2': hx('94f7dd'), '3': hx('ffffff')}
GRID_PAL = {**LEAF, **TULIP, **OAT, **BERRY}
LIT_OUT_INIT = {'5e2a28': '824223', '614822': '915b26', '39341f': '614822', '0b1917': '23281e', '2a4e4a': '47677e'}


# ------------------------------------------------------------------------------------------- drawing helpers
class Layer:
    def __init__(self, w=CELL, h=CELL):
        self.w, self.h = w, h
        self.px = {}

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[(x, y)] = c

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        for (x, y), c in self.px.items():
            im.putpixel((x, y), c + (255,))
        return im


LIGHT = (-0.38, -0.80, 0.46)  # camera looks down at ~3/4: up-facing planes catch the most light


def ellipse_mask(cx, cy, rx, ry):
    m = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1.0:
                m.add((x, y))
    return m


def shade_ellipse(L, cx, cy, rx, ry, ramp, cuts=(-0.05, 0.42, 0.78), clip=None, flat=1.0):
    """Fill an ellipse with a top-left lit ramp (ramp dark->light, len(cuts)+1 shades). Returns the mask."""
    m = ellipse_mask(cx, cy, rx, ry)
    if clip:
        m = {p for p in m if clip(*p)}
    for (x, y) in m:
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny)) * flat
        n = math.sqrt(nx * nx + ny * ny + nz * nz) or 1
        i = (nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]) / n
        k = sum(1 for c in cuts if i > c)
        L.put(x, y, ramp[min(k, len(ramp) - 1)])
    return m


LIT_OUT = {hx(a): hx(b) for a, b in LIT_OUT_INIT.items()}  # dark outline colour -> lighter tinted colour used on the lit (top/left) edges


def outline(L, mask, colour, only=None):
    lit = LIT_OUT.get(colour, colour)
    done = {}
    for (x, y) in mask:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in mask and (only is None or only(*q)):
                # an outline pixel lit from the top-left: shape lies below/right of it on both axes
                litq = (q[0] + 1, q[1]) in mask and (q[0], q[1] + 1) in mask and (q[0] - 1, q[1]) not in mask                     and (q[0], q[1] - 1) not in mask
                done[q] = lit if litq else colour
    for q, c in done.items():
        L.put(q[0], q[1], c)


def grid(L, rows, ox, oy, pal, sway=None):
    """Paint a hand-authored grid. sway(row) -> dx for that row (plants)."""
    for j, row in enumerate(rows):
        dx = sway(j) if sway else 0
        for i, ch in enumerate(row):
            if ch != '.':
                L.put(ox + i + dx, oy + j, pal[ch])


# ------------------------------------------------------------------------------------------- character parts
# Plants in the back basket (origin = top-left at basket x-1, y-12). Rows 0-6 sway with the idle breath.
PLANTS = [
    '...UU.............',
    '..UXcU......oo....',
    '..UctU.....oYyo...',
    '..UtTU....oyYho...',
    '...UU.....ohyyo...',
    '...ov..oo.oyhyo...',
    '...ov.oGgo.ohho...',
    '..oGvooGvo.oho.oo.',
    '.oGgvooGvVo.ho.oGo',
    '.ogvvVoGvoroo.ovvo',
    '..ovvVoGvorero.ovo',
    '..oovVovooRrRovVo.',
    '...ooVoVoo.RRoVo..',
]
# Glow tulip tucked in the hat band (origin set in draw_frame).
HAT_TULIP = [
    '.UU..',
    'UXcU.',
    'UctTU',
    'UtTTU',
    '.UUo.',
    '..ov.',
]
HAT_TULIP_GLOW = [(1, 1, '3'), (2, 1, '2'), (1, 2, '2'), (2, 2, '1'), (3, 2, '1'), (2, 3, '1')]
BASKET_TULIP_GLOW = [(3, 1, '3'), (4, 1, '2'), (3, 2, '2'), (4, 2, '1'), (4, 3, '1')]
# Tin watering can hanging at the hip (spout to the right), 9x7. '#'=outline.
CAN = [  # seen from above: oval top with the dark opening
    '.#####...',
    '#mzzzs#.#',
    '#mMMss##s',
    '#MMsssS#.',
    '#Msss SS#'.replace(' ', 's'),
    '.#SSSS#..',
    '..####...',
]
CAN_PAL = {'z': hx('23281e'), '#': TIN_OUT, 'm': TIN[3], 'M': TIN[2], 's': TIN[1], 'S': TIN[0]}


def draw_body(L, P):
    """Larva body: three stacked segments, feet, seams and the dark silhouette outline."""
    b = P['bob']
    lean = P.get('lean', 0)
    segs = [  # bottom -> top
        (31 + lean * 0.3, 47.9, 8.8 + 0.4 * b, 4.0 - 0.25 * b),
        (31.2 + lean * 0.5, 44.6 + b * 0.6, 8.3, 3.5),
        (31.5 + lean * 0.8, 41.5 + b * 0.9, 7.6, 3.3),
        (31.9 + lean, 38.8 + b, 6.8, 3.1),
    ]
    body = Layer()
    masks = []
    # draw top segment first so lower (nearer) segments overlap it
    for (cx, cy, rx, ry) in reversed(segs):
        m = shade_ellipse(body, cx, cy, rx, ry, BODY)
        masks.append((m, cy))
    union = set().union(*[m for m, _ in masks])
    # seams: top edge of each lower segment drawn inside the segment above
    for m, cy in masks[1:]:
        for (x, y) in m:
            if y < cy and (x, y - 1) not in m and (x, y - 1) in union:
                body.put(x, y - 1, BODY_SEAM)
    # belly sheen: the segments' front-lower quarter gets one lighter row
    feet = []
    for fx, dy in ((P['footB'], P['footBy']), (P['footF'], P['footFy'])):
        fm = ellipse_mask(fx, 51.9 + dy, 2.3, 1.5)
        feet.append(fm)
    feetL = Layer()
    for fm in feet:
        for (x, y) in fm:
            feetL.put(x, y, LEG[1] if y < 52 else LEG[0])
        outline(feetL, fm, HEAD_OUT)
    for p, c in feetL.px.items():
        L.put(*p, c)
    allm = union
    top = segs[-1]
    for (x, y) in list(body.px):
        if int(top[1] - top[3]) + 1 <= y <= int(top[1] - top[3]) + 2:
            body.put(x, y, BODY[1] if body.px[(x, y)] in (BODY[2], BODY[3]) else BODY[0])
    for (x, y), c in list(body.px.items()):  # underside facing the floor
        if (x, y + 1) not in union and y > segs[0][1]:
            body.put(x, y, BODY[0])
    for p, c in body.px.items():
        L.put(*p, c)
    outline(L, allm, BODY_OUT)
    return segs


def draw_basket(L, P):
    """Wicker seed basket on the back (left), seen from above: oval rim (top plane, brightest) around a dark
    opening the plants grow out of, front face in weave bands darkening downward, curved oval bottom."""
    dy = P['bob'] + P.get('basket', 0)
    cx, x0, x1 = 19.5, 14, 25
    rim_cy, bot_cy = 31.5 + dy, 45.0 + dy
    rx, ry = 6.3, 2.6
    face = set()
    for y in range(int(rim_cy), int(bot_cy) + 3):
        for x in range(x0, x1 + 1):
            if y > bot_cy:  # curved bottom edge
                nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - bot_cy) / ry
                if nx * nx + ny * ny > 1:
                    continue
            face.add((x, y))
    rim = ellipse_mask(cx, rim_cy, rx, ry)
    hole = ellipse_mask(cx + 0.2, rim_cy + 0.2, rx - 1.6, ry - 1.0)
    for (x, y) in face:
        r = y - int(rim_cy)
        band = (r // 3) % 2
        k = 2 if band == 0 else 1
        if (x - x0) <= 1 and band == 0:
            k = 3
        if (x - x0) >= 8:
            k -= 1
        if y >= bot_cy - 1:
            k -= 1  # darker underside
        if x - x0 in (4, 8) and r % 3 == 2:
            k -= 1
        L.put(x, y, WICKER[max(0, min(3, k))])
    for (x, y) in rim:  # rim top plane
        L.put(x, y, WICKER[3] if x < cx + 1 else WICKER[2])
    for (x, y) in hole:  # opening: soil, darker at the back
        L.put(x, y, WICKER_OUT if y <= rim_cy else WICKER[0])
    outline(L, face | rim, WICKER_OUT)
    # plants grow out of the opening; then the front lip of the rim covers their stems
    po = (x0 - 2, int(rim_cy) - 12)
    sw = P.get('sway', 0)
    grid(L, PLANTS[:11], po[0], po[1], GRID_PAL, sway=lambda j: sw if j <= 6 else 0)
    for (x, y) in rim:
        if y >= rim_cy and (x, y) not in hole:
            L.put(x, y, WICKER[3] if x < cx else WICKER[2])
    return po, sw


def draw_head(L, P):
    """Dark glossy larva head capsule with cream bead eyes, purple mandibles and an orange collar."""
    b = P['bob']
    lean = P.get('lean', 0)
    cx, cy = 33.6 + lean, 32.2 + b
    # orange collar between head and body
    cm = shade_ellipse(L, cx - 0.8, cy + 3.9, 5.2, 1.6, [COLLAR[0], COLLAR[0], COLLAR[1], COLLAR[1]])
    outline(L, cm, BODY_OUT, only=lambda x, y: y > cy + 3)
    m = shade_ellipse(L, cx, cy, 5.8, 4.9, HEAD, cuts=(-0.2, 0.55, 0.9))
    outline(L, m, HEAD_OUT)
    ey = int(cy) + 1  # face sits low on the head: we look down on it
    eyes = [(35 + lean, ey), (38 + lean, ey)]
    if P.get('happy'):
        for ex, _ in eyes:
            L.put(int(ex), ey + 1, BODY[3])
            L.put(int(ex) - 1, ey + 2, BODY[2])
            L.put(int(ex) + 1, ey + 2, BODY[2])
    else:
        for ex, _ in eyes:
            L.put(int(ex), ey, BODY[3])
            L.put(int(ex), ey + 1, BODY[2])
    # mandibles at the front-bottom
    mx, my = int(cx + 3), int(cy + 3.6)
    for (x, y, c) in ((mx, my, HEAD[3]), (mx + 1, my, HEAD[2]), (mx + 2, my + 1, HEAD[2]), (mx + 1, my + 1, HEAD_OUT),
                      (mx - 2, my + 1, HEAD[3]), (mx - 1, my + 1, HEAD[2]), (mx, my + 2, HEAD_OUT), (mx + 2, my + 2, HEAD_OUT),
                      (mx + 3, my + 1, HEAD_OUT), (mx + 2, my, HEAD_OUT)):
        L.put(x, y, c)
    return (cx, cy), eyes


def draw_hat(L, P):
    """Straw sun hat seen from above: wide oval brim (its top surface is the brightest plane), a short crown whose
    top is a bright oval, a dark front edge showing the brim's thickness and a shadow cast on the head below."""
    b = P['bob']
    lean = P.get('lean', 0)
    lift = P.get('lift', 0)
    tilt = P.get('tilt', 0)
    dy = b + lift
    bx, by = 33.0 + lean, 25.8 + dy
    ccx = 32.8 + lean + tilt * 0.5
    hat = Layer()
    # shadow of the brim on the head (occlusion)
    y = int(by + 4.3)
    for x in range(int(bx - 7), int(bx + 8)):
        if L.px.get((x, y)) in (HEAD[2], HEAD[3]):
            L.put(x, y, HEAD[0])
    brim = ellipse_mask(bx, by, 12.4, 4.3)
    for (x, y) in brim:
        nx, ny = (x + 0.5 - bx) / 12.4, (y + 0.5 - by) / 4.3
        k = 3 if (nx < 0.15 and ny < 0.35) else 2
        if nx > 0.55 or ny > 0.55:
            k = 2 if k == 3 else 1
        hat.put(x, y, STRAW[k])
    inner = ellipse_mask(bx, by - 0.2, 9.6, 3.0)
    for (x, y) in inner:  # one stitched seam following the oval
        if (x, y + 1) not in inner and abs(x - bx) > 5:
            hat.put(x, y, STRAW[1])
    for (x, y) in brim:  # brim thickness: the front edge row is the darker vertical face
        if (x, y + 1) not in brim:
            hat.put(x, y, STRAW[1] if x < bx + 4 else STRAW[0])
    top_cy = by - 5.0
    side = set()
    for yy in range(int(top_cy), int(by) + 1):
        for x in range(int(ccx - 6), int(ccx + 7)):
            if abs((x + 0.5 - ccx) / 5.4) <= 1:
                side.add((x, yy))
    for (x, yy) in side:
        nx = (x + 0.5 - ccx) / 5.4
        k = 2 if nx < -0.2 else (1 if nx < 0.5 else 0)
        if int(by) - 1 <= yy <= int(by):
            hat.put(x, yy, BAND[k])
        else:
            hat.put(x, yy, STRAW[k + 1] if k < 2 else STRAW[2])
    ctop = ellipse_mask(ccx, top_cy, 5.4, 2.1)
    for (x, yy) in ctop:
        hat.put(x, yy, STRAW[3] if (x + 0.5 - ccx) / 5.4 < 0.3 else STRAW[2])
    for (x, yy) in ctop:  # crown dent
        if yy == int(top_cy) and abs(x + 0.5 - ccx) < 2:
            hat.put(x, yy, STRAW[1])
    full = brim | side | ctop
    for p, c in hat.px.items():
        L.put(*p, c)
    outline(L, full, STRAW_OUT)
    to = (int(ccx + 1.5), int(by) - 6)  # glow tulip tucked in the band, front-right of the crown
    grid(L, HAT_TULIP, to[0], to[1], GRID_PAL)
    return to


def draw_strap_and_can(L, P):
    b = P['bob']
    lean = P.get('lean', 0)
    # strap from the left shoulder across the chest down to the right hip
    pts = [(26, 35), (27, 36), (28, 37), (29, 38), (30, 39), (31, 40), (32, 41), (33, 42), (34, 43), (35, 44), (36, 44)]
    for i, (x, y) in enumerate(pts):
        yy = y + (b if y < 42 else 0)
        L.put(x + int(lean * 0.6), yy, BAND[1])
        L.put(x + int(lean * 0.6), yy + 1, BAND[0])
    swing = P.get('canSwing', 0)
    grid(L, CAN, 35 + swing + int(lean * 0.4), 44, CAN_PAL)


def draw_arm(L, P):
    ax, ay = P['arm']
    m = shade_ellipse(L, ax, ay, 2.0, 1.5, LEG)
    outline(L, m, HEAD_OUT)
    L.put(int(ax + 2.0), int(ay), HEAD[3])  # claw tip


def draw_frame(P):
    main, light, eye = Layer(), Layer(), Layer()
    po, sw = draw_basket(main, P)
    draw_body(main, P)
    draw_strap_and_can(main, P)
    _, eyes = draw_head(main, P)
    to = draw_hat(main, P)
    draw_arm(main, P)
    # glow overlays
    pulse = P.get('pulse', 0)
    for (x, y, k) in HAT_TULIP_GLOW[:4 + pulse]:
        light.put(to[0] + x, to[1] + y, GLOW[k])
    for (x, y, k) in BASKET_TULIP_GLOW[:3 + pulse]:
        light.put(po[0] + x + sw, po[1] + y, GLOW[k])
    if not P.get('happy'):
        for (x, y) in eyes:
            eye.put(int(x), y, (255, 255, 255))
    return main, light, eye


# ------------------------------------------------------------------------------------------- animation params
def frames():
    out = {'idle': [], 'move': [], 'wave': []}
    breath = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0]
    sway = [0, 0, 1, 1, 1, 0, 0, -1, -1, 0]
    pulse = [0, 0, 1, 1, 2, 2, 1, 1, 0, 0]
    for i in range(10):
        b = breath[i]
        out['idle'].append(dict(bob=b, sway=sway[i], pulse=pulse[i], footB=26.5, footBy=0, footF=35.5, footFy=0,
                                arm=(38.6, 40.4 + b), canSwing=0))
    bob = [0, 1, 1, 0, 0, 1, 1, 0]
    ff = [1, 1, 0, -1, -1, -1, 0, 1]
    lift = [0, 0, 0, -1, 0, 0, 0, 0]
    swayM = [0, -1, -1, 0, 1, 1, 0, 0]
    for i in range(8):
        f = ff[i]
        out['move'].append(dict(bob=bob[i], sway=swayM[i], pulse=1, lean=1 if i in (1, 2, 5, 6) else 0,
                                footB=26.5 - f, footBy=-1 if f > 0 and i in (0, 7) else 0,
                                footF=35.5 + f, footFy=-1 if f < 0 and i in (3, 4) else 0,
                                arm=(38.6 + (1 if i in (1, 2, 5, 6) else 0), 40.4 + bob[i] - (1 if ff[i] > 0 else 0)),
                                canSwing=-1 if i in (2, 3, 4) else 0, basket=-bob[i]))
    wave = [  # tip the hat: (arm, lift, happy)
        ((39.6, 37.0), 0, False),
        ((40.8, 31.0), 0, False),
        ((41.4, 26.6), -2, True),
        ((41.6, 25.4), -3, True),
        ((41.4, 26.6), -2, True),
        ((39.8, 35.5), 0, False),
    ]
    for (arm, lft, happy) in wave:
        out['wave'].append(dict(bob=0, sway=0, pulse=2, footB=26.5, footBy=0, footF=35.5, footFy=0, arm=arm,
                                lift=lft, happy=happy, tilt=1 if lft else 0))
    return out


# ------------------------------------------------------------------------------------------- sheets
def load_rig():
    ref = json.loads((REF / 'reference' / 'reference.json').read_text())['merchants']['CavelingMerchant']
    return ref


RIG_SHEETS = {'Merchant': (640, 192), 'Merchant_Light': (640, 192), 'Merchant_Eye': (640, 128)}
# fallback layout (used when the extracted reference is missing): vanilla rects, top-down rows of 10 cells
def vanilla_rects(sheet):
    n = 18 if sheet == 'Merchant_Eye' else 24
    w, h = RIG_SHEETS[sheet]
    rows = h // CELL
    rects = []
    for i in range(n):
        # rows: frames 0-9 (idle), 10-17 (move), 18-23 (wave) each start a new row of the sheet
        r, c = (0, i) if i < 10 else ((1, i - 10) if i < 18 else (2, i - 18))
        rects.append({'name': f'{sheet}_{i}', 'rect': [float(c * CELL), float((rows - 1 - r) * CELL), 64.0, 64.0],
                      'pivot': [0.5, 0.5], 'pixelsPerUnit': 16.0})
    return rects


def build_sheets():
    fr = frames()
    seq = fr['idle'] + fr['move'] + fr['wave']
    drawn = [draw_frame(p) for p in seq]
    sheets = {}
    for name, (w, h) in RIG_SHEETS.items():
        img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        rects = vanilla_rects(name)
        for i, s in enumerate(rects):
            x, y, rw, rh = map(int, s['rect'])
            layer = {'Merchant': 0, 'Merchant_Light': 1, 'Merchant_Eye': 2}[name]
            src = drawn[i][layer].image()
            img.paste(src, (x, h - y - rh))
        sheets[name] = (img, rects)
    return sheets, drawn


# ------------------------------------------------------------------------------------------- idol
IDOL_PAL = {
    'o': hx('5e2a28'),  # clay / flesh outline
    '1': hx('824223'), '2': hx('c16436'), '3': hx('e88b69'), '4': hx('ecc1bf'),   # terracotta (ClayPot)
    's': hx('39341f'), 'S': hx('614822'),                                        # soil
    'a': hx('b43221'), 'b': hx('c76369'), 'c': hx('de8e9a'), 'd': hx('ecc1bf'),   # hive flesh (hiveGrowth)
    'W': hx('f9efc3'), 'w': hx('eebebc'), 'p': hx('d69776'),                     # larva eggs
    'q': hx('0f5630'), 'V': hx('0a6d27'), 'v': hx('239029'), 'G': hx('5ca537'), 'g': hx('a3ce4a'),  # sprout
}
IDOL_ICON = [  # seen from above: oval rim (brightest top plane) around the soil, curved oval bottom
    '.......qq.......',
    '..qqq.qgGq.qqq..',
    '.qgGGqqGvqqGgGq.',
    '.qGvvVqvVqVvvGq.',
    '..qVVVqvVqVVVq..',
    '...qqqovVoqqq...',
    '..ooo4dvVcdooo..',
    '.o34sssvVsss3ao.',
    'o34dSSSqqSSSWpao',
    'o3dcSSSSSSSwpb1o',
    'o43dccb33221ba1o',
    '.o33bab222211ao.',
    '.o3322222a1111o.',
    '..o322222a111o..',
    '..o1111111111o..',
    '...oooooooooo...',
]
IDOL_SMALL = [
    '......qq........',
    '...qq.qGq.qq....',
    '...qGqvGqqGq....',
    '....qVqVqVq.....',
    '...oo4cVdooo....',
    '..o3sSqSswao....',
    '..o4dcSScb1o....',
    '...o32222a1o....',
    '...o3222211o....',
    '....oooooo......',
]


def build_idol():
    icon = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(IDOL_ICON):
        assert len(row) == 16, (y, row)
        for x, ch in enumerate(row):
            if ch != '.':
                icon.putpixel((x, y), IDOL_PAL[ch] + (255,))
    small = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    h = len(IDOL_SMALL)
    oy = (16 - h) // 2 + 1
    for y, row in enumerate(IDOL_SMALL):
        for x, ch in enumerate(row):
            if ch != '.':
                small.putpixel((x + 1, y + oy), IDOL_PAL[ch] + (255,))
    # centre the small icon horizontally on its bbox
    bb = small.getbbox()
    cx = (bb[0] + bb[2]) / 2
    shift = int(round(8 - cx))
    small2 = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    small2.paste(small, (shift, 0))
    return icon, small2, icon.copy()


# ------------------------------------------------------------------------------------------- validation
def validate(sheets, idol):
    errs = []
    for name, (img, rects) in sheets.items():
        if img.size != RIG_SHEETS[name]:
            errs.append(f'{name}: size {img.size} != {RIG_SHEETS[name]}')
        w, h = img.size
        inside = set()
        for s in rects:
            x, y, rw, rh = map(int, s['rect'])
            top = h - y - rh
            for yy in range(top, top + rh):
                for xx in range(x, x + rw):
                    inside.add((xx, yy))
        a = img.getchannel('A')
        for yy in range(h):
            for xx in range(w):
                v = a.getpixel((xx, yy))
                if v not in (0, 255):
                    errs.append(f'{name}: soft alpha at {(xx, yy)}')
                if v and (xx, yy) not in inside:
                    errs.append(f'{name}: pixel outside cells at {(xx, yy)}')
        if name == 'Merchant':
            for s in rects:
                x, y, rw, rh = map(int, s['rect'])
                c = img.crop((x, h - y - rh, x + rw, h - y))
                bb = c.getbbox()
                if not bb:
                    errs.append(f'{s["name"]}: empty frame')
                elif bb[3] - 1 != GROUND:
                    errs.append(f'{s["name"]}: ground row {bb[3] - 1} != {GROUND}')
                elif bb[0] < 1 or bb[2] > rw - 1 or bb[1] < 1:
                    errs.append(f'{s["name"]}: touches cell edge {bb}')
    # the rects must match the extracted vanilla rig when it is available
    rp = REF / 'reference' / 'reference.json'
    if rp.exists():
        van = load_rig()['sheets']
        for name, (_, rects) in sheets.items():
            vr = [(s['name'], s['rect'], s['pivot']) for s in van[name]]
            mr = [(s['name'], s['rect'], s['pivot']) for s in rects]
            if vr != mr:
                errs.append(f'{name}: rects differ from the vanilla rig')
    for nm, im in zip(('icon', 'smallIcon', 'placed'), idol):
        if im.size != (16, 16):
            errs.append(f'idol {nm}: size {im.size}')
        if any(v not in (0, 255) for v in im.getchannel("A").get_flattened_data()):
            errs.append(f'idol {nm}: soft alpha')
    return errs


# ------------------------------------------------------------------------------------------- preview
def preview(sheets, drawn, idol):
    bg = (34, 32, 40, 255)
    W = 1500
    canvas = Image.new('RGBA', (W, 1700), bg)
    d = ImageDraw.Draw(canvas)
    y = 10
    d.text((12, y), 'Tilly the Seedgrub (key seeds, CavelingMerchant rig) - idle frame 0 at 6x next to vanilla merchants at 6x. Perspective self-check: top-down 3/4 view (oval hat brim + crown top, basket rim + opening, pot rim + soil, top planes brightest)',
           fill=(235, 235, 235, 255))
    y += 18

    def comp(i):
        m, l, e = drawn[i]
        im = m.image()
        for lay in (l, e):
            o = lay.image()
            im.alpha_composite(o)
        return im

    def crop_bottom(im, w, h):
        bb = im.getbbox()
        if not bb:
            return im
        cx = (bb[0] + bb[2]) // 2
        x0 = max(0, cx - w // 2)
        return im.crop((x0, bb[3] - h, x0 + w, bb[3]))

    sc = 6
    x = 12
    row_h = 52 * sc
    items = [('Tilly the Seedgrub (new)', crop_bottom(comp(0), 40, 52))]
    vb = REF / 'reference' / 'merchants'
    try:
        van = load_rig()
        allref = json.loads((REF / 'reference' / 'reference.json').read_text())['merchants']
        for base, sheet in (('CavelingMerchant', 'Merchant'), ('SlimeMerchant', 'Slime_merchant'),
                            ('FishingMerchant', 'Fisher_merchant')):
            img = Image.open(vb / base / f'{sheet}.png').convert('RGBA')
            s = allref[base]['sheets'][sheet][0]
            rx, ry, rw, rh = map(int, s['rect'])
            c = img.crop((rx, img.height - ry - rh, rx + rw, img.height - ry))
            if base == 'CavelingMerchant':
                for ov in ('Merchant_Light', 'Merchant_Eye'):
                    o = Image.open(vb / base / f'{ov}.png').convert('RGBA')
                    so = allref[base]['sheets'][ov][0]
                    ox, oy, ow, oh = map(int, so['rect'])
                    c.alpha_composite(o.crop((ox, o.height - oy - oh, ox + ow, o.height - oy)))
            items.append((base + ' (vanilla)', crop_bottom(c, 40, 52)))
        vi = Image.open(vb / 'VoidMerchant' / 'voidMerchant_idle.png').convert('RGBA').crop((0, 0, 16, 35))
        items.append(('VoidMerchant (vanilla)', vi))
    except Exception as ex:  # reference not extracted: preview just the new art
        print('preview: vanilla reference unavailable:', ex)
    for label, im in items:
        big = im.resize((im.width * sc, im.height * sc), Image.NEAREST)
        canvas.alpha_composite(big, (x, y + row_h - big.height))
        d.line((x, y + row_h, x + big.width, y + row_h), fill=(90, 85, 100, 255))
        d.text((x, y + row_h + 4), label, fill=(220, 220, 220, 255))
        x += big.width + 24
    y += row_h + 26
    d.text((12, y), 'same at 1x:', fill=(220, 220, 220, 255))
    x = 100
    for label, im in items:
        canvas.alpha_composite(im, (x, y + 52 - im.height))
        x += im.width + 12
    y += 62

    # strips
    for label, rng in (('idle 0-9 (MerchantIdleR)', range(0, 10)), ('move 10-17 (MerchantMoveR)', range(10, 18)),
                       ('wave 18-23 (MerchantWaveR, tips its hat)', range(18, 24))):
        d.text((12, y), label + '  (3x, glow overlays composited)', fill=(220, 220, 220, 255))
        y += 14
        x = 12
        for i in rng:
            im = crop_bottom(comp(i), 40, 46)
            big = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
            canvas.alpha_composite(big, (x, y))
            x += big.width + 4
        y += 46 * 3 + 10

    # idol vs vanilla
    d.text((12, y), 'Idol: Brood Seedpot - 1x and 8x beside the vanilla merchant idols (icon, smallIcon)', fill=(235, 235, 235, 255))
    y += 16
    x = 12
    ir = REF / 'idol_reference'
    idols = [('Brood Seedpot (new)', idol[0], idol[1])]
    for f, nm in (('3930_MysteriousIdol', 'Mysterious Idol'), ('3931_SlimeOil', 'Slime Oil'),
                  ('3932_PileOfChum', 'Pile of Chum'), ('3937_VoidMerchantIdol', 'Void Merchant Idol')):
        try:
            idols.append((nm, Image.open(ir / f'{f}_icon.png').convert('RGBA'),
                          Image.open(ir / f'{f}_smallIcon.png').convert('RGBA')))
        except Exception:
            pass
    for nm, ic, sm in idols:
        canvas.alpha_composite(ic, (x, y))
        canvas.alpha_composite(sm, (x + 20, y))
        canvas.alpha_composite(ic.resize((128, 128), Image.NEAREST), (x, y + 22))
        canvas.alpha_composite(sm.resize((64, 64), Image.NEAREST), (x + 132, y + 22))
        d.text((x, y + 154), nm, fill=(220, 220, 220, 255))
        x += 210
    y += 176

    # placed on a stone floor mock (dark stone tiles, 3x3 tiles, 6x)
    d.text((12, y), 'Placed on a dark stone floor (6x), shadow like vanilla (icon pixels tinted black, offset down)',
           fill=(235, 235, 235, 255))
    y += 16
    floor = Image.new('RGBA', (48, 40), (0, 0, 0, 255))
    fd = ImageDraw.Draw(floor)
    stone = [(42, 44, 56), (50, 52, 66), (36, 38, 48), (58, 60, 76)]
    for ty in range(0, 40, 8):
        for tx in range(0, 64, 16):
            o = 8 if (ty // 8) % 2 else 0
            fd.rectangle((tx - o, ty, tx - o + 15, ty + 7), fill=stone[((tx + ty) // 8) % 3])
            fd.line((tx - o, ty, tx - o + 15, ty), fill=stone[3])
            fd.line((tx - o, ty + 7, tx - o + 15, ty + 7), fill=(28, 28, 36))
    placed = idol[2]
    shadow = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for yy in range(16):
        for xx in range(16):
            if placed.getpixel((xx, yy))[3]:
                shadow.putpixel((xx, yy), (0, 0, 0, 110))
    sh = shadow.resize((16, 6))
    floor.alpha_composite(sh, (16, 28))
    floor.alpha_composite(placed, (16, 16))
    canvas.alpha_composite(floor.resize((48 * 6, 40 * 6), Image.NEAREST), (12, y))
    # also the merchant standing next to it at 3x on a wider floor
    fl2 = Image.new('RGBA', (96, 64), (0, 0, 0, 255))
    f2 = ImageDraw.Draw(fl2)
    for ty in range(0, 64, 8):
        for tx in range(0, 128, 16):
            o = 8 if (ty // 8) % 2 else 0
            f2.rectangle((tx - o, ty, tx - o + 15, ty + 7), fill=stone[((tx + ty) // 8) % 3])
            f2.line((tx - o, ty, tx - o + 15, ty), fill=stone[3])
            f2.line((tx - o, ty + 7, tx - o + 15, ty + 7), fill=(28, 28, 36))
    fl2.alpha_composite(placed, (62, 40))
    t = crop_bottom(comp(0), 40, 52)
    fl2.alpha_composite(t, (14, 4))
    canvas.alpha_composite(fl2.resize((96 * 3, 64 * 3), Image.NEAREST), (330, y))
    y += 40 * 6 + 10
    canvas = canvas.crop((0, 0, W, y))
    OUT_P.mkdir(parents=True, exist_ok=True)
    canvas.convert('RGB').save(OUT_P / f'{KEY}.png')


# ------------------------------------------------------------------------------------------- main
def main():
    sheets, drawn = build_sheets()
    idol = build_idol()
    errs = validate(sheets, idol)
    if errs:
        for e in errs[:40]:
            print('FAIL', e)
        print(f'{len(errs)} validation error(s)')
        return 1
    OUT_M.mkdir(parents=True, exist_ok=True)
    OUT_I.mkdir(parents=True, exist_ok=True)
    entries = []
    renderers = {'Merchant': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SR',
                 'Merchant_Light': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SRLight',
                 'Merchant_Eye': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SREye'}
    for name, (img, rects) in sheets.items():
        fn = f'{KEY}__{name}.png'
        img.save(OUT_M / fn)
        entries.append({'output': f'{KEY}/{fn}', 'replacesTexture': name, 'size': list(img.size),
                        'legacySprites': rects, 'renderer': renderers[name]})
    meta = {'key': KEY, 'rig': 'CavelingMerchantEntity', 'kind': 'legacy',
            'character': 'Tilly the Seedgrub: hive larva gardener (straw hat, Glow Tulip, seed basket, watering can)',
            'animator': {'clips': {'MerchantIdleR': 'frames 0-9', 'MerchantMoveR': 'frames 10-17',
                                   'MerchantWaveR': 'frames 18-23 (Eye_10)'}},
            'textures': entries}
    (OUT_M / 'sheets.json').write_text(json.dumps(meta, indent=1))
    for nm, im in zip(('icon', 'smallIcon', 'placed'), idol):
        im.save(OUT_I / f'{KEY}_{nm}.png')
    preview(sheets, drawn, idol)
    print('ok: wrote', OUT_M, OUT_I, OUT_P / f'{KEY}.png')
    return 0


if __name__ == '__main__':
    sys.exit(main())
