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
# ovals, and the face sits low under a wide oval brim. Parts are built from ellipses / curved bands and shaded in
# clean bands (light from the top-left); no dithering.
CX = 30                      # body centre column
EYE_OFF = (3, 32)            # eye glint position relative to (CX, 0) before the bob

# ---------------------------------------------------------------- animation tables
IDLE_DY = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
IDLE_LANTERN = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0]
IDLE_FLUTTER = [0, 0, 0, 0, 1, 1, 1, 1, 0, 0]
WALK_DY = [0, -1, 0, -1, 0, -1, 0, -1]
WALK_LANTERN = [0, -1, -1, 0, 0, 1, 1, 0]
#            (near boot x offset, lift), (far boot x offset, lift), relative to CX
WALK_LEGS = [((4, 0), (-5, 0)), ((1, 0), (-1, 1)), ((-3, 0), (2, 0)), ((0, 1), (-3, 0)),
             ((4, 0), (-5, 0)), ((1, 0), (-1, 1)), ((-3, 0), (2, 0)), ((0, 1), (-3, 0))]
STAND_LEGS = ((2, 0), (-4, 0))
# hat tip: hand position relative to (CX, 0), hat lift
WAVE = [((-4, 38), 0), ((-6, 33), 0), ((-9, 27), 0), ((-9, 24), -3), ((-9, 24), -3), ((-9, 25), -2)]


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


def draw_boot(img, x0, lift, near):
    """Small boot seen from above: light top plane, leather front, dark sole."""
    y0 = GROUND - 3 - lift
    rows = (["lhhl", "LllL", "LlllL", "DLLLD"] if near else ["LllL", "LLlL", "LLLLL", "DDDDD"])
    grid(img, rows, x0, y0)


