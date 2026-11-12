"""Boss Merchants art for merchant key `eggs`: Ankhra the Clutch-Keeper + the Brood Egg Idol (Ra-Akar).

    python -I BossMerchants/Unity/make_eggs_art.py

Ankhra is a squat scarab beetle-folk from Ra-Akar's brood: green spiked carapace like the Sand Titan,
a single golden eye under a curved gold horn, red-orange claws, a gold-and-lapis collar, and always one egg
cradled in front. Her emote holds the egg out, it wobbles, cracks, and a hatchling peeks out.

Rig: VoidMerchant (SpriteAsset `voidMerchant`): idle 128x35 (8 x 16x35), move 96x34 (6 x 16x34),
emote 621x37 (27 x 23x37, body pivot x = 8). Pivot is bottom centre, so the feet stand on the last row.

Ankhra is drawn procedurally (shaded ellipses + hand-placed props, see the restyle note below); the idols come from
the hand-authored pixel grids. No vanilla pixels are copied. The preview
sheet reads the extracted vanilla references in build/bossmerchants for side-by-side comparison only.
Outputs (paths relative to BossMerchants/):
  Unity/Art/merchants/eggs/eggs__voidMerchant_{idle,move,emote}.png + sheets.json
  Unity/Art/idols/eggs_{icon,smallIcon,placed}.png
  Unity/Art/previews/eggs.png
Validation (exit 1 on failure): sheet sizes = rig sizes, every frame has pixels and stays inside its cell,
hard alpha only, idols 16x16, feet on the ground row.
"""
import json, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
ROOT = os.path.dirname(MOD)
ART = os.path.join(HERE, 'Art')
OUT_M = os.path.join(ART, 'merchants', 'eggs')
OUT_I = os.path.join(ART, 'idols')
OUT_P = os.path.join(ART, 'previews')
REF = os.path.join(ROOT, 'build', 'bossmerchants')

# ---------------------------------------------------------------- palette
# Ramps sampled from vanilla Ra-Akar (Scarab_boss) and the Ra-Akar urn; shadows shift cool, highlights warm.
PAL = {
    # carapace green (Scarab_boss): warm light -> teal-indigo outline
    'g': '#a3ce4a', 'G': '#5ca537', 'h': '#239029', 'H': '#0a6d27', 'k': '#073c31', 'K': '#0e2522',
    # gold horn/spikes/collar (Scarab_boss spikes)
    'y': '#f2df3a', 'Y': '#e2a12c', 'o': '#c16436',
    # red-orange claws/legs (Scarab_boss legs), maroon outline
    'l': '#c16436', 'r': '#c5460a', 'R': '#91210b', 'd': '#5e2a28',
    # lapis collar band (Ra-Akar urn blue)
    'B': '#4f7fd0', 'n': '#2c4fa6', 'N': '#1a2557',
    # egg shell (vanilla Egg icon cream ramp), outline browns
    'e': '#fffbe8', 'E': '#efe2c0', 'f': '#d6bf8e', 'q': '#ad8f62', 'F': '#6f4f35',
    # eye pupil
    'p': '#23281e',
    # sandstone (idol plinth, desert temple ramp)
    'S': '#f0deae', 's': '#d8bb7c', 't': '#b08a52', 'T': '#7c5634', 'u': '#45291f',
}
RGBA = {k: tuple(int(v[i:i + 2], 16) for i in (1, 3, 5)) + (255,) for k, v in PAL.items()}


def grid(rows):
    w = max(len(r) for r in rows)
    assert all(len(r) == w for r in rows), [len(r) for r in rows]
    return rows


def put(img, rows, x0, y0, skip='.'):
    W, H = img.size
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == skip or ch == ' ':
                continue
            x, y = x0 + i, y0 + j
            if 0 <= x < W and 0 <= y < H:
                img.putpixel((x, y), RGBA[ch])


