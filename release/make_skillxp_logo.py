"""1280x720 mod.io listing image for Skill XP Multiplier: skill XP bars with per-skill multiplier tags."""
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1280, 720
OUT = "skillxp-logo.png"

def font(size, bold=True):
    for f in (["C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf"] if bold
              else ["C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"]):
        try:
            return ImageFont.truetype(f, size)
        except Exception:
            pass
    return ImageFont.load_default()

# Background: deep cave gradient with a soft radial glow.
img = Image.new("RGB", (W, H), (14, 12, 22))
d = ImageDraw.Draw(img)
for y in range(H):
    t = y / H
    d.line([(0, y), (W, y)], fill=(int(16 + 10 * t), int(12 + 8 * t), int(30 + 14 * t)))
glow = Image.new("RGB", (W, H), (0, 0, 0))
gd = ImageDraw.Draw(glow)
gd.ellipse([W // 2 - 520, -260, W // 2 + 520, 340], fill=(70, 48, 120))
glow = glow.filter(ImageFilter.GaussianBlur(120))
img = Image.blend(img, Image.composite(glow, img, glow.convert("L").point(lambda v: min(255, v * 2))), 0.55)
d = ImageDraw.Draw(img)

# Title block.
title = "SKILL XP MULTIPLIER"
tf = font(88)
tw = d.textlength(title, font=tf)
x0 = (W - tw) / 2
d.text((x0 + 4, 64), title, font=tf, fill=(30, 20, 50))            # shadow
d.text((x0, 60), title, font=tf, fill=(250, 236, 180))
sub = "A separate XP multiplier for every skill"
sf = font(34, bold=False)
d.text((W / 2, 172), sub, font=sf, fill=(200, 190, 225), anchor="mt")

# Skill bars: (name, fill fraction, multiplier, colour)
skills = [
    ("Mining",     0.85, "10x", (240, 170,  70)),
    ("Melee",      0.55,  "3x", (230,  90,  90)),
    ("Range",      0.40,  "5x", (120, 200, 110)),
    ("Magic",      0.95, "20x", (150, 120, 240)),
    ("Fishing",    0.20,  "1x", ( 90, 180, 230)),
    ("Gardening",  0.65,  "2x", (110, 210, 150)),
    ("Cooking",    0.30,  "0x", (160, 160, 170)),
    ("Explosives", 0.75, "50x", (250, 130,  60)),
]
bx, by = 120, 232
bw, bh, gap = 1040, 40, 11
lf = font(26)
mf = font(26)
for i, (name, frac, mult, col) in enumerate(skills):
    y = by + i * (bh + gap)
    # label
    d.text((bx, y + bh / 2), name, font=lf, fill=(235, 230, 245), anchor="lm")
    # track
    tx0, tx1 = bx + 170, bx + bw - 120
    d.rounded_rectangle([tx0, y + 6, tx1, y + bh - 6], radius=10, fill=(40, 34, 58), outline=(70, 60, 95), width=2)
    # fill with a lighter top highlight
    fx1 = tx0 + (tx1 - tx0) * frac
    if frac > 0:
        d.rounded_rectangle([tx0 + 2, y + 8, fx1, y + bh - 8], radius=8, fill=col)
        hi = tuple(min(255, c + 45) for c in col)
        d.rounded_rectangle([tx0 + 2, y + 8, fx1, y + 16], radius=6, fill=hi)
    # multiplier tag
    tagc = (110, 110, 120) if mult == "0x" else col
    tx, ty = tx1 + 18, y + bh / 2
    d.rounded_rectangle([tx, ty - 16, tx + 92, ty + 16], radius=8, fill=(26, 22, 40), outline=tagc, width=3)
    d.text((tx + 46, ty + 1), mult, font=mf, fill=tagc, anchor="mm")

# Footer.
ff = font(26, bold=False)
d.text((W / 2, 700), "0x freezes a skill  ·  1x vanilla  ·  up to 100x  ·  Mod Settings menu or  /xp <skill> <n>",
       font=ff, fill=(165, 155, 195), anchor="mb")

img.save(OUT)
print("wrote", OUT)
