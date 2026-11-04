"""Check the built Drill Unit bundle(s) (requires UnityPy).

    python -I DrillUnit/Unity/verify_bundle.py [bundle ...]

Defaults to DrillUnit/Bundles/DrillUnit_Windows.assetbundle; any other bundle (the Overhaul's combined
SidsStorageAssets) is checked for the Drill Unit prefab only. Fails when the unit prefab is missing or not
named DrillUnit_* (a ghost prefab named like a vanilla one shares its ghost type), when a ghost prefab
has no prefabId, or when the ObjectAuthoring/BoatAuthoring/recipe values are not what the builder sets.
"""
import glob, os, sys
import UnityPy

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
RECIPE = [('PandoriumBar', 300), ('SolariteBar', 100)]


def check(path):
    env = UnityPy.load(path, *glob.glob(GAME + '/*monoscripts*'))
    combined = not os.path.basename(path).startswith('DrillUnit_')
    units = {}
    textures = {o.read().m_Name for o in env.objects if o.type.name == 'Texture2D'}
    for o in env.objects:
        if o.type.name != 'GameObject':
            continue
        go = o.read()
        comps = {}
        for c in go.m_Component:
            p = c.component
            if p.type.name == 'MonoBehaviour':
                comps[p.read().m_Script.read().m_ClassName] = p.read_typetree()
        if 'GhostAuthoringComponent' in comps:
            if combined and not go.m_Name.startswith('DrillUnit_'):
                continue  # the Overhaul's combined bundle also holds other mods' prefabs
            if not go.m_Name.startswith('DrillUnit_'):
                sys.exit(f'{path}: ghost prefab {go.m_Name} is not named DrillUnit_*')
            if not comps['GhostAuthoringComponent'].get('prefabId'):
                sys.exit(f'{path}: {go.m_Name} has no ghost prefabId')
            units[go.m_Name] = comps
    unit = units.get('DrillUnit_UnitEntity')
    if unit is None:
        sys.exit(f'{path}: DrillUnit_UnitEntity missing (found {sorted(units)})')
    obj = unit['ObjectAuthoring']
    if obj['objectName'] != 'DrillUnit_Unit':
        sys.exit(f'{path}: objectName {obj["objectName"]}')
    speed = unit['BoatAuthoring']['speedMultiplier']
    place = unit['PlaceableObjectAuthoring']
    if not place['canBePlacedOnAnyWalkableTile']:
        sys.exit(f'{path}: not placeable on land')
    recipe = [(r['objectName'], r['amount']) for r in unit['InventoryItemAuthoring']['requiredObjectsToCraft']]
    if recipe != RECIPE:
        sys.exit(f'{path}: recipe {recipe}')
    for t in ('DrillUnit_front', 'DrillUnit_back'):
        if t not in textures:
            sys.exit(f'{path}: skin texture {t} missing')
    print(f'{os.path.basename(path)}: OK (speed {speed:.2f}, recipe {recipe}, ghost id {unit["GhostAuthoringComponent"]["prefabId"]})')


def main():
    paths = sys.argv[1:] or [os.path.join(os.path.dirname(HERE), 'Bundles', 'DrillUnit_Windows.assetbundle')]
    for path in paths:
        check(path)


if __name__ == '__main__':
    main()
