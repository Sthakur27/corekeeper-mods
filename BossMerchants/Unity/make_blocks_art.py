"""Boss Merchants: art for the `blocks` merchant (Morpha / building blocks).

    python -I BossMerchants/Unity/make_blocks_art.py

Character: GROUT, a slippery slime that lives inside a hollow clay-brick block like a hermit crab in its shell,
with a stone-brick block glued on top by slime mortar, Morpha-style slime spikes, and a mason's trowel.
Rig: SlimeMerchant (texture `Slime_merchant`, 300x120, 30x30 cells, 38 frames, pivot centre).
Vanilla clip frame map (read from the game's AnimationClips):
    SlimeMerchantIdleR 0-9 (8 fps), SlimeMerchantMoveR 10-15 (16 fps),
    SlimeMerchantStand 16-31 then 6-9 (10 fps, the arrival animation), SlimeMerchantWaveR 32-37 (10 fps).
Idol: the Slick Cornerstone, a carved clay-brick cornerstone glazed with a dome of Morpha's slime.

Everything is drawn from code with Core Keeper's shared palette colours; no vanilla pixels are copied.
The preview sheet places vanilla reference art (build/bossmerchants, extracted locally) next to ours for review
only; it is skipped if that folder is missing.  Exit code 1 if any validation fails.
"""
import json, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
ROOT = os.path.dirname(MOD)
KEY = 'blocks'
ART = os.path.join(HERE, 'Art')
OUT_MERCH = os.path.join(ART, 'merchants', KEY)
OUT_IDOL = os.path.join(ART, 'idols')
OUT_PREV = os.path.join(ART, 'previews')
REF = os.path.join(ROOT, 'build', 'bossmerchants')

SHEET_W, SHEET_H, CELL, NFRAMES, COLS = 300, 120, 30, 38, 10
GROUND = 25          # lowest opaque row inside a cell (same as vanilla Slime Merchant)


def hx(s):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


# Core Keeper shared palette (sampled colour values, used as a palette only)
SLIME = dict(out=hx('142b5c'), d=hx('143eab'), m=hx('165dd9'), l=hx('1885d8'), c=hx('19bdc6'),
             t=hx('4ad8ae'), p=hx('94f6dc'), w=hx('ffffff'))
CLAY = dict(out=hx('5e2a28'), d=hx('824223'), m=hx('c16436'), l=hx('e88b69'), t=hx('fca895'), p=hx('ecc1bf'))
STONE = dict(out=hx('0e2522'), d=hx('2a4e4a'), m=hx('47677e'), l=hx('677fae'), t=hx('a1aedd'), p=hx('d7dcfb'))
STONE_TOP = dict(out=hx('0e2522'), d=hx('2a4e4a'), m=hx('677fae'), l=hx('a1aedd'), t=hx('a1aedd'), p=hx('d7dcfb'))
WOOD = dict(out=hx('614822'), d=hx('915b26'), m=hx('a98963'), l=hx('d4b987'), t=hx('fae6bc'))
STEEL = dict(out=hx('2a4e4a'), d=hx('677fae'), m=hx('a1aedd'), l=hx('d7dcfb'), w=hx('ffffff'))
CAVE = hx('0b1917')
GLOW = dict(d=hx('19bdc6'), l=hx('94f6dc'), w=hx('ffffff'))


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = {}
        self.clip_y = None   # pixels with y > clip_y are dropped (used to sink things into the slime pool)

    def put(self, x, y, c):
        if c is None or not (0 <= x < self.w and 0 <= y < self.h):
            return
        if self.clip_y is not None and y > self.clip_y:
            return
        self.px[(x, y)] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, c)

    def blit(self, img, ox, oy):
        for (x, y), c in self.px.items():
            img.putpixel((ox + x, oy + y), c)


def ellipse(cx, cy, rx, ry):
    pts = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if rx > 0 and ry > 0 and ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                pts.add((x, y))
    return pts


