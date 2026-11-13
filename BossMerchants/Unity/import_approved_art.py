"""Import approved image_gen cutouts into the game's native animation atlases.

python -I BossMerchants/Unity/import_approved_art.py [--only key,...] [--apply]

Artistic source edits are made with image_gen. This deterministic build step handles
hard alpha, native pixel sizing, palette quantization, modest idle/walk/greeting
motion, and atlas packing. Source PNGs and prompts remain untouched. It does not run
the old procedural generators or palette finisher, which would replace the design.
"""
from pathlib import Path
import argparse
import colorsys
import hashlib
import json
import math
import shutil
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ART = HERE / 'Art'
ROOT = HERE.parents[1]
OUT = ART / 'approved_build'
KEYS = ['blocks', 'decor', 'explosives', 'potions', 'eggs', 'hydras', 'farmanimals', 'seeds']
# Bounds are read from generated alpha, not the RGB halo. Native bounds include props.
SETTINGS = {
    'blocks': (26, 24, 25), 'explosives': (24, 25, 25),
    'decor': (24, 35, 37), 'potions': (31, 49, 53),
    'eggs': (27, 32, 34), 'hydras': (34, 45, 53),
    'farmanimals': (32, 49, 53), 'seeds': (32, 43, 53),
}
IDOL_LIMITS = {key: (16, 16) for key in KEYS}
SMALL_ICON_LIMITS = {key: (10, 11) for key in KEYS}
NAMES = {'blocks': 'Grout', 'decor': 'Ottoline', 'explosives': 'Fizzwick',
         'potions': 'Hemlock', 'eggs': 'Ankhra', 'hydras': 'Tridra',
         'farmanimals': 'Wren', 'seeds': 'Tilly'}


def cutouts(key):
    p = ART / 'approved_sources' / key / 'source.png'
    parts, digest = source_cutouts(p)
    # An approved NPC-only revision must not replace its companion item's art.
    override = p.with_name('npc_source.png')
    if override.exists():
        revised, _ = source_cutouts(override)
        parts[0] = revised[0]
    return parts, digest


def source_cutouts(p):
    rgba = np.array(Image.open(p).convert('RGBA'))
    # Generated files may have maximum alpha 254 and RGB in invisible pixels.
    rgba[rgba[:, :, 3] < 200] = 0
    rgba[rgba[:, :, 3] >= 200, 3] = 255
    occupied = np.any(rgba[:, :, 3] > 0, axis=0)
    # Widest clear vertical gutter between the two objects, around the middle.
    runs = []; start = None
    for x, used in enumerate(occupied):
        if not used and start is None:
            start = x
        elif used and start is not None:
            if start > 0 and x > rgba.shape[1] * .35 and start < rgba.shape[1] * .85:
                runs.append((x - start, start, x))
            start = None
    assert runs, f'{p}: no transparent gutter between NPC and idol'
    _, left, right = max(runs)
    split = (left + right) // 2
    im = Image.fromarray(rgba)
    parts = []
    for box in [(0, 0, split, im.height), (split, 0, im.width, im.height)]:
        crop = im.crop(box)
        bb = crop.getbbox()
        assert bb, f'{p}: missing object'
        parts.append(crop.crop(bb))
    return parts, hashlib.sha256(p.read_bytes()).hexdigest()


def native(im, limit, colours):
    """Nearest pixel sampling and nondithered palette quantization; no source edits."""
    scale = min(limit[0] / im.width, limit[1] / im.height)
    size = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
    small = im.resize(size, Image.Resampling.NEAREST)
    a = np.array(small)
    mask = a[:, :, 3] >= 150
    # Build palette only from visible pixels, excluding transparent RGB.
    pixels = a[:, :, :3][mask]
    assert len(pixels)
    strip = Image.fromarray(pixels.reshape(1, -1, 3))
    palette = strip.quantize(colors=max(8, colours - 16), method=Image.Quantize.MEDIANCUT,
                             dither=Image.Dither.NONE)
    # A population-only quantizer discards a two-pixel cyan lantern or golden eye.
    # Reserve accent entries across hues so small material highlights survive.
    entries = [tuple(palette.getpalette()[i:i + 3]) for i in range(0, max(8, colours - 16) * 3, 3)]
    buckets = [[] for _ in range(8)]
    for rgb in np.unique(pixels, axis=0):
        h, s, v = colorsys.rgb_to_hsv(*(rgb / 255.0))
        buckets[min(7, int(h * 8))].append((v, s * v, tuple(int(c) for c in rgb)))
    for bucket in buckets:
        if bucket:
            entries.extend([max(bucket, key=lambda c: c[0])[2], max(bucket, key=lambda c: c[1])[2]])
    entries = list(dict.fromkeys(entries))[:colours]
    entries += [entries[0]] * (256 - len(entries))
    palette.putpalette([c for rgb in entries for c in rgb])
    rgb = small.convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE).convert('RGB')
    result = np.array(rgb.convert('RGBA'))
    result[:, :, 3] = mask * 255
    result[~mask] = 0
    out = Image.fromarray(result)
    return out.crop(out.getbbox())


