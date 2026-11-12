"""Palette-lock finisher for the Boss Merchants art (one consistent, native-looking pass over every key).

    python -I BossMerchants/Unity/finish_art.py [--only key1,key2] [--rebuild-palette] [--no-previews]

Reads (never modifies):
  Unity/Art/merchants/<key>/sheets.json + sheets     designer merchant sheets (any rig)
  Unity/Art/idols/<key>_{icon,smallIcon,placed}.png  designer idols
  build/bossmerchants/reference, idol_reference      vanilla merchants / idols (extract_reference.py)
  the game's defaultlocalgroup bundle (UnityPy)      broad palette sample (cached)
Writes:
  Unity/Art/final/merchants/<key>/<same filenames> + sheets.json (copied verbatim)
  Unity/Art/final/idols/<key>_*.png
  Unity/Art/previews/final_<key>.png, final_lineup.png
  build/bossmerchants/finisher_cache/  (palette sample cache, master palette json + swatch, per-key stats)

Pipeline per key (merchant sheets as one colour group, idols as another, glow overlays as a third):
  1. Material regions = the group's unique colours clustered by OKLab hue (near-greys form a neutral region).
  2. Value/saturation match per region toward the vanilla reference (merchants for sheets, vanilla idols for
     idols): chroma capped at the vanilla p90 for that hue family, value contrast stretched toward the vanilla
     per-region spread, overall brightness pulled halfway to vanilla, small hue shift (cool shadows, warm lights).
  3. Palette lock: each region's colours, sorted dark->light, are assigned to master-palette colours by a
     monotone dynamic programme in OKLab (order kept, merging distinct shades penalised), so ramps keep their
     light-to-dark order and don't collapse into bands. Glow overlays (*_Light, *_Eye) lock to vanilla glow colours.
  4. Cleanup per cell: isolated single pixels removed (alpha -> 0), same-material dither/stipple pixels
     replaced by their neighbours' colour, silhouette edge recoloured as a dark hue-tinted outline taken from
     the vanilla outline colours, lightened only as clean runs (>= 3 px) on top/left lit edges.
  5. Detail (only when a sheet uses clearly fewer ramp steps per region than vanilla): one-pixel occlusion
     shadow under overlaps of a different material.
  6. Validation (exit 1): same sizes; alpha unchanged except removed speckle pixels inside cells; hard alpha;
     nothing outside cells; idols 16x16; every opaque output pixel is a master-palette (or glow-palette) colour.
Deterministic: no randomness, sorted iteration everywhere. Re-run any time the designer inputs change.
"""
import glob, hashlib, json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
ROOT = os.path.dirname(MOD)
ART = os.path.join(HERE, 'Art')
SRC_M, SRC_I = os.path.join(ART, 'merchants'), os.path.join(ART, 'idols')
OUT_M, OUT_I = os.path.join(ART, 'final', 'merchants'), os.path.join(ART, 'final', 'idols')
PREV = os.path.join(ART, 'previews')
BUILD = os.path.join(ROOT, 'build', 'bossmerchants')
REF_M = os.path.join(BUILD, 'reference', 'merchants')
REF_I = os.path.join(BUILD, 'idol_reference')
CACHE = os.path.join(BUILD, 'finisher_cache')
GAME = (r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa'
        r'\StandaloneWindows64')
KEYS = ['blocks', 'decor', 'explosives', 'potions', 'eggs', 'hydras', 'farmanimals', 'seeds']
IDOL_PARTS = ['icon', 'smallIcon', 'placed']
VANILLA_IDOLS = ['3930_MysteriousIdol', '3931_SlimeOil', '3932_PileOfChum', '3937_VoidMerchantIdol']

# rig -> preview frames (idle, walk, emote) as (texture suffix, frame index)
RIG_FRAMES = {
    'SlimeMerchantEntity': [('Slime_merchant', 0), ('Slime_merchant', 12), ('Slime_merchant', 34)],
    'CavelingMerchantEntity': [('Merchant', 0), ('Merchant', 12), ('Merchant', 20)],
    'FishingMerchantEntity': [('Fisher_merchant', 0), ('Fisher_merchant', 18), ('Fisher_merchant', 26)],
    'VoidMerchantEntity': [('voidMerchant_idle', 0), ('voidMerchant_move', 2), ('voidMerchant_emote', 13)],
}
VANILLA_SHEETS = [  # (rig, texture, path, frames or None for legacy rects from reference.json)
    ('CavelingMerchant', 'Merchant'), ('SlimeMerchant', 'Slime_merchant'),
    ('FishingMerchant', 'Fisher_merchant'), ('VoidMerchant', 'voidMerchant_idle'),
    ('VoidMerchant', 'voidMerchant_move'), ('VoidMerchant', 'voidMerchant_emote')]
VOID_FRAMES = {'voidMerchant_idle': 8, 'voidMerchant_move': 6, 'voidMerchant_emote': 27}

NEUTRAL_C = 0.03       # OKLab chroma below which a colour counts as grey
HUE_JOIN = 32.0
OUTLINE_DL = 0.24     # outline = this much darker (OKLab L) than the fill it bounds
LIT_DL = 0.10         # lit top/left edge runs        # degrees: hue clustering threshold for material regions
PALETTE_DE = 0.015     # OKLab distance under which sampled colours are merged into one palette entry


# ----------------------------------------------------------------------------- colour science
def srgb_to_oklab(rgb):
    c = np.asarray(rgb, dtype=np.float64) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                   [0.2119034982, 0.6806995451, 0.1073969566],
                   [0.0883024619, 0.2817188376, 0.6299787005]])
    m2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                   [1.9779984951, -2.4285922050, 0.4505937099],
                   [0.0259040371, 0.7827717662, -0.8086757660]])
    lms = np.cbrt(c @ m1.T)
    return lms @ m2.T


def lch(lab):
    lab = np.asarray(lab, dtype=np.float64)
    C = np.hypot(lab[..., 1], lab[..., 2])
    h = np.degrees(np.arctan2(lab[..., 2], lab[..., 1])) % 360.0
    return lab[..., 0], C, h


def from_lch(L, C, h):
    r = np.radians(h)
    return np.stack([L, C * np.cos(r), C * np.sin(r)], axis=-1)


def hue_dist(a, b):
    d = np.abs(np.asarray(a) - np.asarray(b)) % 360.0
    return np.minimum(d, 360.0 - d)


def family_of(C, h):
    """Hue family index: 0 = neutral, 1..12 = 30-degree OKLab hue bins."""
    return 0 if C < NEUTRAL_C else 1 + int(((h + 15.0) % 360.0) // 30.0)


FAMILY_NAMES = ['neutral'] + ['h%03d' % (i * 30) for i in range(12)]


# ----------------------------------------------------------------------------- image helpers
def load_rgba(path):
    return np.array(Image.open(path).convert('RGBA'))


def edge_mask(alpha_bool):
    """Opaque pixels with a transparent 4-neighbour (outside the array counts as transparent)."""
    p = np.pad(alpha_bool, 1)
    nb = p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]
    return alpha_bool & ~nb


def unique_colours(rgba):
    m = rgba[..., 3] == 255
    px = rgba[m][:, :3]
    if len(px) == 0:
        return np.zeros((0, 3), np.uint8), np.zeros(0, np.int64)
    cols, counts = np.unique(px, axis=0, return_counts=True)
    return cols, counts


def ckey(rgb):
    return (int(rgb[0]) << 16) | (int(rgb[1]) << 8) | int(rgb[2])


