"""Enchanting cover candidates (960x960) from Sid's 2026-11-04 HDR captures.

    python release/art_direction/make_enchanting_cover.py [Key ...] [--out DIR]

Reuses make_screenshot_covers.py (crop cache, panels, gold frames, teal/gold banner, WIP plaque) with the
canvas switched to a 960x960 square. HDR captures are tone-mapped first with hdr_to_png.py into
build/artwork-review/2026-11-04-enchanting/src. Boxes are in 1280x720 preview pixels (full res / 3).

  01.59.06.79  Iron Bow tooltip: Bossbane II, Power I, Stunshot I (purple enchant lines)
  01.59.21.54  Bronze Helm tooltip: Bosswarden I, Vitality III, Thorns I; equipped helm slot has the violet border
  01.57.36.24  desert base with the placed violet Enchanting Table (also the blurred background)
  01.57.29.87  Enchanting Table recipe tooltip and its crafting-tab slot
  01.10.36.84  the table's red 'Upgrade' panel with the Iron Bow in its slot
Layouts D/E: recipe + placed table + panel on the left, violet-bordered bow slot + bow tooltip on the right.
Candidates A-C carry the WIP plaque; layouts D/E carry an ALPHA plaque (Sid, 2026-11-04). Game UI is never repainted.
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402

covers.W = covers.H = 960
W = H = 960
TOP = covers.TOP + 8
covers.HDR_DIRS.append(covers.REPO / 'build/artwork-review/2026-11-04-enchanting/src')

BOW, HELM = 'hdr:01.59.06.79', 'hdr:01.59.21.54'
BG = ('hdr:01.57.36.24', (200, 0, 920, 720), 6)   # desert base, no tooltip behind the panels
TITLE, CAPTION = 'ENCHANTING', 'RANDOM ENCHANTS FOR YOUR GEAR'
BOW_ENCH = (818, 381, 1261, 603)      # Bossbane II .. "ranged hit"
HELM_ENCH = (829, 376, 1260, 600)     # Bosswarden I .. "+10 thorns damage"
HELM_SLOT = (774, 109, 829, 164)      # equipped Bronze Helm (hover highlight, not the enchant border)

CANDIDATES = {
    # Two enchanted tooltips side by side: a weapon and an armor piece.
    'Enchanting_A_duo': dict(
        title=TITLE, caption=CAPTION, bg=BG, wip=True, frame_outline=True,
        panels=[(BOW, (816, 154, 1263, 640), (16, TOP + 72, 476, H - 16), [BOW_ENCH]),
                (HELM, (827, 210, 1262, 630), (484, TOP + 72, W - 16, H - 16), [HELM_ENCH])]),
    # One big, readable bow tooltip: the three purple enchant lines are the subject.
    'Enchanting_B_hero': dict(
        title=TITLE, caption=CAPTION, bg=BG, wip=True, frame_outline=True,
        panels=[(BOW, (816, 352, 1263, 640), (20, TOP + 72, W - 20, H - 20), [BOW_ENCH])]),
    # Equipped helm with its violet slot border, next to its enchant lines.
    'Enchanting_C_equipped': dict(
        title=TITLE, caption=CAPTION, bg=BG, wip=True, frame_outline=True,
        panels=[(HELM, (703, 60, 1262, 700), (16, TOP + 70, W - 16, H - 14), [HELM_SLOT, HELM_ENCH])]),
}


# --- Layout Sid asked for on 2026-11-04: recipe + placed table on the left, item slot + tooltip on the right.
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont  # noqa: E402

RECIPE = 'hdr:01.57.29.87'      # Enchanting Table recipe tooltip (20 Octarine Bar, 15 Gold Bar), crafting tab slot
PLACED = 'hdr:01.57.36.24'      # the violet table placed on the desert base
TABLE_UI = 'hdr:01.10.36.84'    # the table's red "Upgrade" panel with the Iron Bow in its slot
RECIPE_TIP = (843, 432, 1264, 705)
# F (release cover): title row + Materials rows only; the description still mentions fishing rods,
# which can no longer be enchanted (Sid, 2026-11-04). The two rows are stacked, not repainted.
RECIPE_ROWS = ((843, 432, 1264, 472), (843, 580, 1264, 705))
RECIPE_SLOT = (677, 286, 728, 339)
GROUND = (656, 200, 896, 385)
GROUND_TALL = (656, 150, 896, 400)
GROUND_TABLE = (692, 290, 780, 343)
UI_PANEL = (345, 170, 515, 343)
BOW_SLOT = (400, 368, 469, 437)      # hotbar slot 2 with the violet enchanted border (01.59.21.54)
HOTBAR = (355, 368, 580, 437)        # hotbar slots 1-4: only the bow has the violet border
BOW_TIP = (816, 154, 1263, 640)
GOLD, DARK = '#e8b85a', '#1a0f08'
L0, L1, R0, R1, Y0, Y1 = 16, 470, 486, W - 16, TOP + 70, H - 16


def put(canvas, stamp, box, target, frames=(), gain=1.0, align='center'):
    """Paste a screenshot crop fitted into target with drop shadow, gold border and gold frames."""
    if isinstance(box[0], tuple):   # several crops of one capture stacked vertically (rows of one tooltip)
        parts = [covers.crop(stamp, b) for b in box]
        src = Image.new('RGB', (max(p.width for p in parts), sum(p.height for p in parts)))
        yy = 0
        for p_ in parts:
            src.paste(p_, (0, yy))
            yy += p_.height
        box = (box[0][0], 0, box[0][2], 0)   # only the width is used below (frames are not supported here)
    else:
        src = covers.crop(stamp, box)
    if gain != 1.0:
        src = ImageEnhance.Brightness(src).enhance(gain)
    img, x, y, s = covers.fit(src, target)
    if align == 'top':
        y = target[1]
    elif align == 'bottom':
        y = target[3] - img.height
    s *= src.width / (box[2] - box[0])
    sh = Image.new('L', (W, H), 0)
    ImageDraw.Draw(sh).rectangle((x + 8, y + 10, x + img.width + 8, y + img.height + 10), fill=170)
    canvas.paste('#000000', (0, 0), sh.filter(ImageFilter.GaussianBlur(12)))
    canvas.paste(img, (x, y))
    d = ImageDraw.Draw(canvas)
    d.rectangle((x - 4, y - 4, x + img.width + 3, y + img.height + 3), outline=DARK, width=4)
    d.rectangle((x - 2, y - 2, x + img.width + 1, y + img.height + 1), outline=GOLD, width=2)
    out = []
    for fx0, fy0, fx1, fy1 in frames:
        out.append((x + round((fx0 - box[0]) * s), y + round((fy0 - box[1]) * s),
                    x + round((fx1 - box[0]) * s), y + round((fy1 - box[1]) * s)))
    return (x, y, x + img.width, y + img.height), out


def gold_frames(canvas, frames):
    glow = Image.new('L', (W, H), 0)
    for f in frames:
        ImageDraw.Draw(glow).rounded_rectangle(f, 12, outline=255, width=10)
    glow = glow.filter(ImageFilter.GaussianBlur(10))
    canvas = Image.composite(Image.new('RGB', (W, H), '#ffb347'), canvas, glow.point(lambda v: v // 3))
    d = ImageDraw.Draw(canvas)
    for f in frames:
        d.rounded_rectangle((f[0] - 3, f[1] - 3, f[2] + 3, f[3] + 3), 13, outline=DARK, width=9)
        d.rounded_rectangle(f, 10, outline=GOLD, width=3)
    return canvas


def alpha_tag(canvas, text='ALPHA'):
    """Same red plaque as covers.wip_tag, with ALPHA (Sid, 2026-11-04: ships tonight as an alpha)."""
    from PIL import ImageFont
    P = 4
    tag = Image.new('RGBA', (60, 14), (0, 0, 0, 0))
    t = ImageDraw.Draw(tag)
    t.rectangle((0, 0, 59, 13), fill='#7a1c1c', outline='#e05a4a')
    t.line((1, 1, 58, 1), fill='#a83a30')
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 8)
    layer = Image.new('L', tag.size)
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    s = '   '.join(text)   # letter-spaced like the WIP plaque
    s = text if ld.textlength(s, font=f) > 54 else s
    ld.text(((60 - ld.textlength(s, font=f)) / 2, 1), s, font=f, fill=255)
    tag.paste('#ffe8c8', (0, 0), layer)
    tag = tag.resize((tag.width * P, tag.height * P), Image.NEAREST)
    canvas.paste(tag, ((W - tag.width) // 2, 186), tag)


def compose_layout(right):
    stamp, box, blur = BG
    bg = covers.crop(stamp, box).resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(blur))
    canvas = ImageEnhance.Brightness(bg).enhance(0.45)
    frames = []
    # Left top: the recipe tooltip, with the crafting-tab slot (table icon) over the tooltip's empty corner.
    tip = RECIPE_ROWS if right == 'bow_helm' else RECIPE_TIP
    (tx0, ty0, tx1, ty1), _ = put(canvas, RECIPE, tip, (L0, Y0, L1, Y0 + 300), align='top')
    _, f = put(canvas, RECIPE, RECIPE_SLOT, (tx1 - 118, ty1 - 112, tx1 - 14, ty1 - 12), [RECIPE_SLOT])
    frames += f
    # Left bottom: the placed table, with its Upgrade-style panel (bow in the slot) on the sand beside it.
    ground = GROUND_TALL if right == 'bow_helm' else GROUND   # F: shorter recipe panel, so a taller ground crop
    (gx0, gy0, gx1, gy1), f = put(canvas, PLACED, ground, (L0, ty1 + 22, L1, Y1), [GROUND_TABLE], align='bottom')
    frames += f
    put(canvas, TABLE_UI, UI_PANEL, (gx1 - 182, gy0 + 12, gx1 - 12, gy0 + 190), gain=1.6)
    # Right: the item slot with the violet border above the item's tooltip.
    if right == 'slot':
        (sx0, sy0, sx1, sy1), f = put(canvas, HELM, BOW_SLOT, (R0, Y0, R1, Y0 + 150), align='top')
    elif right == 'bow_helm':
        # Bow slot and helm slot side by side (helm: hotbar slot 5 of 02.21.50.27, un-hovered). With HELM_SLOT_SHOT = None
        # a review placeholder is drawn instead; never fake or recolor a border.
        cx = (R0 + R1) // 2
        (sx0, sy0, sx1, sy1), f = put(canvas, HELM, BOW_SLOT, (cx - 170, Y0, cx - 20, Y0 + 150), align='top')
        if HELM_SLOT_SHOT:
            put(canvas, HELM_SLOT_SHOT[0], HELM_SLOT_SHOT[1], (cx + 20, Y0, cx + 170, Y0 + 150), align='top')
        else:
            d = ImageDraw.Draw(canvas)
            box = (cx + 20, sy0, cx + 20 + (sx1 - sx0), sy1)
            d.rectangle(box, fill='#2a1f18', outline=GOLD, width=2)
            for i, line in enumerate(['HELM SLOT', 'SCREENSHOT', 'NEEDED']):
                d.text(((box[0] + box[2]) // 2, box[1] + 45 + i * 22), line, fill='#ffd36a', anchor='mm',
                       font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 16))
    else:
        (sx0, sy0, sx1, sy1), f = put(canvas, HELM, HOTBAR, (R0, Y0, R1, Y0 + 150), [BOW_SLOT], align='top')
    frames += f
    _, f = put(canvas, BOW, BOW_TIP, (R0, sy1 + 18, R1, Y1), [BOW_ENCH], align='top')
    frames += f
    canvas = gold_frames(canvas, frames)
    covers.banner(canvas, TITLE, CAPTION)
    alpha_tag(canvas)
    return canvas


HELM_SLOT_SHOT = ('hdr:02.21.50.27', (577, 368, 646, 437))   # enchanted Bronze Helm in hotbar slot 5, un-hovered, violet border
LAYOUTS = {'Enchanting_D_layout_slot': 'slot', 'Enchanting_E_layout_hotbar': 'hotbar',
           'Enchanting_F_layout_bow_helm': 'bow_helm'}


if __name__ == '__main__':
    args = sys.argv[1:]
    out_dir = covers.REPO / 'build/artwork-review/2026-11-04-enchanting'
    if '--out' in args:
        i = args.index('--out')
        out_dir = Path(args[i + 1])
        del args[i:i + 2]
    out_dir.mkdir(parents=True, exist_ok=True)
    covers.COVERS.update(CANDIDATES)
    for key in args or [*CANDIDATES, *LAYOUTS]:
        if key in LAYOUTS:
            c = compose_layout(LAYOUTS[key])
            out = out_dir / f'{key}.png'
            c.save(out, optimize=True)
            print(out, c.size, out.stat().st_size)
        else:
            covers.build(key, out_dir)
