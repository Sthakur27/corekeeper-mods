"""Boss Merchants art for the `potions` merchant: Old Hemlock, the Ivy apothecary.

    python -I BossMerchants/Unity/make_potions_art.py

Draws everything from code + hand-authored pixel grids (no vanilla pixels are copied):
  Art/merchants/potions/potions__Merchant.png        CavelingMerchant rig, 640x192, 64x64 cells
  Art/merchants/potions/potions__Merchant_Light.png  unlit overlay (the glowing Ivy brew)
  Art/merchants/potions/potions__Merchant_Eye.png    unlit overlay (the mask's goggle lens)
  Art/merchants/potions/sheets.json
  Art/idols/potions_icon.png, potions_smallIcon.png, potions_placed.png
  Art/previews/potions.png   (review sheet; needs the extracted vanilla reference in build/bossmerchants)
Frame order (vanilla CavelingMerchant clips): 0-9 MerchantIdleR (8 fps), 10-17 MerchantMoveR, 18-23 MerchantWaveR.
The eye sheet has 18 frames; the wave clip holds Merchant_Eye_10, so wave frames keep the head where move frame 0
has it. Exits 1 if validation fails.
"""
import json, math, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
ART = os.path.join(HERE, 'Art')
OUT_M = os.path.join(ART, 'merchants', 'potions')
OUT_I = os.path.join(ART, 'idols')
OUT_P = os.path.join(ART, 'previews')
REF = os.path.join(ROOT, 'build', 'bossmerchants')
KEY = 'potions'

# ---------------------------------------------------------------- palette (Core Keeper colours: Ivy, Azeos greens, glass, copper)
PAL = {
    'k': '1d1429',  # outline, dark purple
    'K': '0c2418',  # outline, dark moss
    'G': '1b3a3e',  # outline, glass
    '1': '1c2c36', '2': '22463f', '3': '2e6343', '4': '50844a', '5': '8fb35c',   # moss robe: cool shadows, warm lights
    '6': '0a6d27', '7': '239029', '8': '5ca537', '9': 'a3ce4a',                  # Azeos vine greens
    'b': '645d53', 'c': '8f7b77', 'd': 'c0bacf', 'e': 'd7dcfb', 'w': 'ffffff',   # bone mask
    'u': '614822', 'v': '915b26', 'x': 'b58047', 'X': 'e8c27e',                  # copper / leather (+ specular)
    'h': '677fae', 'i': 'a1aedd',                                                # glass
    'm': '5e2a28', 'n': 'c76369', 'o': 'de8e9a', 'O': 'ecc1bf',                  # Ivy pink (brew)
    'P': '2e2648', 'p': '4e3c6d', 'q': '723e8a', 'r': '954d94', 'Q': 'b06a9a',   # Ivy purple: indigo shadow, pink light
    'g': '2a4e4a',                                                               # glass outline on lit edges
    'y': 'd9f27e', 'Y': 'a3ce4a',                                                # goggle lens glow
    'f': '2a4e4a',                                                               # dark lens (blink)
}
RGB = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) for k, v in PAL.items()}
GLOW = set('mnoOw')   # brew pixels from the flask layer go to the Light sheet
# exterior outline pixels on the lit (top/left) side take the darkest tone of the material they wrap
LIT = {}
for _chars, _o in (('12345', '1'), ('PpqrQ', 'P'), ('mnoO', 'm'), ('hiew', 'g'), ('bcd', 'b'), ('uvxX', 'u'),
                   ('6789', '6')):
    for _ch in _chars:
        LIT[_ch] = _o

# ---------------------------------------------------------------- 3/4 top-down drawing (camera looks down at the merchant)
# Every part is a pixel mask with a light value per pixel (top planes brightest, light from above/top-left),
# quantised to a vanilla-sampled ramp and drawn back to front with a tinted outline ring.
RAMPS = {'robe': '12345', 'purple': 'PpqrQ', 'pink': 'mnoO', 'bone': 'bcde', 'copper': 'uvxX',
         'glass': 'hie', 'vine': '6789', 'boot': 'uvx'}
OUTLINE = {'robe': 'K', 'purple': 'k', 'pink': 'k', 'bone': 'k', 'copper': 'k', 'glass': 'G', 'vine': 'K',
           'boot': 'k'}


def q(v, ramp):
    r = RAMPS[ramp]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


