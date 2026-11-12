"""Write the Boss Merchants source assets into the Mod SDK project (Assets/BossMerchants).

    python -I BossMerchants/Unity/gen_assets.py [sdk_path]

- Art/final/merchants/<key>/*.png (sheets.json) -> Textures/bossMerchant_<Key>__<vanilla texture>.png (runtime sprite swap, see
  Scripts/Visuals.cs) - except SpriteAsset-based bases (Void Merchant), whose sheets feed a SpriteAssetSkin.
- Art/final/idols/<key>_{icon,smallIcon,placed}.png -> Textures/bossMerchant_<Key>Idol_{icon,smallIcon,placed}.png
- one Items TextDataBlock per merchant and idol (name + description from docs/designs/<key>.json)
- vanilla_merchants.json (dump_vanilla.py) + the theme table, flattened to <sdk>/BossMerchantsVanilla.txt for
  the builder (outside Assets/, never bundled).
Fixed guids so rebuilds keep the same asset addresses.
"""
from pathlib import Path
import hashlib, json, re, shutil, struct, sys, zipfile

HERE = Path(__file__).resolve().parent
sdk = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r'C:/Users/Sid/CoreKeeperModSDK')
root = sdk / 'Assets/BossMerchants'
GUID = 'b055e7c4a1d24f6e8a3c9d1e{:08x}'  # text blocks: + 0x1000 + theme index; fixed forever

ART = HERE / 'Art/final'
DESIGNS = HERE.parent / 'docs/designs'
IDOL_BASE = 'MysteriousIdolEntity'  # every idol is a 16x16 placeable like the Mysterious Idol

# catalog Key (Scripts/Catalog.cs, object names BossMerchants_<Key>Merchant/Idol) -> design / art key
KEYS = [('Blocks', 'blocks'), ('Decor', 'decor'), ('Explosives', 'explosives'), ('Potions', 'potions'),
        ('Eggs', 'eggs'), ('Hydras', 'hydras'), ('FarmAnimals', 'farmanimals'), ('Seeds', 'seeds')]


def load_themes():
    """One theme per merchant from docs/designs/<key>.json + Art/final/merchants/<key>/sheets.json."""
    themes = []
    for key, art in KEYS:
        d = json.load(open(DESIGNS / f'{art}.json', encoding='utf-8'))
        sheets = json.load(open(ART / 'merchants' / art / 'sheets.json', encoding='utf-8'))
        if sheets['rig'] != d['rig']:
            sys.exit(f'{art}: sheets.json rig {sheets["rig"]} != design rig {d["rig"]}')
        themes.append(dict(key=key, art=art, mbase=d['rig'], ibase=IDOL_BASE, sheets=sheets,
                           mname=d['merchantName'], blurb=d['merchantBlurb'],
                           iname=d['idolName'], idesc=d['idolDescription']))
    return themes


def address(g):
    w1 = int(g[:8], 16)
    w2 = int(g[12:16] + g[8:12], 16)
    return struct.unpack('<qq', struct.pack('<II', w1, w2) + bytes.fromhex(g[16:]))


def flatten(values, path, out):
    if isinstance(values, dict):
        if set(values) == {'ref'}:
            out.append(f"R\t{path}\t{values['ref'] or ''}")
            return
        for k, v in values.items():
            flatten(v, f'{path}.{k}' if path else k, out)
    elif isinstance(values, list):
        out.append(f'A\t{path}\t{len(values)}')
        for i, v in enumerate(values):
            flatten(v, f'{path}.Array.data[{i}]', out)
    elif isinstance(values, str):
        out.append(f'S\t{path}\t{values}')
    elif isinstance(values, float):
        out.append(f'N\t{path}\t{values!r}')
    else:  # ints stay exact: data-block addresses are 64-bit
        out.append(f'N\t{path}\t{int(values)}')


def copy_texture(src, name):
    dst = root / 'Textures' / (name + '.png')
    shutil.copy2(src, dst)
    meta = dst.with_suffix('.png.meta')
    if not meta.exists():  # the builder sets importer settings; keep Unity's later edits
        # guid from the texture name: stable whatever the theme order, never shared by two textures
        meta.write_text('fileFormatVersion: 2\nguid: ' + hashlib.md5(('BossMerchants/' + name).encode()).hexdigest() + '\n')


def yaml_str(s):
    return json.dumps(s)  # a JSON string is a valid YAML double-quoted scalar (safe for ':', '#', quotes)


def text_block(template, index, name, title, description):
    g = GUID.format(0x1000 + index)
    lo, hi = address(g)
    t = (template.replace('m_Name: MyNewWorkbench1', 'm_Name: ' + name)
         .replace('title: MyNewWorkbench1', 'title: ' + yaml_str(title))
         .replace('description: A workbench crafted from chaos.', 'description: ' + yaml_str(description)))
    t = re.sub(r'(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+', lambda m: f'{m[1]}{lo}{m[2]}{hi}', t, count=1)
    p = root / 'Data/TextDataBlock/Items' / (name + '.asset')
    p.write_text(t)
    p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: ' + g
                                            + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')


def main():
    if (root / 'Textures').exists():
        for f in (root / 'Textures').glob('*.png'):
            f.unlink()  # metas stay: same guid when a texture comes back
    for p in ('Textures', 'Data/TextDataBlock/Items', 'Data/EntityAuthoringDataBlock', 'Data/SpriteAssetSkin', 'Prefabs'):
        (root / p).mkdir(parents=True, exist_ok=True)

    themes = load_themes()
    lines = []
    for th in themes:
        key, info = th['key'], th['sheets']
        lines.append(f'T	{key}	{th["mbase"]}	{th["ibase"]}	{info["kind"]}')
        for t in info['textures']:
            src = ART / 'merchants' / t['output']
            if info['kind'] == 'spriteAsset':
                name = f'bossMerchant_{key}_skin_{t["replacesTexture"]}'
                a = t['spriteAssetAddress']
                lines.append(f'K	{key}	{name}	{t["animationGuid"]}	{t["pivot"][0]!r}	{t["pivot"][1]!r}	{a[0]}	{a[1]}')
            else:
                name = f'bossMerchant_{key}__{t["replacesTexture"]}'
            copy_texture(src, name)
        for part in ('icon', 'smallIcon', 'placed'):
            copy_texture(ART / 'idols' / f'{th["art"]}_{part}.png', f'bossMerchant_{key}Idol_{part}')

    with zipfile.ZipFile(sdk / 'Assets/Examples.zip') as z:
        template = z.read('Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset').decode()
    for i, th in enumerate(themes):
        text_block(template, 2 * i, f'BossMerchants_{th["key"]}Merchant', th['mname'], th['blurb'])
        text_block(template, 2 * i + 1, f'BossMerchants_{th["key"]}Idol', th['iname'], th['idesc'])

    vanilla = json.load(open(HERE / 'vanilla_merchants.json'))
    for prefab, comps in vanilla['prefabs'].items():
        for c in comps:
            lines.append(f'C\t{prefab}\t{c["class"]}')
            if 'values' in c:
                flatten(c['values'], '', lines)
    (sdk / 'BossMerchantsVanilla.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('assets written to', root)


if __name__ == '__main__':
    main()
