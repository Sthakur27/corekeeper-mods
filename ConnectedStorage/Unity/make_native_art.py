"""Native-resolution runtime art: one texel per game pixel, vanilla sprite sizes.

    python ConnectedStorage/Unity/make_native_art.py

Sizes follow the vanilla references (chestCopper, workbenchWood/Copper, electronicsTable):
storage units 16x16 front/back and 16x19 side; terminal and workbench 16x16 front/back and
16x20 side. Every pixel is drawn here from palette-keyed grids (original art, Runic Kiln
designs redrawn at native density); no game textures are copied. Tiers are palette swaps.
Emissive masks mark vent, screen and crystal light.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).parent
# Archived Runic Kiln set (superseded by make_gem_art.py); kept reproducible, no longer packaged.
ART = ROOT.parent / 'ArtConcepts' / 'RunicKilnNative'
REFS = ROOT.parents[1] / 'build/connectedstorage/art-reference'


def hexc(v):
    return tuple(bytes.fromhex(v)) + (255,)


# Storage unit body keys: O outline, D dark, M mid, L light, H highlight;
# k/n/r metal dark/mid/rivet; a/w vent glow and core.
TIERS = {
    'Copper':    dict(O='4a2219', D='8f3f22', M='c9683f', L='ee916a', H='ffb697', k='3c3436', n='6e6a72', r='c1c7d6', a='f39a1d', w='ffe18c'),
    'Tin':       dict(O='2e3a4c', D='677d99', M='9fb3cc', L='c8d8ea', H='f2f8ff', k='4a5260', n='9aa3b0', r='f0f4f8', a='5fc8ff', w='c8f2ff'),
    'Iron':      dict(O='1c1e23', D='3d4149', M='5d626c', L='7f848e', H='a9adb5', k='2a2c31', n='8d929b', r='d5d9e0', a='f0a030', w='ffe0a0'),
    'Scarlet':   dict(O='3a0c14', D='741a26', M='b02a35', L='e04a4a', H='ff9a8c', k='2e2a2e', n='6a6470', r='d8d8e0', a='ff3048', w='ffb0b8'),
    'Octarine':  dict(O='0a2e28', D='136352', M='21987a', L='3fc9a0', H='9ff0d0', k='25302e', n='627070', r='cfe0dc', a='6dffb0', w='d8ffe8'),
    'Galaxite':  dict(O='220f40', D='4a2585', M='7442c0', L='a070e8', H='d8b8ff', k='2c2638', n='6e6680', r='dcd4ec', a='c070ff', w='f0d8ff'),
    'Solarite':  dict(O='4a2e08', D='9a6510', M='d99a20', L='f5c84a', H='fff0a0', k='3e2c18', n='8a6a3a', r='f0dcb0', a='fff070', w='fffbe0'),
    'Pandorium': dict(O='0c1240', D='1e2c88', M='3248c8', L='5a78f0', H='a8b8ff', k='2a2e40', n='707890', r='dce0f0', a='40a8ff', w='c0e8ff'),
}

UNIT = {
    '': [  # front (down): lid with inset panel, metal seam, three glowing vents, feet
        '..OOOOOOOOOOOO..',
        '.OrHHHHHHHHHHrO.',
        'OnLLDDDDDDDDLLnO',
        'OnLDMMHHHHMMDLnO',
        'OnLDMMMMMMMMDLnO',
        'OnLLDDDDDDDDLLnO',
        'OnMMMMMMMMMMMMnO',
        'OrnnnnnnnnnnnnrO',
        'OkkkkkkkkkkkkkkO',
        'OkHLLLLLLLLLLMkO',
        'OkLMOOMOOMOOMDkO',
        'OrLMawMawMawMDrO',
        'OkMMaaMaaMaaDDkO',
        'OkDDDDDDDDDDDDkO',
        '.OknOOOOOOOOnkO.',
        '.OOOO......OOOO.',
    ],
    '_side': [  # longer lid, one vent on the narrow face
        '..OOOOOOOOOOOO..',
        '.OrHHHHHHHHHHrO.',
        'OnLLLLLLLLLLLLnO',
        'OnLLDDDDDDDDLLnO',
        'OnLDMMHHHHMMDLnO',
        'OnLDMMMMMMMMDLnO',
        'OnLDMMMMMMMMDLnO',
        'OnLLDDDDDDDDLLnO',
        'OnLLLLLLLLLLLLnO',
        'OnMMMMMMMMMMMMnO',
        'OrnnnnnnnnnnnnrO',
        'OkkkkkkkkkkkkkkO',
        'OkHLLLLLLLLLLMkO',
        'OkLMMMMOOMMMMDkO',
        'OrLMMMMawMMMMDrO',
        'OkMMMMMaaMMMMDkO',
        'OkDDDDDDDDDDDDkO',
        '.OknOOOOOOOOnkO.',
        '.OOOO......OOOO.',
    ],
    '_up': [  # back: same lid, hinge strap instead of vents
        '..OOOOOOOOOOOO..',
        '.OrHHHHHHHHHHrO.',
        'OnLLDDDDDDDDLLnO',
        'OnLDMMMMMMMMDLnO',
        'OnLDMMHHHHMMDLnO',
        'OnLLDDDDDDDDLLnO',
        'OnMMMMMMMMMMMMnO',
        'OrnnnnnnnnnnnnrO',
        'OkkkkkkkkkkkkkkO',
        'OkHLLLLLLLLLLMkO',
        'OkLMnnnnnnnnMDkO',
        'OrLkrkkkkkkrkDrO',
        'OkMMMMMMMMMMDDkO',
        'OkDDDDDDDDDDDDkO',
        '.OknOOOOOOOOnkO.',
        '.OOOO......OOOO.',
    ],
}

# Terminal: ivory console, rune screen on the top surface, teal dial and brass levers.
TERMINAL_PAL = dict(O='3a3026', D='8a7a5e', M='c9b58e', L='ebdbb9', H='fff2d9', k='3c3436', n='6e6a72',
                    r='c1c7d6', s='0f3b38', a='2fb894', w='b9ffe0', g='a8701c', G='ffd36a')
TERMINAL = {
    '': [
        '..OOOOOOOOOOOO..',
        '.OnHHHHHHHHHHnO.',
        'OnLOOOOOOOOOOLnO',
        'OnLOssawssssOLnO',
        'OnLOsawwsasaOLnO',
        'OnLOssasssawOLnO',
        'OnLOOOOOOOOOOLnO',
        'OnMMMMMMMMMMMMnO',
        'OrnnnnnnnnnnnnrO',
        'OkkkkkkkkkkkkkkO',
        'OkHLGLLawLLGLMkO',
        'OrLMgMMaaMMgMDrO',
        'OkDDDDDDDDDDDDkO',
        '.OknOOOOOOOOnkO.',
        '.OknO......OnkO.',
        '.OOOO......OOOO.',
    ],
    '_side': [
        '...OOOOOOOOOO...',
        '..OnHHHHHHHHnO..',
        '.OnLLLLLLLLLLnO.',
        '.OnLOOOOOOOOLnO.',
        '.OnLOssawsssOLnO.'[:16],
        '.OnLOsawwsasOLnO',
        '.OnLOssasssaOLnO',
        '.OnLOsasawssOLnO',
        '.OnLOssssawsOLnO',
        '.OnLOOOOOOOOLnO.',
        '.OnLLLLLLLLLLnO.',
        '.OnMMMMMMMMMMnO.',
        '.OrnnnnnnnnnnrO.',
        '.OkkkkkkkkkkkkO.',
        '.OkHLLLGLLLLMkO.',
        '.OrLMMMgMMMMDrO.',
        '.OkDDDDDDDDDDkO.',
        '.OknOOOOOOOOnkO.',
        '.OknO......OnkO.'[:16],
        '.OOOO......OOOO.',
    ],
    '_up': [
        '..OOOOOOOOOOOO..',
        '.OnHHHHHHHHHHnO.',
        'OnLOOOOOOOOOOLnO',
        'OnLOwasssassOLnO',
        'OnLOasasswwaOLnO',
        'OnLOsssswassOLnO',
        'OnLOOOOOOOOOOLnO',
        'OnMMMMMMMMMMMMnO',
        'OrnnnnnnnnnnnnrO',
        'OkkkkkkkkkkkkkkO',
        'OkHLLkLLLLkLLMkO',
        'OrLMMkMMMMkMMDrO',
        'OkDDDDDDDDDDDDkO',
        '.OknOOOOOOOOnkO.',
        '.OknO......OnkO.',
        '.OOOO......OOOO.',
    ],
}

# Workbench: warm timber top with iron corner braces, terracotta crucible, teal crystal in a jig.
BENCH_PAL = dict(O='3b2414', D='6b3a1c', M='a85a2c', L='d07a3c', H='f0a060', k='34303a', n='6a6470',
                 r='a8a4b0', b='c88a2a', B='ffd36a', t='7a2a14', T='c4552a', U='f08a50', x='3a1408',
                 c='1a8a8a', C='4ad8c0', W='c8fff0')
WORKBENCH = {
    '': [
        '.OOOOOOOOOOOOOO.',
        'OkkLLLLLLLLWLkkO',
        'OktUUUtLLLCWCLkO',
        'OLtxxxtHHHcCCHLO',
        'OLtTTTtMMMccCMLO',
        'OLMtttMMMknnnkMO',
        'ODDDDDDDDDDDDDDO',
        'OkLLHHLLLLLLLLkO',
        'OkMMMMMMMMMMMMkO',
        'ODDDDDDDDDDDDDDO',
        'OkDDDDbBBbDDDDkO',
        'OOOOOOOOOOOOOOOO',
        '.OkMO......OMkO.',
        '.OkMO......OMkO.',
        '.OkMODDDDDDOMkO.',
        '.OOOO......OOOO.',
    ],
    '_side': [
        '................',
        '.......OO.......',
        '..OOOOOWCOOOOO..',
        '.OkkLLcWCLLLkkO.',
        '.OkLLLcCCLLLLkO.',
        '.OLHHkncnkHHHLO.',
        '.OLMMMMMMMMMMLO.',
        '.ODDDDDDDDDDDDO.',
        '.OLLLtUUUtLLLLO.',
        '.OLHHtxxxtHHHLO.',
        '.OLMMtTTTtMMMLO.',
        '.OLMMMtttMMMMLO.',
        '.ODDDDDDDDDDDDO.',
        '.OkLLHHLLLLLLkO.',
        '.ODDDDDDDDDDDDO.',
        '.OkDDDDDDDDDDkO.',
        '.OOOOOOOOOOOOOO.',
        '..OkMO....OMkO..',
        '..OkMODDDDOMkO..',
        '..OOOO....OOOO..',
    ],
    '_up': [
        '.OOOOOOOOOOOOOO.',
        'OkkLWLLLLLLLLkkO',
        'OkLCWCLLLtUUUtkO',
        'OLHCCcHHHtxxxtLO',
        'OLMCccMMMtTTTtLO',
        'OMknnnkMMMtttMLO',
        'ODDDDDDDDDDDDDDO',
        'OkLLLLLLLLHHLLkO',
        'OkMMMMMMMMMMMMkO',
        'ODDDDDDDDDDDDDDO',
        'OkDDkDDDDDDkDDkO',
        'OOOOOOOOOOOOOOOO',
        '.OkMO......OMkO.',
        '.OkMO......OMkO.',
        '.OkMODDDDDDOMkO.',
        '.OOOO......OOOO.',
    ],
}

GLOW = set('awCW')
SIZES = {'unit': {'': 16, '_side': 19, '_up': 16}, 'bench': {'': 16, '_side': 20, '_up': 16}}


def mottle(grid, seed):
    """Vanilla materials are mottled, not flat: swap a few body pixels to the neighbouring shade.
    Deterministic, body keys only (outline, metal and light keys untouched)."""
    out = []
    for y, row in enumerate(grid):
        chars = list(row)
        for x, key in enumerate(chars):
            h = ((x * 73856093) ^ (y * 19349663) ^ seed) % 100
            if key == 'L' and h < 16:
                chars[x] = 'M'
            elif key == 'M' and h < 10:
                chars[x] = 'L'
            elif key == 'M' and 90 <= h < 97:
                chars[x] = 'D'
            elif key == 'H' and h < 12:
                chars[x] = 'L'
        out.append(''.join(chars))
    return out


def render(grid, palette, height, seed=7):
    assert len(grid) == height, (len(grid), height)
    grid = mottle(grid, seed)
    im = Image.new('RGBA', (16, height))
    glow = Image.new('RGBA', (16, height))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row)
        for x, key in enumerate(row):
            if key == '.':
                continue
            color = hexc(palette[key])
            im.putpixel((x, y), color)
            if key in GLOW:
                glow.putpixel((x, y), tuple(int(c * .75) for c in color[:3]) + (255,))
    return im, glow


def objects():
    for tier, palette in TIERS.items():
        for suffix, grid in UNIT.items():
            yield tier + 'Chest', suffix, render(grid, palette, SIZES['unit'][suffix])
    for name, grids, palette in (('Terminal', TERMINAL, TERMINAL_PAL), ('Workbench', WORKBENCH, BENCH_PAL)):
        for suffix, grid in grids.items():
            yield name, suffix, render(grid, palette, SIZES['bench'][suffix])


def main():
    made = {}
    for name, suffix, (im, glow) in objects():
        im.save(ART / (name + suffix + '.png'))
        glow.save(ART / (name + suffix + '_emissive.png'))
        made[(name, suffix)] = im
    review(made)
    print(f'{len(made)} views written')


def review(made):
    """Private native comparison in ignored build output (make_art.py writes the shipped preview sheets)."""
    if not (REFS / 'chestCopper.png').exists():
        return
    rows = [
        [('chestCopper', None), ('chestCopper_side', None), ('chestCopper_up', None), ('CopperChest', ''), ('CopperChest', '_side'), ('CopperChest', '_up')],
        [('workbenchWood_down', None), ('workbenchWood_side', None), ('workbenchWood_up', None), ('Workbench', ''), ('Workbench', '_side'), ('Workbench', '_up')],
        [('electronicsTable_down', None), ('electronicsTable_side', None), ('electronicsTable_up', None), ('Terminal', ''), ('Terminal', '_side'), ('Terminal', '_up')],
    ]
    Z = 8
    cmp = Image.new('RGBA', (6 * (16 * Z + 16) + 16, 3 * (20 * Z + 20) + 120), '#2c2f38')
    for r, row in enumerate(rows):
        for c, (name, suffix) in enumerate(row):
            im = Image.open(REFS / (name + '.png')).convert('RGBA') if suffix is None else made[(name, suffix)]
            x, y = 16 + c * (16 * Z + 16), 16 + r * (20 * Z + 20)
            cmp.alpha_composite(im.resize((16 * Z, im.height * Z), Image.Resampling.NEAREST), (x, y + (20 - im.height) * Z))
    # 1x strip as it reads in game: native, ours, native, ours...
    strip = ['workbenchWood_down', ('Workbench', ''), 'chestCopper', ('CopperChest', ''), 'electronicsTable_down', ('Terminal', ''), ('PandoriumChest', ''), ('TinChest', '')]
    for i, item in enumerate(strip):
        im = Image.open(REFS / (item + '.png')).convert('RGBA') if isinstance(item, str) else made[item]
        cmp.alpha_composite(im.resize((48, 48), Image.Resampling.NEAREST), (16 + i * 50, cmp.height - 64))
    cmp.save(REFS / 'native-redraw-comparison.png')


if __name__ == '__main__':
    main()
