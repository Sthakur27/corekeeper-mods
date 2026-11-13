"""Preview common chest corner fittings and optional network seams on shipped art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ART = HERE.parents[1] / 'Unity' / 'Art'
TIERS = ('Copper', 'Octarine', 'Relucite')
FITTINGS = {
    'Steel shoes': ('#354953', '#687a9a', '#b9c4dc'),
    'Bronze clamps': ('#583b33', '#966747', '#c99a6b'),
}


def original(tier):
    return Image.open(ART / f'{tier}Chest.png').convert('RGBA')


def fit(tier, name):
    image = original(tier)
    dark, mid, light = FITTINGS[name]
    draw = ImageDraw.Draw(image)
    for x, sign in ((1, 1), (14, -1)):
        # Riveted upper return and a small foot on the same vertical stile.
        for dx, y, color in ((0, 1, light), (1, 1, mid), (2, 1, dark),
                             (0, 2, mid), (1, 2, light), (0, 3, dark),
                             (0, 12, dark), (0, 13, mid), (1, 13, light),
                             (0, 14, mid), (1, 14, dark), (2, 14, dark)):
            draw.point((x + sign * dx, y), fill=color)
    assert image.getchannel('A').tobytes() == original(tier).getchannel('A').tobytes()
    assert image.crop((5, 9, 11, 14)).tobytes() == original(tier).crop((5, 9, 11, 14)).tobytes()
    return image


def rail(left=False, right=False, tier='Copper', active=True):
    image = fit(tier, 'Steel shoes')
    draw = ImageDraw.Draw(image)
    for enabled, edge, inner in ((left, 0, 2), (right, 15, 13)):
        if enabled:
            x0, x1 = sorted((edge, inner))
            draw.rectangle((x0, 5, x1, 7), fill='#284746')
            draw.line((x0, 5, x1, 5), fill='#758ba0')
            draw.line((x0, 6, x1, 6), fill='#6cbaaf' if active else '#657985')
    return image


def main():
    board = Image.new('RGB', (1056, 760), '#232a32')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 14)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)

    def label(x, y, value, f=font, color='#e5eaf0'):
        draw.text((x, y), value, font=f, fill=color)

    def sprite(image, x, y, scale=7):
        scaled = image.resize((16 * scale, 16 * scale), Image.Resampling.NEAREST)
        board.paste(scaled, (x, y), scaled)

    label(24, 17, 'RUNIC STORAGE | Shared housing studies', title, '#f2dfba')
    label(24, 51, 'Original 16 × 16 art with native-pixel edits. Fittings at 6×, rails at 7×, and small previews at 1×.', small)
    for col, title_text in enumerate(('Shipped housing', 'A · Steel shoes', 'B · Bronze clamps')):
        x = 24 + col * 344
        label(x, 94, title_text)
        for i, tier in enumerate(TIERS):
            im = original(tier) if col == 0 else fit(tier, list(FITTINGS)[col - 1])
            sx = x + 110 * i
            sprite(im, sx, 124, 6)
            sprite(im, sx, 228, 1)
            label(sx + 22, 227, tier, small)
    draw.line((24, 270, 1032, 270), fill='#52616d')
    label(24, 288, 'C · Shared edge rails', title, '#f2dfba')
    label(24, 322, 'A short rail meets its neighbor at the seam. The same teal accent works across tier colors.', small)

    for x, active, heading in ((24, False, 'Adjacent units · idle'), (422, True, 'Terminal-linked units · active')):
        label(x, 361, heading)
        for i, tier in enumerate(TIERS):
            im = rail(i > 0, i < 2, tier, active)
            sprite(im, x + i * 112, 393)
            sprite(im, x + i * 16, 519, 1)
    label(815, 361, 'Standalone')
    sprite(rail(tier='Octarine'), 815, 393)
    sprite(rail(tier='Octarine'), 815, 519, 1)
    draw.line((24, 569, 1032, 569), fill='#52616d')
    label(24, 590, 'Active edge detail', font)
    sprite(rail(right=True, tier='Copper'), 24, 622, 7)
    sprite(rail(left=True, tier='Octarine'), 136, 622, 7)
    label(283, 644, 'A muted blue fitting carries the rail. A single teal pixel row marks the link.', small)
    label(283, 673, 'Front-facing art study; other orientations and junctions still need a placed-world check.', small)
    board.save(HERE / 'review.png')


if __name__ == '__main__':
    main()
