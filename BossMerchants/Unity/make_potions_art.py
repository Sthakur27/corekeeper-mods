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


# Scale: drawn to the vanilla CavelingMerchant body (Sid's in-game note 2026-11-12: the first pass rendered bulkier
# than vanilla merchants and the player). Budget per frame: <= 26 px wide, <= 47 tall, ~575 opaque px like vanilla
# (sheet total 0.85-1.15x vanilla's 13,684); feet on the vanilla ground row 53.
#
# Restyle (2026-11-12, Sid's in-game screenshot beside the vanilla Caveling Merchant): the previous pass read as a
# bright, flat, compact blob with a big glowing flask. This pass follows the vanilla merchant's look instead:
#   - dark, muted ramps (6-8 close steps per material) shaded in small ~2x2 clusters, so the body sinks into the
#     cave light and only small accents (beak ridge, wood highlights, the vial) catch the eye;
#   - no separate outline colour: a part's silhouette ring is a dark step of its own ramp (one step lighter on the
#     lit top/left side), the way vanilla's edges are mid/dark tones of the material they bound (finish_art.py
#     then darkens edges next to solid mass further);
#   - a narrow body with thin props sticking out (a crooked 2 px walking stick held by the reaching back arm with
#     a glowing vial hanging off its crook, the beak, the hood's drooping tip, the flask held out in the front
#     hand) instead of one wide mass; a leather apron and a back satchel carry the mid-tone browns vanilla uses;
#   - the glow (Merchant_Light) is only the hanging vial's brew, ~6 px a frame (vanilla's lantern is ~18).
MPAL = {   # picked from the finisher's master palette (vanilla-sampled) so the palette lock keeps them
    # hood / capelet: deep dusky purple, indigo shadows, warm mauve top light
    'H0': '0d0721', 'H1': '1f102a', 'H2': '2b213b', 'H3': '2b215f', 'H4': '4c376f', 'H5': '6a4388', 'H6': '895392',
    # robe: the vanilla merchant's dark teal-moss
    'R0': '0b1917', 'R1': '0e2522', 'R2': '1c3b31', 'R3': '2a4e4a', 'R4': '2b5432', 'R5': '356a2c', 'R6': '4c6a4a',
    # bone mask
    'B0': '474130', 'B1': '645d53', 'B2': '8f7b77', 'B3': 'a98963', 'B4': 'd4b987',
    # wood / leather (vanilla browns)
    'W0': '352a1c', 'W1': '49351d', 'W2': '614822', 'W3': '915b26', 'W4': 'b58047',
    # glass
    'L0': '0b1917', 'L1': '232930', 'L2': '316471', 'L3': '3d7983', 'L4': 'a5cada',
    # Ivy brew in the held flask (not glowing)
    'Y0': '2b215f', 'Y1': '723e8a', 'Y2': 'a555a4',
    # herbs sticking out of the satchel
    'V0': '2b5432', 'V1': '356a2c', 'V2': '4c8d32',
    # glowing brew in the hanging vial (Merchant_Light)
    'G1': '0f9e88', 'G2': '4ad8ae', 'G3': '94f7dd',
    # goggle lens
    'E1': 'd5a43c', 'E0': '232930',
}
for _k, _v in MPAL.items():
    PAL[_k] = _v
    RGB[_k] = tuple(int(_v[i:i + 2], 16) for i in (0, 2, 4))
MGLOW = {'G1', 'G2', 'G3'}
MR = {'hood': ['H%d' % i for i in range(7)], 'robe': ['R%d' % i for i in range(7)],
      'bone': ['B%d' % i for i in range(5)], 'wood': ['W%d' % i for i in range(5)],
      'glass': ['L%d' % i for i in range(5)], 'brew': ['Y0', 'Y1', 'Y2'],
      # hood fill: weighted toward the dark middle steps; the light step only on the lit crown edge
      'hoodfill': ['H0', 'H1', 'H2', 'H3', 'H3', 'H4', 'H4', 'H4', 'H5']}


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def qm(v, ramp, lo=2):
    """Quantise light value v (0..1) onto ramp[lo:] (the darkest steps are kept for edges and occlusion)."""
    r = MR[ramp][lo:]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


