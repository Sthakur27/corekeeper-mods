"""Clockwork theme: Runic Storage sprites as Core Keeper copper/brass machinery.

    python make.py

Draws chest.png, terminal.png, workstation.png (+ emissive_*.png) from palette-keyed
string grids (one char per pixel), then review.png comparing them with vanilla refs.
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'png'


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


# Shared palette (vanilla hex values where possible).
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

GLOW = set('cpqeEWr')  # keys that are emissive (only where they sit on glass/screens)

CHEST = [
    '.OOOOOOOOOOOOOO.',
    'OxvhhwhhhhwhhvxO',
    'OvtllllllhllmttO',
    'OtlmddddddddmltO',
    'OtlmdxlhllxdmltO',
    'OtlmdlmmmlmdmltO',
    'OtmldmmmmmmdmmtO',
    'OtmmdxddddxdmmtO',
    'OSdmmmlmmmmmmdSO',
    'OOooooOxvOooooOO',
    'OxlmmxccckvmlmxO',
    'OvmlmvcqpctmmmvO',
    'OtmmduppckSddmtO',
    'OxddmtkcckSmddxO',
    'OSoooooOSSoooOSO',
    'OOOOOOOOOOOOOOOO',
]

TERMINAL = [
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

WORKSTATION = [
    'OOOOOOOOOOOOOOOO',
    'OwhuxxxxuhhhwhlO',
    'OhStxvvtSlmGmGmO',
    'OlSlxuluSmGgGgBO',
    'OlSlvtlmSmmGOBmO',
    'OlSvxxvuSmgBgBbO',
    'OlttcpttSmmbmbmO',
    'OmSSSSSSSmmmmmdO',
    'OhhhhhhhhhhhhhlO',
    'OdmmmlmmmmmlmmdO',
    'OOooooobbooooOOO',
    '.OBgOOOOOOOOgBO.',
    '.OBbO......ObBO.',
    '.OgbO......ObgO.',
    '.OBbO......ObBO.',
    '.OOOO......OOOO.',
]

SPRITES = {'chest': CHEST, 'terminal': TERMINAL, 'workstation': WORKSTATION}


def draw(grid, emissive=False):
    assert len(grid) == 16, len(grid)
    im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row, len(row))
        for x, ch in enumerate(row):
            v = PAL[ch]
            if v is None:
                continue
            if emissive and ch not in GLOW:
                continue
            im.putpixel((x, y), hexc(v))
    return im


def save_all():
    out = {}
    for name, grid in SPRITES.items():
        im = draw(grid)
        im.save(HERE / f'{name}.png')
        draw(grid, emissive=True).save(HERE / f'emissive_{name}.png')
        out[name] = im
    return out


def review(mine):
    refs = ['chestCopper', 'workbenchCopper_down', 'electronicsTable_down',
            'automationTable_down', 'glassSmelter']
    vimgs = [Image.open(REFS / f'{r}.png').convert('RGBA') for r in refs]
    bg = (29, 32, 38, 255)
    s8 = 8
    items = list(mine.values()) + vimgs
    labels = list(mine.keys()) + refs
    W = 20 + len(items) * (20 * s8 // 1 + 10)
    H = 30 + 20 * s8 + 30 + 40 + 30 + 20 * 3 + 20
    img = Image.new('RGBA', (W, H), bg)
    d = ImageDraw.Draw(img)
    x = 20
    for im, lab in zip(items, labels):
        d.text((x, 8), lab, fill=(220, 220, 220, 255))
        big = im.resize((im.width * s8, im.height * s8), Image.NEAREST)
        img.alpha_composite(big, (x, 26))
        x += 20 * s8 + 10
    # 1x row mixing
    mix = [vimgs[0], mine['chest'], vimgs[1], mine['workstation'], vimgs[2], mine['terminal'], vimgs[3], vimgs[4]]
    y1 = 26 + 20 * s8 + 20
    d.text((20, y1 - 14), '1x', fill=(220, 220, 220, 255))
    x = 20
    for im in mix:
        img.alpha_composite(im, (x, y1 + (20 - im.height)))
        x += 18
    y3 = y1 + 40
    d.text((20, y3 - 14), '3x', fill=(220, 220, 220, 255))
    x = 20
    for im in mix:
        big = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
        img.alpha_composite(big, (x, y3 + (60 - big.height)))
        x += 16 * 3 + 2
    img.save(HERE / 'review.png')


if __name__ == '__main__':
    mine = save_all()
    review(mine)
    print('ok')
