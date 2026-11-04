"""Check the built Wormhole Potion bundle(s) (requires UnityPy).

    python -I WormholePotion/Unity/verify_bundle.py [bundle ...]

Defaults to WormholePotion/Bundles/WormholePotion_Windows.assetbundle. Fails when:
- any networked prefab (GhostAuthoringComponent) is named like a vanilla prefab (shared ghost type,
  Runic Storage 0.7.37 lesson) or two ghost prefabs share a prefabId;
- the potion is not a 10 Gold Bar cast item, or the return marker is missing, is not a Waypoint-type
  map marker, or would not be saved with the world.
"""
import glob, os, sys
import UnityPy

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'


def main():
    paths = sys.argv[1:] or [os.path.join(os.path.dirname(HERE), 'Bundles', 'WormholePotion_Windows.assetbundle')]
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
    ids = [i for _, i in ghosts]
    if len(ids) != len(set(ids)) or not all(ids):
        errors.append('duplicate or empty ghost prefabIds')
    potion = prefabs.get('WormholePotion_Potion')
    if potion is None:
        errors.append('missing WormholePotion_Potion')
    else:
        need = [(r['objectName'], r['amount']) for r in potion['InventoryItemAuthoring']['requiredObjectsToCraft']]
        if need != [('GoldBar', 10)] or 'CastItemAuthoring' not in potion:
            errors.append(f'potion recipe/cast wrong: {need}')
    marker = prefabs.get('WormholePotion_MarkerEntity')
    if marker is None:
        errors.append('missing WormholePotion_MarkerEntity')
    else:
        if marker['ObjectAuthoring']['objectName'] != 'WormholePotion_Marker':
            errors.append('marker objectName wrong')
        if marker.get('MapMarkerAuthoring', {}).get('mapMarkerType') != 7:
            errors.append('marker is not a Waypoint-type map marker')
        if 'DontSerializeAuthoring' in marker:
            errors.append('marker would not be saved (DontSerializeAuthoring)')
    print(os.path.basename(path), 'ghost prefabs:', len(ghosts), 'prefabs:', sorted(n for n in prefabs if n.startswith('WormholePotion_')))
    if errors:
        sys.exit('FAIL\n  ' + '\n  '.join(errors))
    print('OK')


_vanilla = None


def vanilla_names():
    global _vanilla
    if _vanilla is None:
        env = UnityPy.load(GAME + '/defaultlocalgroup_assets_all.bundle')
        _vanilla = {o.peek_name() for o in env.objects if o.type.name == 'GameObject'}
    return _vanilla


if __name__ == '__main__':
    main()
