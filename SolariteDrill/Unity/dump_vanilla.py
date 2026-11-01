"""Dump the vanilla Scarlet Drill entity prefabs (one per direction) to JSON for the Unity builder.

    python -I SolariteDrill/Unity/dump_vanilla.py

Writes Unity/vanilla_drill.json: per direction, the ordered MonoBehaviour components with their serialized
values (typetree). Object references are replaced by {"ref": "<component class on the same prefab>"} when they
point at the prefab itself, else {"ref": null}. The builder copies these values onto our own prefabs and
overrides the Solarite-specific fields. Re-run after a game update and diff the JSON.
"""
import glob, json, os
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
PREFABS = {'Forward': 'DrillForwardEntity', 'Backward': 'DrillBackwardEntity', 'Left': 'DrillLeftEntity', 'Right': 'DrillRightEntity'}
BLOCKS = ('Drill',)  # GraphicalObjectDataBlock


def main():
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    gos = {}
    blocks = {}
    for o in env.objects:
        if o.type.name == 'GameObject':
            d = o.read()
            if d.m_Name in PREFABS.values():
                gos[d.m_Name] = (o.path_id, d)
        elif o.type.name == 'MonoBehaviour':
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if t.get('m_Name') in BLOCKS and o.read().m_Script.read().m_ClassName == 'GraphicalObjectDataBlock':
                blocks['graphics:' + t['m_Name']] = t['m_address']
    out = {'blocks': blocks, 'prefabs': {}}
    for direction, name in PREFABS.items():
        gid, go = gos[name]
        own = {}
        comps = []
        for c in go.m_Component:
            p = c.component
            if p.type.name != 'MonoBehaviour':
                comps.append({'class': '#' + p.type.name})
                continue
            cls = p.read().m_Script.read().m_ClassName
            own[p.path_id] = cls
            comps.append({'class': cls, 'pid': p.path_id, 'values': p.read_typetree()})
        own[gid] = '#GameObject'

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
                del c['pid']
        out['prefabs'][direction] = comps
    path = os.path.join(HERE, 'vanilla_drill.json')
    json.dump(out, open(path, 'w'), indent=1)
    print('wrote', path, {k: len(v) for k, v in out['prefabs'].items()}, blocks)


if __name__ == '__main__':
    main()