class Canvas:
    def __init__(self, w=64, h=64):
        self.w, self.h = w, h
        self.px = {}   # (x, y) -> (letter, tag)

    def put(self, x, y, ch, tag=''):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[(x, y)] = (ch, tag)

    def part(self, pix, outline, tag='', ring=True):
        """pix: {(x, y): letter}. Draws a 1 px outline ring around the mask first (over what is behind)."""
        if ring:
            for (x, y) in pix:
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if (x + a, y + b) not in pix:
                        self.put(x + a, y + b, outline, tag)
        for (x, y), ch in pix.items():
            self.put(x, y, ch, tag)

    def relight_outlines(self):
        new = {}
        for (x, y), (ch, tag) in self.px.items():
            if ch not in 'kKG':
                continue
            if (x, y - 1) in self.px and (x - 1, y) in self.px:
                continue   # not on a top/left exterior edge
            for nb in ((x + 1, y), (x, y + 1)):
                m = self.px.get(nb, ('',))[0]
                if m in LIT:
                    new[(x, y)] = (LIT[m], tag)
                    break
        self.px.update(new)

    def images(self):
        self.relight_outlines()
        main = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        light = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        eye = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        for (x, y), (ch, tag) in self.px.items():
            c = RGB[ch] + (255,)
            main.putpixel((x, y), c)
            if tag == 'flask' and ch in GLOW:
                light.putpixel((x, y), c)
            if tag == 'eye':
                eye.putpixel((x, y), c)
        return main, light, eye


def ellipse(cx, cy, rx, ry):
    out = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def frustum(cx, ytop, rx0, ry0, ybot, rx1, ry1, hem_dx=0):
    """A cone/bell seen from above: oval top plane, sides, oval bottom edge. Returns (pixels, top_plane)."""
    body = set()
    for y in range(int(ytop), int(ybot) + 1):
        t = (y - ytop) / (ybot - ytop)
        rx = rx0 + (rx1 - rx0) * t
        c = cx + hem_dx * t * t
        for x in range(int(c - rx - 1), int(c + rx + 2)):
            if abs(x - c) <= rx:
                body.add((x, y))
    top = ellipse(cx, ytop, rx0, ry0)
    bottom = {p for p in ellipse(cx + hem_dx, ybot, rx1, ry1) if p[1] >= ybot}
    return body | top | bottom, top


# Scale: drawn to the vanilla CavelingMerchant body (Sid's in-game note 2026-11-12: the first pass rendered bulkier
# than vanilla merchants and the player). Budget per frame: <= 26 px wide, <= 47 tall, ~575 opaque px like vanilla;
# feet centred under the pivot (x 32) on the vanilla ground row 53.
def draw_robe(cv, dy, hem_dx):
    """Moss robe: a squat bell. The hem is an oval that curves toward the viewer; folds run down the front."""
    cx, ytop, ybot = 32, 38 + dy, 48 + dy
    rx0, rx1, ry1 = 5.1, 7.2, 2.5
    pix, top = frustum(cx, ytop, rx0, 1.7, ybot, rx1, ry1, hem_dx)
    out = {}
    for (x, y) in pix:
        t = (y - ytop) / (ybot + ry1 - ytop)
        nx = (x - cx - hem_dx * t * t) / rx1
        v = 0.8 - 0.55 * t - 0.32 * nx
        # folds: two soft creases following the bell
        for f in (-0.3, 0.32):
            fx = cx + f * (rx0 + (rx1 - rx0) * t) + hem_dx * t * t
            if y > ytop + 3 and abs(x - fx) < 0.6:
                v -= 0.22
            elif y > ytop + 3 and 0.6 <= fx - x < 1.6:
                v += 0.1
        out[(x, y)] = q(v, 'robe')
    # Ivy stain along the hem's front oval
    for (x, y) in list(out):
        hem_y = ybot + ry1 * math.sqrt(max(0.0, 1 - ((x - cx - hem_dx) / rx1) ** 2))
        if y >= hem_y - 1.1:
            out[(x, y)] = 'o' if x < cx - 2 + hem_dx else 'n' if x < cx + 3 + hem_dx else 'm'
    # drips (1 px under the stain)
    for dxp in (-4, 3):
        x = cx + dxp + hem_dx
        hy = int(ybot + ry1 * math.sqrt(max(0.0, 1 - (dxp / rx1) ** 2)))
        out[(x, hy + 1)] = 'n' if dxp < 0 else 'm'
    cv.part(out, 'K')
    # belt: a copper band following the body's oval (curves down at the front)
    belt = {}
    by, brx = 41 + dy, 6.0
    for x in range(int(cx - brx), int(cx + brx) + 1):
        yy = by + 1.7 * math.sqrt(max(0.0, 1 - ((x - cx) / (brx + 0.5)) ** 2))
        nx = (x - cx) / brx
        belt[(x, int(round(yy)))] = 'X' if x == cx - 3 else 'x' if nx < -0.3 else 'v' if nx < 0.5 else 'u'
        belt[(x, int(round(yy)) + 1)] = 'u' if nx > 0.2 else 'v'
    for p, ch in belt.items():
        cv.put(p[0], p[1], ch)
    # two little vials hanging off the belt: cork tops seen from above
    for (vx, liquid) in ((cx - 4, 'onm'), (cx + 2, 'ihg')):
        vy = int(round(by + 1.7 * math.sqrt(max(0.0, 1 - ((vx - cx) / 6.5) ** 2)))) + 2
        cv.part({(vx, vy): 'X', (vx + 1, vy): 'u', (vx, vy + 1): liquid[0], (vx + 1, vy + 1): liquid[1],
                 (vx, vy + 2): liquid[1], (vx + 1, vy + 2): liquid[2]}, 'K')


