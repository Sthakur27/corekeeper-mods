"""Export the vanilla drill textures the Solarite Drill skins replace, for art reference only.

    python SolariteDrill/Unity/extract_reference.py [out_dir]

Writes PNGs plus skins.json (which texture fills which slot of the Tin Drill skins, with pivots) into
build/solaritedrill/reference (ignored). Nothing written here is shipped; the art in Art/ is our own.
"""
import glob, json, os, sys
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'build', 'solaritedrill', 'reference')
SKINS = ('drillCrude', 'drillBaseCrude')
ASSETS = ('drill', 'drillBase')
ICONS = (6650, 6656)  # Drill (Scarlet), CrudeDrill (Tin)


def main():
    os.makedirs(OUT, exist_ok=True)
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    by_path = {o.path_id: o for o in env.objects}

    def texture(pptr):
        o = by_path.get(pptr['m_PathID'])
        if o is None or pptr['m_PathID'] == 0:
            return None
        tex = o.read()
        path = os.path.join(OUT, tex.m_Name + '.png')
        if not os.path.exists(path):
            tex.image.save(path)
        return tex.m_Name

    def sprite_data(d):
        return {'texture': texture(d['texture']), 'emissive': texture(d['emissiveTexture']), 'pivot': d['pivot']}

    report = {}
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            t = o.read_typetree()
        except Exception:
            continue
        name = t.get('m_Name')
        if name in SKINS:
            rep = t['m_staticReplacementData']
            report['skin:' + name] = {
                'target': t['m_targetAssetRef'],
                'static': sprite_data(rep['m_spriteData']),
                'static_variants': [sprite_data(v) for v in rep['m_variants']],
                'replacements': [{'guid': r['m_guid'], 'sprite': sprite_data(r['m_spriteData']),
                                  'variants': [sprite_data(v) for v in r['m_variants']]} for r in t['m_replacementData']],
            }
        elif name in ASSETS and 'm_targetAssetRef' not in t:
            # The vanilla (Scarlet) look: dump every texture the SpriteAsset references.
            found = []

            def walk(x):
                if isinstance(x, dict):
                    if 'texture' in x and isinstance(x['texture'], dict) and 'm_PathID' in x['texture']:
                        found.append(sprite_data(x))
                    for v in x.values():
                        walk(v)
                elif isinstance(x, list):
                    for v in x:
                        walk(v)
            walk(t)
            report['asset:' + name] = found
        elif o.type.name == 'MonoBehaviour' and name == '' and False:
            pass
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            t = o.read_typetree()
        except Exception:
            continue
        info = t.get('objectInfo')
        if isinstance(info, dict) and info.get('objectID') in ICONS and info.get('variation') == 0:
            report.setdefault('icons', {})[str(info['objectID'])] = {
                'icon': sprite_name(by_path, info['icon']), 'smallIcon': sprite_name(by_path, info['smallIcon'])}
    json.dump(report, open(os.path.join(OUT, 'skins.json'), 'w'), indent=1)
    print('wrote', OUT)


def sprite_name(by_path, pptr):
    o = by_path.get(pptr['m_PathID'])
    if o is None:
        return None
    s = o.read()
    s.image.save(os.path.join(OUT, 'icon_' + s.m_Name + '.png'))
    return s.m_Name


if __name__ == '__main__':
    main()
