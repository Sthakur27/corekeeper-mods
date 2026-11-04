"""Write the Drill Unit source assets into the Mod SDK project (Assets/DrillUnit).

    python DrillUnit/Unity/gen_assets.py [sdk_path]

- Art/*.png -> Assets/DrillUnit/Textures (fixed guids so rebuilds keep the same addresses)
- Items TextDataBlock "DrillUnit_Unit" from the SDK example template
- vanilla_boat.json (dump_vanilla.py) flattened to <sdk>/DrillUnitVanilla.txt for the builder;
  it sits outside Assets/ so it is never bundled.
"""
from pathlib import Path
import json, re, shutil, struct, sys, zipfile

HERE = Path(__file__).resolve().parent
sdk = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r'C:/Users/Sid/CoreKeeperModSDK')
root = sdk / 'Assets/DrillUnit'
GUID = 'd1a1e7b0c3f84e2a9b6dd0117a17{:04x}'  # + running index; fixed forever

TEXTURES = ['DrillUnit_icon', 'DrillUnit_smallIcon', 'DrillUnit_front', 'DrillUnit_back']
TITLE = 'Drill Unit'
DESCRIPTION = 'A hovering mining craft from the Rift. Crosses water, lava and land. Mine with your pickaxe or drill from the seat; weapons do not work while riding.'


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
    for i, name in enumerate(TEXTURES):
        shutil.copy2(HERE / 'Art' / (name + '.png'), root / 'Textures' / (name + '.png'))
        meta = root / 'Textures' / (name + '.png.meta')
        if not meta.exists():  # the builder sets importer settings; keep Unity's later edits
            meta.write_text('fileFormatVersion: 2\nguid: ' + GUID.format(i + 1) + '\n')

    g = GUID.format(0x100)
    lo, hi = address(g)
    with zipfile.ZipFile(sdk / 'Assets/Examples.zip') as z:
        t = z.read('Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset').decode()
    t = (t.replace('m_Name: MyNewWorkbench1', 'm_Name: DrillUnit_Unit')
          .replace('title: MyNewWorkbench1', 'title: ' + TITLE)
          .replace('description: A workbench crafted from chaos.', 'description: ' + DESCRIPTION))
    t = re.sub(r'(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+', lambda m: f'{m[1]}{lo}{m[2]}{hi}', t, count=1)
    p = root / 'Data/TextDataBlock/Items/DrillUnit_Unit.asset'
    p.write_text(t)
    p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: ' + g
                                            + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')

    vanilla = json.load(open(HERE / 'vanilla_boat.json'))
    lines = []
    for b, a in vanilla['blocks'].items():
        lines.append(f"B\t{b}\t{a['m_low']}\t{a['m_high']}")
    for direction, comps in vanilla['prefabs'].items():
        seen = {}
        for c in comps:
            cls = c['class']
            n = seen[cls] = seen.get(cls, -1) + 1
            lines.append(f'C\t{direction}\t{cls}\t{n}')
            if 'values' in c:
                flatten(c['values'], '', lines)
    (sdk / 'DrillUnitVanilla.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('assets written to', root)


if __name__ == '__main__':
    main()
