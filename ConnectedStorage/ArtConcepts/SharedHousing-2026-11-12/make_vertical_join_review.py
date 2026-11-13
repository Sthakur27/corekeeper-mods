"""Native-pixel concepts for vertical seams and inside corners."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_fused_review import housing

HERE = Path(__file__).resolve().parent
TILE = 16
OUTLINE = '#193337'


def assembly(layout, joint):
    width = TILE * (max(x for x, _ in layout) + 1)
    height = TILE * (max(y for _, y in layout) + 1)
    image = Image.new('RGBA', (width, height))
    for (x, y), tier in layout.items():
        sides = {edge for edge, dx in (('W', -1), ('E', 1)) if (x + dx, y) in layout}
        image.paste(housing(tier, sides, revised=True, bevel=True, inset=True), (x * TILE, y * TILE))

    draw = ImageDraw.Draw(image)
    for (x, y), rear_tier in layout.items():
        front_tier = layout.get((x, y + 1))
        if front_tier is None:
            continue
        left, seam = x * TILE, (y + 1) * TILE
        rear = housing(rear_tier, revised=True)
        front = housing(front_tier, revised=True)
        rear_light = rear.getpixel((1, 1))
        rear_mid = rear.getpixel((2, 1))
        front_mid = front.getpixel((2, 1))
        front_dark = front.getpixel((3, 1))
        # Keep the rear face and gem. Only open the double outline at the joint.
        draw.line((left + 1, seam - 1, left + 14, seam - 1), fill='#354953')
        draw.line((left + 2, seam, left + 13, seam), fill='#263e49')
        if joint == 'straps':
            # Two tapered fittings take the load at the chest corners.
            for start in (2, 11):
                xx = left + start
                draw.rectangle((xx - 1, seam - 3, xx + 3, seam + 2), fill=OUTLINE)
                draw.line((xx, seam - 3, xx + 2, seam - 3), fill=rear_light)
                for offset, tone in ((-2, rear_mid), (-1, rear_mid), (0, front_mid), (1, front_dark)):
                    draw.line((xx, seam + offset, xx + 2, seam + offset), fill=tone)
                draw.line((xx, seam + 2, xx + 1, seam + 2), fill=front_mid)
        else:
            # A short overlap covers the front lid's back rim. Narrow metal
            # returns frame a shadow line where the two stepped surfaces meet.
            draw.rectangle((left + 1, seam - 2, left + 14, seam + 1), fill=OUTLINE)
            draw.line((left + 2, seam - 2, left + 13, seam - 2), fill=rear_mid)
            draw.line((left + 3, seam - 1, left + 12, seam - 1), fill=rear.getpixel((5, 7)))
            draw.line((left + 3, seam + 1, left + 12, seam + 1), fill=front.getpixel((5, 7)))
            for xx in (left + 2, left + 13):
                draw.point((xx, seam - 1), fill=rear_light)
                draw.line((xx, seam, xx, seam + 2), fill=front_mid)
    return image


def main():
    board = Image.new('RGB', (1090, 485), '#252b33')
    draw = ImageDraw.Draw(board)
    regular = 'C:/Windows/Fonts/segoeui.ttf'
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    font = ImageFont.truetype(regular, 17)
    small = ImageFont.truetype(regular, 14)
    draw.text((24, 18), 'Runic Storage | Vertical and L-shaped joins', font=title, fill='#f2dfba')
    draw.text((24, 56), 'Two new fittings at the row seam, with the existing tapered side join on the L.',
              font=small, fill='#e1e6eb')
    pair = {(0, 0): 'Copper', (0, 1): 'Octarine'}
    turn = {(0, 0): 'Relucite', (1, 0): 'Copper', (1, 1): 'Octarine'}
    for x, joint, name, caption in (
        (24, 'straps', 'A · Corner straps', 'Two tier-colored straps cross a recessed seam.'),
        (566, 'overlap', 'B · Stepped overlap', 'A short lip tucks over the next lid, with capped ends.'),
    ):
        draw.text((x, 97), name, font=font, fill='#f2dfba')
        draw.text((x, 125), caption, font=small, fill='#e1e6eb')
        for offset, layout in ((0, pair), (152, turn)):
            sprite = assembly(layout, joint)
            large = sprite.resize((sprite.width * 7, sprite.height * 7), Image.Resampling.NEAREST)
            board.paste(large, (x + offset, 159), large)
            board.paste(sprite, (x + offset, 400), sprite)
            draw.text((x + offset + sprite.width + 8, 405), '1×', font=small, fill='#bdc9d2')
    draw.text((24, 455), '16-pixel tile spacing. Check the overlap at the game’s actual viewing angle.',
              font=small, fill='#bdc9d2')
    board.save(HERE / 'vertical-join-review.png')


if __name__ == '__main__':
    main()