# ---------------------------------------------------------------- Ankhra (coordinates on a 16x35 cell)
# Restyle (2026-11-12, after Old Hemlock's in-game restyle; reference = the vanilla Void Merchant on the same rig,
# checked against all four vanilla merchants). The previous pass was a bright, saturated, front-on blob: a big
# flat lime shell, a large cream egg and a black-ish outline all round. This pass:
#   - dark, muted ramps (6-8 close steps per material, picked from the finisher's vanilla-sampled master palette)
#     shaded in irregular ~2x2 grain clusters; only tiny accents (horn tip, eye, one egg highlight) go bright;
#   - no outline colour: each part's rim is a dark step of its own ramp (one step lighter on lit top/left edges);
#   - turned 3/4 toward screen-right: the domed shell is seen from above with its seam leaning toward the camera,
#     the head sits low and right under the shell's front rim, the eye on the near side of the face;
#   - mass moved into thin props: a curved gold horn and two thin antennae crossing the shell, gold rim spikes,
#     thin segmented legs and arms, a smaller tan egg instead of one wide bright mass.
EPAL = {   # all from build/bossmerchants/finisher_cache/master_palette.json
    # carapace: deep moss green, teal-black shadows, a dull sage sheen
    'C0': '23281e', 'C1': '23281e', 'C2': '373446', 'C3': '2a4e4a', 'C4': '2b5432', 'C5': '5e6e4d',
    'C6': '82907c',
    # old gold (horn, spikes, collar)
    'G0': '474130', 'G1': '474130', 'G2': '624f32', 'G3': '875e2c', 'G4': 'a88b37', 'G5': 'c9ab51',
    # rust claws / legs / antennae
    'R0': '352a1c', 'R1': '474453', 'R2': '6b4545', 'R3': '813530', 'R4': '813530', 'R5': 'b16a3c',
    # lapis band
    'B0': '232930', 'B1': '273fa7', 'B2': '273fa7', 'B3': '4b708a', 'B4': '677fae',
    # egg shell (dusty tan, one small highlight)
    'E0': '474130', 'E1': '645d53', 'E2': '82706d', 'E3': '82706d', 'E4': 'a98963', 'E5': 'a98963',
    # eye
    'Y1': 'e2a12c', 'P0': '23281e',
}
ERGBA = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)) + (255,) for k, v in EPAL.items()}
RAMP = {r: sorted(k for k in EPAL if k[0] == r) for r in 'CGRBE'}


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


def q(v, ramp, lo=2, hi=None):
    """Quantise light value v (0..1) onto RAMP[ramp][lo:hi] (darkest steps stay for rims and occlusion)."""
    r = RAMP[ramp][lo:hi]
    return r[max(0, min(len(r) - 1, int(v * len(r))))]


def ellipse(cx, cy, rx, ry):
    return {(x, y) for y in range(int(cy - ry) - 1, int(cy + ry) + 2)
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2)
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0}


class Cv:
    def __init__(self, W, H):
        self.W, self.H, self.px = W, H, {}

    def put(self, x, y, k):
        if 0 <= x < self.W and 0 <= y < self.H:
            self.px[(x, y)] = k

    def line(self, pts):
        for (x, y), k in pts:
            self.put(x, y, k)

    def part(self, pix, ramp):
        """{(x, y): key}; the part's own boundary becomes its ramp's dark steps (step 1 on shadow sides,
        step 2 where only the lit top/left side is open), so edges are material-tinted, never black."""
        R = RAMP[ramp]
        for (x, y), k in pix.items():
            open_ = [(a, b) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (x + a, y + b) not in pix]
            if open_:
                k = R[2] if all(o in ((-1, 0), (0, -1)) for o in open_) else R[1]
            self.put(x, y, k)

    def image(self):
        img = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))
        for (x, y), k in self.px.items():
            img.putpixel((x, y), ERGBA[k])
        return img


def draw_shell(cv, ox, oy):
    """Domed elytra on her back, seen from above: the crown shows behind the head, the rest flanks the body on
    the far (left) side. Lit top-left sheen, spine seam, dark underside rim."""
    cx, cy, rx, ry = ox + 7.2, oy + 20.2, 7.2, 10.6
    pix = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        v = 0.50 - 0.40 * ny - 0.18 * nx + 0.14 * grain(x, y)
        if (nx + 0.4) ** 2 + (ny + 0.55) ** 2 < 0.02:
            v += 0.45                                    # small sheen on the crown
        sx = cx + 0.6 - 1.0 * (ny + 1) / 2              # spine seam
        if ny > -0.9 and abs(x + 0.5 - sx) < 0.5:
            v -= 0.34
        elif ny > -0.9 and 0.5 <= sx - (x + 0.5) < 1.5:
            v += 0.10
        if ny > 0.8 and abs(x + 0.5 - sx) < 1.0:
            continue                                     # the elytra part at the tip
        if ny > 0.72:
            v -= 0.22                                    # underside of the rim
        pix[(x, y)] = q(v, 'C', 1, 6)
    cv.part(pix, 'C')
    # gold spikes: one over the crown, studs down the outer rim
    cv.line([((ox + 3, oy + 11), 'G3'), ((ox + 3, oy + 10), 'G4'), ((ox + 1, oy + 15), 'G3'), ((ox + 1, oy + 16), 'G1'),
             ((ox + 0, oy + 20), 'G2'), ((ox + 0, oy + 21), 'G1'), ((ox + 1, oy + 25), 'G2'), ((ox + 2, oy + 26), 'G1')])


