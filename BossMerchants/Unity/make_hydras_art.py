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


# ---------------------------------------------------------------- restyle (2026-11-12)
# Sid's in-game screenshot beside the vanilla Caveling Merchant showed our merchants reading too bright and flat.
# This pass follows the vanilla merchants' look (same treatment as Old Hemlock in make_potions_art.py):
#   - dark, muted 6-7 step ramps per material (all picked from the finisher's vanilla-sampled master palette, so
#     the palette lock keeps them), shaded from a per-pixel light value with ~2x2 cluster grain, never single
#     pixels; only tiny accents (crest crystals, ember horn tips, eyes, the pendant, gold glints) are bright;
#   - no separate outline colour: every part's inset silhouette ring is the darkest step of its own ramp (one step
#     lighter on the lit top/left side);
#   - the three heads keep their elements but are muted: moss-green leaves, a slate-blue ice head with a small
#     pale crystal crest, a charcoal fire head with dull lava cracks and two ember-tipped horns;
#   - thin, snaking 2-3 px necks and a slim curled tail tip carry the height (the props), and the coil is a
#     rope-like spiral of two loops with scale seams instead of one wide, lit plate;
#   - the glow (Merchant_Light) is only the crest crystals, ember tips, lava cracks and eyes (~8 px a frame).
M = {k: hexc(v) for k, v in dict(
    # slate scales (coil, torso)
    S0='1c3b31', S1='2a4e4a', S2='316471', S3='39728b', S4='3d7983', S5='4b708a', S6='677fae',
    # bone belly plates
    B0='474130', B1='645d53', B2='8f7b77', B3='a69298',
    # nature head: deep moss greens, warm leaf tips
    N0='0e2522', N1='073c31', N2='0f5630', N3='2b5432', N4='356a2c', N5='4c8d32', N6='5ca537',
    NL1='356a2c', NL2='5ca537', NL3='71a241',
    # ice head: slate-blue, pale crystals
    I0='0f2960', I1='142b5c', I2='316471', I3='39728b', I4='4883d4', I5='599dbb', I6='90a0c4',
    IC1='3d7983', IC2='62b9c5', IC3='a5cada',
    # fire head: charcoal with dull lava
    F0='201d15', F1='373446', F2='474453', F3='655561', F4='82706d', F5='a69298',
    FL0='612624', FL1='91210b', FL2='c5460a', FL3='da7910',
    # gold torc / glints
    G0='39341f', G1='614822', G2='a88b37', G3='c9ab51', G4='e0c961',
    # purple sash
    U0='1f102a', U1='2b213b', U2='4c376f', U3='723e8a', U4='895392',
    # leather purse
    L0='352a1c', L1='49351d', L2='614822', L3='915b26', L4='b58047',
    # eyes / small accents
    eN='d96fa5', eI='a5cada', eF='e2a12c', tongue='954d94', tooth='a69298', gem='4ad8ae',
).items()}
RAMP = {
    'slate': [M['S%d' % i] for i in range(7)],
    'nature': [M['N%d' % i] for i in range(7)],
    'ice': [M['I%d' % i] for i in range(7)],
    'fire': [M['F%d' % i] for i in range(6)],
    'gold': [M['G%d' % i] for i in range(5)],
    'bone': [M['B%d' % i] for i in range(4)],
    'sash': [M['U%d' % i] for i in range(5)],
    'leather': [M['L%d' % i] for i in range(5)],
}


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def qv(v, ramp, lo=2, hi=None):
    """Quantise light value v (0..1) onto ramp[lo:hi] (the darkest steps stay for edges and occlusion)."""
    r = RAMP[ramp][lo:hi]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


def border_of(mask):
    return {(x, y) for (x, y) in mask
            if any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}