def key_image(rgb_arr):
    a = rgb_arr.astype(np.int64)
    return (a[..., 0] << 16) | (a[..., 1] << 8) | a[..., 2]


# ----------------------------------------------------------------------------- palette sample
def sample_bundle_textures():
    """Per-texture colour histograms from the game bundle (pixel-art textures only), cached."""
    bundle = os.path.join(GAME, 'defaultlocalgroup_assets_all.bundle')
    st = os.stat(bundle)
    tag = hashlib.sha1(f'{st.st_size}:{int(st.st_mtime)}:v2'.encode()).hexdigest()[:12]
    cache = os.path.join(CACHE, f'bundle_sample_{tag}.npz')
    if os.path.exists(cache):
        z = np.load(cache, allow_pickle=False)
        return list(z['names']), list(z['glow']), z['offsets'], z['cols'], z['counts']
    import UnityPy
    env = UnityPy.load(bundle)
    texs = []
    for o in env.objects:
        if o.type.name != 'Texture2D':
            continue
        t = o.read()
        if t.m_Width * t.m_Height > 512 * 512 or t.m_Width < 6 or t.m_Height < 6:
            continue
        if str(t.m_TextureFormat) not in ('3', '4', 'TextureFormat.RGB24', 'TextureFormat.RGBA32'):
            continue
        texs.append((t.m_Name, o.path_id, o))
    texs.sort(key=lambda x: (x[0], x[1]))
    skip = ('normal', 'mask', 'noise', 'gradient', 'shadow', 'lut', 'font', 'ripple', 'square', 'circle', 'cloud',
            'fog', 'vignette', 'blur', 'distort', 'flow', 'caustic', 'light_cookie')
    names, glow, offs, cols_all, cnt_all = [], [], [0], [], []
    for name, pid, o in texs:
        low = name.lower()
        if any(s in low for s in skip):
            continue
        try:
            a = np.array(o.read().image.convert('RGBA'))
        except Exception:
            continue
        al = a[..., 3]
        n_op = int((al == 255).sum())
        if n_op < 12:
            continue
        if ((al > 0) & (al < 255)).sum() > 0.02 * max(1, (al > 0).sum()):
            continue  # soft alpha: an effect / painted texture, not pixel art
        cols, counts = unique_colours(a)
        if len(cols) > 96:
            continue  # smooth gradients
        is_glow = any(s in low for s in ('emissive', '_light', 'glow'))
        names.append(name); glow.append(is_glow)
        cols_all.append(cols); cnt_all.append(counts); offs.append(offs[-1] + len(cols))
    os.makedirs(CACHE, exist_ok=True)
    cols = np.concatenate(cols_all).astype(np.uint8)
    counts = np.concatenate(cnt_all).astype(np.int64)
    np.savez_compressed(cache, names=np.array(names), glow=np.array(glow), offsets=np.array(offs),
                        cols=cols, counts=counts)
    return names, glow, np.array(offs), cols, counts


def vanilla_merchant_images():
    out = []
    for rig, tex in VANILLA_SHEETS:
        out.append((rig, tex, load_rgba(os.path.join(REF_M, rig, tex + '.png'))))
    return out


def vanilla_glow_images():
    d = os.path.join(REF_M, 'CavelingMerchant')
    return [load_rgba(os.path.join(d, 'Merchant_Light.png')), load_rgba(os.path.join(d, 'Merchant_Eye.png'))]


def vanilla_idol_images():
    return [load_rgba(p) for p in sorted(glob.glob(os.path.join(REF_I, '*_icon.png')))]


def greedy_reduce(cols, score, min_de, must=()):
    """Keep colours in descending score order unless within min_de of an already kept colour."""
    lab = srgb_to_oklab(cols)
    order = np.lexsort((key_image(cols), -score))
    kept = [i for i in must]
    kept_lab = [lab[i] for i in must]
    for i in order:
        if i in kept:
            continue
        if kept_lab:
            d = np.sqrt(((np.array(kept_lab) - lab[i]) ** 2).sum(1)).min()
            if d < min_de:
                continue
        kept.append(i); kept_lab.append(lab[i])
    return np.array(sorted(set(kept), key=lambda i: ckey(cols[i])))


def build_palette(rebuild=False):
    path = os.path.join(CACHE, 'master_palette.json')
    if os.path.exists(path) and not rebuild:
        with open(path) as f:
            return json.load(f)
    names, glow, offs, cols, counts = sample_bundle_textures()
    score = {}
    texcount = {}
    gscore = {}

    def add(cs, ns, weight, target):
        tot = ns.sum()
        for c, n in zip(cs, ns):
            k = ckey(c)
            target[k] = target.get(k, 0.0) + weight * np.sqrt(n / tot)
            if target is score:
                texcount[k] = texcount.get(k, 0) + 1

    for i in range(len(names)):
        cs, ns = cols[offs[i]:offs[i + 1]], counts[offs[i]:offs[i + 1]]
        add(cs, ns, 1.0, gscore if glow[i] else score)
    must_keys = set()
    for _, _, im in vanilla_merchant_images():
        cs, ns = unique_colours(im); add(cs, ns, 8.0, score); must_keys |= {ckey(c) for c in cs}
    for im in vanilla_idol_images():
        cs, ns = unique_colours(im); add(cs, ns, 4.0, score); must_keys |= {ckey(c) for c in cs}
    gmust = set()
    for im in vanilla_glow_images():
        cs, ns = unique_colours(im); add(cs, ns, 8.0, gscore); gmust |= {ckey(c) for c in cs}

    def finish(sc, must, min_tex, cap):
        ks = sorted(k for k in sc if k in must or texcount.get(k, 99) >= min_tex)
        arr = np.array([[(k >> 16) & 255, (k >> 8) & 255, k & 255] for k in ks], np.uint8)
        s = np.array([sc[k] + (100.0 if k in must else 0.0) for k in ks])
        must_idx = [i for i, k in enumerate(ks) if k in must]
        idx = greedy_reduce(arr, s, PALETTE_DE, must_idx)
        if len(idx) > cap:  # keep the best-scored ones (vanilla merchant colours always survive)
            idx = np.array(sorted(idx, key=lambda i: (-s[i], ckey(arr[i])))[:cap])
            idx = np.array(sorted(idx, key=lambda i: ckey(arr[i])))
        return arr[idx]

    main = finish(score, must_keys, 2, 640)
    # glow colours: vanilla merchant glow overlays + the bright part of emissive textures
    for k in list(gscore):
        if k not in gmust and srgb_to_oklab([k >> 16 & 255, k >> 8 & 255, k & 255])[0] < 0.6:
            del gscore[k]
    gl = finish(gscore, gmust, 1, 96)
    # outline colours: colours vanilla puts on silhouette edges (merchants, idols, sampled creatures aren't
    # available per-pixel from the cache, so use the reference images + the darkest palette ramp members)
    edge_hist = {}
    for im in [x[2] for x in vanilla_merchant_images()] + vanilla_idol_images():
        a = im[..., 3] == 255
        e = edge_mask(a)
        for c in im[e][:, :3]:
            k = ckey(c); edge_hist[k] = edge_hist.get(k, 0) + 1
    pal = {'main': main.tolist(), 'glow': gl.tolist(),
           'vanilla_edge': sorted([[k >> 16 & 255, k >> 8 & 255, k & 255, n] for k, n in edge_hist.items()])}
    # ramps per hue family, ordered by value
    lab = srgb_to_oklab(main)
    L, C, h = lch(lab)
    fam = {}
    for i in range(len(main)):
        fam.setdefault(FAMILY_NAMES[family_of(C[i], h[i])], []).append(i)
    pal['ramps'] = {f: [main[i].tolist() for i in sorted(ix, key=lambda i: (L[i], ckey(main[i])))]
                    for f, ix in sorted(fam.items())}
    os.makedirs(CACHE, exist_ok=True)
    with open(path, 'w') as f:
        json.dump(pal, f)
    save_swatch(pal)
    return pal