def mpart(cv, pix, ramp, tag='', ring=True):
    """Draw {(x, y): key} with a 1 px ring in the ramp's own dark tones: step 1 on the shadow (bottom/right)
    side, step 2 on the lit top/left side. Vanilla edges are dark shades of the material, not one line colour."""
    if ring:
        r = MR[ramp]
        ringpx = {}
        for (x, y) in pix:
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + a, y + b)
                if p in pix:
                    continue
                lit = (a, b) in ((-1, 0), (0, -1))
                if not lit:
                    ringpx[p] = r[2]
                elif p not in ringpx:
                    ringpx[p] = r[3]
        for p, ch in ringpx.items():
            cv.put(p[0], p[1], ch, tag)
    for (x, y), ch in pix.items():
        cv.put(x, y, ch, tag)


def draw_stick(cv, dy, hand, tip_dx, sway):
    """Crooked walking stick on the back side: tip on the ground, crook above the hood, a vial hanging off it.
    Two px wide like vanilla's staff (lit left column, shaded right column), knots every few px."""
    hx, hy = hand
    tip = (18 + tip_dx, 53)
    top = (19 + (hx - 20), 12 + dy)
    n = tip[1] - top[1]
    for i in range(n + 1):
        y = tip[1] - i
        x = int(round(tip[0] + (top[0] - tip[0]) * i / n))
        hi = i > n * 0.4
        lit, sh = ('W4', 'W2') if hi else ('W3', 'W1')
        if i % 6 == 2:
            lit, sh = 'W2', 'W0'          # knot
        cv.put(x, y, lit)
        if i < n - 1:
            cv.put(x + 1, y, sh)
    cx, cy = top
    # crook curling forward (its top catches the light)
    for (x, y), ch in (((cx, cy - 1), 'W3'), ((cx + 1, cy - 2), 'W4'), ((cx + 2, cy - 2), 'W4'),
                       ((cx + 3, cy - 2), 'W3'), ((cx + 1, cy - 1), 'W2'), ((cx + 2, cy - 1), 'W1'),
                       ((cx + 3, cy - 1), 'W1'), ((cx + 4, cy - 1), 'W2'), ((cx + 4, cy), 'W1')):
        cv.put(x, y, ch)
    # cord + potion vial: cork, glowing brew, dark glass; swings with `sway`
    vx = cx + 4 + sway
    cv.put(cx + 4, cy + 1, 'W0')
    cv.put(vx, cy + 2, 'W0')
    vial = {(vx, cy + 3): 'W4', (vx - 1, cy + 4): 'L3', (vx, cy + 4): 'G3', (vx + 1, cy + 4): 'L2',
            (vx - 1, cy + 5): 'G2', (vx, cy + 5): 'G2', (vx + 1, cy + 5): 'G1',
            (vx - 1, cy + 6): 'G1', (vx, cy + 6): 'G1', (vx + 1, cy + 6): 'L2', (vx, cy + 7): 'L1'}
    for (x, y), ch in vial.items():
        cv.put(x, y, ch, 'flask')
    for (x, y), ch in (((vx - 2, cy + 5), 'L2'), ((vx - 2, cy + 6), 'L1'), ((vx + 2, cy + 5), 'L1'),
                       ((vx + 2, cy + 6), 'L0'), ((vx - 1, cy + 7), 'L1'), ((vx + 1, cy + 7), 'L0')):
        cv.put(x, y, ch)
    # the hand gripping it
    for (x, y), ch in (((hx, hy), 'R6'), ((hx + 1, hy), 'R5'), ((hx, hy + 1), 'R4'), ((hx + 1, hy + 1), 'R3'),
                       ((hx - 1, hy), 'R4'), ((hx + 2, hy + 1), 'R1')):
        cv.put(x, y, ch)


def draw_back_arm(cv, dy, hand):
    """The far arm reaching back to the stick: a thin sleeve, shaded (it is on the far side)."""
    hx, hy = hand
    sleeve = {}
    for (x, y) in capsule((27, 35 + dy), (hx + 1, hy), 1.0):
        v = 0.55 - 0.3 * (y - 35 - dy) / 4.0 + 0.15 * grain(x, y)
        sleeve[(x, y)] = qm(v, 'robe')
    mpart(cv, sleeve, 'robe')


