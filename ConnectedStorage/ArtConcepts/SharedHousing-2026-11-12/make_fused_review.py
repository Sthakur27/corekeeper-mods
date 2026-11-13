"""Draw tier-matched clamps and fused housings on shipped front sprites."""
import runpy
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_review import original

HERE = Path(__file__).resolve().parent
GRID = runpy.run_path(str(HERE.parent / 'GemSet' / 'ancient_views.py'))['CHEST_FRONT']
TIERS = ('Copper', 'Tin', 'Iron', 'Scarlet', 'Octarine', 'Galaxite', 'Solarite', 'Pandorium', 'Relucite')
SLOTS = [(dx, y, tone) for dx, y, tone in (
    (0, 1, '4'), (1, 1, '3'), (2, 1, '2'), (0, 2, '3'),
    (1, 2, '4'), (0, 3, '2'), (0, 12, '2'), (0, 13, '3'),
    (1, 13, '4'), (0, 14, '3'), (1, 14, '2'), (2, 14, '2'),
)]


def housing(tier, edges=frozenset(), revised=False, bevel=False, inset=0,
            corner_highlights=False, flip_bottom=False):
    image = original(tier)
    ramp = {key: image.getpixel((x, y))
            for y, row in enumerate(GRID) for x, key in enumerate(row) if key in '234'}
    if revised:
        for y, row in enumerate(GRID[:9]):
            for x, key in enumerate(row):
                if key in 'WHLMdD':
                    base = image.getpixel((x, y))
                    strength = 0.28 if key in 'WH' else 0.42
                    color = tuple(round(a * (1 - strength) + b * strength)
                                  for a, b in zip(base[:3], ramp['4'][:3]))
                    image.putpixel((x, y), (*color, base[3]))
                elif key == 'T':
                    image.putpixel((x, y), ramp['3'])
    for edge, x, sign in (('W', 1, 1), ('E', 14, -1)):
        if edge in edges:
            # The two adjacent tiles supply one pixel each. Keep horizontal
            # bands continuous while removing the interior vertical frame.
            for y in range(16):
                color = image.getpixel((3 if edge == 'W' else 12, y))
                for dx in (-1, 0, 1):
                    image.putpixel((x + dx, y), color)
        else:
            for dx, y, tone in SLOTS:
                if ('N' in edges and y < 4) or ('S' in edges and y > 11):
                    continue
                image.putpixel((x + sign * dx, y), ramp[tone])
    # Front-facing projection study for the other grid axis. A muted horizontal
    # step separates the front face below from the next lid above it.
    x0 = 0 if 'W' in edges else 1
    x1 = 16 if 'E' in edges else 15
    if revised:
        draw = ImageDraw.Draw(image)
        if 'S' in edges:
            # Carry the rear lid down over the face that a front unit hides.
            # Use broad clusters rather than stretching individual flecks.
            lid = image.getpixel((5, 6))
            shade = image.getpixel((5, 7))
            for y in range(8, 16):
                for x in range(16):
                    outside = (x < 2 and 'W' not in edges) or (x > 13 and 'E' not in edges)
                    color = image.getpixel((x, 6)) if outside else shade if y == 15 else lid
                    image.putpixel((x, y), color)
        if 'N' in edges:
            # A shallow step instead of another bright top rim at the row seam.
            for x in range(x0, x1):
                image.putpixel((x, 0), image.getpixel((x, 3)))
                image.putpixel((x, 1), image.getpixel((x, 2)))
        for edge, lo, hi in (('W', 0, 4), ('E', 11, 15)):
            if edge not in edges:
                continue
            # Shorten only the exposed end. Both halves still touch the seam.
            if inset:
                if edge == 'E':
                    lo += inset
                else:
                    hi -= inset
            bevel_lo = lo + 1 if bevel and edge == 'E' else lo
            bevel_hi = hi - 1 if bevel and edge == 'W' else hi
            if 'N' not in edges:
                draw.line((lo, 1, hi, 1), fill=ramp['3'])
                draw.line((bevel_lo, 2, bevel_hi, 2), fill=ramp['2'])
                if corner_highlights:
                    draw.point((lo if edge == 'E' else hi, 1), fill=ramp['4'])
            if 'S' not in edges:
                wide_y, narrow_y = (14, 13) if flip_bottom else (13, 14)
                draw.line((lo, wide_y, hi, wide_y), fill=ramp['3'])
                draw.line((bevel_lo, narrow_y, bevel_hi, narrow_y), fill=ramp['2'])
                if corner_highlights:
                    draw.point((lo if edge == 'E' else hi, wide_y), fill=ramp['4'])
        if 'S' not in edges:
            assert image.crop((5, 9, 11, 14)).tobytes() == original(tier).crop((5, 9, 11, 14)).tobytes()
        return image
    for edge, y, shade in (('N', 0, '#a1aedd'), ('S', 15, '#47677e')):
        if edge in edges:
            ImageDraw.Draw(image).line((x0, y, x1 - 1, y), fill=shade)
    assert image.crop((5, 9, 11, 14)).tobytes() == original(tier).crop((5, 9, 11, 14)).tobytes()
    return image