def save_swatch(pal):
    ramps = pal['ramps']
    w = max(len(r) for r in ramps.values())
    S = 10
    im = Image.new('RGBA', (60 + w * S, (len(ramps) + 2) * S * 2), (24, 24, 28, 255))
    d = ImageDraw.Draw(im)
    for y, (f, r) in enumerate(sorted(ramps.items())):
        d.text((2, y * S * 2 + 4), f, fill=(220, 220, 220, 255))
        for x, c in enumerate(r):
            d.rectangle([60 + x * S, y * S * 2 + 2, 60 + x * S + S - 1, y * S * 2 + 2 + S * 2 - 4], fill=tuple(c) + (255,))
    y = len(ramps)
    d.text((2, y * S * 2 + 4), 'glow', fill=(220, 220, 220, 255))
    for x, c in enumerate(sorted(pal['glow'], key=lambda c: srgb_to_oklab(c)[0])):
        d.rectangle([60 + x * S, y * S * 2 + 2, 60 + x * S + S - 1, y * S * 2 + S * 2 - 2], fill=tuple(c) + (255,))
    im.save(os.path.join(CACHE, 'master_palette.png'))


# ----------------------------------------------------------------------------- regions + reference stats
def cluster_regions(cols, counts):
    """Group unique colours into material regions by OKLab hue. Returns region id per colour."""
    lab = srgb_to_oklab(cols)
    L, C, h = lch(lab)
    chroma = np.where(C >= NEUTRAL_C)[0]
    rid = np.full(len(cols), -1)
    rid[C < NEUTRAL_C] = 0
    # average-linkage agglomerative clustering on circular hue, weighted by pixel count
    clusters = [[int(i)] for i in sorted(chroma, key=lambda i: (h[i], ckey(cols[i])))]

    def chue(cl):
        w = np.sqrt(counts[cl]) * C[cl]
        v = (w * np.exp(1j * np.radians(h[cl]))).sum()
        return np.degrees(np.angle(v)) % 360.0

    while len(clusters) > 1:
        hs = [chue(c) for c in clusters]
        best, bi, bj = None, -1, -1
        for i in range(len(clusters)):
            for j in range(i + 1, len(clusters)):
                d = hue_dist(hs[i], hs[j])
                if best is None or d < best:
                    best, bi, bj = d, i, j
        if best > HUE_JOIN:
            break
        clusters[bi] = clusters[bi] + clusters[bj]
        del clusters[bj]
    clusters.sort(key=lambda c: min(ckey(cols[i]) for i in c))
    for n, cl in enumerate(clusters):
        rid[cl] = n + 1
    return rid


def region_stats(images):
    """Per-region (L std, family, chroma list) over a set of RGBA images, plus overall mean L."""
    allc, alln = [], []
    for im in images:
        c, n = unique_colours(im); allc.append(c); alln.append(n)
    cols, inv = np.unique(np.concatenate(allc), axis=0, return_inverse=True)
    counts = np.bincount(inv.ravel(), weights=np.concatenate(alln)).astype(np.int64)
    rid = cluster_regions(cols, counts)
    lab = srgb_to_oklab(cols)
    L, C, h = lch(lab)
    regs = []
    for r in sorted(set(rid.tolist())):
        ix = rid == r
        w = counts[ix]
        if w.sum() < 30:
            continue
        mL = np.average(L[ix], weights=w)
        sL = np.sqrt(np.average((L[ix] - mL) ** 2, weights=w))
        regs.append({'n': int(w.sum()), 'mean': float(mL), 'std': float(sL), 'colours': int(ix.sum())})
    fam_c = {}
    for i in range(len(cols)):
        if C[i] >= NEUTRAL_C:
            fam_c.setdefault(family_of(C[i], h[i]), []).append((C[i], counts[i]))
    meanL = float(np.average(L, weights=counts))
    chrom = C >= NEUTRAL_C
    gp90 = weighted_pct(C[chrom], counts[chrom], 90) if chrom.any() else 0.1
    caps = {f: weighted_pct(np.array([a for a, _ in v]), np.array([b for _, b in v]), 90)
            for f, v in fam_c.items() if sum(b for _, b in v) >= 40}
    stds = sorted(r['std'] for r in regs)
    return {'meanL': meanL, 'region_std': float(np.median(stds)) if stds else 0.15, 'chroma_p90': float(gp90),
            'family_caps': {int(k): float(v) for k, v in caps.items()},
            'meanC': float(np.average(C[chrom], weights=counts[chrom])) if chrom.any() else 0.0,
            'colours_per_region': float(np.mean([r['colours'] for r in regs])) if regs else 0.0}


def weighted_pct(vals, w, p):
    o = np.argsort(vals)
    v, ww = vals[o], w[o].astype(np.float64)
    cw = np.cumsum(ww) / ww.sum()
    return float(v[min(len(v) - 1, np.searchsorted(cw, p / 100.0))])


def cell_rects(tex, img_h):
    """Cell rects (x0, y0, x1, y1) in image (top-down) coordinates, keyed by frame index."""
    cells = {}
    if 'legacySprites' in tex and tex['legacySprites']:
        for s in tex['legacySprites']:
            x, y, w, h = [int(round(v)) for v in s['rect']]
            idx = int(s['name'].rsplit('_', 1)[1])
            cells[idx] = (x, img_h - y - h, x + w, img_h - y)
    else:
        W, H = tex['size']
        n = int(tex['srcFrameCount'])
        fw = W // n
        for i in range(n):
            cells[i] = (i * fw, 0, (i + 1) * fw, H)
    return cells


def vanilla_cells(rig, texname, img):
    if rig == 'VoidMerchant':
        n = VOID_FRAMES[texname]; fw = img.shape[1] // n
        return {i: (i * fw, 0, (i + 1) * fw, img.shape[0]) for i in range(n)}
    with open(os.path.join(BUILD, 'reference', 'reference.json')) as f:
        ref = json.load(f)
    sprites = ref['merchants'][rig]['sheets'][texname]
    return cell_rects({'legacySprites': sprites}, img.shape[0])


def colours_per_region_cell(img, cells, rid_of):
    vals = []
    for (x0, y0, x1, y1) in cells.values():
        c = img[y0:y1, x0:x1]
        m = c[..., 3] == 255
        if m.sum() < 20:
            continue
        ks = key_image(c[..., :3])[m]
        regs = {}
        for k in ks.tolist():
            regs.setdefault(rid_of.get(k, -1), set()).add(k)
        for r, s in regs.items():
            vals.append(len(s))
    return float(np.mean(vals)) if vals else 0.0


# ----------------------------------------------------------------------------- the lock
class Palette:
    def __init__(self, rgb):
        self.rgb = np.array(rgb, np.uint8)
        self.lab = srgb_to_oklab(self.rgb)
        self.L, self.C, self.h = lch(self.lab)
        order = np.lexsort((key_image(self.rgb), self.L))
        self.rgb, self.lab, self.L, self.C, self.h = (self.rgb[order], self.lab[order], self.L[order],
                                                      self.C[order], self.h[order])
        self.keys = set(ckey(c) for c in self.rgb)


