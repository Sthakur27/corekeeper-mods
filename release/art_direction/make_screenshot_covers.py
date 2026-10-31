"""Workshop covers built from Sid's in-game screenshots (NVIDIA captures, 3840x2160, 2026-10-28).

    python release/art_direction/make_screenshot_covers.py [Key ...] [--out DIR]

Each cover: a darkened background (blurred unless it is itself the subject), one or more screenshot
crops in front with feathered edges, optional gold highlight frames, and the pixel title banner used
by the other covers. Game UI is never repainted. Crops are cached as PNGs in
release/art_direction/screens/ (cut from the NVIDIA folder on first run), so the script is
reproducible without the original captures. Crop/highlight boxes are in 1280x720 preview pixels.
"""
from pathlib import Path
import sys
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont

REPO = Path(__file__).resolve().parents[2]
CACHE = REPO / 'release/art_direction/screens'
NVIDIA = Path.home() / 'Videos/NVIDIA/Desktop'
W, H, TOP = 1280, 720, 176          # canvas; content area starts below the banner


# HDR captures (.jxr scRGB float) are tone-mapped to PNG first with hdr_to_png.py:
# value / 10 (game UI white ~9.5), clamp, sRGB curve. Converted copies live in HDR_DIRS.
HDR_DIRS = [REPO / 'build/artwork-review/hdr', REPO / 'build/artwork-review/2026-10-28/hdr',
            REPO / 'build/artwork-review/2026-10-29/hdr']


def shot(stamp):
    if stamp.startswith('src:'):  # a source image kept next to this script (e.g. one Sid pasted in chat)
        return Path(__file__).parent / stamp[4:]
    if stamp.startswith('hdr:'):
        hits = [d / f'{stamp[4:]}.png' for d in HDR_DIRS if (d / f'{stamp[4:]}.png').exists()]
        return hits[0] if hits else HDR_DIRS[0] / f'{stamp[4:]}.png'
    return NVIDIA / f'Desktop Screenshot 2026.10.28 - {stamp}.png'


