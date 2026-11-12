"""Boss Merchants: art for the `farmanimals` merchant (Azeos the Sky Titan / livestock and ranching supplies).

    python -I BossMerchants/Unity/make_farmanimals_art.py

Character: WREN HAYFEATHER, a stout bird-folk rancher of Azeos' Wilderness. Round Azeos-blue feathered head with a
short gold beak and one bright hawk eye under a wide straw hat (rust band, an Azeos tail feather stuck in it), a muted
rust poncho with a low-contrast brass zigzag, slate stripe and a slate patch, a brass cowbell at the collar, a burlap
feed sack slung on the back by a leather strap with a buckle, a coiled lasso and a horseshoe charm hung on the sack,
slate trousers, leather boots, and a shepherd's crook with a lantern hanging from the hook. The wave/emote is a polite tip of the hat (a little crest
pops up under it).
Rig: CavelingMerchant (legacy SpriteRenderer + Animator). Sheets `Merchant` 640x192 and `Merchant_Light` 640x192
(24 cells of 64x64, rows of 10/8/6 from the top) and `Merchant_Eye` 640x128 (18 cells, rows of 10/8); pivot centre.
Vanilla clip frame map (read from the game's AnimationClips):
    MerchantIdleR 0-9 (8 fps), MerchantMoveR 10-17 (10 fps), MerchantWaveR 18-23 (10 fps, eye overlay stays on
    Merchant_Eye_10 for the whole wave, so the head does not move during the wave).
`Merchant_Light` is an unlit overlay (vanilla: the lantern crystal); here it is the lantern flame.
`Merchant_Eye` is an unlit overlay (vanilla: the glowing eye); here it is Wren's eye glint.
Drawn in Core Keeper's 3/4 top-down view (docs/perspective_rules.md): oval brim and crown top, oval shoulders, sack
mouth, lasso ring and lantern cap seen from above, curved hem and stripes, face low under the brim, small boots.
Idol: the Skyherd Bell, a brass cowbell on a red leather strap, plumed with two Azeos feathers, sitting on a hay bale.

Everything is drawn from hand-authored pixel grids with Core Keeper palette colours; no vanilla pixels are copied.
The preview sheet places vanilla reference art (build/bossmerchants, extracted locally) next to ours for review only;
those panels are skipped if that folder is missing. Exit code 1 if any validation fails.
"""
import json, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
ROOT = os.path.dirname(MOD)
KEY = 'farmanimals'
ART = os.path.join(HERE, 'Art')
OUT_MERCH = os.path.join(ART, 'merchants', KEY)
OUT_IDOL = os.path.join(ART, 'idols')
OUT_PREV = os.path.join(ART, 'previews')
REF = os.path.join(ROOT, 'build', 'bossmerchants')

CELL = 64
ROWS_MAIN = (10, 8, 6)      # Merchant / Merchant_Light, top row first
ROWS_EYE = (10, 8)          # Merchant_Eye
SHEETS = {'Merchant': (640, 192, ROWS_MAIN), 'Merchant_Light': (640, 192, ROWS_MAIN), 'Merchant_Eye': (640, 128, ROWS_EYE)}
RENDERER = {'Merchant': 'SR', 'Merchant_Light': 'SRLight', 'Merchant_Eye': 'SREye'}
GROUND = 53                 # lowest opaque row in a cell (vanilla Caveling Merchant: 53)


def hx(s):
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


# Core Keeper palette colours (sampled from vanilla merchant sheets and item icons; used as a palette only)
PAL = {
    # straw / wood / burlap (Caveling Merchant pack, WallStrawBlock, Slime Merchant sack)
    '1': '614822', '2': '915b26', '3': 'b58047', '4': 'e0c961', '5': 'e9d9a2', '6': 'f7eec7',
    'i': 'd4b987', 'I': 'fae6bc', 'O': '39341f', 'D': '23281e',
    # Azeos blue (AzeosTrophy; same ramp the vanilla merchants use)
    'n': '142b5c', 'b': '143eab', 'B': '165dd9', 'c': '1885d8', 'C': '19bdc6', 'w': '94f6dc',
    # beak (Caveling Merchant orange ramp)
    'k': '824223', 'G': 'da6210', 'g': 'e2a12c', 'y': 'f2df3a', 'W': 'ffffff',
    # poncho: muted rust cloth (Caveling Merchant maroon/rust, Fishing Merchant c16436), hue-shifted shadow
    'P': '5e2a28', 'p': '824223', 'q': 'c16436', 'Q': 'd29a7c',
    # muted brass/gold (Fishing Merchant coat a88b37 / c9ab51)
    'a': 'a88b37', 'A': 'c9ab51',
    # slate cloth / steel (Caveling Merchant 47677e ramp, CattleCage)
    'T': '2a4e4a', 'v': '47677e', 'V': '677fae', 'u': 'a1aedd', 'U': 'd7dcfb',
    # leather (Saddle)
    'L': '562e29', 'l': '774627', 'h': 'b16a3c',
    # emissive eye glint
    'e': 'f7eec7',
}
PAL = {k: hx(v) for k, v in PAL.items()}

# ---------------------------------------------------------------- character (cell coords, facing right, 3/4 top-down)
# Core Keeper's camera looks down at ~3/4: top planes (hat crown, brim, shoulders, sack mouth, lantern cap) are
# foreshortened ovals in the lightest ramp step, front faces are mid-tone and darken downwards, round things are
# ovals, and the face sits low under a wide oval brim. Parts are built from ellipses / curved bands, light from the
# top-left, shaded in ~2x2 clusters (see the restyle note above MPAL); no single-pixel dithering.
# Vanilla scale (2026-11-12 size fix): the CavelingMerchant rig shows the sprite 1:1 at 16 px/unit, so Wren is drawn
# at the vanilla merchant's size: whole figure incl. crook + lantern <= 26 px wide and <= 47 px tall, body about
# 17-20 px wide, ~480-650 opaque px per frame (vanilla ~570), feet on row 53 like vanilla.
CX = 28                      # body centre column (vanilla body spans x 19..38)
EYE_OFF = (3, 31)            # eye glint position relative to (CX, 0) before the bob