def draw_capelet(cv, dy):
    """Purple capelet over the shoulders; its top plane faces the camera and catches the light."""
    cx = 32
    pix, top = frustum(cx, 35 + dy, 4.7, 1.9, 38 + dy, 6.8, 2.2)
    out = {}
    for (x, y) in pix:
        t = (y - (35 + dy)) / 5.4
        nx = (x - cx) / 6.8
        v = 0.95 - 0.75 * t - 0.35 * nx
        out[(x, y)] = q(v, 'purple')
    for (x, y) in list(out):   # pink trim along the front edge
        edge = 38 + dy + 2.2 * math.sqrt(max(0.0, 1 - ((x - cx) / 6.8) ** 2))
        if y >= edge - 0.8:
            out[(x, y)] = 'o' if x < cx - 3 else 'n' if x < cx + 3 else 'm'
    cv.part(out, 'k')


def draw_head(cv, dy, blink):
    """Hood seen from above: a big lit crown, the bone beak mask low at the front, beak tilting down."""
    cx, cy = 33, 29 + dy
    rx, ry = 6.0, 5.3
    hood = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nxv, nyv = (x - cx) / rx, (y - cy) / ry
        v = 0.46 - 0.32 * nxv - 0.6 * nyv + 0.18 * (1 - nxv * nxv - nyv * nyv)
        hood[(x, y)] = q(v, 'purple')
    # drooping tip curling back over the flask
    for (x, y), v in (((26, 24), 0.95), ((27, 24), 0.9), ((26, 25), 0.8), ((27, 25), 0.85), ((28, 25), 0.9),
                      ((28, 26), 0.8), ((29, 26), 0.85), ((25, 24), 0.7)):
        hood[(x, y + dy)] = q(v, 'purple')
    # top plane of the hood catches the light: a warm highlight cluster on the crown
    for (x, y) in ((30, 25), (31, 25), (32, 25), (29, 26), (30, 26), (31, 26), (30, 27)):
        if (x, y + dy) in hood:
            hood[(x, y + dy)] = 'Q'
    cv.part(hood, 'k')
    # face opening: shadowed hollow under the hood's front brim, mask set low into it
    for p in ellipse(36.0, 32.6 + dy, 3.9, 2.5):
        cv.put(p[0], p[1], 'P')
    mask = {}
    for (x, y) in ellipse(36.8, 33.3 + dy, 3.4, 2.1):
        mask[(x, y)] = 'e' if y < 32.5 + dy else 'd' if y < 34 + dy else 'c'
    # beak: tapered wedge pointing right and down; lit top ridge, dark underside
    for i in range(5):
        x = 38 + i
        y0 = int(round(32 + dy + i * 0.5))
        h = 3 if i < 2 else 2 if i < 4 else 1
        for j in range(h):
            mask[(x, y0 + j)] = 'e' if j == 0 else 'c' if j < h - 1 else 'b'
    cv.part(mask, 'k')
    # brim shadow: the hood's front edge overhangs the mask
    for x in range(33, 40):
        y = int(round(31 + dy + 0.15 * (x - 33)))
        if cv.px.get((x, y), ('',))[0] in 'PpqrQ':
            cv.put(x, y, 'P')
    # goggle lens: 2x2, glowing (eye sheet), ringed
    for (x, y) in ((33, 32), (33, 33), (34, 34), (35, 34)):
        cv.put(x, y + dy, 'k')
    for (x, y), ch in (((34, 32), 'y'), ((35, 32), 'y'), ((34, 33), 'y'), ((35, 33), 'Y')):
        if blink:
            cv.put(x, y + dy, 'f')
        else:
            cv.put(x, y + dy, ch, 'eye')


