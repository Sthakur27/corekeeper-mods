"""Runic Storage Workshop / mod.io cover (approved by Sid 2026-11-03).

    python release/art_direction/make_runicstorage_cover.py [out.png]

Top: title banner. Middle left: a terminal with storage units touching it, edges glowing where they
connect. Middle right: the storage panel from Sid's screenshot. Bottom: the upgrade path
(unit -> gem from 40 bars -> next unit) with slot counts. The network uses the mod's own sprites on a
floor patch tiled from the base screenshot. Gems and bars are drawn on the game's 16 px icon grid.
Bar icons are read from the installed game at render time and cached in ignored build/ (never committed).
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).parent))
import make_screenshot_covers as sc

REPO = sc.REPO
ART = REPO / 'ConnectedStorage/Unity/Art'
W, H = sc.W, sc.H
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / 'release/runicstorage_workshop.png'
GOLD, GLOW, MINT = '#e8b85a', '#ffb347', '#94f7dd'


def sprite(name, scale):
    im = Image.open(ART / f'{name}.png').convert('RGBA')
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def pixel_text(canvas, xy, s, size, fill, anchor='mm', scale=2):
    """1-bit text drawn small and scaled up, matching the banner's pixel look."""
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', size)
    tmp = ImageDraw.Draw(Image.new('L', (1, 1)))
    w, h = int(tmp.textlength(s, font=f)) + 4, size + 6
    layer = Image.new('L', (w, h))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    ld.text((2, 1), s, font=f, fill=255)
    layer = layer.resize((w * scale, h * scale), Image.NEAREST)
    x, y = xy
    if anchor == 'mm':
        x, y = x - layer.width // 2, y - layer.height // 2
    canvas.paste('#1a0f08', (x, y), layer.filter(ImageFilter.MaxFilter(5)))
    canvas.paste(fill, (x, y), layer)


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


BARS = REPO / 'build/artwork-review/bars'              # cache of lootsprite_<tier>Bar from the game
GAME_BUNDLE = Path(r'C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_all.bundle')


def bar_icon(tier):
    path = BARS / f'{tier.lower()}bar.png'
    if not path.exists():
        import UnityPy
        BARS.mkdir(parents=True, exist_ok=True)
        for o in UnityPy.load(str(GAME_BUNDLE)).objects:
            if o.type.name == 'Sprite' and o.peek_name() == f'lootsprite_{tier[0].lower()}{tier[1:]}Bar':
                o.read().image.save(path)
                break
    return Image.open(path)
P = 4                                                   # game pixel -> cover pixel in the strip


def game_res(im, size=16):
    """How the game draws an item icon: on its 16 px grid, hard alpha, then scaled up."""
    im = im.convert('RGBA')
    if im.width != size:
        im = im.resize((size, size), Image.BOX)
        im.putalpha(im.getchannel('A').point(lambda a: 255 if a >= 110 else 0))
    return im.resize((size * P, size * P), Image.NEAREST)


