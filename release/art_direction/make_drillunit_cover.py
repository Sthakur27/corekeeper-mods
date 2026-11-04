"""Drill Unit cover draft from Sid's 2026-11-04 HDR captures: three things it does, side by side.

    python release/art_direction/make_drillunit_cover.py [--out DIR]   # default: release/drillunit_logo.png

1. Drilling a tunnel through a dirt wall (01.26.21.64).
2. Hovering on a lava strip at full health (01.24.46.27).
3. A dug pit trench next to a laid stone floor path (01.45.25.93, rider lifted onto the trench end: make_moved).
Background: the lava capture, blurred. Banner and gold frames come from make_screenshot_covers.py, labels
use the Wormhole Potion cover's pixel text. Game UI is never repainted. Convert the captures first with
hdr_to_png.py --out build/artwork-review/2026-11-04/hdr.
"""
from pathlib import Path
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402
import make_wormholepotion_cover as wp  # noqa: E402  (pixel_text / paste_text helpers)

W, H, TOP = covers.W, covers.H, covers.TOP
GOLD, DARK, MINT = '#e8b85a', '#1a0f08', '#94f7dd'
covers.HDR_DIRS.append(covers.REPO / 'build/artwork-review/2026-11-04/hdr')

MOVED = 'hdr:01.45.25.93-moved'  # panel 3 capture with the rider lifted (make_moved)

# (stamp, crop in 1280x720 preview pixels, tone, column x0, x1, label lines). Tone is a brightness gain,
# or ('gamma', g, gain) to lift a dark capture's shadows more than its highlights.
PANELS = [
    ('hdr:01.26.21.64', (490, 190, 770, 520), ('gamma', 0.55, 1.25), 24, 424, ['DRILL THROUGH', 'ANY WALL']),
    ('hdr:01.24.46.27', (510, 192, 770, 498), 1.0, 440, 840, ['HOVER OVER', 'WATER AND LAVA']),
    (MOVED, (505, 150, 785, 480), 1.7, 856, 1256, ['DIG PITS OR', 'LAY FLOOR']),
]
# The game's blue mouse cursor, painted out of these preview boxes (only strongly blue pixels inside them).
CURSORS = {'hdr:01.26.21.64': [(485, 360, 530, 405)], 'hdr:01.24.46.27': [(740, 315, 775, 355)],
           'hdr:01.45.25.93': [(600, 440, 660, 495)]}
CURSOR_COLOURS = [(0, 92, 163), (9, 39, 118), (0, 149, 156),    # 01.26 / 01.24 captures
                  (6, 59, 102), (5, 23, 78), (9, 89, 94)]        # 01.45 capture (darker)
CK = 3  # full-resolution pixels per preview pixel

# Panel 3 edit (Sid, 2026-11-04): in the capture the rider is a step past the dug trench (the dig lags a
# moment behind at speed), so the rider sprite is lifted onto the trench end. The sprite is cut by hand on
# the capture's 12 px art grid (blocks from x 1803, y 1168): rows -> (first col, last col), cursor blocks
# left out; vacated blocks get the floor 22 blocks to the right.
GRID_X, GRID_Y, BLOCK, LIFT, FLOOR_DX = 1803, 1168, 12, 12, 22
SPRITE = {4: (8, 11), 5: (7, 13), 6: (6, 14), 7: (6, 14), 8: (6, 14), 9: (6, 13), 10: (6, 14),
          11: (3, 15), 12: (3, 15), 13: (3, 15), 14: (3, 15), 15: (3, 8), 16: (3, 8)}
CURSOR_BLOCKS = [(r, c) for r in range(14, 20) for c in range(9, 14)]


