"""Extras on top of make_screenshot_covers.py for the 2026-11-01 screenshot covers.

    python release/art_direction/make_cover_extras.py [Key ...] [--out DIR]

- prepare(): writes cleaned copies of the Auto Replant farm captures (<stamp>-clean.png next to the HDR
  PNGs): each sprinkler tile is covered with the same patch from two tiles over, so the before/after
  pair shows only crops and seedlings.
- FishSeller: the Fishing Merchant (cut from capture 11.19.35.63) framed in the bottom-left corner.
- PotionSeller: the Caveling Merchant (capture 11.20.29.50) framed in the bottom-left corner.
- FastAutoFishing: clock_bubble(), a reusable speech-bubble clock icon in the style of the game's bite bubble.
- BiggerWateringCans: the watering can's own hotbar slot (capture 11.23.25.65), blown up between panels.
- InfiniteOreBoulders: a symmetric pixel infinity sign on a framed plate beside the boulder.
- MushroomSlayer: animated GIF; a big pixel damage number pops off the Brute and floats up, plus a
  still PNG. Steam previews must stay under 1 MB.
All sprites are taken from Sid's own screenshots and scaled with nearest-neighbour; overlays use the
banner's palette and are drawn at quarter scale like the banner.
"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402

W, H, P = covers.W, covers.H, 4
HDR = covers.REPO / 'build/artwork-review/2026-11-01/hdr'
GOLD, DARK, TEAL, TEAL_EDGE = '#e8b85a', '#1a0f08', '#164a46', '#2a8a80'

# Sprinkler tile centres in the 3840x2160 farm captures (one tile = 128 px), and where to copy from.
# Sources are two rows away in the same column, clear of the player (cols 2-3) and the cursor.
SPRINKLERS = [((1613, 670), (0, 258)), ((2253, 670), (0, 258)),
              ((1613, 1305), (0, -258)), ((2253, 1305), (0, -258))]
HALF = 110


def prepare():
    for stamp in ('11.21.06.00', '11.21.24.87'):
        out = HDR / f'{stamp}-clean.png'
        if out.exists():
            continue
        im = Image.open(HDR / f'{stamp}.png').convert('RGB')
        for (cx, cy), (dx, dy) in SPRINKLERS:
            box = (cx - HALF, cy - HALF, cx + HALF, cy + HALF)
            patch = im.crop((box[0] + dx, box[1] + dy, box[2] + dx, box[3] + dy))
            mask = Image.new('L', patch.size, 0)
            ImageDraw.Draw(mask).rectangle((14, 14, patch.width - 15, patch.height - 15), fill=255)
            im.paste(patch, box[:2], mask.filter(ImageFilter.GaussianBlur(8)))
        im.save(out)
        print(out)


def sprite(stamp, box, gain=1.9):
    """Full-resolution crop from an HDR capture (3840x2160 pixel box), brightened."""
    return ImageEnhance.Brightness(Image.open(HDR / f'{stamp}.png').convert('RGB').crop(box)).enhance(gain)


def plate(canvas, img, xy, scale=1.0, label=None):
    """Paste img in a teal/gold frame (quarter-scale border like the banner), optional caption under it."""
    img = img.resize((round(img.width * scale), round(img.height * scale)), Image.NEAREST)
    x, y = xy
    d = ImageDraw.Draw(canvas)
    shadow = Image.new('L', (W, H), 0)
    ImageDraw.Draw(shadow).rectangle((x - 4, y - 2, x + img.width + 16, y + img.height + 20), fill=170)
    canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(10)))
    d.rectangle((x - 12, y - 12, x + img.width + 11, y + img.height + 11), fill=TEAL)
    d.rectangle((x - 12, y - 12, x + img.width + 11, y + img.height + 11), outline=TEAL_EDGE, width=4)
    d.rectangle((x - 4, y - 4, x + img.width + 3, y + img.height + 3), outline=GOLD, width=4)
    canvas.paste(img, (x, y))
    if label:
        tag(canvas, label, (x + img.width // 2, y + img.height + 14))


def tag(canvas, text, center_top, size=8):
    """Small cloth label in the banner style, centred under a point (quarter scale, nearest-neighbour)."""
    f = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', size)
    s = text.replace(' ', '   ')
    probe = ImageDraw.Draw(Image.new('L', (1, 1)))
    tw = int(probe.textlength(s, font=f)) + 8
    lab = Image.new('RGBA', (tw, size + 6), TEAL)
    ld = ImageDraw.Draw(lab)
    ld.rectangle((0, 0, tw - 1, size + 5), outline=TEAL_EDGE)
    layer = Image.new('L', lab.size)
    ly = ImageDraw.Draw(layer)
    ly.fontmode = '1'
    ly.text((4, 1), s, font=f, fill=255)
    lab.paste('#94f7dd', (0, 0), layer)
    lab = lab.resize((lab.width * P, lab.height * P), Image.NEAREST)
    canvas.paste(lab, (center_top[0] - lab.width // 2, center_top[1]), lab)


def big_text(text, size, fill, outline=DARK):
    """1-bit Impact text at quarter scale with a dark outline, returned at full scale (RGBA)."""
    f = ImageFont.truetype('C:/Windows/Fonts/impact.ttf', size)
    probe = ImageDraw.Draw(Image.new('L', (1, 1)))
    w, h = int(probe.textlength(text, font=f)) + 6, size + 8
    layer = Image.new('L', (w, h))
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    ld.text((3, 1), text, font=f, fill=255)
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    img.paste(outline, (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
    img.paste(fill, (0, 0), layer)
    return img.resize((w * P, h * P), Image.NEAREST)


def fish_seller():
    c = covers.compose('FishSeller')
    merchant = sprite('11.19.35.63', (2040, 680, 2215, 840))
    plate(c, merchant, (44, H - 166), scale=0.9)
    tag(c, 'FISHING MERCHANT', (340, H - 100))
    return c


def potion_seller():
    c = covers.compose('PotionSeller')
    # the Caveling Merchant walking through the base, with his pack (capture 11.20.29.50)
    merchant = sprite('11.20.29.50', (1570, 670, 1820, 1080))
    tag(c, 'CAVELING MERCHANT', (205, 180))
    plate(c, merchant, (40, 268), scale=0.75)
    return c


def clock_bubble(cell=5):
    """Speech-bubble icon with a clock face, in the style of the game's "!" bite bubble: white bubble with
    a dark outline, grey shading at the bottom and a tail at the lower left. Drawn on a small grid and
    scaled with nearest-neighbour. Reusable for any 'faster / less waiting' mod."""
    import math
    gw, gh = 25, 27
    ink, white, shade, face, gold = DARK, '#f6f2e8', '#c7c1b3', '#fff8e0', '#d79a2b'
    img = Image.new('RGBA', (gw, gh), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, gw - 1, 21), 5, fill=white, outline=ink)
    d.rectangle((2, 18, gw - 3, 20), fill=shade)                     # bottom shading inside the outline
    d.rounded_rectangle((0, 0, gw - 1, 21), 5, outline=ink)
    d.polygon([(5, 20), (11, 20), (5, 26)], fill=white)               # tail
    d.line([(5, 21), (5, 26), (11, 21)], fill=ink)
    d.point((6, 22), fill=shade); d.point((6, 23), fill=shade)
    cx, cy, r = 12, 10, 7.4
    for y in range(gh):
        for x in range(gw):
            dist = math.hypot(x - cx, y - cy)
            if dist <= r - 1.2:
                img.putpixel((x, y), (255, 248, 224, 255))
            elif dist <= r:
                img.putpixel((x, y), (26, 15, 8, 255))
    for tx, ty in ((cx, cy - 5), (cx + 5, cy), (cx, cy + 5), (cx - 5, cy)):   # 12, 3, 6, 9 ticks
        img.putpixel((tx, ty), (215, 154, 43, 255))
    for i in range(1, 5):                                              # minute hand to 12
        img.putpixel((cx, cy - i), (26, 15, 8, 255))
    for i in range(1, 4):                                              # hour hand to 2
        img.putpixel((cx + i, cy - (i + 1) // 2), (26, 15, 8, 255))
    img.putpixel((cx, cy), (200, 60, 40, 255))
    return img.resize((gw * cell, gh * cell), Image.NEAREST)


def fast_fishing():
    c = covers.compose('FastAutoFishing')
    b = stopwatch(None, 0.3, cell=4)   # no multiplier setting, so no number
    c.paste(b, (560, 196), b)
    return c


def vehicle_speed():
    c = covers.compose('VehicleSpeed')
    b = stopwatch('2x', 0.5)   # one of the speed choices
    c.paste(b, (900, 200), b)
    return c


def buff_duration():
    c = covers.compose('BuffDurationFloor')
    b = stopwatch('3:00', 0.5)   # default minimum buff duration, as the setting shows it
    c.paste(b, (930, 260), b)
    return c


DUR_VALUES = ['1x', '0.75x', '0.5x', '0.25x', '0x']   # the mod's 'Durability loss rate' choices


def durability_frames():
    """Hotbar tools plus the Faster Machines dial counting the wear rate down 1x -> 0x (still: 0.5x default)."""
    import make_fastermachines_cover as fm
    fm.VALUES = DUR_VALUES
    fm.DIAL_POS = (930, 300)
    base = covers.compose('DurabilityMultiplier')
    n = len(DUR_VALUES) - 1
    still = fm.frame(base, '0.5x', 2 / n)
    frames, durations = [], []
    for i, v in enumerate(DUR_VALUES):
        if i:
            for s in (0.25, 0.5, 0.75):
                frames.append(fm.frame(base, DUR_VALUES[i - 1], (i - 1 + s) / n))
                durations.append(60)
        frames.append(fm.frame(base, v, i / n))
        durations.append(1400 if v == '0x' else 700)
    for s in (0.8, 0.6, 0.4, 0.2):
        frames.append(fm.frame(base, '0x', s))
        durations.append(50)
    return still, frames, durations


def stopwatch(label='2x', frac=0.3, badge='side', cell=5):
    """Cartoon pixel stopwatch, reusable for any 'faster' mod: brass rim and crown, cream face, a red wedge
    for the elapsed part (frac of a turn from 12 o'clock), a hand at the wedge edge, and a speed label
    either on a teal badge at the lower right (badge='side') or on the face itself (badge='center').
    Drawn on a small grid and scaled with nearest-neighbour like the banner."""
    import math
    gw, gh = 34, 36
    img = Image.new('RGBA', (gw, gh), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy, r = 15, 20, 13.2
    ink, brass, brass_hi, brass_lo = (26, 15, 8, 255), (232, 184, 90, 255), (255, 228, 150, 255), (150, 98, 36, 255)
    face, face_lo, red, red_hi = (255, 248, 224, 255), (226, 214, 186, 255), (214, 58, 44, 255), (246, 112, 84, 255)
    # crown (top button) and side button
    d.rectangle((cx - 2, 2, cx + 2, 4), fill=brass, outline=ink)
    d.rectangle((cx - 1, 4, cx + 1, 6), fill=brass_lo, outline=ink)
    d.polygon([(cx + 7, 8), (cx + 10, 5), (cx + 12, 7), (cx + 9, 10)], fill=brass, outline=ink)
    start = -math.pi / 2
    for y in range(gh):
        for x in range(gw):
            dx, dy = x - cx, y - cy
            dist = math.hypot(dx, dy)
            if dist > r:
                continue
            if dist > r - 1.1:
                img.putpixel((x, y), ink)
            elif dist > r - 3.1:                       # brass rim, lit from the top left
                img.putpixel((x, y), brass_hi if dx + dy < -6 else (brass_lo if dx + dy > 8 else brass))
            elif dist > r - 4.0:
                img.putpixel((x, y), ink)
            else:
                ang = (math.atan2(dy, dx) - start) % (2 * math.pi)
                inside = ang <= frac * 2 * math.pi
                col = (red_hi if dist < 4 else red) if inside else (face_lo if dy > 5 else face)
                img.putpixel((x, y), col)
    for k in range(12):                                # hour ticks
        a = start + k * math.pi / 6
        tx, ty = round(cx + (r - 5.6) * math.cos(a)), round(cy + (r - 5.6) * math.sin(a))
        if k % 3 == 0:
            img.putpixel((tx, ty), ink)
    a = start + frac * 2 * math.pi                     # hand at the edge of the wedge
    for i in range(1, 9):
        img.putpixel((round(cx + i * math.cos(a)), round(cy + i * math.sin(a))), ink)
    for i in range(1, 9):                              # wedge edge from 12
        img.putpixel((cx, cy - i), ink if i > 7 else img.getpixel((cx, cy - i)))
    d.rectangle((cx - 1, cy - 1, cx + 1, cy + 1), fill=ink)
    img.putpixel((cx, cy), brass)
    big = img.resize((gw * cell, gh * cell), Image.NEAREST)
    if not label:
        return big
    t = big_text(label.upper(), 15 if badge == 'side' else 9, '#ffd36a')
    if badge == 'center':
        big.paste(t, (round(cx * cell + cell / 2 - t.width / 2), round((cy + 2) * cell)), t)
        return big
    pad = 10
    plate_w, plate_h = t.width + 2 * pad, t.height + pad
    out = Image.new('RGBA', (big.width + plate_w // 2, big.height + plate_h // 3), (0, 0, 0, 0))
    out.paste(big, (0, 0), big)
    plate = Image.new('RGBA', (plate_w, plate_h), (0, 0, 0, 0))
    pd = ImageDraw.Draw(plate)
    pd.rounded_rectangle((0, 0, plate_w - 1, plate_h - 1), 10, fill=TEAL, outline=TEAL_EDGE, width=4)
    pd.rounded_rectangle((7, 7, plate_w - 8, plate_h - 8), 6, outline=GOLD, width=3)
    plate.paste(t, (pad, pad // 2 - 2), t)
    out.paste(plate, (out.width - plate_w, out.height - plate_h), plate)
    return out


def faster_mushrooms():
    c = covers.compose('FasterMushrooms')
    b = stopwatch('4x', 0.3)   # default growth speed
    c.paste(b, (1020, 230), b)
    return c


def watering_cans():
    c = covers.compose('BiggerWateringCans')
    can = sprite('11.23.25.65', (2462, 1932, 2608, 2083), gain=1.6)
    plate(c, can, ((W - 146) // 2, 300), scale=1.0)
    return c


def infinity(cols=27, rows=13, cell=5):
    """Pixel-art infinity sign: a lemniscate traced on a small grid, mirrored left/right and up/down so it
    is exactly symmetric, then drawn like the banner title (gold, light top band, dark outline)."""
    import math
    grid = [[False] * cols for _ in range(rows)]
    a, cx, cy = (cols - 3) / 2, (cols - 1) / 2, (rows - 1) / 2
    for i in range(2000):
        t = 2 * math.pi * i / 2000
        den = 1 + math.sin(t) ** 2
        px, py = cx + a * math.cos(t) / den, cy + (rows - 3) / 2 / 0.3536 * math.sin(t) * math.cos(t) / den
        for gx in range(cols):
            for gy in range(rows):
                if (gx - px) ** 2 + (gy - py) ** 2 <= 0.8:
                    grid[gy][gx] = True
    sym = [[grid[gy][gx] or grid[gy][cols - 1 - gx] or grid[rows - 1 - gy][gx] or grid[rows - 1 - gy][cols - 1 - gx]
            for gx in range(cols)] for gy in range(rows)]
    small = Image.new('L', (cols + 2, rows + 2))
    for gy in range(rows):
        for gx in range(cols):
            if sym[gy][gx]:
                small.putpixel((gx + 1, gy + 1), 255)
    img = Image.new('RGBA', small.size, (0, 0, 0, 0))
    img.paste(DARK, (0, 0), small.filter(ImageFilter.MaxFilter(3)))
    img.paste('#ffd36a', (0, 0), small)
    return img.resize((img.width * cell, img.height * cell), Image.NEAREST)


def ore_boulders():
    c = covers.compose('InfiniteOreBoulders')
    inf = infinity()
    x, y = 940, 320
    d = ImageDraw.Draw(c)
    d.rounded_rectangle((x - 22, y - 22, x + inf.width + 21, y + inf.height + 21), 10, fill=TEAL, outline=TEAL_EDGE, width=4)
    d.rounded_rectangle((x - 12, y - 12, x + inf.width + 11, y + inf.height + 11), 6, outline=GOLD, width=3)
    c.paste(inf, (x, y), inf)
    return c


# Brute position on the MushroomSlayer canvas (where the number pops off).
BRUTE = (965, 400)


def mushroom_frames():
    base = covers.compose('MushroomSlayer')
    frames, durations = [], []
    steps = [(0, 0.0), (1, 0.15), (2, 0.35), (3, 0.6), (4, 0.85), (5, 1.0), (6, 1.0)]
    for i, t in steps:
        f = base.copy()
        if i:
            # hit flash: a pale burst on the Brute that fades
            burst = Image.new('L', (W, H), 0)
            r = round(40 + 60 * t)
            ImageDraw.Draw(burst).ellipse((BRUTE[0] - r, BRUTE[1] - r, BRUTE[0] + r, BRUTE[1] + r), fill=255)
            f = Image.composite(Image.new('RGB', (W, H), '#fff2c0'), f,
                                burst.filter(ImageFilter.GaussianBlur(18)).point(lambda v, a=1 - t: int(v * 0.55 * a)))
            num = big_text('-245', 13 if i < 2 else 16, '#ffcf3a' if i < 4 else '#ff8a3a')
            y = BRUTE[1] - 120 - round(90 * t)
            f.paste(num, (BRUTE[0] - num.width // 2, y), num)
            if i >= 2:
                tag(f, 'ARMOR BROKEN', (BRUTE[0], y + num.height + 6))
        frames.append(f)
        durations.append(700 if i == 0 else (1300 if i == len(steps) - 1 else 110))
    return frames, durations


def save_gif(frames, durations, path):
    pal = frames[-1].convert('P', palette=Image.ADAPTIVE, colors=255)
    q = [fr.quantize(palette=pal, dither=Image.Dither.NONE) for fr in frames]
    q[0].save(path, save_all=True, append_images=q[1:], duration=durations, loop=0, optimize=False, disposal=1)


BUILDERS = {'FishSeller': fish_seller, 'PotionSeller': potion_seller, 'FastAutoFishing': fast_fishing, 'VehicleSpeed': vehicle_speed, 'FasterMushrooms': faster_mushrooms, 'BuffDurationFloor': buff_duration, 'BiggerWateringCans': watering_cans, 'InfiniteOreBoulders': ore_boulders}

if __name__ == '__main__':
    args = sys.argv[1:]
    out = covers.REPO / 'build/artwork-review/2026-11-01/covers'
    if '--out' in args:
        i = args.index('--out')
        out = Path(args[i + 1])
        del args[i:i + 2]
    out.mkdir(parents=True, exist_ok=True)
    prepare()
    keys = args or ['FishSeller', 'BiggerWateringCans', 'AutoReplant', 'InfiniteOreBoulders', 'MushroomSlayer']
    for key in keys:
        if key == 'DurabilityMultiplier':
            still, frames, durations = durability_frames()
            still.save(out / 'DurabilityMultiplier.png', optimize=True)
            save_gif(frames, durations, out / 'DurabilityMultiplier.gif')
            paths = [out / 'DurabilityMultiplier.png', out / 'DurabilityMultiplier.gif']
        elif key == 'MushroomSlayer':
            frames, durations = mushroom_frames()
            frames[-2].save(out / 'MushroomSlayer.png', optimize=True)
            save_gif(frames, durations, out / 'MushroomSlayer.gif')
            paths = [out / 'MushroomSlayer.png', out / 'MushroomSlayer.gif']
        else:
            img = BUILDERS[key]() if key in BUILDERS else covers.compose(key)
            img.save(out / f'{key}.png', optimize=True)
            paths = [out / f'{key}.png']
        for p in paths:
            print(p, p.stat().st_size)
