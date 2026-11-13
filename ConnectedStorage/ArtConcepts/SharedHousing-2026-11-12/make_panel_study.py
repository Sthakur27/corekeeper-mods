"""Draw whole 16 px tile assemblies with a shared lid and exposed front faces."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_fused_review import housing, original

HERE = Path(__file__).resolve().parent
TILE = 16
FACE = 7
OUTLINE = '#193337'


def panel(layout):
    width = TILE * (max(x for x, _ in layout) + 1)
    height = TILE * (max(y for _, y in layout) + 1)
    image = Image.new('RGBA', (width, height))
    draw = ImageDraw.Draw(image)

    def occupied(x, y):
        return (x // TILE, y // TILE) in layout

    for (col, row), tier in layout.items():
        x, y = col * TILE, row * TILE
        sprite = housing(tier, revised=True)
        top = sprite.getpixel((5, 5))
        light = sprite.getpixel((5, 1))
        shade = sprite.getpixel((5, 7))
        exposed_front = (col, row + 1) not in layout
        roof_end = y + TILE - FACE if exposed_front else y + TILE
        draw.rectangle((x, y, x + TILE - 1, roof_end - 1), fill=top)
        if (col - 1, row) not in layout:
            draw.line((x + 1, y, x + 1, roof_end - 1), fill=light)
            draw.line((x + 2, y, x + 2, roof_end - 1), fill=shade)
        if (col + 1, row) not in layout:
            draw.line((x + TILE - 2, y, x + TILE - 2, roof_end - 1), fill=shade)
        if (col, row - 1) not in layout:
            draw.line((x, y + 1, x + TILE - 1, y + 1), fill=light)
            draw.line((x + 2, y + 2, x + TILE - 3, y + 2), fill=shade)

        # Center the inset on the visible roof space above each position.
        mark = sprite.crop((5, 3, 11, 6))
        image.paste(mark, (x + 5, (y + roof_end - 3) // 2), mark)
        if exposed_front:
            # The fascia exists only along the assembly's visible front boundary.
            draw.rectangle((x, roof_end, x + TILE - 1, y + TILE - 1), fill='#47677e')
            draw.line((x, roof_end, x + TILE - 1, roof_end), fill=shade)
            draw.line((x, roof_end + 1, x + TILE - 1, roof_end + 1), fill=OUTLINE)
            face = original(tier).crop((4, 10, 12, 14))
            image.paste(face, (x + 4, roof_end + 2), face)
            trim = sprite.getpixel((1, 1))
            for a, b in ((0, 3), (12, 15)):
                draw.line((x + a, y + 14, x + b, y + 14), fill=trim)

    # A perimeter drawn from the full footprint. No interior tile outlines.
    for y in range(height):
        for x in range(width):
            if occupied(x, y) and any(not occupied(x + dx, y + dy)
                                      for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))):
                draw.point((x, y), fill=OUTLINE)
    return image


def main():
    board = Image.new('RGB', (980, 482), '#252b33')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 14)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    draw.text((24, 18), 'Runic Storage drawn as an assembly', font=title, fill='#f2dfba')
    draw.text((24, 56), 'A shared lid and perimeter wrap the full footprint. The fascia follows the exposed front edges.',
              font=small, fill='#e1e6eb')
    block = {(0, 0): 'Copper', (1, 0): 'Octarine', (0, 1): 'Relucite', (1, 1): 'Galaxite'}
    turn = {(0, 0): 'Copper', (1, 0): 'Octarine', (1, 1): 'Relucite'}
    for x, name, layout in ((24, 'Four-unit block', block), (488, 'Three-unit L', turn)):
        image = panel(layout)
        draw.text((x, 103), name, font=font, fill='#e1e6eb')
        enlarged = image.resize((256, 256), Image.Resampling.NEAREST)
        board.paste(enlarged, (x, 138), enlarged)
        board.paste(image, (x + 283, 138), image)
        draw.text((x + 283, 179), '1×', font=small, fill='#e1e6eb')
    draw.text((24, 438), '8× details and 1× previews. Sprite study using a 16-pixel tile pitch.',
              font=small, fill='#bdc9d2')
    board.save(HERE / 'panel-study.png')


if __name__ == '__main__':
    main()
