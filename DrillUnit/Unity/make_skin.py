"""Draw the Drill Unit in-world sprite sheets (Unity/Art/DrillUnit_front.png 36x36 / DrillUnit_back.png 36x52).

    python -I DrillUnit/Unity/make_skin.py

Original pixel art (a round hover-drill pod: dark plated ring hull, gold trim, teal energy band and hover glow,
thruster at the stern, big spiral drill bit at the bow), drawn into the exact layout of the vanilla boat sheets
(Art/vanilla/6581_*.png, exported by extract_skin.py). The vanilla Boat graphics only swap the texture under
fixed sprite rects (SpriteSheetSkin), so positions must match. The rects, read from the game's Sprite assets
and converted to top-left image coordinates (x0, y0, x1, y1 exclusive):

    clip       front sheet (SR, drawn over the rider)   back sheet (SRBack, under the rider)
    BoatMoveU  boat_front_0  (0, 0, 16, 18)             boat2_6  (0, 0, 16, 12)
    BoatMoveR  boat_front_1  (16, 0, 36, 18)            boat2_7  (16, 0, 36, 12)
    BoatMoveD  boat_front_2  (0, 18, 16, 36)            boat2_8  (0, 18, 16, 33)
    (left = BoatMoveR mirrored by the XScaler transform; every sprite has its pivot at the rect centre)

U = moving up (away from the camera, bow at the top), D = moving down (bow at the bottom), R = side view, bow
to the right. Front and back share one canvas per direction, exactly as in the vanilla art: a pixel at a sheet
position lands at the same screen position in both layers. So each direction is drawn once as a composite plus
a mask of the pixels in front of the rider. The back sheet gets the whole composite (the game cuts it to its
rect), the front sheet the masked pixels. Composite pixels below the back rect are forced into the front layer,
otherwise they would never show.

Not used by the boat clips but kept in the vanilla layout: both sheets' (16, 18, 36, 36) holds a mirrored side
view in vanilla (a leftover); we put our mirrored side view there too. Back (0, 36, 16, 52) / (16, 36, 32, 52)
are the vanilla items' icon / smallIcon sprites (boat2_10 / boat2_5); we draw our icons there for completeness
(our item uses its own DrillUnit_icon.png / _smallIcon.png from make_art.py).

The boat material (CustomLitAmplify) has no emissive texture, no colour-replacement texture (_UseReplacementTex
0) and clips at alpha 0.5, so there are no special shader colours: plain opaque colour, alpha 0 or 255 only.
"Glow" is just bright colour.
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent

# Core Keeper style: no black outlines. Each outline pixel takes a dark, saturated shade of the part it
# borders (vanilla boats: navy round the hull, dark purple round the trim, brown round the motor), see
# tint_outline. Short ramps, light from the top.
PALETTE = {
    '.': None,
    'o': (28, 38, 72),       # outline placeholder (resolved by tint_outline; hull shade if alone)
    'O': (28, 38, 72),       # hull outline (dark steel blue)
    'Y': (96, 58, 22),       # gold outline (dark amber)
    'T': (10, 64, 78),       # energy outline (deep teal)
    'M': (42, 50, 82),       # drill steel outline
    'k': (30, 34, 50),       # cockpit interior / deep shadow
    'd': (46, 52, 72),       # hull dark
    'h': (72, 80, 104),      # hull
    'l': (108, 118, 146),    # hull light
    'w': (164, 174, 198),    # hull shine
    'y': (142, 94, 34),      # gold dark
    'g': (214, 164, 58),     # gold
    'G': (252, 222, 124),    # gold light
    't': (18, 92, 106),      # energy dark
    'c': (36, 178, 190),     # energy
    'C': (104, 236, 230),    # energy bright
    'W': (214, 255, 250),    # energy core
    's': (82, 88, 110),      # drill steel dark
    'm': (146, 154, 176),    # drill steel
    'S': (214, 220, 234),    # drill steel light
    'n': (56, 62, 84),       # drill steel deep shadow (flute bottoms)
}


def sym(half):
    """Mirror 8-column left halves into a symmetric 16-column grid."""
    return [row + row[::-1] for row in half]


# ---- U: moving up (away from the camera). Drill at the top behind the rider, thruster at the bottom. ----
U = sym([
    '........',
    '........',
    '........',
    '........',
    '..oooooo',
    '.olwwwww',
    'olwlllll',
    'olggottt',
    'oCgtkkkk',
    'oWgtkkkk',
    'oCgtkkkk',
    'ohggokkk',
    'odhggooo',
    'odhlllod',
    '.otccodW',
    '..ottoCW',
    '....ootC',
    '......tc',
])
U_FRONT = sym([
    '........',
    '........',
    '........',
    '........',
    '........',
    '........',
    '........',
    '####....',
    '####....',
    '####....',
    '####....',
    '#####...',
    '########',
    '########',
    '########',
    '########',
    '########',
    '########',
])

# ---- R: side view, thruster left, drill right (left view = this mirrored) ----
# columns: thruster 0-1 | hull 2-13 | drill collar 14, bit 15-19 (drawn by add_drill)
R = [
    '..' '............' '......',
    '..' '............' '......',
    '..' '..oooooooo..' '......',
    '..' '.olwwwwwwlo.' '......',
    '..' 'ohkkkkkkkkho' '......',
    '.o' 'ohkkkkkkkkho' '......',
    'od' 'oGGGGGGGGGGd' '......',
    'Wd' 'olwwlllllllh' '......',
    'Cd' 'ohhChhdhhChd' '......',
    'Wd' 'odhhhhdhhhdd' '......',
    'od' 'otcCcccccCto' '......',
    '.o' '.oddddddddo.' '......',
    '..' '..otcCWCcto.' '......',
    '..' '...otcccto..' '......',
    '..' '....ooooo...' '......',
    '..' '............' '......',
    '..' '............' '......',
    '..' '............' '......',
]
R_FRONT = [
    '....................',
    '....................',
    '....................',
    '....................',
    '....................',
    '....................',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
    '####################',
]

# ---- D: moving down (towards the camera). Thruster at the top behind the rider, drill at the bottom in front. ----
D = sym([
    '........',
    '......oo',
    '.....oCW',
    '..oooodh',
    '.olwwwod',
    'olwlllll',
    'olggottt',
    'oCgtkkkk',
    'oWgtkkkk',
    'oCgtkkkk',
    'ohggoooo',
    'odhgggGG',
    'otdhhhoo',
    '.otccooo',
    '..otto..',
    '........',
    '........',
    '........',
])
D_FRONT = sym([
    '........',
    '........',
    '........',
    '........',
    '........',
    '........',
    '........',
    '####....',
    '####....',
    '####....',
    '########',
    '########',
    '########',
    '########',
    '########',
    '########',
    '########',
    '########',
])

# Drill bits: collar cells (gold ring) and cone cells per row/column, shaded with spiral bands, then outlined.
#   axis 'v': rows -> (x0, x1) inclusive spans; band from row + distance to the centre column (chevrons)
#   axis 'h': cols -> (y0, y1) spans; band from column + distance to the centre row
DRILLS = {
    # U: bit behind the rider pointing away (up); wide enough that its flanks show beside the rider.
    'U': dict(axis='v', centre=7.5, front=False, twist=True,
              collar={6: (1, 14)}, cone={5: (2, 13), 4: (3, 12), 3: (4, 11), 2: (5, 10), 1: (6, 9), 0: (7, 8)}),
    'R': dict(axis='h', centre=8, front=True,
              collar={14: (4, 12)}, cone={15: (4, 12), 16: (5, 11), 17: (6, 10), 18: (7, 9), 19: (8, 8)}),
    # D: big bit pointing at the camera, foreshortened, in front of the rider's legs.
    'D': dict(axis='v', centre=7.5, front=True, twist=True,
              collar={10: (1, 14)}, cone={11: (2, 13), 12: (3, 12), 13: (4, 11), 14: (5, 10), 15: (6, 9), 16: (7, 8)}),
}
BANDS = 'Sms'           # light, mid, dark steel
COLLAR = 'Gggy'         # gold ramp along the collar, lit end first
FLUTE = 'Smsn'          # twisted-flute ramp for the U/D bits: edge highlight .. flute bottom


def twist_shade(x, y, x0, x1, centre):
    """Spiral flutes on a bit seen end-on: '/' grooves two pixels wide (highlight, steel, deep groove),
    one step lighter on the lit left flank and darker on the right."""
    if x1 - x0 <= 1:
        return 'S' if x <= centre else 'm'  # the point
    u = (x - centre) / ((x1 - x0 + 1) / 2)
    step = (0, 1, 3)[((x + y) // 2) % 3]
    if u < -0.5 and step:
        step -= 1
    elif u > 0.5 and step < 3:
        step += 1
    return FLUTE[step]


def add_drill(grid, mask, spec):
    """Draw a drill bit into grid (list of lists) and mark it in mask if it is in front of the rider."""
    h, w = len(grid), len(grid[0])
    cells = {}
    for row_i, (a, (b0, b1)) in enumerate(sorted(spec['collar'].items())):
        n = b1 - b0 + 1
        for i, b in enumerate(range(b0, b1 + 1)):
            ch = COLLAR[min(len(COLLAR) - 1, i * len(COLLAR) // n + row_i)]
            if i == n - 1:
                ch = 'y'
            cells[(a, b) if spec['axis'] == 'h' else (b, a)] = ch
    for a, (b0, b1) in spec['cone'].items():
        for b in range(b0, b1 + 1):
            if spec.get('twist'):
                cells[(b, a)] = twist_shade(b, a, b0, b1, spec['centre'])
                continue
            band = (a + int(abs(b - spec['centre']))) % 3
            if spec['axis'] == 'v':
                x, y = b, a
                band = (band + (1 if x > spec['centre'] else 0)) % 3 if b0 == b1 else band
            else:
                x, y = a, b
            cells[(x, y)] = BANDS[band]
    for (x, y), ch in cells.items():
        grid[y][x] = ch
    outline = set()
    for (x, y) in cells:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in cells and grid[ny][nx] == '.':
                outline.add((nx, ny))
    for x, y in outline:
        grid[y][x] = 'o'
    if spec['front']:
        for x, y in list(cells) + list(outline):
            mask[y][x] = '#'


# (composite, front mask, canvas origin in both sheets, back rect height inside the canvas)
VIEWS = {
    'U': (U, U_FRONT, (0, 0), 12),
    'R': (R, R_FRONT, (16, 0), 12),
    'D': (D, D_FRONT, (0, 18), 15),
}
LEFT_ORIGIN = (16, 18)  # vanilla leftover: mirrored side view, both sheets


def check(name, grid, w, h):
    assert len(grid) == h, f'{name}: {len(grid)} rows, want {h}'
    for i, row in enumerate(grid):
        assert len(row) == w, f'{name} row {i}: {len(row)} cols, want {w}'
        bad = set(row) - set(PALETTE) - {'#'}
        assert not bad, f'{name} row {i}: unknown {bad}'


def view(name):
    """Composite and front mask (lists of lists) for one direction, drill included."""
    grid, mask, _, _ = VIEWS[name]
    w = 20 if name == 'R' else 16
    check(name, grid, w, 18)
    check(name + '_FRONT', mask, w, 18)
    g = [list(r) for r in grid]
    m = [list(r) for r in mask]
    add_drill(g, m, DRILLS[name])
    return g, m


OUTLINE_OF = {**dict.fromkeys('kdhlw', 'O'), **dict.fromkeys('ygG', 'Y'),
              **dict.fromkeys('tcCW', 'T'), **dict.fromkeys('smSn', 'M')}


def tint_outline(grid):
    """Each 'o' becomes the outline shade of the part it borders most (4-neighbours, then diagonals)."""
    h, w = len(grid), len(grid[0])
    out = [list(r) for r in grid]
    for y in range(h):
        for x in range(w):
            if grid[y][x] != 'o':
                continue
            for ring in (((1, 0), (-1, 0), (0, 1), (0, -1)), ((1, 1), (-1, 1), (1, -1), (-1, -1))):
                votes = {}
                for dx, dy in ring:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] in OUTLINE_OF:
                        k = OUTLINE_OF[grid[ny][nx]]
                        votes[k] = votes.get(k, 0) + 1
                if votes:
                    out[y][x] = max(votes, key=votes.get)
                    break
            else:
                out[y][x] = 'O'
    return out


def paste(img, grid, origin, keep=lambda x, y: True, mirror=False):
    grid = tint_outline(grid)
    ox, oy = origin
    w = len(grid[0])
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            rgb = PALETTE[ch]
            if rgb is None or not keep(x, y):
                continue
            img.putpixel((ox + (w - 1 - x if mirror else x), oy + y), rgb + (255,))


def load_make_art():
    import importlib.util
    path = HERE / 'make_art.py'
    if not path.exists():
        return None
    spec = importlib.util.spec_from_file_location('make_art', path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod if hasattr(mod, 'render') else None


def build():
    front = Image.new('RGBA', (36, 36), (0, 0, 0, 0))
    back = Image.new('RGBA', (36, 52), (0, 0, 0, 0))
    for name, (_, _, origin, back_h) in VIEWS.items():
        grid, mask = view(name)
        in_front = lambda x, y, m=mask, bh=back_h: m[y][x] == '#' or y >= bh
        paste(back, grid, origin)
        paste(front, grid, origin, in_front)
        if name == 'R':
            paste(back, grid, LEFT_ORIGIN, mirror=True)
            paste(front, grid, LEFT_ORIGIN, in_front, mirror=True)
    make_art = load_make_art()
    if make_art is not None:  # our icons in the vanilla icon slots of the back sheet
        icon, small = make_art.render()
        back.alpha_composite(icon, (0, 36))
        back.alpha_composite(small, (16, 36))
    return front, back


def main():
    front, back = build()
    art = HERE / 'Art'
    front.save(art / 'DrillUnit_front.png')
    back.save(art / 'DrillUnit_back.png')
    print('wrote', art / 'DrillUnit_front.png', front.size, art / 'DrillUnit_back.png', back.size)


if __name__ == '__main__':
    main()
