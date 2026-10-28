"""Runic Storage Workshop cover built from the mod's own runtime sprites (no AI, no game assets).

    python release/art_direction/make_runicstorage_cover.py [out.png]

Logical canvas 256x144 drawn at the game's pixel scale; objects are the shipped 16 px sprites
(ConnectedStorage/Unity/Art) at 2x, scene and banner are original pixels. Nearest 4x -> 1024x576.
Gem, screen and lantern light come from the sprites' emissive masks, blurred at output size.
"""
from pathlib import Path
import random
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

REPO = Path(__file__).resolve().parents[2]
ART = REPO / 'ConnectedStorage/Unity/Art'
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'build/artwork-review/2026-10-28/runicstorage_cover.png'
W, H, K, S = 256, 144, 4, 2  # logical size, output scale, sprite scale
R = random.Random(2810)
TIERS = ['Copper', 'Tin', 'Iron', 'Scarlet', 'Octarine', 'Galaxite', 'Solarite', 'Pandorium']

base = Image.new('RGBA', (W, H), '#0d1018')
glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(base)
g = ImageDraw.Draw(glow)

# Cave wall: irregular stone blocks, darker toward the top, with teal crystal veins.
for y in range(0, 64, 6):
    x = -R.randrange(0, 10)
    while x < W:
        w = R.randrange(9, 19)
        shade = R.choice(['#1a1f2b', '#1e2432', '#222a39', '#192130'])
        d.rectangle((x, y, x + w - 1, y + 5), fill=shade)
        d.line((x, y, x + w - 2, y), fill='#2b3446')
        d.line((x + w - 1, y + 1, x + w - 1, y + 5), fill='#10141d')
        x += w
for _ in range(7):
    cx, cy = R.randrange(8, W - 8), R.randrange(8, 52)
    for i in range(R.randrange(2, 4)):
        px, py = cx + i * 2, cy - i * 2
        d.line((px, py, px, py + 3), fill='#19bdc6')
        d.point((px, py), fill='#94f7dd')
        g.point((px, py + 1), fill=(25, 189, 198, 255))
# Wall foot: shadowed ledge into the floor.
d.rectangle((0, 62, W, 65), fill='#0a0d14')
d.line((0, 62, W, 62), fill='#2b3446')

