"""Slot Organizer Workshop cover: original pixel art drawn in code (no AI, no game assets).

    python release/art_direction/make_slotorganizer_cover.py [out.png]

Logical canvas 256x144, nearest 4x -> 1024x576, same cave/banner language as the Runic Storage
cover. Scene: a carved slot board like the world/character select list, each row with its number,
a world or character, the trash can and the mod's up/down arrows; slot 3 is lifted mid-swap with
slot 2. Light from lanterns, the selection and the arrows comes from a blurred emissive layer.
"""
from pathlib import Path
import random
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

REPO = Path(__file__).resolve().parents[2]
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'build/artwork-review/2026-10-28/slotorganizer_cover.png'
W, H, K = 256, 144, 4
R = random.Random(2811)

base = Image.new('RGBA', (W, H), '#0d1018')
glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(base)
g = ImageDraw.Draw(glow)

# Cave wall: irregular stone blocks with teal crystal veins.
for y in range(0, 64, 6):
    x = -R.randrange(0, 10)
    while x < W:
        w = R.randrange(9, 19)
        d.rectangle((x, y, x + w - 1, y + 5), fill=R.choice(['#1a1f2b', '#1e2432', '#222a39', '#192130']))
        d.line((x, y, x + w - 2, y), fill='#2b3446')
        d.line((x + w - 1, y + 1, x + w - 1, y + 5), fill='#10141d')
        x += w
for cx, cy in [(14, 40), (238, 46), (20, 58), (232, 22)]:
    for i in range(3):
        px, py = cx + i * 2, cy - i * 2
        d.line((px, py, px, py + 3), fill='#19bdc6')
        d.point((px, py), fill='#94f7dd')
        g.point((px, py + 1), fill=(25, 189, 198, 255))
d.rectangle((0, 62, W, 65), fill='#0a0d14')
d.line((0, 62, W, 62), fill='#2b3446')