def draw_antennae(cv, ox, oy):
    """Two thin rust antennae from the head top, swept back, knobbed tips."""
    cv.line([((ox + 8, oy + 6), 'R2'), ((ox + 7, oy + 5), 'R3'), ((ox + 7, oy + 4), 'R2'), ((ox + 6, oy + 3), 'R3'),
             ((ox + 5, oy + 2), 'R2'), ((ox + 4, oy + 2), 'R3'), ((ox + 4, oy + 1), 'R1')])
    cv.line([((ox + 12, oy + 6), 'R2'), ((ox + 13, oy + 5), 'R3'), ((ox + 13, oy + 4), 'R2'), ((ox + 14, oy + 3), 'R3'),
             ((ox + 14, oy + 2), 'R2'), ((ox + 15, oy + 1), 'R3'), ((ox + 15, oy + 2), 'R1')])


def draw_thorax(cv, ox, oy):
    cx, cy, rx, ry = ox + 9.6, oy + 18.8, 4.2, 4.0
    pix = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        v = 0.50 - 0.45 * ny - 0.25 * nx + 0.12 * grain(x + 1, y)
        pix[(x, y)] = q(v, 'C', 1, 6)
    cv.part(pix, 'C')


def draw_head(cv, ox, oy, blink):
    """Head on top, turned toward screen-right: lit top plane, darker face, one gold eye on the near side,
    a curved gold horn rising off the brow and leaning back."""
    cx, cy, rx, ry = ox + 10.0, oy + 9.6, 4.3, 3.8
    pix = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        v = 0.55 - 0.65 * ny - 0.25 * nx + 0.12 * grain(x, y + 1)
        pix[(x, y)] = q(v, 'C', 1, 6)
    pix[(ox + 8, oy + 7)] = 'C6'
    cv.part(pix, 'C')
    ex, ey = ox + 11, oy + 10
    if blink:
        cv.line([((ex - 1, ey), 'C1'), ((ex, ey), 'C1'), ((ex + 1, ey), 'C2')])
    else:
        cv.line([((ex - 1, ey), 'G3'), ((ex, ey), 'Y1'), ((ex + 1, ey), 'P0'), ((ex, ey + 1), 'G1'),
                 ((ex - 1, ey + 1), 'C1'), ((ex + 1, ey + 1), 'C0')])
    cv.line([((ox + 8, oy + 13), 'R3'), ((ox + 9, oy + 13), 'R1'), ((ox + 12, oy + 13), 'R3'), ((ox + 13, oy + 13), 'R1')])
    cv.line([((ox + 10, oy + 6), 'G4'), ((ox + 11, oy + 6), 'G2'), ((ox + 10, oy + 5), 'G4'), ((ox + 11, oy + 5), 'G2'),
             ((ox + 9, oy + 4), 'G4'), ((ox + 10, oy + 4), 'G2'), ((ox + 9, oy + 3), 'G4'), ((ox + 10, oy + 3), 'G2'),
             ((ox + 8, oy + 2), 'G3'), ((ox + 9, oy + 2), 'G1'), ((ox + 8, oy + 1), 'G5')])


def draw_collar(cv, ox, oy):
    """Narrow broad-collar under the jaw: old-gold upper row (lit left), lapis band."""
    for x in range(ox + 7, ox + 13):
        cv.put(x, oy + 14, 'G4' if x < ox + 9 else 'G3' if (x + grain(x, 0)) % 3 else 'G2')
    cv.put(ox + 13, oy + 14, 'G1')
    for x in range(ox + 6, ox + 15):
        cv.put(x, oy + 15, 'B1' if x in (ox + 6, ox + 14) else 'B3' if x < ox + 9 else 'B2')