def draw_figure(dy=0, hat_dy=0, lantern_dx=0, flutter=0, legs=STAND_LEGS, hand=None):
    """Returns (base, light, eye_xy) for one 64x64 cell."""
    base = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
    light = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
    c = CX

    # ---- back gear: burlap feed sack (mouth seen from above), lasso coil (an oval ring), horseshoe charm
    sx0, sy0 = c - 9, 38 + dy
    sack = ell(sx0, sy0, 4.6, 6.8)
    paint(base, sack, lambda x, y: band(-((x + .5 - sx0) / 4.6) * .6 - ((y + .5 - sy0) / 6.8) * .8,
                                         (-0.5, 0.15, 0.65), '123i'), out_tl='2', out_br='1')
    mouth = ell(sx0, 32 + dy, 3.8, 1.7)
    paint(base, mouth, lambda x, y: '1', out_tl='i', out_br='3')
    for x in range(sx0 - 3, sx0 + 2):
        put(base, x, 32 + dy, 'D' if x < sx0 else '1')
    put(base, sx0 - 1, 33 + dy, 'O'); put(base, sx0, 33 + dy, '3'); put(base, sx0 + 1, 33 + dy, 'O')   # rope tie
    rcx, rcy = c - 12, 43 + dy
    ring = ell(rcx, rcy, 4.4, 2.8) - ell(rcx, rcy - 0.3, 2.4, 1.2)
    for (x, y) in ring:
        tw = ((x * 2 + y) // 2) % 3
        put(base, x, y, '5' if (y < rcy and tw == 0) else ('3' if tw != 2 else '2'))
    tl, br = edges(ring)
    for p in br:
        put(base, p[0], p[1], 'O')
    grid(base, ["V.v", "V.v", ".vT"], c - 15, 46 + dy)

    # ---- legs: two small boots
    (nx, nl), (fx, fl) = legs
    draw_boot(base, c + fx - 2, fl, False)
    draw_boot(base, c + nx - 2, nl, True)

    # ---- poncho: squat cone, oval shoulders on top, oval curved hem, curved stripes
    yt, hy, rxh, ryh, w0 = 33 + dy, 46 + dy, 11.5, 3.0, 7.0
    pon = set()
    for y in range(yt, int(hy + ryh) + 1):
        hw = w0 + (rxh - w0) * min(1.0, (y - yt) / (hy - yt))
        for x in range(c - 13, c + 14):
            dx = x + 0.5 - c
            if abs(dx) <= hw and y + 0.5 <= hy + ryh * max(0.0, 1 - (dx / rxh) ** 2) ** 0.5:
                pon.add((x, y))

    def hem_y(x, k):     # a curve parallel to the hem, k rows below the shoulders
        dx = (x + 0.5 - c) / rxh
        return round(yt + k + ryh * max(0.0, 1 - dx * dx) ** 0.5)

    def pon_key(x, y):
        u = (x + 0.5 - c) / rxh
        t = (y - yt) / (hy - yt)
        v = -0.75 * u - 0.55 * t
        if y == hem_y(x, 6) or y == hem_y(x, 7):           # brass zigzag band (low contrast)
            zig = ((x + (y - hem_y(x, 6)) * 2) // 2) % 2
            return band(v, (-0.35,), 'aA') if zig == 0 or y == hem_y(x, 6) else 'a'
        if y == hem_y(x, 10):                              # slate stripe
            return band(v, (-0.3,), 'vV')
        if y >= hem_y(x, 12):                              # darker hem underside
            return 'P'
        return band(v, (-0.45, 0.3), 'Ppq')
    paint(base, pon, pon_key, out_tl='P', out_br='D')
    for x in range(c - 11, c + 12, 2):                      # fringe under the hem
        y = hem_y(x, 13) + 1
        if (x, y - 1) in pon:
            put(base, x, y, 'P')
    put(base, c - 11, hem_y(c - 11, 12) + 1, 'A'); put(base, c + 11, hem_y(c + 11, 12) + 1, 'A')   # tassels
    # slate patch with a stitch on the lit side
    grid(base, ["VVv", "VQv", "vvv"], c - 6, yt + 8)
    # shoulders: top plane of the poncho, lightest
    sh = ell(c, yt + 1.2, w0 + 1.5, 2.8)
    paint(base, sh, lambda x, y: band(-((x + .5 - c) / (w0 + 1.5)) - ((y - yt) / 2.8) * .4, (-0.6, 0.15), 'pqQ'),
          out_tl='q', out_br='P')
    # sack strap across the chest with a brass buckle
    for (x, y) in line_px(c - 6, yt + 1, c + 7, yt + 10):
        put(base, x, y, 'l'); put(base, x, y + 1, 'L')
    grid(base, ["A4", "aA"], c - 1, yt + 4)

    # ---- head: blue feathered ball, mostly under the brim; face low, beak with a lit top
    hcx, hcy = c + 1, 32.0 + dy
    head = ell(hcx, hcy, 6.0, 4.8)
    paint(base, head, lambda x, y: band(-((x + .5 - hcx) / 6.2) * .55 - ((y + .5 - hcy) / 4.8) * .85,
                                         (-0.55, -0.05, 0.45), 'bBcC'), out_tl='b', out_br='n')
    _brim = ell(c - 0.5, 22.5 + dy + hat_dy, 14.0, 4.6)
    for (x, y) in head:                                               # occlusion under the brim
        if (x, y - 1) in _brim:
            put(base, x, y, 'n')
        elif (x, y - 2) in _brim and (x, y) not in edges(head)[1]:
            put(base, x, y, 'b')
    grid(base, ["kgy.", "kGgy", ".kGk"], c + 6, 32 + dy)             # beak: top plane catches light
    ex, ey = c + EYE_OFF[0], EYE_OFF[1] + dy
    grid(base, ["nn", "nn"], ex, ey)
    grid(base, ["...w", "..wC"], c + 2, 33 + dy)                       # cheek light
    # collar bell (brass, oval top)
    grid(base, [".A4.", "1aA1", ".11."], c + 3, yt + 4)

    # ---- hat: wide oval brim (top plane lit, front edge thickness), cylindrical crown with an oval top
    hdy = dy + hat_dy
    bcx, bcy, brx, bry = c - 0.5, 22.5 + hdy, 14.0, 4.6
    brim = ell(bcx, bcy, brx, bry)
    under = {(x, y + 1) for (x, y) in brim} | {(x, y + 2) for (x, y) in brim if y + 0.5 > bcy}
    rim = under - brim
    for (x, y) in rim:                                                # brim thickness / underside, front only
        if y > bcy:
            put(base, x, y, '1' if (x, y + 1) not in under else '2')
    paint(base, brim, lambda x, y: band(-((x + .5 - bcx) / brx) * .5 - ((y + .5 - bcy) / bry) * .75,
                                         (-0.55, -0.1, 0.4), '3456'), out_tl='4', out_br='2')
    ccx, crx, ctop, cbot = c - 1.5, 5.8, 15.0 + hdy, 22.0 + hdy
    crown = set()
    for y in range(int(ctop), int(cbot) + 2):
        for x in range(int(ccx - crx) - 1, int(ccx + crx) + 2):
            dx = (x + 0.5 - ccx) / crx
            if abs(dx) <= 1 and y + 0.5 <= cbot + 1.6 * max(0.0, 1 - dx * dx) ** 0.5 and y >= ctop:
                crown.add((x, y))
    paint(base, crown, lambda x, y: band(-((x + .5 - ccx) / crx) - ((y - ctop) / 8) * .3, (-0.4, 0.35), '234'),
          out_tl='3', out_br='1')
    for x in range(int(ccx - crx), int(ccx + crx) + 1):               # rust hat band following the crown curve
        dx = (x + 0.5 - ccx) / crx
        yb = round(cbot - 1 + 1.6 * max(0.0, 1 - dx * dx) ** 0.5)
        if (x, yb) in crown:
            put(base, x, yb, 'q' if dx < -0.3 else ('p' if dx < 0.5 else 'P'))
            put(base, x, yb - 1, 'p' if dx < 0.5 else 'P')
    ctopm = ell(ccx, ctop + 0.5, crx, 2.4)
    paint(base, ctopm, lambda x, y: band(-((x + .5 - ccx) / crx) - ((y + .5 - ctop) / 2.4) * .3, (-0.5, 0.2), '456'),
          out_tl='5', out_br='3')
    for x in range(int(ccx) - 2, int(ccx) + 3):                       # pinch crease on the crown top
        put(base, x, int(ctop) + 1, '4')
    # Azeos feather tucked in the band, pointing up and back
    fx0, fy0 = int(ccx - crx) + 1, int(cbot) - 2
    tip = (fx0 - 4 - (1 if flutter else 0), fy0 - 9)
    for i, (x, y) in enumerate(line_px(fx0, fy0, tip[0], tip[1])):
        put(base, x, y, 'n'); put(base, x + 1, y, 'B' if i > 2 else 'c'); put(base, x + 2, y, 'n' if i > 4 else '.')
    put(base, tip[0], tip[1], 'A'); put(base, tip[0] + 1, tip[1], 'A'); put(base, tip[0], tip[1] - 1, 'a')

    # ---- crook (leans slightly, so you see along it), hook and lantern with oval cap
    xb, xt, ytop = c + 13, c + 15, 9 + dy
    for y in range(ytop, GROUND + 1):
        x = round(xb + (xt - xb) * (GROUND - y) / (GROUND - ytop))
        put(base, x, y, '3'); put(base, x + 1, y, '1')
    put(base, xb, GROUND, 'O'); put(base, xb + 1, GROUND, 'O')
    hook = [(0, -1), (1, -2), (2, -3), (3, -3), (4, -2), (5, -1)]
    for i, (hx_, hy_) in enumerate(hook):
        put(base, xt + hx_, ytop + hy_, '4' if i in (1, 2) else '3')
        put(base, xt + hx_, ytop + hy_ - 1, 'O')
        put(base, xt + hx_, ytop + hy_ + 1, '1' if i else '3')
    put(base, xt + 5, ytop, 'T')
    lx, ly = xt + 3 + lantern_dx, ytop + 1
    grid(base, [".TUT.", "TuUuT", "TGyGT", "TyWyT", "TvvvT", ".TDT."], lx, ly)
    for (x, y), ch in {(1, 2): 'G', (2, 2): 'y', (3, 2): 'G', (1, 3): 'y', (2, 3): 'W', (3, 3): 'y'}.items():
        put(light, lx + x, ly + y, ch)
    # right glove on the staff
    gy0 = 38 + dy
    gxs = round(xb + (xt - xb) * (GROUND - gy0) / (GROUND - ytop)) - 1
    grid(base, ["lhh", "Lhl", "LLL"], gxs, gy0)

    # ---- free arm (hat tip): sleeve from the shoulder, glove in front of the brim
    if hand is not None:
        hxp, hyp = c + hand[0], hand[1] + dy
        for (x, y) in line_px(c - 3, yt + 3, hxp + 1, hyp + 2):
            put(base, x - 1, y, 'P'); put(base, x, y, 'q'); put(base, x + 1, y, 'p'); put(base, x + 2, y, 'D')
        grid(base, ["lhh", "Lhl", "LLL"], hxp - 1, hyp - 1)
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