def fill_slime(cv, mask, shine=True, edge=True):
    """Slime blob: dark outline, ramp lit from the top-left, one specular sparkle."""
    if not mask:
        return
    xs = [p[0] for p in mask]; ys = [p[1] for p in mask]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    for (x, y) in mask:
        if edge and any(n not in mask for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))):
            cv.put(x, y, SLIME['out'])
            continue
        t = ((x - x0) / max(1, x1 - x0)) * 0.45 + ((y - y0) / max(1, y1 - y0)) * 0.75
        c = SLIME['c'] if t < 0.28 else SLIME['l'] if t < 0.55 else SLIME['m'] if t < 0.85 else SLIME['d']
        cv.put(x, y, c)
    if shine and x1 - x0 >= 3 and y1 - y0 >= 2:
        cv.put(x0 + 1 + (x1 - x0) // 4, y0 + 1, SLIME['p'])


def block(cv, x, y, w, top, front, P, bricks=True, lit=True, courses=3):
    """A Core Keeper wall-block: coloured outline, lighter top face, front face with brick courses."""
    x1, y1 = x + w - 1, y + top + front
    cv.rect(x, y, x1, y1, P['out'])
    for xx in range(x + 1, x + 1 + w // 3):                          # outline breaks on the lit edge
        cv.put(xx, y, P['d'])
    for yy in range(y + 1, y + top):
        cv.put(x, yy, P['d'])
    cv.rect(x + 1, y + 1, x1 - 1, y + top, P['t'])                  # top face
    for xx in range(x + 1, x1):
        cv.put(xx, y + top, P['l'])                                  # front edge of the top face
    if lit:
        cv.rect(x + 1, y + 1, x + 1 + (w - 3) // 3, y + 1, P['p'])  # specular rim, top-left
    cv.rect(x + 1, y + top + 1, x1 - 1, y1 - 1, P['m'])              # front face
    for yy in range(y + top + 1, y1):
        cv.put(x1 - 1, yy, P['d'])                                   # shaded right edge
    for xx in range(x + 1, x1):
        cv.put(xx, y1 - 1, P['d'])                                   # shaded bottom course
        if front >= 7:
            cv.put(xx, y1 - 2, P['d'] if xx % 6 else P['m'])
    if top >= 5:
        for yy in range(y + 2, y + top):
            cv.put(x1 - 1, yy, P['l'])                               # top plane falls off toward the back-right
        cv.rect(x + 2, y + 2, x + 4, y + 2, P['p'])                  # broad highlight cluster
    if bricks:
        course = 0
        for yy in range(y + top + courses, y1 - 1, courses):
            for xx in range(x + 1, x1):
                cv.put(xx, yy, P['d'])
            off = 3 if course % 2 == 0 else 0
            for xx in range(x + 1 + off, x1 - 1, 6):
                for k in range(1, courses):
                    if yy + k < y1 - 1:
                        cv.put(xx, yy + k, P['d'])
            course += 1
        for xx in range(x + 2, x1 - 1, 6):                           # lit brick tops along first course
            cv.put(xx, y + top + 1, P['l'])


def trowel(cv, hx_, hy, pose):
    """Mason's trowel gripped at (hx_, hy): wooden handle below, broad steel blade above.
    pose 'up' = blade straight up, 'tilt' = blade leaning right, 'back' = leaning left."""
    for k in range(1, 4):                                   # handle
        cv.put(hx_, hy + k, WOOD['l'] if k == 1 else WOOD['m'] if k == 2 else WOOD['d'])
        cv.put(hx_ - 1, hy + k, WOOD['out']); cv.put(hx_ + 1, hy + k, WOOD['out'])
    cv.put(hx_, hy + 4, WOOD['out'])
    lean = {'up': 0, 'tilt': 1, 'back': -1}[pose]
    rows = [0, 1, 1, 2]                                     # blade half-widths, tip first
    for i, hw in enumerate(rows):
        dy = -4 + i
        cxp = hx_ + (lean * (3 - i)) // 2
        for dx in range(-hw - 1, hw + 2):
            if abs(dx) == hw + 1:
                c = STEEL['out']
            elif dx < 0 or (dx == 0 and i < 2):
                c = STEEL['l']
            else:
                c = STEEL['m']
            cv.put(cxp + dx, hy + dy, c)
    cv.put(hx_ + (lean * 3) // 2, hy - 5, STEEL['out'])     # tip
    cv.put(hx_ + (lean * 2) // 2 - 1, hy - 2, STEEL['w'])   # glint
    for dx in range(-3, 4):                                 # blade heel + ferrule
        cv.put(hx_ + dx, hy, STEEL['out'] if abs(dx) == 3 or dx == 0 else STEEL['d'])


STONE_BLOCK = [   # hand-authored 8x9 stone-brick block seen from above: deep bright top plane, short front
    "ODDOOOOO",
    "DPPTTTTO",
    "DPTTTTLO",
    "OTTTTTLO",
    "OTTTTLLO",
    "OLLLDLLO",
    "OMMMDMDO",
    "ODDDDDDO",
    "OOOOOOOO",
]


def stone_block(cv, bx, by):
    key = dict(O=STONE['out'], P=STONE['p'], T=STONE['t'], L=STONE['l'], M=STONE['m'], D=STONE['d'])
    for y, row in enumerate(STONE_BLOCK):
        for x, ch in enumerate(row):
            cv.put(bx + x, by + y, key[ch])


def spikes(cv, base_pts):
    """Little Morpha slime spikes: list of (x, y_base, height)."""
    for x, y, h in base_pts:
        for k in range(h):
            cv.put(x, y - k, SLIME['p'] if k == h - 1 else SLIME['c'])


def character(f):
    """Draw one 30x30 frame from a parameter dict."""
    cv = Canvas(CELL, CELL)
    sy = f.get('sy', 0)              # shell vertical offset (+ = sunk into the pool)
    sq = f.get('sq', 0)              # squash (front rows removed)
    rx = f.get('rx', 11)             # slime pool half width
    sx = f.get('sx', 0)              # whole-body horizontal lean
    clip = f.get('clip')             # arrival: everything of the shell below this row is under the slime

    # slime pool behind everything
    pool = ellipse(15 + sx * 0.5, GROUND - 1.0, rx, f.get('ry', 2.2))
    pool = {p for p in pool if p[1] <= GROUND}
    fill_slime(cv, pool, shine=False)

    # --- clay-brick shell ----------------------------------------------------------------
    X0, W = 6 + sx, 18
    TOP, FRONT = 6, 8 - sq          # 3/4 view: deep top plane, short front face
    top_y = 8 + sy + sq
    cv.clip_y = clip
    block(cv, X0, top_y, W, TOP, FRONT, CLAY)
    fy = top_y + TOP
    ox0, ox1 = X0 + 3, X0 + 13       # mouth (hermit-crab opening), 11 px wide
    oy0 = fy + 2
    oy1 = fy + FRONT
    for y in range(oy0, oy1 + 1):
        for x in range(ox0, ox1 + 1):
            if y == oy0 and x in (ox0, ox1):
                continue
            rim = x in (ox0, ox1) or y == oy0 or (y == oy0 + 1 and x in (ox0 + 1, ox1 - 1))
            cv.put(x, y, CLAY['out'] if rim else CAVE)
    for x in range(ox0 + 1, ox1):
        cv.put(x, oy0 - 1, CLAY['l'])                       # lit arch stones
    cv.put(ox0 + 5, oy0 - 1, CLAY['t'])                     # keystone

    # slime body in the mouth, spilling over the bottom
    face_top = oy0 + 1 + f.get('breath', 0)
    body = set()
    for y in range(face_top, GROUND + 1):
        inset = 2 if y == face_top else 1 if y == face_top + 1 else 0
        grow = 1 if y >= oy1 else 0
        for x in range(ox0 + 1 + inset - grow, ox1 - inset + grow):
            body.add((x, y))
    fill_slime(cv, body, shine=True)
    ex = ox0 + 1 + f.get('ex', 1)
    ey = face_top + 2
    eyes = f.get('eyes', 'open')
    for e in (ex, ex + 4):
        if eyes == 'open':
            for dx, dy, c in ((0, 0, STONE['p']), (1, 0, SLIME['w']), (2, 0, SLIME['w']),
                              (0, 1, SLIME['w']), (1, 1, CAVE), (2, 1, CAVE),
                              (0, 2, STONE['p']), (1, 2, CAVE), (2, 2, SLIME['out'])):
                cv.put(e + dx, ey + dy, c)
        elif eyes == 'happy':
            cv.put(e, ey + 1, SLIME['w']); cv.put(e + 1, ey, SLIME['w']); cv.put(e + 2, ey + 1, SLIME['w'])
        else:  # closed
            for dx in range(3):
                cv.put(e + dx, ey + 1, SLIME['out'])
    if eyes == 'happy' or f.get('mouth'):
        cv.put(ex + 3, ey + 4, SLIME['out']); cv.put(ex + 4, ey + 4, SLIME['out'])
    cv.clip_y = None

    # slime ring in front when sunk (hides the clipped shell bottom)
    if clip is not None:
        ring = set()
        for y in (clip, clip + 1):
            for x in range(int(15 - rx + 2), int(15 + rx - 1)):
                ring.add((x, y))
        fill_slime(cv, ring, shine=False)
        for x in range(int(15 - rx + 3), int(15 - rx + 7)):
            cv.put(x, clip, SLIME['c'])
        cv.put(int(15 - rx + 4), clip, SLIME['p'])

    # pool sheen
    for x in (int(15 - rx + 2), int(15 - rx + 3), int(15 + rx - 4)):
        if (x, GROUND - 1) in pool:
            cv.put(x, GROUND - 1, SLIME['c'])

    # --- stone-brick block on top, glued with slime mortar, crowned with Morpha spikes ---
    tb = f.get('top', (0, 0))
    if tb is not None:
        tx, ty = tb
        TW = 8
        if f.get('top_abs'):
            bx, by = tx, ty
        else:
            bx, by = X0 + 5 + tx, top_y - 4 + ty
        cv.clip_y = f.get('top_clip')
        if not f.get('top_abs'):
            band = set()
            for x in range(bx - 1, bx + TW + 1):
                band.add((x, by + 8)); band.add((x, by + 9))
            band.discard((bx - 1, by + 8)); band.discard((bx + TW, by + 8))
            for (x, y) in band:   # mortar seen from above: lit top lip, darker underside
                cv.put(x, y, SLIME['c'] if y == by + 8 else SLIME['m'] if bx <= x < bx + TW else SLIME['d'])
            cv.put(bx - 2, by + 9, SLIME['out']); cv.put(bx + TW + 1, by + 9, SLIME['out'])
        stone_block(cv, bx, by)
        if not f.get('top_abs') or f.get('crown'):
            crown = ellipse(bx + TW / 2, by + 2.3, 2.9, 1.7)   # an oval resting on the block's top plane
            fill_slime(cv, crown, shine=True)
            spikes(cv, [(bx + 2, by + 1, 2), (bx + 4, by + 1, 2), (bx + 6, by + 1, 1)])
        cv.clip_y = None
        if not f.get('top_abs'):
            d = f.get('drip', 0)
            for x0_, base, mx in ((bx + 1, 1, 3), (bx + 6, 3, 4), (X0 + 2, 0, 2)):
                ln = (d + base) % (mx + 1)
                start = by + 10 if x0_ != X0 + 2 else top_y + TOP
                for k in range(ln):
                    cv.put(x0_, start + k, SLIME['l'] if k < ln - 1 else SLIME['c'])
                if ln:
                    cv.put(x0_, start + ln, SLIME['out'])

    # --- arm (slime pseudopod) + trowel --------------------------------------------------
    arm = f.get('arm', 'rest')
    if arm != 'none':
        ay = face_top + 4
        if arm == 'rest':
            path = [(ox1, ay), (ox1 + 1, ay), (ox1 + 2, ay - 1), (ox1 + 3, ay - 2)]
            hand = (ox1 + 4, ay - 4); tpose = 'up'
        elif arm == 'up1':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 3, ay - 3), (ox1 + 3, ay - 4), (ox1 + 4, ay - 5)]
            hand = (ox1 + 4, ay - 7); tpose = 'tilt'
        elif arm == 'up2':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 2, ay - 3), (ox1 + 2, ay - 4), (ox1 + 2, ay - 5)]
            hand = (ox1 + 2, ay - 7); tpose = 'back'
        elif arm == 'lift':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 2, ay - 3), (ox1 + 3, ay - 4)]
            hand = None; tpose = None
        else:  # 'fwd' (walking)
            path = [(ox1, ay), (ox1 + 1, ay), (ox1 + 2, ay - 1), (ox1 + 3, ay - 1), (ox1 + 4, ay - 2)]
            hand = (ox1 + 4, ay - 3); tpose = 'tilt'
        m = set()
        for (x, y) in path:
            m.add((x, y)); m.add((x, y + 1))
        if hand:
            m.add((hand[0] - 1, hand[1] + 1)); m.add((hand[0] - 1, hand[1] + 2))
        for (x, y) in m:
            c = SLIME['out'] if (x, y + 1) not in m else SLIME['c'] if (x, y - 1) not in m else SLIME['l']
            cv.put(x, y, c)
        if hand:
            trowel(cv, hand[0], hand[1], tpose)
    for (x, y) in f.get('sparkle', []):
        cv.put(x, y, GLOW['w']); cv.put(x - 1, y, GLOW['d']); cv.put(x + 1, y, GLOW['d'])
        cv.put(x, y - 1, GLOW['d']); cv.put(x, y + 1, GLOW['d'])
    return cv


