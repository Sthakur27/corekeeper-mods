"""Independent, read-only checks for imported More NPCs art.

python -I BossMerchants/Unity/validate_approved_art.py [--root PATH]
Defaults to Art/approved_build. Does not import or execute the art importer.
Native reference metadata is checked when the local extracted reference exists.
This validates PNG/layout readiness, not Unity rendering or animation quality.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
KEYS = ('blocks', 'decor', 'explosives', 'potions', 'eggs', 'hydras', 'farmanimals', 'seeds')
FLOORS = dict(blocks=25, explosives=25, decor=37, potions=53,
              hydras=53, farmanimals=53, seeds=53)
RIGS = dict(blocks='SlimeMerchant', explosives='SlimeMerchant', decor='FishingMerchant',
            potions='CavelingMerchant', hydras='CavelingMerchant',
            farmanimals='CavelingMerchant', seeds='CavelingMerchant', eggs='VoidMerchant')
EXPECTED = {'Slime_merchant': ((300, 120), 38), 'Fisher_merchant': ((260, 120), 30),
            'Merchant': ((640, 192), 24), 'Merchant_Light': ((640, 192), 24),
            'Merchant_Eye': ((640, 128), 18),
            'voidMerchant_idle': ((256, 35), 8), 'voidMerchant_move': ((192, 34), 6),
            'voidMerchant_emote': ((864, 37), 27)}


def require(ok, label):
    if not ok:
        raise ValueError(label)


def load_png(path, palette_limit):
    im = Image.open(path)
    require(im.mode == 'RGBA', f'{path}: expected RGBA')
    a = np.array(im)
    require(set(np.unique(a[:, :, 3])) <= {0, 255}, f'{path}: nonbinary alpha')
    visible = a[:, :, 3] > 0
    colours = len(np.unique(a[:, :, :3][visible], axis=0))
    require(colours <= palette_limit, f'{path}: {colours} visible colours > {palette_limit}')
    require(not np.any(a[:, :, :3][~visible]), f'{path}: hidden RGB contamination')
    return im, a, colours


def boxes_for(t, kind):
    width, height = t['size']
    if kind == 'spriteAsset':
        count = t['srcFrameCount']
        require(width % count == 0, f'{t["replacesTexture"]}: nonintegral cell width')
        cw = width // count
        return [(i, (i*cw, 0, (i+1)*cw, height)) for i in range(count)]
    boxes = []
    for s in t['legacySprites']:
        rect = s['rect']
        require(all(int(n) == n for n in rect), f'{s["name"]}: nonintegral rect')
        x, y, w, h = map(int, rect)
        boxes.append((int(s['name'].rsplit('_', 1)[1]), (x, height-y-h, x+w, height-y)))
    return boxes


def groups(key, name):
    if key == 'eggs':
        return [('all', range(EXPECTED[name][1]))]
    if key in ('blocks', 'explosives'):
        return [('idle', range(10)), ('walk', range(10, 16)),
                ('arrival', range(16, 32)), ('greet', range(32, 38))]
    if key == 'decor':
        return [('idle', range(16)), ('walk', range(16, 24)), ('greet', range(24, 30))]
    return [('idle', range(10)), ('walk', range(10, 18)), ('greet', range(18, 24))]


def check(root):
    ref_path = HERE.parents[1] / 'build/bossmerchants/reference/reference.json'
    refs = json.loads(ref_path.read_text())['merchants'] if ref_path.exists() else {}
    report_path = root / 'report.json'
    reports = {r['key']: r for r in json.loads(report_path.read_text())} if report_path.exists() else {}
    results = []
    for key in KEYS:
        layout = json.loads((root / 'merchants' / key / 'sheets.json').read_text())
        require(layout['key'] == key, f'{key}: layout key mismatch')
        require(layout['rig'] == RIGS[key] + 'Entity', f'{key}: rig mismatch')
        frames_by_name = {}; palettes = {}; unique_counts = {}
        expected_names = ({'voidMerchant_idle', 'voidMerchant_move', 'voidMerchant_emote'} if key == 'eggs'
                          else set(refs[RIGS[key]]['sheets']) if refs else
                          {'Slime_merchant'} if key in ('blocks', 'explosives') else
                          {'Fisher_merchant'} if key == 'decor' else {'Merchant', 'Merchant_Light', 'Merchant_Eye'})
        require({t['replacesTexture'] for t in layout['textures']} == expected_names,
                f'{key}: missing/extra texture')
        for t in layout['textures']:
            name = t['replacesTexture']; expected_size, expected_count = EXPECTED[name]
            require(tuple(t['size']) == expected_size, f'{key}/{name}: unexpected sheet size')
            path = root / 'merchants' / t['output']
            im, a, palettes[name] = load_png(path, 48)
            require(im.size == expected_size, f'{key}/{name}: PNG size disagrees')
            boxes = boxes_for(t, layout['kind'])
            require(len(boxes) == expected_count, f'{key}/{name}: wrong cell count')
            require({i for i, _ in boxes} == set(range(expected_count)), f'{key}/{name}: frame indices')
            if key == 'eggs':
                require(t['pivot'] == [.5, 0.0], f'{key}/{name}: wrong pivot')
                require(t['srcFrameCount'] == expected_count, f'{key}/{name}: source count')
                if refs:
                    animations = refs['VoidMerchant']['spriteAssets'][0]['animations']
                    original = next(v for v in animations if v['sprite']['texture'] == name)
                    require(t['animationGuid'] == original['guid'], f'{key}/{name}: animation GUID changed')
                    require(all(t[v] == original[v] for v in ('srcFrameCount', 'fps', 'loop')),
                            f'{key}/{name}: timing/count changed')
            elif refs:
                original = refs[RIGS[key]]['sheets'][name]
                require(t['legacySprites'] == original, f'{key}/{name}: native rect/pivot metadata changed')
            coverage = np.zeros(a.shape[:2], dtype=np.uint8); frames = {}
            overlay = name.endswith(('_Light', '_Eye'))
            for index, box in boxes:
                x0, y0, x1, y1 = box
                require(0 <= x0 < x1 <= im.width and 0 <= y0 < y1 <= im.height,
                        f'{key}/{name}/{index}: out-of-bounds cell')
                coverage[y0:y1, x0:x1] += 1
                frame = im.crop(box); frames[index] = np.array(frame)
                if not overlay:
                    bb = frame.getbbox()
                    floor = frame.height-1 if key == 'eggs' else FLOORS[key]
                    require(bb is not None, f'{key}/{name}/{index}: empty frame')
                    require(bb[3] == floor+1, f'{key}/{name}/{index}: incorrect ground row {bb}')
            require(not np.any(coverage > 1), f'{key}/{name}: overlapping cells')
            require(not np.any(a[:, :, 3][coverage == 0]), f'{key}/{name}: pixels outside defined cells')
            frames_by_name[name] = frames
            if not overlay:
                for group, indices in groups(key, name):
                    count = len({frames[i].tobytes() for i in indices})
                    unique_counts[name + '/' + group] = count
                    require(count >= 2, f'{key}/{name}/{group}: static animation')
        if 'Merchant' in frames_by_name:
            for name in ('Merchant_Eye', 'Merchant_Light'):
                for index, overlay in frames_by_name[name].items():
                    mask = overlay[:, :, 3] > 0
                    if name.endswith('_Eye'):
                        require(not mask.any(), f'{key}: stale eye layer at frame {index}')
                    elif mask.any():
                        base = frames_by_name['Merchant'][index]
                        require(np.all(base[:, :, 3][mask] == 255), f'{key}: light outside body at {index}')
                        require(np.array_equal(base[mask], overlay[mask]), f'{key}: light colour/position drift at {index}')
        for suffix, limit in [('icon', 32), ('placed', 32), ('smallIcon', 24)]:
            im, a, _ = load_png(root / 'idols' / f'{key}_{suffix}.png', limit)
            require(im.size == (16, 16) and im.getbbox(), f'{key}/{suffix}: size/empty idol')
            if suffix != 'smallIcon':
                require(im.getbbox()[3] == 16, f'{key}/{suffix}: not bottom aligned')
            else:
                b = im.getbbox()
                require(b[2]-b[0] <= 10 and b[3]-b[1] <= 11, f'{key}: small icon too large')
        require((root / 'idols' / f'{key}_icon.png').read_bytes() ==
                (root / 'idols' / f'{key}_placed.png').read_bytes(), f'{key}: placed/icon mismatch')
        first_name = next(n for n in frames_by_name if not n.endswith(('_Light', '_Eye')))
        current = Image.fromarray(frames_by_name[first_name][0]).getbbox()
        comparison = None
        if refs and key != 'eggs':
            native_path = ref_path.parent / 'merchants' / RIGS[key] / (first_name + '.png')
            if native_path.exists():
                native_im = Image.open(native_path).convert('RGBA')
                rect = refs[RIGS[key]]['sheets'][first_name][0]['rect']
                x,y,w,h = map(int,rect)
                bb = native_im.crop((x,native_im.height-y-h,x+w,native_im.height-y)).getbbox()
                comparison = {'imported': [current[2]-current[0],current[3]-current[1]],
                              'native': [bb[2]-bb[0],bb[3]-bb[1]]}
        elif refs and key == 'eggs':
            native_path = ref_path.parent / 'merchants/VoidMerchant/voidMerchant_idle.png'
            if native_path.exists():
                native_im = Image.open(native_path).convert('RGBA')
                bb = native_im.crop((0, 0, native_im.width//8, native_im.height)).getbbox()
                comparison = {'imported': [current[2]-current[0],current[3]-current[1]],
                              'native': [bb[2]-bb[0],bb[3]-bb[1]],
                              'intentionalChange': 'Scarab uses wider 32px cells; original idle cells 16px'}
        if key in reports:
            source = HERE / 'Art/approved_sources' / key / 'source.png'
            require(hashlib.sha256(source.read_bytes()).hexdigest() == reports[key]['sourceSha256'],
                    f'{key}: report/source digest stale')
        results.append({'key': key, 'palettes': palettes, 'distinctFrames': unique_counts,
                        'firstFrameBounds': current, 'scaleComparison': comparison})
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=HERE / 'Art/approved_build')
    args = parser.parse_args()
    results = check(args.root)
    print(json.dumps(results, indent=2))
    print(f'PASS: {len(results)} NPC sets and 24 idol PNGs; native metadata, alpha, palette, cells, floors, animation diversity and overlay alignment.')


if __name__ == '__main__':
    main()
