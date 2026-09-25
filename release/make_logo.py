"""Generate a 1280x720 mod.io listing image. Usage: python make_logo.py "TITLE" "tagline" "subline" out.png"""
import sys
from PIL import Image, ImageDraw, ImageFont
title, tag, sub, out = (sys.argv + ["LOADOUT FALLBACK", "Empty slot in loadout 2 or 3?  It falls through to loadout 1.", "Every slot: armor, rings, necklace, off-hand, bag, lantern, pet", "logo.png"])[1:5]
W, H = 1280, 720
img = Image.new("RGB", (W, H)); d = ImageDraw.Draw(img)
for y in range(H):
    c = int(24 + 18 * y / H); d.line([(0, y), (W, y)], fill=(c, int(c * 0.85), c + 14))
def font(sz):
    for f in ["C:/Windows/Fonts/consolab.ttf", "C:/Windows/Fonts/arialbd.ttf"]:
        try: return ImageFont.truetype(f, sz)
        except Exception: pass
    return ImageFont.load_default()
slot, gap = 150, 40; x0 = (W - (3 * slot + 2 * gap)) // 2; y0 = 250
for i in range(3):
    x = x0 + i * (slot + gap)
    d.rounded_rectangle([x, y0, x + slot, y0 + slot], radius=18, fill=(58, 50, 78), outline=(140, 120, 190), width=6)
    a = 255 if i == 0 else 110
    hel = Image.new("RGBA", (slot, slot), (0, 0, 0, 0)); hd = ImageDraw.Draw(hel)
    hd.pieslice([25, 30, slot - 25, slot + 20], 180, 360, fill=(220, 200, 120, a))
    hd.rectangle([25, slot // 2 + 5, slot - 25, slot // 2 + 30], fill=(220, 200, 120, a))
    hd.rectangle([55, slot // 2 + 12, slot - 55, slot // 2 + 24], fill=(40, 30, 50, a))
    img.paste(hel, (x, y0), hel)
    d.text((x + slot // 2, y0 + slot + 22), str(i + 1), fill=(200, 190, 230), font=font(40), anchor="mt")
for i in range(2):
    x = x0 + (i + 1) * slot + i * gap + 8
    d.polygon([(x, y0 + slot // 2 - 10), (x + gap - 16, y0 + slot // 2), (x, y0 + slot // 2 + 10)], fill=(140, 120, 190))
d.text((W // 2, 110), title, fill=(245, 240, 255), font=font(84), anchor="mt")
d.text((W // 2, 520), tag, fill=(190, 180, 215), font=font(34), anchor="mt")
d.text((W // 2, 580), sub, fill=(150, 140, 175), font=font(28), anchor="mt")
img.save(out); print("wrote", out)
