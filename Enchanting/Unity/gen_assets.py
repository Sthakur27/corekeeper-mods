"""Write the Enchanting source assets into the Mod SDK project (Assets/Enchanting).

    python Enchanting/Unity/gen_assets.py [sdk_path]

- Art/EnchantingTable_*.png -> Assets/Enchanting/Textures (fixed guids so rebuilds keep the same addresses)
- Items TextDataBlock "Enchanting_Table" from the SDK example template
- vanilla_forge.json (dump_vanilla.py) flattened to <sdk>/EnchantingVanilla.txt for the builder;
  it sits outside Assets/ so it is never bundled.
"""
from pathlib import Path
import json, re, shutil, struct, sys, zipfile

HERE = Path(__file__).resolve().parent
sdk = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r'C:/Users/Sid/CoreKeeperModSDK')
root = sdk / 'Assets/Enchanting'
GUID = '3e9c0b7a51d24f8e8a6e3c1d0b9a{:04x}'  # + running index; fixed forever

TEXTURES = ['EnchantingTable_icon', 'EnchantingTable_smallIcon',
            'EnchantingTable_front', 'EnchantingTable_side', 'EnchantingTable_side2', 'EnchantingTable_up']  # append only
TITLE = 'Enchanting Table'
DESCRIPTION = 'Place a weapon, tool or armor piece inside and pay gold and ancient coins to bind a random enchantment to it.'


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


def main():
    for p in ('Textures', 'Data/TextDataBlock/Items', 'Data/EntityAuthoringDataBlock', 'Data/SpriteAssetSkin', 'Prefabs'):
        (root / p).mkdir(parents=True, exist_ok=True)
    for i, name in enumerate(TEXTURES):
        shutil.copy2(HERE / 'Art' / (name + '.png'), root / 'Textures' / (name + '.png'))
        meta = root / 'Textures' / (name + '.png.meta')
        if not meta.exists():  # the builder sets importer settings; keep Unity's later edits
            meta.write_text('fileFormatVersion: 2\nguid: ' + GUID.format(i + 1) + '\n')

    g = GUID.format(0x100)
    lo, hi = address(g)
    with zipfile.ZipFile(sdk / 'Assets/Examples.zip') as z:
        t = z.read('Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset').decode()
    t = (t.replace('m_Name: MyNewWorkbench1', 'm_Name: Enchanting_Table')
          .replace('title: MyNewWorkbench1', 'title: ' + TITLE)
          .replace('description: A workbench crafted from chaos.', 'description: ' + DESCRIPTION))
    t = re.sub(r'(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+', lambda m: f'{m[1]}{lo}{m[2]}{hi}', t, count=1)
    p = root / 'Data/TextDataBlock/Items/Enchanting_Table.asset'
    p.write_text(t)
    p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: ' + g
                                            + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')

    vanilla = json.load(open(HERE / 'vanilla_forge.json'))
    lines = []
    seen = {}
    for c in vanilla['prefab']:
        cls = c['class']
        n = seen[cls] = seen.get(cls, -1) + 1
        lines.append(f'C\t{cls}\t{n}')
        if 'values' in c:
            flatten(c['values'], '', lines)
    (sdk / 'EnchantingVanilla.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('assets written to', root)


if __name__ == '__main__':
    main()