def draw_satchel(cv, dy):
    """Leather satchel slung on the back hip, seen from above (lit top face), under the reaching back arm."""
    x0, y0 = 22, 36 + dy
    box = {}
    for y in range(y0, y0 + 8):
        for x in range(x0, x0 + 5):
            if y < y0 + 2:
                box[(x, y)] = 'W4' if (x - x0) + (y - y0) < 2 else 'W3'      # top face
            elif y < y0 + 4:
                box[(x, y)] = 'W2' if x < x0 + 3 else 'W1'                    # flap
            else:
                box[(x, y)] = 'W2' if (x < x0 + 2 and (x + y) % 5) else 'W1'  # front face, darker low
    box[(x0 + 2, y0 + 3)] = 'B3'   # buckle
    box[(x0 + 2, y0 + 4)] = 'B1'
    mpart(cv, box, 'wood')


def draw_robe(cv, dy, hem_dx):
    """Moss robe: a narrow bell, oval hem curving toward the viewer, ragged tatters, cluster-shaded folds."""
    cx, ytop, ybot = 31, 36 + dy, 48 + dy
    rx0, rx1, ry1 = 4.2, 5.8, 1.7
    pix, top = frustum(cx, ytop, rx0, 1.4, ybot, rx1, ry1, hem_dx)
    out = {}
    for (x, y) in pix:
        t = (y - ytop) / (ybot + ry1 - ytop)
        c = cx + hem_dx * t * t
        nx = (x - c) / rx1
        v = 1.04 - 0.55 * t - 0.42 * nx + 0.15 * grain(x, y)
        for f in (-0.35, 0.3):   # two creases: dark line, lit ridge on its left
            fx = c + f * (rx0 + (rx1 - rx0) * t)
            if y > ytop + 2 and abs(x - fx) < 0.5:
                v -= 0.22
            elif y > ytop + 2 and 0.5 <= fx - x < 1.5:
                v += 0.1
        out[(x, y)] = qm(v, 'robe')
    # ragged hem: notch every fourth column of the bottom row, extend a few tatters by one
    bottom = {}
    for (x, y) in out:
        bottom[x] = max(bottom.get(x, -1), y)
    for x, y in sorted(bottom.items()):
        k = (x - cx) % 4
        if k == 1:
            out.pop((x, y), None)
        elif k == 3 and (x, y + 1) not in out:
            out[(x, y + 1)] = 'R2'
    # leather apothecary apron down the front (follows the bell, stained with Ivy brew)
    for (x, y) in list(out):
        if y <= 40 + dy:
            continue
        t = (y - ytop) / (ybot + ry1 - ytop)
        c = cx + 1 + hem_dx * t * t
        half = 1.6 + 1.6 * t
        if abs(x - c) <= half:
            nx = (x - c) / half
            v = 0.7 - 0.6 * t - 0.35 * nx + 0.15 * grain(x, y)
            out[(x, y)] = qm(v, 'wood', 1)
    for (x, y), ch in (((cx + 2, 44 + dy), 'Y1'), ((cx + 3, 44 + dy), 'Y0'), ((cx + 2, 45 + dy), 'Y0'),
                       ((cx - 1, 47 + dy), 'Y1'), ((cx - 1, 48 + dy), 'Y0')):
        if (x, y) in out:
            out[(x, y)] = ch
    mpart(cv, out, 'robe')
    # belt with a bone buckle
    by, brx = 39 + dy, 4.7
    for x in range(int(cx - brx), int(cx + brx) + 1):
        yy = int(round(by + 1.3 * math.sqrt(max(0.0, 1 - ((x - cx) / (brx + 0.6)) ** 2))))
        nx = (x - cx) / brx
        cv.put(x, yy, 'W3' if nx < -0.4 else 'W2' if nx < 0.4 else 'W1')
        cv.put(x, yy + 1, 'W1' if nx < 0.3 else 'W0')
    byy = int(round(by + 1.3))
    cv.put(cx - 1, byy, 'B3'); cv.put(cx - 1, byy + 1, 'B1')
    # a vial tucked in the belt
    for (x, y), ch in (((cx + 2, byy - 1), 'W4'), ((cx + 2, byy), 'Y1'), ((cx + 3, byy), 'Y1'),
                       ((cx + 2, byy + 1), 'Y1'), ((cx + 3, byy + 1), 'Y0')):
        cv.put(x, y, ch)


