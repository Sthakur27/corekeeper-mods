"""Export the vanilla Upgrade Forge skin (SpriteAssetSkin "upgradeForge") for the Enchanting Table skin.

    python -I Enchanting/Unity/extract_forge_skin.py

Writes Unity/Art/forge/<texture>.png for every texture the skin uses and Unity/forge_skin.json with the skin's
layout (target asset, static data, replacements with guids, pivots, which texture fills which slot).
make_art.py recolors these into Art/EnchantingTable_*.png; the builder rebuilds the skin from forge_skin.json.
"""
import glob, json, os
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'Art', 'forge')
SKIN = 'upgradeForge'


def main():
    os.makedirs(OUT, exist_ok=True)
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    by_path = {o.path_id: o for o in env.objects}

    def texture(pptr):
        if not pptr or not pptr.get('m_PathID'):
            return None
        o = by_path.get(pptr['m_PathID'])
        if o is None:
            return None
        tex = o.read()
        path = os.path.join(OUT, tex.m_Name + '.png')
        if not os.path.exists(path):
            tex.image.save(path)
        return tex.m_Name

    def sprite_data(d):
        return {'texture': texture(d.get('texture')), 'emissive': texture(d.get('emissiveTexture')),
                'normal': texture(d.get('normalTexture')), 'pivot': d['pivot'],
                'inheritPivot': d.get('inheritPivot'), 'positionalData': len(d.get('positionalData', []))}

    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            t = o.read_typetree()
        except Exception:
            continue
        if t.get('m_Name') != SKIN or 'm_targetAssetRef' not in t:
            continue
        rep = t['m_staticReplacementData']
        out = {
            'address': t['m_address'],
            'target': t['m_targetAssetRef'],
            'static': {'guid': rep.get('m_guid', ''), 'sprite': sprite_data(rep['m_spriteData']),
                       'variants': [sprite_data(v) for v in rep['m_variants']]},
            'replacements': [{'guid': r['m_guid'], 'sprite': sprite_data(r['m_spriteData']),
                              'variants': [sprite_data(v) for v in r['m_variants']]} for r in t['m_replacementData']],
            'other_keys': [k for k in t if k not in ('m_staticReplacementData', 'm_replacementData')],
        }
        json.dump(out, open(os.path.join(HERE, 'forge_skin.json'), 'w'), indent=1)
        print('wrote forge_skin.json;', len(out['replacements']), 'replacements; textures in', OUT)
        print(sorted(os.listdir(OUT)))
        return
    raise SystemExit('skin not found')


if __name__ == '__main__':
    main()