def lab_cost(t, P):
    """Perceptual cost of every palette colour for target OKLab t: value first, then hue, then chroma."""
    dL = P.lab[:, 0] - t[0]
    tC = np.hypot(t[1], t[2])
    dC = P.C - tC
    dab2 = (P.lab[:, 1] - t[1]) ** 2 + (P.lab[:, 2] - t[2]) ** 2
    dH2 = np.maximum(dab2 - dC ** 2, 0.0)
    cost = 1.6 * dL ** 2 + 1.5 * dC ** 2 + 2.5 * dH2
    if tC >= 0.025:  # keep coloured targets (pale blues, dusty greens) off the greys
        cost = np.where(P.C < 0.022, cost + 0.4 * tC, cost)
    if tC >= 0.03:  # a clearly coloured target never jumps to a different hue family
        th = np.degrees(np.arctan2(t[2], t[1])) % 360.0
        cost = np.where((P.C >= 0.03) & (hue_dist(P.h, th) > 40.0), cost + 1.0, cost)
    return cost


def monotone_lock(targets, weights, P, merge_pen=0.006):
    """Assign targets (sorted dark->light) to palette indices (palette sorted by L), non-decreasing,
    penalising two distinct neighbours sharing one palette colour. Returns palette index per target."""
    n, m = len(targets), len(P.rgb)
    cost = np.array([lab_cost(t, P) * w for t, w in zip(targets, weights)])
    dp = np.zeros((n, m)); back = np.zeros((n, m), np.int64)
    dp[0] = cost[0]
    for i in range(1, n):
        de = np.sqrt(((targets[i] - targets[i - 1]) ** 2).sum())
        pen = merge_pen * min(weights[i], weights[i - 1]) if de > 0.03 else 0.0
        prev = dp[i - 1]
        # best strictly-lower index k < j
        pm = np.minimum.accumulate(prev)
        am = np.zeros(m, np.int64)
        best = 0
        for j in range(m):
            if prev[j] < prev[best]:
                best = j
            am[j] = best
        lower = np.concatenate([[np.inf], pm[:-1]])
        lower_arg = np.concatenate([[0], am[:-1]])
        same = prev + pen
        take_same = same <= lower
        dp[i] = cost[i] + np.where(take_same, same, lower)
        back[i] = np.where(take_same, np.arange(m), lower_arg)
    j = int(np.argmin(dp[-1]))
    out = [j]
    for i in range(n - 1, 0, -1):
        j = int(back[i, j]); out.append(j)
    return out[::-1]


def adjust_targets(cols, counts, rid, ref, sheet_meanL):
    """Value/saturation match per material region. Returns OKLab targets per colour."""
    lab = srgb_to_oklab(cols)
    L, C, h = lch(lab)
    Lt, Ct = L.copy(), C.copy()
    shift = float(np.clip(0.5 * (ref['meanL'] - sheet_meanL), -0.08, 0.08))
    for r in sorted(set(rid.tolist())):
        ix = np.where(rid == r)[0]
        w = counts[ix].astype(np.float64)
        mL = np.average(L[ix], weights=w)
        sL = np.sqrt(np.average((L[ix] - mL) ** 2, weights=w))
        if len(ix) >= 3 and sL > 1e-3:
            s = float(np.clip(ref['region_std'] / sL, 0.9, 1.3))
        else:
            s = 1.0
        # Brightness pull fades out toward the region's lightest step, and that top step (the lit top plane
        # of the 3/4 view) is never darkened, so top planes stay the brightest surfaces.
        lo, hi = L[ix].min(), L[ix].max()
        tpos = (L[ix] - lo) / (hi - lo) if hi - lo > 1e-3 else np.ones(len(ix))
        Lt[ix] = mL + shift * (1.0 - tpos) + (L[ix] - mL) * s
        top = L[ix] >= hi - 0.02
        Lt[ix] = np.where(top, np.maximum(Lt[ix], L[ix]), Lt[ix])
        Lt[ix] = np.clip(Lt[ix], 0.1, 0.985)
        if r != 0:
            hc = np.degrees(np.angle((w * C[ix] * np.exp(1j * np.radians(h[ix]))).sum())) % 360.0
            cap = ref['family_caps'].get(family_of(1.0, hc), ref['chroma_p90'])
            cap = min(cap, ref['chroma_p90'] * 1.25)
            p90 = weighted_pct(C[ix], counts[ix], 90)
            if p90 > cap:
                Ct[ix] = C[ix] * (cap / p90)
    # hue shift: shadows toward indigo, lights toward warm yellow (only for chromatic colours)
    ht = h.copy()
    for i in range(len(cols)):
        if C[i] < NEUTRAL_C:
            continue
        if Lt[i] < 0.45:
            k = min(1.0, (0.45 - Lt[i]) / 0.3) * 10.0
            ht[i] = rotate_toward(h[i], 270.0, k)
        elif Lt[i] > 0.72:
            k = min(1.0, (Lt[i] - 0.72) / 0.2) * 8.0
            ht[i] = rotate_toward(h[i], 85.0, k)
    return from_lch(Lt, Ct, ht)


def rotate_toward(h, target, deg):
    d = (target - h + 540.0) % 360.0 - 180.0
    return (h + np.sign(d) * min(abs(d), deg)) % 360.0


def lock_group(images, P, ref, glow=False):
    """Colour map {src key: (palette rgb, region id)} for a group of RGBA images sharing one mapping."""
    allc, alln = [], []
    for im in images:
        c, n = unique_colours(im); allc.append(c); alln.append(n)
    cols, inv = np.unique(np.concatenate(allc), axis=0, return_inverse=True)
    counts = np.bincount(inv.ravel(), weights=np.concatenate(alln)).astype(np.int64)
    if glow:
        rid = np.zeros(len(cols), np.int64)
        targets = srgb_to_oklab(cols)
    else:
        rid = cluster_regions(cols, counts)
        meanL = float(np.average(lch(srgb_to_oklab(cols))[0], weights=counts))
        targets = adjust_targets(cols, counts, rid, ref, meanL)
    cmap = {}
    for r in sorted(set(rid.tolist())):
        ix = np.where(rid == r)[0]
        ix = ix[np.lexsort((key_image(cols[ix]), targets[ix, 0]))]
        ws = np.sqrt(counts[ix].astype(np.float64))
        ws = ws / ws.mean()
        pj = monotone_lock(targets[ix], ws, P)
        for i, j in zip(ix, pj):
            cmap[ckey(cols[i])] = (tuple(int(v) for v in P.rgb[j]), int(r))
    return cmap


# ----------------------------------------------------------------------------- cleanup
class OutlineSet:
    """Dark hue-tinted outline colours (vanilla silhouette-edge colours that are also in the palette)."""

    def __init__(self, pal, P):
        e = [(c[:3], c[3]) for c in pal['vanilla_edge']]
        rgb = np.array([c for c, _ in e], np.uint8)
        n = np.array([w for _, w in e], np.float64)
        lab = srgb_to_oklab(rgb)
        L = lab[:, 0]
        dark = L < 0.5
        self.medL = weighted_pct(L[dark], n[dark], 50)
        self.floorL = weighted_pct(L[dark], n[dark], 10)
        self.P = P
        self.memo = {}

    def shade(self, base_rgb, dL):
        """Palette colour dL darker than base, same material hue drifting cool, chroma kept (memoised).
        Vanilla silhouette edges are a darker step of the material they bound, not one global line colour."""
        k = (tuple(int(v) for v in base_rgb), round(dL, 3))
        if k in self.memo:
            return self.memo[k]
        P = self.P
        L0, C0, h0 = [float(v) for v in lch(srgb_to_oklab(np.array(k[0])))]
        tL = max(self.floorL, L0 - dL)
        if C0 >= NEUTRAL_C:
            tC, th = max(0.035, 0.85 * C0), rotate_toward(h0, 275.0, 4.0 + 20.0 * dL)
        else:
            tC, th = 0.02, 280.0
        t = from_lch(np.array(tL), np.array(tC), np.array(th))
        cost = 2.0 * (P.L - tL) ** 2 + lab_cost(t, P)
        cost = np.where(P.L > L0 - 0.03, np.inf, cost)
        res = tuple(int(v) for v in P.rgb[int(np.argmin(cost))]) if np.isfinite(cost).any() else k[0]
        self.memo[k] = res
        return res


