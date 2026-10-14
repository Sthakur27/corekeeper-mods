"""Inspect the shipped bundles, not the builder's source, for item/recipe integrity."""
from pathlib import Path
import json
import UnityPy
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
source = Image.open(ROOT / 'Unity/Art/MagicMirror_icon.png').convert('RGBA')
assert source.size == (16, 16)
manifest = json.loads((ROOT / 'ModManifest.json').read_text())
assert manifest['requiredOn'] == 3 and not manifest['skipSafetyChecks']
for file in manifest['files']:
    assert (ROOT / file['path']).is_file(), file['path']
assert {f['path'] for f in manifest['files'] if f['path'].endswith('.cs')} == {
    p.relative_to(ROOT).as_posix() for p in (ROOT / 'Scripts').rglob('*.cs')}

for folder in ('Bundles', 'OverhaulBundles'):
    bundles = list((ROOT / folder).glob('*.assetbundle'))
    assert len(bundles) == 2, folder
    for bundle in bundles:
        env = UnityPy.load(str(bundle))
        behaviours = [o.read_typetree() for o in env.objects if o.type.name == 'MonoBehaviour']
        mirror = next(x for x in behaviours if x.get('objectName') == 'MagicMirror_Mirror')
        assert mirror['objectType'] == 602 and mirror['initialAmount'] == 1
        owner = mirror['m_GameObject']
        components = [x for x in behaviours if x.get('m_GameObject') == owner]
        item = next(x for x in components if 'requiredObjectsToCraft' in x)
        assert item['requiredObjectsToCraft'] == [{'objectName': 'SolariteBar', 'amount': 50}]
        assert not item['isStackable']
        cast = next(x for x in components if 'castTime' in x)
        assert cast['castTime'] == 3 and cast['useType'] == 3 and not cast['allowHoldToRepeat']
        text = next(x for x in behaviours if x.get('m_Name') == 'MagicMirror_Mirror')
        assert text['m_header'] == 'Items'
        assert all(x['title'] == 'Magic Mirror' for x in text['m_localizedTexts']['values'])
        texture = next(o.read() for o in env.objects if o.type.name == 'Texture2D' and o.read().m_Name == 'MagicMirror_icon')
        actual = texture.image.convert('RGBA')
        assert actual.size == source.size
        # Unity dilates RGB into fully transparent texels to avoid edge fringes.
        # Compare alpha and visible color, not those deliberately invisible RGBs.
        for y in range(source.height):
            for x in range(source.width):
                a, b = actual.getpixel((x, y)), source.getpixel((x, y))
                assert a[3] == b[3] and (a[3] == 0 or a == b), (bundle, x, y)
        sprites = [o.read_typetree() for o in env.objects if o.type.name == 'Sprite']
        icon = next(x for x in sprites if x['m_Name'] == 'MagicMirror_icon')
        assert icon['m_Rect']['width'] == 16 and icon['m_Rect']['height'] == 16
        assert icon['m_PixelsToUnits'] == 16
        blocks = [x for x in behaviours if 'm_address' in x]
        addresses = [(x['m_address']['m_low'], x['m_address']['m_high']) for x in blocks]
        assert len(addresses) == len(set(addresses)), 'Duplicate data-block addresses'
        assert mirror['authoringRef']['m_address'] in [x['m_address'] for x in blocks]
        bundle_manifest = json.loads(Path(str(bundle) + '.manifest').read_text())
        assert any('MagicMirror.prefab' in x['path'] for x in bundle_manifest['assets'])
        if folder == 'OverhaulBundles':
            assert any(x.get('objectName') == 'EnderStash_EnderChest' for x in behaviours)
            assert any(x.get('m_Name') == 'EnderStash_EnderChest' for x in behaviours)
            assert any('EnderChestEntity.prefab' in x['path'] for x in bundle_manifest['assets'])
        for obj in env.objects:
            if obj.type.name == 'MonoScript':
                assert obj.read().m_AssemblyName not in ('MagicMirror', 'MagicMirror.dll', 'SidsOverhaul', 'SidsOverhaul.dll')
        print('PASS', bundle.name, 'recipe, cast, art, localization, references, manifest')
print('All shipped-asset checks passed.')
