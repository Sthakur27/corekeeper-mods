"""Coin Drops Workshop / mod.io cover: an "equation" (Sid, 2026-11-05).

    python release/art_direction/make_coindrops_cover.py [out.png]

    slime + sword = pile of Ancient Coins

Background: Sid's cave screenshot (HDR 01.45.08.76), blurred and darkened. The slime (orange slime idle
frame) and sword (Copper Sword item icon) are read from the installed game at render time and cached in
ignored build/. The Ancient Coin icon is cut from Sid's coin-slot screenshot (HDR 00.39.50.40); its "10" stack
count uses the game's own digits cut from his screenshots (the 1 of 17877, the 0 of 407). The + and = signs are drawn on the same pixel grid.
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, str(Path(__file__).parent))
import make_screenshot_covers as sc

REPO = sc.REPO
W, H, TOP = sc.W, sc.H, sc.TOP
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'release/coindrops_logo.png'
CACHE = REPO / 'build/artwork-review/coindrops'
CAVE = 'hdr:01.45.08.76'
EQUIP = 'hdr:00.40.00.79'


GAME_BUNDLE = Path(r'C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_all.bundle')
GOLD, GLOW = '#e8b85a', '#ffb347'


def glow_frames(canvas, rects, radius=10, width=3):
    g = Image.new('L', (W, H), 0)
    for r in rects:
        ImageDraw.Draw(g).rounded_rectangle(r, radius + 2, outline=255, width=10)
    g = g.filter(ImageFilter.GaussianBlur(10))
    canvas = Image.composite(Image.new('RGB', (W, H), GLOW), canvas, g.point(lambda v: v // 3))
    d = ImageDraw.Draw(canvas)
    for r in rects:
        d.rounded_rectangle(r, radius, outline=GOLD, width=width)
    return canvas


def shadowed(canvas, img, x, y):
    sh = Image.new('L', (W, H), 0)
    ImageDraw.Draw(sh).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
    canvas.paste('#000000', (0, 0), sh.filter(ImageFilter.GaussianBlur(12)))
    canvas.paste(img, (x, y), img if img.mode == 'RGBA' else None)


def game_image(kind, name):
    path = CACHE / f'{name}.png'
    if not path.exists():
        import UnityPy
        CACHE.mkdir(parents=True, exist_ok=True)
        for o in UnityPy.load(str(GAME_BUNDLE)).objects:
            if o.type.name == kind and o.peek_name() == name:
                o.read().image.save(path)
                break
    return Image.open(path).convert('RGBA')


def slime_frame():
    sheet = game_image('Texture2D', 'slimeBlob_orange_idle')
    return sheet.crop((0, 0, 16, sheet.height))


def keyed_coin_box(region):
    bg = region.getpixel((2, 2))[:3]
    m = Image.new('L', region.size, 0)
    rp, mp = region.load(), m.load()
    for y in range(region.height):
        for x in range(region.width):
            r, g, b, _ = rp[x, y]
            if not (abs(r - bg[0]) + abs(g - bg[1]) + abs(b - bg[2]) < 70 or (r > g + 25 and r > b + 40)):
                mp[x, y] = 255
    return m.getbbox()


def coin_icon():
    """The Ancient Coin icon from Sid's coin slot (HDR 00.39.50.40, count "407"). The stack count covers the
    lower part of the round icon, so its clean top-left quarter is mirrored into the other three and the
    slot's brown backing is keyed out."""
    full = Image.open(sc.shot('hdr:00.39.50.40')).convert('RGBA')
    region = full.crop((2160, 870, 2221, 960))
    bg = region.getpixel((2, 2))[:3]

    def keyed(im):
        px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, _ = px[x, y]
                if abs(r - bg[0]) + abs(g - bg[1]) + abs(b - bg[2]) < 70 or (r > g + 25 and r > b + 40):
                    px[x, y] = (0, 0, 0, 0)
        return im

    region = keyed(region)
    l, t, _, _ = region.getbbox()
    rx = region.width - l                  # radius (centre column = right edge of the region)
    q = region.crop((l, t, region.width, t + rx))
    im = Image.new('RGBA', (rx * 2 - 1, rx * 2 - 1))
    im.paste(q, (0, 0))
    im.paste(q.transpose(Image.FLIP_LEFT_RIGHT), (rx - 1, 0))
    top = im.crop((0, 0, im.width, rx))
    im.paste(top.transpose(Image.FLIP_TOP_BOTTOM), (0, rx - 1))
    return im


P = 12                                   # game pixel -> cover pixel


def big(im):
    return im.resize((im.width * P, im.height * P), Image.NEAREST)


def on_grid(im, size=16):
    """Put a screenshot-sized icon back on the game's 16 px grid (box downsample, hard alpha)."""
    im = im.resize((size, size), Image.BOX)
    im.putalpha(im.getchannel('A').point(lambda a: 255 if a >= 110 else 0))
    return im