def draw_capelet(cv, dy):
    """Short purple capelet over the shoulders: lit top plane, ragged front edge."""
    cx = 31
    pix, top = frustum(cx, 33 + dy, 4.2, 1.6, 36 + dy, 6.0, 1.6)
    out = {}
    for (x, y) in pix:
        t = (y - (33 + dy)) / 4.6
        nx = (x - cx) / 6.0
        v = 0.8 - 0.7 * t - 0.42 * nx + 0.15 * grain(x, y)
        out[(x, y)] = qm(v, 'hoodfill')
    bottom = {}
    for (x, y) in out:
        bottom[x] = max(bottom.get(x, -1), y)
    for x, y in sorted(bottom.items()):
        if (x - cx) % 3 == 0:
            out[(x, y + 1)] = 'H3'
    mpart(cv, out, 'hood')


def draw_head(cv, dy, blink):
    """Hood seen from above: lit crown, drooping tip behind, a hollow face with the bone beak mask low in front."""
    cx, cy = 31, 27 + dy
    rx, ry = 5.0, 4.6
    hood = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nxv, nyv = (x - cx) / rx, (y - cy) / ry
        v = 0.42 - 0.3 * nxv - 0.55 * nyv + 0.2 * (1 - nxv * nxv - nyv * nyv) + 0.15 * grain(x, y)
        hood[(x, y)] = qm(v, 'hoodfill')
    # drooping tip trailing back (thin, 1-2 px)
    for (x, y), ch in (((26, 24), 'H4'), ((25, 24), 'H5'), ((25, 25), 'H3'), ((24, 25), 'H4'), ((24, 26), 'H2'),
                       ((25, 26), 'H3')):
        hood[(x, y + dy)] = ch
    # top plane highlight: a small warm cluster on the crown
    for (x, y), ch in (((29, 23), 'H6'), ((30, 23), 'H6'), ((28, 24), 'H5'), ((29, 24), 'H6'), ((30, 24), 'H5'),
                       ((31, 23), 'H5')):
        if (x, y + dy) in hood:
            hood[(x, y + dy)] = ch
    mpart(cv, hood, 'hood')
    # face hollow under the brim
    for p in ellipse(33.8, 29.5 + dy, 2.7, 1.9):
        cv.put(p[0], p[1], 'H0' if p[1] > 29 + dy else 'H1')
    # mask + beak (the beak pokes out past the hood, tilting down)
    mask = {}
    for (x, y) in ellipse(34.6, 30.3 + dy, 2.2, 1.4):
        mask[(x, y)] = 'B3' if y < 30 + dy else 'B2'
    for i in range(6):
        x = 36 + i
        y0 = int(round(29 + dy + i * 0.45))
        h = 2 if i < 4 else 1
        for j in range(h):
            mask[(x, y0 + j)] = ('B4' if i < 3 else 'B3') if j == 0 else 'B2'
    mpart(cv, mask, 'bone')
    # brim shadow over the top of the mask
    for x in range(32, 37):
        if cv.px.get((x, 28 + dy), ('',))[0] in MR['hood']:
            cv.put(x, 28 + dy, 'H1')
    # goggle: one lens pixel glows (eye sheet), its rim is dark glass
    cv.put(32, 29 + dy, 'L1'); cv.put(33, 30 + dy, 'L1')
    if blink:
        cv.put(33, 29 + dy, 'E0')
    else:
        cv.put(33, 29 + dy, 'E1', 'eye')


ARM_POSES = {  # shoulder, hand (cell coords at body_dy 0)
    'down': ((35, 35), (38, 41)),
    'mid': ((35, 35), (38, 37)),
    'up': ((35, 35), (38, 33)),
}


def draw_arm(cv, dy, pose, adx, ady):
    (sx, sy), (hx, hy) = ARM_POSES[pose]
    hx, hy = hx + adx, hy + dy + ady
    sy += dy
    sleeve = {}
    for (x, y) in capsule((sx, sy), (hx, hy), 1.2):
        v = 0.8 - 0.4 * ((y - min(sy, hy)) / 6.0) - 0.2 * (x - sx) / 3 + 0.15 * grain(x, y)
        sleeve[(x, y)] = qm(v, 'robe')
    mpart(cv, sleeve, 'robe')
    # round flask held out in the hand: cork top, glass shoulder, Ivy brew, one specular
    fx, fy = hx + 1, hy + 1
    flask = {(fx, fy - 3): 'W4', (fx + 1, fy - 3): 'W2',
             (fx, fy - 2): 'L3', (fx + 1, fy - 2): 'L2',
             (fx - 1, fy - 1): 'L4', (fx, fy - 1): 'L3', (fx + 1, fy - 1): 'L3', (fx + 2, fy - 1): 'L2',
             (fx - 1, fy): 'Y1', (fx, fy): 'Y1', (fx + 1, fy): 'Y1', (fx + 2, fy): 'Y1',
             (fx - 1, fy + 1): 'Y1', (fx, fy + 1): 'Y1', (fx + 1, fy + 1): 'Y0', (fx + 2, fy + 1): 'Y0',
             (fx, fy + 2): 'Y0', (fx + 1, fy + 2): 'Y0'}
    mpart(cv, flask, 'glass')
    glove = {(hx - 1, hy): 'W3', (hx, hy): 'W2', (hx - 1, hy + 1): 'W2', (hx, hy + 1): 'W1'}
    for p, ch in glove.items():
        cv.put(p[0], p[1], ch)


