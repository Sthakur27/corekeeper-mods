"""Boss Merchants, key `decor`: Ottoline Tidewick, a deep-sea decorator, and her Omoroth Shell Lamp idol.

    python -I BossMerchants/Unity/make_decor_art.py

Everything is drawn from code (part masks + a top-left-lit cluster-grain ramp shader, own-ramp edges); no vanilla pixels
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
# Restyle (2026-11-12, after Old Hemlock's in-game restyle): the previous Ottoline read as a compact dark blob
# with a flat pale-cyan brim, near-black outlines all round and 4-step fills. This pass draws her the way the
# vanilla merchants are drawn (all four were measured; she is tuned toward the Fisher she shares a rig with):
#   - 6-8 close shades per material, quantised from a per-pixel light value with a ~2x2 cluster grain (never
#     single pixels), so every surface has form shading instead of one or two flat fills;
#   - no separate outline colour: each part's silhouette ring is a dark step of its own ramp (one step lighter
#     on the lit top/left side); finish_art.py darkens edges that bound solid mass a little further;
#   - the mass goes into thin props: six thin tentacles fanned out on the floor, the thin lantern arm, a slim
#     rolled rug, the lampshade's fringe; the mantle itself is small;
#   - the brim's top plane is a lit-left -> shaded-right gradient, not one flat light colour; the only bright
#     accents are the lantern panes (on the rig's built-in glow spot), the monocle glint and eye whites.
RP = {   # every value is a master-palette colour (vanilla-sampled), so the finisher's palette lock keeps them
    'body': ['1f102a', '2b213b', '4e3c6d', '723e8a', '954d94', 'a555a4', 'c069d8', 'd96fa5', 'df8eb8'],
    'hat': ['142b5c', '2a4e4a', '316471', '39728b', '3d7983', '4b708a', '599dbb', '6e9bb9', '62b9c5'],
    'gold': ['39341f', '614822', '915b26', 'b58047', 'c97f26', 'e2a12c', 'e7b645', 'f2df3a'],
    'rug': ['5e2a28', '822e13', '91210b', 'a24027', 'b43221', 'c16436', 'e88b69'],
    'sucker': ['723e8a', 'c76369', 'de8e9a', 'e5a69f'],
    'eye': ['2b215f', '677fae', 'a5cada', 'd7dcfb', 'ffffff'],
    'pane': ['e2a12c', 'f2df3a', 'fceb4f', 'fae6bc'],
    'moss': ['2b5432', '356a2c', '4c8d32'],
    'cream': ['d4b987', 'e9d9a2'],
}
RP = {k: [hexc(h) for h in v] for k, v in RP.items()}
LDIR = _norm((-0.45, -0.75, 0.6))   # light from above, top-left


def grain(x, y):
    """Deterministic cluster grain in {-1, 0, 1}: constant over irregular ~2x2 blocks (never single pixels)."""
    bx, by = (x + (y // 2) % 2) // 2, y // 2
    h = (bx * 73856093 ^ by * 19349663) & 0xffff
    return (h % 5) // 2 - 1 if h % 7 else 0


class Paint:
    """Back-to-front pixel canvas. part() fills a mask from a light value per pixel (quantised onto the
    ramp's lighter steps) and rings it with the ramp's own dark steps."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = {}

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[(x, y)] = c

    def part(self, mask, ramp, vfn, lo=2, hi=None, ring=True, ring_dark=1, ring_lit=2, g=0.13):
        r = RP[ramp]
        steps = r[lo:hi] if hi else r[lo:]
        if ring:
            ringpx = {}
            for (x, y) in mask:
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    p = (x + a, y + b)
                    if p in mask:
                        continue
                    if (a, b) in ((-1, 0), (0, -1)):
                        ringpx.setdefault(p, r[ring_lit])
                    else:
                        ringpx[p] = r[ring_dark]
            for (x, y), c in ringpx.items():
                self.put(x, y, c)
        for (x, y) in mask:
            v = vfn(x, y) + g * grain(x, y)
            self.put(x, y, steps[max(0, min(len(steps) - 1, int(v * len(steps))))])

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        p = im.load()
        for (x, y), c in self.px.items():
            p[x, y] = c
        return im


def ell(cx, cy, rx, ry):
    return {(x, y) for y in range(int(cy - ry) - 1, int(cy + ry) + 2) for x in range(int(cx - rx) - 1, int(cx + rx) + 2)
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0}


def sphere_v(cx, cy, rx, ry, bias=0.0):
    def v(x, y):
        nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        rr = min(1.0, nx * nx + ny * ny)
        n = _norm((nx, ny, (1 - rr) ** 0.5 + 0.1))
        return 0.15 + 0.85 * max(0.0, n[0] * LDIR[0] + n[1] * LDIR[1] + n[2] * LDIR[2]) + bias
    return v


def curve(p0, p1, p2, n=16):
    """Pixel path along a quadratic bezier: [(x, y, t)] without duplicates, base first."""
    out, seen = [], set()
    for k in range(n + 1):
        t = k / n
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        q = (int(round(x)), int(round(y)))
        if q not in seen:
            seen.add(q)
            out.append((q[0], q[1], t))
    return out


def draw_tentacle(cv, p0, p1, p2, thick=0.5, suckers=True):
    """A thin tentacle seen from above: a lit top line (light near the base, darker toward the tip), a shaded
    underside column toward the base, a dark ring only on the shadow side (bottom/right) so neighbouring
    tentacles stay separate lines, and a pale sucker pair under the tip."""
    path = curve(p0, p1, p2)
    flat = abs(p2[0] - p0[0]) >= abs(p2[1] - p0[1])
    top, under = {}, {}
    for x, y, t in path:
        top[(x, y)] = t
    for x, y, t in path:
        q = (x, y + 1) if flat else (x + 1, y)
        if t < thick and q not in top:
            under[q] = t
    r = RP['body']
    side = (0, 1) if flat else (1, 0)
    for (x, y), t in list(under.items()) + list(top.items()):   # shadow-side ring, darker toward the base
        q = (x + side[0], y + side[1])
        if q not in top and q not in under:
            cv.put(q[0], q[1], r[3] if t < 0.3 else r[4])
    for (x, y), t in under.items():
        cv.put(x, y, r[5] if (grain(x, y) + 1) * 0.15 + t < 0.45 else r[4])
    for (x, y), t in top.items():
        v = 1.05 - 0.5 * t + 0.12 * grain(x, y)
        cv.put(x, y, r[7] if v > 0.95 else r[6] if v > 0.7 else r[5] if v > 0.45 else r[4])
    if suckers and len(path) > 5:
        k = int(len(path) * 0.65)
        for j, (x, y, _) in enumerate(path[k:k + 2]):
            q = (x, y + 1) if flat else (x + 1, y)
            cv.put(q[0], q[1], RP['sucker'][2] if j == 0 else RP['sucker'][1])
    return path


def frame(i):
    """One 26x40 cell at vanilla Fisher scale, ground row 37. Lantern on the rig's glow spot (LANTERN)."""
    lx, dy = LANTERN[i]
    anim = 'idle' if i < 16 else 'move' if i < 24 else 'wave'
    cv = Paint(CW, CH)
    cx, cy = 12, 26 + dy          # mantle centre
    x0, y0 = 15 + lx, 7 + dy      # lantern top-left (vanilla glow spot)

    # rug, rolled, leaning on her left (behind the body): slim cylinder, lit left column, gold bands
    bounce = 0 if anim != 'move' else (i % 2)
    top_y = 22 + dy + bounce
    rug, rv = set(), {}
    for y in range(top_y, 37):
        t = (y - top_y) / (36 - top_y)
        xc = int(round(4 + 2 * t))
        for k, xx in enumerate((xc - 1, xc, xc + 1)):
            rug.add((xx, y))
            rv[(xx, y)] = (0.95, 0.65, 0.35)[k] - 0.2 * t
    cv.part(rug, 'rug', lambda x, y: rv[(x, y)], lo=3, ring_dark=0, ring_lit=1, g=0.1)
    for k, y in enumerate((top_y + 4, top_y + 10)):        # gold bands (lit left, shaded right)
        xs = sorted(x for (x, yy) in rug if yy == y)
        for j, x in enumerate(xs):
            cv.put(x, y, RP['gold'][(5, 4, 2)[j]])
    ys = top_y + 7
    xs = sorted(x for (x, yy) in rug if yy == ys)
    cv.put(xs[0], ys, RP['hat'][6]); cv.put(xs[1], ys, RP['hat'][4]); cv.put(xs[2], ys, RP['hat'][2])
    cap = ell(3.5, top_y - 0.5, 1.7, 1.2)                   # rolled end seen from above, spiral facing up
    cv.part(cap, 'rug', lambda x, y: 0.7 - 0.3 * (x - 2), lo=3, ring_dark=0, ring_lit=1, g=0)
    cv.put(3, top_y - 1, RP['cream'][1]); cv.put(2, top_y - 1, RP['cream'][0]); cv.put(3, top_y, RP['rug'][1])
    cv.put(7, 37, RP['cream'][0]); cv.put(8, 37, RP['cream'][1])   # fringe on the floor

    # tentacles fanned out on the floor (lit tops, pale suckers under the tips), a dark footprint under her
    ph = [0, 0, 1, 1, 0, 0, -1, -1][(i // 2) % 8] if anim == 'idle' else 0
    legs = [((9, 30), (6, 33), (8, 36), (9, 35)),
            ((12, 31), (11, 34), (12, 36), (13, 35)),
            ((14, 31), (16, 34), (17, 36), (18, 35)),
            ((16, 30), (20, 31), (22, 34), (22, 33)),
            ((8, 29), (5, 30), (5, 33), (6, 33))]
    foot = ell(cx + 0.5, 30.5 + dy, 4.5, 1.6)
    cv.part(foot, 'body', lambda x, y: 0.45 - 0.15 * (x - cx) / 4, lo=2, hi=5, ring=False)
    for n, (b, ctl, t, curl) in enumerate(legs):
        if anim == 'move':
            sway = [0, 1, 1, 0, 0, -1, -1, 0][(i - 16 + n * 3) % 8]
            lift = 1 if (i - 16 + n) % 4 == 0 else 0
        else:
            sway, lift = (ph if n % 2 == 0 else -ph), 0
        sway = min(0, sway) if n == 4 else max(0, sway) if n == 3 else sway
        tip = (t[0] + sway, t[1] - lift)
        draw_tentacle(cv, (b[0], b[1] + dy), (ctl[0] + sway, ctl[1] + dy * (n in (3, 4))), tip)
        cv.put(curl[0] + sway, curl[1] - lift, RP['body'][5])     # curled tip catches the light

    # lantern arm out of her right side, up to the rig's glow spot
    draw_tentacle(cv, (16, 25 + dy), (22, 19 + dy), (x0 + 2, y0 + 6), thick=0.45)

    # mantle: a small ellipsoid lit from above, the hat hides its crown
    mantle = ell(cx + 0.5, cy + 0.5, 4.6, 4.2)
    sv = sphere_v(cx + 0.5, cy + 0.5, 4.6, 4.2, bias=0.0)
    hb0 = cy - 5   # occlusion under the brim's overhang: the mantle's top rows sit in the hat's shadow
    cv.part(mantle, 'body', lambda x, y: sv(x, y) - (0.5 if y <= hb0 + 5 else 0.25 if y == hb0 + 6 else 0),
            lo=3, hi=8, ring_dark=2, ring_lit=3, g=0.12)
    cv.put(cx - 4, cy + 1, RP['body'][7]); cv.put(cx - 4, cy, RP['body'][6])   # lit flank
    cv.put(cx - 3, cy + 2, RP['moss'][2]); cv.put(cx - 3, cy + 3, RP['moss'][1]); cv.put(cx - 2, cy + 3, RP['moss'][0])
    cv.put(cx + 3, cy + 3, RP['body'][3]); cv.put(cx + 4, cy + 2, RP['body'][3])   # mottle on the shaded side

    # face, low on the mantle: 2x2 eyes under a lit brow, gold monocle on her right eye
    blink = anim == 'idle' and i in (11, 12)
    ey = cy + 1
    for ex in (cx - 2, cx + 2):
        cv.put(ex, ey - 1, RP['body'][6]); cv.put(ex + 1, ey - 1, RP['body'][5])
        if blink:
            cv.put(ex, ey, RP['body'][2]); cv.put(ex + 1, ey, RP['body'][2])
            continue
        cv.put(ex, ey, RP['eye'][3]); cv.put(ex + 1, ey, RP['eye'][2])
        cv.put(ex, ey + 1, RP['eye'][1]); cv.put(ex + 1, ey + 1, RP['eye'][0])
    mx = cx + 2
    for x, y in ((mx, ey - 1), (mx + 1, ey - 1), (mx + 2, ey), (mx + 2, ey + 1), (mx + 1, ey + 2), (mx, ey + 2),
                 (mx - 1, ey + 1), (mx - 1, ey)):
        cv.put(x, y, RP['gold'][6] if y < ey else RP['gold'][4] if x < mx else RP['gold'][2])
    cv.put(mx, ey - 1, RP['cream'][1])
    cv.put(mx + 3, ey + 2, RP['gold'][2]); cv.put(mx + 3, ey + 3, RP['gold'][1])   # chain

    # one tentacle hugs the rug
    draw_tentacle(cv, (cx - 3, cy + 1), (3, cy + 2), (5, cy - 1), thick=0.6, suckers=False)

    # lampshade hat: oval brim (top plane lit left -> shaded right), short pleated cone, gold rim + fringe
    lift = [1, 1, 2, 2, 1, 1][i - 24] if anim == 'wave' else 0
    hb = cy - 5 - lift
    brx, bry = 5.8, 1.8
    brim = ell(cx + 0.5, hb - 0.3, brx, bry)
    cv.part(brim, 'hat', lambda x, y: 0.8 - 0.45 * (x + 0.5 - cx) / brx - 0.1 * (y - hb), lo=3, hi=9,
            ring_dark=1, ring_lit=2, g=0.14)
    front = {}
    for (x, y) in brim:
        front[x] = max(front.get(x, -1), y)
    for x, y in front.items():   # front lip turns down into gold trim, fringe hanging off it
        t = (x - min(front)) / max(1, max(front) - min(front))
        cv.put(x, y + 1, RP['gold'][6 if t < 0.25 else 5 if t < 0.55 else 4 if t < 0.85 else 3])
        if min(front) < x < max(front):   # the band's lower, shaded half
            cv.put(x, y + 2, RP['gold'][4 if t < 0.35 else 3 if t < 0.7 else 2])
    sw = (i // 3) % 2 if anim != 'move' else i % 2
    for x, y in sorted(front.items()):
        if (x + sw) % 2 == 0 and min(front) + 1 < x < max(front) - 1:
            cv.put(x, y + 3, RP['gold'][3] if x < cx + 2 else RP['gold'][2])
    ht = hb - 5
    cone = set()
    for y in range(ht, hb - 1):
        t = (y - ht) / max(1, hb - 2 - ht)
        a, b = int(round(10 - 1.6 * t)), int(round(14 + 1.6 * t))
        for x in range(a, b + 1):
            cone.add((x, y))

    def cone_v(x, y):
        t = (y - ht) / max(1, hb - 2 - ht)
        a, b = 10 - 1.6 * t, 14 + 1.6 * t
        u = (x + 0.5 - a) / (b - a + 1)
        v = 0.8 - 0.7 * u
        if abs(u - 0.38) < 0.08 or abs(u - 0.72) < 0.08:   # pleats: dark folds
            v -= 0.25
        return v
    cv.part(cone, 'hat', cone_v, lo=2, hi=8, ring_dark=1, ring_lit=3, g=0.1)
    rim = ell(cx + 0.5, ht - 0.2, 3.3, 1.5)            # gold rim of the top opening
    cv.part(rim, 'gold', lambda x, y: 0.95 - 0.7 * (x + 0.5 - cx + 3) / 6.6, lo=2, ring_dark=1, ring_lit=3, g=0)
    inner = ell(cx + 0.5, ht - 0.2, 2.1, 0.7)          # inside of the shade, in shadow
    cv.part(inner, 'hat', lambda x, y: 0.6 if x < cx else 0.2, lo=0, hi=3, ring=False, g=0)
    cv.put(cx - 2, ht - 2, RP['gold'][7])                      # rim glint
    if anim == 'wave':   # a tentacle lifts the brim
        draw_tentacle(cv, (cx - 3, cy + 3), (3, cy + 1), (cx - 5, hb + 1), thick=0.6, suckers=False)

    # lantern: 5 wide, lit gold cap, two warm panes (small), dark base, handle ring
    lant = {(x, y) for x in range(x0, x0 + 5) for y in range(y0 + 1, y0 + 4)}
    lant |= {(x, y0) for x in range(x0 + 1, x0 + 4)} | {(x, y0 + 4) for x in range(x0 + 1, x0 + 4)}
    cv.part(lant, 'gold', lambda x, y: 0.9 - 0.18 * (x - x0) - 0.1 * (y - y0), lo=1, ring_dark=1, ring_lit=2, g=0)
    for y in range(y0 + 1, y0 + 4):
        cv.put(x0 + 1, y, RP['pane'][3] if y == y0 + 2 else RP['pane'][2] if y == y0 + 1 else RP['pane'][1])
        cv.put(x0 + 3, y, RP['pane'][2] if y == y0 + 2 else RP['pane'][1] if y == y0 + 1 else RP['pane'][0])
        cv.put(x0 + 2, y, RP['gold'][2])
        cv.put(x0, y, RP['gold'][(4, 5, 3)[y - y0 - 1]])
    cv.put(x0 + 1, y0, RP['gold'][6]); cv.put(x0 + 2, y0, RP['gold'][5]); cv.put(x0 + 3, y0, RP['gold'][4])
    for x in range(x0 + 1, x0 + 4):
        cv.put(x, y0 + 4, RP['gold'][1])
    cv.put(x0 + 2, y0 - 1, RP['gold'][2])
    for (x, y) in [(x0 - 1, y0 + 1), (x0 - 1, y0 + 2), (x0 + 5, y0 + 1), (x0 + 5, y0 + 2), (x0 + 5, y0 + 3),
                   (x0 - 1, y0 + 3), (x0, y0 - 0), (x0 + 4, y0), (x0, y0 + 4), (x0 + 4, y0 + 4), (x0 + 1, y0 - 1),
                   (x0 + 3, y0 - 1), (x0 + 1, y0 + 5), (x0 + 3, y0 + 5), (x0 + 2, y0 - 2)]:
        cv.px.pop((x, y), None)   # the lantern ring is trimmed back to vanilla's 5x5 + handle footprint
    return cv.image()


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
