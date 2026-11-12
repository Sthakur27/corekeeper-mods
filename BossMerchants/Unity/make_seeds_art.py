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


# ------------------------------------------------------------------------------------------- palette (vanilla)
# Restyle (2026-11-12, Sid's in-game screenshot: Old Hemlock read off-style next to the vanilla Caveling Merchant;
# Tilly had the same faults: a big flat bright-yellow hat, a pale pink body, a round blob silhouette). This pass
# follows the vanilla merchants instead:
#   - dark, muted ramps (5-7 close steps per material, all picked from the finisher's vanilla-sampled master
#     palette) shaded in irregular ~2x2 clusters (grain()), so straw and larva skin read as texture, not fills;
#     the bright steps only appear as small clusters on top-left planes (hat crown, larva back, tin rim);
#   - straw hat in dark ochre/brown straw with woven rings, not lemon yellow; larva body a dusky rose with
#     dark seams, not pale pink; head capsule a deep violet instead of near-black (so outlines never go black);
#   - no separate outline colour: each part's silhouette ring is its own ramp's darkest step (one step lighter
#     on the lit top/left side), like vanilla's edges;
#   - narrower body and brim, with mass moved into thin props: frayed straw ends off the brim, a tall Bloat Oat
#     stalk and a glow tulip on a thin stem out of the basket, a long watering-can spout, stubby prolegs;
#   - glow (Merchant_Light) stays a few small spots on the two tulip blooms.
def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def ramp(*cs):
    return [hx(c) for c in cs]


STRAW = ramp('352a1c', '49351d', '614822', '6e481f', '855525', '915b26', 'ab834c')   # dark ochre straw
STRAW_HI = hx('c18f47')                                                               # tiny crown accent
BAND = ramp('5e2a28', '822e13', 'a24027')                                             # hat band
BODY = ramp('562e29', '612624', '6b4545', '813530', '997171', 'b76a6a', 'e5a69f')    # dusky rose larva
BODY_HI = hx('e5a69f')
HEAD = ramp('1f102a', '2b213b', '4b3e6b', '4c376f', '6a4388', '895392')               # violet head capsule
COLLAR = ramp('612624', '822e13', 'a24027', 'c5460a')
WICK = ramp('201d15', '383420', '474130', '624f32', '645d53', 'a98963')               # grey-brown wicker
LEAF = ramp('0a3926', '0f5630', '2b5432', '356a2c', '4c8d32', '5ca537')
TULIP = ramp('0f2960', '143eab', '165dd9', '1885d8')
OAT = ramp('614822', '855525', '9b5f23', 'c18f47')
TIN = ramp('232930', '47677e', '50667d', '677fae', '90a0c4', 'a1aedd')
EYEC = hx('d4b987')
GLOW = {'1': hx('0f9e88'), '2': hx('4ad8ae'), '3': hx('94f7dd')}


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


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def qv(v, r, lo=1, hi=None):
    """Quantise light value v (0..1) onto r[lo:hi] (the darkest step is kept for the silhouette ring)."""
    rr = r[lo:hi]
    return rr[max(0, min(len(rr) - 1, int(v * len(rr))))]


def ellipse_mask(cx, cy, rx, ry):
    m = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1.0:
                m.add((x, y))
    return m


def part(L, pix, r, ring=True, lo=1):
    """Draw {(x, y): rgb} with a 1 px ring in the ramp's own dark tones: r[lo] on the shadow (bottom/right)
    side, r[lo + 1] on the lit top/left side. Vanilla edges are mid/dark shades of the material, not one line
    (r[0] stays for seams and occlusion inside the part)."""
    if ring:
        rp = {}
        for (x, y) in pix:
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + a, y + b)
                if p in pix:
                    continue
                lit = (a, b) in ((-1, 0), (0, -1))
                if not lit:
                    rp[p] = r[lo]
                elif p not in rp:
                    rp[p] = r[lo + 1]
        for p, c in rp.items():
            L.put(p[0], p[1], c)
    for p, c in pix.items():
        L.put(p[0], p[1], c)