# Floor tiles.
for ty in range(66, H, 16):
    for tx in range(0, W + 16, 16):
        x0 = tx - (8 if (ty // 16) % 2 else 0)
        d.rectangle((x0, ty, x0 + 15, ty + 15), fill=R.choice(['#3a4558', '#3e4a5e', '#36404f']))
        d.line((x0, ty, x0 + 15, ty), fill='#4c5a70')
        d.line((x0, ty, x0, ty + 15), fill='#262d3a')
        for _ in range(3):
            d.point((x0 + R.randrange(2, 14), ty + R.randrange(2, 14)), fill=R.choice(['#323b4a', '#46536a']))
# Moss along the wall foot.
for x in range(0, W, 3):
    if R.random() < .45:
        d.point((x, 65), fill=R.choice(['#3f7a3a', '#5a9a44']))


def lantern(x, y):
    d.line((x + 2, y - 6, x + 2, y - 1), fill='#5a4630')
    d.rectangle((x, y, x + 4, y + 6), fill='#6b4a22')
    d.rectangle((x + 1, y + 1, x + 3, y + 5), fill='#ffcf6a')
    d.point((x + 2, y + 3), fill='#fff4c8')
    g.ellipse((x - 10, y - 8, x + 14, y + 16), fill=(255, 170, 70, 120))
    g.rectangle((x + 1, y + 1, x + 3, y + 5), fill=(255, 210, 120, 255))


lantern(14, 20)
lantern(237, 20)

# ---------------------------------------------------------------- slot board
BX0, BY0, BX1, BY1 = 34, 50, 222, 140
d.rectangle((BX0 + 3, BY0 + 3, BX1 + 3, BY1 + 3), fill='#080a10')          # shadow
d.rectangle((BX0, BY0, BX1, BY1), fill='#5b3b22')                          # timber frame
d.rectangle((BX0 + 1, BY0 + 1, BX1 - 1, BY1 - 1), outline='#7a5230')
d.rectangle((BX0 + 3, BY0 + 3, BX1 - 3, BY1 - 3), fill='#1b2030')          # inset
for x in range(BX0 + 4, BX1 - 3, 9):                                       # wood grain
    d.point((x, BY0 + 2), fill='#4a2f1b')
    d.point((x + 4, BY1 - 2), fill='#4a2f1b')
for cx, cy in [(BX0 + 2, BY0 + 2), (BX1 - 2, BY0 + 2), (BX0 + 2, BY1 - 2), (BX1 - 2, BY1 - 2)]:
    d.point((cx, cy), fill='#d9b25a')                                      # brass rivets

ROW_H, GAP = 18, 3
ROW_X0, ROW_X1 = BX0 + 7, BX1 - 7


def digit(s, x, y, fill):
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 10)
    layer = Image.new('L', (W, H))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    w = ld.textlength(s, font=f)
    ld.text((x - w / 2, y), s, font=f, fill=255)
    base.paste(fill, (0, 0), layer)


def world_icon(x, y):
    # Small round world: moss and stone with a glowing core crystal.
    d.ellipse((x, y, x + 11, y + 11), fill='#2f6a3a')
    d.ellipse((x + 1, y + 1, x + 10, y + 10), fill='#3f8a45')
    d.rectangle((x + 2, y + 6, x + 6, y + 8), fill='#5a6474')
    d.rectangle((x + 6, y + 2, x + 9, y + 4), fill='#5a6474')
    d.point((x + 3, y + 3), fill='#7cc06a')
    d.rectangle((x + 5, y + 5, x + 6, y + 6), fill='#5ff0e0')
    g.rectangle((x + 4, y + 4, x + 7, y + 7), fill=(60, 220, 210, 200))


def character_icon(x, y, hair='#7a4a2a', shirt='#3c6fb0'):
    d.rectangle((x + 2, y, x + 8, y + 2), fill=hair)                       # hair
    d.rectangle((x + 2, y + 3, x + 8, y + 7), fill='#f0c49a')              # face
    d.point((x + 2, y + 3), fill=hair)
    d.point((x + 8, y + 3), fill=hair)
    d.point((x + 4, y + 5), fill='#1a1a22')
    d.point((x + 6, y + 5), fill='#1a1a22')
    d.rectangle((x + 1, y + 8, x + 9, y + 11), fill=shirt)                 # body
    d.line((x + 1, y + 8, x + 9, y + 8), fill='#6a96d0' if shirt == '#3c6fb0' else '#c9a2e0')


def name_bars(x, y, widths, fill):
    # Unreadable "text" strokes; the cover does not invent world names.
    cx = x
    for w in widths:
        d.rectangle((cx, y, cx + w - 1, y + 1), fill=fill)
        cx += w + 2


def trash(x, y, a):
    c = '#aab3c4' if a else '#5e6676'
    d.rectangle((x + 1, y, x + 5, y), fill=c)
    d.rectangle((x, y + 1, x + 6, y + 1), fill=c)
    d.rectangle((x + 1, y + 3, x + 5, y + 8), fill=c)
    for i in (2, 4):
        d.line((x + i, y + 4, x + i, y + 7), fill='#1b2030')


def arrow(x, y, up, lit):
    # Button with a filled triangle, matching the in-game look of the mod's buttons.
    d.rectangle((x, y, x + 10, y + 10), fill='#2a3346' if not lit else '#3a4a3a')
    d.rectangle((x, y, x + 10, y + 10), outline='#4c5a70' if not lit else '#ffd36a')
    col = '#94f7dd' if not lit else '#fff0b8'
    for i in range(4):
        yy = (y + 3 + i) if up else (y + 7 - i)
        d.line((x + 5 - i, yy, x + 5 + i, yy), fill=col)
    if lit:
        g.rectangle((x - 2, y - 2, x + 12, y + 12), fill=(255, 200, 90, 150))


def row(i, kind, dx=0, dy=0, selected=False):
    y = BY0 + 6 + i * (ROW_H + GAP) + dy
    x0, x1 = ROW_X0 + dx, ROW_X1 + dx
    if selected:
        d.rectangle((x0 + 2, y + 2, x1 + 2, y + ROW_H + 2), fill='#05070b')
    d.rectangle((x0, y, x1, y + ROW_H), fill='#2b3347' if not selected else '#33405a')
    d.line((x0, y, x1, y), fill='#3d4862' if not selected else '#ffd36a')
    d.line((x0, y + ROW_H, x1, y + ROW_H), fill='#151a26' if not selected else '#ffd36a')
    if selected:
        d.line((x0, y, x0, y + ROW_H), fill='#ffd36a')
        d.line((x1, y, x1, y + ROW_H), fill='#ffd36a')
        g.rectangle((x0 - 3, y - 3, x1 + 3, y + ROW_H + 3), outline=(255, 190, 80, 170), width=2)
    # Number tab
    d.rectangle((x0 + 2, y + 2, x0 + 14, y + ROW_H - 2), fill='#1b2030')
    digit(str(i + 1), x0 + 8, y + 3, '#ffd36a' if selected else '#c9d2e0')
    # Content
    ix = x0 + 20
    if kind == 'world':
        world_icon(ix, y + 3)
        name_bars(ix + 16, y + 5, [14, 9, 18], '#e7ecf4')
        name_bars(ix + 16, y + 10, [7, 22], '#7c879c')
        for k in range(4):                                                  # crystal pips
            d.point((ix + 70 + k * 4, y + 11), fill=['#5ff0e0', '#ff8a5a', '#ffd36a', '#b08cff'][k])
    elif kind == 'character':
        character_icon(ix, y + 3, '#c9a24a', '#8a4fb0')
        name_bars(ix + 16, y + 5, [11, 16], '#e7ecf4')
        d.rectangle((ix + 16, y + 10, ix + 52, y + 11), fill='#5a2a2a')      # health bar
        d.rectangle((ix + 16, y + 10, ix + 44, y + 11), fill='#e0464a')
    elif kind == 'character2':
        character_icon(ix, y + 3)
        name_bars(ix + 16, y + 5, [15, 10], '#e7ecf4')
        d.rectangle((ix + 16, y + 10, ix + 52, y + 11), fill='#5a2a2a')
        d.rectangle((ix + 16, y + 10, ix + 50, y + 11), fill='#e0464a')
    else:
        d.rectangle((ix + 3, y + 7, ix + 9, y + 8), fill='#4c5a70')          # empty slot "+"
        d.rectangle((ix + 5, y + 5, ix + 6, y + 10), fill='#4c5a70')
        name_bars(ix + 16, y + 7, [12, 9], '#4c5a70')
    # Trash + the mod's arrows (empty slots have none, like in game)
    if kind != 'empty':
        trash(x1 - 37, y + 4, selected)
        arrow(x1 - 27, y + 4, True, selected)
        arrow(x1 - 14, y + 4, False, False)
    return y


row(0, 'world')
row(1, 'character2')
row(3, 'empty')
y3 = row(2, 'character', dx=6, dy=-3, selected=True)     # lifted, on its way up

# Swap arrows left of the board: slot 3 goes up, slot 2 comes down.
sx = BX0 - 14
ya, yb = BY0 + 6 + 1 * (ROW_H + GAP) + 9, BY0 + 6 + 2 * (ROW_H + GAP) + 9
for x in (sx, sx + 1):
    d.line((x, ya, x, yb), fill='#ffd36a')
d.line((sx + 1, ya, sx + 10, ya), fill='#ffd36a')                          # into slot 2 (up)
d.line((sx + 1, ya + 1, sx + 10, ya + 1), fill='#ffd36a')
for i in range(4):
    d.line((sx + 7 + i, ya - 3 + i, sx + 7 + i, ya + 4 - i), fill='#ffd36a')
d.line((sx + 1, yb, sx + 10, yb), fill='#94f7dd')                          # into slot 3 (down)
d.line((sx + 1, yb + 1, sx + 10, yb + 1), fill='#94f7dd')
for i in range(4):
    d.line((sx + 7 + i, yb - 3 + i, sx + 7 + i, yb + 4 - i), fill='#94f7dd')
g.line((sx, ya, sx, yb), fill=(255, 200, 90, 200), width=3)

# ---------------------------------------------------------------- banner
bx0, bx1, by0, by1 = 44, 212, 4, 44
d.line((bx0 - 6, by0, bx1 + 6, by0), fill='#b8863a')                       # brass rod
d.line((bx0 - 6, by0 + 1, bx1 + 6, by0 + 1), fill='#7a5520')
d.polygon([(bx0, by0 + 2), (bx1, by0 + 2), (bx1, by1), (bx1 - 8, by1 - 4), (bx0 + 8, by1 - 4), (bx0, by1)], fill='#164a46')
d.polygon([(bx0, by0 + 2), (bx1, by0 + 2), (bx1, by1), (bx1 - 8, by1 - 4), (bx0 + 8, by1 - 4), (bx0, by1)], outline='#2a8a80')
for x in range(bx0 + 4, bx1 - 3, 6):
    d.point((x, by0 + 4), fill='#1d6a64')


def text(s, size, y, fill, font='impact.ttf'):
    f = ImageFont.truetype('C:/Windows/Fonts/' + font, size)
    layer = Image.new('L', (W, H))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    w = ld.textlength(s, font=f)
    ld.text(((W - w) / 2, y), s, font=f, fill=255)
    base.paste('#1a0f08', (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
    base.paste(fill, (0, 0), layer)
    return layer


title = text('SLOT ORGANIZER', 21, 6, '#ffd36a')
band = Image.new('L', (W, H))
band.paste(255, (0, 0, W, 16))
base.paste('#fff0b8', (0, 0), ImageChops.multiply(title, band))
text('MOVE  WORLDS  &  CHARACTERS', 9, 30, '#94f7dd', 'segoeuib.ttf')

out = base.resize((W * K, H * K), Image.NEAREST).convert('RGB')
bloom = glow.resize((W * K, H * K), Image.NEAREST)
soft = Image.new('RGB', out.size)
soft.paste(bloom.convert('RGB'), (0, 0), bloom)
soft = soft.filter(ImageFilter.GaussianBlur(10))
out = ImageChops.add(out, soft.point(lambda v: int(v * .9)))
OUT.parent.mkdir(parents=True, exist_ok=True)
out.save(OUT, optimize=True)
print(OUT, out.size, OUT.stat().st_size)
