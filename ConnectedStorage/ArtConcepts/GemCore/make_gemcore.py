"""Gem Core art concept: grey riveted steel housings, the tier shown by a round glowing gem.

    python ConnectedStorage/ArtConcepts/GemCore/make_gemcore.py

Concept only (not packaged). Native resolution like make_native_art.py: units 16x16 front/back and
16x19 side, terminal 16x16 front/back and 16x20 side. Inspired by the Magic Storage idea of one shared
housing with a coloured core; all pixels are original. Writes concept PNGs and preview.png here and,
when the private vanilla references exist, a comparison sheet in build/artwork-review/.
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
REPO = HERE.parents[2]
REFS = REPO / 'build/connectedstorage/art-reference'
REVIEW = REPO / 'build/artwork-review/2026-10-28'

STEEL = dict(O='1c2027', D='3b424d', M='5e6875', L='8792a0', H='b7c1cd', k='252a32', r='dde3ea',
             s='0f3b38', t='2fb894', u='b9ffe0')
# Gem ramps: d deepest, g dark, G mid, a light, w specular. Colours follow the existing ore tiers.
GEMS = {
    'Copper':    dict(d='6a2a0e', g='a8461a', G='e0782c', a='ffaa60', w='fff0d8'),
    'Tin':       dict(d='3a4a5c', g='6a8098', G='a8c0d8', a='dceaf6', w='ffffff'),
    'Iron':      dict(d='4a3438', g='7a5a60', G='b08a90', a='dcc0c4', w='fff4f6'),
    'Scarlet':   dict(d='5a0a14', g='9a1424', G='e02838', a='ff7080', w='ffd8dc'),
    'Octarine':  dict(d='0a4a3a', g='128a6a', G='2ed0a0', a='90ffd8', w='e8fff6'),
    'Galaxite':  dict(d='2e0e5a', g='5a24a0', G='9a5ae8', a='d0a8ff', w='f6ecff'),
    'Solarite':  dict(d='6a4404', g='b07a08', G='f0c020', a='ffe880', w='fffbe0'),
    'Pandorium': dict(d='0a1a5a', g='1a3ab0', G='3a78ff', a='90c0ff', w='e8f2ff'),
}
TERMINAL_GEM = dict(d='0a3a40', g='12707a', G='2ec8c0', a='a0fff0', w='f0fffc')
GLOW = set('Gawtu')

GEM6 = ['.kkkk.', 'kwaGGk', 'kaGGgk', 'kGGggk', 'kGggdk', '.kkkk.']
GEM8 = ['..kkkk..', '.kwaGGk.', 'kwaGGGgk', 'kaGGGggk', 'kGGGggdk', 'kGGggddk', '.kgggdk.', '..kkkk..']
GEM4 = ['.kk.', 'kaGk', 'kGgk', '.kk.']
ORB7 = ['..kkk..', '.kwaGk.', 'kwaGGgk', 'kaGGggk', 'kGGggdk', '.kggdk.', '..kkk..']

# Take A: steel cube, 6 px gem set in the front face.
A_FRONT = [
    '..OOOOOOOOOOOO..',
    '.OHHHHHHHHHHHHO.',
    'OLrLLLLLLLLLLrMO',
    'OLLDDDDDDDDDDLMO',
    'OLLDMMMMMMMMDLMO',
    'OLLDDDDDDDDDDLMO',
    'OMrMMMMMMMMMMrDO',
    'OkkkkkkkkkkkkkkO',
    'OHrMMMMMMMMMMrDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLrMMMMMMMMMMrDO',
    'ODDDDDDDDDDDDDDO',
    '.OkkOOOOOOOOkkO.',
]
A_SIDE = A_FRONT[:6] + ['OLLDMMMMMMMMDLMO', 'OLLDDDDDDDDDDLMO', 'OLLLLLLLLLLLLLMO'] + A_FRONT[6:]
A_BACK = A_FRONT[:9] + ['OLMDDDDDDDDDDMDO', 'OLMkMkMkMkMkMMDO', 'OLMkMkMkMkMkMMDO', 'OLMDDDDDDDDDDMDO'] + A_FRONT[13:]
# Take B: shorter lid, larger 8 px gem dominating the front.
B_FRONT = [
    '..OOOOOOOOOOOO..',
    '.OHHHHHHHHHHHHO.',
    'OLrLLLLLLLLLLrMO',
    'OLLDDDDDDDDDDLMO',
    'OMrMMMMMMMMMMrDO',
    'OkkkkkkkkkkkkkkO',
    'OHrMMMMMMMMMMrDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLMMMMMMMMMMMMDO',
    'OLrMMMMMMMMMMrDO',
    'ODDDDDDDDDDDDDDO',
    '.OkkOOOOOOOOkkO.',
]
# Terminal: same housing, a large crystal orb held in a cradle on top, rune screen on the front.
T_FRONT = [
    '................', '................', '................', '................', '................',
    '..OOOOOOOOOOOO..',
    '.OHHHHHHHHHHHHO.',
    'OLLDDDDDDDDDDLMO',
    'OLrLLLLLLLLLLrMO',
    'OkkkkkkkkkkkkkkO',
    'OLMOOOOOOOOOOMDO',
    'OLMOtsusttsuOMDO',
    'OLMOOOOOOOOOOMDO',
    'OLrMMMMMMMMMMrDO',
    'ODDDDDDDDDDDDDDO',
    '.OkkOOOOOOOOkkO.',
]
T_SIDE = (['................'] * 5 + ['..OOOOOOOOOOOO..', '.OHHHHHHHHHHHHO.', 'OLLDDDDDDDDDDLMO', 'OLLLLLLLLLLLLLMO',
          'OLLDDDDDDDDDDLMO', 'OLrLLLLLLLLLLrMO', 'OkkkkkkkkkkkkkkO', 'OLMMMMMMMMMMMMDO', 'OLMMMMsuMMMMMMDO',
          'OLMMMMMMMMMMMMDO', 'OLrMMMMMMMMMMrDO', 'ODDDDDDDDDDDDDDO', '.OkkOOOOOOOOkkO.'])
T_SIDE = ['................'] + T_SIDE + ['................'] * (20 - len(T_SIDE) - 1)
T_BACK = T_FRONT[:10] + ['OLMDDDDDDDDDDMDO', 'OLMkMkMkMkMkMMDO', 'OLMDDDDDDDDDDMDO'] + T_FRONT[13:]


def stamp(grid, shape, x, y):
    rows = [list(r) for r in grid]
    for dy, line in enumerate(shape):
        for dx, key in enumerate(line):
            if key != '.':
                rows[y + dy][x + dx] = key
    return [''.join(r) for r in rows]


def mottle(grid, seed=11):
    """Worn steel, not flat fill: a few body pixels drop to the neighbouring shade (deterministic)."""
    swap = {'L': ('M', 18), 'M': ('D', 12), 'H': ('L', 15)}
    out = []
    for y, row in enumerate(grid):
        chars = list(row)
        for x, key in enumerate(chars):
            h = ((x * 73856093) ^ (y * 19349663) ^ seed) % 100
            if key in swap and h < swap[key][1]:
                chars[x] = swap[key][0]
        out.append(''.join(chars))
    return out


def render(grid, gem):
    grid = mottle(grid)
    palette = {**STEEL, **gem}
    im, glow = Image.new('RGBA', (16, len(grid))), Image.new('RGBA', (16, len(grid)))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row)
        for x, key in enumerate(row):
            if key == '.':
                continue
            color = tuple(bytes.fromhex(palette[key])) + (255,)
            im.putpixel((x, y), color)
            if key in GLOW:
                glow.putpixel((x, y), color)
    return im, glow


def views():
    out = {}
    for tier, gem in GEMS.items():
        out['A', tier, ''] = render(stamp(A_FRONT, GEM6, 5, 8), gem)
        out['A', tier, '_side'] = render(stamp(A_SIDE, GEM4, 6, 12), gem)
        out['A', tier, '_up'] = render(A_BACK, gem)
        out['B', tier, ''] = render(stamp(B_FRONT, GEM8, 4, 6), gem)
    orb = lambda g, y: stamp(stamp(g, ORB7, 4, y), ['r.......r', 'k.......k'], 3, y + 4)
    out['T', 'Terminal', ''] = render(orb(T_FRONT, 0), TERMINAL_GEM)
    out['T', 'Terminal', '_side'] = render(orb(T_SIDE, 1), TERMINAL_GEM)
    out['T', 'Terminal', '_up'] = render(orb(T_BACK, 0), TERMINAL_GEM)
    return out


def sheet(made, refs):
    Z, cell = 6, 16 * 6 + 14
    tiers = list(GEMS)
    img = Image.new('RGBA', (14 + cell * 9, 4 * (20 * Z + 30) + 120), '#1d2026')
    d = ImageDraw.Draw(img)

    def put(im, col, row, label=None):
        x, y = 14 + col * cell, 24 + row * (20 * Z + 30)
        if label:
            d.text((x, y - 14), label, fill='white')
        img.alpha_composite(im.resize((16 * Z, im.height * Z), Image.Resampling.NEAREST), (x, y + (20 - im.height) * Z))
    for i, tier in enumerate(tiers):
        put(made['A', tier, ''][0], i, 0, 'A ' + tier)
        put(made['B', tier, ''][0], i, 1, 'B ' + tier)
    for j, s in enumerate(('', '_side', '_up')):
        put(made['A', 'Galaxite', s][0], j, 2, 'A ' + ('front', 'side', 'back')[j])
        put(made['T', 'Terminal', s][0], 4 + j, 2, 'Terminal ' + ('front', 'side', 'back')[j])
    if refs:
        for j, n in enumerate(('chestCopper', 'workbenchWood_down', 'electronicsTable_down')):
            put(Image.open(REFS / (n + '.png')).convert('RGBA'), j, 3, 'vanilla ' + n.split('_')[0])
    # 1x and 3x strips as they read in a dark cave.
    strip = [made['A', t, ''][0] for t in tiers] + [made['T', 'Terminal', ''][0]]
    if refs:
        strip = [Image.open(REFS / 'chestCopper.png').convert('RGBA')] + strip
    for i, im in enumerate(strip):
        img.alpha_composite(im.resize((48, 48), Image.Resampling.NEAREST), (14 + i * 52, img.height - 100))
        img.alpha_composite(im, (14 + i * 20, img.height - 30))
    return img


def main():
    made = views()
    for (take, name, suffix), (im, glow) in made.items():
        stem = f'{take}_{name}{suffix}'
        im.save(HERE / (stem + '.png'))
        glow.save(HERE / (stem + '_emissive.png'))
    sheet(made, False).save(HERE / 'preview.png')
    if (REFS / 'chestCopper.png').exists():
        REVIEW.mkdir(parents=True, exist_ok=True)
        sheet(made, True).save(REVIEW / 'gemcore-vs-vanilla.png')
    print(f'{len(made)} concept views written')


if __name__ == '__main__':
    main()