def draw_flask(cv, dx, dy, level, bubbles):
    """Round glass flask on the back. From above: the brew's surface is an oval, the cork top is an oval."""
    cx, cy, r = 26 + dx, 29 + dy, 5.4
    ball = ellipse(cx, cy, r, r)
    sy = cy + level                      # brew surface height
    out = {}
    for (x, y) in ball:
        half = math.sqrt(max(0.0, r * r - (sy - cy) ** 2))
        on_surface = ((x - cx) / max(half, 1)) ** 2 + ((y - sy) / 1.4) ** 2 <= 1.0
        if on_surface:
            out[(x, y)] = 'O' if x < cx + half * 0.3 else 'o'
        elif y > sy:
            nxv, nyv = (x - cx) / r, (y - cy) / r
            v = 0.62 - 0.3 * nxv - 0.45 * nyv
            out[(x, y)] = q(v, 'pink')
        else:
            nxv, nyv = (x - cx) / r, (y - cy) / r
            out[(x, y)] = 'e' if nxv + nyv < -1.05 else 'i' if nxv + nyv < -0.2 else 'h'
    # glass highlight (deliberate 3 px cluster on the top-left of the glass)
    for p, ch in (((cx - 3, cy - 3), 'w'), ((cx - 4, cy - 2), 'e'), ((cx - 2, cy - 4), 'e')):
        if p in out:
            out[p] = ch
    for bx, by in bubbles:
        p = (cx + bx, cy + by)
        if p in out and p[1] > sy + 1 and out[p] in 'mn':
            out[p] = 'O'
    neck = {}
    for y in range(cy - 9, cy - 4):
        for x in range(cx - 2, cx + 1):
            neck[(x, y)] = 'e' if x == cx - 2 else 'i' if x == cx - 1 else 'h'
    cv.part(neck, 'G', 'flask')
    cv.part(out, 'G', 'flask')
    # cork: a short copper cylinder whose top is a bright oval
    cork = {}
    for (x, y) in ellipse(cx - 1, cy - 10, 2.2, 1.1):
        cork[(x, y)] = 'X' if (x < cx - 1 and y < cy - 10) else 'x'
    for x in range(cx - 3, cx + 1):
        cork[(x, cy - 9)] = 'v' if x < cx - 1 else 'u'
    cv.part(cork, 'k')
    # vine wrapped round the bulb with two leaf clusters
    vine = {}
    for (x, y) in ball:
        if math.hypot(x - cx, y - cy) > r - 0.6:
            continue
        s = (x - cx) - 0.75 * (y - cy)
        if -4.9 < s < -3.7:
            vine[(x, y)] = '7' if s > -4.3 else '6'
    for p, ch in vine.items():
        cv.put(p[0], p[1], ch, 'vine')
    for leaf in (((cx - 6, cy - 1), (cx - 5, cy - 1), (cx - 6, cy), (cx - 5, cy)),
                 ((cx - 2, cy + 5), (cx - 1, cy + 5), (cx - 2, cy + 6))):
        cv.part({p: '9' if i == 0 else '8' if i < 3 else '7' for i, p in enumerate(leaf)}, 'K', 'vine')


def capsule(p0, p1, rad):
    (x0, y0), (x1, y1) = p0, p1
    out = set()
    for y in range(int(min(y0, y1) - rad) - 1, int(max(y0, y1) + rad) + 2):
        for x in range(int(min(x0, x1) - rad) - 1, int(max(x0, x1) + rad) + 2):
            dxs, dys = x1 - x0, y1 - y0
            L = dxs * dxs + dys * dys or 1
            t = max(0.0, min(1.0, ((x - x0) * dxs + (y - y0) * dys) / L))
            if math.hypot(x - (x0 + t * dxs), y - (y0 + t * dys)) <= rad:
                out.add((x, y))
    return out


ARM_POSES = {  # shoulder, hand (cell coords at body_dy 0)
    'down': ((37, 37), (40, 42)),
    'mid': ((37, 37), (40, 38)),
    'up': ((37, 37), (40, 34)),
}