N4 = [(-1, 0), (1, 0), (0, -1), (0, 1)]
N8 = N4 + [(-1, -1), (-1, 1), (1, -1), (1, 1)]


def clean_cell(rgba, regions, outlines, region_lab, P, detail):
    """Cleanup on one cell (rgba HxWx4 already palette-locked, regions HxW int). Returns (rgba, removed)."""
    H, W = rgba.shape[:2]
    a = rgba[..., 3] == 255
    rgb = rgba[..., :3].astype(np.int64)
    removed = np.zeros_like(a)

    def op(y, x):
        return 0 <= y < H and 0 <= x < W and a[y, x]

    # 1a. isolated single pixels -> transparent
    for y in range(H):
        for x in range(W):
            if a[y, x] and not any(op(y + dy, x + dx) for dy, dx in N8):
                a[y, x] = False; removed[y, x] = True
    lab_cache = {}

    def lab_of(c):
        k = (int(c[0]), int(c[1]), int(c[2]))
        if k not in lab_cache:
            lab_cache[k] = srgb_to_oklab(np.array(k))
        return lab_cache[k]

    # 1b. dither / stipple: a pixel whose colour appears in none of its 8 neighbours while >= 3 of its
    # 4 neighbours share one colour of the same material that is close in value -> take that colour.
    # Strong value jumps (specular dots, eyes, glints) are kept.
    for y in range(H):
        for x in range(W):
            if not a[y, x]:
                continue
            nb4 = [(y + dy, x + dx) for dy, dx in N4]
            if not all(op(*p) for p in nb4):
                continue
            me = tuple(rgb[y, x])
            if any(op(y + dy, x + dx) and tuple(rgb[y + dy, x + dx]) == me for dy, dx in N8):
                continue
            votes = {}
            for p in nb4:
                votes.setdefault(tuple(rgb[p]), []).append(p)
            q, ps = max(sorted(votes.items()), key=lambda kv: len(kv[1]))
            if len(ps) < 3:
                continue
            lm, lq = lab_of(me), lab_of(q)
            de = float(np.sqrt(((lm - lq) ** 2).sum()))
            if de < 0.16 and abs(lm[0] - lq[0]) < 0.14 and regions[y, x] == regions[ps[0]]:
                rgb[y, x] = q; regions[y, x] = regions[ps[0]]
    # 1c. broken lit edges: a silhouette-edge pixel lighter than its neighbours whose colour appears in none
    # of them (a lone highlight dot on the outline reads as stipple) takes its neighbours' colour; clean lit
    # runs are re-added by the outline pass below.
    e = edge_mask(a)
    for y in range(H):
        for x in range(W):
            if not e[y, x]:
                continue
            me = tuple(rgb[y, x])
            nbs = [tuple(rgb[y + dy, x + dx]) for dy, dx in N8 if op(y + dy, x + dx)]
            if len(nbs) < 2 or me in nbs:
                continue
            votes = {}
            for c in nbs:
                votes[c] = votes.get(c, 0) + 1
            q, n = max(sorted(votes.items()), key=lambda kv: kv[1])
            if n < 2:
                continue
            if lab_of(me)[0] > lab_of(q)[0] + 0.04:
                rgb[y, x] = q
                ys, xs = [(y + dy, x + dx) for dy, dx in N8 if op(y + dy, x + dx) and tuple(rgb[y + dy, x + dx]) == q][0]
                regions[y, x] = regions[ys, xs]
    # 2. outlines on the silhouette edge: a dark, hue-tinted step of the material it bounds (bottom/right
    # and sides), lighter only as clean runs of >= 3 px along top/left lit edges (run ends stay dark).
    interior = a & ~e
    # "solid" = interior pixels whose 4 neighbours are interior too: only edges touching solid mass (forms at
    # least 5 px across) get an outline, so thin details (fuses, feathers, torcs, crooks, boots, straps) keep
    # their own ramp colours instead of turning into outline.
    pi = np.pad(interior, 1)
    solid = interior & pi[:-2, 1:-1] & pi[2:, 1:-1] & pi[1:-1, :-2] & pi[1:-1, 2:]
    ps = np.pad(solid, 1)
    near_solid = np.zeros_like(a)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            near_solid |= ps[1 + dy:1 + dy + H, 1 + dx:1 + dx + W]
    near2 = near_solid.copy()
    pn = np.pad(near_solid, 1)
    for dy, dx in N8:
        near2 |= pn[1 + dy:1 + dy + H, 1 + dx:1 + dx + W]
    new = rgb.copy()
    is_outline = np.zeros_like(a)
    base_of = {}
    for y in range(H):
        for x in range(W):
            if not e[y, x] or not near2[y, x]:
                continue
            inn = [(y + dy, x + dx) for dy, dx in N4 if op(y + dy, x + dx) and interior[y + dy, x + dx]]
            if not inn:
                inn = [(y + dy, x + dx) for dy, dx in N8 if op(y + dy, x + dx) and interior[y + dy, x + dx]]
            if not inn:
                continue  # thin 1-2 px feature (strap, rod, tentacle tip): keep its own ramp colour
            votes = {}
            for p in inn:
                votes[tuple(rgb[p])] = votes.get(tuple(rgb[p]), 0) + 1
            base = max(sorted(votes), key=lambda c: votes[c])
            o = outlines.shade(base, OUTLINE_DL)
            if lab_of(rgb[y, x])[0] <= lab_of(o)[0] + 0.01:
                continue  # already at/below the outline step: designer's own dark line
            new[y, x] = o; is_outline[y, x] = True; base_of[(y, x)] = base

    def lit_run(cells):
        if len(cells) < 3:
            return
        for (y, x) in (cells[1:-1] if len(cells) >= 5 else cells[1:]):
            lit = outlines.shade(base_of[(y, x)], LIT_DL)
            if lab_of(lit)[0] > lab_of(new[y, x])[0] + 0.04:
                new[y, x] = lit

    for y in range(H):
        run = []
        for x in range(W + 1):
            if x < W and is_outline[y, x] and not op(y - 1, x) and op(y + 1, x):
                run.append((y, x))
            else:
                lit_run(run); run = []
    for x in range(W):
        run = []
        for y in range(H + 1):
            if y < H and is_outline[y, x] and not op(y, x - 1) and op(y, x + 1) and op(y - 1, x):
                run.append((y, x))
            else:
                lit_run(run); run = []
    rgb = new
    # 3. detail: occlusion shadow under overlaps of a different material (sparse sheets only)
    if detail:
        cand = np.zeros_like(a)
        for y in range(1, H):
            for x in range(W):
                if interior[y, x] and not is_outline[y, x] and a[y - 1, x] and regions[y - 1, x] != regions[y, x] \
                        and regions[y, x] != 0:
                    cand[y, x] = True
        for y in range(H):
            x = 0
            while x < W:
                if cand[y, x]:
                    x1 = x
                    while x1 < W and cand[y, x1]:
                        x1 += 1
                    if x1 - x >= 2:
                        for xx in range(x, x1):
                            rgb[y, xx] = step_darker(rgb[y, xx], P)
                    x = x1
                else:
                    x += 1
    out = rgba.copy()
    out[..., :3] = rgb.astype(np.uint8)
    out[..., 3] = np.where(a, 255, 0)
    out[~a, :3] = 0
    return out, removed


