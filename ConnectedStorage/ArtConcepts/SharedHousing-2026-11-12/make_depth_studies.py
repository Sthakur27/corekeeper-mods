"""Compare three front-to-back joins using the same 16-pixel tier artwork."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_fused_review import GRID, housing
from make_review import original

HERE = Path(__file__).resolve().parent
NEIGHBORS = (('N', 0, -1), ('S', 0, 1), ('W', -1, 0), ('E', 1, 0))
GRID_LAYOUT = {(0, 0): 'Copper', (1, 0): 'Octarine',
               (0, 1): 'Relucite', (1, 1): 'Galaxite'}
TURN_LAYOUT = {(0, 0): 'Copper', (1, 0): 'Octarine', (1, 1): 'Galaxite'}


def gem_ramp(tier):
    image = original(tier)
    return {key: image.getpixel((x, y))
            for y, row in enumerate(GRID)
            for x, key in enumerate(row) if key in '123456'}


def tile(tier, edges, style):
    image = housing(tier, edges, revised=True, bevel=True)
    paint = ImageDraw.Draw(image)
    ramp = gem_ramp(tier)
    lo = 0 if 'W' in edges else 2
    hi = 15 if 'E' in edges else 13
    roof = image.getpixel((5, 6))
    bevel = image.getpixel((5, 7))

    if style in ('clamp', 'socket'):
        # Carry the perimeter around both exposed corners. The shoulders
        # narrow toward the lid and split at the tier boundary.
        for exposed_edge, side, inward in (('W', 1, 1), ('E', 14, -1)):
            if exposed_edge in edges:
                continue
            if 'S' in edges:
                paint.line((side, 10, side, 15), fill=ramp['3'])
                paint.line((side + inward, 12, side + inward, 15), fill=ramp['2'])
                paint.point((side, 12), fill=ramp['4'])
            if 'N' in edges:
                paint.line((side, 0, side, 4), fill=ramp['3'])
                paint.line((side + inward, 0, side + inward, 2), fill=ramp['2'])
                paint.point((side, 1), fill=ramp['4'])
        if 'S' in edges:
            paint.line((lo, 15, hi, 15), fill=roof)
        if 'N' in edges:
            paint.line((lo, 0, hi, 0), fill=roof)

    if style == 'step' and 'S' in edges:
        # A sliver of front bevel is still visible above the next lid.
        paint.line((lo, 13, hi, 13), fill=bevel)
        paint.line((lo, 14, hi, 14), fill='#47677e')
        paint.line((lo, 15, hi, 15), fill='#2a4e4a')
        for edge, left, right in (('W', 1, 3), ('E', 12, 14)):
            if edge not in edges:
                paint.line((left, 13, right, 13), fill=ramp['4'])
                paint.line((left, 14, right, 14), fill=ramp['2'])
    if style == 'step' and 'N' in edges:
        paint.line((lo, 0, hi, 0), fill=image.getpixel((5, 2)))

    if style == 'socket' and 'S' in edges:
        # Replace the lid emblem with a low gem bezel. Its width and
        # short facet rows read as a setting on the top surface.
        paint.rectangle((4, 3, 11, 7), fill=roof)
        paint.line((6, 5, 9, 5), fill=bevel)
        paint.rounded_rectangle((5, 6, 10, 10), radius=1, fill='#294443')
        paint.line((7, 6, 8, 6), fill=ramp['4'])
        paint.line((6, 7, 9, 7), fill=ramp['4'])
        paint.line((6, 8, 9, 8), fill=ramp['3'])
        paint.line((7, 9, 8, 9), fill=ramp['2'])
        paint.point((7, 7), fill=ramp['5'])
    return image


def render(layout, style):
    image = Image.new('RGBA', (32, 32))
    for (x, y), tier in layout.items():
        edges = frozenset(edge for edge, dx, dy in NEIGHBORS if (x + dx, y + dy) in layout)
        image.alpha_composite(tile(tier, edges, style), (x * 16, y * 16))
    return image


def main():
    board = Image.new('RGB', (1152, 460), '#252b33')
    draw = ImageDraw.Draw(board)
    font_dir = Path('C:/Windows/Fonts')
    title = ImageFont.truetype(str(font_dir / 'segoeuib.ttf'), 25)
    heading = ImageFont.truetype(str(font_dir / 'segoeui.ttf'), 19)
    body = ImageFont.truetype(str(font_dir / 'segoeui.ttf'), 15)
    note = ImageFont.truetype(str(font_dir / 'segoeui.ttf'), 14)
    draw.text((24, 17), 'RUNIC STORAGE | Front-to-back join studies', font=title, fill='#f2dfba')
    draw.text((24, 54), 'Same mixed-tier grid and L-shape. Sprites shown at 5× and 1×.', font=body, fill='#e1e6eb')
    studies = [
        ('clamp', 'A · Continuous lid', ('The outer clamps span the join.', 'Only the exposed front faces carry gems.')),
        ('step', 'B · Shallow step', ('A narrow bevel marks the row seam.', 'The front row covers the rear gems.')),
        ('socket', 'C · Gem in the lid', ('The rear gems sit flat in the lid.', 'The outer clamps span the join.')),
    ]
    for i, (style, label, description) in enumerate(studies):
        x = 24 + 384 * i
        draw.text((x, 105), label, font=heading, fill='#f2dfba')
        for j, layout in enumerate((GRID_LAYOUT, TURN_LAYOUT)):
            art = render(layout, style)
            large = art.resize((160, 160), Image.Resampling.NEAREST)
            px = x + 175 * j
            board.paste(large, (px, 144), large)
            board.paste(art, (px, 324), art)
        for j, line in enumerate(description):
            draw.text((x, 375 + 20 * j), line, font=body, fill='#e1e6eb')
    draw.text((24, 430), 'Front-view sprite mockups. The 16-pixel row spacing and overlap need an in-game check.', font=note, fill='#e1e6eb')
    board.save(HERE / 'depth-studies.png')


if __name__ == '__main__':
    main()
