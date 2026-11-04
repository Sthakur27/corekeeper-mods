"""Draw the Drill Unit item icons (16x16) into Unity/Art: DrillUnit_icon.png and DrillUnit_smallIcon.png.

    python -I DrillUnit/Unity/make_art.py

The pod seen from the side, bow to the right, like the vanilla drill tools: plated steel hull with an open
cockpit, gold trim band, teal running lights and hover emitters, and a steel spiral drill bit on a gold collar.
Rendered like the vanilla late-game icons (vanilla Speeder Boat Art/vanilla/6581_icon.png, drill tools,
minecart): colours from the game's own item palette (the steel ramp 35,40,30 .. 215,220,251, the gold ramp,
muted steel-blue teal instead of flat saturated cyan), 4-5 step ramps lit from the top-left, tinted dark
outlines (dark olive-steel round metal, brown round gold), small highlights.
`icon` is the full-size inventory icon, `smallIcon` a smaller version of the same object, as in vanilla.
Opaque pixels only (alpha 0 or 255).
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent

PALETTE = {
    '.': None,
    'K': (35, 40, 30),       # outline round metal (vanilla machinery outline)
    'k': (14, 37, 34),       # cockpit interior
    'D': (42, 78, 74),       # steel dark
    'H': (71, 103, 126),     # steel
    'L': (103, 127, 174),    # steel light
    'P': (161, 174, 221),    # steel pale
    'X': (215, 220, 251),    # steel shine
    'b': (57, 52, 31),       # outline round gold
    'y': (145, 91, 38),      # gold dark
    'g': (226, 161, 44),     # gold
    'G': (242, 223, 58),     # gold light
    'e': (20, 62, 80),       # teal dark
    'c': (57, 114, 139),     # teal
    'C': (89, 157, 187),     # teal light
    'E': (165, 202, 218),    # teal glint
}

# Hull; the bit (columns right of the collar) is drawn by add_bit.
ICON = [
    '................',
    '................',
    '................',
    '..KKKKK.........',
    '.KPXPPLK........',
    'KLDkkkkDK.......',
    'KHkkcCkHK.......',
    'KygGGGggy.......',
    'KPXLLLLHD.......',
    'KHEHHDHEH.......',
    'KDHHHDHHD.......',
    'KeCcccCce.......',
    '.KDDDDDDK.......',
    '..KeCEeK........',
    '...KKKK.........',
    '................',
]
ICON_BIT = dict(collar_x=9, rows=(4, 12), cone={10: (4, 12), 11: (4, 12), 12: (5, 11), 13: (6, 10), 14: (7, 9)})

SMALL = [
    '................',
    '................',
    '................',
    '................',
    '................',
    '...KKKK.........',
    '..KPXPLK........',
    '.KDkcCkDK.......',
    '.KygGGgy........',
    '.KPLLLHD........',
    '.KHEHEHD........',
    '.KeCccCe........',
    '..KDDDDK........',
    '...KeEK.........',
    '....KK..........',
    '................',
]
SMALL_BIT = dict(collar_x=8, rows=(6, 12), cone={9: (6, 12), 10: (7, 11), 11: (7, 11), 12: (8, 10), 13: (9, 9)})

RAMP = 'XPLHD'  # bit steel, light to dark


def add_bit(grid, spec):
    """Gold collar plus a spiral bit pointing right, shaded like the vanilla drill tools: lit from the top,
    alternating light/dark ridges that shift along the bit (the spiral), tinted outline."""
    y0, y1 = spec['rows']
    cx = spec['collar_x']
    for y in range(y0, y1 + 1):
        t = (y - y0) / (y1 - y0)
        grid[y][cx] = 'G' if t < 0.2 else ('g' if t < 0.7 else 'y')
    grid[y0 - 1][cx] = 'b'
    grid[y1 + 1][cx] = 'b'
    cells = set()
    for x, (a, b) in spec['cone'].items():
        for y in range(a, b + 1):
            cells.add((x, y))
    for x, y in cells:
        t = (y - y0) / max(1, y1 - y0)            # 0 top .. 1 bottom
        level = 1 if t < 0.25 else (2 if t < 0.75 else 3)
        if (x + (y - y0) // 3) % 2:               # ridge in shadow
            level += 1
        elif level > 0:                           # ridge catching light
            level -= 1
        grid[y][x] = RAMP[min(level, len(RAMP) - 1)]
    for x, y in cells:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < 16 and 0 <= ny < 16 and (nx, ny) not in cells and grid[ny][nx] == '.':
                grid[ny][nx] = 'K'


def draw(rows, bit):
    assert len(rows) == 16 and all(len(r) == 16 for r in rows), 'icon grids are 16x16'
    grid = [list(r) for r in rows]
    add_bit(grid, bit)
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if PALETTE[ch] is not None:
                img.putpixel((x, y), PALETTE[ch] + (255,))
    return img


def render():
    return draw(ICON, ICON_BIT), draw(SMALL, SMALL_BIT)


def main():
    icon, small = render()
    out = HERE / 'Art'
    out.mkdir(exist_ok=True)
    icon.save(out / 'DrillUnit_icon.png')
    small.save(out / 'DrillUnit_smallIcon.png')
    print('wrote', out / 'DrillUnit_icon.png', out / 'DrillUnit_smallIcon.png')


if __name__ == '__main__':
    main()
