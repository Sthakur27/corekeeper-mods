"""More NPCs (Alpha) cover (mod id BossMerchants), built from the finished sprite assets on Sid's own grass.

    python release/art_direction/make_bossmerchants_cover.py [--out DIR]

Writes release/bossmerchants_logo.png (960x960 Steam cover) and release/bossmerchants_logo_modio.png (1280x720).
The scene is drawn at game-pixel resolution (16 px tiles) and scaled up by one integer factor with nearest
neighbour, so merchants, idols, floor and shadows share one crisp pixel grid. Each merchant uses its first idle
frame (Caveling rigs: Merchant + Merchant_Light + Merchant_Eye frame 0 stacked like the game's SR/SREye
renderers; the Void Merchant uses frame 0 of voidMerchant_idle, 16x35). Each idol (16x16 icon) sits on the grass
just in front of its merchant, below the feet (never over them).

The floor is real grass from Sid's in-game capture "Desktop Screenshot 2026.11.01 - 17.43.22.67" (HDR, tone-mapped
by hdr_to_png.py to build/artwork-review/hdr/17.43.22.67.png). At 4K the game draws one game pixel as 8x8 screen
pixels (pixel edges at x % 8 == 3, y % 8 == 4), so sampling each cell's centre gives the lawn back at native
resolution with no resampling. The lawn crop (GRASS_BOX, native px; the player, drill and cursor masked out) is
cached in screens/. Its uneven night lighting is flattened per channel, then 16x16 game-pixel blocks of clean grass
are laid on the 16 px tile grid in a seeded order (deterministic) and a slight banded vignette darkens the edges.
Banner and the red plaque reuse make_screenshot_covers.py (same style as every other cover).
"""
from pathlib import Path
import json
import random
import sys

import numpy as np

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402

REPO = covers.REPO
ART = REPO / 'BossMerchants/Unity/Art/final'
TITLE, CAPTION = 'MORE NPCS', 'NEW MERCHANTS FROM BOSS IDOLS'
GRASS_SRC = REPO / 'build/artwork-review/hdr/17.43.22.67.png'
GRASS_BOX = (170, 0, 400, 215)                       # native game pixels in the 480x270 capture
GRASS_CACHE = covers.CACHE / 'hdr_17432267_grass_native_170_0_400_215.png'
GRASS_HOLES = ((58, 108, 112, 140), (135, 150, 155, 168))   # player + drill, mouse cursor (crop coords)
# Back row: the four tall Caveling-rig merchants; front row: the smaller slimes, fisher and void merchant.
ROWS = (('hydras', 'potions', 'farmanimals', 'seeds'), ('blocks', 'decor', 'explosives', 'eggs'))
SEED = 20261111


def frame(path, rect, sheet_h):
    x, y, w, h = map(int, rect)   # Unity sprite rects: origin bottom-left
    return Image.open(path).convert('RGBA').crop((x, sheet_h - y - h, x + w, sheet_h - y))


def merchant(key):
    if key == 'eggs':
        return Image.open(ART / 'merchants/eggs/eggs__voidMerchant_idle.png').convert('RGBA').crop((0, 0, 16, 35))
    d = json.loads((ART / f'merchants/{key}/sheets.json').read_text())
    img = None
    for t in d['textures']:   # Merchant, Merchant_Light, Merchant_Eye: same 64x64 frame grid, drawn in that order
        layer = frame(ART / 'merchants' / t['output'], t['legacySprites'][0]['rect'], t['size'][1])
        img = layer if img is None else Image.alpha_composite(img, layer)
    return img.crop(img.getbbox())


def idol(key):
    im = Image.open(ART / f'idols/{key}_icon.png').convert('RGBA')
    return im.crop(im.getbbox())


def grass_source():
    """The lawn at native game resolution (one sample per 8x8 screen cell), cached in screens/."""
    if not GRASS_CACHE.exists():
        a = np.asarray(Image.open(GRASS_SRC).convert('RGB'))[4 + 4::8, 3 + 4::8]
        x0, y0, x1, y1 = GRASS_BOX
        Image.fromarray(a[y0:y1, x0:x1]).save(GRASS_CACHE)
    return np.asarray(Image.open(GRASS_CACHE).convert('RGB')).astype(float)


def _blur(a, s):
    k = np.exp(-0.5 * (np.arange(-3 * s, 3 * s + 1) / s) ** 2)
    k /= k.sum()
    a = np.apply_along_axis(lambda v: np.convolve(v, k, 'same'), 0, a)
    return np.apply_along_axis(lambda v: np.convolve(v, k, 'same'), 1, a)


