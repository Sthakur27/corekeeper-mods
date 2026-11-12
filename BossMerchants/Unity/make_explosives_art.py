"""Boss Merchants: Fizzwick the Molten Bombardier (key `explosives`) + the Igneous Fuse Core idol.

    python -I BossMerchants/Unity/make_explosives_art.py

Everything is drawn from code (shape masks + material rules + small hand-authored grids); no vanilla pixels are
copied. Vanilla sheets are only *read* for the side-by-side preview (build/bossmerchants/reference, if present).

Rig: SlimeMerchant (texture `Slime_merchant`, 300x120, 38 cells of 30x30, pivot centre, ground line y=25).
Drawn in Core Keeper's top-down 3/4 view (docs/perspective_rules.md): visible top planes, oval brims/bases.
Clip frame order (from the game's AnimationClips):
  0-9   SlimeMerchantIdleR      10-15 SlimeMerchantMoveR      32-37 SlimeMerchantWaveR (+ Float)
  16-31 SlimeMerchantStand: 16-19 asleep, 20-31 getting up, then 6-9 of the idle
Fizzwick: a crusted lava slime split off from Igneous (cooled crust plates, molten core showing in cracks and a melt
band at the floor), wearing an iron blast helmet with a lit fuse, soot goggles, a fuse-cord sash with a mini bomb,
a linstock (match pole) held on the back side, and always a bomb in hand. Asleep it melts into a cooling puddle
with the helmet, bomb and linstock set down beside it, then pulls itself back together.
Restyled 2026-11-12 to sit beside the vanilla merchants (see the restyle note above the drawing code).

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


# Idol palette (unchanged by the 2026-11-12 restyle; the idol keeps its approved look)
LAVA = [hx(c) for c in ('#91210b', '#b43221', '#da6210', '#e2a12c', '#f2df3a', '#f7eec7')]
LAVA_OL = hx('#5e2a28')
CRUST = [hx(c) for c in ('#232930', '#383447', '#484454', '#645d53', '#8f7b77')]
STEEL = [hx(c) for c in ('#2a4e4a', '#47677e', '#677fae', '#a1aedd')]
STEEL_OL = hx('#073c31')
LEATHER = [hx(c) for c in ('#39341f', '#614822', '#915b26', '#b58047')]
SPARK = [hx(c) for c in ('#c5460a', '#da6210', '#f2df3a', '#f7eec7')]


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


def ellipse(cx, cy, rx, ry):
    return {(x, y) for x in range(int(cx - rx) - 1, int(cx + rx) + 2) for y in range(int(cy - ry) - 1, int(cy + ry) + 2)
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1.0}


# ---------------------------------------------------------------------------------------------- restyle
# Restyle (2026-11-12, after Sid's in-game screenshot of Old Hemlock beside a vanilla merchant): the previous Fizzwick
# was a wide (25 px) flat-orange lava blob with a near-black outline: one big saturated light area, 1-2-shade fills,
# 60% of the silhouette near-black. This pass follows the vanilla merchants instead:
#   - the body is Igneous' cooled crust: muted warm-grey/mauve plates (7 close steps) shaded in ~2x2 grain clusters;
#     the molten core only shows in the 1 px seams between plates and a dark-red melt band where he meets the floor,
#     so orange is a network of thin lines, not a fill; the brightest lava steps are kept for seam junctions only;
#   - no separate outline colour: each part's silhouette ring is a dark step of its own ramp (one step lighter on the
#     lit top/left), and finish_art.py darkens edges next to solid mass further;
#   - narrower body (~16 px) with the mass moved into thin props: a 2 px linstock (cannoneer's match pole) held up on
#     the back side with a smouldering slow-match, the bomb held out on a thin molten arm, the fuse on the helmet;
#   - glow is only tiny embers: the helmet fuse spark, the slow-match tip and the bomb fuse (2-4 px each); the rig
#     has no Light layer of its own.
def ramp(*hexes):
    return [hx('#' + h) for h in hexes]


# picked from the finisher's master palette (vanilla-sampled) so the palette lock keeps them
CR = ramp('232930', '383447', '484454', '655561', '645d53', '82706d', '997171', 'a69298')   # crust
LV = ramp('2f0000', '5e2a28', '8c130d', 'b43221', 'cf4200', 'e46005', 'e2a12c', 'f2df3a')             # molten
ST = ramp('142b5c', '232930', '2a4e4a', '316471', '4b708a', '677fae', '90a0c4', 'a1aedd', 'd7dcfb')   # iron
LE = ramp('23281e', '39341f', '49351d', '614822', '915b26', 'b58047')                                # rope/wood
IRON = ramp('232930', '383447', '484454', '4b708a', '677fae', '90a0c4', 'a1aedd', 'd7dcfb')       # bomb iron
EMBER = ramp('cf4200', 'e46005', 'f2df3a', 'f7eec7')
SMOKE = hx('#645d53')


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def pick(r, i):
    return r[clamp(int(i), 0, len(r) - 1)]


def ring(cv, mask, r, lit_i=2, dark_i=1, skip=()):
    """Silhouette ring of a part in its own ramp: dark step on the shadow side, one lighter on the lit top/left."""
    for (x, y) in mask:
        if (x, y) in skip or not is_border(mask, x, y):
            continue
        lit = (x - 1, y) not in mask or (x, y - 1) not in mask
        shadow = (x + 1, y) not in mask or (x, y + 1) not in mask
        cv.put(x, y, r[dark_i] if shadow or not lit else r[lit_i])


def body_mask(cx, T, B, A, r1, flare=1.0):
    """Slime mound seen from above: domed top half (radius r1 down from T), a slightly wider lower half spreading
    onto the floor; the last ~3 rows round off into the oval floor contact."""
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
                a = A * (0.97 + 0.16 * math.sin(math.pi * min(1.0, 0.85 * dy / rb))) + flare * (dy / rb) ** 2
                k = max(0.0, (dy - (rb - 4.0)) / 4.3)
                inside = abs(dx) <= a * math.sqrt(max(0.0, 1 - k * k))
            if inside:
                m.add((x, y))
    return m


LIGHT = (-0.3, -0.8, 0.52)   # from above / top-left, camera looking down


def shade(x, y, cx, T, B, A, r1):
    cyc = T + r1
    rb = B + 1 - cyc
    nx = clamp((x + .5 - cx) / (A + .5), -1, 1)
    dy = y + .5 - cyc
    ny = clamp(dy / r1, -1, 1) if dy <= 0 else clamp(0.35 * dy / rb, -1, 1)
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    L = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
    if dy > 0:
        L -= 0.35 * (dy / rb) ** 2.0   # front face darkens toward the floor
    return L


# Designed cracks in the crust (body-local: u = -1..1 across the body, w = rows from the top for a 17-row body),
# each a polyline; the molten core shows through them. They stay off the face (goggles + grin) and the sash.
CRACKS = [
    [(-0.70, 8.0), (-0.86, 10.0), (-0.70, 12.0), (-0.88, 14.0)],        # left flank
    [(-0.70, 12.0), (-0.45, 13.0)],                                     # its branch
    [(0.72, 7.0), (0.60, 9.0), (0.82, 11.0), (0.70, 13.0)],             # right flank
]


def crack_pixels(cx, T, B, A):
    """{(x, y): heat 0..1} for the crack polylines at this body size; heat is highest at the polyline middles."""
    sy = (B - T) / 17.0
    out = {}
    for poly in CRACKS:
        pts = [(cx + u * A, T + w * sy) for u, w in poly]
        segs = len(pts) - 1
        for si in range(segs):
            (x0, y0), (x1, y1) = pts[si], pts[si + 1]
            n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
            for k in range(n + 1):
                t = k / n
                x, y = int(math.floor(lerp(x0, x1, t))), int(math.floor(lerp(y0, y1, t)))
                pos = (si + t) / segs
                out[(x, y)] = max(out.get((x, y), 0.0), 1.0 - abs(pos - 0.5) * 1.6)
    return out


MELT_EDGE = [1, 1, 0, -1, -1, 0, 1, 1, 1, 0, 0, -1, 0, 1]   # designed 2-3 px steps (drips up into the crust)


def draw_body(cv, cx, T, B, A, r1, melt=3, crust=1.0, hot=True, bubble=None, shadow_mask=(), flare=1.0):
    """Crusted Igneous slime. melt = rows of exposed dark melt along the floor; crust < 1 lowers the crust line
    (asleep: more crust, cooling). Returns the body mask."""
    m = body_mask(cx, T, B, A, r1, flare)
    melt_row = B - melt + 1
    cracks = crack_pixels(cx, T, B, A)
    idx = {}
    for (x, y) in m:
        L = shade(x, y, cx, T, B, A, r1)
        v = clamp((L + 0.3) / 1.1, 0, 1)
        g = grain(int(x - cx + 40), y - T)
        if y >= melt_row + MELT_EDGE[int(x - cx + 40) % len(MELT_EDGE)]:   # molten floor band, ragged top edge
            idx[(x, y)] = ('L', clamp(int(3.6 + v * 2.6) + g - (1 if y == B else 0), 3, 6 if hot else 5))
            continue
        if (x, y) in cracks and not is_border(m, x, y):              # molten core showing through a crack
            heat = cracks[(x, y)] * (1.0 if hot else 0.4)
            idx[(x, y)] = ('L', clamp(int(3.0 + heat * 2.6 + v * 1.0), 3, 6 if heat > 0.8 and v > 0.4 else 5))
            continue
        i = 4.5 + v * 4.0 + 1.1 * g
        idx[(x, y)] = ('C', clamp(int(i), 2, 7))
    # plate thickness: the crust row right above a seam is one step darker (plates sit on the melt)
    for (x, y), (k, i) in list(idx.items()):
        below = idx.get((x, y + 1))
        if k == 'C' and below and below[0] == 'L':
            idx[(x, y)] = ('C', max(2, i - 2))
    for p in shadow_mask:                                           # occlusion under the helmet brim
        if p in idx:
            k, i = idx[p]
            idx[p] = (k, max(1, i - 2))
    if bubble:
        bx, by, popped = bubble
        for (px_, py_), c in (((bx, by), LV[6] if popped else LV[5]), ((bx + 1, by), LV[5] if popped else LV[4])):
            if (px_, py_) in m and not is_border(m, px_, py_):
                idx[(px_, py_)] = ('L', 6 if c == LV[6] else 5 if c == LV[5] else 4)
    for (x, y), (k, i) in idx.items():
        cv.put(x, y, pick(CR if k == 'C' else LV, i))
    # silhouette: crust parts ring in crust darks, the melt band in molten darks
    for (x, y) in m:
        if not is_border(m, x, y):
            continue
        k = idx[(x, y)][0]
        lit = ((x - 1, y) not in m or (x, y - 1) not in m) and x + .5 < cx + A * 0.2 and y < T + r1
        if k == 'C':
            cv.put(x, y, CR[5] if lit else CR[3])
        else:
            cv.put(x, y, LV[4] if y < B else LV[3])
    return m


def draw_helmet(cv, hxc, hy, spark=0, lit=True):
    """Iron blast helmet seen from above: oval brim (lit top surface) round a small domed crown with a rivet band and
    a short fuse. Returns (mask, brim shadow)."""
    BR, CRW = 4.8, 3.0
    brim = ellipse(hxc, hy, BR, 1.7)
    dome = {p for p in ellipse(hxc - 0.4, hy - 0.8, CRW, 3.1) if p[1] + .5 <= hy - 0.2}
    for (x, y) in brim:
        if (x, y) in dome:
            continue
        nx = (x + .5 - hxc) / BR
        g = grain(x - int(hxc) + 40, y)
        if y + .5 < hy + 0.4:
            i = 7 - (1 if nx > 0.25 else 0) + (g if g < 0 else 0)
        else:
            i = 4 + (1 if nx < -0.2 else 0)                         # front edge of the brim, in shadow
        cv.put(x, y, pick(ST, i))
    ring(cv, brim - dome, ST, lit_i=6, dark_i=4)
    bottom = {}
    for (x, y) in dome:
        bottom[x] = max(bottom.get(x, -99), y)
    top = min(y for (_, y) in dome)
    for (x, y) in dome:
        nx = (x + .5 - (hxc - 0.4)) / CRW
        if y == bottom[x]:
            c = ST[2] if x % 2 else ST[4]                            # rivet band where the crown meets the brim
        elif y <= top + 1 and nx < 0.2:
            c = ST[7]                                                # crown top: brightest plane, a couple of px
        elif nx > 0.4:
            c = ST[3]
        else:
            c = ST[6] if (x + y) % 3 else ST[5]
        cv.put(x, y, c)
    ring(cv, dome, ST, lit_i=5, dark_i=3, skip={(x, bottom[x]) for x in bottom})
    cv.put(int(hxc - 1.6), top + 1, ST[8])                           # one specular dot
    # short fuse out of the crown (rope), leaning back, then a 2-3 px spark
    wx = int(hxc) + 1
    cv.put(wx, top - 1, LE[3])
    cv.put(wx + 1, top - 2, LE[4])
    tip = (wx + 1, top - 3)
    if not lit:
        cv.put(*tip, CR[3])
        if spark % 2 == 0:
            cv.put(tip[0] + 1, tip[1] - 1, SMOKE)
    else:
        for dx, dy, c in SPARKS[spark % len(SPARKS)]:
            cv.put(tip[0] + dx, tip[1] + dy, c)
    shadow = {(x, y + 1) for (x, y) in brim if (x, y + 1) not in brim}
    return brim | dome, shadow


SPARKS = [
    [(0, 0, EMBER[2]), (0, -1, EMBER[1])],
    [(0, 0, EMBER[3]), (1, -1, EMBER[0])],
    [(0, 0, EMBER[2]), (-1, -1, EMBER[1])],
]


def draw_face(cv, body, cx, gy, open_eyes=True, mouth=True):
    """Soot goggles on a leather strap (two short round lenses: dark glass with a fire glint) and a molten grin."""
    gx = int(round(cx)) - 4
    for x in range(gx - 4, gx + 12):
        if (x, gy + 1) in body and not is_border(body, x, gy + 1):
            cv.put(x, gy + 1, LE[3] if x < cx else LE[2])
    if open_eyes:
        cv.grid(gx, gy, ['.ab..ab.', 'bLkcbLkc', '.cc..cc.'],
                {'a': ST[7], 'b': ST[5], 'c': ST[3], 'L': LV[6], 'k': CR[1]})
    else:
        cv.grid(gx, gy + 1, ['.cc..cc.'], {'c': CR[1]})
    if mouth:
        cv.grid(gx + 2, gy + 4, ['d....d', '.dood.'], {'d': CR[2], 'o': LV[5]})


ROPE = ramp('645d53', 'a98963', 'd4b987', 'fae6bc')


def draw_sash(cv, body, cx, T, B, A):
    """Coiled fuse cord worn as a sash from the back shoulder down to the front hip (2 px, twisted), with two mini
    bombs clipped on: a thin light line across the crust instead of a light area."""
    sy = (B - T) / 17.0
    x0, y0 = cx - A * 0.95, T + 6.0 * sy
    x1, y1 = cx + A * 0.95, B - 3.0
    n = int(abs(x1 - x0)) + 1
    line = []
    for k in range(n + 1):
        x = int(math.floor(lerp(x0, x1, k / n)))
        y = int(math.floor(lerp(y0, y1, k / n)))
        line.append((x, y))
        for dy, c in ((0, ROPE[3] if k % 3 else ROPE[2]), (1, ROPE[1] if k % 3 == 1 else ROPE[2])):
            if (x, y + dy) in body:
                cv.put(x, y + dy, c if not is_border(body, x, y + dy) else ROPE[1] if dy else ROPE[2])
    bx, by = line[int(0.25 * (len(line) - 1))]
    cv.grid(bx - 1, by - 1, ['.ab', 'abc', 'bcd'], {'a': IRON[5], 'b': IRON[4], 'c': IRON[3], 'd': IRON[2]})


BOMB = [                      # sphere lit from above/left: bright top, dark underside, own-ramp rim
    '.3554.',
    '376653',
    '465543',
    '354432',
    '233321',
    '.2221.',
]


def draw_bomb(cv, x0, y0, spark=0, lit=True):
    cv.grid(x0, y0, BOMB, {k: IRON[int(k)] for k in '1234567'})
    cv.put(x0 + 1, y0 + 1, ST[8])
    cv.put(x0 + 3, y0 - 1, LE[3])         # fuse stands up from the top, tilted back
    cv.put(x0 + 4, y0 - 2, LE[4])
    if lit:
        cv.put(x0 + 4, y0 - 3, EMBER[2] if spark % 2 == 0 else EMBER[3])
        if spark % 3 == 1:
            cv.put(x0 + 5, y0 - 4, EMBER[0])
    else:
        cv.put(x0 + 4, y0 - 3, CR[3])


def draw_arm(cv, body, x0, y0, x1, y1):
    """Thin molten pseudopod (2 px) from the body edge to the hand point, crusted on top."""
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        x = int(round(lerp(x0, x1, i / n)))
        y = int(round(lerp(y0, y1, i / n)))
        if (x, y) not in body:
            cv.put(x, y, CR[4] if i % 3 else CR[5])
        if (x, y + 1) not in body:
            cv.put(x, y + 1, LV[3] if i % 2 else LV[2])


def draw_linstock(cv, x, y_top, y_bot, ember=0, lit=True, lean=0):
    """Cannoneer's linstock: 2 px wooden pole (lit left column, shaded right), forked iron head, smouldering
    slow-match coiled round it with a 2 px ember at the tip. lean shifts the top sideways (1 px per ~7 rows)."""
    n = y_bot - y_top
    for k in range(n + 1):
        y = y_top + k
        xx = x + int(round(lean * (1 - k / max(n, 1))))
        knot = k % 6 == 3
        cv.put(xx, y, LE[3] if knot else LE[5])
        cv.put(xx + 1, y, LE[3] if knot else LE[4])
    xt = x + lean
    cv.grid(xt - 1, y_top - 3, ['a..a', 'baab'], {'a': ST[5], 'b': ST[2]})   # forked head
    cv.put(xt - 1, y_top - 1, LE[3])                                         # slow-match wound round the fork
    cv.put(xt + 2, y_top - 1, LE[2])
    if lit:
        cv.put(xt - 1, y_top - 4, EMBER[2] if ember % 2 == 0 else EMBER[1])
        cv.put(xt - 1, y_top - 3, EMBER[0])
        if ember % 3 == 2:
            cv.put(xt - 2, y_top - 5, SMOKE)
    else:
        cv.put(xt - 1, y_top - 4, CR[3])


def draw_linstock_down(cv, x0, x1, y):
    """Linstock lying on the floor behind the puddle (asleep)."""
    for x in range(x0, x1 + 1):
        cv.put(x, y, LE[3] if x % 6 else LE[2])
        cv.put(x, y + 1, LE[1])
    cv.grid(x1 + 1, y - 1, ['ab', 'b.', 'ab'], {'a': ST[5], 'b': ST[2]})


CX = 14.0
A0 = 7.0
R1 = 7.5
TOP = 8          # body top row; helmet crown ~y=6, fuse spark ~y=2
MELT = 3
BOMB_X = 20


def character(T=TOP, B=GROUND, A=A0, r1=R1, cx=CX, melt=MELT, hot=True, spark=0, bubble=None,
              eyes=True, mouth=True, bomb=(BOMB_X, None), arm=True, hand_dy=0, helmet=None, helmet_lit=True,
              belt=True, stick=True, stick_down=False, stick_lean=0, extras=()):
    cv = Canvas(CELL, CELL)
    cy1 = T + r1
    hxc, hy = helmet if helmet else (cx + 0.5, T + 1.5)
    rest_y = int(cy1) + 1
    by = bomb[1] if bomb[1] is not None else rest_y
    if stick_down:
        draw_linstock_down(cv, 3, 21, B - 1)
    probe = Canvas(CELL, CELL)
    _, shadow = draw_helmet(probe, hxc, hy, spark=spark, lit=helmet_lit)
    body = draw_body(cv, cx, T, B, A, r1, melt=melt, hot=hot, bubble=bubble, shadow_mask=shadow)
    if stick:     # held on the back (left) side by a short pseudopod; pole foot on the floor
        sx = int(cx - A0) - 3
        draw_linstock(cv, sx, T - 2 + (1 if B < GROUND else 0), B, ember=spark, lean=stick_lean)
        draw_arm(cv, body, int(cx - A) + 1, int(cy1) + 1, sx + 2, int(cy1))
    if eyes is not None:
        draw_face(cv, body, cx, min(int(hy) + 3, B - 5), open_eyes=eyes, mouth=mouth)
    if belt:
        draw_sash(cv, body, cx, T, B, A)
    draw_helmet(cv, hxc, hy, spark=spark, lit=helmet_lit)
    if bomb[0] is not None:
        if arm:
            draw_arm(cv, body, int(cx + A) - 1, int(cy1) + 2, bomb[0] + 1, min(by + 4, B - 2) + hand_dy)
        draw_bomb(cv, bomb[0], by, spark=spark + 1)
    for x, y, c in extras:
        cv.put(x, y, c)
    return cv.image()


def frames():
    out = []
    # 0-9 idle: breathe (top sinks 1px, spreads), fuse + slow-match flicker, a bubble rises in a seam and pops
    dh = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0]
    bub = {4: (10, 21, False), 5: (10, 20, False), 6: (10, 19, False), 7: (10, 18, False), 8: (10, 17, True)}
    for i in range(10):
        out.append(character(T=TOP + dh[i], A=A0 + 0.4 * dh[i], spark=i % 3, bubble=bub.get(i)))
    # 10-15 move: squash, stretch, hop, molten drips left on the floor
    mv = [(0, 0.0, 0), (1, 0.4, 0), (2, 0.8, 0), (-1, -0.4, -1), (-2, -0.8, -2), (0, 0.0, -1)]
    drips = {3: [(6, 25, LV[3]), (7, 25, LV[2])], 4: [(5, 25, LV[2]), (6, 25, LV[2])], 5: [(5, 25, LV[1])]}
    for i, (dT, dA, lift) in enumerate(mv):
        out.append(character(T=TOP + dT + lift, B=GROUND + lift, A=A0 + dA, spark=i % 3, extras=drips.get(i, ())))
    # 16-19 asleep: a flat cooling crust puddle, helmet + bomb set down beside it, linstock on the floor, fuse smoking
    for i in range(4):
        T = GROUND - 6 - (i % 2)
        out.append(character(T=T, A=9.5, r1=2.5 + 0.5 * (i % 2), melt=1, hot=i % 2 == 0, spark=i, eyes=False,
                             mouth=False, bomb=(BOMB_X + 1, GROUND - 6), arm=False, helmet=(8.5, GROUND - 4.0),
                             helmet_lit=False, belt=False, stick=False, stick_down=True))
    # 20-31 getting up: the puddle gathers into Fizzwick, helmet rides up onto him, goggles on, bomb + linstock picked up
    for k in range(12):
        t = (k + 1) / 12.0
        e = t * t * (3 - 2 * t)
        T = int(round(lerp(GROUND - 6, TOP, e)))
        r1 = lerp(2.5, R1, e)
        holding = t > 0.6
        tgt = (CX + 0.5, T + 1.5)
        hel = (lerp(8.5, tgt[0], e), lerp(GROUND - 4.0, tgt[1], e))
        out.append(character(T=T, A=round(lerp(9.5, A0, e) * 2) / 2, r1=r1, melt=1 if t < 0.5 else MELT,
                             hot=k % 2 == 0, spark=k % 3, eyes=(t >= 0.5) if t >= 0.4 else None, mouth=t >= 0.7,
                             bomb=(BOMB_X, None) if holding else (BOMB_X + 1, GROUND - 6), arm=holding,
                             helmet=hel, helmet_lit=t > 0.45, belt=t >= 0.5, stick=holding, stick_down=not holding))
    # 32-37 wave: tosses the bomb up and catches it, linstock raised a little
    toss = [0, -3, -6, -8, -4, 0]
    hand = [0, -2, -3, -3, -2, 0]
    sq = [0, 1, 0, 0, 0, 1]
    for i in range(6):
        T = TOP + sq[i]
        out.append(character(T=T, A=A0 + 0.4 * sq[i], spark=i % 3,
                             bomb=(BOMB_X, int(T + R1) + 1 + toss[i]), hand_dy=hand[i]))
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
    for gname, g, w in (('BOMB', BOMB, 6),):
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
