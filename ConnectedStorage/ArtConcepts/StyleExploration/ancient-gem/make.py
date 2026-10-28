"""ancient-gem theme: Runic Storage chest / terminal / workstation, 16x16 native pixels.

    python make.py      (writes sprites, emissive masks and review.png next to this file)
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'png'

PAL = {
    '.': None,
    # ancient stone/steel (vanilla Ancient chest relationships)
    'O': '073c31',  # deep teal outline
    'D': '2a4e4a',  # dark teal-grey
    'd': '47677e',  # shadow blue-grey
    'M': '677fae',  # mid blue-grey
    'L': 'a1aedd',  # light
    'H': 'c0bacf',  # pale
    'W': 'd7dcfb',  # highlight
    'k': '0e2522',  # socket black-teal
    # copper tier gem (base tier)
    '1': '5e2a28', '2': '824223', '3': 'c16436', '4': 'e88b69', '5': 'fca895', '6': 'ffe4d6',
    # teal core glow
    'T': '1a7f86', 't': '19bdc6', 'g': '4ad8ae', 'G': '94f7dd', 'w': 'eafff8',
}
GLOW = set('3456tgGwT')  # keys that go into emissive masks (per sprite filtered below)

CHEST = [
    '.OOOOOOOOOOOOOO.',
    'OWHHHHHHHHHHHHdO',
    'OHWLLMLLLLLMLLdO',
    'OHLLLLdDDdLLMLdO',
    'OHLMdDDTTDDdLLdO',
    'OHLLLLdDDdLLLLdO',
    'OHLMLLLMLLLLMLdO',
    'OLMMMMdMMMMMMMdO',
    'OdddddddddddddDO',
    'OOOOOkHWWHkOOOOO',
    'OWLMkH4653HkMLdO',
    'OHMMkH3542HkMMdO',
    'OLMdkL2331LkdMdO',
    'OLHMMkLddLkMMLdO',
    'ODddDddkkddDddDO',
    'OOOOOOOOOOOOOOOO',
]
CHEST_GLOW = '3456T'

TERMINAL = [
    '.......OO.......',
    '......OGgO......',
    '.....OGwGtO.....',
    '....OGwGgttO....',
    '..O.OGGggtTO.O..',
    '.OHOOgggttTOOdO.',
    '.OLOkOgtTTOkOdO.',
    'OWHHHkOTTOkHHHMO',
    'OHLLMLkOOkLMLLdO',
    'OdMMdMMMMMMdMMDO',
    'ODddddddddddddDO',
    'OLdkkkkkkkkkkdDO',
    'OLdkgkT44TkgkdDO',
    'OLdkkkkkkkkkkdDO',
    'OHLMMMMMMMMMMMdO',
    'OOOOOOOOOOOOOOOO',
]
TERMINAL_GLOW = 'tgGwT4'

WORKSTATION = [
    '.OOOOOOOOOOOOOO.',
    'OWHHHHHHHHHHHHdO',
    'OHLLLLMLLMLLLMdO',
    'OHLkHWHkLLLLWLdO',
    'OHLD463DLLLHdLdO',
    'OHWD342DLL3dLLdO',
    'OHLDk1kDL2dLTLdO',
    'OHMkDDDkMMMMMMdO',
    'OdMddddddddddMDO',
    'OLHMMMMMMMMMMLdO',
    'OdMMMMkLLkMMMMDO',
    'OOOOOOOOOOOOOOOO',
    '.OHdO......OLdO.',
    '.OLdO......OMdO.',
    '.ODDO......ODDO.',
    '.OOOO......OOOO.',
]
WORKSTATION_GLOW = '3456T'


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


def draw(grid, glow_keys=None):
    assert len(grid) == 16, len(grid)
    im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row, len(row))
        for x, ch in enumerate(row):
            if glow_keys is not None and ch not in glow_keys:
                continue
            c = PAL[ch]
            if c:
                im.putpixel((x, y), hexc(c))
    return im


def scale(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def review(sprites):
    bg = (29, 32, 38, 255)
    refs = ['chestAncient.png', 'electronicsTable_down.png', 'workbenchIron_down.png', 'chestCopper.png']
    ref_ims = [Image.open(REFS / r).convert('RGBA') for r in refs]
    S, pad = 8, 16
    big = list(sprites) + ref_ims
    W = pad + sum(i.width * S + pad for i in big)
    row1_h = max(i.height for i in big) * S
    mix = [ref_ims[0], sprites[0], ref_ims[1], sprites[1], ref_ims[2], sprites[2], ref_ims[3]]
    H = pad + row1_h + pad + 20 + pad + 20 * 3 + pad
    out = Image.new('RGBA', (W, H), bg)
    x = pad
    for i, im in enumerate(big):
        out.alpha_composite(scale(im, S), (x, pad))
        if i == len(sprites) - 1:
            x += pad * 2
        x += im.width * S + pad
    y = pad + row1_h + pad
    x = pad
    for im in mix:
        out.alpha_composite(im, (x, y + 20 - im.height))
        x += im.width + 4
    y += 20 + pad
    x = pad
    for im in mix:
        out.alpha_composite(scale(im, 3), (x, y + 60 - im.height * 3))
        x += im.width * 3 + 6
    out.save(HERE / 'review.png')


def main():
    specs = [('chest', CHEST, CHEST_GLOW), ('terminal', TERMINAL, TERMINAL_GLOW),
             ('workstation', WORKSTATION, WORKSTATION_GLOW)]
    sprites = []
    for name, grid, glow in specs:
        im = draw(grid)
        im.save(HERE / f'{name}.png')
        draw(grid, set(glow)).save(HERE / f'emissive_{name}.png')
        sprites.append(im)
    review(sprites)


if __name__ == '__main__':
    main()
