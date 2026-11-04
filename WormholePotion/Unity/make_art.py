"""Original 16x16 Wormhole Potion icon (hand-placed pixels; no game textures are read).

    python -I WormholePotion/Unity/make_art.py

Styled to sit next to the vanilla potions: no black outline, pale lavender glass lit from the
top-left that darkens to slate and then deep teal along the bottom-right, a white specular glint
on the upper-left of the liquid. Inside, violet liquid holds a teal wormhole spiral that curls
into a white-hot core; the stopper is a violet crystal.
"""
from pathlib import Path
from PIL import Image

ART = Path(__file__).resolve().parent / 'Art'

ROWS = [
    '.......ls.......',
    '......lWvc......',
    '.....hgvcgm.....',
    '......ghhg......',
    '.....dggggd.....',
    '.....mNNNNm.....',
    '....gg5445gg....',
    '...g45544333g...',
    '..g4W54qqt333g..',
    '..g45t3333t33m..',
    '..g3t3TT33t32m..',
    '..m3t33WT3t22m..',
    '..dm3t333t22md..',
    '...dm2ttt22mD...',
    '....ddmmmmmDD...',
    '.....ddddDD.....',
]

PALETTE = {
    # glass (lit top-left, shadowed bottom-right)
    'h': '#d7dcfb', 'H': '#f1f3ff', 'g': '#a1aedd', 'm': '#677fae', 'd': '#036672', 'D': '#073c31',
    'N': '#1d2a52',                      # empty neck interior
    # violet crystal stopper
    'W': '#ffffff', 'l': '#e9c8ff', 'v': '#b06ee8', 's': '#8a4bd0', 'c': '#5a2a9e',
    # violet liquid, light to dark
    '5': '#c88af2', '4': '#9a52dc', '3': '#6f34b8', '2': '#4a2190',
    # teal wormhole spiral
    'q': '#1b8fa8', 't': '#2fc4d0', 'T': '#8ff5e6',
}


def build():
    icon = Image.new('RGBA', (16, 16))
    for y, row in enumerate(ROWS):
        assert len(row) == 16, (y, row)
        for x, ch in enumerate(row):
            if ch != '.':
                icon.putpixel((x, y), tuple(bytes.fromhex(PALETTE[ch][1:])) + (255,))
    return icon


def main():
    ART.mkdir(exist_ok=True)
    icon = build()
    icon.save(ART / 'WormholePotion_icon.png')
    preview = Image.new('RGBA', (320, 320), '#121827')
    preview.alpha_composite(icon.resize((240, 240), Image.Resampling.NEAREST), (40, 40))
    preview.save(ART / 'WormholePotion_inventory_preview.png')
    print('wrote', ART)


if __name__ == '__main__':
    main()