def draw_body(cv, ox, oy, feet):
    """Short banded abdomen and thin segmented rust legs (far leg darker)."""
    cx, cy, rx, ry = ox + 8.8, oy + 28.0, 5.4, 4.4
    pix = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        v = 0.50 - 0.45 * ny - 0.30 * nx + 0.12 * grain(x, y)
        if (y - oy) % 3 == 0:
            v -= 0.22
        pix[(x, y)] = q(v, 'C', 1, 5)
    cv.part(pix, 'C')
    fl, fr = feet
    cv.line([((ox + 4, oy + 30 + fl), 'R1'), ((ox + 3, oy + 31 + fl), 'R2'), ((ox + 3, oy + 32 + fl), 'R1'),
             ((ox + 2, oy + 33 + fl), 'R2'), ((ox + 1, oy + 34 + fl), 'R1'), ((ox + 2, oy + 34 + fl), 'R0')])
    cv.line([((ox + 6, oy + 30 + fl), 'R3'), ((ox + 7, oy + 30 + fl), 'R2'), ((ox + 6, oy + 31 + fl), 'R3'),
             ((ox + 5, oy + 32 + fl), 'R3'), ((ox + 6, oy + 32 + fl), 'R1'), ((ox + 5, oy + 33 + fl), 'R2'),
             ((ox + 4, oy + 34 + fl), 'R2'), ((ox + 5, oy + 34 + fl), 'R3'), ((ox + 6, oy + 34 + fl), 'R1')])
    cv.line([((ox + 10, oy + 30 + fr), 'R3'), ((ox + 11, oy + 30 + fr), 'R2'), ((ox + 11, oy + 31 + fr), 'R3'),
             ((ox + 11, oy + 32 + fr), 'R3'), ((ox + 12, oy + 32 + fr), 'R1'), ((ox + 12, oy + 33 + fr), 'R2'),
             ((ox + 11, oy + 34 + fr), 'R2'), ((ox + 12, oy + 34 + fr), 'R3'), ((ox + 13, oy + 34 + fr), 'R1')])
    cv.line([((ox + 7, oy + 31 + fl), 'R1'), ((ox + 7, oy + 32 + fl), 'R0'), ((ox + 6, oy + 33 + fl), 'R1'),
             ((ox + 12, oy + 31 + fr), 'R1'), ((ox + 13, oy + 32 + fr), 'R0'), ((ox + 13, oy + 33 + fr), 'R1'),
             ((ox + 3, oy + 33 + fl), 'R0'), ((ox + 4, oy + 33 + fl), 'R1')])
    cv.line([((ox + 14, oy + 31 + fr), 'R2'), ((ox + 14, oy + 32 + fr), 'R1'), ((ox + 15, oy + 33 + fr), 'R2'),
             ((ox + 14, oy + 34 + fr), 'R1'), ((ox + 15, oy + 34 + fr), 'R0')])


def egg_pix(ex, ey):
    cx, cy, rx, ry = ex + 3.5, ey + 4.1, 3.6, 4.1
    pix = {}
    for (x, y) in ellipse(cx, cy, rx, ry):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        v = 0.62 - 0.40 * ny - 0.36 * nx + 0.10 * grain(x + 1, y)
        pix[(x, y)] = q(v, 'E', 1, 5)
    pix[(ex + 2, ey + 1)] = 'E5'       # the one bright spot
    pix[(ex + 1, ey + 2)] = 'E4'
    pix[(ex + 2, ey + 2)] = 'E4'
    return pix


def draw_egg(cv, ex, ey, state='whole', baby_blink=False):
    pix = egg_pix(ex, ey)
    if state == 'whole':
        cv.part(pix, 'E')
        return
    if state == 'crack':
        cv.part(pix, 'E')
        cv.line([((ex + 1, ey + 4), 'E0'), ((ex + 2, ey + 3), 'E0'), ((ex + 3, ey + 4), 'E1'), ((ex + 4, ey + 4), 'E0'),
                 ((ex + 5, ey + 3), 'E0')])
        return
    lift, dx = (2, 1) if state == 'open' else (3, 0)
    # hatchling: a tiny beetle head (lit top, gold eye) peeking out of the shell
    cv.line([((ex + 2, ey + 2), 'C5'), ((ex + 3, ey + 2), 'C5'), ((ex + 4, ey + 2), 'C4'),
             ((ex + 1, ey + 3), 'C4'), ((ex + 2, ey + 3), 'C3'),
             ((ex + 3, ey + 3), 'C3' if baby_blink else 'G4'), ((ex + 4, ey + 3), 'C2'), ((ex + 5, ey + 3), 'C2')])
    base = {p: k for p, k in pix.items() if p[1] >= ey + 4}
    for x in range(ex, ex + 7, 2):
        base.pop((x, ey + 4), None)       # jagged rim
    cv.part(base, 'E')
    cap = {(x + dx, y - lift): k for (x, y), k in pix.items() if y < ey + 4}
    cv.part(cap, 'E')




