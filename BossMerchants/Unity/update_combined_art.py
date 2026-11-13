"""Replace only More NPCs art in an existing (possibly installed) combined bundle.

python -I BossMerchants/Unity/update_combined_art.py INPUT OUTPUT [--art ART_FINAL]

Does not install anything. INPUT cannot be OUTPUT. All source images and targets are
validated before mutation; OUTPUT is atomically published only after re-opening and
verifying pixels, skin references/pivots, and every unrelated object's raw bytes.
Requires UnityPy and Pillow. Existing resource streams are retained byte-for-byte.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import tempfile

from PIL import Image
import UnityPy


HERE = Path(__file__).resolve().parent
KEYS = {
    'Blocks': 'blocks', 'Decor': 'decor', 'Explosives': 'explosives',
    'Potions': 'potions', 'Eggs': 'eggs', 'Hydras': 'hydras',
    'FarmAnimals': 'farmanimals', 'Seeds': 'seeds',
}
SKIN = 'bossMerchant_Eggs_skin'
EGG_ANIMATIONS = {
    'voidMerchant_idle': ('580fae3f-66dc-4b7a-900c-ee7986f742e6', 8, 35),
    'voidMerchant_move': ('7128f66e-0c6d-4aed-949e-93fdcd9f5138', 6, 34),
    'voidMerchant_emote': ('e1e86c38-70e3-402a-a63f-ecb6d1f3edf0', 27, 37),
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def identity(obj):
    return (obj.assets_file.name, obj.path_id)


def object_map(env):
    result = {}
    for obj in env.objects:
        key = identity(obj)
        require(key not in result, f'Duplicate object identity: {key}')
        result[key] = obj
    return result


def resources(env):
    """Snapshot resource streams, which other mods' textures may reference."""
    return {name: digest(value.bytes) for name, value in env.file.files.items()
            if hasattr(value, 'bytes')}


def load_sources(art):
    images, legacy, eggs = {}, set(), {}

    def add(name, path, expected_size):
        require(name not in images, f'Duplicate source target: {name}')
        with Image.open(path) as source:
            image = source.convert('RGBA')
        require(image.size == tuple(expected_size),
                f'{path}: size {image.size} differs from metadata {expected_size}')
        images[name] = image

    for key, folder in KEYS.items():
        info = json.loads((art / 'merchants' / folder / 'sheets.json').read_text(encoding='utf-8'))
        require(info['kind'] == ('spriteAsset' if key == 'Eggs' else 'legacy'),
                f'{key}: unexpected rig kind {info["kind"]}')
        for tx in info['textures']:
            name = (f'bossMerchant_{key}_skin_{tx["replacesTexture"]}' if key == 'Eggs'
                    else f'bossMerchant_{key}__{tx["replacesTexture"]}')
            path = (art / 'merchants' / tx['output']).resolve()
            require(path.is_relative_to((art / 'merchants' / folder).resolve()),
                    f'{key}: source outside its art directory: {path}')
            add(name, path, tx['size'])
            if key == 'Eggs':
                require(tx['replacesTexture'] in EGG_ANIMATIONS, f'Unknown egg animation: {name}')
                guid, count, height = EGG_ANIMATIONS[tx['replacesTexture']]
                require(tx['animationGuid'] == guid and tx['srcFrameCount'] == count,
                        f'{name}: changed animation GUID/count')
                width, actual_height = images[name].size
                require(actual_height == height and width > 0 and width % count == 0,
                        f'{name}: invalid frame dimensions/count')
                require(guid not in eggs, f'Duplicate egg animation: {guid}')
                eggs[guid] = name
            else:
                legacy.add(name)
        for part in ('icon', 'smallIcon', 'placed'):
            name = f'bossMerchant_{key}Idol_{part}'
            add(name, art / 'idols' / f'{folder}_{part}.png', (16, 16))
            legacy.add(name)
    require(len(eggs) == 3, 'Missing egg animations')
    return images, legacy, eggs


def find_targets(env, names):
    textures, skins = {}, []
    for obj in env.objects:
        if obj.type.name == 'Texture2D':
            name = obj.read().m_Name
            if name in names:
                require(name not in textures, f'Duplicate texture target: {name}')
                textures[name] = obj
        elif obj.type.name == 'MonoBehaviour':
            # The bundle includes type trees; do not load or mutate external bundles.
            tree = obj.read_typetree()
            if tree.get('m_Name') == SKIN:
                skins.append((obj, tree))
    require(set(textures) == set(names), f'Missing textures: {sorted(set(names) - set(textures))}')
    require(len(skins) == 1, f'Expected one {SKIN}, found {len(skins)}')
    return textures, skins[0]