def main(revised=False):
    board = Image.new('RGB', (1120, 720), '#252b33')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 14)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)

    def label(x, y, value, f=font):
        draw.text((x, y), value, font=f, fill='#f2dfba' if f == title else '#e1e6eb')

    def show(image, x, y, scale):
        enlarged = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
        board.paste(enlarged, (x, y), enlarged)

    label(24, 16, 'RUNIC STORAGE | Tier clamps and fused housing', title)
    subtitle = 'Gem-tinted lids and matching clamps on 16 × 16 front sprites.' if revised else 'Front sprites at actual 16 × 16. Each clamp uses the colors of its shipped gem.'
    label(24, 52, subtitle, small)
    for i, tier in enumerate(TIERS):
        x = 24 + i * 120
        image = housing(tier, revised=revised)
        assert image.getchannel('A').tobytes() == original(tier).getchannel('A').tobytes()
        show(image, x, 91, 6)
        show(image, x, 199, 1)
        label(x + 23, 197, tier, small)
    draw.line((24, 240, 1096, 240), fill='#52616d')
    label(24, 257, 'A · Tier clamps on adjacent units', font)
    label(408, 257, 'B · Fused edges with a shared clamp band' if revised else 'B · Same units with fused edges', font)
    for i, tier in enumerate(('Copper', 'Octarine', 'Relucite')):
        show(housing(tier, revised=revised), 24 + 112 * i, 290, 7)
        edges = frozenset(('E',) if i == 0 else ('W',) if i == 2 else ('W', 'E'))
        show(housing(tier, edges, revised), 408 + 112 * i, 290, 7)
        show(housing(tier, edges, revised), 408 + 16 * i, 412, 1)
    label(24, 455, 'Grid and turn studies', font)
    for start, layout in ((24, {(0, 0): 'Copper', (1, 0): 'Octarine', (0, 1): 'Relucite', (1, 1): 'Galaxite'}),
                          (280, {(0, 0): 'Copper', (1, 0): 'Octarine', (1, 1): 'Relucite'})):
        for (x, y), tier in layout.items():
            edges = {edge for edge, dx, dy in (('N', 0, -1), ('S', 0, 1), ('W', -1, 0), ('E', 1, 0))
                     if (x + dx, y + dy) in layout}
            show(housing(tier, edges, revised), start + 96 * x, 486 + 96 * y, 6)
    label(526, 501, 'Neighboring clamps meet as trim, with a color change at the tier boundary.' if revised else 'Each touching edge trades its outline for shared housing.', small)
    label(526, 529, 'A front unit hides the rear unit’s face and gem in the same column.' if revised else 'Outside corners keep their tier-colored clamps.', small)
    label(526, 575, 'The grid is a front-view mockup. Depth, side views,', small)
    label(526, 596, 'and placement or removal need a game test.', small)
    board.save(HERE / ('fused-review-v2.png' if revised else 'fused-review.png'))


def side_review():
    board = Image.new('RGB', (1120, 365), '#252b33')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    draw.text((24, 16), 'Side joins with shorter clamp bands', font=title, fill='#f2dfba')
    draw.text((24, 51), 'The tapered ends move one pixel closer to the seam on both sides.',
              font=font, fill='#e1e6eb')
    for x, inset, label in ((24, False, 'Previous taper'), (576, True, 'One pixel shorter on each side')):
        draw.text((x, 96), label, font=font, fill='#e1e6eb')
        for i, tier in enumerate(('Copper', 'Octarine', 'Relucite')):
            edges = ({'E'}, {'W', 'E'}, {'W'})[i]
            image = housing(tier, edges, revised=True, bevel=True, inset=inset)
            enlarged = image.resize((160, 160), Image.Resampling.NEAREST)
            board.paste(enlarged, (x + i * 160, 127), enlarged)
            board.paste(image, (x + i * 16, 306), image)
        draw.text((x + 60, 303), '1×', font=font, fill='#e1e6eb')
    board.save(HERE / 'side-inset-review.png')


if __name__ == '__main__':
    main(revised=True)
    side_review()
