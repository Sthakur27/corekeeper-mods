"""Compose chest joins with the rear fascia hidden by a front neighbor."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_fused_review import housing

HERE = Path(__file__).resolve().parent
WIDTH = 16
# The front sprite's lid takes up the first nine rows. Use that as the
# projected row spacing for this study. Actual placement needs a game check.
ROW_STEP = 9


def soft_shadow(sprite):
    lid = sprite.getpixel((5, 5))
    shadow = sprite.getpixel((5, 7))
    return tuple(round((2 * light + dark) / 3) for light, dark in zip(lid, shadow))


def assembly(layout, shorter_clamps=False, row_step=ROW_STEP):
    width = (max(col for col, _ in layout) + 1) * WIDTH
    height = max(row for _, row in layout) * row_step + WIDTH
    image = Image.new('RGBA', (width, height))
    for col, row in sorted(layout, key=lambda tile: (tile[1], tile[0])):
        tier = layout[col, row]
        sides = {edge for edge, dx in (('W', -1), ('E', 1)) if (col + dx, row) in layout}
        if (col, row - 1) in layout:
            sides.add('N')
        sprite = housing(tier, sides, revised=True, bevel=True,
                         inset=3 if shorter_clamps else 1, corner_highlights=shorter_clamps,
                         flip_bottom=True)
        has_front = (col, row + 1) in layout
        if has_front:
            if row_step == WIDTH:
                # The camera projects a full tile between rows. Extend the
                # visible roof over the rear fascia within that same tile.
                sprite = sprite.crop((0, 0, WIDTH, ROW_STEP)).resize(
                    (WIDTH, WIDTH), Image.Resampling.NEAREST)
            else:
                sprite = sprite.crop((0, 0, WIDTH, row_step))
        image.alpha_composite(sprite, (col * WIDTH, row * row_step))

    draw = ImageDraw.Draw(image)
    for (col, row), front_tier in layout.items():
        rear_tier = layout.get((col, row - 1))
        if rear_tier is None:
            continue
        x, y = col * WIDTH, row * row_step
        rear = housing(rear_tier, revised=True)
        front = housing(front_tier, revised=True)
        # Continue the roof shadow across the seam and remove the separate
        # top rim of the front lid where it tucks beneath the rear lid.
        joins_left = (col - 1, row) in layout and (col - 1, row - 1) in layout
        joins_right = (col + 1, row) in layout and (col + 1, row - 1) in layout
        start = x if joins_left else x + 1
        end = x + 15 if joins_right else x + 14
        draw.line((start, y - 1, end, y - 1), fill=soft_shadow(rear))
        draw.line((start, y, end, y), fill=soft_shadow(front))
        draw.line((start, y + 1, end, y + 1), fill=front.getpixel((5, 5)))
        for direction, edge_x in ((-1, x + 2), (1, x + 13)):
            adjacent = (col + direction, row)
            if adjacent in layout and (col + direction, row - 1) in layout:
                continue
            # Keep the outside edge straight and taper each half inward.
            # The two mirrored wedges are widest exactly at the seam.
            outside = edge_x + direction
            for offset, width, color in (
                (-2, 1, rear.getpixel((1, 1))),
                (-1, 2, rear.getpixel((2, 1))),
                (0, 3, front.getpixel((3, 1))),
                (1, 2, front.getpixel((1, 1))),
                (2, 1, front.getpixel((2, 1))),
            ):
                start_x, end_x = sorted((outside, outside - direction * (width - 1)))
                draw.line((start_x, y + offset, end_x, y + offset), fill=color)
    for (col, row), tier in layout.items():
        if not all(tile in layout for tile in ((col + 1, row), (col, row + 1), (col + 1, row + 1))):
            continue
        x, y = (col + 1) * WIDTH, (row + 1) * row_step + 1
        # Lower the mirrored clamp one pixel to center it on the visible seam.
        for dx, dy, px, py in ((-1, -1, col, row), (1, -1, col + 1, row),
                               (-1, 1, col, row + 1), (1, 1, col + 1, row + 1)):
            corner = housing(layout[px, py], revised=True)
            inner_x = x - 1 if dx < 0 else x
            inner_y = y - 1 if dy < 0 else y
            draw.point((inner_x, inner_y), fill=corner.getpixel((2, 1)))
            draw.point((inner_x + dx, inner_y), fill=corner.getpixel((1, 1)))
            draw.point((inner_x, inner_y + dy), fill=corner.getpixel((3, 1)))
            draw.point((inner_x + dx, inner_y + dy), fill=soft_shadow(corner))
    return image


def main(tier=None):
    board = Image.new('RGB', (1080, 448), '#252b33')
    draw = ImageDraw.Draw(board)
    regular = 'C:/Windows/Fonts/segoeui.ttf'
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    font = ImageFont.truetype(regular, 17)
    small = ImageFont.truetype(regular, 14)
    heading = f'Runic Storage | {tier} join study' if tier else 'Runic Storage | Joined lids and exposed faces'
    draw.text((24, 18), heading, font=title, fill='#f2dfba')
    subtitle = ('Softer lid transitions and a lower, mirrored center clamp.' if tier else
                'A front unit covers the rear face in its column. The top clamps meet at the lid seam.')
    draw.text((24, 56), subtitle,
              font=small, fill='#e1e6eb')
    pair = {(0, 0): 'Copper', (0, 1): 'Octarine'}
    turn = {(0, 0): 'Relucite', (1, 0): 'Copper', (1, 1): 'Octarine'}
    block = {**turn, (0, 1): 'Galaxite'}
    for x, name, layout in ((24, 'Front-to-back pair', pair), (265, 'Three-unit L', turn),
                            (666, 'Four-unit block', block)):
        if tier:
            layout = dict.fromkeys(layout, tier)
        sprite = assembly(layout, shorter_clamps=bool(tier))
        draw.text((x, 99), name, font=font, fill='#f2dfba')
        large = sprite.resize((sprite.width * 9, sprite.height * 9), Image.Resampling.NEAREST)
        board.paste(large, (x, 134), large)
        board.paste(sprite, (x, 378), sprite)
        draw.text((x + sprite.width + 9, 383), '1×', font=small, fill='#bdc9d2')
    draw.text((24, 423), 'Projection study at 9 pixels between rows. Actual in-game spacing is unverified.',
              font=small, fill='#bdc9d2')
    output = f'occlusion-{tier.lower()}-seam-review.png' if tier else 'occlusion-seam-review.png'
    board.save(HERE / output)


if __name__ == '__main__':
    main('Copper')
