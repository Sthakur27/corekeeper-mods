"""Read-only native prefab/animation scale evidence for More NPCs size review.

python -I BossMerchants/Unity/size_review/extract_native_scales.py [--out DIR]
Writes JSON evidence only; does not export/change sprites, bundles or installed mods.
"""
import argparse
import glob
import json
from pathlib import Path
import struct
import zlib

import UnityPy
import numpy as np

GAME = Path(r'C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/StreamingAssets/aa/StandaloneWindows64')
ROOT = Path(__file__).resolve().parents[3]
NAMES = ['CavelingMerchant', 'SlimeMerchant', 'FishingMerchant', 'VoidMerchant',
         'MysteriousIdol', 'SlimeOil', 'PileOfChum', 'VoidMerchantIdol']


def vector(d, keys='xyz'):
    return [d[k] for k in keys]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, default=ROOT / 'build/bossmerchants/size-review-native')
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    inputs = glob.glob(str(GAME / '*monoscripts*')) + [str(GAME / 'defaultlocalgroup_assets_all.bundle')]
    env = UnityPy.load(*inputs)
    by_id = {}
    for obj in env.objects:
        if obj.type.name == 'AssetBundle':  # both bundles use path ID 1 for their container
            continue
        if obj.path_id in by_id:
            raise ValueError(f'Path ID collision: {obj.path_id}; use file-qualified pointer resolution')
        by_id[obj.path_id] = obj
    scripts = {o.path_id: o.read().m_ClassName for o in env.objects if o.type.name == 'MonoScript'}

    def tree(pid):
        return by_id[pid].read_typetree()

    def cls(t):
        return scripts.get(t.get('m_Script', {}).get('m_PathID'), 'unknown')

    def sprite(pid):
        if not pid:
            return None
        s = by_id[pid].read()
        return {'path_id': pid, 'name': s.m_Name,
                'rect': [s.m_Rect.x, s.m_Rect.y, s.m_Rect.width, s.m_Rect.height],
                'pivot': [s.m_Pivot.x, s.m_Pivot.y], 'ppu': s.m_PixelsToUnits,
                'texture_path_id': s.m_RD.texture.path_id}

    def matrix(t):
        x, y, z, w = vector(t['m_LocalRotation'], 'xyzw')
        rot = np.array([[1-2*y*y-2*z*z, 2*x*y-2*z*w, 2*x*z+2*y*w],
                        [2*x*y+2*z*w, 1-2*x*x-2*z*z, 2*y*z-2*x*w],
                        [2*x*z-2*y*w, 2*y*z+2*x*w, 1-2*x*x-2*y*y]])
        result = np.eye(4)
        result[:3, :3] = rot @ np.diag(vector(t['m_LocalScale']))
        result[:3, 3] = vector(t['m_LocalPosition'])
        return result

    clips = {}

    def clip_info(pid):
        if pid in clips:
            return clips[pid]
        t = tree(pid)
        # Retain complete transform/float curve and binding evidence. Sprite keyframes
        # are summarized separately; they cannot change transform scale themselves.
        evidence = {k: v for k, v in t.items() if k not in ('m_PPtrCurves', 'm_EditorCurves')}
        mappings = t.get('m_ClipBindingConstant', {}).get('pptrCurveMapping', [])
        evidence['sprite_references'] = [sprite(p['m_PathID']) for p in mappings
                                         if p['m_PathID'] in by_id and by_id[p['m_PathID']].type.name == 'Sprite']
        clips[pid] = evidence
        return evidence

    def walk(pid, parent_path='', parent_matrix=None):
        go = tree(pid)
        path = parent_path + '/' + go['m_Name']
        components = [by_id[c['component']['m_PathID']] for c in go['m_Component']]
        transform = next(c for c in components if c.type.name == 'Transform')
        t = transform.read_typetree()
        world = (np.eye(4) if parent_matrix is None else parent_matrix) @ matrix(t)
        node = {'path': path, 'go_path_id': pid, 'transform_path_id': transform.path_id,
                'local_position': t['m_LocalPosition'], 'local_rotation': t['m_LocalRotation'],
                'local_scale': t['m_LocalScale'], 'prefab_cumulative_matrix': world.tolist(),
                'prefab_axis_lengths': np.linalg.norm(world[:3, :3], axis=0).tolist(),
                'components': [], 'children': []}
        for component in components:
            if component.type.name == 'Transform':
                continue
            ct = component.read_typetree()
            item = {'type': component.type.name, 'path_id': component.path_id}
            if component.type.name == 'MonoBehaviour':
                item['class'] = cls(ct)
                item['tree'] = ct
            elif component.type.name == 'SpriteRenderer':
                item['sprite'] = sprite(ct['m_Sprite']['m_PathID'])
                item['tree'] = ct
                if item['sprite']:
                    ppu = item['sprite']['ppu']
                    item['world_units_per_sprite_pixel_xy'] = [float(v/ppu) for v in node['prefab_axis_lengths'][:2]]
            elif component.type.name == 'Animator':
                item['tree'] = ct
                controller_pid = ct['m_Controller']['m_PathID']
                if controller_pid:
                    controller = tree(controller_pid)
                    item['controller'] = {'name': controller['m_Name'], 'path_id': controller_pid,
                                          'clips': []}
                    for pointer in controller.get('m_AnimationClips', []):
                        clip = clip_info(pointer['m_PathID'])
                        item['controller']['clips'].append({'name': clip['m_Name'], 'path_id': pointer['m_PathID']})
            node['components'].append(item)
        for child in t['m_Children']:
            child_go = tree(child['m_PathID'])['m_GameObject']['m_PathID']
            node['children'].append(walk(child_go, path, world))
        return node

    report = {'inputs': [{'path': p, 'size': Path(p).stat().st_size} for p in inputs],
              'prefabs': {}, 'sprite_assets': {}, 'transform_animations': {}, 'clips': {}}
    wanted = set(NAMES + [n + 'Entity' for n in NAMES])
    for obj in env.objects:
        if obj.type.name == 'GameObject' and obj.peek_name() in wanted:
            node = walk(obj.path_id)
            expected_class = 'EntityMonoBehaviourData' if obj.peek_name().endswith('Entity') else obj.peek_name()
            if not any(c.get('class') == expected_class for c in node['components']):
                continue  # e.g. a loose SlimeMerchant sprite is not the graphics prefab
            report['prefabs'].setdefault(obj.peek_name(), []).append(node)
    addresses = set()

    def find_addresses(node):
        for comp in node['components']:
            t = comp.get('tree', {})
            if comp.get('class') == 'SpriteObject' and 'm_assetRef' in t:
                a = t['m_assetRef']['m_address']
                addresses.add((a['m_low'], a['m_high']))
        for child in node['children']:
            find_addresses(child)
    for nodes in report['prefabs'].values():
        for node in nodes:
            find_addresses(node)
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour':
            continue
        t = obj.read_typetree()
        if cls(t) == 'TransformAnimation':
            report['transform_animations'][t['m_Name']] = {'path_id': obj.path_id, 'tree': t}
        if cls(t) != 'SpriteAsset' or 'm_address' not in t:
            continue
        a = t['m_address']
        if (a['m_low'], a['m_high']) not in addresses:
            continue
        texture_info = {}
        datas = [t['m_staticSpriteData']] + [a['m_spriteData'] for a in t.get('m_animations', [])]
        for data in datas:
            for field in ('texture', 'emissiveTexture', 'normalTexture'):
                pid = data[field]['m_PathID']
                if pid and pid in by_id:
                    texture = by_id[pid].read()
                    texture_info[str(pid)] = {'name': texture.m_Name, 'width': texture.m_Width, 'height': texture.m_Height}
        report['sprite_assets'][t['m_Name']] = {'path_id': obj.path_id, 'tree': t, 'textures': texture_info}
    report['clips'] = clips
    # Unity's compiled generic transform attributes: 1 position, 2 quaternion,
    # 3 scale, 4 Euler angles. Keep packed data too; empty m_ScaleCurves is NOT
    # evidence of no scale animation in a player build.
    paths = {}
    def path_hashes(node, root):
        relative = node['path'][len(root):].lstrip('/')
        paths[zlib.crc32(relative.encode())] = relative
        for child in node['children']:
            path_hashes(child, root)
    for nodes in report['prefabs'].values():
        for node in nodes:
            path_hashes(node, node['path'])
    report['animation_summary'] = {}
    for clip in clips.values():
        bindings = clip['m_ClipBindingConstant']['genericBindings']
        stream = clip['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']
        raw = struct.pack('<' + 'I' * len(stream), *stream)
        offset, key_values = 0, {}
        while offset < len(raw):
            time, count = struct.unpack_from('<fI', raw, offset)
            offset += 8
            for _ in range(count):
                index, a, b, c, value = struct.unpack_from('<Iffff', raw, offset)
                offset += 20
                if np.isfinite(time) and time >= 0:
                    key_values.setdefault(index, []).append([time, value])
        binding_offset, scales = 0, []
        for binding in bindings:
            dim = 1
            if binding['typeID'] == 4:
                dim = 4 if binding['attribute'] == 2 else 3
                if binding['attribute'] == 3:
                    scales.append({'path': paths.get(binding['path'], f'unresolved hash {binding["path"]}'),
                                   'binding': binding,
                                   'streamed_key_values_xyz': [key_values.get(binding_offset + k, []) for k in range(3)]})
            binding_offset += dim
        report['animation_summary'][clip['m_Name']] = {'scale_bindings': scales}
    path = args.out / 'native-prefab-scales.json'
    path.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(path)

    def summary(node):
        for c in node['components']:
            if c['type'] == 'SpriteRenderer' and c['sprite']:
                print(node['path'], 'scale', node['prefab_axis_lengths'], 'ppu', c['sprite']['ppu'],
                      'sprite', c['sprite']['name'])
            if c.get('class') == 'SpriteObject':
                print(node['path'], 'SpriteObject scale', node['prefab_axis_lengths'])
        for ch in node['children']:
            summary(ch)
    for name in NAMES:
        for node in report['prefabs'].get(name, []):
            summary(node)
    for pid, clip in clips.items():
        print('CLIP', clip['m_Name'], 'scaleCurves', len(clip.get('m_ScaleCurves', [])),
              'floatCurves', len(clip.get('m_FloatCurves', [])))


if __name__ == '__main__':
    main()