def motion(key, index, texture):
    if key == 'eggs':
        mode = 'idle' if texture.endswith('_idle') else 'walk' if texture.endswith('_move') else 'greet'
        n = 8 if mode == 'idle' else 6 if mode == 'walk' else 27
        return mode, index / n
    if key in ('blocks', 'explosives'):
        if index < 10: return 'idle', index / 10
        if index < 16: return 'walk', (index - 10) / 6
        if index < 32: return 'arrive', (index - 16) / 15
        return 'greet', (index - 32) / 6
    if key == 'decor':
        if index < 16: return 'idle', index / 16
        if index < 24: return 'walk', (index - 16) / 8
        return 'greet', (index - 24) / 6
    if index < 10: return 'idle', index / 10
    if index < 18: return 'walk', (index - 10) / 8
    return 'greet', (index - 18) / 6


def animate(base, key, index, texture, size, ground):
    mode, phase = motion(key, index, texture)
    wave = math.sin(phase * math.tau)
    # Keep the ground contact fixed. Native one-pixel motions avoid subpixel blur.
    compress = (1 if wave > .35 else 0) if mode == 'idle' else (
        round((1 - math.cos(phase * math.tau)) * 1.0) if mode == 'greet' else
        (1 if abs(wave) > .6 else 0))
    if mode == 'arrive':
        compress = 0
    tile = base.resize((base.width, base.height - compress), Image.Resampling.NEAREST)
    data = np.array(tile)
    canvas = np.zeros((size[1], size[0], 4), dtype=np.uint8)
    x0 = (size[0] - tile.width) // 2
    y0 = ground + 1 - tile.height
    for y in range(tile.height):
        # Gentle upper-body sway; lower rows alternate feet / ripple a slime base.
        shift = 0
        if mode == 'walk':
            shift = round(wave) if y < tile.height * .82 else -round(wave)
        elif mode == 'greet' and y < tile.height * .38:
            shift = round(wave)
        elif key in ('decor', 'hydras') and y > tile.height * .82:
            shift = round(wave * .7)
        if mode == 'arrive' and y < tile.height * (1 - phase) * .85:
            continue
        for x in range(tile.width):
            dx, dy = x0 + x + shift, y0 + y
            if data[y, x, 3]:
                assert 0 <= dx < size[0] and 0 <= dy < size[1], (key, index, dx, dy)
                canvas[dy, dx] = data[y, x]
    return Image.fromarray(canvas)


def emission(frame, key):
    """Only the upper lantern glints; no separate eye layer that can drift in greetings."""
    a = np.array(frame)
    r, g, b = [a[:, :, i].astype(int) for i in range(3)]
    ys = np.indices(a.shape[:2])[0]
    bb = frame.getbbox()
    mask = np.zeros(a.shape[:2], bool)
    if key == 'potions':
        mask = (g > 180) & (b > 145) & (g > r * 1.15) & (ys < bb[1] + (bb[3] - bb[1]) * .36)
    elif key == 'farmanimals':
        mask = (r > 215) & (g > 170) & (b < 150) & (ys < bb[1] + (bb[3] - bb[1]) * .32)
    a[~mask] = 0
    return Image.fromarray(a)


def cell_box(rect, height):
    x, y, w, h = map(int, rect)
    return x, height - y - h, x + w, height - y