def draw_feet(cv, feet):
    for fx, lift in feet:
        boot = {}
        for (x, y) in ellipse(fx + 1, 51.8 - lift, 1.6, 0.9):
            boot[(x, y)] = 'W3' if y < 51.8 - lift and x <= fx + 1 else 'W2'
        mpart(cv, boot, 'wood')


def frame(body_dy=0, sway=0, feet=((27, 0), (32, 0)), arm='down', arm_dx=0, arm_dy=0, blink=False, hem_dx=0,
          stick_dx=0, tip_dx=0):
    cv = Canvas()
    draw_satchel(cv, body_dy)
    draw_robe(cv, body_dy, hem_dx)
    draw_feet(cv, feet)
    draw_capelet(cv, body_dy)
    draw_head(cv, body_dy, blink)
    draw_arm(cv, body_dy, arm, arm_dx, arm_dy)
    draw_back_arm(cv, body_dy, (20 + stick_dx, 39 + body_dy))
    draw_stick(cv, body_dy, (20 + stick_dx, 39 + body_dy), tip_dx, sway)
    return images_m(cv)


def images_m(cv):
    main = Image.new('RGBA', (cv.w, cv.h), (0, 0, 0, 0))
    light = Image.new('RGBA', (cv.w, cv.h), (0, 0, 0, 0))
    eye = Image.new('RGBA', (cv.w, cv.h), (0, 0, 0, 0))
    for (x, y), (ch, tag) in cv.px.items():
        c = RGB[ch] + (255,)
        main.putpixel((x, y), c)
        if tag == 'flask' and ch in MGLOW:
            light.putpixel((x, y), c)
        if tag == 'eye':
            eye.putpixel((x, y), c)
    return main, light, eye


def build_frames():
    frames = []
    # idle: breathe (body sinks 1 px), the vial swings on its cord, one blink
    idle_dy = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
    idle_sw = [0, 0, 1, 1, 0, 0, -1, -1, 0, 0]
    for t in range(10):
        frames.append(frame(body_dy=idle_dy[t], sway=idle_sw[t], blink=(t == 8),
                            arm_dy=1 if t in (5, 6) else 0))
    # move: alternating steps, bob, hem swings, the stick is planted and lifted, the vial lags behind
    feet = [((27, 0), (33, 1)), ((28, 0), (33, 2)), ((29, 0), (32, 1)), ((30, 0), (32, 0)),
            ((31, 1), (31, 0)), ((30, 2), (30, 0)), ((29, 1), (31, 0)), ((27, 0), (32, 0))]
    move_dy = [0, 1, 1, 0, 0, 1, 1, 0]
    for t in range(8):
        frames.append(frame(body_dy=move_dy[t], sway=[0, -1, -1, 0, 0, -1, -1, 0][t], feet=feet[t],
                            arm_dx=-1 if t in (1, 2, 5, 6) else 0, hem_dx=[0, 1, 1, 0, 0, -1, -1, 0][t],
                            stick_dx=[0, 0, 1, 1, 0, 0, 1, 1][t], tip_dx=[0, 0, 1, 2, 2, 1, 1, 0][t]))
    # wave: raise the flask and shake it (head stays put: the wave clip holds Eye_10)
    seq = [('mid', 0, 0), ('up', 0, 0), ('up', 1, -1), ('up', -1, 0), ('up', 1, -1), ('mid', 0, 0)]
    for t, (arm, ax, ay) in enumerate(seq):
        frames.append(frame(arm=arm, arm_dx=ax, arm_dy=ay, sway=[0, 1, 0, -1, 0, 1][t]))
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
