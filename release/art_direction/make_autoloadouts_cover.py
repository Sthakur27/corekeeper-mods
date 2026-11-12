"""Auto Loadout Switch (folder AutoLoadouts) cover, from Sid's 2026-11-11 HDR captures.

    python release/art_direction/make_autoloadouts_cover.py [out.png]

Top: the player with the mod's "Linked to loadout III" emote bubble (capture 21.53.11.05).
Bottom: the hotbar from the same capture, with a gold frame on the held slot.
Background: the same capture, blurred and darkened. Panels, frames and the banner follow
make_screenshot_covers.py; game UI is never repainted.
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as sc  # noqa: E402

W, H, TOP = sc.W, sc.H, sc.TOP
REPO = sc.REPO
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'release/autoloadouts_logo.png'
GOLD, GLOW = '#e8b85a', '#ffb347'
GAIN = 1.4                          # HDR tone-mapped captures are dark

SHOT = 'hdr:21.53.11.05'
SCENE = (467, 227, 827, 427)        # bubble + player + pet (1280x720 preview pixels)
HOTBAR = (310, 641, 817, 699)       # slots 1-8 (the tooltip starts right of slot 8)
HELD = (760, 643, 813, 697)         # slot 8, the held torch


def lit(img):
    return ImageEnhance.Brightness(img).enhance(GAIN)


def shadowed(canvas, img, x, y):
    sh = Image.new('L', (W, H), 0)
    ImageDraw.Draw(sh).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
    canvas.paste('#000000', (0, 0), sh.filter(ImageFilter.GaussianBlur(12)))
    canvas.paste(img, (x, y))


def frames(canvas, rects, radius=10, width=3):
    g = Image.new('L', (W, H), 0)
    for r in rects:
        ImageDraw.Draw(g).rounded_rectangle(r, radius + 2, outline=255, width=10)
    g = g.filter(ImageFilter.GaussianBlur(10))
    canvas = Image.composite(Image.new('RGB', (W, H), GLOW), canvas, g.point(lambda v: v // 3))
    d = ImageDraw.Draw(canvas)
    for r in rects:
        d.rounded_rectangle(r, radius, outline=GOLD, width=width)
    return canvas


def place(img, box, scale, x_center, y):
    # crops come back at full capture resolution; scale is relative to the 1280x720 preview box
    im = img.resize((round((box[2] - box[0]) * scale), round((box[3] - box[1]) * scale)), Image.LANCZOS)
    return im, x_center - im.width // 2, y


bg = sc.crop(SHOT, (200, 100, 1100, 606)).resize((W, H), Image.LANCZOS)
canvas = ImageEnhance.Brightness(bg.filter(ImageFilter.GaussianBlur(6))).enhance(0.45 * GAIN)

scene, sx, sy = place(lit(sc.crop(SHOT, SCENE)), SCENE, 1.85, W // 2, TOP + 12)
shadowed(canvas, scene, sx, sy)

bar_scale = 1.9
bar, bx, by = place(lit(sc.crop(SHOT, HOTBAR)), HOTBAR, bar_scale, W // 2, sy + scene.height + 22)
shadowed(canvas, bar, bx, by)

hx0 = bx + round((HELD[0] - HOTBAR[0]) * bar_scale)
hy0 = by + round((HELD[1] - HOTBAR[1]) * bar_scale)
held = (hx0 - 6, hy0 - 6, hx0 + round((HELD[2] - HELD[0]) * bar_scale) + 6, hy0 + round((HELD[3] - HELD[1]) * bar_scale) + 6)
canvas = frames(canvas, [held])

sc.banner(canvas, 'AUTO LOADOUT SWITCH', 'HOLD A LINKED ITEM AND YOUR GEAR SWAPS TO ITS LOADOUT')
OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT, optimize=True)
print(OUT, canvas.size, OUT.stat().st_size)
