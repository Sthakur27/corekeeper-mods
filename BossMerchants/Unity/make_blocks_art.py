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


# ------------------------------------------------------------------------------------------------------------
# Restyle (2026-11-12, same treatment as Old Hemlock after Sid's in-game screenshot): the previous Grout read as
# a flat salmon box with a saturated blue blob, 2-3 shades per material, a near-black outline all round and one
# wide rectangular mass.  This pass follows the vanilla merchants instead:
#   - muted ramps of 7-8 close steps per material (clay, slime, stone, wood, steel), shaded per pixel from a
#     top-left light and quantised through a deterministic ~2x2 grain, so every surface breaks into small
#     clusters; only tiny accents reach the lightest steps (the stone's top-left corner, the slime's sheen,
#     the trowel's edge), and there is no wide flat light plane any more;
#   - no separate outline colour: each part is ringed with dark steps of its own ramp (one step lighter on the
#     lit top/left side); finish_art.py then darkens and tints edges further;
#   - a narrower, rounder shell (14 px instead of 18, chipped corners) with the mass moved into thin props: a
#     long-handled trowel held out on a slime pseudopod, a thin slime-spike crown, 1 px drips and a lower,
#     thinner slime pool;
#   - no glow layer (the rig's glow is the vanilla flare sprite, unchanged); the sparkle is two pale pixels.
RAMP = {   # dark -> light; fills use the middle steps, the darkest are kept for rings / occlusion
    'clay':  ['2a1814', '45231e', '5c3026', '74402e', '8a5236', 'a1683f', 'bd8458', 'd9a983'],
    'slime': ['142b5c', '273fa7', '365dd5', '4883d4', '6e9bb9', '88a2ee', 'a5cada', 'd7dcfb'],
    'stone': ['232930', '3a3a3a', '4a4a4a', '595959', '6e6e6e', '878787', 'a0a0a0', 'c0bacf'],
    'wood':  ['2e2216', '49351d', '614822', '7a5527', '915b26', 'b58047'],
    'steel': ['2b3439', '3c4a52', '667b84', '8195a0', 'a4b6c0', 'cbd7dd'],
}
RAMP = {k: [hx(c) for c in v] for k, v in RAMP.items()}
EYE_W, EYE_P = hx('d7dcfb'), hx('16203a')


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def qv(v, ramp, lo=2, hi=None):
    r = RAMP[ramp][lo:hi]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


def part(cv, pix, ramp, ring=True, chip=()):
    """Draw {(x, y): colour} ringed by its own ramp's dark steps (step 1 on bottom/right, step 2 on top/left)."""
    if ring:
        r = RAMP[ramp]
        rp = {}
        for (x, y) in pix:
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + a, y + b)
                if p in pix:
                    continue
                if (a, b) in ((-1, 0), (0, -1)):
                    rp.setdefault(p, r[2])
                else:
                    rp[p] = r[1]
        for p in chip:
            rp.pop(p, None)
        for (x, y), c in rp.items():
            cv.put(x, y, c)
    for (x, y), c in pix.items():
        cv.put(x, y, c)


def slime_mass(mask, bias=0.0, sheen=True, shade_above=None):
    """Slime shading for a mask: top-left light, darker toward the bottom/right, grainy, a 2 px sheen."""
    if not mask:
        return {}
    xs = [p[0] for p in mask]; ys = [p[1] for p in mask]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    out = {}
    for (x, y) in mask:
        tx = (x - x0) / max(1, x1 - x0); ty = (y - y0) / max(1, y1 - y0)
        v = 1.06 + bias - 0.62 * ty - 0.3 * tx + 0.16 * grain(x, y)
        if shade_above is not None and y <= shade_above:
            v -= 0.3 * (1.0 - (y - y0) / max(1, shade_above - y0 + 1))   # occluded under the shell's arch
        out[(x, y)] = qv(v, 'slime', 2, 7)
    if sheen and x1 - x0 >= 4 and y1 - y0 >= 2:
        sx_, sy_ = x0 + 1 + (x1 - x0) // 5, y0 + 1
        for p in ((sx_, sy_), (sx_ + 1, sy_)):
            if p in out:
                out[p] = RAMP['slime'][6]
    return out