def step_darker(c, P):
    lab = srgb_to_oklab(np.array(c))
    tL = lab[0] - 0.08
    t = np.array([tL, lab[1], lab[2]])
    t = from_lch(np.array(tL), *[np.array(v) for v in lch(lab)[1:]])
    t = from_lch(np.array(tL), lch(lab)[1], rotate_toward(float(lch(lab)[2]), 270.0, 6.0))
    cost = lab_cost(t, P)
    cost = np.where((P.L > lab[0] - 0.03) | (P.L < lab[0] - 0.16), np.inf, cost)
    if not np.isfinite(cost).any():
        return c
    return P.rgb[int(np.argmin(cost))].astype(np.int64)


# ----------------------------------------------------------------------------- per key
def apply_cmap(img, cmap):
    out = img.copy()
    regions = np.full(img.shape[:2], -1, np.int64)
    ks = key_image(img[..., :3])
    m = img[..., 3] == 255
    for y, x in zip(*np.where(m)):
        c, r = cmap[int(ks[y, x])]
        out[y, x, :3] = c; regions[y, x] = r
    out[~m] = 0
    return out, regions


def region_labs(cmap):
    acc = {}
    for _, (c, r) in sorted(cmap.items()):
        acc.setdefault(r, []).append(srgb_to_oklab(np.array(c)))
    return {r: np.median(np.array(v), axis=0) for r, v in acc.items()}


def finish_sheet(img, cells, cmap, outlines, P, detail, cleanup=True):
    locked, regions = apply_cmap(img, cmap)
    if not cleanup:
        return locked, np.zeros(img.shape[:2], bool)
    rl = region_labs(cmap)
    rl.setdefault(-1, np.array([0.3, 0.0, 0.0]))
    out = locked.copy()
    removed = np.zeros(img.shape[:2], bool)
    for idx in sorted(cells):
        x0, y0, x1, y1 = cells[idx]
        o, rm = clean_cell(locked[y0:y1, x0:x1].copy(), regions[y0:y1, x0:x1].copy(), outlines, rl, P, detail)
        out[y0:y1, x0:x1] = o
        removed[y0:y1, x0:x1] |= rm
    return out, removed


def validate(name, src, out, cells, removed, allowed_keys, errors, idol=False):
    if src.shape != out.shape:
        errors.append(f'{name}: size {out.shape} != {src.shape}'); return
    if idol and src.shape[:2] != (16, 16):
        errors.append(f'{name}: idol not 16x16')
    if not set(np.unique(out[..., 3]).tolist()) <= {0, 255}:
        errors.append(f'{name}: soft alpha')
    sa, oa = src[..., 3] == 255, out[..., 3] == 255
    if (oa & ~sa).any():
        errors.append(f'{name}: {int((oa & ~sa).sum())} new opaque pixels')
    gone = sa & ~oa
    if (gone & ~removed).any():
        errors.append(f'{name}: {int((gone & ~removed).sum())} pixels lost that were not speckle')
    inside = np.zeros(sa.shape, bool)
    for (x0, y0, x1, y1) in cells.values():
        inside[y0:y1, x0:x1] = True
    if (oa & ~inside).any() or (gone & ~inside).any():
        errors.append(f'{name}: pixels outside cells')
    ks = key_image(out[..., :3])[oa]
    bad = set(np.unique(ks).tolist()) - allowed_keys
    if bad:
        errors.append(f'{name}: {len(bad)} colours not in the master palette')


def process_key(key, P, G, outlines, ref_m, ref_i, ref_cpr, errors, log):
    sj_path = os.path.join(SRC_M, key, 'sheets.json')
    with open(sj_path) as f:
        sj = json.load(f)
    sheets = []
    for t in sj['textures']:
        p = os.path.join(SRC_M, t['output'].replace('/', os.sep))
        img = load_rgba(p)
        if list(img.shape[1::-1]) != list(t['size']):
            errors.append(f'{key}: input {t["output"]} is {img.shape[1::-1]}, sheets.json says {t["size"]}')
        glow = t['replacesTexture'].endswith('_Light') or t['replacesTexture'].endswith('_Eye')
        sheets.append((t, p, img, cell_rects(t, img.shape[0]), glow))
    main = [s for s in sheets if not s[4]]
    cmap = lock_group([s[2] for s in main], P, ref_m)
    gl = [s for s in sheets if s[4]]
    gmap = lock_group([s[2] for s in gl], G, None, glow=True) if gl else {}
    # sparsity vs vanilla (colours per material region per cell, measured on the source)
    rid_of = {k: r for k, (_, r) in cmap.items()}
    cpr = np.mean([colours_per_region_cell(s[2], s[3], rid_of) for s in main])
    detail = cpr < 0.8 * ref_cpr
    out_dir = os.path.join(OUT_M, key)
    os.makedirs(out_dir, exist_ok=True)
    results = {}
    for t, p, img, cells, glow in sheets:
        if glow:
            out, removed = finish_sheet(img, cells, gmap, outlines, G, False, cleanup=False)
            allowed = G.keys
        else:
            out, removed = finish_sheet(img, cells, cmap, outlines, P, detail)
            allowed = P.keys
        validate(f'{key}/{os.path.basename(p)}', img, out, cells, removed, allowed, errors)
        Image.fromarray(out, 'RGBA').save(os.path.join(out_dir, os.path.basename(p)))
        results[t['replacesTexture']] = (img, out, cells)
    shutil.copyfile(sj_path, os.path.join(out_dir, 'sheets.json'))
    # idols: one colour group for icon/smallIcon/placed
    ids = [(part, os.path.join(SRC_I, f'{key}_{part}.png')) for part in IDOL_PARTS]
    missing = [p for _, p in ids if not os.path.exists(p)]
    if missing:
        errors.append(f'{key}: missing idol files {missing}')
        return sj, results, None
    imgs = [load_rgba(p) for _, p in ids]
    imap = lock_group(imgs, P, ref_i)
    os.makedirs(OUT_I, exist_ok=True)
    idol_out = {}
    for (part, p), img in zip(ids, imgs):
        cells = {0: (0, 0, img.shape[1], img.shape[0])}
        out, removed = finish_sheet(img, cells, imap, outlines, P, False)
        validate(f'idols/{key}_{part}.png', img, out, cells, removed, P.keys, errors, idol=True)
        Image.fromarray(out, 'RGBA').save(os.path.join(OUT_I, f'{key}_{part}.png'))
        idol_out[part] = (img, out)
    # stats
    def st(imgs_):
        allc = [unique_colours(i) for i in imgs_]
        c = np.concatenate([a for a, _ in allc]); n = np.concatenate([b for _, b in allc])
        L, C, _ = lch(srgb_to_oklab(c))
        return float(np.average(L, weights=n)), float(np.average(C, weights=n)), weighted_pct(C, n, 90)
    b = st([s[2] for s in main]); a = st([results[s[0]['replacesTexture']][1] for s in main])
    bi = st([idol_out['icon'][0]]); ai = st([idol_out['icon'][1]])
    log[key] = {'rig': sj['rig'], 'sheet_before': b, 'sheet_after': a, 'idol_before': bi, 'idol_after': ai,
                'colours_per_region': float(cpr), 'detail_pass': bool(detail),
                'removed_px': int(sum(int(((r[0][..., 3] == 255) & (r[1][..., 3] == 0)).sum())
                                      for r in results.values()))}
    return sj, results, idol_out