# ---------------------------------------------------------------- animation tables
IDLE_DY = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
IDLE_LANTERN = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0]
IDLE_FLUTTER = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
WALK_DY = [0, -1, 0, -1, 0, -1, 0, -1]
WALK_LANTERN = [0, -1, -1, 0, 0, 1, 1, 0]
#            (near boot x offset, lift), (far boot x offset, lift), relative to CX
WALK_LEGS = [((3, 0), (-5, 0)), ((1, 0), (-3, 1)), ((-2, 0), (1, 0)), ((0, 1), (-4, 0)),
             ((3, 0), (-5, 0)), ((1, 0), (-3, 1)), ((-2, 0), (1, 0)), ((0, 1), (-4, 0))]
STAND_LEGS = ((2, 0), (-4, 0))
# hat tip: hand position relative to (CX, 0), hat lift
WAVE = [((-3, 37), 0), ((-5, 33), 0), ((-7, 28), 0), ((-7, 25), -2), ((-7, 25), -2), ((-7, 26), -1)]

# hat / head geometry (relative to CX and the bob)
BRIM = (-0.5, 25.0, 9.0, 3.1)          # dx, cy, rx, ry
HEAD = (1.0, 31.5, 4.2, 3.7)


def put(img, x, y, ch):
    if ch in '. ':
        return
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), PAL[ch])


def grid(img, rows, ox, oy):
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            put(img, ox + x, oy + y, ch)


def line_px(x0, y0, x1, y1):
    pts, n = [], max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        pts.append((round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n)))
    return pts


def ell(cx, cy, rx, ry):
    """Pixels inside an ellipse (pixel centres)."""
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def edges(mask):
    """(top/left edge pixels, bottom/right edge pixels) of a mask."""
    tl, br = set(), set()
    for (x, y) in mask:
        if (x - 1, y) not in mask or (x, y - 1) not in mask:
            tl.add((x, y))
        elif (x + 1, y) not in mask or (x, y + 1) not in mask:
            br.add((x, y))
    return tl, br


def band(v, cuts, keys):
    """Ramp key for light value v: keys[0] darkest .. keys[-1] lightest, cuts ascending."""
    i = 0
    while i < len(cuts) and v > cuts[i]:
        i += 1
    return keys[i]


def paint(img, mask, keyfn, out_tl=None, out_br=None):
    tl, br = edges(mask)
    for (x, y) in mask:
        if out_br and (x, y) in br:
            put(img, x, y, out_br)
        elif out_tl and (x, y) in tl:
            put(img, x, y, out_tl)
        else:
            put(img, x, y, keyfn(x, y))


# Restyle (2026-11-12, after Old Hemlock's restyle beside the vanilla Caveling Merchant in Sid's screenshot): the
# previous pass read as bright and flat next to vanilla: a big pale straw disc of a brim, a wide flat rust poncho,
# a bright blue head and 1-2 shade fills. This pass follows the vanilla merchants' look instead:
#   - dark, muted ramps (6-7 close steps per material, picked from the finisher's master palette) shaded in small
#     ~2x2 clusters (grain()), so the figure sinks into the cave light and only small accents catch the eye: a few
#     sunlit straw strands on the crown and the back of the brim, the beak, the brass bell and buckle, the lantern;
#   - the straw hat is no longer a flat pale disc: weathered ochre straw, darker toward the front, woven in rings of
#     clusters, a shadow cast by the crown across the right half of the brim, a frayed front edge;
#   - no separate outline colour: each part's silhouette ring is a dark step of its own ramp (one step lighter on
#     the lit top/left side); the finisher darkens edges next to solid mass further;
#   - a narrower poncho (mass moved into thin props: the crook, the lasso hanging off the sack, the horseshoe charm,
#     the feather, the fringe and the trouser legs);
#   - the glow (Merchant_Light) stays the lantern flame, ~6 px a frame (vanilla's lantern is ~18).
MPAL = {
    # straw hat: weathered ochre, cool-brown shadow, two warm sunlit steps used only in small clusters
    'S0': '201d15', 'S1': '352a1c', 'S2': '49351d', 'S3': '614822', 'S4': '624f32', 'S5': '875e2c',
    'S6': 'ab834c', 'S7': 'ab834c',
    # Azeos-blue feathers (navy shadow, muted mid blues, one slate-blue light step)
    'F0': '0f2960', 'F1': '142b5c', 'F2': '273fa7', 'F3': '4b708a', 'F4': '4883d4', 'F5': '6e9bb9', 'F6': '6e9bb9',
    # poncho: muted rust, purple-maroon shadow
    'P0': '2b213b', 'P1': '5e2a28', 'P2': '5e2a28', 'P3': '6b4545', 'P4': '6b4545', 'P5': '824223', 'P6': '774627',
    'P7': 'b16a3c',
    # brass (zigzag, bell, buckle, feather tip)
    'A0': '49351d', 'A1': '614822', 'A2': 'a88b37', 'A3': 'a88b37',
    # slate cloth / lantern metal
    'T0': '232930', 'T1': '2a4e4a', 'T2': '47677e', 'T3': '47677e', 'T4': '4b708a',
    # leather (strap, boots, glove)
    'L0': '352a1c', 'L1': '5e2a28', 'L2': '774627', 'L3': 'b16a3c',
    # crook wood
    'W0': '352a1c', 'W1': '49351d', 'W2': '614822', 'W3': '915b26', 'W4': '915b26',
    # burlap sack / rope
    'U0': '352a1c', 'U1': '49351d', 'U2': '614822', 'U3': '624f32', 'U4': '875e2c', 'U5': 'ab834c',
    # beak
    'K0': '824223', 'K1': 'da6210', 'K2': 'e2a12c',
    # lantern flame (Merchant_Light)
    'G1': 'da6210', 'G2': 'f2df3a', 'G3': 'ffffff',
}
for _k, _v in MPAL.items():
    PAL[_k] = hx(_v)
