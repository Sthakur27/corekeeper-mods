"""Boss Merchants sprites (ALPHA art), derived from vanilla merchant sheets: identical size, frame layout
and alpha mask; new boss-themed palette plus a few colour-anchored detail pixels.

    python -I BossMerchants/Unity/make_merchant_art.py

Reads build/bossmerchants/reference (exported by extract_reference.py) and writes
Art/merchants/<theme>/bossMerchant_<theme>_<vanillaName>.png, Art/merchants/skins.json and
Art/merchants/preview.png. Deterministic; ends with a validation step that every output matches its
reference's size, mode and alpha mask exactly and only uses the theme palette (exit 1 otherwise).

How the recolour works (same idea as SolariteDrill/Unity/make_art.py, generalised to bigger sheets):
every vanilla colour of a base merchant belongs to exactly one material GROUP (outline, robe, metal...).
A theme gives each group a target RAMP of colours taken from the boss / enemy / chest sprites. Both are
sorted by luminance and source rank i maps to ramp index round(i * (len(ramp)-1) / (len(src)-1)), so
shading order is kept, outlines stay the darkest tint of their local colour (Core Keeper has no black
outlines) and no new gradients appear. Unlisted vanilla colours abort the build.
DETAILS are colour-anchored rules ("a pixel of vanilla colour A whose neighbour at (dx, dy) is vanilla
colour B becomes C"), so they move with the animation instead of flickering on a fixed grid, and they
only ever repaint already-opaque pixels.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
REF = ROOT / 'build' / 'bossmerchants' / 'reference'
ART = HERE / 'Art' / 'merchants'


def hx(s):
    return tuple(bytes.fromhex(s.lstrip('#')))


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


# ------------------------------------------------------------------ vanilla material groups per base
BASES = {
    'SlimeMerchant': {
        'sheets': ['Slime_merchant'],
        'groups': {
            'outline': '000000 0b1917 23281e 2a4e4a 47677e',
            'pack': '39341f 5e2a28 614822 915b26 a98963 b58047',
            'body': '677fae 90a0c4 a1aedd d7dcfb ffffff',   # slime body + pickaxe steel (shared ramp)
            'blobs': '142b5c 165dd9 1885d8 19bdc6',
            'hood': 'd4b987 fae6bc',
            'flame': 'cf4200 da6210 e46005 eba320 f2df3a',
        },
    },
    'CavelingMerchant': {
        'sheets': ['Merchant', 'Merchant_Light', 'Merchant_Eye'],
        'groups': {
            'outline': '000000 0b1917 0e2522 23281e 073c31',
            'cloak': '2a4e4a 47677e 677fae a1aedd d7dcfb',
            'wood': '39341f 614822 824223 915b26 b58047',
            'warm': '5e2a28 b43221 c5460a da6210 d29a7c ecc1bf f7eec7',
            'glow': '142b5c 143eab 165dd9 1885d8 19bdc6 4ad8ae 94f7dd',
        },
    },
    'FishingMerchant': {
        'sheets': ['Fisher_merchant'],
        'groups': {
            'outline': '000000 0b1917 0e2522 23281e 073c31 2a4e4a',
            'coat': 'a88b37 c9ab51 e2a12c f2df3a e9d9a2',
            'wood': '39341f 614822 915b26 b58047',
            'blue': '142b5c 143eab 165dd9 1885d8 599dbb 19bdc6',
            'steel': '47677e 677fae a1aedd c0bacf d6dbfa d7dcfb ffffff',
            'pants': '5e2a28 91210b b43221 c16436',
            'green': '0f5630 0a6d27 239029',
        },
    },
    'VoidMerchant': {
        'sheets': ['voidMerchant_idle', 'voidMerchant_move', 'voidMerchant_emote'],
        'groups': {
            'outline': '0f1817 383420',
            'robe': '373446 4b3e6b 6a4388',
            'skin': '474453 50667d 6e7fac',
            'trim': '822e13 b2521b c97f26',
            'eyes': 'bd4b9c',
            'gem': '62b9c5 b0f2dd',
            'orb': '000000 2b215f 5722c2 8539d1',  # emote-only: the void orb, its rim and sparkles
        },
    },
}

# ------------------------------------------------------------------ themes
# Ramps are game colours sampled from the palette references (build/bossmerchants/reference/palette).
THEMES = {
    'morpha': {
        'shop': 'Blocks', 'boss': 'Morpha (SlipperySlimeBoss 3024)', 'base': 'SlimeMerchant',
        'why': 'slime-bodied merchant; the body becomes slippery slime',
        'ramps': {  # slimeBlob_slippery_idle, boss_slime_blue, bossChestMorpha
            'outline': '0b1917 0f2960 142b5c 0b3ab3 143eab',
            'pack': '0f2960 142b5c 143eab 165dd9 1885d8 599dbb',
            'body': '0d86e2 19bdc6 4ad8ae 94f6dc ffffff',
            'blobs': '0b3ab3 143eab 165dd9 1885d8',
            'hood': '599dbb a5cada',
            'flame': '143eab 165dd9 19bdc6 4ad8ae 94f6dc',
        },
        # glossy glint: top-left lit edge of each slime blob
        'details': [('blobs', (0, -1), 'body', '8cfcde')],
    },
    'igneous': {
        'shop': 'Explosives', 'boss': 'Igneous (LavaSlimeBoss 3028)', 'base': 'SlimeMerchant',
        'why': 'same slime merchant as Morpha; molten body + charcoal pack separates it at a glance',
        'ramps': {  # slimeBlob_lava_idle(+_emissive), boss_slime_lava, bossChestIgneous
            'outline': '000000 0b1917 232930 383447 484454',
            'pack': '232930 383447 484454 645d53 8f7b77 a69298',
            'body': 'b43221 da6210 e2a12c f2df3a f7eec7',
            'blobs': '232930 383447 484454 645d53',
            'hood': '8f7b77 a69298',
            'flame': '91210b b43221 da6210 e2a12c f2df3a',
        },
        # lava seams: crust spots get a glowing rim where they touch the molten body below
        'details': [('blobs', (0, 1), 'body', 'da6210')],
    },
    'ivy': {
        'shop': 'Potions', 'boss': 'Ivy (PoisonSlimeBoss 3023)', 'base': 'CavelingMerchant',
        'why': 'robed caveling with glowing flasks reads as a potion seller and keeps it apart from the '
               'two slime merchants',
        'ramps': {  # boss_slime_poison, slimeBlob_poison_idle, bossChestIvy, bossChestRaAkar greens (vines)
            'outline': '0b1917 0b1917 232930 2b215f 1c3b31',
            'cloak': '4e3c6d 723e8a 954d94 c76369 de8e9a',
            'wood': '1c3b31 2b5432 356a2c 4c8d32 71a241',
            'warm': '5e2a28 954d94 c76369 de8e9a ecc1bf fca895 ffffff',
            'glow': '2b5432 356a2c 4c8d32 5ca537 71a241 a3ce4a adcc56',
        },
        'details': [('cloak', (0, -1), 'outline', 'fca895')],  # pink rim light along the hood top
    },
    'omoroth': {
        'shop': 'Decor', 'boss': 'Omoroth (OctopusBoss 3110)', 'base': 'FishingMerchant',
        'why': 'the sea merchant; coat goes Omoroth purple/pink, rod and pants deep sea green',
        'ramps': {  # Octopus_boss_body, omorothTentacle_idle, bossChestOmoroth, OmorothTrophy/Beak icons
            'outline': '000000 0b1917 0e2522 152422 073c31 1c3b31',
            'coat': '4b3e6b 6a4388 895392 d96fa5 ecad97',
            'wood': '1c3b31 2b5432 356a2c 4c8d32',
            'blue': '1c2b5a 2a4e4a 4b708a 539fb6 a5cada aec9d9',
            'steel': '354d4a 50667d 6e7fac 985ca2 aec9d9 c0bacf d7dcfb',
            'pants': '152422 1c3b31 354d4a 4b708a',
            'green': '4e3c6d 723e8a a555a4',
        },
        'details': [('coat', (0, 1), 'outline', '4e3c6d')],  # darker hem shadow on the coat
    },
    'raakar': {
        'shop': 'Eggs', 'boss': 'Ra-Akar (ScarabBoss 3131)', 'base': 'VoidMerchant',
        'why': 'tall robed figure fits a desert priest, and it is the only base merchant drawn with a '
               'SpriteAsset, so real SpriteAssetSkins work on it',
        'ramps': {  # Scarab_boss, bossChestRaAkar, RaAkarTrophy icon, raAkarUrn
            'outline': '0e2522 23281e',
            'robe': '0f5630 0a6d27 239029',
            'skin': '91210b b43221 c16436',
            'trim': 'a88b37 c9ab51 f2df3a',
            'eyes': '19bdc6',
            'gem': '1885d8 94f6dc',
            'orb': 'c97f26 e2a12c f2df3a f7eec7',  # the void orb becomes a golden sun orb
        },
        'details': [('robe', (0, -1), 'trim', 'a3ce4a')],  # carapace sheen under the gold trims
    },
}
ORDER = ['morpha', 'igneous', 'ivy', 'omoroth', 'raakar']


def build_map(base, theme):
    groups = BASES[base]['groups']
    ramps = THEMES[theme]['ramps']
    cmap, group_of = {}, {}
    for g, srcs in groups.items():
        src = sorted({hx(s) for s in srcs.split()}, key=lum)
        ramp = sorted((hx(c) for c in ramps[g].split()), key=lum)
        for i, c in enumerate(src):
            j = round(i * (len(ramp) - 1) / (len(src) - 1)) if len(src) > 1 else len(ramp) // 2
            if c in cmap:
                raise SystemExit(f'{base}: colour #{bytes(c).hex()} is in two groups')
            cmap[c] = ramp[j]
            group_of[c] = g
    return cmap, group_of


def recolour(img, cmap):
    a = np.array(img.convert('RGBA'))
    out = np.zeros_like(a)
    op = a[..., 3] > 0
    seen = set()
    for c in {tuple(int(v) for v in p) for p in a[op][:, :3]}:
        if c not in cmap:
            seen.add(c)
            continue
        m = op & np.all(a[..., :3] == c, axis=-1)
        out[m, :3] = cmap[c]
        out[m, 3] = a[m, 3]
    if seen:
        raise SystemExit('unmapped reference colours: ' + ', '.join('#%02x%02x%02x' % c for c in sorted(seen)))
    return a, out


def apply_details(src, out, group_of, rules):
    """Repaint opaque pixels whose vanilla colour is in group A and whose neighbour at (dx, dy) is in
    group B. Rules are evaluated on the vanilla image, so they are order independent."""
    h, w = src.shape[:2]
    gid = np.full((h, w), '', dtype=object)
    op = src[..., 3] > 0
    for c, g in group_of.items():
        gid[op & np.all(src[..., :3] == c, axis=-1)] = g
    for ga, (dx, dy), gb, col in rules:
        nb = np.full((h, w), '', dtype=object)
        ys, xs = slice(max(0, -dy), h - max(0, dy)), slice(max(0, -dx), w - max(0, dx))
        ys2, xs2 = slice(max(0, dy), h - max(0, -dy)), slice(max(0, dx), w - max(0, -dx))
        nb[ys, xs] = gid[ys2, xs2]
        m = (gid == ga) & (nb == gb) & op
        out[m, :3] = hx(col)
    return out


def out_name(theme, sheet):
    return f'bossMerchant_{theme}_{sheet}.png'


def build():
    ref = json.load(open(REF / 'reference.json'))
    ART.mkdir(parents=True, exist_ok=True)
    pairs, skins = [], {'note': 'ALPHA art. Every output has the exact size, frame layout and alpha mask of '
                                'the vanilla texture it replaces.', 'merchants': {}}
    for theme in ORDER:
        t = THEMES[theme]
        base = t['base']
        cmap, group_of = build_map(base, theme)
        mref = ref['merchants'][base]
        entry = {'shop': t['shop'], 'boss': t['boss'], 'baseMerchant': base, 'baseObjectID': mref['objectID'],
                 'why': t['why'], 'kind': mref['kind'], 'textures': []}
        (ART / theme).mkdir(exist_ok=True)
        for sheet in BASES[base]['sheets']:
            src_path = REF / 'merchants' / base / (sheet + '.png')
            src_img = Image.open(src_path)
            a, out = recolour(src_img, cmap)
            out = apply_details(a, out, group_of, t['details'])
            dst = ART / theme / out_name(theme, sheet)
            Image.fromarray(out, 'RGBA').save(dst)
            pairs.append((src_path, dst, set(cmap.values()) | {hx(r[3]) for r in t['details']}))
            tex = {'output': f'{theme}/{dst.name}', 'replacesTexture': sheet, 'size': list(src_img.size)}
            if mref['kind'] == 'legacy':
                tex['legacySprites'] = mref['sheets'][sheet]  # name, rect (x, y from bottom), pivot, ppu
                tex['renderer'] = next((r['path'] for r in mref['renderers'] if r['texture'] == sheet), None)
            else:
                for sa in mref['spriteAssets']:
                    for an in sa['animations']:
                        if an['sprite']['texture'] == sheet:
                            tex.update(spriteAsset=sa['spriteAsset'], spriteAssetAddress=sa['address'],
                                       animationGuid=an['guid'], slot='m_animations[].m_spriteData.texture',
                                       pivot=an['sprite']['pivot'], srcFrameCount=an['srcFrameCount'],
                                       fps=an['fps'], loop=an['loop'])
            entry['textures'].append(tex)
        if mref['kind'] == 'legacy':
            entry['animator'] = mref.get('animator')
            entry['skinNote'] = ('legacy SpriteRenderer + Animator merchant: no SpriteAsset exists, so a '
                                 'SpriteAssetSkin cannot target it. Swap at runtime by rebuilding each Sprite '
                                 'from the new sheet with the same rect/pivot/ppu (Sprite.Create) and mapping '
                                 'vanilla sprite name -> new sprite on the SpriteRenderers listed.')
        skins['merchants'][theme] = entry
    with open(ART / 'skins.json', 'w') as f:
        json.dump(skins, f, indent=1)
    return pairs, ref


# ------------------------------------------------------------------ preview
def idle_frame(base, ref, folder_fn):
    """Composite frame 0 of every sheet of the base (vanilla or themed via folder_fn)."""
    mref = ref['merchants'][base]
    layers = []
    for sheet in BASES[base]['sheets']:
        im = Image.open(folder_fn(sheet)).convert('RGBA')
        if mref['kind'] == 'legacy':
            sp = mref['sheets'][sheet][0]
            x, y, w, h = (int(v) for v in sp['rect'])
            layers.append(im.crop((x, im.height - y - h, x + w, im.height - y)))
        else:
            if 'idle' not in sheet:
                continue
            n = next(an['srcFrameCount'] for sa in mref['spriteAssets'] for an in sa['animations']
                     if an['sprite']['texture'] == sheet)
            layers.append(im.crop((0, 0, im.width // n, im.height)))
    fr = layers[0].copy()
    for l in layers[1:]:
        fr.alpha_composite(l)
    return fr


def preview(ref, k=6):
    rows = []
    for theme in ORDER:
        base = THEMES[theme]['base']
        van = idle_frame(base, ref, lambda s: REF / 'merchants' / base / (s + '.png'))
        new = idle_frame(base, ref, lambda s: ART / theme / out_name(theme, s))
        bb = van.getbbox()
        bb = (max(0, bb[0] - 2), max(0, bb[1] - 2), min(van.width, bb[2] + 2), min(van.height, bb[3] + 2))
        rows.append((theme, van.crop(bb), new.crop(bb)))
    cw = max(v.width for _, v, _ in rows) * k + 30
    label_w = 230
    W = label_w + 2 * cw + 10
    rh = [max(v.height * k, 60) + 34 for _, v, _ in rows]
    H = 34 + sum(rh)
    sheet = Image.new('RGBA', (W, H), (34, 32, 42, 255))
    d = ImageDraw.Draw(sheet)
    d.text((label_w, 10), 'vanilla', fill=(230, 220, 190))
    d.text((label_w + cw, 10), 'Boss Merchants (alpha)', fill=(230, 220, 190))
    y = 34
    for (theme, van, new), h in zip(rows, rh):
        t = THEMES[theme]
        d.line((10, y - 4, W - 10, y - 4), fill=(60, 58, 72))
        d.text((12, y + 4), f"{t['shop']} merchant", fill=(240, 235, 215))
        d.text((12, y + 20), t['boss'].split(' (')[0] + ' theme', fill=(170, 170, 185))
        d.text((12, y + 36), 'base: ' + t['base'], fill=(130, 130, 150))
        for i, im in enumerate((van, new)):
            big = im.resize((im.width * k, im.height * k), Image.NEAREST)
            sheet.alpha_composite(big, (label_w + i * cw, y))
        y += h
    sheet.convert('RGB').save(ART / 'preview.png')


# ------------------------------------------------------------------ validation
def validate(pairs):
    bad = []
    for ref_path, out, allowed in pairs:
        r = Image.open(ref_path)
        o = Image.open(out)
        if r.size != o.size:
            bad.append(f'{out.name}: size {o.size} != {r.size}')
            continue
        if r.mode != o.mode:
            bad.append(f'{out.name}: mode {o.mode} != {r.mode}')
        ra = np.array(r.convert('RGBA'))[..., 3]
        oa = np.array(o.convert('RGBA'))
        if not np.array_equal(ra, oa[..., 3]):
            bad.append(f'{out.name}: alpha mask differs from {ref_path.name}')
        if not set(np.unique(oa[..., 3])) <= set(np.unique(ra)):
            bad.append(f'{out.name}: alpha values not in reference')
        cols = {tuple(int(v) for v in p[:3]) for p in oa.reshape(-1, 4) if p[3]}
        extra = cols - allowed
        if extra:
            bad.append(f'{out.name}: colours outside palette {sorted(extra)}')
    for b in bad:
        print('FAIL', b)
    if bad:
        sys.exit(1)
    print(f'OK: {len(pairs)} sheets match reference size, mode and alpha mask; palettes respected')


if __name__ == '__main__':
    p, r = build()
    preview(r)
    validate(p)
