"""Side-by-side previews of the Drill Unit art against vanilla (8x, nearest neighbour) into Unity/Art/preview/.

    python -I DrillUnit/Unity/make_preview.py

- sheet_front.png / sheet_back.png: vanilla Speeder Boat sheet (6581) left, Drill Unit sheet right, with the
  sprite rects the boat clips use outlined (cyan = used by BoatMove*, grey = vanilla leftover / icon slots).
- views.png: what the game composes per direction (back rect, a stand-in rider, front rect), vanilla top row,
  Drill Unit bottom row. Left is the right view flipped, as the XScaler does. The rider is a rough grey
  placeholder only to show what the front layer covers.
- icons.png: vanilla icon / smallIcon vs Drill Unit icon / smallIcon.
- icon_compare.png: our icon + smallIcon in a row with vanilla late-game icons (Art/vanilla/6581_icon.png and
  icon_*.png from extract_skin.py), at 8x on top and 2x below (roughly inventory size).
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ART = HERE / 'Art'
OUT = ART / 'preview'
Z = 8
BG = (74, 84, 100, 255)

# top-left image coords, x1/y1 exclusive
FRONT_RECTS = {'U': (0, 0, 16, 18), 'R': (16, 0, 36, 18), 'D': (0, 18, 16, 36)}
BACK_RECTS = {'U': (0, 0, 16, 12), 'R': (16, 0, 36, 12), 'D': (0, 18, 16, 33)}
CANVAS = {'U': (0, 0, 16, 18), 'R': (16, 0, 36, 18), 'D': (0, 18, 16, 36)}
EXTRA_FRONT = [(16, 18, 36, 36)]
EXTRA_BACK = [(16, 18, 36, 36), (0, 36, 16, 52), (16, 36, 32, 52)]

RIDER = [  # stand-in seated rider, 10 wide
    '...oooo...',
    '..oaaaao..',
    '.oaaaaaao.',
    '.oaaaaaao.',
    '.oaaaaaao.',
    '..oaaaao..',
    '.oobbbboo.',
    'obbbbbbbbo',
    'obbbbbbbbo',
    'obbbbbbbbo',
    'obbbbbbbbo',
    'oobbbbbboo',
    '.oooooooo.',
]
RIDER_PAL = {'o': (40, 40, 48, 255), 'a': (150, 150, 160, 255), 'b': (120, 120, 132, 255)}


def up(img, z=Z):
    return img.resize((img.width * z, img.height * z), Image.NEAREST)


def sheet_compare(van, new, rects, extra, name):
    a, b = up(van), up(new)
    pad = 16
    out = Image.new('RGBA', (a.width + b.width + pad * 3, a.height + pad * 2), BG)
    for img, x in ((a, pad), (b, a.width + pad * 2)):
        out.alpha_composite(img, (x, pad))
        d = ImageDraw.Draw(out)
        for r in extra:
            d.rectangle((x + r[0] * Z, pad + r[1] * Z, x + r[2] * Z - 1, pad + r[3] * Z - 1), outline=(150, 150, 150, 255))
        for r in rects.values():
            d.rectangle((x + r[0] * Z, pad + r[1] * Z, x + r[2] * Z - 1, pad + r[3] * Z - 1), outline=(0, 230, 230, 255))
    out.save(OUT / name)


def rider_img():
    img = Image.new('RGBA', (len(RIDER[0]), len(RIDER)))
    for y, row in enumerate(RIDER):
        for x, ch in enumerate(row):
            if ch in RIDER_PAL:
                img.putpixel((x, y), RIDER_PAL[ch])
    return img


def view(front, back, d):
    cx0, cy0, cx1, cy1 = CANVAS[d]
    top = 6  # headroom for the rider
    cv = Image.new('RGBA', (cx1 - cx0, cy1 - cy0 + top))
    br, fr = BACK_RECTS[d], FRONT_RECTS[d]
    cv.alpha_composite(back.crop(br), (br[0] - cx0, br[1] - cy0 + top))
    r = rider_img()
    cv.alpha_composite(r, ((cv.width - r.width) // 2, top + {'U': 0, 'R': -2, 'D': 1}[d]))
    cv.alpha_composite(front.crop(fr), (fr[0] - cx0, fr[1] - cy0 + top))
    return cv


def views(sets, name):
    cells = []
    for front, back in sets:
        row = []
        for d in ('U', 'R', 'D'):
            v = view(front, back, d)
            row.append(v)
            if d == 'R':
                row.append(v.transpose(Image.FLIP_LEFT_RIGHT))
        cells.append(row)
    cw, ch = 22, 26
    out = Image.new('RGBA', (len(cells[0]) * cw * Z, len(cells) * ch * Z), BG)
    for j, row in enumerate(cells):
        for i, v in enumerate(row):
            vv = up(v)
            out.alpha_composite(vv, (i * cw * Z + (cw * Z - vv.width) // 2, j * ch * Z + (ch * Z - vv.height) // 2))
    out.save(OUT / name)


def icons(pairs, name):
    pad = 16
    cells = [up(Image.open(p).convert('RGBA')) for p in pairs]
    out = Image.new('RGBA', (len(cells) * (16 * Z + pad) + pad, 16 * Z + pad * 2), BG)
    for i, c in enumerate(cells):
        out.alpha_composite(c, (pad + i * (16 * Z + pad), pad))
    out.save(OUT / name)


def icon_compare(name):
    ours = [ART / 'DrillUnit_icon.png', ART / 'DrillUnit_smallIcon.png']
    vanilla = [ART / 'vanilla' / '6581_icon.png'] + sorted((ART / 'vanilla').glob('icon_*.png'))
    imgs = [Image.open(p).convert('RGBA') for p in ours + vanilla]
    pad, cell = 12, 16 * Z
    w = pad + len(imgs) * (cell + pad)
    out = Image.new('RGBA', (w, cell + pad * 3 + 32), BG)
    d = ImageDraw.Draw(out)
    for i, im in enumerate(imgs):
        x = pad + i * (cell + pad)
        out.alpha_composite(up(im), (x, pad))
        out.alpha_composite(up(im, 2), (x + (cell - 32) // 2, cell + pad * 2))
        if i < len(ours):
            d.rectangle((x - 4, pad - 4, x + cell + 3, pad + cell + 3), outline=(232, 190, 80, 255))
    out.save(OUT / name)


def main():
    OUT.mkdir(exist_ok=True)
    vf = Image.open(ART / 'vanilla' / '6581_front.png').convert('RGBA')
    vb = Image.open(ART / 'vanilla' / '6581_back.png').convert('RGBA')
    nf = Image.open(ART / 'DrillUnit_front.png').convert('RGBA')
    nb = Image.open(ART / 'DrillUnit_back.png').convert('RGBA')
    sheet_compare(vf, nf, FRONT_RECTS, EXTRA_FRONT, 'sheet_front.png')
    sheet_compare(vb, nb, BACK_RECTS, EXTRA_BACK, 'sheet_back.png')
    views([(vf, vb), (nf, nb)], 'views.png')
    icons([ART / 'vanilla' / '6581_icon.png', ART / 'vanilla' / '6581_smallIcon.png',
           ART / 'DrillUnit_icon.png', ART / 'DrillUnit_smallIcon.png'], 'icons.png')
    icon_compare('icon_compare.png')
    print('wrote', *sorted(p.name for p in OUT.glob('*.png')))


if __name__ == '__main__':
    main()
