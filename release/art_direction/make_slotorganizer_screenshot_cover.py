"""Slot Organizer Workshop cover from Sid's in-game screenshot of the Select World menu.

    python release/art_direction/make_slotorganizer_screenshot_cover.py [out.png]

Source: release/art_direction/slotorganizer_source.png (crop of a 3840x2160 NVIDIA capture,
2026-10-28, rows 1-4 with the mod's up/down buttons and the cursor on slot 2's up arrow).
Adds a pixel title banner on top and a soft glow behind the button column; the UI itself is
not repainted.
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

REPO = Path(__file__).resolve().parents[2]
SRC = REPO / 'release/art_direction/slotorganizer_source.png'
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'build/artwork-review/2026-10-28/slotorganizer_screenshot_cover.png'
W, H = 1280, 720

shot = Image.open(SRC).convert('RGB')
scale = (W - 40) / shot.width
shot = shot.resize((round(shot.width * scale), round(shot.height * scale)), Image.LANCZOS)
sx, sy = (W - shot.width) // 2, H - shot.height - 18

# Background: the menu's deep navy with a faint vignette so the banner area is not flat black.
canvas = Image.new('RGB', (W, H), '#000101')   # the menu's own backdrop colour
haze = Image.new('L', (W, H), 0)
ImageDraw.Draw(haze).ellipse((-200, -260, W + 200, 360), fill=70)
haze = haze.filter(ImageFilter.GaussianBlur(80))
canvas.paste('#16264a', (0, 0), haze)

# Glow behind the up/down column (x 1100..1235 of the 1280-wide preview -> source crop space).
glow = Image.new('L', (W, H), 0)
col_x0 = sx + round((1100 * 3 - 600) * scale) - 10
col_x1 = sx + round((1235 * 3 - 600) * scale) + 10
ImageDraw.Draw(glow).rounded_rectangle((col_x0, sy + 4, col_x1, sy + shot.height - 4), 18, fill=150)
glow = glow.filter(ImageFilter.GaussianBlur(22))
# Feathered paste so the screenshot has no hard edge against the backdrop.
fade = Image.new('L', shot.size, 0)
ImageDraw.Draw(fade).rectangle((14, 14, shot.width - 15, shot.height - 15), fill=255)
canvas.paste(shot, (sx, sy), fade.filter(ImageFilter.GaussianBlur(8)))
light = Image.new('RGB', (W, H), '#ffb347')
canvas = Image.composite(ImageChops.screen(canvas, light), canvas, glow.point(lambda v: v // 4))
# Thin gold frame around that column.
d = ImageDraw.Draw(canvas)
d.rounded_rectangle((col_x0, sy + 4, col_x1, sy + shot.height - 4), 10, outline='#e8b85a', width=3)

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
    ld.text(((bw - w) / 2, y), s, font=f, fill=255)
    ban.paste('#1a0f08', (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
    ban.paste(fill, (0, 0), layer)
    return layer


title = text('SLOT ORGANIZER', 21, 7, '#ffd36a', 'impact.ttf')
band = Image.new('L', (bw, bh))
band.paste(255, (0, 0, bw, 17))
ban.paste('#fff0b8', (0, 0), ImageChops.multiply(title, band))
text('MOVE   WORLDS   &   CHARACTERS   UP   OR   DOWN', 8, 30, '#94f7dd', 'segoeuib.ttf')
ban = ban.resize((bw * P, bh * P), Image.NEAREST)
canvas.paste(ban, (0, 0), ban)

OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT, optimize=True)
print(OUT, canvas.size, OUT.stat().st_size)