def ankhra(W=16, H=35, ox=0, oy=0, up=0, egg_up=0, blink=False, feet=(0, 0), egg=None,
           egg_state='whole', baby_blink=False, sparks=()):
    """Compose one frame. oy moves the whole figure down (taller emote cell). up / egg_up raise the
    head+shell / egg+arms. feet = vertical offset of each leg (negative = lifted). egg=(dx, dy): held out."""
    cv = Cv(W, H)
    draw_shell(cv, ox, oy - up)
    draw_body(cv, ox, oy, feet)
    draw_thorax(cv, ox, oy - min(up, egg_up))
    draw_collar(cv, ox, oy - up)
    draw_head(cv, ox, oy - up, blink)
    draw_antennae(cv, ox, oy - up)
    if egg is None:
        ex, ey = ox + 5, oy + 18 - egg_up
        cv.line([((ox + 6, oy + 16), 'R3'), ((ox + 5, oy + 17), 'R2'), ((ox + 4, oy + 18), 'R3'), ((ox + 4, oy + 19), 'R2'),
                 ((ox + 4, oy + 20 - egg_up), 'R3'), ((ox + 4, oy + 21 - egg_up), 'R1'),
                 ((ox + 13, oy + 16), 'R2'), ((ox + 14, oy + 17), 'R3'), ((ox + 14, oy + 18), 'R2'), ((ox + 14, oy + 19), 'R1'),
                 ((ox + 13, oy + 20 - egg_up), 'R2'), ((ox + 14, oy + 20 - egg_up), 'R1')])
        draw_egg(cv, ex, ey, egg_state, baby_blink)
        cv.line([((ex, ey + 5), 'R4'), ((ex, ey + 6), 'R3'), ((ex + 1, ey + 7), 'R2'),     # claws in front of the egg
                 ((ex + 7, ey + 5), 'R3'), ((ex + 7, ey + 6), 'R2'), ((ex + 6, ey + 7), 'R1')])
    else:
        dx, dy = egg
        ex, ey = ox + 5 + dx, oy + 18 + dy
        cv.line([((ox + 6, oy + 16), 'R3'), ((ox + 5, oy + 17), 'R2'), ((ox + 4, oy + 18), 'R3'),   # far arm hangs
                 ((ox + 4, oy + 19), 'R2'), ((ox + 4, oy + 20), 'R3'), ((ox + 4, oy + 21), 'R2'), ((ox + 5, oy + 22), 'R1')])
        draw_egg(cv, ex, ey, egg_state, baby_blink)
        for x in range(ex - 1, ex + 9):                            # a little straw nest on the palm
            cv.put(x, ey + 7, 'G0' if x in (ex - 1, ex + 8) else ('G2' if (x - ex) // 2 % 2 else 'G1'))
        for x in range(ex, ex + 8):
            cv.put(x, ey + 8, 'G0' if (x - ex) // 2 % 2 else 'G1')
        py = ey + 9                                                # palm row under the nest
        sx = ox + 13
        cv.put(sx, oy + 16 - up, 'R2')
        for y in range(oy + 17 - up, py + 1):                     # upper arm
            cv.put(sx, y, 'R2'); cv.put(sx + 1, y, 'R1')
        for x in range(min(sx, ex + 1), max(sx + 2, ex + 7)):     # forearm, lit top row
            cv.put(x, py, 'R3'); cv.put(x, py + 1, 'R2'); cv.put(x, py + 2, 'R1')
        cv.line([((ex + 1, py - 1), 'R4'), ((ex + 6, py - 1), 'R2')])   # claw tips
    for (sx, sy, big) in sparks:
        sx, sy = sx + 1, sy - 6                                    # spark sets were placed for the old, lower egg
        if big:
            cv.line([((sx + 1, sy), 'G3'), ((sx, sy + 1), 'G3'), ((sx + 1, sy + 1), 'G5'), ((sx + 2, sy + 1), 'G3'),
                     ((sx + 1, sy + 2), 'G3')])
        else:
            cv.put(sx, sy, 'G4')
    return cv.image()


# ---------------------------------------------------------------- animations
IDLE_UP = [0, 0, 1, 1, 1, 1, 0, 0]
IDLE_EGG = [0, 0, 0, 1, 1, 1, 1, 0]


def idle_frame(i, **kw):
    return ankhra(up=IDLE_UP[i], egg_up=IDLE_EGG[i], blink=(i == 6), **kw)


def make_idle():
    return [idle_frame(i) for i in range(8)]


def make_move():
    feet = [(-1, 0), (-2, 0), (-1, 0), (0, -1), (0, -2), (0, -1)]
    bob = [0, 1, 0, 0, 1, 0]
    frames = []
    for i in range(6):
        f = ankhra(up=bob[i], egg_up=bob[i], feet=feet[i])
        frames.append(f.crop((0, 1, 16, 35)))      # move cell is 34 tall, same ground row
    return frames


def make_emote():
    W, H, oy = 23, 37, 2
    F = []
    for i in range(5):                                         # 0-4: idle lead-in
        F.append(idle_frame(i, W=W, H=H, oy=oy))
    for dx, dy in [(2, -1), (4, -2), (6, -3), (8, -4)]:        # 5-8: hold the egg out
        F.append(ankhra(W, H, oy=oy, up=1, egg=(dx, dy)))
    out = (9, -4)
    spark_sets = [[(19, 19, 1)], [(12, 21, 0), (21, 20, 0)], [(11, 23, 1)], [(21, 25, 0), (12, 23, 0)]]
    for s in spark_sets:                                       # 9-12: it glows
        F.append(ankhra(W, H, oy=oy, up=1, egg=out, sparks=s))
    F.append(ankhra(W, H, oy=oy, up=1, egg=(8, -4)))           # 13: wobble
    F.append(ankhra(W, H, oy=oy, up=1, egg=(10, -4)))          # 14: wobble
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, egg_state='crack'))            # 15: crack
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, egg_state='open2'))            # 16: cap pops
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open'))             # 17: hatchling peeks
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open', baby_blink=True))  # 18: it blinks
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open'))             # 19
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, sparks=[(19, 18, 1), (12, 24, 0)]))  # 20: cap shut, sparkle
    for dx, dy in [(8, -4), (6, -3), (4, -2), (2, -1)]:         # 21-24: bring it back
        F.append(ankhra(W, H, oy=oy, up=1, egg=(dx, dy)))
    F.append(idle_frame(6, W=W, H=H, oy=oy))                   # 25-26: settle
    F.append(idle_frame(7, W=W, H=H, oy=oy))
    assert len(F) == 27
    return F