def ball(cx, cy, rx, ry, r, base=0.62, kx=0.42, ky=0.5, kz=0.25, g=0.16, lo=1, hi=None):
    """Ellipse shaded from the top-left with cluster grain: {(x, y): rgb}."""
    out = {}
    for (x, y) in ellipse_mask(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
        v = base - kx * nx - ky * ny + kz * (nz - 0.5) + g * grain(x, y)
        out[(x, y)] = qv(v, r, lo, hi)
    return out


def grid(L, rows, ox, oy, pal, sway=None):
    """Paint a hand-authored grid. sway(row) -> dx for that row (plants)."""
    for j, row in enumerate(rows):
        dx = sway(j) if sway else 0
        for i, ch in enumerate(row):
            if ch != '.':
                L.put(ox + i + dx, oy + j, pal[ch])


# ------------------------------------------------------------------------------------------- character parts
# Scale: sized to the vanilla Caveling Merchant (its body is ~20 px wide and ~29 px tall from the floor; Tilly's
# hat/basket/can silhouette stays <= 26 px wide and ~540 opaque px per frame, like vanilla). Feet on row 53.
# Back-basket plants (origin = top-left at basket x0-2, rim row-12). Rows 0-8 sway with the idle breath: a tall
# Bloat Oat stalk (thin, drooping ear) and a glow tulip on a thin stem, low leafy sprouts at the rim.
PLANTS = [
    '..hh.......',
    '.hyYh......',
    '.nyyh......',
    '..nhn......',
    '...hn...o..',
    '....n..oGo.',
    'UU..n..vVo.',
    'TcU.n..ov..',
    'tTU.n..v...',
    '.Uo.n.ov...',
    '.v..n.v....',
    'ovGonoGv...',
    'oVvonvVo...',
    'VoVvVvo....',
    'v.oVoV.....',
    'o..........',
]
PLANT_PAL = {'o': LEAF[0], 'V': LEAF[1], 'v': LEAF[3], 'G': LEAF[4], 'g': LEAF[5],
             'U': TULIP[0], 'T': TULIP[1], 't': TULIP[2], 'c': TULIP[3],
             'n': OAT[0], 'h': OAT[1], 'y': OAT[2], 'Y': OAT[3]}
BASKET_TULIP_GLOW = [(1, 7, '2'), (1, 8, '1'), (0, 7, '1')]
# Glow tulip tucked in the hat band (origin set in draw_hat): bloom on a short stem leaning out.
HAT_TULIP = [
    '.UU',
    'UcT',
    'UtU',
    '.v.',
]
HAT_TULIP_GLOW = [(1, 1, '3'), (2, 1, '2'), (1, 2, '1'), (2, 2, '1')]
# Tin watering can at the hip, seen from above: oval top with the dark opening, long thin spout ending in a rose.
CAN = [
    '.aSa....',
    'azMsa.bb',
    'aMssSbSb',
    'aMssSb..',
    '.aSSSb..',
    '..aaa...',
]
CAN_PAL = {'z': TIN[0], 'a': TIN[1], 'b': TIN[1], 'S': TIN[2], 's': TIN[3], 'M': TIN[4], 'm': TIN[5]}


def draw_body(L, P):
    """Larva body: four stacked segments shaded in clusters, dark seams, stubby prolegs and feet."""
    b = P['bob']
    lean = P.get('lean', 0)
    segs = [  # bottom -> top
        (30.0 + lean * 0.3, 48.8, 5.5 + 0.3 * b, 2.6 - 0.2 * b),
        (30.2 + lean * 0.5, 46.2 + b * 0.6, 5.1, 2.3),
        (30.4 + lean * 0.8, 43.8 + b * 0.9, 4.7, 2.1),
        (30.7 + lean, 41.6 + b, 4.2, 2.0),
    ]
    for fx, dy in ((P['footB'], P['footBy']), (P['footF'], P['footFy'])):   # feet behind the bottom segment
        foot = {}
        for (x, y) in ellipse_mask(fx, 52.0 + dy, 1.5, 1.0):
            foot[(x, y)] = HEAD[3] if (y < 52 + dy and x < fx + 0.5) else HEAD[2]
        part(L, foot, HEAD, lo=2)
    body = {}
    seam = set()
    for k, (cx, cy, rx, ry) in enumerate(reversed(segs)):   # top segment first; lower (nearer) ones overlap
        m = ball(cx, cy, rx, ry, BODY, base=0.47 - 0.03 * k, kx=0.5, ky=0.5, kz=0.3, g=0.24, lo=2, hi=6)
        top = min(y for _, y in m)
        for (x, y) in m:
            if k and y == top and (x, y - 1) in body:      # the segment's upper rim tucks under the one above
                seam.add((x, y))
        body.update(m)
    for p in seam:
        body[p] = BODY[1] if (p[0] + p[1]) % 5 else BODY[2]
    for cx, cy, rx, ry in segs:   # a small lit cluster on the back of each segment: the only light steps
        x0, y0 = int(cx - rx * 0.45), int(cy - ry * 0.55)
        for (x, y), c in (((x0, y0), BODY[6]), ((x0 + 1, y0), BODY[6]), ((x0, y0 + 1), BODY[5]),
                          ((x0 + 1, y0 + 1), BODY[5])):
            if (x, y) in body and (x, y) not in seam:
                body[(x, y)] = c
    sx, sy, srx, _ = segs[2]
    body[(int(sx - srx * 0.45), int(sy - 1))] = BODY_HI
    body[(int(sx - srx * 0.45) + 1, int(sy - 1))] = BODY_HI
    union = set(body)
    for (x, y) in list(body):   # underside facing the floor
        if (x, y + 1) not in union and y > segs[0][1]:
            body[(x, y)] = BODY[2]
    part(L, body, BODY)
    for (cx, cy, rx, ry) in segs[1:3]:   # stubby prolegs poking out on both sides (break the round outline)
        for side in (-1, 1):
            x = int(round(cx + side * (rx + 1.2)))
            y = int(cy + 0.6)
            if (x, y) not in L.px:
                L.put(x, y, HEAD[2])
                L.put(x, y + 1, HEAD[1])
    return segs


def draw_basket(L, P):
    """Wicker seed basket on the back (left), seen from above: oval rim around a dark soil opening, front face
    in weave bands darkening downward, curved oval bottom; plants grow out of the opening."""
    dy = P['bob'] + P.get('basket', 0)
    cx, x0, x1 = 21.4, 18, 24
    rim_cy, bot_cy = 36.4 + dy, 45.6 + dy
    rx, ry = 3.9, 1.5
    face = {}
    for y in range(int(rim_cy), int(bot_cy) + 2):
        for x in range(x0, x1 + 1):
            if y > bot_cy:
                nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - bot_cy) / ry
                if nx * nx + ny * ny > 1:
                    continue
            t = (y - rim_cy) / (bot_cy - rim_cy + 1)
            nx = (x + 0.5 - cx) / rx
            v = 0.7 - 0.5 * t - 0.35 * nx + 0.14 * grain(x, y)
            r = int(y - rim_cy)
            v += 0.16 if ((x - x0) // 2 + r // 2) % 2 else -0.12   # basket weave in 2x2 clusters
            face[(x, y)] = qv(v, WICK, 2, 6)
    rim = ellipse_mask(cx, rim_cy, rx, ry)
    hole = ellipse_mask(cx + 0.2, rim_cy + 0.1, rx - 1.2, ry - 0.6)
    for (x, y) in rim:
        nx = (x + 0.5 - cx) / rx
        face[(x, y)] = WICK[5] if (nx < -0.2 and y < rim_cy) else (WICK[4] if nx < 0.5 else WICK[3])
    for (x, y) in hole:
        face[(x, y)] = WICK[0] if y <= rim_cy else STRAW[1]
    part(L, face, WICK)
    po = (x0 - 1, int(rim_cy) - 14)
    sw = P.get('sway', 0)
    grid(L, PLANTS, po[0], po[1], PLANT_PAL, sway=lambda j: sw if j <= 9 else 0)
    for (x, y) in rim:   # front lip of the rim covers the stems
        if y >= rim_cy and (x, y) not in hole:
            L.put(x, y, WICK[4] if x < cx + 1 else WICK[3])
    return po, sw


def draw_head(L, P):
    """Glossy violet larva head capsule with small cream bead eyes, mandibles and a rust collar."""
    b = P['bob']
    lean = P.get('lean', 0)
    cx, cy = 31.6 + lean, 35.4 + b
    col = ball(cx - 0.5, cy + 3.2, 3.4, 1.1, COLLAR, base=0.6, kx=0.6, ky=0.4, g=0.1, lo=1)
    part(L, col, COLLAR)
    head = ball(cx, cy, 3.8, 3.2, HEAD, base=0.58, kx=0.45, ky=0.6, kz=0.3, g=0.15, lo=1, hi=5)
    for (x, y), c in (((int(cx - 2), int(cy - 1)), HEAD[5]), ((int(cx - 1), int(cy - 1)), HEAD[5]),
                      ((int(cx - 2), int(cy)), HEAD[4])):   # gloss cluster
        if (x, y) in head:
            head[(x, y)] = c
    part(L, head, HEAD)
    ey = int(cy) + 1   # face sits low on the head: we look down on it
    eyes = [(int(cx + 0.6), ey), (int(cx + 2.8), ey)]
    for ex, _ in eyes:
        L.put(ex, ey, EYEC)
        L.put(ex, ey + 1, HEAD[1])
    mx, my = int(cx + 1.6), int(cy + 2.9)
    for (x, y, c) in ((mx, my, HEAD[4]), (mx + 1, my, HEAD[3]), (mx + 2, my, HEAD[1]), (mx, my + 1, HEAD[1]),
                      (mx + 1, my + 1, HEAD[0])):
        L.put(x, y, c)
    return (cx, cy), eyes


def draw_hat(L, P):
    """Straw sun hat from above: oval brim with woven rings in clusters (top plane, but dark straw), a short
    crown whose top oval carries the only light straw, a darker front edge, frayed straw ends at both sides."""
    b = P['bob']
    lean = P.get('lean', 0)
    lift = P.get('lift', 0)
    tilt = P.get('tilt', 0)
    dy = b + lift
    bx, by = 31.0 + lean, 30.8 + dy
    brx, bry = 7.4, 3.0
    crx = 3.3
    ccx = 30.8 + lean + tilt * 0.5
    y = int(by + bry) + 1   # brim shadow on the head below
    for x in range(int(bx - 4), int(bx + 5)):
        if L.px.get((x, y)) in HEAD[2:]:
            L.put(x, y, HEAD[1])
    hat = {}
    brim = ellipse_mask(bx, by, brx, bry)
    for (x, yy) in brim:
        nx, ny = (x + 0.5 - bx) / brx, (yy + 0.5 - by) / bry
        r = math.sqrt(nx * nx + ny * ny)
        v = 0.6 - 0.35 * nx - 0.3 * ny + 0.16 * grain(x, yy)
        if 0.62 < r < 0.8 and (x + yy) % 4:     # woven ring, broken
            v -= 0.22
        hat[(x, yy)] = qv(v, STRAW, 2, 7)
    for (x, yy) in brim:   # brim thickness: the front edge row is the darker vertical face
        if (x, yy + 1) not in brim:
            hat[(x, yy)] = STRAW[2] if x < bx + 2 else STRAW[1]
    top_cy = by - 3.8
    for yy in range(int(top_cy), int(by) + 1):
        for x in range(int(ccx - 4), int(ccx + 5)):
            nx = (x + 0.5 - ccx) / crx
            if abs(nx) <= 1:
                if yy == int(by) - 1:
                    hat[(x, yy)] = BAND[2] if nx < -0.3 else (BAND[1] if nx < 0.5 else BAND[0])
                else:
                    hat[(x, yy)] = qv(0.62 - 0.5 * nx + 0.14 * grain(x, yy), STRAW, 2, 6)
    for (x, yy) in ellipse_mask(ccx, top_cy, crx, 1.3):
        nx = (x + 0.5 - ccx) / crx
        hat[(x, yy)] = qv(0.78 - 0.5 * nx + 0.14 * grain(x, yy), STRAW, 3, 7)
    hat[(int(ccx - 1), int(top_cy))] = STRAW_HI     # 2 px warm accent on the crown's lit top
    hat[(int(ccx - 2), int(top_cy))] = STRAW_HI
    hat[(int(ccx), int(top_cy))] = STRAW[3]         # crown dent
    hat[(int(ccx + 1), int(top_cy))] = STRAW[3]
    part(L, hat, STRAW)
    xl, xr = int(bx - brx) - 1, int(bx + brx)   # frayed straw ends poking out of the brim edge (thin, no ring)
    for (x, yy, c) in ((xl - 1, int(by), STRAW[4]), (xl - 2, int(by) + 1, STRAW[2]), (xl, int(by) - 2, STRAW[3]),
                       (xr + 1, int(by) + 1, STRAW[2]), (xr + 1, int(by) + 2, STRAW[1]), (xr, int(by) - 2, STRAW[2])):
        L.put(x, yy, c)
    to = (int(ccx + 1.5), int(by) - 5)
    grid(L, HAT_TULIP, to[0], to[1], PLANT_PAL)
    return to


def draw_strap_and_can(L, P):
    b = P['bob']
    lean = P.get('lean', 0)
    pts = [(25, 39), (26, 40), (27, 41), (28, 42), (29, 43), (30, 44), (31, 45), (32, 46), (33, 46)]
    for (x, y) in pts:
        yy = y + (b if y < 44 else 0)
        L.put(x + int(lean * 0.6), yy, BAND[1])
        L.put(x + int(lean * 0.6), yy + 1, BAND[0])
    swing = P.get('canSwing', 0)
    grid(L, CAN, 33 + swing + int(lean * 0.4), 46, CAN_PAL)


def draw_arm(L, P):
    ax, ay = P['arm']
    m = ball(ax, ay, 1.4, 1.1, HEAD, base=0.6, g=0.0, lo=2, hi=5)
    part(L, m, HEAD, lo=2)
    L.put(int(ax + 1.6), int(ay), HEAD[4])   # claw tip
    L.put(int(ax + 1.6), int(ay) + 1, HEAD[2])


def draw_frame(P):
    main, light, eye = Layer(), Layer(), Layer()
    po, sw = draw_basket(main, P)
    draw_body(main, P)
    draw_strap_and_can(main, P)
    _, eyes = draw_head(main, P)
    to = draw_hat(main, P)
    draw_arm(main, P)
    pulse = P.get('pulse', 0)
    for (x, y, k) in HAT_TULIP_GLOW[:2 + pulse]:
        light.put(to[0] + x, to[1] + y, GLOW[k])
    for (x, y, k) in BASKET_TULIP_GLOW[:1 + pulse]:
        light.put(po[0] + x + sw, po[1] + y, GLOW[k])
    light.px = {p: c for p, c in light.px.items() if p in main.px}
    for (x, y) in eyes:   # glints stay on during the wave too (the wave clip reuses Eye_10)
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
        out['idle'].append(dict(bob=b, sway=sway[i], pulse=pulse[i], footB=26.0, footBy=0, footF=33.5, footFy=0,
                                arm=(35.6, 42.4 + b), canSwing=0))
    bob = [0, 1, 1, 0, 0, 1, 1, 0]
    ff = [1, 1, 0, -1, -1, -1, 0, 1]
    swayM = [0, -1, -1, 0, 1, 1, 0, 0]
    for i in range(8):
        f = ff[i]
        out['move'].append(dict(bob=bob[i], sway=swayM[i], pulse=1, lean=1 if i in (1, 2, 5, 6) else 0,
                                footB=26.0 - f, footBy=-1 if f > 0 and i in (0, 7) else 0,
                                footF=33.5 + f, footFy=-1 if f < 0 and i in (3, 4) else 0,
                                arm=(35.6 + (1 if i in (1, 2, 5, 6) else 0), 42.4 + bob[i] - (1 if ff[i] > 0 else 0)),
                                canSwing=-1 if i in (2, 3, 4) else 0, basket=-bob[i]))
    wave = [  # tip the hat: (arm, lift, happy)
        ((36.2, 40.0), 0, False),
        ((37.2, 35.0), 0, False),
        ((37.8, 30.6), -2, True),
        ((38.0, 29.6), -3, True),
        ((37.8, 30.6), -2, True),
        ((36.4, 38.6), 0, False),
    ]
    for (arm, lft, happy) in wave:
        out['wave'].append(dict(bob=0, sway=0, pulse=2, footB=26.0, footBy=0, footF=33.5, footFy=0, arm=arm,
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
