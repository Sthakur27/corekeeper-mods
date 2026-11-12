"""Boss Merchants, key `decor`: Ottoline Tidewick, a deep-sea decorator, and her Omoroth Shell Lamp idol.

    python -I BossMerchants/Unity/make_decor_art.py

Everything is drawn from code (part masks + a top-left-lit ramp shader + coloured outlines); no vanilla pixels
are copied. Outputs:
  Art/merchants/decor/decor__Fisher_merchant.png   260x120 sheet on the FishingMerchant rig (26x40 cells)
  Art/merchants/decor/sheets.json                  rig entry (same schema as Art/merchants/skins.json)
  Art/idols/decor_icon.png, decor_smallIcon.png, decor_placed.png
  Art/previews/decor.png                           review sheet (vanilla references read from build/, preview only)

Rig: FishingMerchantEntity. Clips (from the game's AnimationClips): IdleR = frames 0-15 (8 fps),
MoveR = 16-23 (10 fps), WaveR = 24-29 (10 fps). The rig's glow (Fisher_merchant_light, shared atlas, not
replaceable) sits on the vanilla lantern, so Ottoline holds her own lantern up in a tentacle at exactly the
vanilla lantern position of every frame (LANTERN table below, measured once from the vanilla sheet).
Exit code 1 if validation fails.
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent  # CoreKeeperMods
ART = HERE / 'Art'
OUT_M = ART / 'merchants' / 'decor'
OUT_I = ART / 'idols'
OUT_P = ART / 'previews'
REF = ROOT / 'build' / 'bossmerchants'

CW, CH, COLS, ROWS = 26, 40, 10, 3
SHEET = (CW * COLS, CH * ROWS)
GROUND = 37  # lowest opaque row in a cell (vanilla Fisher_merchant bbox bottom)

# vanilla lantern top-left per frame minus (14, 7): (dx, dy). Body bob uses the same dy.
LANTERN = [(0, 0), (0, 0), (0, 0), (1, 0), (1, 1), (1, 1), (1, 1), (0, 0), (-1, 0), (0, 0),
           (0, 0), (0, 0), (0, 0), (-1, 0), (-1, 0), (0, 0),
           (0, 0), (0, 0), (0, 1), (0, 0), (0, 0), (1, 0), (1, 1), (1, 0),
           (0, 0), (1, 0), (1, 0), (0, 0), (0, 0), (0, 0)]


def hexc(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def ramp(*hs):
    return [hexc(h) for h in hs]


# ramps: [outline, shadow, mid, light, highlight]. Every value is taken from the game's shared sprite palette
# (sampled from Octopus_boss_body, omorothTentacle_idle, Fisher_merchant, voidMerchant): shadows lean to
# indigo/navy, highlights lean warm, as in the vanilla sprites.
BODY = ramp('#142b5c', '#4e3c6d', '#723e8a', '#a555a4', '#de8e9a')
SUCKER = ramp('#4e3c6d', '#723e8a', '#c76369', '#de8e9a', '#e9d9a2')
GOLD = ramp('#39341f', '#614822', '#915b26', '#e2a12c', '#f2df3a')
SHADE = ramp('#0e2522', '#2a4e4a', '#47677e', '#599dbb', '#a5cada')
RUG = ramp('#23281e', '#5e2a28', '#91210b', '#b43221', '#c16436')
EYE = ramp('#142b5c', '#47677e', '#677fae', '#a5cada', '#ffffff')
GLOW = ramp('#614822', '#c97f26', '#e2a12c', '#f2df3a', '#ffffff')
MOSS = ramp('#0e2522', '#073c31', '#0f5630', '#0a6d27', '#239029')
SLATE = ramp('#0e2522', '#2a4e4a', '#47677e', '#677fae', '#a5cada')
TEAL = ramp('#142b5c', '#2a4e4a', '#47677e', '#599dbb', '#a5cada')
WHITE = hexc('#ffffff')
CREAM = hexc('#e9d9a2')
LIGHT = (-0.3, -0.8, 0.5)  # from above (top-left): top planes brightest, undersides dark


def _norm(v):
    m = sum(t * t for t in v) ** 0.5 or 1.0
    return tuple(t / m for t in v)


class Canvas:
    """Parts are painted back to front. Each part is shaded from a normal estimated from its own distance field
    (rim light top-left, core shadow, bounce light on the lower-right edge), throws contact shadow onto the
    parts beneath it, and its outline is tinted and lightened where the light hits."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.col = {}
        self.part = {}
        self.parts = []

    def add(self, mask, rmp, sep=True, flat=None, bias=0, occlude=True, sphere=None, **_):
        pid = len(self.parts)
        self.parts.append(rmp)
        mask = {(x, y) for x, y in mask if 0 <= x < self.w and 0 <= y < self.h}
        if not mask:
            return mask
        # distance to the outside (4-neighbour BFS)
        dist = {}
        frontier = [p for p in mask if any(n not in mask for n in
                                         ((p[0] + 1, p[1]), (p[0] - 1, p[1]), (p[0], p[1] + 1), (p[0], p[1] - 1)))]
        for p in frontier:
            dist[p] = 1
        k = 1
        while frontier:
            nxt = []
            for x, y in frontier:
                for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if n in mask and n not in dist:
                        dist[n] = k + 1
                        nxt.append(n)
            frontier, k = nxt, k + 1
        dmax = max(dist.values())
        L = _norm(LIGHT)
        for (x, y) in mask:
            if flat is not None:
                kk = flat
            elif sphere is not None:  # analytic ellipsoid normal for the big round forms
                ex, ey, erx, ery = sphere
                nx, ny = (x + 0.5 - ex) / erx, (y + 0.5 - ey) / ery
                rr = min(1.0, nx * nx + ny * ny)
                n = _norm((nx, ny, (1 - rr) ** 0.5 + 0.05))
                lam = n[0] * L[0] + n[1] * L[1] + n[2] * L[2]
                kk = 4 if lam > 0.86 else 3 if lam > 0.5 else 2 if lam > 0.08 else 1
                if dist[(x, y)] == 1 and kk == 1 and nx > 0 and ny > 0.2:
                    kk = 2  # bounce light on the lower-right rim
                kk = max(1, min(4, kk + bias))
            else:
                d = dist[(x, y)]
                gx = dist.get((x + 1, y), 0) - dist.get((x - 1, y), 0)
                gy = dist.get((x, y + 1), 0) - dist.get((x, y - 1), 0)
                nz = 0.35 + 1.4 * (d - 1) / max(1, dmax - 1) if dmax > 1 else 0.6
                n = _norm((-gx, -gy, nz))
                lam = n[0] * L[0] + n[1] * L[1] + n[2] * L[2]
                kk = 4 if lam > 0.88 else 3 if lam > 0.55 else 2 if lam > 0.12 else 1
                if d == 1 and kk == 1 and dmax >= 3 and (gx < 0 or gy < 0):
                    kk = 2  # bounce light on the lower-right rim of big forms
                if kk == 4 and dmax >= 3 and not (d <= 2):
                    kk = 3  # keep the brightest step for the rim/specular area only
                kk = max(1, min(4, kk + bias))
            self.col[(x, y)] = rmp[kk]
            self.part[(x, y)] = pid
        if sep and occlude:  # contact shadow on the parts underneath (darker directly below this part)
            for x, y in mask:
                for n, below in (((x, y + 1), True), ((x + 1, y), False), ((x - 1, y), False), ((x, y - 1), False)):
                    if n not in mask and n in self.part and self.part[n] < pid:
                        under = self.parts[self.part[n]]
                        self.col[n] = under[1]
        return mask

    def put(self, x, y, c, pid=None):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.col[(x, y)] = c
            if pid is not None or (x, y) not in self.part:
                self.part[(x, y)] = len(self.parts) - 1 if pid is None else pid

    def outline(self):
        add = {}
        for (x, y), pid in self.part.items():
            for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if n not in self.part and 0 <= n[0] < self.w and 0 <= n[1] < self.h:
                    if n not in add or add[n] < pid:
                        add[n] = pid
        for (x, y), pid in add.items():
            r = self.parts[pid]
            # clean segments: edges facing up catch the light (one step lighter), every other edge is dark
            top_edge = self.part.get((x, y + 1)) == pid
            self.col[(x, y)] = r[1] if top_edge else r[0]
            self.part[(x, y)] = pid

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        px = im.load()
        for (x, y), c in self.col.items():
            px[x, y] = c
        return im