def draw_arm(cv, dy, pose, adx, ady):
    (sx, sy), (hx, hy) = ARM_POSES[pose]
    hx, hy = hx + adx, hy + dy + ady
    sy += dy
    sleeve = {}
    for (x, y) in capsule((sx, sy), (hx, hy), 1.5):
        v = 0.85 - 0.35 * ((y - min(sy, hy)) / 5.0) - 0.15 * (x - sx) / 3
        sleeve[(x, y)] = q(v, 'robe')
    cv.part(sleeve, 'K')
    # vial standing in the hand: small bulb, cork whose top is a lit oval
    vial = {(hx, hy - 4): 'X', (hx + 1, hy - 4): 'x', (hx, hy - 3): 'i', (hx + 1, hy - 3): 'h',
            (hx, hy - 2): 'O', (hx + 1, hy - 2): 'n', (hx, hy - 1): 'n', (hx + 1, hy - 1): 'm'}
    cv.part(vial, 'k')
    glove = {}
    for (x, y) in ellipse(hx + 0.5, hy + 0.4, 1.4, 1.1):
        glove[(x, y)] = 'x' if y <= hy else 'v' if x <= hx else 'u'
    cv.part(glove, 'k')


def draw_feet(cv, feet):
    for fx, lift in feet:
        boot = {}
        for (x, y) in ellipse(fx + 1, 51.6 - lift, 1.6, 1.0):
            boot[(x, y)] = 'x' if y < 51.6 - lift else 'v' if x < fx + 1 else 'u'
        cv.part(boot, 'k')


def frame(body_dy=0, flask_dx=0, level=-1, bubbles=(), feet=((28, 0), (33, 0)),
          arm='down', arm_dx=0, arm_dy=0, blink=False, hem_dx=0):
    cv = Canvas()
    draw_flask(cv, flask_dx, body_dy, level, bubbles)
    draw_robe(cv, body_dy, hem_dx)
    draw_feet(cv, feet)   # toes peek out in front of the hem
    draw_capelet(cv, body_dy)
    # copper strap from the flask over the capelet (top plane of the strap lit)
    for i, (x, y) in enumerate(((29, 35), (30, 36), (31, 37), (32, 37), (33, 38))):
        cv.put(x, y + body_dy, 'x' if i < 2 else 'v')
    draw_head(cv, body_dy, blink)
    draw_arm(cv, body_dy, arm, arm_dx, arm_dy)
    return cv.images()


def bubble_set(t):
    out = []
    for col, phase in ((-3, 0), (1, 3), (3, 5)):
        out.append((col, 5 - ((t + phase) % 6)))
    return out


def build_frames():
    frames = []
    # idle: breathe (body sinks 1 px), brew sloshes, bubbles rise, one blink
    idle_dy = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
    idle_lv = [-1, -1, -2, -2, -2, -1, -1, 0, 0, -1]
    for t in range(10):
        frames.append(frame(body_dy=idle_dy[t], level=idle_lv[t], bubbles=bubble_set(t), blink=(t == 8),
                            arm_dy=1 if t in (5, 6) else 0))
    # move: alternating steps, bob, hem swings, flask lags behind the stride
    feet = [((28, 0), (34, 1)), ((29, 0), (34, 2)), ((30, 0), (33, 1)), ((31, 0), (33, 0)),
            ((32, 1), (32, 0)), ((31, 2), (31, 0)), ((30, 1), (32, 0)), ((28, 0), (33, 0))]
    move_dy = [0, 1, 1, 0, 0, 1, 1, 0]
    flask = [0, 0, -1, -1, 0, 0, -1, -1]
    lv = [-1, -2, -1, 0, -1, -2, -1, 0]
    for t in range(8):
        frames.append(frame(body_dy=move_dy[t], flask_dx=flask[t], level=lv[t], bubbles=bubble_set(t * 2),
                            feet=feet[t], arm_dx=-1 if t in (1, 2, 5, 6) else 0,
                            hem_dx=[0, 1, 1, 0, 0, -1, -1, 0][t]))
    # wave: raise the vial and shake it (head stays put: the wave clip holds Eye_10)
    seq = [('mid', 0, 0), ('up', 0, 0), ('up', 1, -1), ('up', -1, 0), ('up', 1, -1), ('mid', 0, 0)]
    for t, (arm, ax, ay) in enumerate(seq):
        frames.append(frame(level=[-1, -2, 0, -2, 0, -1][t], bubbles=bubble_set(t * 3), arm=arm,
                            arm_dx=ax, arm_dy=ay))
    return frames


