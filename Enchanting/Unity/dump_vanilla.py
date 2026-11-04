"""Dump the vanilla Upgrade Forge entity prefab to JSON for the Unity builder, plus its icons.

    python -I Enchanting/Unity/dump_vanilla.py

Writes Unity/vanilla_forge.json (the ordered MonoBehaviour components of UpgradeForgeEntity with their serialized
values; object references become {"ref": "<component class on the same prefab>"} or {"ref": null}) and
Unity/Art/forge_icon.png + forge_smallIcon.png (source for make_art.py). Re-run after a game update and diff.
"""
import glob, json, os
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
PREFAB = 'UpgradeForgeEntity'


def main():
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    go = gid = None
    for o in env.objects:
        if o.type.name == 'GameObject':
            d = o.read()
            if d.m_Name == PREFAB:
                go, gid = d, o.path_id
                break
    if go is None:
        raise SystemExit(PREFAB + ' not found')

    own = {gid: '#GameObject'}
    comps = []
    for c in go.m_Component:
        p = c.component
        if p.type.name != 'MonoBehaviour':
            comps.append({'class': '#' + p.type.name})
            continue
        mb = p.read()
        cls = mb.m_Script.read().m_ClassName
        own[p.path_id] = cls
        values = p.read_typetree()
        if cls == 'EntityMonoBehaviourData':
            for key in ('icon', 'smallIcon'):
                export_sprite(mb.objectInfo, key)
        comps.append({'class': cls, 'values': values})

    def fix(x):
        if isinstance(x, dict):
            if set(x) == {'m_FileID', 'm_PathID'}:
                return {'ref': own.get(x['m_PathID']) if x['m_PathID'] else None}
            return {k: fix(v) for k, v in x.items() if k not in ('m_GameObject', 'm_Script', 'm_Enabled', 'm_Name')}
        if isinstance(x, list):
            return [fix(v) for v in x]
        return x
    for c in comps:
        if 'values' in c:
            c['values'] = fix(c['values'])
    path = os.path.join(HERE, 'vanilla_forge.json')
    json.dump({'prefab': comps}, open(path, 'w'), indent=1)
    print('wrote', path, len(comps), 'components')


def export_sprite(object_info, key):
    ref = getattr(object_info, key, None)
    if ref is None:
        return
    try:
        sprite = ref.read()
    except Exception as e:
        print('could not read', key, e)
        return
    os.makedirs(os.path.join(HERE, 'Art'), exist_ok=True)
    out = os.path.join(HERE, 'Art', 'forge_' + key + '.png')
    sprite.image.save(out)
    print('exported', out, sprite.image.size)


if __name__ == '__main__':
    main()
