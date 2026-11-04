"""Wormhole Potion cover: the potion and its recipe, then the mod as three numbered steps (Sid's 2026-11-04 screenshots).

    python release/art_direction/make_wormholepotion_cover.py [--out DIR]

0. Crafting column: the potion's own 16 px inventory sprite blown up nearest-neighbour, the Ancient Hologram
   Pod window ("Remnants") with the potion's recipe slot, and the 10 Gold Bar icon from its tooltip (capture 00.36.10.40).
1. The player mid-cast with the potion: blue glow and cast bar (HDR capture 01.10.59.19).
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
    ('hdr:01.10.59.19', (500, 250, 780, 620), GAIN, 352, 566, ['DRINK IT ANYWHERE', 'YOU WARP HOME']),
    ('hdr:00.38.00.07', (500, 170, 1045, 470), GAIN, 604, 896, ['YOUR SPOT GETS', 'A MAP MARKER']),
    ('src:wormholepotion_map.png', (40, 150, 1000, 600), 1.0, 934, 1256, ['WARP BACK ONCE', 'FROM ANY WAYPOINT']),
]
# Gold frames on the subject of a step (preview pixels).
FRAMES = {'hdr:01.10.59.19': [(586, 286, 676, 386)], 'hdr:00.36.10.40': [(676, 286, 736, 346)]}
PANEL_Y0, PANEL_Y1 = TOP + 16, 566
ICON = covers.REPO / 'WormholePotion/Unity/Art/WormholePotion_icon.png'
CRAFT = ('hdr:00.36.10.40', (540, 182, 740, 352), GAIN, 24, 314)   # the pod's window, below the big icon
GOLD_BAR = ('hdr:00.36.10.40', (854, 661, 887, 699))                 # gold bar icon with its green "10"


def pixel_text(s, size=8, gap=3):
    """1-bit text mask at quarter scale (spaces widened, as in the banner caption)."""
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', size)
    s = s.replace(' ', ' ' * gap)
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

    def panel(stamp, box, gain, x0, x1, y0, y1):
        src = covers.crop(stamp, box)
        if gain != 1.0:
            src = ImageEnhance.Brightness(src).enhance(gain)
        img, x, y, _ = covers.fit(src, (x0, y0, x1, y1))
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

    # Crafting column: big potion over a violet glow, the pod window under it.
    cx = (CRAFT[3] + CRAFT[4]) // 2
    icon = Image.open(ICON).convert('RGBA')
    icon = icon.resize((icon.width * 11, icon.height * 11), Image.NEAREST)
    iy = PANEL_Y0 - 6
    glow = Image.new('L', (W, H), 0)
    ImageDraw.Draw(glow).ellipse((cx - 95, iy + 10, cx + 95, iy + icon.height - 4), fill=200)
    canvas.paste('#9a5cff', (0, 0), glow.filter(ImageFilter.GaussianBlur(28)))
    shade = Image.new('L', icon.size, 0)
    shade.paste(icon.split()[3].point(lambda v: 150 if v else 0), (0, 0))
    canvas.paste('#000000', (cx - icon.width // 2 + 8, iy + 10), shade)
    canvas.paste(icon, (cx - icon.width // 2, iy), icon)
    panel(*CRAFT, iy + icon.height + 8, PANEL_Y1)

    for stamp, box, gain, x0, x1, _ in STEPS:
        panel(stamp, box, gain, x0, x1, PANEL_Y0, PANEL_Y1)
    columns = [(CRAFT[3], CRAFT[4])] + [(st[3], st[4]) for st in STEPS]
    for (_, a1), (b0, _) in zip(columns, columns[1:]):
        arrow(canvas, (a1 + b0) // 2, (PANEL_Y0 + PANEL_Y1) // 2)

    # Labels: the gold bar icon (with its "10") in place of a number under the crafting column.
    bar = covers.crop(GOLD_BAR[0], GOLD_BAR[1])
    bar = ImageEnhance.Brightness(bar).enhance(GAIN)
    bar = bar.resize((round(bar.width * 52 / bar.height), 52), Image.LANCZOS)
    # keep only the icon's bright pixels; the tooltip's dark backing drops out
    keep = bar.convert('L').point(lambda v: 255 if v > 60 else 0).filter(ImageFilter.MaxFilter(3))
    canvas.paste(bar, (cx - bar.width // 2, PANEL_Y1 + 4), keep)
    paste_text(canvas, pixel_text('10 GOLD BARS', gap=2), cx, PANEL_Y1 + 62, '#ffffff')
    paste_text(canvas, pixel_text('AT HOLOGRAM POD', gap=2), cx, PANEL_Y1 + 94, MINT)
    for n, (_, _, _, x0, x1, lines) in enumerate(STEPS, 1):
        c = (x0 + x1) // 2
        badge(canvas, n, c, PANEL_Y1 + 30)
        for k, line in enumerate(lines):
            paste_text(canvas, pixel_text(line, gap=2), c, PANEL_Y1 + 62 + k * 32, '#ffffff' if k == 0 else MINT)

    covers.banner(canvas, 'WORMHOLE POTION', 'A ONE-USE TRIP HOME, AND A WAY BACK')
    return canvas


if __name__ == '__main__':
    args = sys.argv[1:]
    out_dir = covers.REPO / 'build/artwork-review/2026-11-04'
    if '--out' in args:
        out_dir = Path(args[args.index('--out') + 1])
    out_dir.mkdir(parents=True, exist_ok=True)
    c = compose()
    # Approved by Sid on 2026-11-04: also written to the release cover the publisher uses.
    for out in (out_dir / 'WormholePotion.png', covers.REPO / 'release/wormholepotion_logo.png'):
        c.save(out, optimize=True)
        print(out, c.size, out.stat().st_size)
