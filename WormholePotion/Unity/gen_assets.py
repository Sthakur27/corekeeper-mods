"""Write the Potion of Return source assets into the Mod SDK project (Assets/WormholePotion).

    python -I WormholePotion/Unity/gen_assets.py [sdk_path]

- Art/WormholePotion_icon.png -> Assets/WormholePotion/Textures (fixed guid)
- Items TextDataBlocks for the potion and the return marker, from the SDK example template
- vanilla.json (dump_vanilla.py) flattened to <sdk>/WormholePotionVanilla.txt for the builder;
  it sits outside Assets/ so it is never bundled.
"""
from pathlib import Path
import json, re, shutil, struct, sys, zipfile

HERE = Path(__file__).resolve().parent
sdk = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r'C:/Users/Sid/CoreKeeperModSDK')
root = sdk / 'Assets/WormholePotion'
GUID = 'b7e3c2a19d4f4e8a8c5b6a0f1e2d{:04x}'  # + running index; fixed forever
TEXTS = [
    ('WormholePotion_Potion', 'Potion of Return', 0x101,
     'Returns you to the Core after a short cast and marks where you drank it on the map. Teleport back there once from any active waypoint.'),
    ('WormholePotion_Marker', 'Return Point', 0x102,
     'Where a Potion of Return was drunk. Teleport here once from any active waypoint.'),
]


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
    for p in ('Textures', 'Data/TextDataBlock/Items', 'Data/EntityAuthoringDataBlock', 'Prefabs'):
        (root / p).mkdir(parents=True, exist_ok=True)
    shutil.copy2(HERE / 'Art/WormholePotion_icon.png', root / 'Textures/WormholePotion_icon.png')
    meta = root / 'Textures/WormholePotion_icon.png.meta'
    if not meta.exists():  # the builder sets importer settings; keep Unity's later edits
        meta.write_text('fileFormatVersion: 2\nguid: ' + GUID.format(1) + '\n')

    with zipfile.ZipFile(sdk / 'Assets/Examples.zip') as z:
        template = z.read('Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset').decode()
    for name, title, index, description in TEXTS:
        g = GUID.format(index)
        lo, hi = address(g)
        t = (template.replace('m_Name: MyNewWorkbench1', 'm_Name: ' + name)
             .replace('title: MyNewWorkbench1', 'title: ' + title)
             .replace('description: A workbench crafted from chaos.', 'description: ' + description))
        t = re.sub(r'(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+', lambda m: f'{m[1]}{lo}{m[2]}{hi}', t, count=1)
        p = root / f'Data/TextDataBlock/Items/{name}.asset'
        p.write_text(t)
        p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: ' + g
                                                + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')

    vanilla = json.load(open(HERE / 'vanilla.json'))
    lines = []
    for key, comps in vanilla['prefabs'].items():
        seen = {}
        for c in comps:
            cls = c['class']
            n = seen[cls] = seen.get(cls, -1) + 1
            lines.append(f'C\t{key}\t{cls}\t{n}')
            if 'values' in c:
                flatten(c['values'], '', lines)
    (sdk / 'WormholePotionVanilla.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('assets written to', root)


if __name__ == '__main__':
    main()