def upgrade_cost(canvas, x, cy, tier):
    """Above the arrow: the tier's gem. Below: its bar with a 40 stack count, like an inventory slot."""
    gem = game_res(Image.open(ART / f'{tier}Gem.png'))
    canvas.paste(gem, (x - gem.width // 2, cy - gem.height - 6), gem)
    bar = game_res(bar_icon(tier))
    bx, by = x - bar.width // 2 - 10, cy + 4
    canvas.paste(bar, (bx, by), bar)
    pixel_text(canvas, (bx + bar.width + 10, by + bar.height - 14), 'x40', 9, '#ffffff')


# Background: the base screenshot, blurred and dark so the foreground reads.
bg = sc.crop('20.12.13.42', (260, 130, 1000, 546)).resize((W, H), Image.LANCZOS)
canvas = ImageEnhance.Brightness(bg.filter(ImageFilter.GaussianBlur(6))).enhance(0.4)

# --- Network (left): floor patch + terminal and units edge to edge.
S = 6                     # 16 px sprites -> 96 px tiles
T = 16 * S
area = (36, 200, 36 + 5 * T + 24, 200 + 2 * T + 24)
# Clean dirt-and-moss patch from the base shot (no objects), tiled at the sprites' scale.
patch = sc.crop('20.12.13.42', (260, 130, 1000, 546)).crop((1340, 850, 1560, 960))
patch = patch.resize((round(patch.width * T / 115), round(patch.height * T / 115)), Image.LANCZOS)
floor = Image.new('RGB', (area[2] - area[0], area[3] - area[1]))
for fy in range(0, floor.height, patch.height):
    for fx in range(0, floor.width, patch.width):
        floor.paste(patch.transpose(Image.FLIP_LEFT_RIGHT) if (fx // patch.width + fy // patch.height) % 2 else patch, (fx, fy))
floor = ImageEnhance.Brightness(floor).enhance(0.95)
fade = Image.new('L', floor.size, 0)
ImageDraw.Draw(fade).rounded_rectangle((4, 4, floor.width - 5, floor.height - 5), 14, fill=255)
sh = Image.new('L', (W, H), 0)
ImageDraw.Draw(sh).rounded_rectangle((area[0] + 8, area[1] + 10, area[2] + 8, area[3] + 10), 14, fill=170)
canvas.paste('#000000', (0, 0), sh.filter(ImageFilter.GaussianBlur(12)))
canvas.paste(floor, area[:2], fade.filter(ImageFilter.GaussianBlur(3)))

ox, oy = area[0] + 12, area[1] + 12
layout = [  # (column, row, sprite) - an L of units around the terminal, all sharing edges
    (0, 0, 'CopperChest'), (1, 0, 'IronChest'), (2, 0, 'Terminal'), (3, 0, 'OctarineChest'), (4, 0, 'GalaxiteChest'),
    (1, 1, 'ScarletChest'), (2, 1, 'SolariteChest'), (3, 1, 'ReluciteChest'),
]
cells = {}
for cx, cy, name in layout:
    im = sprite(name, S)
    x, y = ox + cx * T, oy + cy * T + (T - im.height)
    canvas.paste(im, (x, y), im)
    cells[(cx, cy)] = (x, oy + cy * T)

# Gold seams on every shared edge: this is what "connected" means in the mod.
seam = Image.new('L', (W, H), 0)
sd = ImageDraw.Draw(seam)
lines = []
for (cx, cy), (x, y) in cells.items():
    if (cx + 1, cy) in cells:
        lines.append((x + T, y + 18, x + T, y + T - 18))
    if (cx, cy + 1) in cells:
        lines.append((x + 18, y + T, x + T - 18, y + T))
for l in lines:
    sd.line(l, fill=255, width=10)
seam = seam.filter(ImageFilter.GaussianBlur(6))
# Soft amber halo plus a thin warm line: visible, but quieter than the gems and screen.
canvas = Image.composite(Image.new('RGB', (W, H), '#e8a840'), canvas, seam.point(lambda v: v * 3 // 5))
d = ImageDraw.Draw(canvas)
for l in lines:
    d.line(l, fill='#f0c870', width=2)

# --- Storage panel (right), with the category tabs on its left edge (first six of twelve).
PX0, PY0, PX1 = 18, 148, 572          # preview coords of the crop: tab column + panel
panel = sc.crop('20.05.30.46', (PX0, PY0, PX1, 632))
panel = panel.crop((0, 0, panel.width, round(panel.height * 237 / 484)))  # rows with items only
pbox = (area[2] + 30, area[1], W - 30, area[3])
img, px, py, s = sc.fit(panel, pbox)
shadowed(canvas, img, px, py)
k = img.width / (PX1 - PX0)           # preview pixels -> canvas pixels
tabs = (px + round((27 - PX0) * k), py + round(2 * k), px + round((63 - PX0) * k), py + img.height - 2)  # tab boxes span preview x 29-61
canvas = glow_frames(canvas, [(px - 4, py - 4, px + img.width + 4, py + img.height + 4)])
canvas = glow_frames(canvas, [tabs], radius=6, width=2)

# --- Upgrade path (bottom strip).
strip = (24, area[3] + 22, W - 24, H - 10)
plate = Image.new('RGBA', (strip[2] - strip[0], strip[3] - strip[1]), (14, 40, 38, 215))
ImageDraw.Draw(plate).rounded_rectangle((0, 0, plate.width - 1, plate.height - 1), 12, outline='#2a8a80', width=3)
canvas.paste(plate, strip[:2], plate)

steps = [('Copper', 40), ('Tin', 80), ('Iron', 120), (None, None), ('Relucite', 360)]
n_nodes = len(steps)
cy = (strip[1] + strip[3]) // 2 - 10
slot_w = (strip[2] - strip[0]) / n_nodes
U = 6
for i, (tier, slots) in enumerate(steps):
    cx = round(strip[0] + slot_w * (i + 0.5))
    if tier is None:
        pixel_text(canvas, (cx - 40, cy), '. . .', 14, GOLD, scale=3)
        d = ImageDraw.Draw(canvas)
        ax0, ax1 = cx + 10, round(strip[0] + slot_w * (i + 1.5)) - 64
        d.line((ax0, cy, ax1 - 10, cy), fill=GOLD, width=5)
        d.polygon([(ax1, cy), (ax1 - 16, cy - 11), (ax1 - 16, cy + 11)], fill=GOLD)
        upgrade_cost(canvas, (ax0 + ax1) // 2, cy, 'Relucite')
        continue
    unit = sprite(tier + 'Chest', U)
    canvas.paste(unit, (cx - unit.width // 2, cy - unit.height // 2 - 4), unit)
    pixel_text(canvas, (cx, cy + unit.height // 2 + 18), f'{slots} SLOTS', 9, MINT)
    if i + 1 < n_nodes:
        # Arrow to the next node with the gem that does the upgrade and its bar cost.
        nxt = steps[i + 1][0]
        if nxt is None:
            continue
        ax0, ax1 = cx + unit.width // 2 + 14, round(strip[0] + slot_w * (i + 1.5)) - unit.width // 2 - 14
        mid = (ax0 + ax1) // 2
        d = ImageDraw.Draw(canvas)
        d.line((ax0, cy, ax1 - 10, cy), fill=GOLD, width=5)
        d.polygon([(ax1, cy), (ax1 - 16, cy - 11), (ax1 - 16, cy + 11)], fill=GOLD)
        if True:
            upgrade_cost(canvas, mid, cy, nxt)

sc.banner(canvas, 'RUNIC STORAGE', 'CONNECT STORAGE UNITS AND UPGRADE THEM')
OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT, optimize=True)
print(OUT, OUT.stat().st_size)
