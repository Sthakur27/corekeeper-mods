"""Ancient-gem Runic Storage: chest + workstation in front / side / back views.

Fronts are the approved grids from styles/ancient-gem/make.py (unchanged, pixel for pixel).
    python views.py     (writes review.png next to this file)
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'directional'

PAL = {
    '.': None,
    'O': '073c31',  # deep teal outline
    'D': '2a4e4a',  # dark teal-grey
    'd': '47677e',  # shadow blue-grey
    'M': '677fae',  # mid blue-grey
    'L': 'a1aedd',  # light
    'H': 'c0bacf',  # pale
    'W': 'd7dcfb',  # highlight
    'k': '0e2522',  # socket black-teal
    # copper tier gem (base tier; later tiers recolour 1-6)
    '1': '5e2a28', '2': '824223', '3': 'c16436', '4': 'e88b69', '5': 'fca895', '6': 'ffe4d6',
    # teal core glow
    'T': '1a7f86', 't': '19bdc6', 'g': '4ad8ae', 'G': '94f7dd', 'w': 'eafff8',
}

GLOW_CHEST = '3456T'
GLOW_WORKSTATION = '3456T'

CHEST_FRONT = [
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

# 16x19. Rotated 90 degrees: back (hinges) to the left, front (latch plate + gem) to the right.
CHEST_SIDE = [
    '....OOOOOO......',
    '..OOWHHHHdOO....',
    '.OWHLLLLLLLdO...',
    'OWHLLLLMLLLLdO..',
    'OHLLLdDDdLLLdO..',
    'OHLMdDTTDdLLdO..',
    'OHLLLdDDdLLMdO..',
    'OHLMLLLLLLLLdO..',
    'OHLLLLMLLLLLdO..',
    'OLMMMMdMMMMMdO..',
    'OdddddddddddDO..',
    'OOOOOOOOOOOOOOk.',
    'OWLMMMMMMMMMdO54',
    'OHMMMMMMMMMMdO43',
    'OLMdkHWWHkdMdO32',
    'OLMdMdDDdMdMdOk.',
    'OLHMMMMMMMMLdO..',
    'ODddDddddddDDO..',
    'OOOOOOOOOOOOOO..',
]

# 16x16. Rear of the chest: same lid, continuous seam with two hinges, no plate or gem.
CHEST_BACK = [
    '.OOOOOOOOOOOOOO.',
    'OWHHHHHHHHHHHHdO',
    'OHWLMLLLLLMLLLdO',
    'OHLMLLdDDdLLLLdO',
    'OHLLdDDTTDDdLLdO',
    'OHLLLLdDDdLLLLdO',
    'OHLMLLLLMLLLMLdO',
    'OLMMMMMMMdMMMMdO',
    'OdddddddddddddDO',
    'OOOkHkOOOOkHkOOO',
    'OWLkLkMLLMkLkMdO',
    'OHMMDMMMMMMDMMdO',
    'OLMdMMMMMMMMdMdO',
    'OLHMMMMMMMMMMLdO',
    'ODddDddDDddDddDO',
    'OOOOOOOOOOOOOOOO',
]

WS_FRONT = [
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

# 16x20. Narrow end of the bench: stylus on the far half, gem clamp nearest the viewer.
WS_SIDE = [
    '................',
    '................',
    '..OOOOOOOOOOOO..',
    '..OWHHHHHHHHdO..',
    '..OHLWLLLLLLdO..',
    '..OHLdHLLLLMdO..',
    '..OHLLd3LLLLdO..',
    '..OHLLLd2TLLdO..',
    '..OHLkHWHkLLdO..',
    '..OHLD463DLLdO..',
    '..OHWD342DLLdO..',
    '..OHMkD1DkMMdO..',
    '..OdddddddddDO..',
    '..OLHMMMMMMLdO..',
    '..OdMMMMMMMMDO..',
    '..OOOOOOOOOOOO..',
    '..OHdO....OLdO..',
    '..OLdO....OMdO..',
    '..ODDO....ODDO..',
    '..OOOO....OOOO..',
]

# 16x16. Rear of the bench: tools mirrored, clamp seen from behind, plain apron (no drawer).
WS_BACK = [
    '.OOOOOOOOOOOOOO.',
    'OWHHHHHHHHHHHHdO',
    'OHLMLLLMLLMLLLdO',
    'OHLWLLLLkHWHkLdO',
    'OHLdHLLLD364DLdO',
    'OHWLd3LLD243DLdO',
    'OHLTLd2LDk1kDLdO',
    'OHMMMMdMkDDDkMdO',
    'OdMddddddddddMDO',
    'OLHMMMMMMMMMMLdO',
    'OdMdMMMMMMMMdMDO',
    'OOOOOOOOOOOOOOOO',
    '.OHdO......OLdO.',
    '.OLdO......OMdO.',
    '.ODDO......ODDO.',
    '.OOOO......OOOO.',
]

CHEST = {'': CHEST_FRONT, '_side': CHEST_SIDE, '_up': CHEST_BACK}
WORKSTATION = {'': WS_FRONT, '_side': WS_SIDE, '_up': WS_BACK}


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


def draw(grid, glow_keys=None):
    h, w = len(grid), len(grid[0])
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == w, (y, row, len(row))
        for x, ch in enumerate(row):
            if glow_keys is not None and ch not in glow_keys:
                continue
            c = PAL[ch]
            if c:
                im.putpixel((x, y), hexc(c))
    return im


def check():
    assert len(CHEST_SIDE) == 19 and len(WS_SIDE) == 20
    for g in (CHEST_FRONT, CHEST_BACK, WS_FRONT, WS_BACK):
        assert len(g) == 16
    for g in list(CHEST.values()) + list(WORKSTATION.values()):
        assert all(len(r) == 16 for r in g), g
        assert all(ch in PAL for r in g for ch in r)
    assert not any(ch in '123456' for r in CHEST_BACK for ch in r)


def scale(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def review():
    bg = (29, 32, 38, 255)
    S, pad = 8, 12
    views = ('', '_side', '_up')
    sets = [
        (CHEST, ['chestAncient.png', 'chestAncient_side.png', 'chestAncient_up.png']),
        (WORKSTATION, ['workbenchCopper_down.png', 'workbenchCopper_side.png', 'workbenchCopper_up.png']),
        (None, ['electronicsTable_down.png', 'electronicsTable_side.png', 'electronicsTable_up.png']),
    ]
    rows = []
    for v, refs in sets:
        ours = [draw(v[k]) for k in views] if v else []
        rows.append((ours, [Image.open(REFS / r).convert('RGBA') for r in refs]))
    cell = 16 * S + pad
    gap = pad * 3
    W = pad + 6 * cell + gap
    rowh = 20 * S + pad
    H = pad + len(rows) * rowh + pad + 20 + pad + 2 * (60 + pad) + pad
    out = Image.new('RGBA', (W, H), bg)
    y = pad
    for ours, refs in rows:
        for i, im in enumerate(ours):
            out.alpha_composite(scale(im, S), (pad + i * cell, y + 20 * S - im.height * S))
        for i, im in enumerate(refs):
            out.alpha_composite(scale(im, S), (pad + 3 * cell + gap + i * cell, y + 20 * S - im.height * S))
        y += rowh
    mix = []
    for ours, refs in rows:
        for i in range(3):
            if ours:
                mix.append(ours[i])
            mix.append(refs[i])
    y += pad
    x = pad
    for im in mix:
        out.alpha_composite(im, (x, y + 20 - im.height))
        x += im.width + 4
    y += 20 + pad
    x = pad
    for im in mix:
        if x + im.width * 3 > W - pad:
            x = pad
            y += 60 + pad
        out.alpha_composite(scale(im, 3), (x, y + 60 - im.height * 3))
        x += im.width * 3 + 6
    out.save(HERE / 'review.png')


if __name__ == '__main__':
    check()
    review()
