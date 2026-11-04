"""Wormhole Potion cover: the mod as three numbered steps, from Sid's 2026-11-04 screenshots.

    python release/art_direction/make_wormholepotion_cover.py [--out DIR]

1. The player holding the potion out in the world (HDR capture 00.35.22.78).
2. The potion-icon "Wormhole Return Point" on the map, with its hover title (capture 00.38.00.07).
3. The map's teleport line from a waypoint to that marker (screenshot Sid pasted in chat, SDR).
Panels, gold frames and the title banner come from make_screenshot_covers.py; step numbers, labels
and arrows are drawn at quarter scale in the banner's palette. Game UI is never repainted.
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402

W, H, TOP, P = covers.W, covers.H, covers.TOP, 4
GOLD, GOLD_HI, DARK, MINT = '#e8b85a', '#ffd36a', '#1a0f08', '#94f7dd'
GAIN = 1.7

# (stamp, crop in 1280x720 preview pixels, gain, column x0, x1, label lines)
STEPS = [
    ('hdr:00.35.22.78', (490, 170, 810, 520), GAIN, 24, 300, ['DRINK IT ANYWHERE', 'YOU WARP HOME']),
    ('hdr:00.38.00.07', (500, 170, 1045, 470), GAIN, 346, 732, ['YOUR SPOT GETS', 'A MAP MARKER']),
    ('src:wormholepotion_map.png', (40, 150, 1000, 600), 1.0, 778, 1256, ['WARP BACK ONCE', 'FROM ANY WAYPOINT']),
]
# Gold frames on the subject of a step (preview pixels).
FRAMES = {'hdr:00.35.22.78': [(600, 296, 668, 372)]}
PANEL_Y0, PANEL_Y1 = TOP + 16, 566


def pixel_text(s, size=8):
    """1-bit text mask at quarter scale (spaces tripled, as in the banner caption)."""
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', size)
    s = s.replace(' ', '   ')
    probe = ImageDraw.Draw(Image.new('L', (1, 1)))
    w = round(probe.textlength(s, font=f)) + 4
    m = Image.new('L', (w, size + 6))
    d = ImageDraw.Draw(m)
    d.fontmode = '1'
    d.text((2, 1), s, font=f, fill=255)
    return m


def paste_text(canvas, mask, cx, y, fill, scale=3):
    big = mask.resize((mask.width * scale, mask.height * scale), Image.NEAREST)
    x = cx - big.width // 2
    canvas.paste(DARK, (x, y), big.filter(ImageFilter.MaxFilter(7)))
    canvas.paste(fill, (x, y), big)


def badge(canvas, n, cx, cy):
    """Gold pixel disc with the step number, drawn at quarter scale."""
    s = 13
    b = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(b)
    d.ellipse((0, 0, s - 1, s - 1), fill=DARK)
    d.ellipse((1, 1, s - 2, s - 2), fill=GOLD)
    d.arc((2, 2, s - 3, s - 3), 200, 340, fill=GOLD_HI)
    m = pixel_text(str(n), 9)
    bbox = m.getbbox()
    m = m.crop(bbox)
    b.paste(DARK, ((s - m.width) // 2, (s - m.height) // 2), m)
    b = b.resize((s * P, s * P), Image.NEAREST)
    canvas.paste(b, (cx - b.width // 2, cy - b.height // 2), b)


def arrow(canvas, x, y):
    """Gold pixel triangle pointing right, between two columns."""
    rows = ['X....', 'XX...', 'XXX..', 'XXXX.', 'XXXXX', 'XXXX.', 'XXX..', 'XX...', 'X....']
    a = Image.new('RGBA', (7, 11), (0, 0, 0, 0))
    for j, row in enumerate(rows):
        for i, c in enumerate(row):
            if c == 'X':
                a.putpixel((i + 1, j + 1), (232, 184, 90, 255))
    edge = a.split()[3].filter(ImageFilter.MaxFilter(3))
    out = Image.new('RGBA', a.size, (0, 0, 0, 0))
    out.paste((26, 15, 8, 255), (0, 0), edge)
    out.alpha_composite(a)
    out = out.resize((out.width * 5, out.height * 5), Image.NEAREST)
    canvas.paste(out, (x - out.width // 2 + 3, y - out.height // 2), out)


def compose():
    bg = covers.crop('hdr:00.35.22.78', (330, 100, 970, 460))
    bg = bg.resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(6))
    canvas = ImageEnhance.Brightness(bg).enhance(0.45 * GAIN)

    centres = []
    for stamp, box, gain, x0, x1, _ in STEPS:
        src = covers.crop(stamp, box)
        if gain != 1.0:
            src = ImageEnhance.Brightness(src).enhance(gain)
        img, x, y, _ = covers.fit(src, (x0, PANEL_Y0, x1, PANEL_Y1))
        shadow = Image.new('L', (W, H), 0)
        ImageDraw.Draw(shadow).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
        canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(12)))
        canvas.paste(img, (x, y))
        d = ImageDraw.Draw(canvas)
        d.rectangle((x - 4, y - 4, x + img.width + 3, y + img.height + 3), outline=DARK, width=4)
        d.rectangle((x - 2, y - 2, x + img.width + 1, y + img.height + 1), outline=GOLD, width=2)
        k = img.width / (box[2] - box[0])
        for hx0, hy0, hx1, hy1 in FRAMES.get(stamp, []):
            f = (x + round((hx0 - box[0]) * k), y + round((hy0 - box[1]) * k),
                 x + round((hx1 - box[0]) * k), y + round((hy1 - box[1]) * k))
            d.rounded_rectangle((f[0] - 3, f[1] - 3, f[2] + 3, f[3] + 3), 13, outline=DARK, width=9)
            d.rounded_rectangle(f, 10, outline=GOLD, width=3)
        centres.append(((x0 + x1) // 2, y + img.height // 2))

    for i in range(len(STEPS) - 1):
        gap = (STEPS[i][4] + STEPS[i + 1][3]) // 2
        arrow(canvas, gap, (PANEL_Y0 + PANEL_Y1) // 2)

    for n, ((_, _, _, x0, x1, lines), (cx, _)) in enumerate(zip(STEPS, centres), 1):
        badge(canvas, n, cx, PANEL_Y1 + 30)
        for k, line in enumerate(lines):
            paste_text(canvas, pixel_text(line), cx, PANEL_Y1 + 54 + k * 32, '#ffffff' if k == 0 else MINT)

    covers.banner(canvas, 'WORMHOLE POTION', '10 GOLD BARS AT THE ANCIENT HOLOGRAM POD')
    return canvas


if __name__ == '__main__':
    args = sys.argv[1:]
    out_dir = covers.REPO / 'build/artwork-review/2026-11-04'
    if '--out' in args:
        out_dir = Path(args[args.index('--out') + 1])
    out_dir.mkdir(parents=True, exist_ok=True)
    out = out_dir / 'WormholePotion.png'
    c = compose()
    c.save(out, optimize=True)
    print(out, c.size, out.stat().st_size)
