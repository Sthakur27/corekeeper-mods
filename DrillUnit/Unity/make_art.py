"""Draw the Drill Unit item icons (16x16) into Unity/Art: DrillUnit_icon.png and DrillUnit_smallIcon.png.

    python -I DrillUnit/Unity/make_art.py

Same look as the in-world sheets (make_skin.py, whose palette and drill-bit generator are reused): the pod
seen from the side with its teal glass canopy, gold trim, teal energy band and hover glow, spiral drill bit
pointing right. Like the vanilla boats, `icon` is the full-size inventory icon and `smallIcon` a smaller
version of the same object (vanilla Speeder Boat: Art/vanilla/6581_icon.png / 6581_smallIcon.png).
Opaque pixels only (alpha 0 or 255), as in vanilla icons.
"""
import importlib.util
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent


def _skin():
    spec = importlib.util.spec_from_file_location('make_skin', HERE / 'make_skin.py')
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# Hull only; the drill bit is drawn by make_skin.add_drill from the spec below.
ICON = [
    '................',
    '...ooooo........',
    '..oWCccto.......',
    '.oWCcccccto.....',
    '.oCccccctto.....',
    'oGGGGGGGGGG.....',
    'olwwllllllh.....',
    'WhChhdhhChd.....',
    'odhhhdhhhdd.....',
    'otcCcccCcto.....',
    '.oddddddddo.....',
    '..otcCWCcto.....',
    '...otcccto......',
    '....ooooo.......',
    '................',
    '................',
]
ICON_DRILL = dict(axis='h', centre=7.5, front=True,
                  collar={11: (4, 11)}, cone={12: (4, 11), 13: (5, 10), 14: (6, 9)})

SMALL = [
    '................',
    '................',
    '................',
    '................',
    '.....oooo.......',
    '....oWCcto......',
    '...oCccccto.....',
    '..oGGGGGGGG.....',
    '..olwlllllh.....',
    '..WhChhhChd.....',
    '..otcCccCto.....',
    '...odddddo......',
    '....otWWto......',
    '.....oooo.......',
    '................',
    '................',
]
SMALL_DRILL = dict(axis='h', centre=9, front=True,
                   collar={11: (6, 12)}, cone={12: (6, 12), 13: (7, 11), 14: (8, 10)})


def draw(skin, rows, drill):
    grid = [list(r) for r in rows]
    mask = [['.'] * 16 for _ in rows]
    assert len(grid) == 16 and all(len(r) == 16 for r in grid)
    skin.add_drill(grid, mask, drill)
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    skin.paste(img, grid, (0, 0))
    return img


def render():
    skin = _skin()
    return draw(skin, ICON, ICON_DRILL), draw(skin, SMALL, SMALL_DRILL)


def main():
    icon, small = render()
    out = HERE / 'Art'
    out.mkdir(exist_ok=True)
    icon.save(out / 'DrillUnit_icon.png')
    small.save(out / 'DrillUnit_smallIcon.png')
    print('wrote', out / 'DrillUnit_icon.png', out / 'DrillUnit_smallIcon.png')


if __name__ == '__main__':
    main()
