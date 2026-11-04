"""Dump the vanilla prefabs the Wormhole Potion copies (waypoint map marker, Recall Idol) to JSON for the Unity builder.

    python -I WormholePotion/Unity/dump_vanilla.py

Writes Unity/vanilla.json: per prefab, the ordered MonoBehaviour components with their serialized values
(typetree). Object references become {"ref": "<component class on the same prefab>"} when they point at the
prefab itself, else {"ref": null}. Re-run after a game update and diff the JSON.
"""
import glob, json, os
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
PREFABS = {'Marker': 'WayPointMapMarker', 'Potion': 'RecallIdol'}


def main():
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    gos = {}
    for o in env.objects:
        if o.type.name == 'GameObject':
            d = o.read()
            if d.m_Name in PREFABS.values() and d.m_Name not in gos:
                gos[d.m_Name] = (o.path_id, d)
    out = {'prefabs': {}}
    for key, name in PREFABS.items():
        gid, go = gos[name]
        own = {gid: '#GameObject'}
        comps = []
        for c in go.m_Component:
            p = c.component
            if p.type.name != 'MonoBehaviour':
                comps.append({'class': '#' + p.type.name})
                continue
            cls = p.read().m_Script.read().m_ClassName
            own[p.path_id] = cls
            comps.append({'class': cls, 'values': p.read_typetree()})

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
        out['prefabs'][key] = comps
    path = os.path.join(HERE, 'vanilla.json')
    json.dump(out, open(path, 'w'), indent=1)
    print('wrote', path, {k: [c['class'] for c in v] for k, v in out['prefabs'].items()})


if __name__ == '__main__':
    main()