# ------------------------------------------------------------------------------------------------------------
# frame table
IDLE = [  # 0-9: slime breathes, top block bobs on its mortar, drips crawl, one blink
    dict(drip=0),
    dict(drip=0, top=(0, 1)),
    dict(rx=12, breath=1, drip=1, top=(0, 1)),
    dict(rx=12, breath=1, drip=1, top=(0, 1)),
    dict(rx=12, breath=1, drip=2, top=(0, 0)),
    dict(drip=2),
    dict(drip=3, top=(0, -1)),
    dict(drip=3, top=(0, -1), eyes='closed'),
    dict(drip=4),
    dict(drip=4, ex=2),
]
MOVE = [  # 10-15: slime foot surges forward, shell hops and leans
    dict(arm='fwd', ex=2),
    dict(rx=12, sx=1, sy=-1, arm='fwd', ex=2, top=(-1, 0)),
    dict(rx=12, sx=1, sy=-2, arm='fwd', ex=2, top=(-1, 1), drip=1),
    dict(sy=-1, arm='fwd', ex=2, top=(0, 1), drip=1),
    dict(rx=10, sq=1, arm='fwd', ex=2, top=(1, 0), drip=2),
    dict(arm='fwd', ex=2, drip=2),
]
_SUNK = dict(eyes='closed', arm='none', top_abs=True, top_clip=GROUND - 3)
STAND = [  # 16-31: arrival. Rises out of a slime pool, then lifts its top block into place.
    dict(_SUNK, rx=13, ry=2.6, sy=12, clip=GROUND - 3, top=(17, 20)),
    dict(_SUNK, rx=13, ry=2.6, sy=11, clip=GROUND - 3, top=(17, 19)),
    dict(_SUNK, rx=13, ry=2.6, sy=9, clip=GROUND - 3, top=(17, 19)),
    dict(_SUNK, rx=13, ry=2.5, sy=6, clip=GROUND - 3, top=(17, 18)),
    dict(_SUNK, rx=12, ry=2.4, sy=3, clip=GROUND - 3, top=(17, 18)),
    dict(_SUNK, rx=12, sq=1, top=(17, 17)),
    dict(rx=12, eyes='open', arm='lift', top=(18, 12), top_abs=True),
    dict(rx=11, eyes='open', arm='lift', top=(14, 2), top_abs=True),
    dict(eyes='open', arm='lift', top=(0, -3)),
    dict(rx=12, sq=1, eyes='closed', arm='rest', top=(0, 1)),
    dict(rx=12, eyes='happy', arm='rest', drip=0, sparkle=[(26, 3)]),
    dict(eyes='happy', arm='rest', top=(0, -1), drip=1, sparkle=[(27, 2)]),
    dict(eyes='happy', arm='rest', drip=2),
    dict(eyes='open', arm='rest', drip=3),
    dict(eyes='open', arm='rest', drip=4),
    dict(eyes='open', arm='rest', drip=0),
]
WAVE = [  # 32-37: raises the trowel and waves it, happy eyes
    dict(eyes='happy', arm='up1'),
    dict(eyes='happy', arm='up2', top=(0, -1), breath=1, sparkle=[(26, 2)]),
    dict(eyes='happy', arm='up1'),
    dict(eyes='happy', arm='up2', top=(0, -1), breath=1),
    dict(eyes='open', arm='up1', mouth=True),
    dict(eyes='open', arm='rest', mouth=True),
]
FRAMES = IDLE + MOVE + STAND + WAVE
assert len(FRAMES) == NFRAMES, len(FRAMES)


