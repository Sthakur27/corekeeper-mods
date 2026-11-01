"""Solarite Drill sprites, derived from the vanilla Scarlet Drill sheets the same way the game's own
Tin Drill skin is derived: identical layout and silhouette, new material palette, a few detail pixels.

    python -I SolariteDrill/Unity/make_art.py

Reads build/solaritedrill/reference (exported by extract_reference.py; Solarite palette refs in
reference/solarite) and writes Art/*.png plus Art/preview.png. Deterministic; ends with a validation
step that every output matches its reference's size and alpha mask exactly (exit 1 otherwise).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
REF = ROOT / 'build' / 'solaritedrill' / 'reference'
ART = HERE / 'Art'


def hx(s):
    return tuple(bytes.fromhex(s.lstrip('#')))


# Scarlet material -> Solarite. Target colours are taken from the game's own Solarite workbench / anvil /
# chest / bar sprites (reference/solarite), so the drill sits in the same family as the station set.
PALETTE = {
    # housing (scarlet red ramp -> solarite gold ramp, ordered by luminance)
    '#de8e9a': '#f7eec7',  # top highlight      -> warm white
    '#c76369': '#e0c961',  # highlight          -> pale gold
    '#b43221': '#e2a12c',  # body               -> gold
    '#91210b': '#da7910',  # shade              -> amber/orange
    '#5e2a28': '#915b26',  # edge / deep shade  -> solarite brown
    '#4e3c6d': '#143eab',  # purple vent slot   -> solarite deep blue
    # steel drill bit, gears and ink: kept, as both vanilla tiers keep their neutrals
    '#d7dcfb': '#d7dcfb', '#a1aedd': '#a1aedd', '#677fae': '#677fae',
    '#47677e': '#47677e', '#2a4e4a': '#2a4e4a',
    '#0e2522': '#0e2522', '#23281e': '#23281e',
}
PALETTE = {hx(k): hx(v) for k, v in PALETTE.items()}
GEM = hx('#1885d8')    # solarite blue accent (bright)
GEM_D = hx('#143eab')  # solarite blue accent (deep)
SUN = hx('#f7eec7')    # sun emblem core
SUN_R = hx('#e0c961')  # sun emblem rays

SHEETS = ['drill_active', 'drill_active_side', 'drill_active_up',
          'drill_inactive', 'drill_inactive_side', 'drill_inactive_up',
          'drillBase_idle', 'drillBase_idle_side', 'drillBase_idle_side2', 'drillBase_idle_up']
OUT_NAME = {n: n.replace('drillBase_', 'solariteDrillBase_').replace('drill_', 'solariteDrill_') for n in SHEETS}
TIN_NAME = {n: n.replace('drillBase_', 'drillBaseCrude_').replace('drill_', 'drillCrude_') for n in SHEETS}
ICONS = {'icon_lootsprites_402': ('SolariteDrill_icon', 'icon_lootspritesAnimalUpdate_62'),
         'icon_lootsprites_403': ('SolariteDrill_smallIcon', 'icon_lootspritesAnimalUpdate_63')}


def recolour(img):
    a = np.array(img.convert('RGBA'))
    out = a.copy()
    seen = set()
    for y in range(a.shape[0]):
        for x in range(a.shape[1]):
            if a[y, x, 3] == 0:
                out[y, x] = 0
                continue
            c = tuple(int(v) for v in a[y, x, :3])
            if c not in PALETTE:
                seen.add(c)
                continue
            out[y, x, :3] = PALETTE[c]
            out[y, x, 3] = 255
    if seen:
        raise SystemExit('unmapped reference colours: ' + ', '.join('#%02x%02x%02x' % c for c in sorted(seen)))
    return out


def put(a, x, y, col, only_if=None):
    """Set an opaque pixel (never touches transparent pixels, so the silhouette is preserved)."""
    if a[y, x, 3] == 0:
        return
    if only_if is not None and tuple(a[y, x, :3]) not in only_if:
        return
    a[y, x, :3] = col


SOL = {k: hx(k) for k in ('#e2a12c', '#da7910', '#e0c961', '#f7eec7', '#915b26')}
GOLDS = {SOL['#e2a12c'], SOL['#da7910'], SOL['#e0c961']}


def band_gem(a, x0, y):
    """Front/up housing: the orange collar band 'S H S S S S H S' gets a 2-pixel solarite gem."""
    put(a, x0 + 5, y, GEM, GOLDS)
    put(a, x0 + 6, y, GEM_D, GOLDS)


def side_gem(a, x0):
    """Side housing: the (now deep-blue) vent slot on row 8 gets a lit solarite-blue core.
    A separate gem on the panel was tried first; together with the slot it read as a face."""
    put(a, x0 + 4, 8, GEM, {GEM_D})
    put(a, x0 + 5, 8, GEM, {GEM_D})


def sun(a, cx, cy):
    """Small sun emblem centred on (cx, cy)-(cx+1, cy+1) for the base plate."""
    for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
        put(a, cx + dx, cy + dy, SUN, GOLDS)
    for dx, dy in ((0, -1), (1, -1), (0, 2), (1, 2), (-1, 0), (-1, 1), (2, 0), (2, 1)):
        put(a, cx + dx, cy + dy, SUN_R, GOLDS)
    for dx, dy in ((-1, -1), (2, -1), (-1, 2), (2, 2)):
        put(a, cx + dx, cy + dy, SOL['#da7910'], GOLDS)


DETAILS = {
    'drill_active': lambda a: [band_gem(a, f * 12, 5) for f in range(3)],
    'drill_inactive': lambda a: band_gem(a, 0, 5),
    'drill_active_up': None,  # filled below once we know the row (same collar as inactive_up)
    'drill_inactive_up': lambda a: band_gem(a, 0, 14),
    'drill_active_side': lambda a: [side_gem(a, f * 21) for f in range(3)],
    'drill_inactive_side': lambda a: side_gem(a, 0),
    'drillBase_idle': lambda a: sun(a, 7, 6),
    'drillBase_idle_up': lambda a: sun(a, 7, 7),
    'drillBase_idle_side': lambda a: sun(a, 6, 6),
    'drillBase_idle_side2': lambda a: sun(a, 7, 6),
}


def active_up(a):
    # Frames are 12 px wide; the collar row is the 'SDSHSSSSHSDS' row, found per frame.
    target = [SOL['#da7910'], SOL['#915b26'], SOL['#da7910'], SOL['#e0c961']]
    for f in range(a.shape[1] // 12):
        for y in range(a.shape[0]):
            if [tuple(a[y, f * 12 + i, :3]) for i in range(4)] == target and a[y, f * 12, 3]:
                band_gem(a, f * 12, y)
                break


DETAILS['drill_active_up'] = active_up


def icon_big(a):
    # Same lit vent as the placed side view (icon slot is row 7, cols 4-7).
    put(a, 5, 7, GEM, {GEM_D})
    put(a, 6, 7, GEM, {GEM_D})


DETAILS['icon_lootsprites_402'] = icon_big
DETAILS['icon_lootsprites_403'] = lambda a: put(a, 7, 6, GEM, GOLDS)


def save(arr, ref_img, path):
    img = Image.fromarray(arr, 'RGBA')
    if ref_img.mode == 'RGB':  # base plates ship as opaque RGB in vanilla; keep that
        img = img.convert('RGB')
    img.save(path)


def build():
    ART.mkdir(exist_ok=True)
    pairs = []  # (reference name, output path, tin name)
    for n in SHEETS:
        pairs.append((n, ART / (OUT_NAME[n] + '.png'), TIN_NAME[n]))
    for n, (o, t) in ICONS.items():
        pairs.append((n, ART / (o + '.png'), t))
    for ref, out, _ in pairs:
        src = Image.open(REF / (ref + '.png'))
        a = recolour(src)
        fn = DETAILS.get(ref)
        if fn:
            fn(a)
        save(a, src, out)
    return pairs


# ---------------------------------------------------------------- preview
def cave_bg(w, h, seed=7):
    rng = np.random.RandomState(seed)
    bg = np.zeros((h, w, 3), np.uint8)
    base = np.array([28, 26, 34])
    tile = 24
    for ty in range(0, h, tile):
        for tx in range(0, w, tile):
            v = rng.randint(-5, 6)
            bg[ty:ty + tile, tx:tx + tile] = np.clip(base + v + [0, 0, rng.randint(0, 4)], 0, 255)
            bg[ty:ty + 1, tx:tx + tile] = np.clip(base - 8, 0, 255)
            bg[ty:ty + tile, tx:tx + 1] = np.clip(base - 8, 0, 255)
    return Image.fromarray(bg, 'RGB').convert('RGBA')


def preview(pairs, k=6):
    from PIL import ImageDraw
    cols = ('Tin', 'Scarlet', 'Solarite')
    rows = []
    for ref, out, tin in pairs:
        ims = [Image.open(REF / (tin + '.png')).convert('RGBA'), Image.open(REF / (ref + '.png')).convert('RGBA'),
               Image.open(out).convert('RGBA')]
        rows.append((out.stem, ims))
    cw = max(im.width for _, ims in rows for im in ims) * k + 40
    rh = [max(im.height for im in ims) * k + 30 for _, ims in rows]
    one_w = max(im.width for _, ims in rows for im in ims) + 8
    W = 3 * cw + 3 * one_w + 40
    H = 30 + sum(rh) + 10
    sheet = cave_bg(W, H)
    d = ImageDraw.Draw(sheet)
    for i, c in enumerate(cols):
        d.text((20 + i * cw, 8), c, fill=(230, 220, 190))
    d.text((3 * cw + 20, 8), '1x', fill=(230, 220, 190))
    y = 30
    for (name, ims), h in zip(rows, rh):
        d.text((20, y), name, fill=(150, 150, 165))
        for i, im in enumerate(ims):
            sheet.alpha_composite(im.resize((im.width * k, im.height * k), Image.NEAREST), (20 + i * cw, y + 14))
            sheet.alpha_composite(im, (3 * cw + 20 + i * one_w, y + 14))
        y += h
    sheet.convert('RGB').save(ART / 'preview.png')


# ---------------------------------------------------------------- validation
def validate(pairs):
    allowed = set(PALETTE.values()) | {GEM, GEM_D, SUN, SUN_R}
    bad = []
    for ref, out, _ in pairs:
        r = Image.open(REF / (ref + '.png'))
        o = Image.open(out)
        if r.size != o.size:
            bad.append(f'{out.name}: size {o.size} != {r.size}')
            continue
        if r.mode != o.mode:
            bad.append(f'{out.name}: mode {o.mode} != {r.mode}')
        ra = np.array(r.convert('RGBA'))[..., 3]
        oa = np.array(o.convert('RGBA'))
        if not np.array_equal(ra, oa[..., 3]):
            bad.append(f'{out.name}: alpha mask differs from {ref}')
        if not set(np.unique(oa[..., 3])) <= {0, 255}:
            bad.append(f'{out.name}: non-binary alpha')
        cols = {tuple(int(v) for v in p[:3]) for p in oa.reshape(-1, 4) if p[3]}
        extra = cols - allowed
        if extra:
            bad.append(f'{out.name}: colours outside palette {sorted(extra)}')
    for b in bad:
        print('FAIL', b)
    if bad:
        sys.exit(1)
    print(f'OK: {len(pairs)} sprites match reference size + alpha; palette {len(allowed)} colours')


if __name__ == '__main__':
    p = build()
    preview(p)
    validate(p)
