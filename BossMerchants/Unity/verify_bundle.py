"""Check the built Boss Merchants bundle(s) (requires UnityPy).

    python -I BossMerchants/Unity/verify_bundle.py [--combined] [bundle ...]

Defaults to BossMerchants/Bundles/BossMerchants_Windows.assetbundle. --combined checks the Overhaul's
SidsStorageAssets bundle: other mods' ghost prefabs are allowed there, but every ghost name and prefabId must
still be unique. Fails when:
- any networked prefab (GhostAuthoringComponent) is not named BossMerchants_*Entity or shares a prefabId:
  NetCode derives the ghost type from the prefab name (AGENTS.md, Runic Storage lesson);
- one of the sixteen merchant/idol prefabs is missing, or a merchant still carries vanilla MerchantAuthoring items;
- a theme's textures (every sheet in Art/final/merchants/<key>/sheets.json, the three idol textures) or the
  SpriteAssetSkin of a spriteAsset theme are missing.
"""
import glob, json, os, sys
import UnityPy

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
# catalog Key -> art key (same table as gen_assets.py KEYS)
KEYS = {'Blocks': 'blocks', 'Decor': 'decor', 'Explosives': 'explosives', 'Potions': 'potions',
        'Eggs': 'eggs', 'Hydras': 'hydras', 'FarmAnimals': 'farmanimals', 'Seeds': 'seeds'}


def sheets(art):
    return json.load(open(os.path.join(HERE, 'Art', 'final', 'merchants', art, 'sheets.json'), encoding='utf-8'))


def main():
    args = sys.argv[1:]
    combined = '--combined' in args
    paths = [a for a in args if a != '--combined'] or [os.path.join(os.path.dirname(HERE), 'Bundles', 'BossMerchants_Windows.assetbundle')]
    bad = []
    for path in paths:
        bad += check(path, combined)
    if bad:
        print('\n'.join('FAIL: ' + b for b in bad))
        sys.exit(1)
    print('OK')


def check(path, combined=False):
    env = UnityPy.load(path, *glob.glob(GAME + '/*monoscripts*'))
    bad, prefabs, ghosts, textures, skins = [], {}, [], set(), set()
    for o in env.objects:
        if o.type.name == 'GameObject':
            go = o.read()
            comps = {}
            for c in go.m_Component:
                p = c.component
                if p.type.name != 'MonoBehaviour':
                    continue
                try:
                    comps.setdefault(p.read().m_Script.read().m_ClassName, p.read_typetree())
                except Exception:
                    pass
            prefabs[go.m_Name] = comps
            if 'GhostAuthoringComponent' in comps:
                ghosts.append((go.m_Name, comps['GhostAuthoringComponent']['prefabId']))
        elif o.type.name == 'Texture2D':
            textures.add(o.read().m_Name)
        elif o.type.name == 'MonoBehaviour':
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if 'm_targetAssetRef' in t:
                skins.add(t.get('m_Name'))
    names = [name for name, _ in ghosts]
    if len(names) != len(set(names)):
        bad.append(f'{path}: duplicate ghost prefab names {sorted(n for n in set(names) if names.count(n) > 1)}')
    for name, pid in ghosts:
        if combined and not name.startswith('BossMerchants'):
            continue
        if not (name.startswith('BossMerchants_') and name.endswith('Entity')):
            bad.append(f'{path}: ghost prefab {name} is not named BossMerchants_*Entity')
    ids = [pid for _, pid in ghosts]
    if len(ids) != len(set(ids)):
        bad.append(f'{path}: duplicate ghost prefabIds')
    for k in KEYS:
        for kind in ('Merchant', 'Idol'):
            name = f'BossMerchants_{k}{kind}Entity'
            comps = prefabs.get(name)
            if comps is None:
                bad.append(f'{path}: missing {name}')
                continue
            oa = comps.get('ObjectAuthoring', {})
            if oa.get('objectName') != f'BossMerchants_{k}{kind}':
                bad.append(f'{path}: {name} objectName {oa.get("objectName")!r}')
            if kind == 'Merchant' and comps.get('MerchantAuthoring', {}).get('items'):
                bad.append(f'{path}: {name} still lists vanilla merchant items')
            if kind == 'Idol' and 'InventoryItemAuthoring' not in comps:
                bad.append(f'{path}: {name} has no InventoryItemAuthoring')
        info = sheets(KEYS[k])
        want = [f'bossMerchant_{k}Idol_{part}' for part in ('icon', 'smallIcon', 'placed')]
        for tx in info['textures']:
            want.append(f'bossMerchant_{k}_skin_{tx["replacesTexture"]}' if info['kind'] == 'spriteAsset'
                        else f'bossMerchant_{k}__{tx["replacesTexture"]}')
        for w in want:
            if w not in textures:
                bad.append(f'{path}: missing texture {w}')
        if info['kind'] == 'spriteAsset' and f'bossMerchant_{k}_skin' not in skins:
            bad.append(f'{path}: missing SpriteAssetSkin bossMerchant_{k}_skin')
    print(f'{os.path.basename(path)}: {len(ghosts)} ghost prefabs, {len(textures)} textures, skins {sorted(skins)}')
    return bad


if __name__ == '__main__':
    main()
