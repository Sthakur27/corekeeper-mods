"""Hydra merchant ("hydras"): Tridra the Haggler + the Triune Idol. Original pixel art drawn from code.

    python -I BossMerchants/Unity/make_hydras_art.py

Character: a young three-headed hydra trader sitting on its own coiled tail. Each head takes after one of the
biome Hydras and argues prices with the other two: a leafy green head (Druidra the Wild Titan), a cyan ice head
with a glowing crystal crest (Crydra the Ice Titan) and a charcoal head with lava cracks and ember horns (Pyrdra
the Fire Titan). Slate scales, bone-cream belly plates, gold neck bangles, a purple trade sash with a three-gem
pendant and a coin purse.

Rig: CavelingMerchant (legacy SpriteRenderer + Animator), three sheets with the vanilla cell grid (64x64 cells,
pivot centre, 16 ppu): Merchant 640x192 (24 frames), Merchant_Light 640x192 (24), Merchant_Eye 640x128 (18).
Clips (from the vanilla AnimationClips): MerchantIdleR = frames 0-9, MerchantMoveR = 10-17, MerchantWaveR = 18-23
(eye sheet only has 0-17; the wave clip shows Merchant_Eye_10 for its whole length, so the eye layer holds only
the pendant gem, which stays put during the wave). Light and Eye are unlit overlays drawn on top of the base at the
same pixel positions (vanilla does the same with the lantern), so every overlay pixel is also in the base sheet.
The vanilla cyan flare child ("Glow", fixed at cell ~(33,20)) lands on the ice head's crystal crest.
Ground line = vanilla's: the lowest opaque row in each cell is 53.

Idol: a bone totem that forks into three serpent heads (green, ice, fire) around a gold-ringed tri-colour gem;
icon 16x16 bottom-anchored like Mysterious Idol / Void Merchant Idol, smallIcon ~11x11 centred, placed = icon.

Everything here is drawn from code / hand-authored grids with colours from the game's shared item palette;
no vanilla pixels are copied. Vanilla images (build/bossmerchants/...) are read for the review preview only.
Validation (exit 1 on failure): sheet sizes = rig sizes, pixels only inside cells, hard alpha, overlay pixels
are a subset of the base, ground line, idol sizes 16x16.
"""
import json
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
MOD = HERE.parent
ROOT = MOD.parent
KEY = 'hydras'
ART = HERE / 'Art'
OUT_M = ART / 'merchants' / KEY
OUT_I = ART / 'idols'
OUT_P = ART / 'previews'
REF = ROOT / 'build' / 'bossmerchants'

CELL = 64
GROUND = 53          # lowest opaque row in a cell (vanilla Caveling Merchant)
SHEETS = {           # texture -> (size, frame count, frames per row)
    'Merchant': ((640, 192), 24),
    'Merchant_Light': ((640, 192), 24),
    'Merchant_Eye': ((640, 128), 18),
}
RENDERER = {
    'Merchant': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SR',
    'Merchant_Light': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SRLight',
    'Merchant_Eye': '/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SREye',
}


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


# ---------------------------------------------------------------- palette (all from vanilla item icons)
P = {k: hexc(v) for k, v in dict(
    # slate scales (body, coil)
    sl0='0b1917', sl1='2a4e4a', sl2='47677e', sl3='677fae', sl4='88a2ee',
    # bone belly
    bo0='383447', bo1='645d53', bo2='a69298', bo3='c0bacf', bo4='d7dcfb',
    # nature head
    gr0='073c31', gr1='0f5630', gr2='0a6d27', gr3='239029', gr4='5ca537', grE='f4a7c5', grP='b22e69',
    # ice head
    ic0='142b5c', ic1='143eab', ic2='1885d8', ic3='19bdc6', ic4='4ad8ae', ic5='94f7dd', icW='ffffff',
    # fire head
    fi0='232930', fi1='383447', fi2='484454', fi3='645d53', fiL0='91210b', fiL1='c5460a', fiL2='da7910', fiL3='f2df3a',
    # gold
    go0='39341f', go1='614822', go2='a88b37', go3='c9ab51', go4='e0c961',
    # purple sash
    pu0='2a235d', pu1='723e8a', pu2='a555a4', pu3='ee62a6',
    # leather
    le0='39341f', le1='614822', le2='915b26', le3='b58047',
    tongue='d2399e', tooth='d7dcfb', bo1x='8f7b77',
).items()}
CLEAR = (0, 0, 0, 0)