def make_key(key):
    source_parts, digest = cutouts(key)
    maxw, maxh, ground = SETTINGS[key]
    base = native(source_parts[0], (maxw, maxh), 48)
    idol = native(source_parts[1], IDOL_LIMITS[key], 32)
    small = native(source_parts[1], SMALL_ICON_LIMITS[key], 24)
    layout = json.loads((ART / 'merchants' / key / 'sheets.json').read_text())
    dest = OUT / 'merchants' / key
    dest.mkdir(parents=True, exist_ok=True)
    idle = None; walk = []; counts = {}
    for t in layout['textures']:
        name = t['replacesTexture']
        if layout['kind'] == 'spriteAsset':
            # SpriteObject uses replacement texture width / original srcFrameCount.
            # Widen the scarab's old 16px robe rig without distorting the approved shell.
            count = t['srcFrameCount']; cw = 32; ch = t['size'][1]
            t['size'] = [cw * count, ch]; t['pivot'] = [.5, 0.0]
            boxes = [(i * cw, 0, (i + 1) * cw, ch) for i in range(count)]
            indices = range(count)
        else:
            boxes = [cell_box(s['rect'], t['size'][1]) for s in t['legacySprites']]
            indices = [int(s['name'].rsplit('_', 1)[1]) for s in t['legacySprites']]
        sheet = Image.new('RGBA', tuple(t['size']))
        is_eye = name.endswith('_Eye'); is_light = name.endswith('_Light')
        for box, index in zip(boxes, indices):
            size = (box[2] - box[0], box[3] - box[1])
            floor = size[1] - 1 if key == 'eggs' else ground
            frame = animate(base, key, index, name, size, floor)
            if is_eye:
                frame = Image.new('RGBA', size)
            elif is_light:
                frame = emission(frame, key)
            else:
                if idle is None: idle = frame.copy()
                mode, _ = motion(key, index, name)
                if mode == 'walk': walk.append(frame.copy())
            sheet.paste(frame, box[:2])
            if not is_eye and not is_light:
                assert frame.getbbox(), (key, name, index)
                assert frame.getbbox()[3] == floor + 1, (key, name, index, 'ground')
        assert set(np.unique(np.array(sheet)[:, :, 3])) <= {0, 255}
        assert sheet.size == tuple(t['size'])
        fn = f'{key}__{name}.png'; t['output'] = f'{key}/{fn}'
        sheet.save(dest / fn)
        counts[name] = len(boxes)
    (dest / 'sheets.json').write_text(json.dumps(layout, indent=1) + '\n', newline='\n')
    (OUT / 'idols').mkdir(exist_ok=True)
    for suffix, im in [('icon', idol), ('placed', idol), ('smallIcon', small)]:
        tile = Image.new('RGBA', (16, 16))
        tile.paste(im, ((16 - im.width) // 2, 16 - im.height if suffix != 'smallIcon' else (16 - im.height) // 2))
        tile.save(OUT / 'idols' / f'{key}_{suffix}.png')
    (OUT / 'previews').mkdir(exist_ok=True)
    # Lossless nearest-neighbour inspection at actual native pixel sizes.
    board = Image.new('RGBA', (1000, 420), '#25242d'); draw = ImageDraw.Draw(board)
    draw.text((14, 10), f'{NAMES[key]}: imported sprite at 6x | actual 1x | walk frames at 3x', fill='white')
    board.alpha_composite(idle.resize((idle.width * 6, idle.height * 6), Image.Resampling.NEAREST), (10, 30))
    board.alpha_composite(idle, (415, 50))
    icon = Image.open(OUT / 'idols' / f'{key}_icon.png').convert('RGBA')
    board.alpha_composite(icon.resize((128, 128), Image.Resampling.NEAREST), (425, 130))
    board.alpha_composite(icon, (570, 180))
    for j, f in enumerate(walk[:8]):
        tile = f.crop(f.getbbox())
        scale = 2 if tile.width > 27 else 3
        tile = tile.resize((tile.width * scale, tile.height * scale), Image.Resampling.NEAREST)
        board.alpha_composite(tile, (610 + (j % 4) * 95, 35 + (j // 4) * 180))
    board.save(OUT / 'previews' / f'{key}.png')
    # Animation preview uses actual atlas pixels, not generated illustrative poses.
    frames = []
    for f in walk:
        bg = Image.new('RGBA', f.size, '#25242d'); bg.alpha_composite(f)
        frames.append(bg.resize((f.width * 8, f.height * 8), Image.Resampling.NEAREST))
    if frames:
        frames[0].save(OUT / 'previews' / f'{key}-walk.gif', save_all=True,
                       append_images=frames[1:], duration=120, loop=0, disposal=2)
    return {'key': key, 'sourceSha256': digest, 'nativeBounds': list(base.size),
            'idolBounds': list(idol.size), 'frames': counts, 'paletteLimit': 48,
            'animation': 'pixel-aligned idle compression, alternating walk sway, greeting nod; slime arrival reveal'}


def lineup():
    board = Image.new('RGBA', (1280, 720), '#25242d')
    d = ImageDraw.Draw(board)
    d.text((14, 10), 'MORE NPCs - BUILT SPRITES: NPCs 4x / 1x, matching idols 6x', fill='white')
    for i, key in enumerate(KEYS):
        meta = json.loads((OUT / 'merchants' / key / 'sheets.json').read_text())
        tx = meta['textures'][0]
        im = Image.open(OUT / 'merchants' / tx['output']).convert('RGBA')
        box = (0, 0, im.width // tx['srcFrameCount'], im.height) if meta['kind'] == 'spriteAsset' else cell_box(tx['legacySprites'][0]['rect'], im.height)
        f = im.crop(box); f = f.crop(f.getbbox())
        x = i * 158 + 12
        board.alpha_composite(f.resize((f.width * 4, f.height * 4), Image.Resampling.NEAREST), (x, 235 - f.height * 4))
        d.text((x, 245), NAMES[key], fill='white')
        board.alpha_composite(f, (x, 280))
        idol = Image.open(OUT / 'idols' / f'{key}_icon.png').convert('RGBA')
        board.alpha_composite(idol.resize((96, 96), Image.Resampling.NEAREST), (x, 344))
    d.text((14, 460), 'EXTRACTED VANILLA MERCHANTS (4x) + vanilla summoning icons (6x)', fill='white')
    # Actual native assets; do not redraw or generate reference-looking substitutes.
    refs = [('SlimeMerchant', 'Slime_merchant', (0, 0, 30, 30)),
            ('FishingMerchant', 'Fisher_merchant', (0, 0, 26, 40)),
            ('CavelingMerchant', 'Merchant', (0, 0, 64, 64)),
            ('VoidMerchant', 'voidMerchant_idle', (0, 0, 16, 35))]
    for i, (folder, name, box) in enumerate(refs):
        f = Image.open(ROOT / 'build/bossmerchants/reference/merchants' / folder / (name + '.png')).convert('RGBA').crop(box)
        f = f.crop(f.getbbox())
        board.alpha_composite(f.resize((f.width * 4, f.height * 4), Image.Resampling.NEAREST), (14 + i * 155, 690 - f.height * 4))
        d.text((14 + i * 155, 700), folder.replace('Merchant', ''), fill='white')
    for i, name in enumerate(['3930_MysteriousIdol', '3931_SlimeOil', '3932_PileOfChum', '3937_VoidMerchantIdol']):
        f = Image.open(ROOT / 'build/bossmerchants/idol_reference' / (name + '_icon.png')).convert('RGBA')
        board.alpha_composite(f.resize((96, 96), Image.Resampling.NEAREST), (670 + i * 140, 520))
    board.save(OUT / 'previews' / 'lineup.png')


def main():
    global OUT
    p = argparse.ArgumentParser(); p.add_argument('--only'); p.add_argument('--apply', action='store_true')
    p.add_argument('--profile', type=Path, help='Explicit native-size review profile; default art remains unchanged')
    p.add_argument('--out', type=Path, help='Separate staging directory for a review pass')
    args = p.parse_args(); keys = args.only.split(',') if args.only else KEYS
    if args.profile:
        profile = json.loads(args.profile.read_text(encoding='utf-8'))
        for key, spec in profile['npcs'].items():
            SETTINGS[key] = (*spec['maxSpriteSize'], SETTINGS[key][2])
        for key, spec in profile['items'].items():
            IDOL_LIMITS[key] = tuple(spec['maxSpriteSize'])
            SMALL_ICON_LIMITS[key] = tuple(spec['smallIconSize'])
    if args.out: OUT = args.out.resolve()
    assert set(keys) <= set(KEYS)
    OUT.mkdir(parents=True, exist_ok=True)
    report = []
    for key in keys:
        report.append(make_key(key)); print(key, report[-1]['nativeBounds'], 'validated')
    (OUT / 'report.json').write_text(json.dumps(report, indent=2) + '\n', newline='\n')
    if set(keys) == set(KEYS): lineup()
    if args.apply:
        for key in keys:
            for root in [ART, ART / 'final']:
                shutil.copytree(OUT / 'merchants' / key, root / 'merchants' / key, dirs_exist_ok=True)
                for suffix in ['icon', 'placed', 'smallIcon']:
                    shutil.copy2(OUT / 'idols' / f'{key}_{suffix}.png', root / 'idols' / f'{key}_{suffix}.png')
        print('Applied approved assets; do not run legacy procedural art generators over these files.')


if __name__ == '__main__':
    main()
