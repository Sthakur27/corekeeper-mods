"""Concepts built on vanilla chest sprites: the lock becomes a tier-coloured gem.

    python ConnectedStorage/ArtConcepts/NativeBase/make_native_base.py

Reads the privately extracted vanilla textures (build/connectedstorage/art-reference/chests, see
extract.py there) and writes review images to ignored build/artwork-review/. Outputs contain game
pixels, so they stay out of git; only this script is tracked.

  Ancient  chestAncient: its blue gem lock becomes a rounder 6 px tier gem in a dark socket.
  Gray     chestGray: gold lock removed, a 6 px gem set into the dark crossbar.
"""
from pathlib import Path
from PIL import Image, ImageDraw

REPO = Path(__file__).parents[3]
SRC = REPO / 'build/connectedstorage/art-reference/chests'
OUT = REPO / 'build/artwork-review/2026-10-28/native-base'

# Gem ramps: F highlight, G light, I mid, H dark (same roles as the Ancient lock's four blues).
GEMS = {
    'Copper':    dict(F='ffd2a0', G='f08a3a', I='c2561c', H='7a2c0c'),
    'Tin':       dict(F='ffffff', G='c8dcee', I='8aa4c0', H='4e6680'),
    'Iron':      dict(F='fff0f2', G='d8b0b8', I='a07880', H='5e4248'),
    'Scarlet':   dict(F='ffc0c8', G='f03848', I='b81828', H='6a0a16'),
    'Octarine':  dict(F='c8ffe8', G='3ee0a8', I='18a07a', H='0a5a44'),
    'Galaxite':  dict(F='f0d8ff', G='b070ff', I='7a3ad0', H='3e1880'),
    'Solarite':  dict(F='fff6c0', G='ffd030', I='d09810', H='7a5208'),
    'Pandorium': dict(F='d0e4ff', G='4a8aff', I='2050d0', H='0e2280'),
}
SOCKET = '0e2522'  # vanilla darkest teal, used by both chests for seams
GEM6 = ['.ssss.', 'sFGGIs', 'sGGIHs', 'sGIIHs', 'sIHHHs', '.ssss.']
# Side-view lock pixels (seen at the right edge) recoloured by role.
ANCIENT_SIDE = {'94f7dd': 'F', '1885d8': 'G', '165dd9': 'I', '143eab': 'H'}
GRAY_SIDE = {'ecc1bf': 'F', 'e2a12c': 'G', '915b26': 'I', '39341f': 'H'}


def rgb(v):
    return tuple(bytes.fromhex(v)) + (255,)


def hexof(p):
    return '%02x%02x%02x' % p[:3]


def load(name):
    return Image.open(SRC / (name + '.png')).convert('RGBA')


def stamp(im, shape, x0, y0, gem, glow):
    for dy, row in enumerate(shape):
        for dx, key in enumerate(row):
            if key == '.':
                continue
            color = rgb(SOCKET if key == 's' else gem[key])
            im.putpixel((x0 + dx, y0 + dy), color)
            if key in 'FG':
                glow.putpixel((x0 + dx, y0 + dy), color)


def recolor(im, mapping, gem, glow):
    for y in range(im.height):
        for x in range(im.width):
            p = im.getpixel((x, y))
            if p[3] and hexof(p) in mapping:
                key = mapping[hexof(p)]
                im.putpixel((x, y), rgb(gem[key]))
                if key in 'FG':
                    glow.putpixel((x, y), rgb(gem[key]))


def ancient(gem):
    views = {}
    front = load('chestAncient'); glow = Image.new('RGBA', front.size)
    # Old 4x4 lock occupied x6-9, rows 9-12; the 6x6 socketed gem covers it and sits on the crossbar.
    stamp(front, GEM6, 5, 8, gem, glow)
    views[''] = (front, glow)
    side = load('chestAncient_side'); glow = Image.new('RGBA', side.size)
    recolor(side, ANCIENT_SIDE, gem, glow)
    views['_side'] = (side, glow)
    views['_up'] = (load('chestAncient_up'), Image.new('RGBA', (16, 16)))
    return views


def gray(gem):
    views = {}
    front = load('chestGray'); glow = Image.new('RGBA', front.size)
    # Remove the gold lock (x6-9, rows 11-14) by continuing the surrounding panel pattern.
    fill = {11: '2a4e4a2a4e4a2a4e4a2a4e4a', 12: '47677e2a4e4a2a4e4a47677e',
            13: '677fae677fae677fae677fae', 14: '39728b39728b39728b39728b'}
    for y, colors in fill.items():
        for i in range(4):
            front.putpixel((6 + i, y), rgb(colors[i * 6:i * 6 + 6]))
    stamp(front, GEM6, 5, 7, gem, glow)  # centred on the crossbar (row 9)
    views[''] = (front, glow)
    side = load('chestGray_side'); glow = Image.new('RGBA', side.size)
    recolor(side, GRAY_SIDE, gem, glow)
    views['_side'] = (side, glow)
    views['_up'] = (load('chestGray_up'), Image.new('RGBA', (16, 16)))
    return views


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    made = {}
    for tier, gem in GEMS.items():
        for take, build in (('Ancient', ancient), ('Gray', gray)):
            for suffix, (im, glow) in build(gem).items():
                im.save(OUT / f'{take}_{tier}{suffix}.png')
                glow.save(OUT / f'{take}_{tier}{suffix}_emissive.png')
                made[take, tier, suffix] = im
    Z, cell = 7, 16 * 7 + 14
    sheet = Image.new('RGBA', (14 + cell * 8, 4 * (19 * Z + 30) + 150), '#1d2026')
    d = ImageDraw.Draw(sheet)

    def put(im, col, row, label):
        x, y = 14 + col * cell, 24 + row * (19 * Z + 30)
        d.text((x, y - 14), label, fill='white')
        sheet.alpha_composite(im.resize((16 * Z, im.height * Z), Image.Resampling.NEAREST), (x, y + (19 - im.height) * Z))
    for i, tier in enumerate(GEMS):
        put(made['Ancient', tier, ''], i, 0, 'Ancient ' + tier)
        put(made['Gray', tier, ''], i, 1, 'Gray ' + tier)
    for j, (suffix, label) in enumerate((('', 'front'), ('_side', 'side'), ('_up', 'back'))):
        put(made['Ancient', 'Galaxite', suffix], j, 2, 'Ancient ' + label)
        put(made['Gray', 'Galaxite', suffix], 4 + j, 2, 'Gray ' + label)
    put(load('chestAncient'), 0, 3, 'vanilla Ancient')
    put(load('chestGray'), 1, 3, 'vanilla Gray')
    # As they read in game: a row of placed units at 3x and 1x.
    y = sheet.height - 110
    for row, take in enumerate(('Ancient', 'Gray')):
        for i, tier in enumerate(GEMS):
            im = made[take, tier, '']
            sheet.alpha_composite(im.resize((48, 48), Image.Resampling.NEAREST), (14 + i * 48 + row * 420, y))
            sheet.alpha_composite(im, (14 + i * 16 + row * 420, y + 60))
    sheet.save(OUT / 'review.png')
    print('wrote', OUT / 'review.png')


if __name__ == '__main__':
    main()
