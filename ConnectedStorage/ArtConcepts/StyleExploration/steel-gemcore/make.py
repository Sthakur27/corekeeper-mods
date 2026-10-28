"""Steel Gemcore: riveted grey steel storage family with a round copper gem as tier feature.

    python make.py      (writes chest/terminal/workstation + emissive masks + review.png)

Every pixel is drawn from the palette-keyed grids below (original art; vanilla hex colours only).
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'png'

PAL = {
    '.': None,
    # steel (vanilla chestGray / workbenchIron ramp)
    'O': '0e2522',  # deepest seam / shadow
    'o': '2a4e4a',  # dark teal outline
    'd': '47677e',  # dark steel
    'm': '677fae',  # mid steel
    'l': 'a1aedd',  # light steel
    'h': 'd7dcfb',  # highlight / rivet
    's': '39728b',  # blue shadow steel
    # copper gem (tier feature) ramp
    'a': '5e2a28',
    'b': '824223',
    'c': 'c16436',
    'e': 'e88b69',
    'f': 'fca895',
    'w': 'ffffff',
    'y': 'e2a12c',  # amber glow
}
GLOW = set('bcefwy')  # glowing keys, only counted inside gem regions (see EMIT masks)

CHEST = [
    'oooooooooooooooo',
    'ohhlhhhhhhhhlhho',
    'ollhllhhllllhlmo',
    'olmdlhlllmllmdmo',
    'ommdmmmmmmmmmddo',
    'olmdllmlllmmmddo',
    'omdhmmmmmmmmhdso',
    'oddddddddddddddo',
    'OOOOOOOOOOOOOOOO',
    'ohlhdoooooodhlmo',
    'olmdobeffebodmdo',
    'olmhoefwfecohmdo',
    'ommdocefecbodddo',
    'omddoacccbaoddso',
    'oddddoooooodddso',
    'oooooooooooooooo',
]
CHEST_E = [
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '.....xxxxxx.....',
    '.....xxxxxx.....',
    '.....xxxxxx.....',
    '.....xxxxxx.....',
    '................',
    '................',
]

TERMINAL = [
    '......oooo......',
    '.....offeco.....',
    '....ofwfecbo....',
    '..oofwyyeccboo..',
    '.ohdefyyecbbdhO.',
    '.olmoeeccbbomdO.',
    '.oldmoccbbomddO.',
    'olhhhmooooomhhlo',
    'oOOOOOOOOOOOOOOo',
    'ohlhllllllllhlmo',
    'olmdoOOOOOOodmdo',
    'olmhOyOyyOyOhmdo',
    'ommdOOOOOOOOdddo',
    'omddoooooooodddo',
    'odddhddddddhddso',
    'oooooooooooooooo',
]
TERMINAL_E = [
    '................',
    '......xxxx......',
    '.....xxxxxx.....',
    '....xxxxxxxx....',
    '....xxxxxxxx....',
    '.....xxxxxx.....',
    '......xxxx......',
    '................',
    '................',
    '................',
    '................',
    '.....x.xx.x.....',
    '................',
    '................',
    '................',
    '................',
]

WORKSTATION = [
    'oooooooooooooooo',
    'ohhhhhhhhoooohho',
    'oloefollohhlmolo',
    'oofwecolohhOmomo',
    'olocbollolmmdodo',
    'oodhhdolomddsolo',
    'oloooollloooolmo',
    'omhlllllllllllmo',
    'oddddddddddddddo',
    'ohllllllllllllmo',
    'olhdOObccbOOdhmo',
    'ommdddddddddddso',
    '.OOOOOOOOOOOOOO.',
    '.olmo......omdo.',
    '.ohdo......omdo.',
    '.oooo......oooo.',
]
WORKSTATION_E = [
    '................',
    '................',
    '...xx...........',
    '..xxxx..........',
    '...xx...........',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
]

SPRITES = {'chest': (CHEST, CHEST_E), 'terminal': (TERMINAL, TERMINAL_E),
           'workstation': (WORKSTATION, WORKSTATION_E)}


def rgba(key):
    v = PAL[key]
    return (0, 0, 0, 0) if v is None else tuple(bytes.fromhex(v)) + (255,)


def render(grid, emask):
    assert len(grid) == 16 and all(len(r) == 16 for r in grid), [len(r) for r in grid]
    assert len(emask) == 16 and all(len(r) == 16 for r in emask)
    im = Image.new('RGBA', (16, 16))
    em = Image.new('RGBA', (16, 16))
    for y, row in enumerate(grid):
        for x, k in enumerate(row):
            c = rgba(k)
            im.putpixel((x, y), c)
            if emask[y][x] == 'x' and c[3]:
                em.putpixel((x, y), c)
    return im, em


def review(made):
    Z = 8
    refs = ['chestGray', 'chestIron', 'workbenchIron_down', 'anvilIron_down', 'electronicsTable_down']
    refims = [Image.open(REFS / (r + '.png')).convert('RGBA') for r in refs]
    mine = [made[k] for k in ('chest', 'terminal', 'workstation')]
    W = 16 + 8 * (16 * Z + 16)
    H = 16 + 16 * Z + 24 + 32 + 24 + 48 + 24
    out = Image.new('RGBA', (W, H), '#1d2026')
    dr = ImageDraw.Draw(out)
    for i, im in enumerate(mine + refims):
        x = 16 + i * (16 * Z + 16)
        out.alpha_composite(im.resize((16 * Z, im.height * Z), Image.Resampling.NEAREST), (x, 16))
        dr.text((x, 16 + 16 * Z + 4), (['chest', 'terminal', 'workstation'] + refs)[i], fill='#aaaaaa')
    y1 = 16 + 16 * Z + 24
    row = [mine[0], refims[0], mine[1], refims[4], mine[2], refims[2], refims[1], refims[3]]
    for i, im in enumerate(row):
        out.alpha_composite(im, (16 + i * 18, y1))
    y3 = y1 + 32
    for i, im in enumerate(row):
        out.alpha_composite(im.resize((48, im.height * 3), Image.Resampling.NEAREST), (16 + i * 50, y3))
    out.save(HERE / 'review.png')


def main():
    made = {}
    for name, (grid, emask) in SPRITES.items():
        im, em = render(grid, emask)
        im.save(HERE / f'{name}.png')
        em.save(HERE / f'emissive_{name}.png')
        made[name] = im
    review(made)


if __name__ == '__main__':
    main()