def shape(w, h, fn):
    m = Image.new('L', (w, h), 0)
    fn(ImageDraw.Draw(m))
    px = m.load()
    return {(x, y) for x in range(w) for y in range(h) if px[x, y]}


def ellipse(w, h, box):
    return shape(w, h, lambda d: d.ellipse(box, fill=255))


def poly(w, h, pts):
    return shape(w, h, lambda d: d.polygon(pts, fill=255))


def line(w, h, pts, width):
    return shape(w, h, lambda d: d.line(pts, fill=255, width=width))


def bez(p0, p1, p2, n=12):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in [i / n for i in range(n + 1)]]


def tentacle(w, h, p0, p1, p2, w0=2, w1=1):
    pts = bez(p0, p1, p2)
    half = len(pts) // 2 + 1
    return line(w, h, pts[:half], w0) | line(w, h, pts[half - 1:], w1)


# ---------------------------------------------------------------------------------------------- merchant
def frame(i):
    lx, dy = LANTERN[i]
    ly = dy
    anim = 'idle' if i < 16 else 'move' if i < 24 else 'wave'
    c = Canvas(CW, CH)
    cx, cy = 10, 20 + dy  # head centre

    # Seen from above at the game's 3/4 angle: the hat's top opening and brim are ovals, the face sits low on
    # the head, the tentacles lie on the floor around her and show their lit tops.

    # lantern arm (behind the head): out to the right, up, hooks over the lantern
    hx, hy = 17 + lx, 6 + ly  # handle pixel above the lantern centre
    arm = (tentacle(CW, CH, (15, 24 + dy), (22, 25 + dy), (22, 15), 3, 2) |
           tentacle(CW, CH, (22, 15), (22, 3), (hx + 1, hy - 2), 2, 1))
    c.add(arm, BODY)
    for k, (x, y) in enumerate(bez((15, 25 + dy), (22, 26 + dy), (22, 15), 12)):  # suckers on the inner side
        if k in (6, 9):
            c.put(round(x) - 1, round(y), SUCKER[3])
            c.put(round(x) - 1, round(y) + 1, SUCKER[1])

    # tentacles lying on the floor in an oval footprint around her (tops lit, undersides dark)
    if anim == 'idle':
        ph = [0, 0, 1, 1, 0, 0, -1, -1][(i // 2) % 8]
    else:
        ph = 0
    legs = [  # base, control, tip, curl direction
        ((6, 27), (2, 29), (3, 33), 1),
        ((8, 28), (6, 33), (5, 35), -1),
        ((11, 28), (11, 33), (12, 35), 1),
        ((14, 28), (16, 33), (18, 34), 1),
        ((16, 26), (21, 28), (22, 32), -1),
    ]
    feet = ellipse(CW, CH, (5, 24 + dy, 17, 30))
    tips = []
    for n, (b, ctl, t, d) in enumerate(legs):
        if anim == 'move':
            sway = [0, 1, 1, 0, 0, -1, -1, 0][(i - 16 + n * 3) % 8]
            lift = 1 if (i - 16 + n) % 4 == 0 else 0
        else:
            sway = ph if n % 2 == 0 else -ph
            lift = 0
        sway = max(0, sway) if n == 0 else min(0, sway) if n == 4 else sway
        tx, ty = t[0] + sway, t[1] - lift
        b2 = (b[0], b[1] + dy)
        feet |= tentacle(CW, CH, b2, (ctl[0] + sway, ctl[1]), (tx, ty), 3, 2)
        tips.append((tx, ty, d))
    c.add(feet, BODY)
    for tx, ty, d in tips:  # curled tip: a small lit loop with a sucker under it
        c.put(tx + d, ty - 1, BODY[3])
        c.put(tx, ty - 1, BODY[3])
        c.put(tx, ty, SUCKER[2])
    for x, y in ((8, 31), (14, 31), (4, 31), (19, 31)):  # creases where tentacles fold over each other
        if (x, y) in c.part and (x, y + 1) in c.part:
            c.put(x, y, BODY[1])

    # head: a big ellipsoid lit from above; the hat hides its crown
    head = ellipse(CW, CH, (cx - 7, cy - 8, cx + 7, cy + 8))
    c.add(head, BODY, sphere=(cx + 0.5, cy + 0.5, 7.5, 8.5))
    for sx, sy in ((15, 0), (16, 0), (16, 1)):  # one darker mottle cluster on the shaded side
        if (sx, cy + sy) in head:
            c.put(sx, cy + sy, BODY[1])
    c.put(5, cy + 3, MOSS[3])  # a small moss patch
    c.put(5, cy + 4, MOSS[2])
    c.put(6, cy + 4, MOSS[2])

    # face, low on the head (we look down on her): eyes 3x2, gold monocle on the right one
    blink = anim == 'idle' and i in (11, 12)
    ey = cy + 3
    for ex in (9, 14):
        for xx in range(ex - 1, ex + 2):
            c.put(xx, ey - 1, BODY[3])  # lit brow ridge (a top plane)
        if blink:
            for xx in range(ex - 1, ex + 2):
                c.put(xx, ey, BODY[0])
            continue
        for xx in range(ex - 1, ex + 2):
            c.put(xx, ey, EYE[2])
            c.put(xx, ey + 1, EYE[3])
            c.put(xx, ey + 2, EYE[1])
        c.put(ex - 1, ey, WHITE)
        c.put(ex, ey, EYE[3])
        c.put(ex + 1, ey + 1, EYE[0])
        c.put(ex + 1, ey + 2, EYE[0])
        c.put(ex, ey + 3, BODY[1])  # cheek shadow under the eye
    mx, my = 14, ey + 1
    ring = ((mx - 2, my - 1), (mx - 1, my - 2), (mx, my - 2), (mx + 1, my - 2), (mx + 2, my - 1), (mx + 2, my),
            (mx + 2, my + 1), (mx + 1, my + 2), (mx, my + 2), (mx - 1, my + 2), (mx - 2, my + 1), (mx - 2, my))
    for x, y in ring:
        c.put(x, y, GOLD[4] if y < my else GOLD[3] if x < mx else GOLD[1])
    c.put(mx - 1, my - 2, WHITE)
    c.put(mx + 2, my + 3, GOLD[3])  # chain
    c.put(mx + 1, my + 4, GOLD[2])

    # rug, rolled, leaning on her left side: the top end is an oval with its spiral facing up
    bounce = 0 if anim != 'move' else (i % 2)
    ry0 = 18 + dy + bounce
    rug = poly(CW, CH, [(2, ry0 + 1), (5, ry0), (8, 34), (5, 35)]) | ellipse(CW, CH, (4, 33 - bounce, 8, 36 - bounce))
    c.add(rug, RUG)
    for k in range(3, 18):  # gold bands with a teal diamond between
        y = ry0 + k
        xs = sorted(x for (x, yy) in rug if yy == y)
        if len(xs) < 2:
            continue
        if k in (5, 14):
            for j, x in enumerate(xs):
                c.put(x, y, GOLD[3] if j < 2 else GOLD[2] if j < len(xs) - 1 else GOLD[1])
        elif k in (9, 11):
            c.put(xs[len(xs) // 2], y, TEAL[3])
        elif k == 10:
            for x in xs[1:-1]:
                c.put(x, y, TEAL[2] if x != xs[len(xs) // 2] else CREAM)
    cap = ellipse(CW, CH, (2, ry0 - 2, 6, ry0 + 1))  # rolled end seen from above
    c.add(cap, RUG, flat=3)
    c.put(3, ry0 - 1, CREAM)
    c.put(4, ry0 - 1, RUG[4])
    c.put(4, ry0, RUG[1])  # spiral centre
    c.put(5, ry0, RUG[2])
    for x in (5, 7):
        c.put(x, 36 - bounce, CREAM)  # fringe on the floor
    wrap = tentacle(CW, CH, (9, 27 + dy), (3, 30 + dy), (6, 27 + dy), 2, 1)
    c.add(wrap, BODY)
    c.put(4, 30 + dy, SUCKER[3])

    # lampshade hat: wide oval brim, cone of slate-teal cloth, oval top opening with a gold rim
    lift = [1, 2, 3, 3, 2, 1][i - 24] if anim == 'wave' else 0
    hb = cy - 5 - lift  # brim centre line
    brim = ellipse(CW, CH, (2, hb - 2, 16, hb + 3))
    c.add(brim, SHADE, flat=3)
    for (x, y) in brim:  # brim top plane is the lightest; its front lip turns down into gold trim
        c.put(x, y, SHADE[4] if x < 9 else SHADE[3])
    front = {}
    for (x, y) in brim:
        front[x] = max(front.get(x, -1), y)
    for x, y in front.items():
        c.put(x, y, GOLD[3] if x < 7 else GOLD[2] if x < 13 else GOLD[1])
    sw = (i // 3) % 2 if anim != 'move' else i % 2
    for x, y in sorted(front.items()):  # tassels hanging from the lip
        if (x + sw) % 2 == 0 and 3 < x < 15:
            c.put(x, y + 1, GOLD[2] if x < 9 else GOLD[1], pid=len(c.parts) - 1)
    ht = hb - 5  # top opening centre line
    cone = poly(CW, CH, [(6, ht), (11, ht), (14, hb - 1), (4, hb - 1)])
    c.add(cone, SHADE)
    for f in (0.2, 0.45, 0.7):  # pleats converge toward the opening
        for y in range(ht + 1, hb - 1):
            t = (y - ht) / (hb - 1 - ht)
            x = round((6 + f * 5) + t * ((4 + f * 10) - (6 + f * 5)))
            if (x, y) in cone:
                c.put(x, y, SHADE[1])
                if (x - 1, y) in cone:
                    c.put(x - 1, y, SHADE[3] if t < 0.5 else SHADE[2])
    opening = ellipse(CW, CH, (6, ht - 1, 11, ht + 1))
    c.add(opening, GOLD, flat=3)
    for (x, y) in opening:
        if y == ht and 7 <= x <= 10:
            c.put(x, y, SHADE[0] if x > 7 else SHADE[1])  # inside of the shade, in shadow
    c.put(7, ht - 1, GOLD[4])
    c.put(8, ht - 1, WHITE)
    if anim == 'wave':  # a tentacle lifts the brim
        c.add(tentacle(CW, CH, (7, cy + 6), (2, cy + 3), (3, hb + 2), 2, 1), BODY)

    # lantern (on the vanilla glow position): lit oval-ish cap, glass panes, darker base
    x0, y0 = 14 + lx, 7 + ly
    lant = {(x, y) for x in range(x0, x0 + 7) for y in range(y0 + 2, y0 + 5)}
    lant |= {(x, y0 + 1) for x in range(x0 + 1, x0 + 6)} | {(x, y0) for x in range(x0 + 2, x0 + 5)}
    lant |= {(x, y0 + 5) for x in range(x0 + 1, x0 + 6)}
    c.add(lant, GOLD)
    for y in range(y0 + 2, y0 + 5):
        for x in (x0 + 1, x0 + 2, x0 + 4, x0 + 5):
            c.put(x, y, GLOW[4] if (y == y0 + 3 and x in (x0 + 2, x0 + 4)) else GLOW[3] if y < y0 + 4 else GLOW[2])
        c.put(x0 + 3, y, GOLD[1])
        c.put(x0, y, GOLD[3] if y < y0 + 4 else GOLD[2])
        c.put(x0 + 6, y, GOLD[1])
    c.put(x0 + 3, y0 + 3, GLOW[1])
    for x in range(x0 + 1, x0 + 6):
        c.put(x, y0 + 1, GOLD[4] if x < x0 + 4 else GOLD[3])  # top plane of the cap
        c.put(x, y0 + 5, GOLD[1])
    c.put(x0 + 2, y0, GOLD[4])
    c.put(x0 + 3, y0, WHITE)
    c.put(x0 + 4, y0, GOLD[3])
    c.put(x0 + 3, y0 - 1, GOLD[3])  # handle ring under the tentacle tip

    c.outline()
    return c.image()


def build_sheet():
    sheet = Image.new('RGBA', SHEET, (0, 0, 0, 0))
    for i in range(30):
        r, col = divmod(i, COLS)
        sheet.alpha_composite(frame(i), (col * CW, r * CH))
    return sheet


# ---------------------------------------------------------------------------------------------- idol
def idol(small=False):
    """Omoroth Shell Lamp, seen from above at the game's 3/4 angle: a round temple plinth (bright oval top,
    darker front band), purple tentacle feet lying on it, a mossy shell dome lit from above, slate spikes
    pointing up, and Omoroth's pale eye low on the dome's front as the lamp."""
    S = 16
    c = Canvas(S, S)
    moss = MOSS
    if not small:
        drum = ellipse(S, S, (1, 9, 14, 14)) | {(x, y) for x in range(1, 15) for y in range(12, 14)} | \
            ellipse(S, S, (1, 11, 14, 15))
        c.add(drum, TEAL, flat=2)
        for (x, y) in drum:  # front band darkens toward the floor
            if y >= 13:
                c.put(x, y, TEAL[1])
        top = ellipse(S, S, (2, 9, 13, 13))
        c.add(top, TEAL, flat=3)
        for (x, y) in top:
            if y <= 10 and x < 8:
                c.put(x, y, TEAL[4])
        rim = {}
        for (x, y) in top:
            rim[x] = max(rim.get(x, -1), y)
        for x, y in rim.items():  # gold lip where the top plane meets the front band
            c.put(x, y + 1, GOLD[3] if x < 6 else GOLD[2] if x < 11 else GOLD[1])
        feet = set()
        for p0, p1, p2 in (((5, 10), (2, 10), (3, 12)), ((10, 10), (13, 10), (12, 12))):
            feet |= tentacle(S, S, p0, p1, p2, 2, 1)
        c.add(feet, BODY)
        c.put(3, 11, BODY[4])
        c.put(12, 11, BODY[3])
        dome = ellipse(S, S, (4, 2, 11, 10))
        c.add(dome, moss, sphere=(7.5, 5.5, 4, 4.5))
        c.put(6, 3, moss[4])  # highlight cluster on the top plane
        c.put(7, 3, moss[4])
        c.put(6, 4, moss[4])
        c.put(10, 5, moss[1])  # shell plate seam
        c.put(10, 6, moss[1])
        spikes = {(5, 2), (5, 1), (8, 1), (8, 0), (10, 2), (11, 1)}
        c.add(spikes, SLATE, sep=False, flat=2)
        c.put(5, 1, SLATE[4])
        c.put(8, 0, SLATE[4])
        c.put(11, 1, SLATE[3])
        face = ellipse(S, S, (5, 7, 10, 10))
        c.add(face, BODY, sphere=(7.5, 7.5, 3, 2))
        lens = {(6, 8), (7, 8), (8, 8), (9, 8), (7, 9), (8, 9), (6, 9)}
        c.add(lens, EYE, sep=False, flat=2)
        c.put(6, 8, EYE[4])
        c.put(7, 8, WHITE)
        c.put(9, 8, EYE[1])
        c.put(8, 9, EYE[0])
    else:
        drum = ellipse(S, S, (4, 10, 11, 13))
        c.add(drum, TEAL, flat=1)
        top = ellipse(S, S, (4, 10, 11, 12))
        c.add(top, TEAL, flat=3)
        for x in range(5, 11):
            c.put(x, 12, GOLD[3] if x < 8 else GOLD[2])
        dome = ellipse(S, S, (5, 5, 10, 11))
        c.add(dome, moss, sphere=(7.5, 7.5, 3, 3.5))
        c.put(6, 6, moss[4])
        c.add({(7, 4), (8, 4)}, SLATE, sep=False, flat=3)
        c.put(7, 4, SLATE[4])
        lens = {(6, 9), (7, 9), (8, 9)}
        c.add(lens, EYE, flat=2)
        c.put(6, 9, WHITE)
        c.put(8, 9, EYE[1])
    c.outline()
    return c.image()


# ---------------------------------------------------------------------------------------------- validation
def validate(sheet, icon, small, placed):
    errs = []
    if sheet.size != SHEET:
        errs.append(f'sheet size {sheet.size} != {SHEET}')
    a = sheet.getchannel('A')
    if any(a.histogram()[1:255]):
        errs.append('sheet has soft alpha')
    for i in range(30):
        r, col = divmod(i, COLS)
        cell = sheet.crop((col * CW, r * CH, col * CW + CW, r * CH + CH))
        bb = cell.getbbox()
        if not bb:
            errs.append(f'frame {i} empty')
            continue
        if bb[0] < 1 or bb[1] < 1 or bb[2] > CW - 1 or bb[3] > GROUND + 1:
            errs.append(f'frame {i} touches the cell edge / below ground: {bb}')
        if bb[3] != GROUND + 1:
            errs.append(f'frame {i} does not stand on the ground line: {bb}')
    for name, im in (('icon', icon), ('smallIcon', small), ('placed', placed)):
        if im.size != (16, 16):
            errs.append(f'{name} size {im.size}')
        if any(im.getchannel('A').histogram()[1:255]):
            errs.append(f'{name} soft alpha')
    if icon.getbbox()[3] != 16:
        errs.append('icon does not sit on the bottom edge')
    bb = small.getbbox()
    if not (8 <= bb[2] - bb[0] <= 12 and 8 <= bb[3] - bb[1] <= 12):
        errs.append(f'smallIcon not ~10x11: {bb}')
    return errs


# ---------------------------------------------------------------------------------------------- preview
def preview(sheet, icon, small, path):
    S = 6
    font = ImageFont.truetype('C:/Windows/Fonts/consolab.ttf', 16)
    bgc = (34, 32, 44, 255)
    W, H = 1500, 1340
    out = Image.new('RGBA', (W, H), bgc)
    d = ImageDraw.Draw(out)

    def paste(im, x, y, s):
        out.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, y))

    d.text((16, 10), 'Ottoline Tidewick (decor) - FishingMerchant rig - 3/4 top-down view - idle frame 0 at 6x and 1x beside vanilla',
           fill=(240, 220, 160), font=font)
    x = 16
    base = 40 + 46 * S  # shared ground line for the merchant row
    own = sheet.crop((0, 0, CW, CH))
    own = own.crop(own.getbbox())
    paste(own, x, base - own.height * S, S)
    d.text((x, base + 4), 'Ottoline', fill=(255, 255, 255), font=font)
    x += own.width * S + 30
    refs = [('Caveling', 'merchants/CavelingMerchant/Merchant.png', (0, 0, 64, 64)),
            ('Slime', 'merchants/SlimeMerchant/Slime_merchant.png', (0, 0, 30, 30)),
            ('Fishing', 'merchants/FishingMerchant/Fisher_merchant.png', (0, 0, 26, 40)),
            ('Void', 'merchants/VoidMerchant/voidMerchant_idle.png', (0, 0, 16, 35))]
    for name, rel, box in refs:
        p = REF / 'reference' / rel
        if p.exists():
            im = Image.open(p).convert('RGBA').crop(box)
            bb = im.getbbox()
            im = im.crop(bb)
            paste(im, x, base - im.height * S, S)
            d.text((x, base + 4), name, fill=(200, 200, 200), font=font)
            x += im.width * S + 30
    # the same row at 1x (how it reads in game)
    x += 20
    d.text((x, base - 70), '1x', fill=(240, 220, 160), font=font)
    out.alpha_composite(own, (x, base - own.height))
    x += own.width + 8
    for name, rel, box in refs:
        p = REF / 'reference' / rel
        if p.exists():
            im = Image.open(p).convert('RGBA').crop(box)
            im = im.crop(im.getbbox())
            out.alpha_composite(im, (x, base - im.height))
            x += im.width + 8

    y = base + 34
    for label, rng in (('idle 0-15', range(0, 16)), ('move 16-23', range(16, 24)), ('wave 24-29 (tips hat)', range(24, 30))):
        d.text((16, y), label, fill=(240, 220, 160), font=font)
        y += 22
        for k, i in enumerate(rng):
            r, col = divmod(i, COLS)
            paste(sheet.crop((col * CW, r * CH, col * CW + CW, r * CH + CH)), 16 + k * (CW * 3 + 6), y, 3)
        y += CH * 3 + 10

    y += 6
    d.text((16, y), 'Omoroth Shell Lamp: icon 1x / 8x, smallIcon 8x, vanilla idols 1x / 8x', fill=(240, 220, 160), font=font)
    y += 24
    out.alpha_composite(icon, (16, y))
    out.alpha_composite(small, (36, y))
    paste(icon, 60, y, 8)
    paste(small, 200, y, 8)
    x = 350
    for f in ('3930_MysteriousIdol_icon', '3931_SlimeOil_icon', '3932_PileOfChum_icon', '3937_VoidMerchantIdol_icon',
              '5127_OmorothTrophy_icon', '5904_TempleOmorothUrn_icon'):
        p = REF / 'idol_reference' / (f + '.png')
        if p.exists():
            im = Image.open(p).convert('RGBA')
            out.alpha_composite(im, (x, y))
            paste(im, x + 20, y, 8)
            x += 150

    # placed on a dark stone floor mock (3x3 tiles), 6x
    fl = Image.new('RGBA', (48, 48), (0, 0, 0, 255))
    fp = fl.load()
    for yy in range(48):
        for xx in range(48):
            tx, ty = xx % 16, yy % 16
            v = 52 + ((xx * 7 + yy * 13) % 5) * 2
            if tx == 0 or ty == 0:
                v = 36
            elif tx == 1 or ty == 1:
                v = 64
            fp[xx, yy] = (v - 6, v - 8, v + 4, 255)
    fl.alpha_composite(icon, (16, 16 - 1))
    paste(fl, 16, y + 175, 6)
    d.text((16, y + 150), 'placed on stone floor (6x)', fill=(240, 220, 160), font=font)
    path.parent.mkdir(parents=True, exist_ok=True)
    out.convert('RGB').save(path)


def main():
    sheet = build_sheet()
    icon = idol()
    small = idol(small=True)
    placed = icon.copy()
    errs = validate(sheet, icon, small, placed)
    OUT_M.mkdir(parents=True, exist_ok=True)
    OUT_I.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT_M / 'decor__Fisher_merchant.png')
    icon.save(OUT_I / 'decor_icon.png')
    small.save(OUT_I / 'decor_smallIcon.png')
    placed.save(OUT_I / 'decor_placed.png')
    sprites = []
    for i in range(30):
        r, col = divmod(i, COLS)
        sprites.append({'name': f'Fisher_merchant_{i}', 'rect': [float(col * CW), float((ROWS - 1 - r) * CH), float(CW), float(CH)],
                        'pivot': [0.5, 0.5], 'pixelsPerUnit': 16.0})
    sheets = {'key': 'decor', 'rig': 'FishingMerchantEntity', 'kind': 'legacy',
              'textures': [{'output': 'decor/decor__Fisher_merchant.png', 'replacesTexture': 'Fisher_merchant',
                            'size': list(SHEET), 'legacySprites': sprites,
                            'renderer': '/FishingMerchant/XScaler/AnimPositionRotation/AnimScale/SRPivot/SR'}]}
    skins = ART / 'merchants' / 'skins.json'
    if skins.exists():  # cross-check our rects against the rig's recorded layout
        ref = json.load(open(skins))['merchants']['omoroth']['textures'][0]['legacySprites']
        if [(s['name'], s['rect'], s['pivot']) for s in ref] != [(s['name'], s['rect'], s['pivot']) for s in sprites]:
            errs.append('legacySprites differ from the FishingMerchant layout in skins.json')
    (OUT_M / 'sheets.json').write_text(json.dumps(sheets, indent=1))
    preview(sheet, icon, small, OUT_P / 'decor.png')
    if errs:
        print('VALIDATION FAILED:\n  ' + '\n  '.join(errs))
        sys.exit(1)
    print('ok: decor sheet, idols, sheets.json, preview written')


if __name__ == '__main__':
    main()