# ---------------------------------------------------------------- tiny raster helpers
class Canvas:
    def __init__(self, w=CELL, h=CELL):
        self.w, self.h = w, h
        self.px = {}

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[(x, y)] = c

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), CLEAR)
        for (x, y), c in self.px.items():
            im.putpixel((x, y), c)
        return im


def shade_mask(cv, mask, ramp, spec=True, lit_edges=True):
    """Fill a pixel set with a 5-step ramp [outline, shadow, base, light, spec], light from the top-left.
    The outline is inset (the set's own border pixels), so a part drawn later is cleanly separated from
    whatever it covers, the way vanilla sprites separate overlapping parts."""
    border = {(x, y) for (x, y) in mask
              if any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    for (x, y) in mask:
        if (x, y) in border:
            out = [(dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (x + dx, y + dy) not in mask]
            # broken, lighter outline on lit (top/left) edges; full dark outline elsewhere
            c = ramp[1] if all(d in ((-1, 0), (0, -1)) for d in out) and lit_edges else ramp[0]
        else:
            tl = (x, y - 1) in border or (x - 1, y - 1) in border
            br = (x, y + 1) in border or (x + 1, y + 1) in border
            if tl and spec and (x - 1, y) in border and (x, y - 1) in border:
                c = ramp[4]
            elif tl and not br:
                c = ramp[3]
            elif br and not tl:
                c = ramp[1]
            else:
                c = ramp[2]
        cv.put(x, y, c)


def ellipse(cx, cy, rx, ry):
    s = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                s.add((x, y))
    return s


def tube(points, r0, r1):
    """Pixels of a tapered tube along a polyline (sampled as discs)."""
    s = set()
    n = len(points)
    for i in range(n - 1):
        (x0, y0), (x1, y1) = points[i], points[i + 1]
        steps = max(2, int(math.hypot(x1 - x0, y1 - y0) * 3))
        for k in range(steps + 1):
            t = (i + k / steps) / (n - 1)
            x = x0 + (x1 - x0) * k / steps
            y = y0 + (y1 - y0) * k / steps
            r = r0 + (r1 - r0) * t
            s |= ellipse(x, y, r, r)
    return s


def bez(p0, p1, p2, n=10):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / n for i in range(n + 1))]


def blit(cv, grid, pal, ox, oy, flip=False, light=None, lightkeys=''):
    """Stamp a hand-authored grid; keys in lightkeys are copied to the light canvas too."""
    w = max(len(r) for r in grid)
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch == '.' or ch == ' ':
                continue
            xx = ox + (w - 1 - x if flip else x)
            cv.put(xx, oy + y, pal[ch])
            if light is not None and ch in lightkeys:
                light.put(xx, oy + y, pal[ch])