# Floor: blue-grey stone tiles (16 px) with mortar, wear and a faint rune circle under the network.
for ty in range(66, H, 16):
    for tx in range(0, W + 16, 16):
        off = 8 if (ty // 16) % 2 else 0
        x0 = tx - off
        d.rectangle((x0, ty, x0 + 15, ty + 15), fill=R.choice(['#3a4558', '#3e4a5e', '#36404f']))
        d.line((x0, ty, x0 + 15, ty), fill='#4c5a70')
        d.line((x0, ty, x0, ty + 15), fill='#262d3a')
        for _ in range(3):
            d.point((x0 + R.randrange(2, 14), ty + R.randrange(2, 14)), fill=R.choice(['#323b4a', '#46536a']))
d.ellipse((30, 70, 190, 140), outline='#2a6e70')
d.ellipse((34, 73, 186, 137), outline='#1d4d50')
g.ellipse((30, 70, 190, 140), outline=(25, 140, 150, 140))

# Wall lanterns with warm glow.
for lx in (22, 232):
    d.rectangle((lx - 2, 30, lx + 2, 37), fill='#3b2a18')
    d.rectangle((lx - 1, 31, lx + 1, 35), fill='#ffcf6a')
    d.point((lx, 32), fill='#fff4c8')
    d.line((lx, 26, lx, 29), fill='#5a4024')
    g.ellipse((lx - 18, 16, lx + 18, 52), fill=(255, 170, 60, 70))
    g.ellipse((lx - 6, 27, lx + 6, 39), fill=(255, 200, 110, 200))


def sprite(name):
    im = Image.open(ART / (name + '.png')).convert('RGBA')
    em = Image.open(ART / (name + '_emissive.png')).convert('RGBA')
    return im.resize((im.width * S, im.height * S), Image.NEAREST), em.resize((em.width * S, em.height * S), Image.NEAREST)


def place(name, x, y):
    im, em = sprite(name)
    shadow = Image.new('RGBA', (im.width, 6), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((2, 0, im.width - 3, 5), fill=(0, 0, 0, 110))
    base.alpha_composite(shadow, (x, y + im.height - 4))
    base.alpha_composite(im, (x, y))
    glow.alpha_composite(em, (x, y))


# Network: 8 units edge to edge (two rows of four), terminal docked on the right, workbench nearby.
bx, by = 30, 72
for i, tier in enumerate(TIERS):
    place(tier + 'Chest', bx + (i % 4) * 32, by + (i // 4) * 32)
place('Terminal', bx + 128, by + 16)
place('Workbench', 206, 96)

# Props: copper and tin bars waiting at the bench, moss along the wall foot, wall crystal clusters.
for i, (dark, mid, light) in enumerate([('#824223', '#c16436', '#fca895'), ('#5a6c84', '#a1aedd', '#e2eef8'), ('#824223', '#c16436', '#fca895')]):
    x, y = 202 + i * 9 - (i == 2) * 4, 132 - (i == 2) * 4
    d.polygon([(x, y + 3), (x + 2, y), (x + 8, y), (x + 9, y + 3)], fill=mid)
    d.line((x + 2, y, x + 7, y), fill=light)
    d.line((x, y + 3, x + 9, y + 3), fill=dark)
for x in range(0, W, 3):
    if R.random() < .35:
        d.point((x, 66), fill=R.choice(['#239029', '#5ca537']))
        d.point((x + 1, 65), fill='#0f5630')
for cx, cy in ((246, 58), (8, 56), (150, 60)):
    for dx, h in ((-2, 4), (0, 7), (2, 5)):
        d.line((cx + dx, cy - h, cx + dx, cy), fill='#4ad8ae')
        d.point((cx + dx, cy - h), fill='#eafff8')
        g.line((cx + dx, cy - h, cx + dx, cy), fill=(74, 216, 174, 200))

# Banner: teal cloth on a brass rod, title in gold with a dark outline.
bx0, bx1, by0, by1 = 38, 218, 5, 45
d.rectangle((bx0 - 4, by0 - 2, bx1 + 4, by0), fill='#915b26')
d.line((bx0 - 4, by0 - 2, bx1 + 4, by0 - 2), fill='#e2a12c')
d.polygon([(bx0, by0 + 1), (bx1, by0 + 1), (bx1, by1), (bx1 - 10, by1 - 5), (128, by1), (bx0 + 10, by1 - 5), (bx0, by1)], fill='#0f4a48')
d.polygon([(bx0 + 2, by0 + 2), (bx1 - 2, by0 + 2), (bx1 - 2, by1 - 3), (bx1 - 10, by1 - 7), (128, by1 - 2), (bx0 + 10, by1 - 7), (bx0 + 2, by1 - 3)], outline='#2a8a80')
for x in range(bx0 + 4, bx1 - 3, 6):
    d.point((x, by0 + 3), fill='#1d6a64')


def text(s, size, y, fill, font='impact.ttf'):
    f = ImageFont.truetype('C:/Windows/Fonts/' + font, size)
    layer = Image.new('L', (W, H))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    w = ld.textlength(s, font=f)
    ld.text(((W - w) / 2, y), s, font=f, fill=255)
    outline = layer.filter(ImageFilter.MaxFilter(3))
    base.paste('#1a0f08', (0, 0), outline)
    base.paste(fill, (0, 0), layer)
    return layer


title = text('RUNIC STORAGE', 22, 7, '#ffd36a')
# Two-tone gold: the upper rows of the letters are lighter.
band = Image.new('L', (W, H))
band.paste(255, (0, 0, W, 17))
base.paste('#fff0b8', (0, 0), ImageChops.multiply(title, band))
text('CRAFT  \u2022  UPGRADE  \u2022  EXPAND', 9, 32, '#94f7dd', 'segoeuib.ttf')

# Output: nearest 4x, then additive bloom from the emissive layer.
out = base.resize((W * K, H * K), Image.NEAREST).convert('RGB')
bloom = glow.resize((W * K, H * K), Image.NEAREST)
soft = Image.new('RGB', out.size)
soft.paste(bloom.convert('RGB'), (0, 0), bloom)
soft = soft.filter(ImageFilter.GaussianBlur(10))
out = ImageChops.add(out, soft.point(lambda v: int(v * .9)))
OUT.parent.mkdir(parents=True, exist_ok=True)
out.save(OUT, optimize=True)
print(OUT, out.size, OUT.stat().st_size)
