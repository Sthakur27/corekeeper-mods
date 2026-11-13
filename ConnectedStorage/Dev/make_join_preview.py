"""Author the local join preview's native sprite data blocks in the Mod SDK."""
import argparse
from hashlib import md5
from pathlib import Path
import re
from struct import unpack
import sys
from uuid import UUID

from PIL import Image

MOD = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(MOD / 'ArtConcepts/SharedHousing-2026-11-12'))
from make_fused_review import TIERS
from make_occlusion_review import assembly

ART = MOD / 'Unity/Art'
# Bit order matches StorageJoinPreview. Coordinates here are on the image.
OFFSETS = ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1))
CORNER_SIDES = (5, 6, 9, 10)


def stable_guid(name):
    return md5(f'RunicStorageJoinPreview/{name}'.encode()).hexdigest()


def write_asset(path, text, meta=''):
    path.write_text(text, encoding='utf-8', newline='\n')
    metadata = Path(f'{path}.meta')
    if not metadata.exists():
        metadata.write_text(
            f'fileFormatVersion: 2\nguid: {stable_guid(path.name)}\n{meta}', encoding='utf-8')


def write_texture(path, image):
    image.save(path)
    guid = stable_guid(path.name)
    metadata = Path(f'{path}.meta')
    if not metadata.exists():
        metadata.write_text(f'fileFormatVersion: 2\nguid: {guid}\n', encoding='utf-8')
    return f'{{fileID: 2800000, guid: {guid}, type: 3}}'


def generate(sdk):
    root = sdk / 'Assets/RunicStorageJoinPreview'
    data = root / 'Data'
    textures = root / 'Textures'
    for folder in (data, textures):
        folder.mkdir(parents=True, exist_ok=True)
    for tier in TIERS:
        source = sdk / f'Assets/ConnectedStorage/Data/SpriteAsset/{tier}Chest.asset'
        template = source.read_text(encoding='utf-8-sig')
        for mask, color, glow in tier_images(tier):
            name = f'RunicPreview_{tier}Chest_{mask}'
            color_ref = write_texture(textures / f'{name}.png', color)
            glow_ref = write_texture(textures / f'{name}_emissive.png', glow)
            path = data / f'{name}.asset'
            low, high = unpack('<qq', UUID(hex=stable_guid(path.name)).bytes_le)
            asset = template.replace(f'  m_Name: {tier}Chest\n', f'  m_Name: {name}\n')
            asset, count = re.subn(r'(?m)^  m_address:\n    m_low: -?\d+\n    m_high: -?\d+',
                                   f'  m_address:\n    m_low: {low}\n    m_high: {high}', asset)
            assert count == 1
            asset = re.sub(r'(?m)^    texture: .*', f'    texture: {color_ref}', asset, count=1)
            asset = re.sub(r'(?m)^    emissiveTexture: .*', f'    emissiveTexture: {glow_ref}', asset, count=1)
            asset = re.sub(r'(?ms)^  m_staticVariants:.*?(?=^  m_animations:)',
                           '  m_staticVariants: []\n', asset, count=1)
            write_asset(path, asset, 'NativeFormatImporter:\n  mainObjectFileID: 11400000\n')
    return root


def tier_images(tier):
    with Image.open(ART / f'{tier}Chest_emissive.png') as source:
        mask_source = source.convert('RGBA')
    for mask in range(256):
        if any(mask & (16 << i) and mask & sides != sides for i, sides in enumerate(CORNER_SIDES)):
            continue
        layout = {(1, 1): tier}
        for i, (dx, dy) in enumerate(OFFSETS):
            if mask & (1 << i):
                layout[1 + dx, 1 + dy] = tier
        color = assembly(layout, shorter_clamps=True, row_step=16).crop((16, 16, 32, 32))
        glow = Image.new('RGBA', (16, 16))
        for y in range(16):
            if mask & 8 and y >= 9:
                continue
            for x in range(16):
                if mask_source.getpixel((x, y))[3]:
                    glow.putpixel((x, y), color.getpixel((x, y)))
        yield mask, color, glow


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sdk', type=Path)
    generate(parser.parse_args().sdk)