def shade_top(cv, mask, ramp, spec=True):
    """Top-down 3/4 form shading, light from above / top-left: in every column the upper part of a shape
    is its top plane (lightest, spec on the upper-left), the middle is the viewer-facing side, and it darkens
    toward the underside. Inset outline: dark, but softened (shadow step) along the lit top edge."""
    border = {(x, y) for (x, y) in mask
              if any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    xs = [x for x, _ in mask]
    x0, x1 = min(xs), max(xs)
    for (x, y) in mask:
        if (x, y) in border:
            out = [(dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (x + dx, y + dy) not in mask]
            cv.put(x, y, ramp[1] if out == [(0, -1)] and x < (x0 + x1) / 2 else ramp[0])
            continue
        top = y
        while (x, top - 1) in mask and (x, top - 1) not in border:
            top -= 1
        bot = y
        while (x, bot + 1) in mask and (x, bot + 1) not in border:
            bot += 1
        n = bot - top + 1
        t = (y - top) / max(1, n - 1) if n > 1 else 0.0
        right = (x1 - x) <= 1 or (x + 1, y) in border
        if t <= 0.34 and n > 1 or n == 1 and not right:
            c = ramp[4] if (spec and y == top and x < x0 + (x1 - x0) * 0.45 and (x - 1, y) not in border) else ramp[3]
        elif t >= 0.75 or right:
            c = ramp[1]
        else:
            c = ramp[2]
        cv.put(x, y, c)


# ---------------------------------------------------------------- heads (facing right, seen from above)
# Vanilla-scale heads (8 px wide): the top of the skull/snout is the lit top plane (s/h), the eye sits low on the
# side of the face (b row), the jaw underside is in shadow (d). o outline, n nostril, m mouth line, w tooth,
# t tongue, k open-mouth dark, c/C crest (horns / crystals / leaves).
JAW_SHUT = [
    '.oddmmmmo',
    '..oooooo.',
]
JAW_OPEN = [
    '.oddwkkoo',
    '.oddttoo.',
    '..ooooo..',
]
TOP_FIRE = [
    '.C...C...',
    '.coooCoo.',
    'ohsshhhoo',
    'ohshhhhho',
    'obhhhhhno',
    'obebbbbbo',
]
TOP_ICE = [
    '..cCc....',
    '.oCcCooo.',
    'ohsshhhoo',
    'ohshhhhho',
    'obhhhhhno',
    'obebbbbbo',
]
TOP_NATURE = [
    'cC.Cc....',
    'oCCCCooo.',
    'ohsshhhoo',
    'ohshhhhho',
    'obhhhhhno',
    'obebbbbbo',
]
HEAD_W = 9
PAL_FIRE = {'o': P['fi0'], 'd': P['fi1'], 'b': P['fi2'], 'h': P['fi3'], 's': P['bo1x'], 'e': P['fiL3'],
            'n': P['fiL0'], 'm': P['fiL0'], 't': P['tongue'], 'w': P['tooth'], 'k': P['fiL0'],
            'c': P['fiL3'], 'C': P['fiL2']}
PAL_ICE = {'o': P['ic0'], 'd': P['ic1'], 'b': P['ic2'], 'h': P['ic3'], 's': P['ic4'], 'e': P['icW'],
           'n': P['ic0'], 'm': P['ic0'], 't': P['tongue'], 'w': P['tooth'], 'k': P['ic0'],
           'c': P['ic5'], 'C': P['ic4']}
PAL_NATURE = {'o': P['gr0'], 'd': P['gr1'], 'b': P['gr2'], 'h': P['gr3'], 's': P['gr4'], 'e': P['grE'],
              'n': P['gr0'], 'm': P['gr0'], 't': P['tongue'], 'w': P['tooth'], 'k': P['gr0'],
              'c': P['gr4'], 'C': P['gr3']}
HEADS = {'n': (TOP_NATURE, PAL_NATURE, 'e'), 'i': (TOP_ICE, PAL_ICE, 'ecC'), 'f': (TOP_FIRE, PAL_FIRE, 'ecC')}

RAMP_SLATE = [P['sl0'], P['sl1'], P['sl2'], P['sl3'], P['sl4']]
RAMP_GREEN = [P['gr0'], P['gr1'], P['gr2'], P['gr3'], P['gr4']]
RAMP_ICE = [P['ic0'], P['ic1'], P['ic2'], P['ic3'], P['ic4']]
RAMP_FIRE = [P['fi0'], P['fi1'], P['fi2'], P['fi3'], P['bo1x']]
RAMP_GOLD = [P['go1'], P['go2'], P['go3'], P['go4'], P['go4']]

PENDANT = [  # tiny gold-framed tri-gem on the sash
    '.Z.',
    'NIF',
    '.G.',
]
PAL_PENDANT = {'o': P['go0'], 'g': P['go3'], 'G': P['go2'], 'Z': P['go4'], 'N': P['gr4'], 'I': P['ic5'], 'F': P['fiL2']}

PURSE = [  # seen from above: the tied top and the bag's upper bulge catch the light
    '.yo.',
    'oLlo',
    'olko',
    '.oo.',
]
PAL_PURSE = {'o': P['le0'], 'l': P['le2'], 'L': P['le3'], 'k': P['le1'], 'y': P['go4']}


def head(base, light, key, x, y, open_mouth, flip=False):
    top, pal, keys = HEADS[key]
    grid = top + (JAW_OPEN if open_mouth else JAW_SHUT)
    blit(base, grid, pal, x, y, flip=flip, light=light, lightkeys=keys)


# ---------------------------------------------------------------- one frame
# Scale: drawn to the vanilla Caveling Merchant's footprint (body ~16 px wide, whole silhouette <= 28 x 47,
# ~570 opaque px per frame, ground row 53, centred on x ~31 like vanilla). The three necks stand in for the
# vanilla staff/lantern height; the centre (ice) head sits near the vanilla lantern glow (~(33,20)).
def frame(kind, i):
    """Return (base, light, eye) canvases for frame i of 'idle' (10), 'move' (8) or 'wave' (6)."""
    base, light, eye = Canvas(), Canvas(), Canvas()
    by = 0
    hn = hi = hf = (0, 0)      # head offsets (nature, ice, fire)
    open_ = {'n': False, 'i': False, 'f': False}
    band = 0                   # scale-seam phase on the coil (slither)
    tail = 0
    if kind == 'idle':
        ph = 2 * math.pi * i / 10
        by = 1 if i in (4, 5, 6, 7) else 0                  # slow breath
        hn = (0, round(0.8 * math.sin(ph + 2.2)))
        hi = (0, round(0.8 * math.sin(ph)))
        hf = (round(0.6 * math.sin(ph + 1.0)), round(0.8 * math.sin(ph + 4.2)))
        tail = 1 if i in (2, 3, 4, 5) else 0
        open_['f'] = i in (7, 8)                             # the fire head mutters a price
    elif kind == 'move':
        ph = 2 * math.pi * i / 8
        by = [0, -1, -1, 0, 0, -1, -1, 0][i]
        band = i % 4
        hn = (round(0.7 * math.sin(ph + 1.5)), round(0.8 * math.sin(ph + 0.5)))
        hi = (round(0.7 * math.sin(ph + 0.8)), round(0.8 * math.sin(ph + 2.0)))
        hf = (round(0.7 * math.sin(ph)), round(0.8 * math.sin(ph + 3.5)))
        tail = round(1.2 * math.sin(ph + 1.0))
    elif kind == 'wave':
        # all three heads rear up to greet the customer one after another; mouths open at the peak
        hf = (1 if i in (2, 3) else 0, [0, -2, -3, -3, -2, -1][i])
        hi = (0, [0, -1, -2, -2, -1, 0][i])
        hn = (0, [0, 0, -1, -2, -2, -1][i])
        open_ = {'n': i in (3, 4), 'i': i in (2, 3), 'f': i in (1, 2, 3)}
        tail = [0, 1, 1, 0, -1, 0][i]

    # ---- coil seen from above: an oval ring of tail on the floor (lit top band, floor visible in the hole);
    # the torso rises out of its back half, the front half overlaps the torso base.
    cx, cy = 31.0, 49.3
    outer = ellipse(cx, cy, 10.2, 4.7)
    inner = ellipse(cx + 0.4, cy - 0.5, 5.0, 1.6)
    ring = {p for p in outer - inner if p[1] <= GROUND}
    back = {p for p in ring if p[1] < cy - 0.5}
    front = {p for p in ring if p[1] >= cy - 0.5}
    tip = tube(bez((23.5, 51.0), (20.0, 50.5 - tail), (20.0, 46.5 - tail), 6), 1.5, 0.7)
    shade_top(base, tip, RAMP_SLATE)
    shade_top(base, back, RAMP_SLATE)

    # ---- torso: squat and foreshortened; the shoulders are a lit top plane
    torso = ellipse(31.5, 40.6 + by, 4.6, 4.8) | ellipse(31.0, 44.6 + by, 5.5, 3.6)
    torso = {p for p in torso if p[1] <= cy + 0.5}
    shade_top(base, torso, RAMP_SLATE)
    # bone belly plates on the facing (right) side
    for y in range(38, 49):
        row = sorted(x for (x, yy) in torso if yy == y + by)
        if not row:
            continue
        xr = row[-1] - 1
        for x in (xr - 1, xr):
            if (x, y + by) in torso and base.px.get((x, y + by)) != P['sl0']:
                c = P['bo1x'] if y % 2 == 0 else (P['bo4'] if x == xr - 1 and y < 43 else P['bo3'])
                base.put(x, y + by, c)
    # purple sash from the left shoulder to the right hip (lit upper edge, dark lower edge)
    for k in range(9):
        x = 27 + round(k * 0.7)
        y = 38 + k + by
        for w, c in enumerate((P['pu2'], P['pu0'])):
            if (x + w, y) in torso and base.px.get((x + w, y)) != P['sl0']:
                base.put(x + w, y, c)

    # front half of the coil over the torso base, with scale seams that slide when moving
    shade_top(base, front, RAMP_SLATE)
    for x in range(21, 42):
        col = sorted(y for (xx, y) in front if xx == x)
        if len(col) < 4:
            continue
        if (x + band) % 4 == 0:
            for y in col[1:-1]:
                if base.px.get((x, y)) == P['sl2']:
                    base.put(x, y, P['sl1'])
    # occlusion: the underside of the torso where it sinks into the coil
    for (x, y) in list(torso):
        if (x, y + 1) in front and (x, y) not in front and base.px.get((x, y)) != P['sl0']:
            base.put(x, y, P['sl1'])

    blit(base, PURSE, PAL_PURSE, 36, 46 + by)

    # ---- torc around the neck bases, an oval seen from above: back half first, necks, then front half
    torc_outer = ellipse(31.5, 37.2 + by, 5.0, 2.0)
    torc_inner = ellipse(31.5, 36.7 + by, 2.6, 0.7)
    torc = torc_outer - torc_inner
    torc_back = {p for p in torc if p[1] < 36.9 + by}
    torc_front = torc - torc_back
    shade_top(base, torc_back, RAMP_GOLD, spec=False)

    # ---- necks (back to front) with heads: the tall ice neck in the middle (behind), the nature head looks
    # away (left), the fire head faces the customer (right)
    pos = {'n': (18 + hn[0], 22 + hn[1] + by), 'i': (29 + hi[0], 15 + hi[1] + by), 'f': (35 + hf[0], 23 + hf[1] + by)}
    attach = {'n': (pos['n'][0] + HEAD_W - 3, pos['n'][1] + 7), 'i': (pos['i'][0] + 2.5, pos['i'][1] + 7),
              'f': (pos['f'][0] + 2.5, pos['f'][1] + 7)}
    necks = {'n': ((29.0, 37.5 + by), (24 + hn[0] * 0.5, 37 + by), RAMP_GREEN),
             'i': ((31.5, 37 + by), (28.5 + hi[0] * 0.5, 29 + by), RAMP_ICE),
             'f': ((34.0, 37.5 + by), (38.5 + hf[0] * 0.5, 37 + by), RAMP_FIRE)}
    for k in ('i', 'n', 'f'):
        sh, ctrl, ramp = necks[k]
        pts = bez(sh, ctrl, attach[k], 12)
        m = tube(pts, 2.4, 1.8)
        shade_top(base, m, ramp)
        if k == 'f':   # lava cracks down the fire neck: short 2-px fissures
            for t in (5, 8):
                x, y = pts[t]
                for q in ((int(x), int(y)), (int(x), int(y) + 1)):
                    if q in m and base.px.get(q) not in (ramp[0], ramp[3], ramp[4]):
                        base.put(q[0], q[1], P['fiL1'])
                        light.put(q[0], q[1], P['fiL1'])
        # occlusion under the jaw where the head overhangs the neck
        hx, hy = pos[k]
        for x in range(hx, hx + HEAD_W):
            q = (x, hy + 8)
            if q in m and base.px.get(q) != ramp[0]:
                base.put(q[0], q[1], ramp[1])
        head(base, light, k, pos[k][0], pos[k][1], open_[k], flip=(k == 'n'))

    shade_top(base, torc_front, RAMP_GOLD, spec=False)
    base.put(28, int(38.6 + by), P['go4'])
    base.put(29, int(38.6 + by), P['go4'])

    # pendant over the sash; its gems glow (eye layer, which stays put during the wave)
    blit(base, PENDANT, PAL_PENDANT, 30, 40 + by, light=eye, lightkeys='NIF')
    # overlays only keep glow pixels that are still visible on top in the base
    for ov in (light, eye):
        ov.px = {q: c for q, c in ov.px.items() if base.px.get(q) == c}
    return base, light, eye


FRAMES = [('idle', i) for i in range(10)] + [('move', i) for i in range(8)] + [('wave', i) for i in range(6)]


def cell_xy(tex, idx):
    """Top-left (PIL coords) of frame idx, matching the vanilla rects: one row per clip (idle 0-9, move 10-17,
    wave 18-23), first row at the top of the image (= highest Unity y)."""
    row = 0 if idx < 10 else 1 if idx < 18 else 2
    col = idx - (0, 10, 18)[row]
    return col * CELL, row * CELL


def vanilla_rect(tex, idx):
    (w, h), n = SHEETS[tex]
    x, ytop = cell_xy(tex, idx)
    return [float(x), float(h - ytop - CELL), float(CELL), float(CELL)]


def build_sheets():
    frames = [frame(k, i) for k, i in FRAMES]
    sheets = {t: Image.new('RGBA', SHEETS[t][0], CLEAR) for t in SHEETS}
    for idx, (b, l, e) in enumerate(frames):
        sheets['Merchant'].paste(b.image(), cell_xy('Merchant', idx))
        sheets['Merchant_Light'].paste(l.image(), cell_xy('Merchant_Light', idx))
        if idx < 18:
            sheets['Merchant_Eye'].paste(e.image(), cell_xy('Merchant_Eye', idx))
    return sheets, frames


# ---------------------------------------------------------------- idol
IDOL_PAL = {  # bone (cool shadows, warm-white light), slate plinth, gold, three biome gems
    'o': P['bo0'], 's': P['bo1'], 'm': P['bo1x'], 'd': P['bo2'], 'c': P['bo3'], 'w': P['bo4'],
    'O': P['sl0'], '1': P['sl1'], '2': P['sl2'], '3': P['sl3'], '4': P['sl4'],
    'G': P['gr4'], 'g': P['gr3'], 'I': P['ic5'], 'i': P['ic3'], 'F': P['fiL3'], 'f': P['fiL2'],
    'n': P['go1'], 'a': P['go2'], 'Y': P['go3'], 'Z': P['go4'],
}
# Seen from above at 3/4 like the vanilla idols: lit head tops, an oval gold band (lit top surface, gems on the
# front), and an oval slate plinth whose top face is the brightest plane. A carved bone hydra bust on it: left head looks left (green eye), right head looks right (fire
# eye), the centre head faces you with an ice crest; a gold band with the three biome gems binds the necks.
IDOL_ICON = [
    '......oIIo......',
    '.oo..oIwwIo..oo.',
    'owwooowwwwooowwo',
    'owwwdowwcdoowwco',
    'osGddocccdosdFso',
    '.oosdosddsosdso.',
    '...osdcwcddsso..',
    '...oZZZZZYYao...',
    '..oaYGYIYFYYao..',
    '...onaaaaaano...',
    '....osssssso....',
    '..O4443333222O..',
    '.O443ssssss3221O',
    '.O333333222221O.',
    '.O112222221111O.',
    '..OOOOOOOOOOOO..',
]
IDOL_SMALL = [
    '................',
    '................',
    '................',
    '.......oIo......',
    '....oo.oIo.oo...',
    '...owwoowcoowwo.',
    '...oGdocdsodFo..',
    '....oosdcdsoo...',
    '.....oZZZYo.....',
    '.....aGYIFa.....',
    '.....onaano.....',
    '....O4433322O...',
    '....O3ssss21O...',
    '....O1122211O...',
    '.....OOOOOOO....',
    '................',
]


def grid_img(grid, pal):
    im = Image.new('RGBA', (16, 16), CLEAR)
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != '.':
                im.putpixel((x, y), pal[ch])
    return im


# ---------------------------------------------------------------- validation
def validate(sheets, idols):
    errs = []
    for t, im in sheets.items():
        (w, h), n = SHEETS[t]
        if im.size != (w, h):
            errs.append(f'{t}: size {im.size} != {(w, h)}')
        inside = set()
        for idx in range(n):
            x0, y0 = cell_xy(t, idx)
            for y in range(y0, y0 + CELL):
                for x in range(x0, x0 + CELL):
                    inside.add((x, y))
        px = im.load()
        for y in range(h):
            for x in range(w):
                a = px[x, y][3]
                if a not in (0, 255):
                    errs.append(f'{t}: soft alpha at {(x, y)}')
                if a and (x, y) not in inside:
                    errs.append(f'{t}: pixel outside cells at {(x, y)}')
        for idx in range(n):
            x0, y0 = cell_xy(t, idx)
            c = im.crop((x0, y0, x0 + CELL, y0 + CELL))
            bb = c.getbbox()
            if t == 'Merchant':
                if not bb or bb[3] - 1 != GROUND:
                    errs.append(f'{t}_{idx}: lowest row {bb and bb[3] - 1} != ground {GROUND}')
                if bb and (bb[0] < 1 or bb[2] > CELL - 1 or bb[1] < 1):
                    errs.append(f'{t}_{idx}: touches the cell edge {bb}')
            elif not bb:
                errs.append(f'{t}_{idx}: empty overlay frame')
    # overlays must sit on base pixels of the same frame
    base = sheets['Merchant'].load()
    for t in ('Merchant_Light', 'Merchant_Eye'):
        ov = sheets[t]
        n = SHEETS[t][1]
        o = ov.load()
        for idx in range(n):
            ox, oy = cell_xy(t, idx)
            bx, by = cell_xy('Merchant', idx)
            for y in range(CELL):
                for x in range(CELL):
                    if o[ox + x, oy + y][3] and base[bx + x, by + y][:3] != o[ox + x, oy + y][:3]:
                        errs.append(f'{t}_{idx}: overlay pixel {(x, y)} differs from base')
    for name, im in idols.items():
        if im.size != (16, 16):
            errs.append(f'idol {name}: size {im.size}')
        if any(p[3] not in (0, 255) for p in im.get_flattened_data()):
            errs.append(f'idol {name}: soft alpha')
    if idols['icon'].getbbox()[3] != 16:
        errs.append('idol icon does not sit on the bottom edge')
    for g in (IDOL_ICON, IDOL_SMALL):
        if len(g) != 16 or any(len(r) != 16 for r in g):
            errs.append('idol grid is not 16x16')
    return errs


# ---------------------------------------------------------------- preview for Sid
def preview(sheets, frames, idols):
    S = 6
    bg = (34, 32, 42, 255)
    sections = []

    def scaled(im, s):
        return im.resize((im.width * s, im.height * s), Image.NEAREST)

    def comp(b, l, e):
        im = b.image()
        im.alpha_composite(l.image())
        im.alpha_composite(e.image())
        return im

    # 1) idle frame at 6x next to vanilla merchants (frame 0 of their idle), bottoms aligned
    van = []
    try:
        r = REF / 'reference' / 'merchants'
        cav = Image.open(r / 'CavelingMerchant' / 'Merchant.png').convert('RGBA').crop((0, 0, 64, 64))
        for o in ('Merchant_Light', 'Merchant_Eye'):
            cav.alpha_composite(Image.open(r / 'CavelingMerchant' / (o + '.png')).convert('RGBA').crop((0, 0, 64, 64)))
        van.append(('Caveling', cav))
        van.append(('Slime', Image.open(r / 'SlimeMerchant' / 'Slime_merchant.png').convert('RGBA').crop((0, 0, 30, 30))))
        van.append(('Fishing', Image.open(r / 'FishingMerchant' / 'Fisher_merchant.png').convert('RGBA').crop((0, 0, 26, 40))))
        van.append(('Void', Image.open(r / 'VoidMerchant' / 'voidMerchant_idle.png').convert('RGBA').crop((0, 0, 16, 35))))
    except FileNotFoundError:
        pass
    row = [('Tridra (new)', comp(*frames[0]))] + van
    row = [(n, im.crop(im.getbbox())) for n, im in row]
    hmax = max(im.height for _, im in row) * S
    W = sum(im.width * S + 30 for _, im in row) + 60 + sum(im.width + 6 for _, im in row)
    sec = Image.new('RGBA', (W, hmax + 40), bg)
    d = ImageDraw.Draw(sec)
    x = 30
    for n, im in row:
        sec.alpha_composite(scaled(im, S), (x, 10 + hmax - im.height * S))
        d.text((x, hmax + 18), n, fill=(230, 230, 240))
        x += im.width * S + 30
    d.text((x, hmax + 18), '1x', fill=(230, 230, 240))
    for n, im in row:   # the same line-up at 1x, bottoms aligned
        sec.alpha_composite(im, (x, 10 + hmax - im.height))
        x += im.width + 6
    sections.append(sec)

    # 2) strips: idle, move, wave at 3x on a cell grid
    s2 = 3
    for name, rng in (('idle 0-9', range(0, 10)), ('move 10-17', range(10, 18)), ('wave 18-23', range(18, 24))):
        sec = Image.new('RGBA', (30 + 10 * (40 * s2 + 6), 46 * s2 + 30), bg)
        d = ImageDraw.Draw(sec)
        d.text((8, 4), name, fill=(230, 230, 240))
        for k, idx in enumerate(rng):
            im = comp(*frames[idx]).crop((12, 8, 52, 54))
            sec.alpha_composite(scaled(im, s2), (30 + k * (40 * s2 + 6), 20))
        sections.append(sec)
    # light + eye overlays (frame 0) for reference
    sec = Image.new('RGBA', (900, 64 * 3 + 30), bg)
    d = ImageDraw.Draw(sec)
    for k, (n, im) in enumerate((('base', frames[0][0].image()), ('light', frames[0][1].image()),
                                 ('eye', frames[0][2].image()), ('wave 3 base', frames[21][0].image()),
                                 ('wave 3 light', frames[21][1].image()))):
        sec.alpha_composite(scaled(im, 3), (10 + k * 178, 20))
        d.text((10 + k * 178, 4), n, fill=(230, 230, 240))
    sections.append(sec)

    # 3) idol at 1x and 8x beside vanilla idols
    vid = []
    for f, n in (('3930_MysteriousIdol_icon.png', 'Mysterious Idol'), ('3931_SlimeOil_icon.png', 'Slime Oil'),
                 ('3932_PileOfChum_icon.png', 'Pile of Chum'), ('3937_VoidMerchantIdol_icon.png', 'Void Merchant Idol')):
        try:
            vid.append((n, Image.open(REF / 'idol_reference' / f).convert('RGBA')))
        except FileNotFoundError:
            pass
    items = [('Triune Idol', idols['icon']), ('smallIcon', idols['smallIcon'])] + vid
    sec = Image.new('RGBA', (30 + len(items) * 150, 16 * 8 + 70), bg)
    d = ImageDraw.Draw(sec)
    d.text((20, 0), 'perspective self-check: idol vs vanilla idols at 1x and 8x (top planes: head tops, band top, plinth top)',
           fill=(230, 230, 240))
    for k, (n, im) in enumerate(items):
        x = 20 + k * 150
        sec.alpha_composite(im, (x, 14))
        sec.alpha_composite(scaled(im, 8), (x + 18, 14))
        d.text((x, 16 * 8 + 24), n, fill=(230, 230, 240))
    sections.append(sec)

    # 4) placed on a dark stone floor mock (3x3 tiles), 6x, plus 1x
    floor = Image.new('RGBA', (48, 48), CLEAR)
    stone = [hexc('232930'), hexc('2b2e36'), hexc('333744'), hexc('1c1f26')]
    for ty in range(3):
        for tx in range(3):
            for y in range(16):
                for x in range(16):
                    h = (x * 7 + y * 13 + tx * 5 + ty * 11) % 17
                    c = stone[0] if h < 9 else stone[1] if h < 14 else stone[2]
                    if x == 0 or y == 0:
                        c = stone[3]
                    floor.putpixel((tx * 16 + x, ty * 16 + y), c)
    shadow = Image.new('RGBA', (16, 16), CLEAR)
    for y in range(14, 16):
        for x in range(3, 13):
            shadow.putpixel((x, y), (10, 10, 14, 255))
    placed = floor.copy()
    placed.alpha_composite(shadow, (16, 17))
    placed.alpha_composite(idols['placed'], (16, 15))
    sec = Image.new('RGBA', (48 * 6 + 120, 48 * 6 + 40), bg)
    d = ImageDraw.Draw(sec)
    sec.alpha_composite(scaled(placed, 6), (20, 20))
    sec.alpha_composite(placed, (48 * 6 + 50, 20))
    d.text((20, 4), 'placed on a stone floor (6x and 1x)', fill=(230, 230, 240))
    sections.append(sec)

    W = max(s.width for s in sections)
    H = sum(s.height + 10 for s in sections)
    out = Image.new('RGBA', (W, H), bg)
    y = 0
    for s in sections:
        out.alpha_composite(s, (0, y))
        y += s.height + 10
    return out


def main():
    OUT_M.mkdir(parents=True, exist_ok=True)
    OUT_I.mkdir(parents=True, exist_ok=True)
    OUT_P.mkdir(parents=True, exist_ok=True)
    sheets, frames = build_sheets()
    icon = grid_img(IDOL_ICON, IDOL_PAL)
    idols = {'icon': icon, 'smallIcon': grid_img(IDOL_SMALL, IDOL_PAL), 'placed': icon.copy()}
    errs = validate(sheets, idols)
    if errs:
        print('\n'.join(errs[:40]))
        print(f'FAILED: {len(errs)} problem(s)')
        return 1
    textures = []
    for t, im in sheets.items():
        fn = f'{KEY}__{t}.png'
        im.save(OUT_M / fn)
        n = SHEETS[t][1]
        textures.append({'output': f'{KEY}/{fn}', 'replacesTexture': t, 'size': list(SHEETS[t][0]),
                         'renderer': RENDERER[t],
                         'legacySprites': [{'name': f'{t}_{i}', 'rect': vanilla_rect(t, i), 'pivot': [0.5, 0.5],
                                            'pixelsPerUnit': 16.0} for i in range(n)]})
    # cross-check the cell layout against the rig recorded in skins.json (CavelingMerchant), if present
    sk = ART / 'merchants' / 'skins.json'
    if sk.exists():
        rig = next((m for m in json.load(open(sk))['merchants'].values() if m.get('baseMerchant') == 'CavelingMerchant'), None)
        if rig:
            for t in rig['textures']:
                mine = next(x for x in textures if x['replacesTexture'] == t['replacesTexture'])
                if [(s['name'], s['rect']) for s in t['legacySprites']] != [(s['name'], s['rect']) for s in mine['legacySprites']] \
                        or t['size'] != mine['size']:
                    print('FAILED: cell layout differs from the CavelingMerchant rig in skins.json for', t['replacesTexture'])
                    return 1
    json.dump({'key': KEY, 'rig': 'CavelingMerchantEntity', 'kind': 'legacy', 'textures': textures},
              open(OUT_M / 'sheets.json', 'w'), indent=1)
    for n, im in idols.items():
        im.save(OUT_I / f'{KEY}_{n}.png')
    preview(sheets, frames, idols).save(OUT_P / f'{KEY}.png')
    print('ok: sheets', {t: im.size for t, im in sheets.items()}, '-> ', OUT_M)
    print('ok: idol ->', OUT_I, '; preview ->', OUT_P / f'{KEY}.png')
    return 0


if __name__ == '__main__':
    sys.exit(main())