def make_moved():
    out = covers.HDR_DIRS[-1] / f'{MOVED[4:]}.png'
    src = Image.open(covers.HDR_DIRS[-1] / '01.45.25.93.png').convert('RGB')
    img = src.copy()

    def block(r, c, dr=0, dc=0):
        x, y = GRID_X + (c + dc) * BLOCK, GRID_Y + (r + dr) * BLOCK
        return (x, y, x + BLOCK, y + BLOCK)

    cells = [(r, c) for r, (c0, c1) in SPRITE.items() for c in range(c0, c1 + 1)]
    def mean(boxes):
        return np.mean([np.asarray(src.crop(b), np.float32).mean() for b in boxes])

    for r, c in cells + CURSOR_BLOCKS:   # vacate: floor from further right on the same row, relit to match
        gain = mean([block(r, 1), block(r, 2), block(r, 16), block(r, 17)]) / mean([block(r, cc, dc=FLOOR_DX) for cc in (1, 2, 16, 17)])
        patch = ImageEnhance.Brightness(src.crop(block(r, c, dc=FLOOR_DX))).enhance(gain)
        img.paste(patch, block(r, c)[:2])
    for r, c in cells:                    # the sprite, LIFT blocks higher
        img.paste(src.crop(block(r, c)), block(r, c, dr=-LIFT)[:2])
    img.save(out)


def remove_cursor(img, stamp, box):
    """Fill the cursor's pixels from their surroundings (repeated neighbour averaging, numpy only)."""
    a = np.asarray(img, dtype=np.float32).copy()
    # The cursor is flat UI art: match its exact tone-mapped colours (they vary with the capture's brightness).
    blue = np.zeros(a.shape[:2], bool)
    for c in CURSOR_COLOURS:
        blue |= (np.abs(a - np.array(c, np.float32)) <= 6).all(axis=2)
    mask = np.zeros(blue.shape, bool)
    for x0, y0, x1, y1 in CURSORS.get(stamp, []):
        sx0, sy0 = max(0, (x0 - box[0]) * CK), max(0, (y0 - box[1]) * CK)
        sx1, sy1 = max(0, (x1 - box[0]) * CK), max(0, (y1 - box[1]) * CK)
        mask[sy0:sy1, sx0:sx1] |= blue[sy0:sy1, sx0:sx1]
    if not mask.any():
        return img
    m = np.array(Image.fromarray(mask.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(9))) > 0
    known = ~m
    while not known.all():
        acc = np.zeros_like(a)
        cnt = np.zeros(m.shape, np.float32)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                k = np.roll(np.roll(known, dy, 0), dx, 1)
                acc += np.roll(np.roll(a, dy, 0), dx, 1) * k[..., None]
                cnt += k
        grow = ~known & (cnt > 0)
        a[grow] = acc[grow] / cnt[grow][:, None]
        known |= grow
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def tone(img, t):
    if isinstance(t, tuple):
        _, g, gain = t
        img = img.point(lambda v: round(255 * (v / 255) ** g))
        return ImageEnhance.Brightness(img).enhance(gain) if gain != 1.0 else img
    return ImageEnhance.Brightness(img).enhance(t) if t != 1.0 else img
PANEL_Y0, PANEL_Y1 = TOP + 10, H - 74


def compose():
    bg = covers.crop('hdr:01.24.46.27', (0, 0, 1280, 720))
    canvas = ImageEnhance.Brightness(bg.resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(6))).enhance(0.45)
    for stamp, box, gain, x0, x1, label in PANELS:
        src = tone(remove_cursor(covers.crop(stamp, box), stamp, box), gain)
        img, x, y, _ = covers.fit(src, (x0, PANEL_Y0, x1, PANEL_Y1))
        shadow = Image.new('L', (W, H), 0)
        ImageDraw.Draw(shadow).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
        canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(12)))
        canvas.paste(img, (x, y))
        d = ImageDraw.Draw(canvas)
        d.rectangle((x - 4, y - 4, x + img.width + 3, y + img.height + 3), outline=DARK, width=4)
        d.rectangle((x - 2, y - 2, x + img.width + 1, y + img.height + 1), outline=GOLD, width=2)
        cx = (x0 + x1) // 2
        for i, line in enumerate(label):
            wp.paste_text(canvas, wp.pixel_text(line), cx, PANEL_Y1 + 6 + i * 32, MINT if i else '#ffd36a')
    covers.banner(canvas, 'DRILL UNIT (ALPHA)', 'A HOVERING MINING CRAFT FROM THE RIFT')
    return canvas


if __name__ == '__main__':
    out = Path(sys.argv[sys.argv.index('--out') + 1]) if '--out' in sys.argv else covers.REPO / 'release'
    out.mkdir(parents=True, exist_ok=True)
    make_moved()
    compose().save(out / 'drillunit_logo.png', optimize=True)
    print('wrote', out / 'drillunit_logo.png')
