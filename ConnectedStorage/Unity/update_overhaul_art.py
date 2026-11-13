"""Transfer verified storage artwork to an existing Overhaul bundle, preserving unrelated objects."""
import argparse
from pathlib import Path

import UnityPy

from apply_release_art import TIERS, VIEWS

GEMS = {tier + 'Gem' for tier in TIERS}
NAMES = GEMS | {'Workbench', 'OctarineChest'}
TEXTURES = {name + suffix for name in NAMES
            for suffix in [v + e for v in VIEWS for e in ('', '_emissive')] + ['_icon']}
GEM_SPRITES = {name for name in TEXTURES if name.split('_')[0] in GEMS}


def named(env, kind, names):
    found = {}
    for obj in env.objects:
        if obj.type.name != kind:
            continue
        name = obj.peek_name()
        if name in names:
            assert name not in found, (kind, name)
            found[name] = obj
    assert found.keys() == names, (kind, names - found.keys())
    return found


def snapshot(env):
    return {(o.assets_file.name, o.path_id): o.get_raw_data() for o in env.objects}


def resources(env):
    return {name: f.bytes for name, f in env.file.files.items() if hasattr(f, 'bytes')}


def patch(source, standalone, output):
    assert output.resolve() not in {source.resolve(), standalone.resolve()}
    env, fresh = [UnityPy.load(p.read_bytes()) for p in (source, standalone)]
    before = snapshot(env)
    streams = resources(env)
    changed = set()

    def mark(obj):
        changed.add((obj.assets_file.name, obj.path_id))

    images = {n: o.read().image.convert('RGBA')
              for n, o in named(fresh, 'Texture2D', TEXTURES).items()}
    for name, obj in named(env, 'Texture2D', TEXTURES).items():
        tex = obj.read()
        tex.set_image(images[name], target_format=4, mipmap_count=1)
        tex.save()
        mark(obj)
    sprite_geometry = ('m_Rect', 'm_Offset', 'm_Border', 'm_PixelsToUnits', 'm_Pivot',
                       'm_Extrude', 'm_IsPolygon', 'm_PhysicsShape')
    fresh_sprites = named(fresh, 'Sprite', GEM_SPRITES)
    for name, obj in named(env, 'Sprite', GEM_SPRITES).items():
        old, new = obj.read_typetree(), fresh_sprites[name].read_typetree()
        for key in sprite_geometry:
            old[key] = new[key]
        for key in new['m_RD']:
            if key not in ('texture', 'alphaTexture', 'secondaryTextures'):
                old['m_RD'][key] = new['m_RD'][key]
        obj.save_typetree(old)
        mark(obj)
    fresh_assets = named(fresh, 'MonoBehaviour', GEMS)
    for name, obj in named(env, 'MonoBehaviour', GEMS).items():
        old, new = obj.read_typetree(), fresh_assets[name].read_typetree()
        assert len(old['m_staticVariants']) == len(new['m_staticVariants']) == 2
        for a, b in zip([old['m_staticSpriteData'], *old['m_staticVariants']],
                        [new['m_staticSpriteData'], *new['m_staticVariants']]):
            a['pivot'] = b['pivot']
        obj.save_typetree(old)
        mark(obj)
    scaled = set()
    for obj in env.objects:
        if obj.type.name != 'Transform':
            continue
        tr = obj.read()
        if tr.m_GameObject.read().m_Name != 'ArtScale':
            continue
        parent = tr.m_Father.read()
        name = parent.m_Father.read().m_GameObject.read().m_Name
        if name not in {n + 'Graphics' for n in GEMS}:
            continue
        tree = obj.read_typetree()
        tree['m_LocalScale'] = dict(x=1.0, y=1.0, z=1.0)
        obj.save_typetree(tree)
        mark(obj)
        scaled.add(name)
    assert scaled == {n + 'Graphics' for n in GEMS}
    result = env.file.save(packer='lz4')
    reopened = UnityPy.load(result)
    after = snapshot(reopened)
    assert before.keys() == after.keys()
    assert all(after[k] == before[k] for k in before.keys() - changed)
    assert resources(reopened) == streams
    for name, obj in named(reopened, 'Texture2D', TEXTURES).items():
        actual = obj.read().image.convert('RGBA')
        assert actual.size == images[name].size and actual.tobytes() == images[name].tobytes(), name
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(result)
    print(f'{output.name}: {len(images)} exact textures, {len(before) - len(changed)} unrelated objects unchanged')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for arg in ('source', 'standalone', 'output'):
        parser.add_argument(arg, type=Path)
    args = parser.parse_args()
    patch(args.source, args.standalone, args.output)