def make_sheet():
    img = Image.new('RGBA', (SHEET_W, SHEET_H), (0, 0, 0, 0))
    for i, f in enumerate(FRAMES):
        c, r = i % COLS, i // COLS
        character(f).blit(img, c * CELL, r * CELL)
    return img


# ------------------------------------------------------------------------------------------------------------
# idol: Slick Cornerstone (16x16 icon, ~10x11 small icon, placed = icon)
IDOL_ROWS = [
    # 0123456789abcdef     clay-brick cornerstone seen from above; a dome of Morpha's slime sits on its top plane
    "................",  # 0
    "......c..p......",  # 1  spike nubs
    ".....occpco.....",  # 2
    "....occppllo....",  # 3
    "..QQocllllmoOO..",  # 4  back edge of the top plane
    "..QPolllllmdTO..",  # 5
    "..QTolllmmmdTO..",  # 6
    "..QTodmmmmddTO..",  # 7
    "..QTTodddddoTO..",  # 8  dome's darker underside
    "..OTLLoooooLLO..",  # 9  occlusion on the top plane under the dome
    "..OTTTTTclTTLO..",  # a  slime running toward the front edge
    "..OLLLLLLlLLLO..",  # b  front edge
    "..OMMgMMMcMMDO..",  # c  short front face with a glowing glyph
    "..OMgGgMMMMMDO..",  # d
    "..ODDgDDDDDDDO..",  # e  darker toward the ground
    "...OOOOOOOOOO...",  # f
]
IDOL_KEY = {
    '.': None, 'o': SLIME['out'], 'd': SLIME['d'], 'm': SLIME['m'], 'l': SLIME['l'], 'c': SLIME['c'],
    'p': SLIME['p'],
    'O': CLAY['out'], 'Q': CLAY['d'], 'D': CLAY['d'], 'M': CLAY['m'], 'L': CLAY['l'], 'T': CLAY['t'], 'P': CLAY['p'],
    'Z': STONE['out'], 'z': STONE['d'], 'y': STONE['m'], 'x': STONE['l'], 'v': STONE['t'],
    'g': GLOW['d'], 'G': GLOW['l'],
}
SMALL_ROWS = [
    "................",
    "................",
    "................",
    ".......c.p......",
    "......occpo.....",
    "....QQollmdoO...",
    "....QTolmmdoO...",
    "....QTTodddoO...",
    "....OTLooooLO...",
    "....OTTTclTLO...",
    "....OLLLLlLLO...",
    "....OMgGgcMDO...",
    "....ODDgDDDDO...",
    ".....OOOOOOO....",
    "................",
    "................",
]