# ----------------------------------------------------------------------------- previews
def font(sz=12):
    for f in ('consola.ttf', 'DejaVuSansMono.ttf'):
        try:
            return ImageFont.truetype(f, sz)
        except OSError:
            pass
    return ImageFont.load_default()


def crop(img, rect):
    x0, y0, x1, y1 = rect
    return Image.fromarray(img[y0:y1, x0:x1], 'RGBA')


def composite_frame(results, rig, tex, idx, which):
    """Frame image (main + glow overlays for the Caveling rig). which 0 = before, 1 = after."""
    img = results[tex][which]
    base = crop(img, results[tex][2][idx]).copy()
    if rig == 'CavelingMerchantEntity':
        if 'Merchant_Light' in results:
            lt = results['Merchant_Light']
            if idx in lt[2]:
                base.alpha_composite(crop(lt[which], lt[2][idx]))
        if 'Merchant_Eye' in results:
            ey = results['Merchant_Eye']
            ei = 10 if idx >= 18 else idx
            if ei in ey[2]:
                e = crop(ey[which], ey[2][ei])
                base.alpha_composite(e, (0, base.size[1] - e.size[1]))
    return base


def vanilla_idles():
    out = []
    for rig, tex, label in [('CavelingMerchant', 'Merchant', 'Caveling'), ('SlimeMerchant', 'Slime_merchant', 'Slime'),
                            ('FishingMerchant', 'Fisher_merchant', 'Fishing'), ('VoidMerchant', 'voidMerchant_idle', 'Void')]:
        img = load_rgba(os.path.join(REF_M, rig, tex + '.png'))
        fr = crop(img, vanilla_cells(rig, tex, img)[0]).copy()
        if rig == 'CavelingMerchant':
            for g in ('Merchant_Light', 'Merchant_Eye'):
                gi = load_rgba(os.path.join(REF_M, rig, g + '.png'))
                gc = crop(gi, vanilla_cells(rig, g, gi)[0]) if os.path.exists(os.path.join(REF_M, rig, g + '.png')) else None
                if gc is not None:
                    fr.alpha_composite(gc, (0, fr.size[1] - gc.size[1]))
        out.append((label, fr))
    return out


def scaled(im, s):
    return im.resize((im.size[0] * s, im.size[1] * s), Image.NEAREST)


BG = (34, 32, 42, 255)
FLOOR = (52, 48, 58, 255)


def strip(items, s, gap, label_font, label=True):
    """Bottom-aligned strip of (label, image) at scale s."""
    ims = [(l, scaled(i, s)) for l, i in items]
    W = sum(i.size[0] for _, i in ims) + gap * (len(ims) + 1)
    H = max(i.size[1] for _, i in ims) + (16 if label else 0)
    out = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(out)
    x = gap
    for l, i in ims:
        out.alpha_composite(i, (x, H - (16 if label else 0) - i.size[1]))
        if label:
            d.text((x, H - 13), l, fill=(220, 220, 220, 255), font=label_font)
        x += i.size[0] + gap
    return out


def stack(parts, title_font):
    W = max(p.size[0] for _, p in parts) + 20
    H = sum(p.size[1] + 24 for _, p in parts) + 10
    out = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(out)
    y = 8
    for t, p in parts:
        d.text((10, y), t, fill=(240, 220, 150, 255), font=title_font); y += 18
        out.alpha_composite(p, (10, y)); y += p.size[1] + 6
    return out


def key_preview(key, sj, results, idols):
    f = font(12)
    rig = sj['rig']
    frames = RIG_FRAMES[rig]
    names = ['idle', 'walk', 'emote']
    before = [(f'{n} (before)', composite_frame(results, rig, t, i, 0)) for n, (t, i) in zip(names, frames)]
    after = [(f'{n} (after)', composite_frame(results, rig, t, i, 1)) for n, (t, i) in zip(names, frames)]
    row = strip([x for pair in zip(before, after) for x in pair], 6, 12, f)
    idol_items = []
    for part in IDOL_PARTS[:2]:
        b, a = idols[part]
        idol_items += [(f'{part} before', Image.fromarray(b, 'RGBA')), (f'{part} after', Image.fromarray(a, 'RGBA'))]
    irow = strip(idol_items, 6, 12, f)
    img = stack([(f'{key} ({rig}): before / after at 6x', row), ('idol before / after at 6x', irow)], font(14))
    img.save(os.path.join(PREV, f'final_{key}.png'))


def design_names(key):
    p = os.path.join(MOD, 'docs', 'designs', key + '.json')
    try:
        with open(p, encoding='utf-8') as f:
            d = json.load(f)
        return d.get('merchantName', key), d.get('idolName', '')
    except (OSError, ValueError):
        return key, ''