def shade(cv, mask, ramp, vf, lo=2, hi=None, ring=True):
    """Fill mask from the light function vf(x, y) -> 0..1 (+ cluster grain); inset silhouette ring in the ramp's
    own dark steps (darkest on undersides, one step lighter on the shadow side, two where it faces the light)."""
    r = RAMP[ramp]
    lift = 1 if ramp == 'slate' else 0   # the big slate body keeps mid-dark edges (finish_art darkens them)
    bd = border_of(mask) if ring else set()
    out = {}
    for (x, y) in mask:
        if (x, y) in bd:
            open_ = [(dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (x + dx, y + dy) not in mask]
            if all(d in ((-1, 0), (0, -1)) for d in open_):
                out[(x, y)] = r[2 + lift]   # lit top/left edge
            elif (0, 1) in open_:
                out[(x, y)] = r[lift]       # underside
            else:
                out[(x, y)] = r[1 + lift]
        else:
            out[(x, y)] = qv(vf(x, y) + 0.13 * grain(x, y), ramp, lo, hi)
    for p, c in out.items():
        cv.put(p[0], p[1], c)
    return out


def tube_shade(cv, pts, r0, r1, ramp, lit=0.8):
    """A thin tube along a polyline: lit on its upper-left flank, shaded on the lower-right, inset dark ring."""
    m = tube(pts, r0, r1)
    cen = []
    for i in range(len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        L = math.hypot(x1 - x0, y1 - y0) or 1
        for k in range(6):
            t = k / 6
            cen.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, (x1 - x0) / L, (y1 - y0) / L, (i + t) / (len(pts) - 1)))

    def vf(x, y):
        cx, cy, tx, ty, s = min(cen, key=lambda c: (c[0] - x - 0.5) ** 2 + (c[1] - y - 0.5) ** 2)
        nx, ny = -ty, tx                      # normal; flip it toward the upper-left light
        if nx + ny > 0:
            nx, ny = -nx, -ny
        d = ((x + 0.5 - cx) * nx + (y + 0.5 - cy) * ny) / max(0.8, r0 + (r1 - r0) * s)
        return lit + 0.32 * d - 0.15 * (1 - s)
    return m, shade(cv, m, ramp, vf)


# ---------------------------------------------------------------- heads (facing right, seen from above)
# 9 px wide like vanilla heads. Digits are steps of the head's ramp (0 edge .. 5 lit top plane), letters:
# e eye (glows), n nostril, m mouth line, k open-mouth dark, w tooth, t tongue; a/b/c crest (leaves, crystals or
# horns, per head), l lava crack (glows). The top of the skull is the lit plane, the eye sits low on the side.
TOP = {
    'n': ['.a.ba....',
          'abcbc111.',
          '135544311',
          '134544331',
          '2334433n1',
          '2e3322221'],
    'i': ['..cb.c...',
          '.abcba11.',
          '135544311',
          '134544331',
          '2334433n1',
          '2e3322221'],
    'f': ['.c...c...',
          '.b1lb111.',
          '135l44311',
          '134544331',
          '23344l3n1',
          '2e3322221'],
}
JAW_SHUT = ['.1221mmm1',
            '..111111.']
JAW_OPEN = ['.122wkk11',
            '.122ttk1.',
            '..11111..']
HEAD_W = 9
CREST = {'n': (M['NL1'], M['NL2'], M['NL3']), 'i': (M['IC1'], M['IC2'], M['IC3']),
         'f': (M['F1'], M['FL2'], M['FL3'])}
EYE = {'n': M['eN'], 'i': M['eI'], 'f': M['eF']}
HEAD_RAMP = {'n': 'nature', 'i': 'ice', 'f': 'fire'}
GLOWKEYS = {'n': 'e', 'i': 'ec', 'f': 'ec'}


def head(base, light, key, x, y, open_mouth, flip=False):
    r = RAMP[HEAD_RAMP[key]]
    a, b, c = CREST[key]
    pal = {str(i): r[min(i, len(r) - 1)] for i in range(7)}
    pal.update({'e': EYE[key], 'a': a, 'b': b, 'c': c, 'n': r[0], 'm': r[0], 'k': r[0],
                'w': M['tooth'], 't': M['tongue'], 'l': M['FL1']})
    if key == 'f':
        pal['n'] = M['FL0']
        pal['m'] = M['FL0']
    grid = TOP[key] + (JAW_OPEN if open_mouth else JAW_SHUT)
    blit(base, grid, pal, x, y, flip=flip, light=light, lightkeys=GLOWKEYS[key])