def strip(frames, cw, ch):
    img = Image.new('RGBA', (cw * len(frames), ch), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        assert f.size == (cw, ch), (f.size, cw, ch)
        img.alpha_composite(f, (i * cw, 0))
    return img


# ---------------------------------------------------------------- idol: Brood Egg Idol
IDOL_ICON = grid([        # 3/4 top-down: brood egg (lit top) clasped by a sand scarab seen from above, on a slab
    '......qqqF......',
    '.....qeeeEF.....',
    '....qeeeeEfF....',
    '....qEeeEEfF....',
    '....qEEEEEfF....',
    '...dFEEEEEfFd...',
    '..dlhhggggHkRd..',
    '..dlhgeegGHHRd..',
    '..ddkGGGHhHKdd..',
    '.uSSSkKKKKKSStu.',
    '.uSSSSSSSSSSstu.',
    '.usssssssssstTu.',
    '.uBBBnnnnnnnnNu.',
    '.uttttttttttTTu.',
    '..uTTTTTTTTTTu..',
    '...uuuuuuuuuu...',
])
IDOL_SMALL = grid([
    '................',
    '................',
    '......qqqF......',
    '.....qeeEfF.....',
    '.....qEEEfF.....',
    '....dhggGHkd....',
    '....lhgeGHkR....',
    '....uSkKKKSu....',
    '....uSSSSStu....',
    '....uBBnnnNu....',
    '....utttttTu....',
    '.....uuuuuu.....',
    '................',
    '................',
    '................',
    '................',
])


def make_idols():
    icon = Image.new('RGBA', (16, 16), (0, 0, 0, 0)); put(icon, IDOL_ICON, 0, 0)
    small = Image.new('RGBA', (16, 16), (0, 0, 0, 0)); put(small, IDOL_SMALL, 0, 0)
    return icon, small, icon.copy()


# ---------------------------------------------------------------- outputs
RIG = {
    'idle': ('voidMerchant_idle', (128, 35), 16, 8, '580fae3f-66dc-4b7a-900c-ee7986f742e6', [0.5, 0.0], 10.0, 1),
    'move': ('voidMerchant_move', (96, 34), 16, 6, '7128f66e-0c6d-4aed-949e-93fdcd9f5138', [0.5, 0.0], 11.0, 1),
    'emote': ('voidMerchant_emote', (621, 37), 23, 27, 'e1e86c38-70e3-402a-a63f-ecb6d1f3edf0',
              [0.3478260934352875, 0.0], 14.0, 0),
}
SPRITE_ASSET_ADDRESS = [9202072890546798702, -5828687261374344469]


def validate(sheets, idols):
    errs = []
    for key, img in sheets.items():
        tex, size, cw, n, *_ = RIG[key]
        if img.size != size:
            errs.append(f'{key}: size {img.size} != {size}')
        if cw * n != size[0]:
            errs.append(f'{key}: cell layout mismatch')
        alphas = {p[3] for p in img.get_flattened_data()}
        if not alphas <= {0, 255}:
            errs.append(f'{key}: soft alpha {sorted(alphas)[:5]}')
        for i in range(n):
            cell = img.crop((i * cw, 0, i * cw + cw, size[1]))
            bb = cell.getbbox()
            if bb is None:
                errs.append(f'{key} frame {i}: empty')
                continue
            if bb[3] != size[1]:
                errs.append(f'{key} frame {i}: feet not on the ground row (bbox {bb})')
            if key == 'emote':
                body = cell.crop((0, 0, 16, size[1])).getbbox()
                if body is None or body[0] > 1:
                    errs.append(f'emote frame {i}: body not at the pivot column')
    for name, img in idols.items():
        if img.size != (16, 16):
            errs.append(f'idol {name}: size {img.size}')
        if not {p[3] for p in img.get_flattened_data()} <= {0, 255}:
            errs.append(f'idol {name}: soft alpha')
        if img.getbbox() is None:
            errs.append(f'idol {name}: empty')
    icon = idols['icon']
    if icon.getbbox()[3] != 16:
        errs.append('idol icon does not sit on the bottom edge')
    return errs


def load_ref(path):
    return Image.open(path).convert('RGBA') if os.path.exists(path) else None


def ref_frame(merchant, texture, sprite):
    ref = os.path.join(REF, 'reference', 'reference.json')
    img = load_ref(os.path.join(REF, 'reference', 'merchants', merchant, texture + '.png'))
    if img is None or not os.path.exists(ref):
        return None
    meta = json.load(open(ref))['merchants'][merchant]['sheets'][texture]
    for s in meta:
        if s['name'] == sprite:
            x, y, w, h = [int(v) for v in s['rect']]
            return img.crop((x, img.height - y - h, x + w, img.height - y))
    return None


def scale(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def floor_mock(icon, tiles=3, s=6):
    """Idol placed in the middle of a dark stone floor (own procedural tiles)."""
    T = 16
    fl = Image.new('RGBA', (T * tiles, T * tiles), (0, 0, 0, 255))
    stone = [(54, 50, 58), (47, 43, 51), (40, 37, 45), (33, 30, 38), (63, 58, 66)]
    for ty in range(tiles):
        for tx in range(tiles):
            for y in range(T):
                for x in range(T):
                    v = (x * 7 + y * 13 + tx * 5 + ty * 11 + (x * y) % 5) % 9
                    c = stone[0] if v < 4 else stone[1] if v < 7 else stone[2]
                    if x == 0 or y == 0:
                        c = stone[3]
                    elif x == 1 or y == 1:
                        c = stone[4]
                    fl.putpixel((tx * T + x, ty * T + y), c + (255,))
    mid = T * (tiles // 2)
    shadow = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((2, 12, 13, 15), fill=(16, 14, 20, 255))
    fl.alpha_composite(shadow, (mid, mid))
    fl.alpha_composite(icon, (mid, mid - 1))
    return scale(fl, s)


def label(d, xy, text, fill=(233, 217, 162, 255)):
    d.text(xy, text, fill=fill)


def make_preview(idle, move, emote, icon, small):
    BG = (28, 26, 32, 255)
    P = Image.new('RGBA', (1500, 1060), BG)
    d = ImageDraw.Draw(P)
    label(d, (16, 8), 'Boss Merchants  |  eggs: Ankhra the Clutch-Keeper (VoidMerchant rig)  |  idol: Brood Egg Idol (Ra-Akar)')
    # row 1: idle frames at 6x next to vanilla merchants, bottom aligned
    S = 6
    base_y = 20 + 40 * S
    x = 16
    vanilla = [('Caveling', ref_frame('CavelingMerchant', 'Merchant', 'Merchant_0')),
               ('Slime', ref_frame('SlimeMerchant', 'Slime_merchant', 'Slime_merchant_0')),
               ('Fishing', ref_frame('FishingMerchant', 'Fisher_merchant', 'Fisher_merchant_0')),
               ('Void', (load_ref(os.path.join(REF, 'reference', 'merchants', 'VoidMerchant',
                                                'voidMerchant_idle.png')) or Image.new('RGBA', (16, 35))).crop((0, 0, 16, 35)))]
    for name, fr in vanilla + [('Ankhra (new)', idle[0])]:
        if fr is None:
            continue
        bb = fr.getbbox() or (0, 0, fr.width, fr.height)
        fr = fr.crop((0, 0, fr.width, bb[3]))
        im = scale(fr, S)
        P.alpha_composite(im, (x, base_y - im.height))
        label(d, (x, base_y + 4), name)
        x += im.width + 24
    # same merchants at 1x, bottom aligned
    label(d, (x, base_y - 60), '1x')
    for name, fr in vanilla + [('Ankhra (new)', idle[0])]:
        if fr is None:
            continue
        bb = fr.getbbox() or (0, 0, fr.width, fr.height)
        fr = fr.crop((0, 0, fr.width, bb[3]))
        P.alpha_composite(fr, (x, base_y - fr.height))
        x += fr.width + 4
    # row 2: animation strips at 4x
    y = base_y + 30
    S2 = 4
    label(d, (16, y), 'idle (8 @10fps)'); y += 14
    P.alpha_composite(scale(strip(idle, 16, 35), S2), (16, y))
    label(d, (16 + 128 * S2 + 30, y - 14), 'move (6 @11fps)')
    P.alpha_composite(scale(strip(move, 16, 34), S2), (16 + 128 * S2 + 30, y))
    y += 35 * S2 + 10
    label(d, (16, y), 'emote (27 @14fps): hold out the egg, it glows, cracks, a hatchling peeks, back in the claws')
    y += 14
    em = strip(emote, 23, 37)
    P.alpha_composite(scale(em.crop((0, 0, 23 * 14, 37)), 4), (16, y))
    y += 37 * 4 + 6
    P.alpha_composite(scale(em.crop((23 * 14, 0, 621, 37)), 4), (16, y))
    y += 37 * 4 + 16
    # row 3: idols 1x and 8x beside vanilla
    label(d, (16, y), 'PERSPECTIVE SELF-CHECK  idols at 1x and 8x (vanilla: Mysterious Idol, Slime Oil, Pile of Chum, Void Merchant Idol; ours last, with smallIcon)')
    y += 16
    idref = os.path.join(REF, 'idol_reference')
    ids = [load_ref(os.path.join(idref, f)) for f in ('3930_MysteriousIdol_icon.png', '3931_SlimeOil_icon.png',
                                                     '3932_PileOfChum_icon.png', '3937_VoidMerchantIdol_icon.png')]
    ids = [i for i in ids if i is not None] + [icon, small]
    x = 16
    for im in ids:
        P.alpha_composite(im, (x, y))
        x += 22
    x += 10
    for im in ids:
        P.alpha_composite(scale(im, 8), (x, y))
        x += 16 * 8 + 8
    # row 4: placed on a floor mock
    fm = floor_mock(icon)
    P.alpha_composite(fm, (P.width - fm.width - 16, 760))
    label(d, (P.width - fm.width - 16, 760 + fm.height + 4), 'placed on a stone floor (mock)')
    return P


def main():
    idle, move, emote = make_idle(), make_move(), make_emote()
    sheets = {'idle': strip(idle, 16, 35), 'move': strip(move, 16, 34), 'emote': strip(emote, 23, 37)}
    icon, small, placed = make_idols()
    idols = {'icon': icon, 'smallIcon': small, 'placed': placed}
    errs = validate(sheets, idols)
    if errs:
        print('VALIDATION FAILED'); [print(' -', e) for e in errs]
        sys.exit(1)
    os.makedirs(OUT_M, exist_ok=True); os.makedirs(OUT_I, exist_ok=True); os.makedirs(OUT_P, exist_ok=True)
    textures = []
    for key, img in sheets.items():
        tex, size, cw, n, guid, pivot, fps, loop = RIG[key]
        fn = f'eggs__{tex}.png'
        img.save(os.path.join(OUT_M, fn))
        textures.append({'output': f'eggs/{fn}', 'replacesTexture': tex, 'size': list(size),
                         'spriteAsset': 'voidMerchant', 'spriteAssetAddress': SPRITE_ASSET_ADDRESS,
                         'animationGuid': guid, 'slot': 'm_animations[].m_spriteData.texture',
                         'pivot': pivot, 'srcFrameCount': n, 'fps': fps, 'loop': loop})
    json.dump({'key': 'eggs', 'rig': 'VoidMerchantEntity', 'kind': 'spriteAsset', 'textures': textures},
              open(os.path.join(OUT_M, 'sheets.json'), 'w'), indent=1)
    for name, img in idols.items():
        img.save(os.path.join(OUT_I, f'eggs_{name}.png'))
    make_preview(idle, move, emote, icon, small).save(os.path.join(OUT_P, 'eggs.png'))
    print('ok: wrote', OUT_M, OUT_I, os.path.join(OUT_P, 'eggs.png'))


if __name__ == '__main__':
    main()