MGLOW = {'G1', 'G2', 'G3'}
MR = {'straw': ['S%d' % i for i in range(8)], 'blue': ['F%d' % i for i in range(7)],
      'rust': ['P%d' % i for i in range(8)], 'brass': ['A%d' % i for i in range(4)],
      'slate': ['T%d' % i for i in range(5)], 'leather': ['L%d' % i for i in range(4)],
      'wood': ['W%d' % i for i in range(5)], 'burlap': ['U%d' % i for i in range(6)],
      'beak': ['K0', 'K0', 'K1', 'K2']}


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def qm(v, ramp, lo=2, hi=None):
    """Quantise light value v (0..1) onto ramp[lo:hi] (the darkest steps are kept for edges and occlusion)."""
    r = MR[ramp][lo:hi]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


class Canvas:
    def __init__(self):
        self.px = {}

    def put(self, x, y, ch, tag=''):
        if 0 <= x < CELL and 0 <= y < CELL:
            self.px[(x, y)] = (ch, tag)

    def part(self, pix, ramp, tag='', ring=True):
        """Draw {(x, y): key} with a 1 px ring in the ramp's own dark tones: step 0 on the shadow (bottom/right)
        side, step 1 on the lit top/left side (vanilla edges are dark shades of the material, not one line colour)."""
        if ring:
            r = MR[ramp]
            ringpx = {}
            for (x, y) in pix:
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    p = (x + a, y + b)
                    if p in pix:
                        continue
                    if (a, b) in ((1, 0), (0, 1)):
                        ringpx[p] = r[0]
                    elif p not in ringpx:
                        ringpx[p] = r[1]
            for p, ch in ringpx.items():
                self.put(p[0], p[1], ch, tag)
        for (x, y), ch in pix.items():
            self.put(x, y, ch, tag)

    def key(self, x, y):
        return self.px.get((x, y), ('',))[0]

    def images(self):
        base = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
        light = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
        for (x, y), (ch, tag) in self.px.items():
            base.putpixel((x, y), PAL[ch])
            if tag == 'flame' and ch in MGLOW:
                light.putpixel((x, y), PAL[ch])
        return base, light