def grid(rows, key):
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (y, row)
        for x, ch in enumerate(row):
            c = key[ch]
            if c is not None:
                img.putpixel((x, y), c)
    return img


def shift_x(img, dx):
    out = Image.new('RGBA', img.size, (0, 0, 0, 0))
    out.paste(img, (dx, 0))
    return out


# ------------------------------------------------------------------------------------------------------------
# validation
def pixels(im):
    return list(im.get_flattened_data()) if hasattr(im, 'get_flattened_data') else list(im.getdata())


def validate(sheet, icon, small, placed):
    errs = []
    if sheet.size != (SHEET_W, SHEET_H):
        errs.append('sheet size %s' % (sheet.size,))
    used = set()
    for i in range(NFRAMES):
        used.add((i % COLS, i // COLS))
    for y in range(sheet.height):
        for x in range(sheet.width):
            a = sheet.getpixel((x, y))[3]
            if a not in (0, 255):
                errs.append('soft alpha at %d,%d' % (x, y)); break
            if a and (x // CELL, y // CELL) not in used:
                errs.append('pixel outside frame cells at %d,%d' % (x, y)); break
    for i in range(NFRAMES):
        c, r = i % COLS, i // COLS
        bb = sheet.crop((c * CELL, r * CELL, c * CELL + CELL, r * CELL + CELL)).getbbox()
        if not bb:
            errs.append('empty frame %d' % i)
        elif bb[3] - 1 != GROUND:
            errs.append('frame %d ground row %d != %d' % (i, bb[3] - 1, GROUND))
        elif bb[0] < 1 or bb[2] > CELL - 1 or bb[1] < 0:
            errs.append('frame %d touches the cell edge %s' % (i, bb))
    for name, im in (('icon', icon), ('smallIcon', small), ('placed', placed)):
        if im.size != (16, 16):
            errs.append('%s size %s' % (name, im.size))
        if any(p[3] not in (0, 255) for p in pixels(im)):
            errs.append('%s soft alpha' % name)
        if any(p[3] == 255 and p[:3] == (0, 0, 0) for p in pixels(im)):
            errs.append('%s uses pure black' % name)
    if any(p[3] == 255 and p[:3] == (0, 0, 0) for p in pixels(sheet)):
        errs.append('sheet uses pure black')
    bb = icon.getbbox()
    if bb[3] != 16:
        errs.append('icon does not sit on the bottom edge')
    sb = small.getbbox()
    if not (8 <= sb[2] - sb[0] <= 11 and 8 <= sb[3] - sb[1] <= 12):
        errs.append('smallIcon bbox %s not ~10x11' % (sb,))
    return errs


# ------------------------------------------------------------------------------------------------------------
# preview sheet for Sid
def up(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def cell(sheet, i, w=CELL, h=CELL):
    c, r = i % COLS, i // COLS
    return sheet.crop((c * w, r * h, c * w + w, r * h + h))


def make_preview(sheet, icon, small, placed):
    BG = (28, 30, 44, 255)
    W, H = 1560, 1180
    pv = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(pv)
    TXT = (205, 205, 220, 255)
    d.text((16, 8), 'blocks: GROUT the Slime Mason (SlimeMerchant rig, 3/4 top-down redraw)   idol: Slick Cornerstone (drops from Morpha)', fill=TXT)
    S = 6
    lineup = [('Grout (ours)', cell(sheet, 0))]
    refm = os.path.join(REF, 'reference', 'merchants')
    if os.path.isdir(refm):
        rj = json.load(open(os.path.join(REF, 'reference', 'reference.json')))
        def first(base, tex):
            im = Image.open(os.path.join(refm, base, tex + '.png')).convert('RGBA')
            r = rj['merchants'][base]['sheets'][tex][0]['rect']
            c = im.crop((int(r[0]), int(im.height - r[1] - r[3]), int(r[0] + r[2]), int(im.height - r[1])))
            return c.crop(c.getbbox())
        lineup.append(('Slime Merchant', first('SlimeMerchant', 'Slime_merchant')))
        lineup.append(('Caveling Merchant', first('CavelingMerchant', 'Merchant')))
        lineup.append(('Fishing Merchant', first('FishingMerchant', 'Fisher_merchant')))
        vi = Image.open(os.path.join(refm, 'VoidMerchant', 'voidMerchant_idle.png')).convert('RGBA')
        v = vi.crop((0, 0, 16, vi.height)); lineup.append(('Void Merchant', v.crop(v.getbbox())))
    lineup[0] = (lineup[0][0], lineup[0][1].crop(lineup[0][1].getbbox()))
    # 6x lineup on a shared ground line
    x, base_y = 16, 40 + 48 * S
    for label, im in lineup:
        big = up(im, S)
        pv.alpha_composite(big, (x, base_y - big.height))
        d.text((x, base_y + 6), label, fill=TXT)
        x += max(big.width, 120) + 30
    # 1x lineup
    x1 = x + 20
    d.text((x1, 40), '1x', fill=TXT)
    for label, im in lineup:
        pv.alpha_composite(im, (x1, 120 - im.height)); x1 += im.width + 8
    # animation strips (3x)
    y = base_y + 34
    for title, idxs in (('idle 0-9', range(0, 10)), ('walk 10-15', range(10, 16)), ('wave 32-37', range(32, 38)),
                        ('arrival 16-31 (then idle 6-9)', range(16, 32))):
        d.text((16, y), title, fill=TXT); y += 14
        xx = 16
        for i in idxs:
            pv.alpha_composite(up(cell(sheet, i), 3), (xx, y)); xx += 3 * CELL + 2
        y += 3 * CELL + 10
    # idols
    iy = y + 6
    d.text((16, iy), 'idols at 1x and 8x (perspective self-check): ours first, then Mysterious Idol, Slime Oil, Pile of Chum, Void Merchant Idol; small icons', fill=TXT)
    iy += 18
    vids, vsm = [], []
    ir = os.path.join(REF, 'idol_reference')
    if os.path.isdir(ir):
        for n in ('3930_MysteriousIdol', '3931_SlimeOil', '3932_PileOfChum', '3937_VoidMerchantIdol'):
            vids.append(Image.open(os.path.join(ir, n + '_icon.png')).convert('RGBA'))
            vsm.append(Image.open(os.path.join(ir, n + '_smallIcon.png')).convert('RGBA'))
    row = [icon] + vids
    xx = 16
    for im in row:
        pv.alpha_composite(im, (xx, iy)); xx += 22
    xx += 20
    for im in [small] + vsm:
        pv.alpha_composite(im, (xx, iy)); xx += 22
    xx = 16
    for im in row:
        pv.alpha_composite(up(im, 8), (xx, iy + 24)); xx += 16 * 8 + 12
    pv.alpha_composite(up(small, 8), (xx + 20, iy + 24))
    # placed on a dark stone floor mock, with Grout beside it (4x)
    room = Image.new('RGBA', (96, 48), (0, 0, 0, 255))
    rd = ImageDraw.Draw(room)
    for ty in range(3):
        for tx in range(6):
            rd.rectangle((tx * 16, ty * 16, tx * 16 + 15, ty * 16 + 15), fill=hx('1c3534'))
            rd.line((tx * 16, ty * 16, tx * 16 + 15, ty * 16), fill=hx('2a4e4a'))
            rd.line((tx * 16, ty * 16, tx * 16, ty * 16 + 15), fill=hx('2a4e4a'))
    sh = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse((3, 13, 12, 16), fill=(8, 20, 18, 150))
    room.alpha_composite(sh, (64, 24)); room.alpha_composite(placed, (64, 24))
    room.alpha_composite(cell(sheet, 0), (18, 15))
    pv.alpha_composite(up(room, 4), (W - 96 * 4 - 16, iy - 10))
    d.text((W - 96 * 4 - 16, iy - 26), 'placed idol + Grout on a stone floor mock (4x)', fill=TXT)
    return pv.convert('RGB')


def main():
    for p in (OUT_MERCH, OUT_IDOL, OUT_PREV, os.path.join(MOD, 'docs', 'designs')):
        os.makedirs(p, exist_ok=True)
    sheet = make_sheet()
    icon = grid(IDOL_ROWS, IDOL_KEY)
    small = grid(SMALL_ROWS, IDOL_KEY)
    placed = icon.copy()
    errs = validate(sheet, icon, small, placed)
    sheet_name = '%s__Slime_merchant.png' % KEY
    sheet.save(os.path.join(OUT_MERCH, sheet_name))
    icon.save(os.path.join(OUT_IDOL, KEY + '_icon.png'))
    small.save(os.path.join(OUT_IDOL, KEY + '_smallIcon.png'))
    placed.save(os.path.join(OUT_IDOL, KEY + '_placed.png'))
    # sheets.json: same entry schema as the SlimeMerchant rig entry in skins.json
    skins = json.load(open(os.path.join(ART, 'merchants', 'skins.json')))
    src = next(t for m in skins['merchants'].values() if m.get('baseMerchant') == 'SlimeMerchant'
               for t in m['textures'] if t['replacesTexture'] == 'Slime_merchant')
    entry = dict(src)
    entry['output'] = '%s/%s' % (KEY, sheet_name)
    if len(entry['legacySprites']) != NFRAMES:
        errs.append('skins.json SlimeMerchant entry has %d sprites' % len(entry['legacySprites']))
    for i, s in enumerate(entry['legacySprites']):
        c, r = i % COLS, i // COLS
        want = [float(c * CELL), float(SHEET_H - (r + 1) * CELL), float(CELL), float(CELL)]
        if s['name'] != 'Slime_merchant_%d' % i or [float(v) for v in s['rect']] != want:
            errs.append('rect mismatch for %s: %s vs %s' % (s['name'], s['rect'], want))
    json.dump({'key': KEY, 'rig': 'SlimeMerchantEntity', 'kind': 'legacy', 'textures': [entry]},
              open(os.path.join(OUT_MERCH, 'sheets.json'), 'w'), indent=1)
    make_preview(sheet, icon, small, placed).save(os.path.join(OUT_PREV, KEY + '.png'))
    if errs:
        print('VALIDATION FAILED:')
        for e in errs:
            print('  ' + e)
        sys.exit(1)
    print('ok: %s, idols, sheets.json, preview' % sheet_name)


if __name__ == '__main__':
    main()
