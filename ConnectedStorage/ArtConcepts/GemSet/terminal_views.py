"""Storage Terminal (clockwork): approved front plus side and back views.

    python views.py

Front grid and palette are copied unchanged from styles/clockwork/make.py.
Renders review.png: the three views at 8x beside vanilla electronicsTable down/side/up,
plus 1x and 3x rows with the ancient-gem chest.
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
ROOT = HERE.parents[1]
DIR_REFS = ROOT / 'style-refs' / 'directional'
CHEST = ROOT / 'styles' / 'ancient-gem' / 'chest.png'

PAL = {
    '.': None,
    # outlines
    'O': '39341f',   # dark olive-brown outline (copper/brass family)
    'Y': '0e2522',   # deepest teal-black (screen frame, shadows)
    # copper ramp
    'o': '5e2a28', 'd': '824223', 'm': 'c16436', 'l': 'e88b69', 'h': 'fca895', 'w': 'ecc1bf',
    # brass ramp
    'b': '614822', 'B': '915b26', 'g': 'b58047', 'G': 'e2a12c',
    # steel ramp
    'S': '2a4e4a', 't': '47677e', 'u': '677fae', 'v': 'a1aedd', 'x': 'd7dcfb',
    # tier core (copper-orange glass) ramp: deep, mid, bright, hot
    'k': '8f3f22', 'c': 'da6210', 'p': 'f39a1d', 'q': 'ffd27a',
    # screen (teal glow) ramp
    'y': '073c31', 'e': '19bdc6', 'E': '4ad8ae', 'W': 'ffffff',
    # red indicator
    'r': 'c76369',
}

# Emissive keys: screen glass, status light and the copper-orange core glass (not plain copper).
GLOW_TERMINAL = 'cpqeEWr'

FRONT = [  # approved, unchanged
    '..OOOOOOOOOOOO..',
    '.OxYYYYYYYYYYxO.',
    '.OvYyEEWyyyyYtO.',
    'OBOYyyyyyyyyYOBO',
    'OgOYyeEeEeyyYObO',
    'OgOYyyyyyyyyYObO',
    'OgOYyEeyyyyyYObO',
    'OGOYYYYYYYYYYObO',
    'OgSmmmlmmmmmmSbO',
    'OOhhhhhhhhhhhhOO',
    'OlgGgllcplgGgdxO',
    'OlGOBmmkcmGOBdvO',
    'OmBbbmlmdmBbbdtO',
    'OmmOmOmOmOmmmdxO',
    'OSooooooooooooSO',
    'OOOOOOOOOOOOOOOO',
]

SIDE = [  # 16x20, facing right (screen edge on the right)
    '................',
    '................',
    '................',
    '....OOOOOOOO....',
    '....OhhhhhxYO...',
    '.OOOOlmmmmYEO...',
    '.OGgOmmmmdYeO...',
    '.OgBOmooodYeO...',
    '.OgBOmmmmdYEO...',
    '.OtSOmooodYyO...',
    '.OgBOmmmmdYeO...',
    '.OgBOdddddYYO...',
    '.OgbOSmmlmmSOO..',
    '.OhhhhhhhhhhhO..',
    '.OlmmmmmmmmmdO..',
    '.OlmgGgmdddmGO..',
    '.OlmGOBmmmmmBO..',
    '.OmmBbbmdddmbO..',
    '.OSoooooooooSO..',
    '.OOOOOOOOOOOOO..',
]

BACK = [  # 16x16, rear of the console
    '..OOOOOOOOOOOO..',
    '.OxhhhhhhhhhhxO.',
    '.OvlmmmmmmmmdtO.',
    'OBOlmmmmmmmmdOBO',
    'OgOmoooooomEdObO',
    'OgOmmmmmmmmmdObO',
    'OgOmoooooommdObO',
    'OGOddddddddddObO',
    'OgSmmmlmmmmmmSbO',
    'OOhhhhhhhhhhhhOO',
    'OxlmmvvvvvvmmmdO',
    'OvmmmtwttwtmmmdO',
    'OtmmmSSSSSSmmmdO',
    'OxmmOmOmOmOmmddO',
    'OSooooooooooooSO',
    'OOOOOOOOOOOOOOOO',
]

TERMINAL = {'': FRONT, '_side': SIDE, '_up': BACK}


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


def draw(grid, emissive=False):
    h, w = len(grid), len(grid[0])
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == w, (y, row, len(row))
        for x, ch in enumerate(row):
            v = PAL[ch]
            if v is None or (emissive and ch not in GLOW_TERMINAL):
                continue
            im.putpixel((x, y), hexc(v))
    return im


def check():
    assert len(FRONT) == 16 and len(BACK) == 16 and len(SIDE) == 20
    for g in TERMINAL.values():
        assert all(len(r) == 16 for r in g)
        assert all(c in PAL for r in g for c in r)


def review():
    bg = (29, 32, 38, 255)
    fg = (220, 220, 220, 255)
    mine = {k: draw(g) for k, g in TERMINAL.items()}
    glow = {k: draw(g, emissive=True) for k, g in TERMINAL.items()}
    vref = {k: Image.open(DIR_REFS / f'electronicsTable{s}.png').convert('RGBA')
            for k, s in (('', '_down'), ('_side', '_side'), ('_up', '_up'))}
    chest = Image.open(CHEST).convert('RGBA')
    s8, col = 8, 16 * 8 + 12
    cols = [('front', mine['']), ('side', mine['_side']), ('back', mine['_up']),
            ('glow side', glow['_side']), ('glow back', glow['_up']),
            ('eTable down', vref['']), ('eTable side', vref['_side']), ('eTable up', vref['_up'])]
    W = 20 + len(cols) * col
    y1 = 26 + 20 * s8 + 34
    y3 = y1 + 20 + 34
    H = y3 + 60 + 20
    img = Image.new('RGBA', (W, H), bg)
    d = ImageDraw.Draw(img)
    x = 20
    for lab, im in cols:
        d.text((x, 8), lab, fill=fg)
        big = im.resize((im.width * s8, im.height * s8), Image.NEAREST)
        img.alpha_composite(big, (x, 26 + (20 * s8 - big.height)))
        x += col
    mix = [chest, mine[''], chest, mine['_side'], chest, mine['_up'], chest,
           vref[''], vref['_side'], vref['_up']]
    d.text((20, y1 - 14), '1x', fill=fg)
    x = 20
    for im in mix:
        img.alpha_composite(im, (x, y1 + (20 - im.height)))
        x += 17
    d.text((20, y3 - 14), '3x', fill=fg)
    x = 20
    for im in mix:
        big = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
        img.alpha_composite(big, (x, y3 + (60 - big.height)))
        x += 16 * 3 + 3
    img.save(HERE / 'review.png')


if __name__ == '__main__':
    check()
    review()
    print('ok')