def sign(kind):
    """+ or = as gold pixel blocks with a dark outline, 9x9 game pixels."""
    g = Image.new('RGBA', (9, 9), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    if kind == '+':
        d.rectangle((3, 0, 5, 8), fill=GOLD)
        d.rectangle((0, 3, 8, 5), fill=GOLD)
    else:
        d.rectangle((0, 1, 8, 3), fill=GOLD)
        d.rectangle((0, 5, 8, 7), fill=GOLD)
    a = g.getchannel('A')
    out = Image.new('RGBA', (11, 11), (0, 0, 0, 0))
    out.paste((26, 15, 8, 255), (0, 0), a.filter(ImageFilter.MaxFilter(3)).resize((11, 11)) if False else None) if False else None
    ring = Image.new('L', (11, 11), 0)
    ring.paste(a, (1, 1))
    ring = ring.filter(ImageFilter.MaxFilter(3))
    out.paste((26, 15, 8, 255), (0, 0), ring)
    out.paste(g, (1, 1), g)
    return big(out)


def outlined(im):
    ring = Image.new('L', (im.width + 2, im.height + 2), 0)
    ring.paste(im.getchannel('A'), (1, 1))
    out = Image.new('RGBA', ring.size, (0, 0, 0, 0))
    out.paste((26, 15, 8, 255), (0, 0), ring.filter(ImageFilter.MaxFilter(3)))
    out.paste(im, (1, 1), im)
    return out


def digit(stamp, x0):
    """The white strokes of one stack-count digit cut from a screenshot (cell x0..x0+31, rows 956..1011)."""
    im = Image.open(sc.shot(stamp)).convert('L').crop((x0, 956, x0 + 32, 1012))
    return im.point(lambda v: 255 if v > 200 else 0)


def count_label(digits):
    """White digits with an even 1-UI-pixel (4 px) dark outline, as the inventory draws stack counts."""
    mask = Image.new('L', (32 * len(digits) + 8, 56 + 8), 0)
    for i, d in enumerate(digits):
        mask.paste(d, (4 + 32 * i, 4), d)
    out = Image.new('RGBA', mask.size, (0, 0, 0, 0))
    out.paste((12, 8, 6, 255), (0, 0), mask.filter(ImageFilter.MaxFilter(9)))
    out.paste((255, 255, 255, 255), (0, 0), mask)
    return out


def sword():
    im = game_image('Sprite', 'lootspritesAnimalUpdate_copperSword_small')
    im = outlined(im.crop(im.getbbox()))
    return im.resize((im.width * 15, im.height * 15), Image.NEAREST)


def coin_with_count():
    """One Ancient Coin with a "10" stack count, as the inventory draws it (screenshot scale x2)."""
    full = Image.open(sc.shot('hdr:00.39.50.40')).convert('RGBA')
    region = full.crop((2160, 870, 2221, 960))
    l, t, _, _ = keyed_coin_box(region)
    coin = coin_icon()
    layer = Image.new('RGBA', (2296 - 2160, 1012 - 870), (0, 0, 0, 0))
    layer.paste(coin, (l, t), coin)
    label = count_label([digit('hdr:01.10.36.84', 2136),    # the 1 of "17877"
                         digit('hdr:00.39.50.40', 2232)])   # the 0 of "407"
    label = label.crop(label.getbbox())
    layer.paste(label, (layer.width - label.width, layer.height - label.height), label)
    layer = layer.crop(layer.getbbox())
    return layer.resize((layer.width * 2, layer.height * 2), Image.NEAREST)


# Background: the cave shot, blurred and dark.
bg = sc.crop(CAVE, (300, 150, 1100, 600)).resize((W, H), Image.LANCZOS)
canvas = ImageEnhance.Brightness(bg.filter(ImageFilter.GaussianBlur(6))).enhance(0.45)

# Plate behind the equation.
plate_box = (40, TOP + 40, W - 40, H - 40)
plate = Image.new('RGBA', (plate_box[2] - plate_box[0], plate_box[3] - plate_box[1]), (14, 40, 38, 205))
ImageDraw.Draw(plate).rounded_rectangle((0, 0, plate.width - 1, plate.height - 1), 14, outline='#2a8a80', width=3)
canvas.paste(plate, plate_box[:2], plate)

parts = [big(outlined(slime_frame())), sign('+'), sword(), sign('='), coin_with_count()]
gap = 34
total = sum(p.width for p in parts) + gap * (len(parts) - 1)
x = (W - total) // 2
cy = (plate_box[1] + plate_box[3]) // 2
for p in parts:
    sh = Image.new('L', (W, H), 0)
    sh.paste(p.getchannel('A'), (x + 8, cy - p.height // 2 + 10))
    canvas.paste('#000000', (0, 0), sh.filter(ImageFilter.GaussianBlur(8)).point(lambda v: v * 2 // 3))
    canvas.paste(p, (x, cy - p.height // 2), p)
    x += p.width + gap

sc.banner(canvas, 'COIN DROPS', 'ENEMIES YOU KILL DROP ANCIENT COINS')
OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT, optimize=True)
print(OUT, OUT.stat().st_size)