def stone_floor(w, h, tile=16):
    """Dark stone floor mock (procedural, deterministic) for the 1x standing strip."""
    im = Image.new('RGBA', (w, h), (44, 41, 50, 255))
    d = ImageDraw.Draw(im)
    for ty in range(0, h, tile):
        for tx in range(0, w, tile):
            v = ((tx * 7 + ty * 13) // tile) % 3
            base = [(46, 43, 53), (42, 40, 49), (49, 45, 55)][v]
            d.rectangle([tx, ty, tx + tile - 1, ty + tile - 1], fill=base + (255,))
            d.line([tx, ty, tx + tile - 1, ty], fill=(56, 52, 62, 255))       # lit top edge
            d.line([tx, ty + tile - 1, tx + tile - 1, ty + tile - 1], fill=(33, 31, 39, 255))
            d.line([tx + tile - 1, ty, tx + tile - 1, ty + tile - 1], fill=(36, 34, 42, 255))
    return im


def review_all(done):
    """One review sheet for Sid: a row per merchant + vanilla row + 1x standing strip on a stone floor."""
    f, fb = font(13), font(15)
    S, SI, GAP = 4, 8, 14
    rows = []
    for key, sj, results, idol in done:
        rig = sj['rig']
        frs = [composite_frame(results, rig, t, i, 1) for t, i in RIG_FRAMES[rig]]
        ico = Image.fromarray(idol['icon'][1], 'RGBA')
        mname, iname = design_names(key)
        rows.append((f'{mname}  ({key}, {rig.replace("Entity", "")})', f'idol: {iname}',
                     [('idle', frs[0]), ('walk', frs[1]), ('emote', frs[2])], ico))
    van = vanilla_idles()
    vi = [(n.split('_', 1)[1], Image.open(os.path.join(REF_I, n + '_icon.png')).convert('RGBA')) for n in VANILLA_IDOLS]
    rows.append(('Vanilla merchants (Caveling, Slime, Fishing, Void)', 'vanilla idols: Mysterious Idol, Slime Oil, '
                 'Pile of Chum, Void Merchant Idol', [(l, im) for l, im in van], None))
    LABEL_W = 16
    blocks = []
    for title, sub, frames, ico in rows:
        ims = [scaled(im, S) for _, im in frames]
        if ico is not None:
            idols_ = [(ico, ico)]
        else:
            idols_ = [(im, im) for _, im in vi]
        TOP = 44
        h = max([i.size[1] for i in ims] + [16 * SI]) + 18 + TOP
        w = LABEL_W + sum(i.size[0] + GAP for i in ims) + 20 + sum(16 + 8 + 16 * SI + GAP for _ in idols_)
        blk = Image.new('RGBA', (w, h), BG)
        d = ImageDraw.Draw(blk)
        d.text((6, 6), title, fill=(240, 220, 150, 255), font=fb)
        d.text((6, 26), sub, fill=(200, 200, 210, 255), font=f)
        x = LABEL_W
        for (l, _), im in zip(frames, ims):
            blk.alpha_composite(im, (x, h - 18 - im.size[1]))
            d.text((x, h - 15), l, fill=(190, 190, 190, 255), font=f)
            x += im.size[0] + GAP
        x += 20
        for one, _ in idols_:
            blk.alpha_composite(one, (x, h - 18 - 16))
            blk.alpha_composite(scaled(one, SI), (x + 24, h - 18 - 16 * SI))
            x += 24 + 16 * SI + GAP
        d.text((x - (24 + 16 * SI + GAP) * len(idols_), h - 15), 'idol 1x / 8x', fill=(190, 190, 190, 255), font=f)
        blocks.append(blk)
    # 1x standing strip on a stone floor, feet on one ground line
    chars = []
    for key, sj, results, idol in done:
        t, i = RIG_FRAMES[sj['rig']][0]
        chars.append(trim(composite_frame(results, sj['rig'], t, i, 1)))
    chars += [trim(im) for _, im in van]
    sw = sum(c.size[0] + 6 for c in chars) + 26
    sh = max(c.size[1] for c in chars) + 30
    floor = stone_floor(sw, sh)
    x = 14
    for c in chars:
        floor.alpha_composite(c, (x, sh - 12 - c.size[1])); x += c.size[0] + 6
    strip1 = scaled(floor, 1)
    strip2 = scaled(floor, 3)
    W = min(2400, max(max(b.size[0] for b in blocks), strip2.size[0]) + 20)
    H = sum(b.size[1] + 6 for b in blocks) + strip1.size[1] + strip2.size[1] + 90
    out = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(out)
    y = 8
    d.text((10, y), 'Boss Merchants: finished art (palette-locked) at 4x, idols 1x / 8x, vanilla for comparison',
           fill=(255, 235, 170, 255), font=fb); y += 24
    for b in blocks:
        out.alpha_composite(b, (10, y)); y += b.size[1] + 6
        d.line([10, y - 3, W - 10, y - 3], fill=(60, 56, 70, 255))
    d.text((10, y + 2), 'all 8 + vanilla on a stone floor at 1x (and 3x)', fill=(240, 220, 150, 255), font=fb); y += 22
    out.alpha_composite(strip1, (10, y)); y += strip1.size[1] + 8
    if strip2.size[0] + 20 <= W:
        out.alpha_composite(strip2, (10, y))
    out.save(os.path.join(PREV, 'review_all.png'))


def trim(im):
    """Crop transparent columns (keep rows so ground lines stay comparable at the bottom)."""
    a = np.array(im)[..., 3]
    cols = np.where(a.max(0) > 0)[0]
    rows = np.where(a.max(1) > 0)[0]
    if len(cols) == 0:
        return im
    return im.crop((cols[0], 0, cols[-1] + 1, rows[-1] + 1))


def lineup(done):
    f = font(12)
    chars, idols = [], []
    for key, sj, results, idol in done:
        t, i = RIG_FRAMES[sj['rig']][0]
        chars.append((key, composite_frame(results, sj['rig'], t, i, 1)))
        idols.append((key, Image.fromarray(idol['icon'][1], 'RGBA')))
    van = vanilla_idles()
    vi = [(n.split('_', 1)[1][:9], Image.open(os.path.join(REF_I, n + '_icon.png')).convert('RGBA')) for n in VANILLA_IDOLS]
    c_items = chars + [('v:' + l, im) for l, im in van]
    i_items = idols + [('v:' + l, im) for l, im in vi]
    parts = [('merchants idle, finished + vanilla (v:) at 1x', strip(c_items, 1, 6, f, label=False)),
             ('merchants idle at 4x', strip(c_items, 4, 10, f)),
             ('idols, finished + vanilla at 1x', strip(i_items, 1, 6, f, label=False)),
             ('idols at 8x', strip(i_items, 8, 12, f))]
    stack(parts, font(14)).save(os.path.join(PREV, 'final_lineup.png'))


# ----------------------------------------------------------------------------- main
def main():
    args = sys.argv[1:]
    only = None
    if '--only' in args:
        only = args[args.index('--only') + 1].split(',')
    keys = [k for k in KEYS if (only is None or k in only)]
    pal = build_palette('--rebuild-palette' in args)
    P, G = Palette(pal['main']), Palette(pal['glow'])
    outlines = OutlineSet(pal, P)
    vm = vanilla_merchant_images()
    ref_m = region_stats([im for _, _, im in vm])
    ref_i = region_stats(vanilla_idol_images())
    # vanilla colours-per-region-per-cell (sparsity reference)
    vcols = np.unique(np.concatenate([unique_colours(im)[0] for _, _, im in vm]), axis=0)
    vrid = cluster_regions(vcols, np.ones(len(vcols), np.int64))
    vrid_of = {ckey(c): int(r) for c, r in zip(vcols, vrid)}
    ref_cpr = float(np.mean([colours_per_region_cell(im, vanilla_cells(rig, tex, im), vrid_of) for rig, tex, im in vm]))
    errors, log, done = [], {}, []
    for k in keys:
        if not os.path.exists(os.path.join(SRC_M, k, 'sheets.json')):
            errors.append(f'{k}: no sheets.json'); continue
        sj, results, idol = process_key(k, P, G, outlines, ref_m, ref_i, ref_cpr, errors, log)
        if idol is None:
            continue
        done.append((k, sj, results, idol))
        s = log[k]
        print(f'{k:12s} sheet L/C/Cp90 {s["sheet_before"][0]:.3f}/{s["sheet_before"][1]:.3f}/{s["sheet_before"][2]:.3f}'
              f' -> {s["sheet_after"][0]:.3f}/{s["sheet_after"][1]:.3f}/{s["sheet_after"][2]:.3f}'
              f'  idol {s["idol_before"][0]:.3f}/{s["idol_before"][1]:.3f} -> {s["idol_after"][0]:.3f}/{s["idol_after"][1]:.3f}'
              f'  cpr {s["colours_per_region"]:.2f} detail={s["detail_pass"]} speckle={s["removed_px"]}')
    print(f'vanilla merchants   L/C/Cp90 {ref_m["meanL"]:.3f}/{ref_m["meanC"]:.3f}/{ref_m["chroma_p90"]:.3f}'
          f'  idols {ref_i["meanL"]:.3f}/{ref_i["meanC"]:.3f}  cpr {ref_cpr:.2f}  palette {len(P.rgb)} + glow {len(G.rgb)}')
    os.makedirs(CACHE, exist_ok=True)
    with open(os.path.join(CACHE, 'finisher_stats.json'), 'w') as f:
        json.dump({'vanilla_merchants': ref_m, 'vanilla_idols': ref_i, 'vanilla_cpr': ref_cpr, 'keys': log}, f, indent=1)
    if '--no-previews' not in args and done:
        os.makedirs(PREV, exist_ok=True)
        for k, sj, results, idol in done:
            key_preview(k, sj, results, idol)
        lineup(done)
        if len(done) == len(KEYS):
            review_all(done)
    if errors:
        print('VALIDATION FAILED:'); [print('  ' + e) for e in errors]
        sys.exit(1)
    print('ok:', ', '.join(k for k, *_ in done))


if __name__ == '__main__':
    main()
