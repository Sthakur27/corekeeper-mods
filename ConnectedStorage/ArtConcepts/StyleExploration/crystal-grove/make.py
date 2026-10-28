"""Crystal Grove theme: Runic Storage chest / terminal / workstation at native 16x16.

    python make.py

Warm vanilla-style wood with moss and glowing crystal clusters. Every pixel is drawn here
from palette-keyed string grids. The chest's tier feature is the crystal cluster on its
front (copper orange for the base tier: swap keys x/c/C/X for later tiers).
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'png'


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


PAL = {
    # wood (vanilla wood bench / carpenter ramp), outline dark olive-brown
    'O': '39341f', 'w': '614822', 'd': '915b26', 'm': 'b58047', 'l': 'd29a7c', 'h': 'ecc1bf',
    # moss
    'g': '0f5630', 'G': '239029', 'M': '5ca537',
    # blue-grey metal (vanilla bench corner caps / tools)
    's': '47677e', 'S': '677fae', 'r': 'a1aedd', 'R': 'd7dcfb',
    # teal/mint crystal ramp
    't': '073c31', 'T': '19bdc6', 'a': '4ad8ae', 'A': '94f7dd', 'W': 'ffffff',
    # copper crystal ramp (tier feature)
    'x': '5e2a28', 'c': 'c5460a', 'C': 'e88b69', 'X': 'fca895',
    # stone / clay pot
    'p': '5e2a28', 'P': 'c16436', 'q': 'e88b69',
}
# glowing keys per sprite (emissive mask)
GLOW = {
    'chest': set('cCXW'),
    'terminal': set('TaAW'),
    'workstation': set('aAW'),
}

CHEST = [
    '.OOOOOOOOOOOOOO.',
    'OMMhllllllllhldO',
    'OGMmlllmllllmldO',
    'OgGmmmmmmmmmmmdO',
    'OwddwdddxdddwdwO',
    'OdmllmlxWxlmmgdO',
    'OdmmmmxXCxmxGMdO',
    'OwddxdxXCxxCxGwO',
    'OOOxXxxXCxCcxgOO',
    'OsRxXCxXCxCcxmRO',
    'OSrxXCcXCcCcxmsO',
    'OsSxcCcccCccxdgO',
    'OsSdxxxxxxxxddGO',
    'OSsmdmdmmdmmdgMO',
    'OssdddwdddwddGsO',
    'OOOOOOOOOOOOOOOO',
]

TERMINAL = [
    '......tt........',
    '.....tWAt.......',
    '.....tAaTt......',
    '..t..tAaTt..t...',
    '.tAt.tAaTt.tAt..',
    '.tATttAaTTttaTt.',
    '..tTtAAaTTtaTt..',
    '.OOtTtAaTTtTtOO.',
    'OhlltttaTTtttlmO',
    'OlmdlllttttlldmO',
    'OMGmmdmmlmdmmdwO',
    'OgGmwmmlmwmmdmwO',
    'OgdmwmdlmwmdmdgO',
    'OdmmwmdmmwmlmgGO',
    'OwddwddddwddwdwO',
    'OOOO.OOOOOO.OOOO',
]

WORKSTATION = [
    'OOOOOOOOOOOOOOOO',
    'OMhllllllltlllhO',
    'OGmlmmmlmtWtmmlO',
    'OgmlmmRlmtAatmdO',
    'OdmlmRrltTAaTtdO',
    'OdmlsrlpqqqPPpdO',
    'OdmwmmmlpqPPpldO',
    'OwwddddddpPpdddO',
    'OOOOOOOOOOOOOOOO',
    'OSmlmmlmmlmmlmSO',
    'OsddwdddwdddgGsO',
    'OOOOOOOOOOOOOOMO',
    '.wmmw......wmmw.',
    '.wdmw......wdmw.',
    '.wddw......wddw.',
    '.OOOO......OOOO.',
]

SPRITES = {'chest': CHEST, 'terminal': TERMINAL, 'workstation': WORKSTATION}


def render(grid, glow_keys):
    assert len(grid) == 16 and all(len(r) == 16 for r in grid), grid
    im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    em = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        for x, k in enumerate(row):
            if k == '.':
                continue
            col = hexc(PAL[k])
            im.putpixel((x, y), col)
            if k in glow_keys:
                em.putpixel((x, y), col)
    return im, em


def review(made):
    Z = 8
    refs = ['chestCopper', 'workbenchWood_down', 'carpenterTable_down', 'loom_down']
    refims = [Image.open(REFS / (r + '.png')).convert('RGBA') for r in refs]
    W = 16 + 7 * (16 * Z + 16)
    H = 16 + 16 * Z + 24 + 16 + 24 + 48 + 16
    bg = (0x1d, 0x20, 0x26, 255)
    out = Image.new('RGBA', (W, H), bg)
    allims = list(made.values()) + refims
    for i, im in enumerate(allims):
        x = 16 + i * (16 * Z + 16)
        out.alpha_composite(im.resize((16 * Z, im.height * Z), Image.Resampling.NEAREST), (x, 16))
    # 1x row: mixed
    order = [refims[0], made['chest'], refims[1], made['workstation'], refims[2], made['terminal'], refims[3],
             made['chest'], made['terminal'], made['workstation']]
    y1 = 16 + 16 * Z + 24
    for i, im in enumerate(order):
        out.alpha_composite(im, (16 + i * 18, y1))
    y3 = y1 + 16 + 24
    for i, im in enumerate(order):
        out.alpha_composite(im.resize((48, 48), Image.Resampling.NEAREST), (16 + i * 50, y3))
    out.save(HERE / 'review.png')


def main():
    made = {}
    for name, grid in SPRITES.items():
        im, em = render(grid, GLOW[name])
        im.save(HERE / f'{name}.png')
        em.save(HERE / f'emissive_{name}.png')
        made[name] = im
    review(made)


if __name__ == '__main__':
    main()