# ---------------------------------------------------------------- idol: Ivy Culture Jar (3/4 view from above)
# A squat specimen jar like the vanilla Slime Oil: the copper lid's top is a big bright oval, a vine is tied
# round the neck, and through the glass a living lump of Ivy shows its maw. Round bottom edge, darker underside.
def draw_jar(cv, cx, rx, lid_top, body_top, body_bot, knob=True, maw=True):
    # glass body with the Ivy inside
    pix, _ = frustum(cx, body_top, rx, 1.6, body_bot, rx, 1.8)
    out = {}
    for (x, y) in pix:
        nxv = (x - cx) / rx
        t = (y - body_top) / (body_bot + 1.8 - body_top)
        if nxv < -0.72:
            out[(x, y)] = 'e' if t < 0.6 else 'i'          # glass highlight strip on the lit side
        elif nxv > 0.8:
            out[(x, y)] = 'h'
        else:
            v = 0.85 - 0.35 * nxv - 0.7 * t
            out[(x, y)] = q(v, 'pink')
    if maw:
        mx, my = cx + 1, body_top + (body_bot - body_top) * 0.62
        for (x, y) in ellipse(mx, my, 2.6, 1.7):
            if (x, y) in out:
                out[(x, y)] = 'q' if y < my else 'p'
        for (x, y) in ellipse(mx, my + 0.3, 1.5, 0.8):
            if (x, y) in out:
                out[(x, y)] = 'm'
        out[(int(mx), int(round(my + 0.3)))] = 'k'
        out[(int(mx) + 1, int(round(my + 0.3)))] = 'k'
        sp = (int(cx - rx * 0.35), int(body_top + 1))
        if sp in out:
            out[sp] = 'w'                                   # one wet specular on the Ivy
    cv.part(out, 'G')
    # vine tied round the neck: follows the front of the oval
    for x in range(int(cx - rx), int(cx + rx) + 1):
        y = int(round(body_top + 0.2 + 1.3 * math.sqrt(max(0.0, 1 - ((x - cx) / (rx + 0.5)) ** 2))))
        cv.put(x, y, '8' if x < cx - 1 else '7' if x < cx + 3 else '6')
    # lid: short copper cylinder, its top a big lit oval
    lrx = rx - 0.3
    lry = 0.37 * lrx
    lid, top = frustum(cx, lid_top, lrx, lry, body_top - 1.9, lrx + 0.2, 1.3)
    out = {}
    for (x, y) in lid:
        nxv = (x - cx) / lrx
        if (x, y) in top:
            out[(x, y)] = 'X' if nxv + (y - lid_top) / lry < 0.25 else 'x'
        else:
            out[(x, y)] = 'v' if nxv < 0.45 else 'u'
    cv.part(out, 'k')
    if knob:
        kn = {}
        for (x, y) in ellipse(cx - 0.5, lid_top - 1, 1.6, 0.9):
            kn[(x, y)] = 'X' if x < cx else 'v'
        cv.part(kn, 'k')


def make_idol():
    cv = Canvas(16, 16)
    draw_jar(cv, 7.5, 6.4, 3.2, 7.4, 12.6, knob=False)
    icon = cv.images()[0]
    cv = Canvas(16, 16)
    draw_jar(cv, 7.5, 4.2, 5.2, 8.2, 10.4, knob=False, maw=True)
    small = cv.images()[0]
    return icon, small