def floor(w, h, rng):
    """Grass floor from the screenshot: lighting flattened per channel, 16x16 blocks on the tile grid."""
    n = grass_source()
    r, g, b = n[..., 0], n[..., 1], n[..., 2]
    m = ((g > r * 1.12) & (g > b * 1.05) & (g > 20)).astype('uint8') * 255
    m = np.asarray(Image.fromarray(m).filter(ImageFilter.MedianFilter(5)).filter(ImageFilter.MinFilter(5))) > 0
    for x0, y0, x1, y1 in GRASS_HOLES:
        m[y0:y1, x0:x1] = False
    wgt = m.astype(float)
    den = np.maximum(_blur(wgt, 4), 1e-3)
    lum = n.mean(2)
    lit = m & (lum > np.percentile(lum[m], 70))
    target = [np.median(n[..., c][lit]) * 1.15 for c in range(3)]
    flat = np.stack([n[..., c] * target[c] / np.maximum(_blur(n[..., c] * wgt, 4) / den, 1) for c in range(3)], -1)
    flat = np.clip(flat, 0, 255).astype('uint8')
    B = 16
    clean = m & (flat[..., 1].astype(int) > flat[..., 0].astype(int))   # no red specks or sand bits
    pool = [(x, y) for y in range(0, m.shape[0] - B, 2) for x in range(0, m.shape[1] - B, 2)
            if clean[y:y + B, x:x + B].all()]
    out = np.zeros((h, w, 3), 'uint8')
    for ty in range(0, h, B):
        for tx in range(0, w, B):
            x, y = rng.choice(pool)
            out[ty:ty + B, tx:tx + B] = flat[y:y + B, x:x + B][:h - ty, :w - tx]
    return Image.fromarray(out)


def vignette(img):
    """Slight banded edge darkening on the game-pixel grid, so the merchants pop off the lawn."""
    w, h = img.size
    a = np.asarray(img).astype(float)
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt(((xx + 0.5 - w / 2) / (w / 2)) ** 2 + ((yy + 0.5 - h * 0.58) / (h * 0.62)) ** 2)
    k = 1.0 - 0.42 * np.clip((d - 0.55) / 0.75, 0, 1)
    k = np.round(k * 10) / 10                  # banded falloff, like the game's light steps
    return Image.fromarray(np.clip(a * k[..., None], 0, 255).astype('uint8'))


def shadow(scene, cx, foot, half_w):
    """The game's soft blob shadow: two stepped ellipses at native resolution."""
    sh = Image.new('RGBA', scene.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(sh)
    d.ellipse((cx - half_w - 2, foot - 4, cx + half_w + 2, foot + 3), fill=(0, 0, 0, 70))
    d.ellipse((cx - half_w, foot - 3, cx + half_w, foot + 2), fill=(0, 0, 0, 90))
    scene.alpha_composite(sh)


def plaque(canvas, W, y):
    """Red ALPHA plaque, same construction as covers.wip_tag (the mod ships as More NPCs (Alpha))."""
    P = 4
    tag = Image.new('RGBA', (60, 14), (0, 0, 0, 0))
    t = ImageDraw.Draw(tag)
    t.rectangle((0, 0, 59, 13), fill='#7a1c1c', outline='#e05a4a')
    t.line((1, 1, 58, 1), fill='#a83a30')
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 8)
    layer = Image.new('L', tag.size)
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    s = '   '.join('ALPHA')
    ld.text(((60 - ld.textlength(s, font=f)) / 2, 1), s, font=f, fill=255)
    tag.paste('#ffe8c8', (0, 0), layer)
    tag = tag.resize((tag.width * P, tag.height * P), Image.NEAREST)
    canvas.paste(tag, ((W - tag.width) // 2, y), tag)


def compose(W, H, S, top, row_gap, stagger):
    """W x H canvas at integer scale S; merchant rows start at canvas y=top (canvas pixels)."""
    nw, nh = W // S, H // S
    rng = random.Random(SEED)
    scene_bg = floor(nw, nh, rng)
    cols = len(ROWS[0])
    col_w = nw / (cols + (0.5 if stagger else 0))
    spr = {k: (merchant(k), idol(k)) for row in ROWS for k in row}
    placements = []
    for r, row in enumerate(ROWS):
        foot = top // S + max(spr[k][0].height for k in ROWS[0]) + r * row_gap
        for c, key in enumerate(row):
            cx = round(col_w * (c + 0.5 + (0.5 if stagger and r == 1 else 0)))
            placements.append((key, cx, foot))
    scene = vignette(scene_bg).convert('RGBA')
    for key, cx, foot in placements:   # back row first, so the front row overlaps it
        m, i = spr[key]
        ix, iy = cx + 6 - i.width // 2, foot + 3   # idol on the grass in front of the merchant, below its feet
        shadow(scene, cx, foot, max(6, m.width // 2 - 4))
        scene.alpha_composite(m, (cx - m.width // 2, foot - m.height))
        shadow(scene, ix + i.width // 2, iy + i.height, i.width // 2 - 1)
        scene.alpha_composite(i, (ix, iy))
    canvas = scene.resize((nw * S, nh * S), Image.NEAREST).convert('RGB')
    if canvas.size != (W, H):
        full = Image.new('RGB', (W, H), '#000000')
        full.paste(canvas, ((W - canvas.width) // 2, 0))
        canvas = full
    covers.W, covers.H = W, H        # banner() sizes itself from the module globals
    covers.banner(canvas, TITLE, CAPTION)
    plaque(canvas, W, 186)
    return canvas


def main():
    args = sys.argv[1:]
    out_dir = REPO / 'release'
    if '--out' in args:
        out_dir = Path(args[args.index('--out') + 1])
    out_dir.mkdir(parents=True, exist_ok=True)
    jobs = {'bossmerchants_logo.png': dict(W=960, H=960, S=5, top=255, row_gap=60, stagger=False),
            'bossmerchants_logo_modio.png': dict(W=1280, H=720, S=4, top=236, row_gap=48, stagger=True)}
    for name, kw in jobs.items():
        c = compose(**kw)
        out = out_dir / name
        c.save(out, optimize=True)
        print(out, c.size, out.stat().st_size)


if __name__ == '__main__':
    main()