def draw_sack(cv, c, dy):
    """Burlap feed sack slung on the back (mouth seen from above), a rope tie, the lasso coil and horseshoe."""
    sx, sy, srx, sry = c - 6.5, 38.5 + dy, 2.8, 4.4
    sack = {}
    for (x, y) in ell(sx, sy, srx, sry):
        nx, ny = (x + .5 - sx) / srx, (y + .5 - sy) / sry
        v = 0.55 - 0.35 * nx - 0.4 * ny + 0.18 * grain(x, y)
        if (x + y) % 4 == 0 and y > sy:                       # a burlap seam
            v -= 0.18
        sack[(x, y)] = qm(v, 'burlap', 1, 5)
    for (x, y) in ell(sx, 34.2 + dy, 2.2, 1.1):              # gathered mouth, top plane
        sack[(x, y)] = 'U4' if x < sx else 'U3'
    sack[(int(sx) - 1, 34 + dy)] = 'U5'
    cv.part(sack, 'burlap')
    cv.put(int(sx), 35 + dy, 'U1'); cv.put(int(sx) + 1, 35 + dy, 'U1')   # rope tie
    # lasso: a thin coil hanging off the sack's lower side (1 px rope, lit top arc)
    rcx, rcy = c - 7.5, 44.0 + dy
    ring = ell(rcx, rcy, 2.8, 1.9) - ell(rcx, rcy - 0.2, 1.6, 0.8)
    for (x, y) in ring:
        top = y + .5 < rcy
        cv.put(x, y, ('U5' if (x // 2) % 2 == 0 else 'U4') if top else ('U3' if x < rcx else 'U1'))
    cv.put(int(rcx) + 2, int(rcy) + 2, 'U1'); cv.put(int(rcx) + 2, int(rcy) + 3, 'U2')   # loose end
    # horseshoe charm (steel) under the coil
    for (x, y), ch in (((c - 10, 46), 'T2'), ((c - 10, 47), 'T1'), ((c - 9, 48), 'T1'), ((c - 8, 47), 'T0'),
                       ((c - 8, 46), 'T1')):
        cv.put(x, y + dy, ch)


def draw_leg(cv, x0, lift, near, top):
    """Slate trouser leg down from under the poncho, then a small boot: lit top plane, leather front, dark sole."""
    y0 = GROUND - 2 - lift
    for y in range(top, y0):
        cv.put(x0 + 1, y, 'T2' if near else 'T1'); cv.put(x0 + 2, y, 'T1' if near else 'T0')
    rows = (('L2', 'L3', 'L2', 'L1'), ('L1', 'L2', 'L1', 'L0'), ('L0', 'L0', 'L0', 'T0')) if near else \
           (('L1', 'L2', 'L1', 'L0'), ('L0', 'L1', 'L0', 'L0'), ('T0', 'L0', 'L0', 'T0'))
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            cv.put(x0 + i, y0 + j, ch)


def draw_poncho(cv, c, dy):
    """Rust poncho: narrow cone with oval shoulders and a curved hem, cluster-shaded folds, a low-contrast brass
    zigzag, a slate stripe, a fringe, a patch, and the sack strap with a brass buckle across the chest."""
    yt, hy, rxh, ryh, w0 = 35 + dy, 45 + dy, 7.2, 2.2, 4.4
    pon = set()
    for y in range(yt, int(hy + ryh) + 1):
        hw = w0 + (rxh - w0) * min(1.0, (y - yt) / (hy - yt))
        for x in range(c - 10, c + 11):
            dx = x + 0.5 - c
            if abs(dx) <= hw and y + 0.5 <= hy + ryh * max(0.0, 1 - (dx / rxh) ** 2) ** 0.5:
                pon.add((x, y))

    def hem_y(x, k):
        dx = (x + 0.5 - c) / rxh
        return round(yt + k + ryh * max(0.0, 1 - dx * dx) ** 0.5)

    out = {}
    for (x, y) in pon:
        u = (x + 0.5 - c) / rxh
        t = (y - yt) / (hy + ryh - yt)
        v = 1.1 - 0.45 * t - 0.5 * u + 0.1 * grain(x, y)
        for f in (-0.3,):                                        # one fold: dark crease, lit ridge on its left
            fx = c + f * (w0 + (rxh - w0) * t)
            if y > yt + 3 and abs(x + .5 - fx) < 0.5:
                v -= 0.25
            elif y > yt + 3 and 0.5 <= fx - x - .5 < 1.5:
                v += 0.12
        if y == hem_y(x, 4):                                     # brass zigzag, muted, in 2 px steps
            out[(x, y)] = 'A2' if (x // 2) % 2 == 0 and u < -0.15 else 'A1' if (x // 2) % 2 == 0 else 'P3'
            continue
        if y == hem_y(x, 7):                                     # slate stripe
            out[(x, y)] = 'T2' if u < -0.2 else 'T1'
            continue
        if y >= hem_y(x, 9):
            v -= 0.25                                            # underside of the hem
        out[(x, y)] = qm(v, 'rust', 1, 8)
    for (x, y), ch in (((c - 4, yt + 7), 'T3'), ((c - 3, yt + 7), 'T2'), ((c - 4, yt + 8), 'T2'),
                       ((c - 3, yt + 8), 'T1')):                 # slate patch
        if (x, y) in out:
            out[(x, y)] = ch
    # fringe: little 2 px tassels under the hem
    for x in range(c - 6, c + 7, 2):
        y = hem_y(x, 10) + 1
        if (x, y - 1) in pon:
            out[(x, y)] = 'P2'
            if x % 4 == 0:
                out[(x, y + 1)] = 'P1'
    cv.part(out, 'rust')
    # shoulders: the poncho's top plane
    sh = {}
    for (x, y) in ell(c, yt + 1.0, w0 + 1.0, 2.0):
        u = (x + .5 - c) / (w0 + 1.0)
        v = 0.85 - 0.5 * u - 0.25 * (y - yt) + 0.15 * grain(x, y)
        sh[(x, y)] = qm(v, 'rust', 3)
    cv.part(sh, 'rust', ring=False)
    # sack strap across the chest with a brass buckle
    for (x, y) in line_px(c - 4, yt + 1, c + 4, yt + 7):
        cv.put(x, y, 'L2'); cv.put(x, y + 1, 'L0')
    cv.put(c, yt + 4, 'A3'); cv.put(c + 1, yt + 4, 'A2')


def draw_head(cv, c, dy, hat_dy):
    """Blue feathered head, mostly under the brim: face low, gold beak with a lit top, a dark eye (glint overlay)."""
    hcx, hcy, hrx, hry = c + HEAD[0], HEAD[1] + dy, HEAD[2], HEAD[3]
    head = {}
    for (x, y) in ell(hcx, hcy, hrx, hry):
        nx, ny = (x + .5 - hcx) / hrx, (y + .5 - hcy) / hry
        v = 0.64 - 0.4 * nx - 0.4 * ny + 0.14 * grain(x, y)
        if y % 2 == 0 and ((x // 2) + (y // 2)) % 2:            # overlapping feather rows (2 px dashes)
            v -= 0.22
        head[(x, y)] = qm(v, 'blue', 2, 6)
    brim = ell(c + BRIM[0], BRIM[1] + dy + hat_dy, BRIM[2], BRIM[3])
    for (x, y) in head:                                          # occlusion under the brim
        if (x, y - 1) in brim:
            head[(x, y)] = 'F0'
        elif (x, y - 2) in brim:
            head[(x, y)] = 'F1' if x > hcx else 'F2'
    for (x, y), ch in (((c + 2, 33), 'F5'), ((c + 3, 33), 'F6'), ((c + 2, 34), 'F4'), ((c + 1, 34), 'F5')):
        if (x, y + dy) in head:                                  # cheek feathers catch a little light
            head[(x, y + dy)] = ch
    ex, ey = c + EYE_OFF[0], EYE_OFF[1] + dy
    for p in ((ex, ey), (ex + 1, ey), (ex, ey + 1), (ex + 1, ey + 1)):
        head[p] = 'F0'
    cv.part(head, 'blue')
    beak = {(c + 5, 31): 'K1', (c + 6, 31): 'K2', (c + 7, 31): 'K1', (c + 5, 32): 'K0', (c + 6, 32): 'K1',
            (c + 7, 32): 'K0', (c + 6, 33): 'K0'}
    for (x, y), ch in beak.items():
        cv.put(x, y + dy, ch)
    return ex, ey


def draw_bell(cv, c, yt):
    for (x, y), ch in (((c + 3, yt + 2), 'A3'), ((c + 2, yt + 3), 'A2'), ((c + 3, yt + 3), 'A2'),
                       ((c + 4, yt + 3), 'A1'), ((c + 3, yt + 4), 'A0')):
        cv.put(x, y, ch)


def draw_hat(cv, c, hdy, flutter):
    """Weathered straw hat: wide oval brim (woven rings of clusters, darker toward the front, the crown's shadow on
    its right half, frayed front edge), a cylindrical crown with a rust band and a pinched oval top, an Azeos
    feather tucked in the band."""
    bcx, bcy, brx, bry = c + BRIM[0], BRIM[1] + hdy, BRIM[2], BRIM[3]
    brim = ell(bcx, bcy, brx, bry)
    # brim thickness / underside along the front
    under = {(x, y + 1) for (x, y) in brim if y + .5 > bcy}
    for (x, y) in sorted(under - brim):
        cv.put(x, y, 'S1' if (x // 2) % 3 else 'S0')
    for x in range(int(bcx - brx) + 2, int(bcx + brx) - 1, 3):  # frayed straw ends hanging off the front edge
        y = max(yy for (xx, yy) in brim if xx == x) + 2
        if (x, y - 1) in under and x % 2:
            cv.put(x, y, 'S1')
    pix = {}
    ccx = c - 1.0
    for (x, y) in brim:
        nx, ny = (x + .5 - bcx) / brx, (y + .5 - bcy) / bry
        r = (nx * nx + ny * ny) ** 0.5
        v = 0.72 - 0.4 * nx - 0.3 * ny + 0.16 * grain(x, y)
        if int(r * 4.2) % 2:                                      # woven rings
            v -= 0.12
        if x + .5 > ccx + 1 and y + .5 > bcy - 1:                 # crown's shadow on the right/front half
            v -= 0.18
        pix[(x, y)] = qm(v, 'straw', 3, 7)
    # sunlit strands on the back-left of the brim: a few small clusters only
    for (x, y), ch in (((-8, -1), 'S6'), ((-7, -1), 'S6'), ((-7, -2), 'S7'), ((-6, -2), 'S6'), ((-3, -3), 'S5'),
                       ((-2, -3), 'S6'), ((5, -2), 'S5'), ((6, -2), 'S5')):
        p = (int(bcx) + x, int(bcy) + y)
        if p in pix:
            pix[p] = ch
    cv.part(pix, 'straw')
    # crown
    crx, ctop, cbot = 3.4, 19.0 + hdy, 23.5 + hdy
    crown = {}
    for y in range(int(ctop), int(cbot) + 2):
        for x in range(int(ccx - crx) - 1, int(ccx + crx) + 2):
            dx = (x + 0.5 - ccx) / crx
            if abs(dx) <= 1 and ctop <= y and y + 0.5 <= cbot + 1.1 * max(0.0, 1 - dx * dx) ** 0.5:
                v = 0.66 - 0.7 * dx - 0.06 * (y - ctop) + 0.14 * grain(x, y)
                crown[(x, y)] = qm(v, 'straw', 3, 7)
    for x in range(int(ccx - crx), int(ccx + crx) + 1):          # rust band following the crown's curve
        dx = (x + 0.5 - ccx) / crx
        yb = round(cbot - 1 + 1.1 * max(0.0, 1 - dx * dx) ** 0.5)
        if (x, yb) in crown:
            crown[(x, yb)] = 'P6' if dx < -0.3 else ('P4' if dx < 0.5 else 'P2')
    for (x, y) in ell(ccx, ctop + 0.5, crx, 1.3):                # pinched top plane
        dx = (x + .5 - ccx) / crx
        crown[(x, y)] = 'S6' if dx < -0.3 and (x + y) % 3 else ('S5' if dx < 0.4 else 'S4')
    for x in range(int(ccx) - 1, int(ccx) + 1):
        crown[(x, int(ctop) + 1)] = 'S3'                          # crease
    cv.part(crown, 'straw')
    # Azeos feather tucked in the band, pointing up and back (navy quill side, mid-blue vane, brass tip)
    fx0, fy0 = int(ccx - crx) + 1, int(cbot) - 2
    tip = (fx0 - 3 - (1 if flutter else 0), fy0 - 7)
    for i, (x, y) in enumerate(line_px(fx0, fy0, tip[0], tip[1])):
        cv.put(x, y, 'F1'); cv.put(x + 1, y, 'F4' if i > 2 else 'F3')
    cv.put(tip[0], tip[1], 'A2'); cv.put(tip[0] + 1, tip[1], 'A1'); cv.put(tip[0], tip[1] - 1, 'A1')


def draw_crook(cv, c, dy, lantern_dx):
    """Shepherd's crook (2 px, lit left column, knots), a hook at the top and a small lantern hanging from it."""
    xb, xt, ytop = c + 8, c + 9, 11 + dy
    for y in range(ytop, GROUND + 1):
        i = GROUND - y
        x = round(xb + (xt - xb) * i / (GROUND - ytop))
        lit, sh = ('W3', 'W1') if i > 14 else ('W2', 'W1')
        if i % 7 == 4:
            lit, sh = 'W1', 'W0'                                  # knot
        cv.put(x, y, lit); cv.put(x + 1, y, sh)
    cv.put(xb, GROUND, 'W0'); cv.put(xb + 1, GROUND, 'W0')
    hook = [(0, -1), (1, -2), (2, -2), (3, -1)]
    for i, (a, b) in enumerate(hook):
        cv.put(xt + a, ytop + b, 'W4' if i == 1 else 'W3')
        cv.put(xt + a, ytop + b - 1, 'W0')
        cv.put(xt + a, ytop + b + 1, 'W1' if i else 'W2')
    cv.put(xt + 3, ytop, 'T0')
    lx, ly = xt + 1 + lantern_dx, ytop + 1
    cage = [('', 'T0', 'T3', 'T0', ''), ('T1', 'T2', 'T4', 'T2', 'T0'), ('T1', 'G1', 'G2', 'G1', 'T0'),
            ('T1', 'G2', 'G3', 'G1', 'T0'), ('T1', 'T2', 'T1', 'T1', 'T0'), ('', 'T0', 'T0', 'T0', '')]
    for j, row in enumerate(cage):
        for i, ch in enumerate(row):
            if ch:
                cv.put(lx + i, ly + j, ch, 'flame' if ch in MGLOW else '')
    # right glove on the staff
    gy0 = 39 + dy
    gx = round(xb + (xt - xb) * (GROUND - gy0) / (GROUND - ytop)) - 1
    for (a, b), ch in (((0, 0), 'L2'), ((1, 0), 'L3'), ((2, 0), 'L2'), ((0, 1), 'L1'), ((1, 1), 'L2'),
                       ((2, 1), 'L1'), ((0, 2), 'L0'), ((1, 2), 'L0'), ((2, 2), 'L0')):
        cv.put(gx + a, gy0 + b, ch)


def draw_figure(dy=0, hat_dy=0, lantern_dx=0, flutter=0, legs=STAND_LEGS, hand=None):
    """Returns (base, light, eye_xy) for one 64x64 cell."""
    cv = Canvas()
    c = CX
    draw_sack(cv, c, dy)
    (nx, nl), (fx, fl) = legs
    draw_leg(cv, c + fx - 2, fl, False, 44 + dy)
    draw_leg(cv, c + nx - 2, nl, True, 44 + dy)
    draw_poncho(cv, c, dy)
    ex, ey = draw_head(cv, c, dy, hat_dy)
    draw_bell(cv, c, 35 + dy)
    draw_hat(cv, c, dy + hat_dy, flutter)
    draw_crook(cv, c, dy, lantern_dx)
    if hand is not None:                                          # hat tip: sleeve from the shoulder, glove
        hxp, hyp = c + hand[0], hand[1] + dy
        for (x, y) in line_px(c - 3, 38 + dy, hxp + 1, hyp + 2):
            cv.put(x - 1, y, 'P1'); cv.put(x, y, 'P5'); cv.put(x + 1, y, 'P0')
        for (a, b), ch in (((0, 0), 'L2'), ((1, 0), 'L3'), ((2, 0), 'L2'), ((0, 1), 'L1'), ((1, 1), 'L2'),
                           ((2, 1), 'L1'), ((0, 2), 'L0'), ((1, 2), 'L0'), ((2, 2), 'L0')):
            cv.put(hxp - 1 + a, hyp - 1 + b, ch)
    base, light = cv.images()
    return base, light, (ex, ey)


def frames():
    """24 frames in sheet order: (base, light, eye_xy)."""
    out = []
    for f in range(10):
        out.append(draw_figure(dy=IDLE_DY[f], lantern_dx=IDLE_LANTERN[f], flutter=IDLE_FLUTTER[f]))
    for f in range(8):
        out.append(draw_figure(dy=WALK_DY[f], lantern_dx=WALK_LANTERN[f], flutter=f % 2, legs=WALK_LEGS[f]))
    for f, (hand, lift) in enumerate(WAVE):
        out.append(draw_figure(dy=0, hat_dy=lift, lantern_dx=[0, 0, 1, 1, 0, 0][f], flutter=f in (3, 4), hand=hand))
    return out


def cell_rects(w, h, rows):
    """[(name_index, x, y_top_image, unity_rect)] in frame order."""
    out, i = [], 0
    for r, n in enumerate(rows):
        for c in range(n):
            x, ytop = c * CELL, r * CELL
            out.append((i, x, ytop, [float(x), float(h - ytop - CELL), float(CELL), float(CELL)]))
            i += 1
    return out


# ---------------------------------------------------------------- idol (3/4 top-down: bale top plane, bell with oval crown and lip)
IDOL = [
    ".A............A.",
    "Acn....OO....ncA",
    ".nBn..O23O..nBn.",
    "..nBn.2..2.nBn..",
    "..ncBn1PP1nBcn..",
    "...nc146641cn...",
    "....1A6644a1....",
    "...1aA44Aaa21...",
    "...1aA4AAaa21...",
    "..1aA4AAaaa221..",
    ".1A66644AAaa21..",
    "15aAAAaaaaa22151",
    "1561222222221651",
    "1222P333333P2221",
    "O12P2222222P211O",
    ".OOOOOOOOOOOOOO.",
]
IDOL_SMALL = [  # 10 x 11, centred like the vanilla smallIcons (bbox 3,2 .. 13,13)
    ".A..OO..A.",
    ".n.O23O.n.",
    "..n1661n..",
    "..1A64a1..",
    "..1A4aa1..",
    ".1A44Aa21.",
    ".166AAa21.",
    "15aaaa2251",
    "1561111651",
    "12P3333P21",
    ".OOOOOOOO.",
]


def make_idol():
    icon = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    grid(icon, IDOL, 0, 0)
    small = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    grid(small, IDOL_SMALL, 3, 2)
    return icon, small, icon.copy()


# ---------------------------------------------------------------- validation
def validate(sheets, icon, small, placed, eye_cells):
    errs = []
    for name, img in sheets.items():
        w, h, rows = SHEETS[name]
        if img.size != (w, h):
            errs.append(f'{name}: size {img.size} != {(w, h)}')
        inside = Image.new('L', img.size, 0)
        d = ImageDraw.Draw(inside)
        for _, x, ytop, _ in cell_rects(w, h, rows):
            d.rectangle([x + 1, ytop + 1, x + CELL - 2, ytop + CELL - 2], fill=255)   # 1 px margin inside cells
        a, m = img.getchannel('A'), inside
        for y in range(h):
            for x in range(w):
                al = a.getpixel((x, y))
                if al not in (0, 255):
                    errs.append(f'{name}: soft alpha at {x},{y}')
                    break
                if al and not m.getpixel((x, y)):
                    errs.append(f'{name}: pixel outside a cell at {x},{y}')
                    break
                if al and img.getpixel((x, y))[:3] == (0, 0, 0):
                    errs.append(f'{name}: pure black at {x},{y}')
        for i, x, ytop, _ in cell_rects(w, h, rows):
            bb = img.crop((x, ytop, x + CELL, ytop + CELL)).getbbox()
            if bb is None:
                errs.append(f'{name}: frame {i} is empty')
            elif name == 'Merchant' and bb[3] - 1 != GROUND:
                errs.append(f'{name}: frame {i} ground row {bb[3] - 1} != {GROUND}')
    for nm, im in (('icon', icon), ('smallIcon', small), ('placed', placed)):
        if im.size != (16, 16):
            errs.append(f'idol {nm}: size {im.size}')
        if any(p[3] not in (0, 255) for p in im.getdata()):
            errs.append(f'idol {nm}: soft alpha')
    if icon.getbbox()[3] != 16:
        errs.append('idol icon does not sit on the bottom edge')
    # eye overlay must sit on the dark eye pixel of the base frame
    for i, (base_cell, ex, ey) in enumerate(eye_cells):
        if base_cell.getpixel((ex, ey))[3] != 255:
            errs.append(f'eye {i}: glint not over the head')
    return errs


# ---------------------------------------------------------------- preview
def scaled(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def on_bg(img, bg=(40, 36, 46, 255)):
    out = Image.new('RGBA', img.size, bg)
    out.alpha_composite(img)
    return out


def vanilla_idles():
    """Idle frame 0 of each vanilla merchant (reference only)."""
    m = os.path.join(REF, 'reference', 'merchants')
    res = []
    try:
        cav = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        for n in ('Merchant', 'Merchant_Light'):
            cav.alpha_composite(Image.open(os.path.join(m, 'CavelingMerchant', n + '.png')).convert('RGBA').crop((0, 0, 64, 64)))
        cav.alpha_composite(Image.open(os.path.join(m, 'CavelingMerchant', 'Merchant_Eye.png')).convert('RGBA').crop((0, 0, 64, 64)))
        res.append(('Caveling', cav))
        res.append(('Slime', Image.open(os.path.join(m, 'SlimeMerchant', 'Slime_merchant.png')).convert('RGBA').crop((0, 0, 30, 30))))
        res.append(('Fishing', Image.open(os.path.join(m, 'FishingMerchant', 'Fisher_merchant.png')).convert('RGBA').crop((0, 0, 26, 40))))
        res.append(('Void', Image.open(os.path.join(m, 'VoidMerchant', 'voidMerchant_idle.png')).convert('RGBA').crop((0, 0, 16, 35))))
    except OSError:
        pass
    return res


def vanilla_idols():
    d = os.path.join(REF, 'idol_reference')
    res = []
    for f in ('3930_MysteriousIdol_icon.png', '3931_SlimeOil_icon.png', '3932_PileOfChum_icon.png', '3937_VoidMerchantIdol_icon.png'):
        p = os.path.join(d, f)
        if os.path.exists(p):
            res.append(Image.open(p).convert('RGBA'))
    return res


def floor_tile(w, h):
    """Dark stone floor mock (16 px tiles, drawn from code)."""
    img = Image.new('RGBA', (w, h), hx('1b2a2a'))
    d = ImageDraw.Draw(img)
    for ty in range(0, h, 16):
        for tx in range(0, w, 16):
            d.rectangle([tx, ty, tx + 15, ty + 15], fill=hx('243837'))
            d.line([tx, ty, tx + 15, ty], fill=hx('2f4847'))
            d.line([tx, ty, tx, ty + 15], fill=hx('2f4847'))
            d.line([tx, ty + 15, tx + 15, ty + 15], fill=hx('142120'))
            d.line([tx + 15, ty, tx + 15, ty + 15], fill=hx('142120'))
            d.point([(tx + 5, ty + 6), (tx + 11, ty + 10), (tx + 3, ty + 12)], fill=hx('1b2a2a'))
    return img


def make_preview(fr, icon, path):
    S = 6
    W = 1600
    sheet = Image.new('RGBA', (W, 1180), (30, 27, 35, 255))
    d = ImageDraw.Draw(sheet)
    y = 10
    d.text((10, y), 'farmanimals: Wren Hayfeather (CavelingMerchant rig) | idol: Skyherd Bell (Azeos the Sky Titan)', fill=(240, 230, 200, 255))
    y += 20
    # idle frame 0 at 6x vs vanilla idles at 6x (bottoms aligned)
    ours = fr[0][0].copy()
    ours.alpha_composite(fr[0][1])
    x = 10
    row = [('Wren (ours)', ours)] + vanilla_idles()
    tallest = max(im.height for _, im in row) * S
    for label, im in row:
        sc = scaled(on_bg(im), S)
        sheet.alpha_composite(sc, (x, y + tallest - sc.height))
        d.text((x, y + tallest + 4), label, fill=(220, 220, 220, 255))
        x += sc.width + 16
    # the same row at 1x (bottoms aligned)
    x1 = x + 20
    d.text((x1, y), '1x', fill=(220, 220, 220, 255))
    for label, im in row:
        sheet.alpha_composite(on_bg(im), (x1, y + tallest - im.height))
        x1 += im.width + 6
    y += tallest + 26
    # walk + emote strip (3x), idle strip too
    d.text((10, y), 'idle 0-9 / walk 10-17 / hat-tip wave 18-23 (3x, light overlay composited)', fill=(220, 220, 220, 255))
    y += 16
    for seg in (range(0, 10), range(10, 18), range(18, 24)):
        x = 10
        for i in seg:
            im = fr[i][0].copy()
            im.alpha_composite(fr[i][1])
            ex, ey = fr[i][2] if i < 18 else fr[10][2]
            im.putpixel((ex, ey), PAL['e'])
            sc = scaled(on_bg(im.crop((12, 4, 52, 58))), 3)
            sheet.alpha_composite(sc, (x, y))
            d.text((x + 2, y + 2), str(i), fill=(255, 255, 255, 255))
            x += sc.width + 4
        y += 54 * 3 + 6
    # idol 1x and 8x next to vanilla idols
    d.text((10, y), 'perspective self-check: idol icon 1x and 8x (ours first, then Mysterious Idol, Slime Oil, Pile of Chum, Void Merchant Idol)', fill=(220, 220, 220, 255))
    y += 16
    idols = [icon] + vanilla_idols()
    x = 10
    for im in idols:
        sheet.alpha_composite(on_bg(im), (x, y))
        x += 24
    x = 10
    y += 24
    for im in idols:
        sheet.alpha_composite(scaled(on_bg(im), 8), (x, y))
        x += 16 * 8 + 12
    # placed on a stone floor mock, 1x and 6x
    fl = floor_tile(48, 48)
    fl.alpha_composite(icon, (16, 16))
    sheet.alpha_composite(fl, (x + 10, y))
    sheet.alpha_composite(scaled(fl, 6), (x + 70, y - 100))
    d.text((x + 70, y - 116), 'placed on stone floor (1x / 6x)', fill=(220, 220, 220, 255))
    sheet.save(path)


def main():
    os.makedirs(OUT_MERCH, exist_ok=True)
    os.makedirs(OUT_IDOL, exist_ok=True)
    os.makedirs(OUT_PREV, exist_ok=True)
    fr = frames()
    sheets = {n: Image.new('RGBA', (w, h), (0, 0, 0, 0)) for n, (w, h, _) in SHEETS.items()}
    eye_checks = []
    for i, x, ytop, _ in cell_rects(*SHEETS['Merchant']):
        sheets['Merchant'].alpha_composite(fr[i][0], (x, ytop))
        sheets['Merchant_Light'].alpha_composite(fr[i][1], (x, ytop))
    for i, x, ytop, _ in cell_rects(*SHEETS['Merchant_Eye']):
        base, _, (ex, ey) = fr[i]
        sheets['Merchant_Eye'].putpixel((x + ex, ytop + ey), PAL['e'])
        eye_checks.append((base, ex, ey))
    # the wave clip shows Merchant_Eye_10 (= move frame 0): the head must be there in every wave frame
    for i in range(18, 24):
        eye_checks.append((fr[i][0],) + fr[10][2])
    icon, small, placed = make_idol()

    errs = validate(sheets, icon, small, placed, eye_checks)
    textures = []
    for n, (w, h, rows) in SHEETS.items():
        out = os.path.join(OUT_MERCH, f'{KEY}__{n}.png')
        sheets[n].save(out)
        textures.append({
            'output': f'{KEY}/{KEY}__{n}.png',
            'replacesTexture': n,
            'size': [w, h],
            'legacySprites': [{'name': f'{n}_{i}', 'rect': rect, 'pivot': [0.5, 0.5], 'pixelsPerUnit': 16.0}
                              for i, _, _, rect in cell_rects(w, h, rows)],
            'renderer': f'/CavelingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/{RENDERER[n]}',
        })
    # cross-check the layout against the shared skins.json entry for this rig (if present)
    try:
        skins = json.load(open(os.path.join(ART, 'merchants', 'skins.json')))
        for m in skins['merchants'].values():
            if m.get('baseMerchant') != 'CavelingMerchant':
                continue
            for t in m['textures']:
                mine = next(x for x in textures if x['replacesTexture'] == t['replacesTexture'])
                if [s['rect'] for s in t['legacySprites']] != [s['rect'] for s in mine['legacySprites']] or t['size'] != mine['size']:
                    errs.append(f"layout of {t['replacesTexture']} differs from skins.json")
            break
    except (OSError, KeyError, StopIteration):
        pass
    with open(os.path.join(OUT_MERCH, 'sheets.json'), 'w') as f:
        json.dump({'key': KEY, 'rig': 'CavelingMerchantEntity', 'kind': 'legacy', 'textures': textures}, f, indent=1)
    icon.save(os.path.join(OUT_IDOL, f'{KEY}_icon.png'))
    small.save(os.path.join(OUT_IDOL, f'{KEY}_smallIcon.png'))
    placed.save(os.path.join(OUT_IDOL, f'{KEY}_placed.png'))
    make_preview(fr, icon, os.path.join(OUT_PREV, f'{KEY}.png'))
    if errs:
        print('\n'.join(errs[:40]))
        print(f'FAILED: {len(errs)} problem(s)')
        return 1
    print('ok: sheets', {n: im.size for n, im in sheets.items()}, 'idol 16x16 x3, preview written')
    return 0


if __name__ == '__main__':
    sys.exit(main())