# key -> title, caption, background (stamp, crop, blur), panels [(stamp, crop, target box, highlights)]
COVERS = {
    'FiveLoadouts': dict(
        title='FIVE LOADOUTS', caption='FIVE EQUIPMENT LOADOUTS, TABS I TO V',
        bg=('19.57.12.56', (300, 40, 1280, 600), 6),
        panels=[('19.57.12.56', (655, 58, 1002, 354), (40, TOP + 4, W - 40, H - 14), [(664, 148, 708, 340)])]),
    'LoadoutSharing': dict(
        title='SHARED LOADOUT GEAR & VANITY', caption='EMPTY SLOTS BORROW FROM EARLIER LOADOUTS',
        bg=('19.57.23.85', (300, 40, 1280, 600), 6),
        panels=[('19.57.23.85', (655, 58, 1002, 354), (40, TOP + 4, W - 40, H - 14), [(710, 105, 970, 342)])]),
    'PetEditor': dict(
        title='PET EDITOR', caption='CHOOSE TALENTS, LEVEL AND COLOR',
        bg=('19.57.42.05', (0, 40, 1280, 720), 6),
        panels=[('19.57.42.05', (690, 55, 1272, 596), (40, TOP + 4, W - 40, H - 14), [(1008, 188, 1264, 406)])]),
    'ArmorDye': dict(
        title='ARMOR DYE', caption='RECOLOR ANY ARMOR, WEAPON OR TOOL',
        bg=('19.59.16.75', (0, 40, 1280, 720), 6),
        panels=[('19.59.16.75', (335, 55, 1000, 426), (40, TOP + 4, W - 40, H - 14), [(424, 166, 769, 403)])]),
    'ModOptions': dict(
        title='MOD OPTIONS', caption="EVERY MOD'S SETTINGS IN ONE MENU",
        bg=('19.58.40.45', (0, 0, 1280, 720), 4),
        panels=[('19.58.40.45', (300, 40, 1100, 615), (24, TOP + 4, 640, H - 14), []),
                ('19.58.32.13', (230, 40, 1050, 600), (640, TOP + 4, W - 24, H - 14), [])]),
    'ConnectedStorage': dict(
        title='RUNIC STORAGE', caption='SEARCH EVERY CONNECTED CHEST',
        bg=('20.12.13.42', (260, 130, 1000, 546), 0),
        panels=[('20.05.30.46', (68, 148, 572, 632), (28, TOP + 8, 560, H - 16), [])]),
    # Farm shot: golden Glow Tulips among blue ones, a golden Heart Berry among red ones.
    'GoldenChance': dict(
        title='GOLDEN CHANCE', caption='YOUR TALENTS GROW MORE GOLDEN CROPS',
        bg=('20.01.09.34', (180, 100, 820, 460), 0),
        marks=[(662, 242), (577, 370)],
        panels=[('20.01.09.34', (538, 78, 618, 152), (40, 392, 330, H - 22), [(538, 78, 618, 152)]),
                ('20.01.09.34', (626, 206, 706, 280), (W - 330, 392, W - 40, H - 22), [(626, 206, 706, 280)])]),
    # Ender Chest placed in the desert base (bg), its stash window, and the merchant's price tooltip.
    'EnderStash': dict(
        title='ENDER STASH', caption='YOUR PERSONAL STASH IN EVERY ENDER CHEST',
        bg=('hdr:17.51.28.10', (291, 160, 851, 475), 0),
        marks=[(676, 322, 70)],
        panels=[('hdr:17.51.45.25', (340, 85, 950, 345), (24, 420, 640, H - 18), [(340, 85, 950, 345)]),
                ('hdr:17.51.19.80', (1045, 625, 1256, 705), (W - 360, H - 150, W - 24, H - 22), [])]),
    # Mid-cast with the mirror (blue teleport ring) as background, and its crafting tooltip.
    'MagicMirror': dict(
        title='MAGIC MIRROR', caption='A RECALL IDOL YOU NEVER USE UP',
        bg=('hdr:00.23.31.80', (330, 150, 970, 510), 0),
        panels=[('hdr:00.23.58.85', (858, 423, 1262, 705), (W - 470, TOP + 120, W - 24, H - 18), [])]),
    # Five machines lined up on a grass platform, each framed.
    'FasterMachines': dict(
        title='FASTER MACHINES', caption='',
        bg=('hdr:13.32.08.61', (300, 100, 1060, 527), 6),
        panels=[('hdr:13.32.08.61', (350, 150, 1010, 380), (40, TOP + 4, W - 40, H - 14),
                 [(434, 229, 489, 284), (519, 219, 574, 284), (606, 197, 659, 286), (691, 224, 744, 278), (776, 226, 831, 281)])]),
    # 10x10 in three steps on the same patch of sand: the tool preview, the tilled ground, a seed brush
    # planting all of it. Darker HDR captures, so brightened ('gain').
    'BuildTools': dict(
        title='BUILD TOOLS', caption='BIGGER TOOLS, BLOCK AND SEED BRUSH, SIZES PER TOOL', gain=1.7,
        bg=('hdr:14.21.40.31', (300, 100, 980, 483), 6),
        panels=[('hdr:14.21.30.03', (405, 120, 865, 580), (24, TOP + 40, 424, H - 24), []),
                ('hdr:14.21.35.01', (405, 120, 865, 580), (440, TOP + 40, 840, H - 24), []),
                ('hdr:14.21.40.31', (405, 120, 865, 580), (856, TOP + 40, 1256, H - 24), [])]),
    # 1.1 cover: the 1.0 panels made smaller, plus the cooking equation (golden ingredients + Gourmet = epic
    # dish, 9999 on the cursor) along the bottom.
    'ItemSpawner': dict(
        title='ITEM SPAWNER', caption='ANY ITEM, FULL STACKS, LEVEL UP GEAR, COOKING', gain=1.7,
        bg=('hdr:14.41.05.93', (300, 100, 1060, 527), 6),
        panels=[('hdr:14.41.28.59', (75, 150, 570, 470), (24, TOP + 10, 540, H - 150),
                 [(364, 398, 452, 456)]),
                ('hdr:14.41.49.61', (75, 150, 570, 262), (560, TOP + 10, W - 24, TOP + 150),
                 [(308, 156, 528, 192)]),
                ('hdr:14.41.49.61', (826, 216, 1262, 382), (640, TOP + 160, W - 24, H - 150),
                 [(830, 256, 1004, 288)]),
                ('hdr:15.53.04.82', (70, 196, 515, 262), (24, H - 140, W - 24, H - 14),
                 [(312, 202, 370, 259), (382, 210, 418, 250)])]),
    # Gallery showcase for cooking: the equation (golden + golden = epic dish, 9999 on the cursor) blown up,
    # the Royal Starlight Sushi equation, and the spawned stacks in the bag.
    'ItemSpawnerCooking': dict(
        title='COOK ANY DISH', caption='PICK TWO INGREDIENTS, GET THE DISH', gain=1.7,
        bg=('hdr:15.53.04.82', (300, 100, 1060, 527), 6),
        panels=[('hdr:15.53.04.82', (70, 196, 515, 262), (24, TOP + 14, W - 24, TOP + 175),
                 [(312, 202, 370, 259), (382, 210, 418, 250)]),
                ('hdr:15.53.31.63', (75, 150, 570, 320), (24, TOP + 195, 620, H - 14),
                 [(312, 202, 370, 259)]),
                ('hdr:15.53.04.82', (630, 318, 1228, 430), (640, TOP + 195, W - 24, H - 14),
                 [(1048, 328, 1170, 380)])]),
}


