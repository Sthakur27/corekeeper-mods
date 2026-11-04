"""Check the built Enchanting bundle(s) (requires UnityPy).

    python -I Enchanting/Unity/verify_bundle.py [bundle ...]

Defaults to Enchanting/Bundles/Enchanting_Windows.assetbundle. Fails when the table prefab is missing, not named
Enchanting_*, has the wrong objectName/recipe/graphics, has an empty ghost prefabId, or any networked prefab shares
a name with a vanilla prefab (shared ghost type).
"""
import glob, os, sys
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
PREFAB = 'Enchanting_TableEntity'
FORGE_GRAPHICS = (-5420990219213726073, -5864256534343259814)  # vanilla Upgrade Forge graphicalRef


def main():
    paths = sys.argv[1:] or [os.path.join(os.path.dirname(HERE), 'Bundles', 'Enchanting_Windows.assetbundle')]
    for path in paths:
        check(path)


def check(path):
    env = UnityPy.load(path, *glob.glob(GAME + '/*monoscripts*'))
    prefabs, ghosts = {}, []
    for o in env.objects:
        if o.type.name != 'GameObject':
            continue
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
    errors = []
    clashes = sorted({n for n, _ in ghosts} & vanilla_names())
    if clashes:
        errors.append('networked prefabs named like vanilla prefabs (shared ghost type): ' + str(clashes))
    c = prefabs.get(PREFAB)
    if c is None:
        errors.append('missing ' + PREFAB)
    else:
        obj = c['ObjectAuthoring']
        if obj['objectName'] != 'Enchanting_Table':
            errors.append('objectName ' + obj['objectName'])
        g = obj['graphicalRef']['m_address']
        if (g['m_low'], g['m_high']) != FORGE_GRAPHICS:
            errors.append(f'graphicalRef {g}')
        recipe = [(r['objectName'], r['amount']) for r in c['InventoryItemAuthoring']['requiredObjectsToCraft']]
        if not recipe:
            errors.append('no recipe')
        if not c['GhostAuthoringComponent']['prefabId']:
            errors.append('empty prefabId')
        for need in ('CraftingAuthoring', 'LocalizationAuthoring', 'PlaceableObjectAuthoring'):
            if need not in c:
                errors.append('missing component ' + need)
        print(os.path.basename(path), 'ghost prefabs:', len(ghosts), 'recipe:', recipe)
    if errors:
        sys.exit('FAIL\n  ' + '\n  '.join(errors))
    print('OK')


def vanilla_names():
    env = UnityPy.load(GAME + '/defaultlocalgroup_assets_all.bundle')
    return {o.peek_name() for o in env.objects if o.type.name == 'GameObject'}


if __name__ == '__main__':
    main()