def clay_shell(cv, X0, top_y, W, TOP, FRONT):
    """Hollow clay-brick block in 3/4 view: grainy top plane with a few brick joints, front face in staggered
    courses darkening toward the ground, chipped corners."""
    x1 = X0 + W - 1
    fy = top_y + TOP
    y1 = fy + FRONT
    pix = {}
    for y in range(top_y, y1 + 1):
        for x in range(X0, x1 + 1):
            if (x, y) in ((X0, top_y), (x1, top_y), (x1, y1)):
                continue                                         # chipped corners
            g = 0.15 * grain(x, y)
            if y < fy:                                           # top plane (seen from above)
                ty = (y - top_y) / max(1, TOP - 1); tx = (x - X0) / (W - 1)
                v = 1.0 - 0.22 * ty - 0.4 * tx + g
                r_ = y - top_y                                   # two brick courses seen from above
                if r_ == 2 or (x - X0 + (3 if r_ > 2 else 0)) % 6 == 5:
                    v -= 0.2                                     # joints, one step darker than the brick face
                c = qv(v, 'clay', 3, 8)
            else:                                                # front face: brick courses
                ty = (y - fy) / max(1, FRONT); tx = (x - X0) / (W - 1)
                v = 1.1 - 0.45 * ty - 0.3 * tx + g
                c = qv(v, 'clay', 3, 8)
                row = y - fy
                if row == 0:
                    c = qv(v + 0.2, 'clay', 3, 7)                # front edge of the top plane catches light
                elif row % 3 == 0:
                    c = RAMP['clay'][2]                          # mortar course
                elif (x - X0 + (3 if (row // 3) % 2 else 0)) % 6 == 0:
                    c = RAMP['clay'][2]                          # staggered head joints
            pix[(x, y)] = c
    part(cv, pix, 'clay')
    # a small warm highlight cluster on the lit top-left corner of the top plane
    for p in ((X0 + 1, top_y + 1), (X0 + 2, top_y + 1), (X0 + 1, top_y + 2)):
        cv.put(p[0], p[1], RAMP['clay'][7])
    return fy, y1


def stone_block(cv, bx, by, w=7, top=4, front=3):
    """Stone-brick block seen from above: lit top plane, short front face with one mortar course."""
    pix = {}
    for y in range(by, by + top + front):
        for x in range(bx, bx + w):
            tx = (x - bx) / (w - 1)
            g = 0.15 * grain(x, y)
            if y < by + top:
                v = 1.0 - 0.3 * (y - by) / top - 0.4 * tx + g
                c = qv(v, 'stone', 3, 7)
                if y == by + 2 and x == bx + 3:
                    c = RAMP['stone'][3]
            else:
                v = 0.7 - 0.35 * (y - by - top) / front - 0.3 * tx + g
                c = qv(v, 'stone', 3, 6)
                if y == by + top + 1 and (x - bx) % 4 == 2:
                    c = RAMP['stone'][2]
            pix[(x, y)] = c
    part(cv, pix, 'stone', chip=((bx - 1, by - 1), (bx + w, by - 1)))
    for p in ((bx, by), (bx + 1, by)):
        cv.put(p[0], p[1], RAMP['stone'][7])


def trowel(cv, hx_, hy, pose):
    """Mason's trowel gripped at (hx_, hy): long thin wooden handle below, narrow steel blade above."""
    W_ = RAMP['wood']; S_ = RAMP['steel']
    for k in range(1, 5):                                   # 2 px handle: lit left, shaded right
        cv.put(hx_, hy + k, W_[5] if k == 1 else W_[4] if k < 3 else W_[3])
        cv.put(hx_ + 1, hy + k, W_[2] if k < 3 else W_[1])
    cv.put(hx_, hy + 5, W_[1]); cv.put(hx_ + 1, hy + 5, W_[0])
    lean = {'up': 0, 'tilt': 1, 'back': -1}[pose]
    for dx in range(-1, 3):                                 # ferrule / heel
        cv.put(hx_ + dx, hy, S_[1] if dx in (-1, 2) else S_[2])
    rows = [0, 1, 1, 1, 1]                                  # blade half-widths, tip first
    for i, hw in enumerate(rows):
        dy = -5 + i
        cx_ = hx_ + (lean * (4 - i)) // 2
        for dx in range(-hw, hw + 2):
            if dx == -hw:
                c = S_[4] if i < 3 else S_[3]               # lit edge
            elif dx == hw + 1:
                c = S_[1]                                   # shadow edge
            else:
                c = S_[3] if i < 2 else S_[2]
            cv.put(cx_ + dx, hy + dy, c)
    cv.put(hx_ + (lean * 4) // 2, hy - 6, S_[2])            # tip
    cv.put(hx_ + (lean * 4) // 2, hy - 5, S_[5])            # glint (2 px with the lit edge below)


def spikes(cv, base_pts):
    """Thin Morpha slime spikes: list of (x, y_base, height)."""
    S_ = RAMP['slime']
    for x, y, h in base_pts:
        for k in range(h):
            cv.put(x, y - k, S_[6] if k == h - 1 else S_[5] if k == h - 2 else S_[4])
        cv.put(x + 1, y, S_[3])


def character(f):
    """Draw one 30x30 frame from a parameter dict."""
    cv = Canvas(CELL, CELL)
    sy = f.get('sy', 0)              # shell vertical offset (+ = sunk into the pool)
    sq = f.get('sq', 0)              # squash (front rows removed)
    rx = f.get('rx', 11) - 2         # slime pool half width (narrower than before)
    sx = f.get('sx', 0)              # whole-body horizontal lean
    clip = f.get('clip')             # arrival: everything of the shell below this row is under the slime
    S_ = RAMP['slime']

    # slime pool behind everything: a thin oval
    pool = {p for p in ellipse(15 + sx * 0.5, GROUND - 0.6, rx, f.get('ry', 2.2) - 0.5) if p[1] <= GROUND}
    pm = {}
    for (x, y) in pool:
        v = 0.55 - 0.35 * (y - (GROUND - 2)) / 2 - 0.2 * (x - 6) / 18 + 0.15 * grain(x, y)
        pm[(x, y)] = qv(v + 0.2, 'slime', 2, 7)
    part(cv, pm, 'slime', ring=False)
    for (x, y) in pool:                                       # rim of the pool in its own dark step
        if (x, y + 1) not in pool or (x + 1, y) not in pool or (x - 1, y) not in pool:
            cv.put(x, y, S_[1] if (x, y - 1) in pool else S_[2])

    # --- clay-brick shell ----------------------------------------------------------------
    X0, W = 8 + sx, 14
    TOP, FRONT = 5, 8 - sq          # 3/4 view: deep top plane, short front face
    top_y = 9 + sy + sq
    cv.clip_y = clip
    fy, y1 = clay_shell(cv, X0, top_y, W, TOP, FRONT)
    ox0, ox1 = X0 + 3, X0 + 10       # mouth (hermit-crab opening), 8 px wide
    oy0 = fy + 2
    oy1 = y1
    for y in range(oy0, oy1 + 1):
        for x in range(ox0, ox1 + 1):
            if y == oy0 and x in (ox0, ox1):
                continue
            rim = x in (ox0, ox1) or y == oy0
            cv.put(x, y, RAMP['clay'][1] if rim else RAMP['clay'][0])
    for x in range(ox0 + 1, ox1):
        cv.put(x, oy0 - 1, RAMP['clay'][5] if x < ox0 + 4 else RAMP['clay'][4])   # lit arch stones
    cv.put(ox0 + 3, oy0 - 1, RAMP['clay'][6]); cv.put(ox0 + 4, oy0 - 1, RAMP['clay'][6])   # keystone

    # slime body in the mouth, spilling over the bottom
    face_top = oy0 + 1 + f.get('breath', 0)
    body = set()
    for y in range(face_top, GROUND + 1):
        inset = 1 if y == face_top else 0
        grow = 1 if y > oy1 else 0
        for x in range(ox0 + 1 + inset - grow, ox1 - inset + grow):
            body.add((x, y))
    part(cv, slime_mass(body, 0.05, shade_above=oy1), 'slime', ring=False)
    ex = ox0 + f.get('ex', 1)
    ey = face_top + 1
    eyes = f.get('eyes', 'open')
    for e in (ex, ex + 4):
        if eyes == 'open':
            cv.put(e, ey, EYE_W); cv.put(e + 1, ey, EYE_W)
            cv.put(e, ey + 1, EYE_W); cv.put(e + 1, ey + 1, EYE_P)
        elif eyes == 'happy':
            cv.put(e, ey + 1, EYE_W); cv.put(e + 1, ey, EYE_W); cv.put(e + 2, ey + 1, S_[6])
        else:  # closed
            cv.put(e, ey + 1, S_[1]); cv.put(e + 1, ey + 1, S_[1])
    if eyes == 'happy' or f.get('mouth'):
        cv.put(ex + 2, ey + 3, S_[1]); cv.put(ex + 3, ey + 3, S_[1])
    cv.clip_y = None

    # slime ring in front when sunk (hides the clipped shell bottom)
    if clip is not None:
        ring = set()
        for y in (clip, clip + 1):
            for x in range(int(15 - rx + 1), int(15 + rx)):
                ring.add((x, y))
        part(cv, slime_mass(ring, -0.05, sheen=False), 'slime', ring=False)
        for x in range(int(15 - rx + 2), int(15 - rx + 5)):
            cv.put(x, clip, S_[5])

    # --- stone-brick block on top, glued with slime mortar, crowned with Morpha spikes ---
    tb = f.get('top', (0, 0))
    if tb is not None:
        tx, ty = tb
        TW = 7
        if f.get('top_abs'):
            bx, by = tx, ty
        else:
            bx, by = X0 + 4 + tx, top_y - 5 + ty
        cv.clip_y = f.get('top_clip')
        if not f.get('top_abs'):
            for x in range(bx - 1, bx + TW + 1):          # mortar band seen from above: lit lip, darker underside
                lit = x < bx + 3
                cv.put(x, by + 8, S_[5] if lit else S_[4])
                if bx <= x < bx + TW:
                    cv.put(x, by + 9, S_[3] if lit else S_[2])
            cv.put(bx - 2, by + 9, S_[2]); cv.put(bx + TW + 1, by + 9, S_[1])
        stone_block(cv, bx, by, TW)
        if not f.get('top_abs') or f.get('crown'):
            crown = ellipse(bx + TW / 2, by + 2.2, 2.6, 1.5)   # an oval of slime resting on the block's top plane
            part(cv, slime_mass(crown, 0.0), 'slime', ring=False)
            spikes(cv, [(bx + 2, by + 1, 3), (bx + 4, by + 1, 2)])
        cv.clip_y = None
        if not f.get('top_abs'):
            d = f.get('drip', 0)
            for x0_, base, mx in ((bx, 1, 3), (bx + 5, 3, 4), (X0 + 1, 0, 2)):
                ln = (d + base) % (mx + 1)
                start = by + 10 if x0_ != X0 + 1 else top_y + TOP
                for k in range(ln):
                    cv.put(x0_, start + k, S_[4] if k < ln - 1 else S_[5])
                if ln:
                    cv.put(x0_, start + ln, S_[2])

    # --- arm (slime pseudopod) + long trowel ---------------------------------------------
    arm = f.get('arm', 'rest')
    if arm != 'none':
        ay = face_top + 3
        if arm == 'rest':
            path = [(ox1, ay), (ox1 + 1, ay), (ox1 + 2, ay), (ox1 + 3, ay - 1), (ox1 + 4, ay - 2), (ox1 + 5, ay - 3)]
            hand = (ox1 + 6, ay - 5); tpose = 'up'
        elif arm == 'up1':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 3, ay - 3), (ox1 + 4, ay - 4), (ox1 + 5, ay - 5)]
            hand = (ox1 + 6, ay - 7); tpose = 'tilt'
        elif arm == 'up2':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 3, ay - 3), (ox1 + 3, ay - 4), (ox1 + 3, ay - 5)]
            hand = (ox1 + 4, ay - 7); tpose = 'back'
        elif arm == 'lift':
            path = [(ox1, ay), (ox1 + 1, ay - 1), (ox1 + 2, ay - 2), (ox1 + 3, ay - 3), (ox1 + 4, ay - 4)]
            hand = None; tpose = None
        else:  # 'fwd' (walking)
            path = [(ox1, ay), (ox1 + 1, ay), (ox1 + 2, ay - 1), (ox1 + 3, ay - 1), (ox1 + 4, ay - 2), (ox1 + 5, ay - 2)]
            hand = (ox1 + 6, ay - 3); tpose = 'tilt'
        m = set()
        for (x, y) in path:
            m.add((x, y)); m.add((x, y + 1))
        if hand:
            m.add((hand[0] - 1, hand[1] + 1)); m.add((hand[0] - 1, hand[1] + 2))
        for (x, y) in m:
            c = S_[2] if (x, y + 1) not in m else S_[5] if (x, y - 1) not in m else S_[4]
            cv.put(x, y, c)
        if hand:
            trowel(cv, hand[0], hand[1], tpose)
    for (x, y) in f.get('sparkle', []):
        cv.put(x, y, S_[7]); cv.put(x, y + 1, S_[6])
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