def crop(stamp, box):
    """Full-resolution crop for a 1280x720 preview box, cached in the repo."""
    CACHE.mkdir(parents=True, exist_ok=True)
    name = CACHE / f"{stamp.replace('.', '').replace(':', '_')}_{'_'.join(map(str, box))}.png"
    if not name.exists():
        full = Image.open(shot(stamp)).convert('RGB')
        k = full.width / 1280
        full.crop(tuple(round(v * k) for v in box)).save(name, optimize=True)
    return Image.open(name).convert('RGB')


def fit(img, box):
    bw, bh = box[2] - box[0], box[3] - box[1]
    s = min(bw / img.width, bh / img.height)
    img = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
    return img, box[0] + (bw - img.width) // 2, box[1] + (bh - img.height) // 2, s


def banner(canvas, title, caption):
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
        layer = Image.new('L', (bw, bh))
        ld = ImageDraw.Draw(layer)
        ld.fontmode = '1'
        while True:   # shrink long titles until they fit inside the cloth
            f = ImageFont.truetype('C:/Windows/Fonts/' + font, size)
            if ld.textlength(s, font=f) <= bx1 - bx0 - 14 or size <= 6:
                break
            size -= 1
        ld.text(((bw - ld.textlength(s, font=f)) / 2, y + (21 - size) // 2 if font == 'impact.ttf' else y), s, font=f, fill=255)
        ban.paste('#1a0f08', (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
        ban.paste(fill, (0, 0), layer)
        return layer

    ty = 7 if caption else 12   # no caption: centre the title on the cloth
    t = text(title, 21, ty, '#ffd36a', 'impact.ttf')
    band = Image.new('L', (bw, bh))
    band.paste(255, (0, 0, bw, ty + 10))
    ban.paste('#fff0b8', (0, 0), ImageChops.multiply(t, band))
    if caption:
        text(caption.replace(' ', '   '), 8, 30, '#94f7dd', 'segoeuib.ttf')   # 1 px spaces vanish at this size
    ban = ban.resize((bw * P, bh * P), Image.NEAREST)
    canvas.paste(ban, (0, 0), ban)


def wip_tag(canvas):
    """Red 'WORK IN PROGRESS' plaque under the banner, drawn at quarter scale like the banner."""
    P = 4
    tag = Image.new('RGBA', (92, 14), (0, 0, 0, 0))
    t = ImageDraw.Draw(tag)
    t.rectangle((0, 0, 91, 13), fill='#7a1c1c', outline='#e05a4a')
    t.line((1, 1, 90, 1), fill='#a83a30')
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 8)
    layer = Image.new('L', tag.size)
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    s = 'WORK   IN   PROGRESS'
    ld.text(((92 - ld.textlength(s, font=f)) / 2, 1), s, font=f, fill=255)
    tag.paste('#ffe8c8', (0, 0), layer)
    tag = tag.resize((tag.width * P, tag.height * P), Image.NEAREST)
    canvas.paste(tag, ((W - tag.width) // 2, 186), tag)


def build(key, out_dir):
    canvas = compose(key)
    out = out_dir / f'{key}.png'
    canvas.save(out, optimize=True)
    print(out, canvas.size, out.stat().st_size)


def compose(key):
    """The finished cover as an RGB image (also used by scripts that add their own overlay)."""
    c = COVERS[key]
    stamp, box, blur = c['bg']
    bg = crop(stamp, box)
    bg = bg.resize((W, round(bg.height * W / bg.width)), Image.LANCZOS)
    if bg.height < H:
        bg = bg.resize((round(bg.width * H / bg.height), H), Image.LANCZOS)
    bg = bg.crop(((bg.width - W) // 2, (bg.height - H) // 2, (bg.width - W) // 2 + W, (bg.height - H) // 2 + H))
    if blur:
        bg = bg.filter(ImageFilter.GaussianBlur(blur))
    gain = c.get('gain', 1.0)  # for dark (HDR tone-mapped) captures
    canvas = ImageEnhance.Brightness(bg).enhance((0.45 if blur else 0.8) * gain)

    # Rings around points of interest in the background (preview coords; bg crop must be 16:9).
    if c.get('marks'):
        k = W / (box[2] - box[0])
        ring = Image.new('L', (W, H), 0)
        rd = ImageDraw.Draw(ring)
        pts = [(round((m[0] - box[0]) * k), round((m[1] - box[1]) * k), m[2] if len(m) > 2 else 40) for m in c['marks']]
        for px, py, r in pts:
            rd.ellipse((px - r, py - r, px + r, py + r), outline=255, width=8)
        ring = ring.filter(ImageFilter.GaussianBlur(7))
        canvas = Image.composite(Image.new('RGB', (W, H), '#ffcf5a'), canvas, ring.point(lambda v: v // 2))
        d = ImageDraw.Draw(canvas)
        for px, py, r in pts:
            d.ellipse((px - r + 4, py - r + 4, px + r - 4, py + r - 4), outline='#e8b85a', width=2)

    frames = []
    for stamp, box, target, highlights in c['panels']:
        src = crop(stamp, box)
        if gain != 1.0:
            src = ImageEnhance.Brightness(src).enhance(gain)
        img, x, y, s = fit(src, target)
        s *= src.width / (box[2] - box[0])        # preview pixels -> canvas pixels
        shadow = Image.new('L', (W, H), 0)
        ImageDraw.Draw(shadow).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
        canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(12)))
        fade = Image.new('L', img.size, 0)
        ImageDraw.Draw(fade).rectangle((6, 6, img.width - 7, img.height - 7), fill=255)
        canvas.paste(img, (x, y), fade.filter(ImageFilter.GaussianBlur(4)))
        for hx0, hy0, hx1, hy1 in highlights:
            frames.append((x + round((hx0 - box[0]) * s), y + round((hy0 - box[1]) * s),
                           x + round((hx1 - box[0]) * s), y + round((hy1 - box[1]) * s)))

    if frames:
        glow = Image.new('L', (W, H), 0)
        for f in frames:
            ImageDraw.Draw(glow).rounded_rectangle(f, 12, outline=255, width=10)
        glow = glow.filter(ImageFilter.GaussianBlur(10))
        canvas = Image.composite(Image.new('RGB', (W, H), '#ffb347'), canvas, glow.point(lambda v: v // 3))
        d = ImageDraw.Draw(canvas)
        for f in frames:
            d.rounded_rectangle(f, 10, outline='#e8b85a', width=3)

    banner(canvas, c['title'], c['caption'])
    if c.get('wip'):
        wip_tag(canvas)
    return canvas


if __name__ == '__main__':
    args = sys.argv[1:]
    out_dir = REPO / 'build/artwork-review/2026-10-28/screens'
    if '--out' in args:
        i = args.index('--out')
        out_dir = Path(args[i + 1])
        del args[i:i + 2]
    out_dir.mkdir(parents=True, exist_ok=True)
    for key in args or COVERS:
        build(key, out_dir)
