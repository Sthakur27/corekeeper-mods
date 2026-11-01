"""Check the built Solarite Drill bundle(s) (requires UnityPy).

    python -I SolariteDrill/Unity/verify_bundle.py [bundle ...]

Defaults to SolariteDrill/Bundles/SolariteDrill_Windows.assetbundle. Fails when:
- a drill prefab is not named SolariteDrill_*, or any networked prefab (GhostAuthoringComponent) in the bundle
  has the same name as a vanilla prefab: NetCode derives the ghost type from the prefab name, so it would share
  the vanilla ghost type and keep GhostCollectionSystem running every frame (Runic Storage 0.7.37 lesson);
- two ghost prefabs share a prefabId;
- the four direction prefabs are not variations 0..3 of SolariteDrill_Drill with 3x Scarlet mining damage;
- either skin is missing or does not target the vanilla drill SpriteAssets.
"""
import glob, os, sys
import UnityPy

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
EXPECT = {'SolariteDrill_ForwardEntity': (0, (0, 1)), 'SolariteDrill_RightEntity': (1, (1, 0)),
          'SolariteDrill_BackwardEntity': (2, (0, -1)), 'SolariteDrill_LeftEntity': (3, (-1, 0))}
TARGETS = {'SolariteDrill_DrillSkin': (6432503404751252551, 6649026345659046154),
           'SolariteDrill_BaseSkin': (-6366884559295650734, 2711081489943583545)}


def main():
    paths = sys.argv[1:] or [os.path.join(os.path.dirname(HERE), 'Bundles', 'SolariteDrill_Windows.assetbundle')]
    for path in paths:
        check(path)


def check(path):
    env = UnityPy.load(path, *glob.glob(GAME + '/*monoscripts*'))
    prefabs, skins, ghosts = {}, {}, []
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
        elif o.type.name == 'MonoBehaviour':
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if t.get('m_Name') in TARGETS:
                skins[t['m_Name']] = t
    errors = []
    # The Overhaul's combined bundle also holds other mods' prefabs: for those, only a name shared with a
    # vanilla prefab is a problem.
    unprefixed = [n for n, _ in ghosts if n in EXPECT and not n.startswith('SolariteDrill_')]
    if unprefixed:
        errors.append('unprefixed drill prefabs: ' + str(unprefixed))
    clashes = sorted({n for n, _ in ghosts} & vanilla_names())
    if clashes:
        errors.append('networked prefabs named like vanilla prefabs (shared ghost type): ' + str(clashes))
    ids = [i for _, i in ghosts]
    if len(ids) != len(set(ids)):
        errors.append('duplicate ghost prefabIds')
    for name, (variation, direction) in EXPECT.items():
        c = prefabs.get(name)
        if c is None:
            errors.append('missing ' + name); continue
        obj = c['ObjectAuthoring']
        if obj['objectName'] != 'SolariteDrill_Drill' or obj['variation'] != variation:
            errors.append(f"{name}: objectName/variation {obj['objectName']}/{obj['variation']}")
        miner = c['AutomatedMinerAuthoring']
        if miner['damage'] != 180 or [(p['x'], p['y']) for p in miner['damagePositions']] != [direction]:
            errors.append(f'{name}: miner {miner}')
        d = c['DirectionBasedOnVariationAuthoring']['direction']
        if (d['x'], d['y']) != direction:
            errors.append(f'{name}: direction {d}')
        if not c['GhostAuthoringComponent']['prefabId']:
            errors.append(f'{name}: empty prefabId')
    for name, target in TARGETS.items():
        s = skins.get(name)
        a = s and s['m_targetAssetRef']['m_address']
        if not s or (a['m_low'], a['m_high']) != target:
            errors.append(f'skin {name}: missing or wrong target')
    print(os.path.basename(path), 'ghost prefabs:', len(ghosts), 'drill prefabs:', sum(n in prefabs for n in EXPECT), 'skins:', len(skins))
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