def updated_skin(obj, tree, textures, eggs):
    result = copy.deepcopy(tree)
    replacements = result['m_replacementData']
    # SpriteObject.GetReplacementAt uses the native animation INDEX, not the GUID lookup.
    native_order = ['580fae3f-66dc-4b7a-900c-ee7986f742e6',
                    'e1e86c38-70e3-402a-a63f-ecb6d1f3edf0',
                    '7128f66e-0c6d-4aed-949e-93fdcd9f5138']
    replacements.sort(key=lambda r: native_order.index(r['m_guid']))
    require(len(replacements) == 3, 'Egg skin must have three animation replacements')
    require({r['m_guid'] for r in replacements} == set(eggs), 'Egg skin animation GUID mismatch')
    address = result['m_targetAssetRef']['m_address']
    require(address == {'m_low': 9202072890546798702, 'm_high': -5828687261374344469},
            'Egg skin does not target vanilla voidMerchant')
    idle_name = 'bossMerchant_Eggs_skin_voidMerchant_idle'
    for replacement in [result['m_staticReplacementData'], *replacements]:
        require(not replacement['m_variants'], 'Unexpected egg skin variants; refusing to alter them')
        name = eggs[replacement['m_guid']] if replacement in replacements else idle_name
        tex_obj = textures[name]
        require(tex_obj.assets_file is obj.assets_file, 'Egg texture is in a different serialized file')
        data = replacement['m_spriteData']
        require(data['texture'] == {'m_FileID': 0, 'm_PathID': tex_obj.path_id},
                f'Egg skin texture reference mismatch: {name}')
        data['pivot'] = {'x': 0.5, 'y': 0.0}
        data['inheritPivot'] = False
    return result


def update_bundle(source, output, art):
    source, output, art = source.resolve(), output.resolve(), art.resolve()
    require(source != output, 'Refusing to overwrite input bundle')
    require(not output.exists() or not os.path.samefile(source, output),
            'Output is a hard link to input bundle')
    input_hash = digest(source.read_bytes())
    images, legacy, eggs = load_sources(art)
    # Load bytes so UnityPy does not hold a Windows file handle across atomic rename.
    env = UnityPy.load(source.read_bytes())
    require(hasattr(env.file, 'files'), 'Input is not a Unity bundle')
    before = object_map(env)
    raw_before = {key: digest(obj.get_raw_data()) for key, obj in before.items()}
    resource_before = resources(env)
    file_names = set(env.file.files)
    textures, (skin_obj, skin_tree) = find_targets(env, images)
    expected_skin = updated_skin(skin_obj, skin_tree, textures, eggs)
    changed_ids = {identity(obj) for obj in textures.values()} | {identity(skin_obj)}
    for name in legacy:
        texture = textures[name].read()
        require((texture.m_Width, texture.m_Height) == images[name].size,
                f'Legacy texture dimensions cannot change: {name}')

    # Preserve path IDs, names and all references. RGBA32 avoids lossy encoding.
    for name, obj in textures.items():
        texture = obj.read()
        texture.set_image(images[name], target_format=4, mipmap_count=1)
        texture.save()
    skin_obj.save_typetree(expected_skin)

    output.parent.mkdir(parents=True, exist_ok=True)
    fd, temp_name = tempfile.mkstemp(prefix=output.name + '.', suffix='.tmp', dir=output.parent)
    temp = Path(temp_name)
    try:
        with os.fdopen(fd, 'wb') as stream:
            stream.write(env.file.save(packer='lz4'))
            stream.flush()
            os.fsync(stream.fileno())
        reopened = UnityPy.load(temp.read_bytes())
        after = object_map(reopened)
        require(set(before) == set(after), 'Object identities changed')
        require(set(reopened.file.files) == file_names, 'Bundle file entries changed')
        require(resources(reopened) == resource_before, 'Resource streams changed')
        for key, obj in after.items():
            if key not in changed_ids:
                require(digest(obj.get_raw_data()) == raw_before[key],
                        f'Unrelated object bytes changed: {key} ({obj.type.name})')
        check_textures, (check_skin, check_tree) = find_targets(reopened, images)
        require(identity(check_skin) == identity(skin_obj) and check_tree == expected_skin,
                'Skin changed beyond the four pivot/inheritPivot fields')
        updated_skin(check_skin, check_tree, check_textures, eggs)  # validate refs again
        for name, obj in check_textures.items():
            require(identity(obj) == identity(textures[name]), f'Texture identity changed: {name}')
            image = obj.read().image.convert('RGBA')
            require(image.size == images[name].size and image.tobytes() == images[name].tobytes(),
                    f'Reopened texture pixels differ from source: {name}')
        require(digest(source.read_bytes()) == input_hash, 'Input bundle changed during update')
        os.replace(temp, output)
    finally:
        if temp.exists():
            temp.unlink()
    print(f'OK: {len(images)} textures verified pixel-exact; egg skin pivots updated; '
          f'{len(before) - len(changed_ids)} unrelated objects and {len(resource_before)} streams unchanged')
    print(output)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--art', type=Path, default=HERE / 'Art' / 'final')
    args = parser.parse_args()
    try:
        update_bundle(args.input, args.output, args.art)
    except (ValueError, KeyError, OSError) as error:
        parser.exit(1, f'FAIL: {error}\n')


if __name__ == '__main__':
    main()