def grid_img(grid):
    im = Image.new('RGBA', (len(grid[0]), len(grid)), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch not in '. ':
                im.putpixel((x, y), RGB[ch] + (255,))
    return im


# ---------------------------------------------------------------- sheets
def load_layout():
    skins = json.load(open(os.path.join(ART, 'merchants', 'skins.json')))
    for m in skins['merchants'].values():
        if m['baseMerchant'] == 'CavelingMerchant':
            return m['textures']
    raise SystemExit('no CavelingMerchant layout in skins.json')


def pil_box(rect, H):
    x, y, w, h = (int(round(v)) for v in rect)
    return x, H - (y + h), x + w, H - y


def main():
    errors = []
    os.makedirs(OUT_M, exist_ok=True); os.makedirs(OUT_I, exist_ok=True); os.makedirs(OUT_P, exist_ok=True)
    layout = load_layout()
    frames = build_frames()
    assert len(frames) == 24
    entries = []
    sheets = {}
    for tex in layout:
        name = tex['replacesTexture']
        W, H = tex['size']
        sheet = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        layer = {'Merchant': 0, 'Merchant_Light': 1, 'Merchant_Eye': 2}[name]
        for s in tex['legacySprites']:
            idx = int(s['name'].rsplit('_', 1)[1])
            box = pil_box(s['rect'], H)
            if (box[2] - box[0], box[3] - box[1]) != (64, 64):
                errors.append(f'{name}: unexpected cell size {box}')
            sheet.paste(frames[idx][layer], box[:2])
        fn = f'{KEY}__{name}.png'
        sheet.save(os.path.join(OUT_M, fn))
        sheets[name] = sheet
        e = dict(tex)
        e['output'] = f'{KEY}/{fn}'
        entries.append(e)
        # validation: size, pixels only inside the cells, hard alpha
        if sheet.size != (W, H):
            errors.append(f'{name}: size {sheet.size} != {(W, H)}')
        cells = [pil_box(s['rect'], H) for s in tex['legacySprites']]
        a = sheet.getchannel('A')
        for y in range(H):
            for x in range(W):
                v = a.getpixel((x, y))
                if v not in (0, 255):
                    errors.append(f'{name}: soft alpha at {x},{y}')
                if v and not any(c[0] <= x < c[2] and c[1] <= y < c[3] for c in cells):
                    errors.append(f'{name}: pixel outside cells at {x},{y}')
        for c in cells:  # nothing touching a cell border (keeps sprites from bleeding into neighbours)
            sub = sheet.crop(c).getchannel('A').getbbox()
            if sub and (sub[0] == 0 or sub[1] == 0 or sub[2] == 64 or sub[3] == 64):
                errors.append(f'{name}: frame touches its cell edge {c}')
    json.dump({'key': KEY, 'rig': 'CavelingMerchantEntity', 'kind': 'legacy', 'textures': entries},
              open(os.path.join(OUT_M, 'sheets.json'), 'w'), indent=1)

    # ground line check: every main frame stands on row 53, like vanilla
    for i, (m, _, _) in enumerate(frames):
        bb = m.getchannel('A').getbbox()
        if bb is None or bb[3] != 54:
            errors.append(f'frame {i}: bottom {bb} not on the vanilla ground row 53')
    # the wave clip shows Merchant_Eye_10: wave heads must match move frame 0 exactly
    for i in range(18, 24):
        if frames[i][2].tobytes() != frames[10][2].tobytes():
            errors.append(f'wave frame {i}: eye differs from Eye_10')

    icon, small = make_idol()
    icon.save(os.path.join(OUT_I, f'{KEY}_icon.png'))
    small.save(os.path.join(OUT_I, f'{KEY}_smallIcon.png'))
    icon.save(os.path.join(OUT_I, f'{KEY}_placed.png'))
    for nm, im in (('icon', icon), ('smallIcon', small)):
        if im.size != (16, 16):
            errors.append(f'idol {nm} size {im.size}')
        if set(im.getchannel('A').tobytes()) - {0, 255}:
            errors.append(f'idol {nm} soft alpha')
    if icon.getchannel('A').getbbox()[3] != 16:
        errors.append('idol icon does not sit on the bottom edge')
    sb = small.getchannel('A').getbbox()
    if sb[2] - sb[0] > 11 or sb[3] - sb[1] > 11:
        errors.append(f'small icon too big {sb}')

    make_preview(frames, icon, small)

    if errors:
        print('\n'.join(errors[:40]))
        print(f'FAILED: {len(errors)} problems')
        sys.exit(1)
    print('ok:', ', '.join(sorted(os.listdir(OUT_M))), '+ idol + preview')


# ---------------------------------------------------------------- preview for Sid
def make_preview(frames, icon, small):
    from PIL import ImageDraw
    BG = (34, 32, 44, 255)
    S = 6
    vanilla = []
    rm = os.path.join(REF, 'reference', 'merchants')
    specs = [('CavelingMerchant/Merchant.png', (0, 0, 64, 64)), ('SlimeMerchant/Slime_merchant.png', (0, 0, 30, 30)),
             ('FishingMerchant/Fisher_merchant.png', None), ('VoidMerchant/voidMerchant_idle.png', (0, 0, 16, 35))]
    for p, box in specs:
        fp = os.path.join(rm, p)
        if not os.path.exists(fp):
            continue
        im = Image.open(fp).convert('RGBA')
        if box is None:
            box = (0, 0, im.width // 10, im.height // 3)
        vanilla.append(im.crop(box))
    mine = frames[0][0].crop((14, 2, 50, 58))
    tiles = [mine] + [v.crop(v.getchannel('A').getbbox()) for v in vanilla]
    tiles[0] = mine.crop(mine.getchannel('A').getbbox())
    row1_h = max(t.height for t in tiles) * S + 40
    strip = frames[10:18] + frames[18:24]
    idols = []
    ri = os.path.join(REF, 'idol_reference')
    for n in ('3930_MysteriousIdol', '3931_SlimeOil', '3932_PileOfChum', '3937_VoidMerchantIdol'):
        fp = os.path.join(ri, n + '_icon.png')
        if os.path.exists(fp):
            idols.append(Image.open(fp).convert('RGBA'))
    W = max(sum(t.width * S + 30 for t in tiles) + 40, 14 * (46 * 3 + 4) + 40, 1100)
    H = row1_h + 64 * 3 + 60 + 64 * 3 + 40 + 16 * 8 + 60 + 200
    out = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(out)
    x, y = 20, 24
    d.text((20, 6), 'Old Hemlock (idle, 6x) vs vanilla merchants at 6x', fill=(230, 230, 230, 255))
    for t in tiles:
        big = t.resize((t.width * S, t.height * S), Image.NEAREST)
        out.alpha_composite(big, (x, y + (row1_h - 40 - big.height)))
        x += big.width + 30
    # the same lineup at 1x, plus the idol beside the vanilla idols at 1x
    d.text((x + 10, y), '1x', fill=(230, 230, 230, 255))
    x1 = x + 10
    for t in tiles:
        out.alpha_composite(t, (x1, y + 20 + 50 - t.height))
        x1 += t.width + 6
    x1 = x + 10
    for im in [icon] + idols:
        out.alpha_composite(im, (x1, y + 90))
        x1 += 22
    y += row1_h
    d.text((20, y - 14), 'walk (8) + wave (6) at 3x, with Light/Eye overlays', fill=(230, 230, 230, 255))
    x = 20
    for i, (m, l, e) in enumerate(strip):
        f = m.copy(); f.alpha_composite(l); f.alpha_composite(e)
        f = f.crop((14, 4, 60, 58))
        if x + f.width * 3 > W:
            x = 20; y += f.height * 3 + 6
        out.alpha_composite(f.resize((f.width * 3, f.height * 3), Image.NEAREST), (x, y))
        x += f.width * 3 + 4
    y += 54 * 3 + 30
    d.text((20, y - 14), 'idle frames 0-9 at 3x', fill=(230, 230, 230, 255))
    x = 20
    for m, l, e in frames[:10]:
        f = m.copy(); f.alpha_composite(l); f.alpha_composite(e)
        f = f.crop((14, 4, 60, 58))
        out.alpha_composite(f.resize((f.width * 3, f.height * 3), Image.NEAREST), (x, y))
        x += f.width * 3 + 4
    y += 54 * 3 + 30
    d.text((20, y - 14), 'Ivy Culture Jar: 1x, 8x, small icon 8x | vanilla idols 1x / 8x', fill=(230, 230, 230, 255))
    x = 20
    out.alpha_composite(icon, (x, y + 56)); x += 30
    out.alpha_composite(icon.resize((128, 128), Image.NEAREST), (x, y)); x += 140
    out.alpha_composite(small.resize((128, 128), Image.NEAREST), (x, y)); x += 160
    for im in idols:
        out.alpha_composite(im, (x, y + 56)); x += 24
        out.alpha_composite(im.resize((128, 128), Image.NEAREST), (x, y)); x += 140
    y += 150
    d.text((20, y), 'placed on a stone floor (4x)', fill=(230, 230, 230, 255))
    y += 16
    floor = Image.new('RGBA', (48, 32), (0, 0, 0, 255))
    fd = ImageDraw.Draw(floor)
    for ty in range(0, 32, 16):
        for tx in range(0, 48, 16):
            fd.rectangle((tx, ty, tx + 15, ty + 15), fill=(52, 50, 58, 255), outline=(36, 34, 42, 255))
            fd.point((tx + 4, ty + 5), fill=(66, 64, 72, 255)); fd.point((tx + 11, ty + 10), fill=(44, 42, 50, 255))
    sh = Image.new('RGBA', (12, 3), (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse((0, 0, 11, 2), fill=(10, 8, 14, 150))
    floor.alpha_composite(sh, (18, 13 + 13))
    floor.alpha_composite(icon, (16, 13))
    out.alpha_composite(floor.resize((192, 128), Image.NEAREST), (20, y))
    out = out.crop((0, 0, W, y + 140))
    out.save(os.path.join(OUT_P, f'{KEY}.png'))


if __name__ == '__main__':
    main()
