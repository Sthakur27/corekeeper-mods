"""Crisp close-up of one placed Solarite Drill for the Solarite Drill cover (COVERS['SolariteDrill']).

    python release/art_direction/make_solaritedrill_hero.py

The game draws its 480x270 frame scaled 8x into the 3840x2160 capture, with a soft 1 px seam between
game pixels. This samples the centre of every 8x8 block of the tone-mapped HDR capture 17.43.22.67 (one
drill facing right on grass, the player beside it), giving the true game pixels, then scales them up by
an integer factor with nearest-neighbour. Nothing is repainted. Output: solaritedrill_hero.png next to
this script, read by make_screenshot_covers.py as the stamp 'src:solaritedrill_hero.png'.
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
import sys  # noqa: E402
sys.path.insert(0, str(HERE))
from make_screenshot_covers import shot  # noqa: E402

STAMP = 'hdr:17.43.22.67'
PX, PHASE_X, PHASE_Y = 8, 6, 7           # game pixel size in the 4K capture; block-centre offsets (x % 8, y % 8)
COLS, ROWS = range(247, 285), range(110, 142)   # game-pixel columns/rows: the drill (cols 254-277, rows 117-134) on grass
SCALE = 15                               # integer nearest-neighbour scale


def hero():
    full = Image.open(shot(STAMP)).convert('RGB')
    native = Image.new('RGB', (len(COLS), len(ROWS)))
    for j, r in enumerate(ROWS):
        for i, c in enumerate(COLS):
            native.putpixel((i, j), full.getpixel((c * PX + PHASE_X, r * PX + PHASE_Y)))
    return native.resize((native.width * SCALE, native.height * SCALE), Image.NEAREST)


if __name__ == '__main__':
    out = HERE / 'solaritedrill_hero.png'
    img = hero()
    img.save(out, optimize=True)
    print(out, img.size)
