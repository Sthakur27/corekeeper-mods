"""Slot Manager (folder SlotOrganizer) cover from Sid's in-game screenshots, 2026-11-05.

    python release/art_direction/make_slotmanager_cover.py [out.png]

Sources: HDR captures tone-mapped by hdr_to_png.py into build/artwork-review/2026-11-05/hdr
(11.16.10.29 = Select World, cursor on slot 2's copy button; 11.16.01.55 = Select Character,
cursor on slot 2's edit button). Two crops (rows 1-2 of each menu) are stacked under the usual
teal/gold pixel title banner; gold frames mark the mod's button grid. The game UI is not repainted.
"""
from pathlib import Path
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

REPO = Path(__file__).resolve().parents[2]
HDR = REPO / 'build/artwork-review/2026-11-05/hdr'
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'build/artwork-review/2026-11-05/slotmanager_cover.png'
W, H, TOP = 1280, 720, 176
S = 3   # captures are 3840x2160; coordinates below are 1280x720 preview pixels

# (stamp, crop box, button-grid box) in preview pixels
PANELS = [('11.16.10.29', (225, 137, 1250, 342), (1096, 142, 1243, 337)),
          ('11.16.01.55', (225, 136, 1250, 328), (1096, 137, 1243, 326))]

world = Image.open(HDR / '11.16.10.29.png').convert('RGB')
bg = world.crop((300 * S, 100 * S, 1060 * S, 527 * S)).resize((W, H), Image.LANCZOS)
bg = bg.filter(ImageFilter.GaussianBlur(5))
canvas = Image.eval(bg, lambda v: v * 45 // 100)
haze = Image.new('L', (W, H), 0)
ImageDraw.Draw(haze).ellipse((-200, -260, W + 200, 360), fill=60)
canvas.paste('#16264a', (0, 0), haze.filter(ImageFilter.GaussianBlur(80)))

x0, gap, y = 20, 14, TOP + 16
pw = W - 2 * x0
frames = []
for stamp, (cx0, cy0, cx1, cy1), (gx0, gy0, gx1, gy1) in PANELS:
    src = Image.open(HDR / f'{stamp}.png').convert('RGB')
    crop = src.crop((cx0 * S, cy0 * S, cx1 * S, cy1 * S))
    k = pw / crop.width
    crop = crop.resize((pw, round(crop.height * k)), Image.LANCZOS)
    shadow = Image.new('L', (W, H), 0)
    ImageDraw.Draw(shadow).rectangle((x0 + 8, y + 10, x0 + pw + 8, y + crop.height + 10), fill=200)
    canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(12)))
    canvas.paste(crop, (x0, y))
    k3 = k * S   # preview pixel -> canvas pixel
    frames.append((x0 + (gx0 - cx0) * k3, y + (gy0 - cy0) * k3,
                   x0 + (gx1 - cx0) * k3, y + (gy1 - cy0) * k3))
    y += crop.height + gap

# Gold highlight frames with a soft glow around each button grid.
glow = Image.new('L', (W, H), 0)
gd = ImageDraw.Draw(glow)
for f in frames:
    gd.rounded_rectangle(f, 10, outline=255, width=10)
glow = glow.filter(ImageFilter.GaussianBlur(10))
canvas = Image.composite(ImageChops.screen(canvas, Image.new('RGB', (W, H), '#ffb347')), canvas,
                         glow.point(lambda v: v * 2 // 3))
d = ImageDraw.Draw(canvas)
for f in frames:
    d.rounded_rectangle(f, 10, outline='#e8b85a', width=3)

# Title banner, drawn at quarter scale and upscaled with nearest for a pixel look.
P = 4
bw, bh = W // P, 46
ban = Image.new('RGBA', (bw, bh), (0, 0, 0, 0))
b = ImageDraw.Draw(ban)
bx0, bx1 = 34, bw - 34
b.line((bx0 - 6, 2, bx1 + 6, 2), fill='#b8863a')
b.line((bx0 - 6, 3, bx1 + 6, 3), fill='#7a5520')
poly = [(bx0, 4), (bx1, 4), (bx1, 42), (bx1 - 8, 38), (bx0 + 8, 38), (bx0, 42)]
b.polygon(poly, fill='#164a46')
b.polygon(poly, outline='#2a8a80')
for x in range(bx0 + 4, bx1 - 3, 6):
    b.point((x, 6), fill='#1d6a64')


def text(s, size, y, fill, font):
    f = ImageFont.truetype('C:/Windows/Fonts/' + font, size)
    layer = Image.new('L', (bw, bh))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    w = ld.textlength(s, font=f)
    assert w <= bx1 - bx0 - 8, f'{s!r} too wide ({w})'
    ld.text(((bw - w) / 2, y), s, font=f, fill=255)
    ban.paste('#1a0f08', (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
    ban.paste(fill, (0, 0), layer)
    return layer


title = text('SLOT MANAGER', 21, 7, '#ffd36a', 'impact.ttf')
band = Image.new('L', (bw, bh))
band.paste(255, (0, 0, bw, 17))
ban.paste('#fff0b8', (0, 0), ImageChops.multiply(title, band))
text('REORDER,   COPY   &   EDIT   WORLDS   AND   CHARACTERS', 8, 30, '#94f7dd', 'segoeuib.ttf')
ban = ban.resize((bw * P, bh * P), Image.NEAREST)
canvas.paste(ban, (0, 0), ban)

OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT, optimize=True)
print(OUT, canvas.size, OUT.stat().st_size)