PENDANT = [  # tiny gold-framed tri-gem on the sash (gems glow on the eye layer)
    '.Z.',
    'NIF',
    '.G.',
]
PAL_PENDANT = {'G': M['G1'], 'Z': M['G3'], 'N': M['NL3'], 'I': M['IC3'], 'F': M['FL3']}

PURSE = [  # seen from above: gold tie on top, the bag's upper bulge catches the light
    '.yo.',
    'oLlo',
    'olko',
    '.oo.',
]
PAL_PURSE = {'o': M['L0'], 'l': M['L2'], 'L': M['L3'], 'k': M['L1'], 'y': M['G3']}


# ---------------------------------------------------------------- one frame
# Scale: drawn to the vanilla Caveling Merchant's footprint (whole silhouette <= 28 x 47, ~530-570 opaque px per
# frame, ground row 53, centred on x ~31 like vanilla). The three necks stand in for the vanilla staff/lantern
# height; the centre (ice) head sits near the vanilla lantern glow (~(33,20)).
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

    # ---- coil seen from above: two stacked rope loops of tail on the floor (each a tube: lit top, dark
    # underside, scale seams), the torso rising out of the upper loop. Back halves first, front halves last.
    cx, cy = 31.0, 49.6

    def loop(ocx, ocy, rx, ry, irx, iry):
        outer = ellipse(ocx, ocy, rx, ry)
        inner = ellipse(ocx + 0.3, ocy - 0.4, irx, iry)
        ring = {p for p in outer - inner if p[1] <= GROUND}

        def vf(x, y):
            px, py = (x + 0.5 - ocx) / rx, (y + 0.5 - ocy) / ry
            rho = math.hypot(px, py)
            rin = (irx / rx + iry / ry) / 2
            u = max(0.0, min(1.0, (rho - rin) / max(0.1, 1 - rin)))
            v = 1.0 - 0.85 * abs(u - (0.3 if py < 0 else 0.15)) - 0.22 * px - (0.12 if py > 0.2 else 0)
            th = math.atan2(py, px)
            if (th * 7 / math.pi + band * 0.5) % 2.0 < 0.3:      # scale seam (2-3 px wide cluster)
                v -= 0.3
            return v
        return ring, vf

    big, vbig = loop(cx, cy, 10.0, 4.2, 6.2, 2.0)
    small, vsmall = loop(cx + 0.5, cy - 2.6, 6.8, 2.8, 3.4, 1.0)
    tipd = bez((22.5, 50.5), (18.5, 50.0 - tail), (19.0, 45.0 - tail), 6)
    tube_shade(base, tipd, 1.3, 0.6, 'slate', lit=0.55)
    shade(base, {p for p in big if p[1] < cy - 0.6}, 'slate', vbig)
    shade(base, {p for p in small if p[1] < cy - 3.0}, 'slate', vsmall)

    # ---- torso: squat and foreshortened; the shoulders are the lit top plane
    tcx, tcy = 31.3, 41.0 + by
    torso = ellipse(tcx, tcy, 4.0, 4.6) | ellipse(31.0, 44.4 + by, 4.6, 2.8)
    torso = {p for p in torso if p[1] <= cy - 2}

    def vt(x, y):
        nx, ny = (x + 0.5 - tcx) / 4.6, (y + 0.5 - tcy) / 5.0
        return 0.75 - 0.38 * nx - 0.45 * ny + 0.12 * (1 - nx * nx - ny * ny)
    tor = shade(base, torso, 'slate', vt)
    # bone belly plates on the facing (right) side: 2-row plates with a dark seam between them
    for y in range(37, 47):
        row = sorted(x for (x, yy) in torso if yy == y + by)
        if not row:
            continue
        xr = row[-1] - 1
        for x in (xr - 1, xr):
            if tor.get((x, y + by)) not in (None, RAMP['slate'][0]):
                k = (y - 37) % 3
                c = M['B1'] if k == 2 else (M['B3'] if (k == 0 and x == xr - 1 and y < 42) else M['B2'])
                base.put(x, y + by, c)
    # purple sash from the left shoulder to the right hip (lit upper row, dark lower row)
    for k in range(8):
        x = 27 + round(k * 0.75)
        y = 38 + k + by
        for w, c in enumerate((M['U3'] if k < 4 else M['U2'], M['U1'])):
            if tor.get((x + w, y)) not in (None, RAMP['slate'][0]):
                base.put(x + w, y, c)

    # front halves of the loops over the torso base
    shade(base, {p for p in small if p[1] >= cy - 3.0}, 'slate', vsmall)
    shade(base, {p for p in big if p[1] >= cy - 0.6}, 'slate', vbig)
    blit(base, PURSE, PAL_PURSE, 37, 46 + by)

    # ---- torc around the neck bases, an oval seen from above: back half first, necks, then front half
    torc_outer = ellipse(31.5, 37.2 + by, 4.6, 1.8)
    torc_inner = ellipse(31.5, 36.7 + by, 2.4, 0.6)
    torc = torc_outer - torc_inner
    torc_back = {p for p in torc if p[1] < 36.9 + by}
    torc_front = torc - torc_back

    def vg(x, y):
        return 0.7 - 0.25 * (x + 0.5 - 31.5) / 4.6 - 0.35 * (y + 0.5 - 36.5 - by) / 1.8
    shade(base, torc_back, 'gold', vg, lo=1, hi=4)

    # ---- necks (back to front) with heads: the tall ice neck in the middle (behind), the nature head looks
    # away (left), the fire head faces the customer (right)
    pos = {'n': (18 + hn[0], 22 + hn[1] + by), 'i': (29 + hi[0], 15 + hi[1] + by), 'f': (35 + hf[0], 23 + hf[1] + by)}
    attach = {'n': (pos['n'][0] + HEAD_W - 3, pos['n'][1] + 7), 'i': (pos['i'][0] + 2.5, pos['i'][1] + 7),
              'f': (pos['f'][0] + 2.5, pos['f'][1] + 7)}
    necks = {'n': ((29.0, 37.5 + by), (22.5 + hn[0] * 0.5, 36 + by), 'nature'),
             'i': ((31.5, 37 + by), (27.0 + hi[0] * 0.5, 30 + by), 'ice'),
             'f': ((34.0, 37.5 + by), (40.0 + hf[0] * 0.5, 36 + by), 'fire')}
    for k in ('i', 'n', 'f'):
        sh, ctrl, ramp = necks[k]
        pts = bez(sh, ctrl, attach[k], 12)
        m, filled = tube_shade(base, pts, 2.2, 1.5, ramp)
        if k == 'f':   # lava cracks down the fire neck: short 2-px fissures (glow)
            for t in (5, 9):
                x, y = pts[t]
                for qq in ((int(x), int(y)), (int(x), int(y) + 1)):
                    if qq in m and filled.get(qq) not in RAMP['fire'][:2]:
                        base.put(qq[0], qq[1], M['FL1'])
                        light.put(qq[0], qq[1], M['FL1'])
        elif k == 'n':  # a few darker leaf-scale clusters down the green neck
            for t in (4, 8):
                x, y = pts[t]
                for qq in ((int(x), int(y)), (int(x) + 1, int(y))):
                    if qq in m and filled.get(qq) not in RAMP['nature'][:2]:
                        base.put(qq[0], qq[1], M['NL1'])
        # occlusion under the jaw where the head overhangs the neck
        hx, hy = pos[k]
        for x in range(hx, hx + HEAD_W):
            qq = (x, hy + 8)
            if qq in m:
                base.put(qq[0], qq[1], RAMP[ramp][1])
        head(base, light, k, pos[k][0], pos[k][1], open_[k], flip=(k == 'n'))

    shade(base, torc_front, 'gold', vg, lo=1, hi=4)
    base.put(28, int(38.4 + by), M['G4'])          # small gold glint on the torc's lit front

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
