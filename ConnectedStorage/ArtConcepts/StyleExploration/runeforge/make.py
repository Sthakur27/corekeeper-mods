"""Runeforge theme: dwarven rune-forged storage (16x16 native sprites).

    python make.py

Dark iron plates + heavy brass/gold banding + dark wood, copper-orange rune glow as the
tier feature. Every pixel is drawn from the palette-keyed grids below (original art).
Writes chest/terminal/workstation.png, emissive_*.png and review.png.
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).parent
REFS = HERE.parents[1] / 'style-refs' / 'png'

PAL = {
    '.': None,
    # dark iron (teal-navy outline like vanilla iron/ancient)
    'O': '0e2522', '1': '2a4e4a', '2': '47677e', '3': '677fae', '4': 'a1aedd', '5': 'd7dcfb',
    # brass / gold banding (dark brown outline like vanilla jewelry bench)
    'o': '39341f', 'b': '614822', 'B': '915b26', 'g': 'b58047', 'G': 'e2a12c', 'y': 'f2df3a', 'w': 'f7eec7',
    # dark wood
    'x': '23281e', 'n': '4a3a22', 'm': '614822', 'M': '7a5530',
    # rune stone + copper rune glow (tier feature)
    'd': '232930', 'k': '383447',
    'r': '824223', 'R': 'da6210', 'Q': 'f39a1d', 'W': 'ffd88c',
}
GLOW = set('rRQW')

CHEST = [
    'OOooOOOOOOOOooOO',
    'O4Gg44444443Gg3O',
    'O3gB34333323gB2O',
    'O3GB22322222GB1O',
    'O2gB11111111gB1O',
    'O2gB43333332gB1O',
    'O2gb22232212gb1O',
    'O1Bb11111111Bb1O',
    'oGggggggggggggBo',
    'OObbOgGGGgbOObbO',
    'O3gB2GkQkRb2gB1O',
    'O3GB2gkWQQb2GB1O',
    'O2gB2gkQQkb1gB1O',
    'O2gB1gkRkkb1gB1O',
    'O1Bb1obbbbo1Bb1O',
    'OOooOOOOOOOOooOO',
]

TERMINAL = [
    '.oooooooooooooo.',
    'oGGggggggggggBBo',
    'oGdkdddRddQddkbo',
    'ogdrdddQdQRdddbo',
    'ogdrdddWQQdddkbo',
    'ogdddddQQddddrbo',
    'ogdkdddQdddddrbo',
    'ogddkddRddddkdbo',
    'obbbbbbbbbbbbbbo',
    'O23333333333221O',
    'oGgggbooooBgggBo',
    'O21G11oQWo11G11O',
    'O21111oRQo11111O',
    'obBBBBgoogBBBBbo',
    'O21O........O11O',
    'OOOO........OOOO',
]

WORKSTATION = [
    'OOooooooooooooOO',
    'oGMMMMMMMMMMMMBo',
    'ommO45554OmWQ1mo',
    'oO34444432OR12mo',
    'omO1222211Om43mo',
    'oMmxxO21Oxmmm21o',
    'ommO23221OmmmgBo',
    'oMmxxxxxxxmmMbbo',
    'omMmmMmmmMmmMmmo',
    'oGggggggggggggBo',
    'obBGBBBBBBBBGBbo',
    '.oxxxxxxxxxxxxo.',
    '.O21O......O21O.',
    '.O21O......O21O.',
    '.O11O......O11O.',
    '.OOOO......OOOO.',
]


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


def render(grid):
    assert len(grid) == 16, len(grid)
    im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    glow = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row, len(row))
        for x, ch in enumerate(row):
            c = PAL[ch]
            if c is None:
                continue
            im.putpixel((x, y), hexc(c))
            if ch in GLOW:
                glow.putpixel((x, y), hexc(c))
    return im, glow


def review(made):
    bg = (0x1d, 0x20, 0x26, 255)
    refs = [Image.open(REFS / f'{n}.png').convert('RGBA') for n in
            ('chestIron', 'railwayForge_down', 'repairStation_down', 'workbenchJewelry_down')]
    big = made + refs
    S, pad = 8, 8
    W = len(big) * (16 * S + pad) + pad
    sheet = Image.new('RGBA', (W, 16 * S + 2 * pad + 3 * 16 + 16 + 3 * pad + 20), bg)
    for i, im in enumerate(big):
        sheet.alpha_composite(im.resize((16 * S, im.height * S), Image.NEAREST), (pad + i * (16 * S + pad), pad))
    row = [refs[0], made[0], refs[1], made[1], refs[3], made[2], refs[2]]
    y1 = 16 * S + 2 * pad
    for i, im in enumerate(row):
        sheet.alpha_composite(im, (pad + i * 17, y1))
    y3 = y1 + 16 + pad
    for i, im in enumerate(row):
        sheet.alpha_composite(im.resize((48, 48), Image.NEAREST), (pad + i * 50, y3))
    sheet.save(HERE / 'review.png')


def main():
    made = []
    for name, grid in (('chest', CHEST), ('terminal', TERMINAL), ('workstation', WORKSTATION)):
        im, glow = render(grid)
        im.save(HERE / f'{name}.png')
        glow.save(HERE / f'emissive_{name}.png')
        made.append(im)
    review(made)


if __name__ == '__main__':
    main()
