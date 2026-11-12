"""Export the vanilla merchant idols and the boss reference art, for drawing the Boss Merchants idols.

    python -I BossMerchants/Unity/extract_idol_reference.py [out_dir]

Writes into build/bossmerchants/idol_reference (ignored, never shipped):
  <id>_<Name>_icon.png / _smallIcon.png   item icons (objectInfo.icon / smallIcon of the entity prefab)
  idol_<Name>_placed.png                 sprite on the placed object's SR renderer (graphical prefab)
  idol_<Name>_shadow.png                 sprite used for its drop shadow
  summon_idols_<n>.png                   the summon_idols sheet sprites (extra reference)
  reference.json                         sprite sizes, pivots (normalised and px), pixelsPerUnit, SR offsets
The art in Art/idols is our own; nothing here is copied into it.
"""
import glob, json, os, sys
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'build', 'bossmerchants', 'idol_reference')

IDOLS = {3930: 'MysteriousIdol', 3931: 'SlimeOil', 3932: 'PileOfChum', 3937: 'VoidMerchantIdol'}
REFS = {
    5125: 'MorphaTrophy', 1632: 'SlipperySlime', 8405: 'MorphaBag',
    5127: 'OmorothTrophy', 8354: 'OmorothBeak', 5904: 'TempleOmorothUrn', 5139: 'OracleCardOmoroth',
    5160: 'IgneousTrophy', 1633: 'LavaSlime', 5165: 'LavaSlimeTrophy',
    5085: 'IvyTrophy', 8869: 'IvyRing',
    5161: 'RaAkarTrophy', 5903: 'TempleRaAkarUrn', 5140: 'OracleCardRaAkar',
}
EXTRA_SPRITES = ('summon_idols_0', 'summon_idols_1', 'summon_idols_2')


def sprite_meta(s):
    r = s.m_Rect
    w, h = round(r.width), round(r.height)
    px, py = s.m_Pivot.x, s.m_Pivot.y
    return {'name': s.m_Name, 'w': w, 'h': h, 'pivot': [px, py], 'pivot_px': [px * w, py * h],
            'ppu': s.m_PixelsToUnits}


def main():
    os.makedirs(OUT, exist_ok=True)
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    by_path = {o.path_id: o for o in env.objects}
    wanted = {**IDOLS, **REFS}
    report = {'icons': {}, 'placed': {}, 'extra': {}}

    def save_sprite(pid, fname):
        o = by_path.get(pid)
        if o is None or pid == 0:
            return None
        s = o.read()
        s.image.save(os.path.join(OUT, fname))
        meta = sprite_meta(s)
        meta['file'] = fname
        return meta

    graphic_gos = {}
    for o in env.objects:
        if o.type.name == 'GameObject':
            try:
                n = o.peek_name()
            except Exception:
                continue
            if n in IDOLS.values():
                graphic_gos.setdefault(n, []).append(o.path_id)
            continue
        if o.type.name == 'Sprite':
            try:
                n = o.peek_name()
            except Exception:
                continue
            if n in EXTRA_SPRITES:
                report['extra'][n] = save_sprite(o.path_id, n + '.png')
            continue
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            t = o.read_typetree()
        except Exception:
            continue
        info = t.get('objectInfo')
        if not isinstance(info, dict) or info.get('variation') != 0 or info.get('objectID') not in wanted:
            continue
        oid = info['objectID']
        name = wanted[oid]
        report['icons'][str(oid)] = {
            'name': name,
            'icon': save_sprite(info['icon']['m_PathID'], f'{oid}_{name}_icon.png'),
            'smallIcon': save_sprite(info['smallIcon']['m_PathID'], f'{oid}_{name}_smallIcon.png'),
            'iconOffset': info.get('iconOffset'), 'prefabTileSize': info.get('prefabTileSize'),
        }

    # Placed graphics: graphical prefab GameObject (named like the entity minus "Entity") -> SR + Shadow renderers.
    def walk(go_pid, path, acc):
        go = by_path[go_pid].read_typetree()
        here = path + '/' + go['m_Name']
        tr = None
        for c in go['m_Component']:
            co = by_path[c['component']['m_PathID']]
            if co.type.name == 'Transform':
                tr = co.read_typetree()
                acc.append((here, 'pos', tr['m_LocalPosition']))
            elif co.type.name == 'SpriteRenderer':
                acc.append((here, 'sprite', co.read_typetree()['m_Sprite']['m_PathID']))
        for ch in tr['m_Children']:
            walk(by_path[ch['m_PathID']].read_typetree()['m_GameObject']['m_PathID'], here, acc)

    for name, pids in graphic_gos.items():
        for pid in pids:
            acc = []
            walk(pid, '', acc)
            sprites = [(p, v) for p, k, v in acc if k == 'sprite']
            if not sprites:
                continue
            entry = {'offsets': {p: v for p, k, v in acc if k == 'pos' and ('SR' in p.split('/')[-1] or 'Shadow' in p)}}
            for p, spid in sprites:
                kind = 'shadow' if 'Shadow' in p else 'placed'
                entry[kind] = save_sprite(spid, f'idol_{name}_{kind}.png')
            report['placed'][name] = entry

    json.dump(report, open(os.path.join(OUT, 'reference.json'), 'w'), indent=1)
    for oid, e in report['icons'].items():
        print(oid, e['name'], 'icon', e['icon'] and (e['icon']['w'], e['icon']['h'], e['icon']['pivot_px']),
              'small', e['smallIcon'] and (e['smallIcon']['w'], e['smallIcon']['h']))
    for n, e in report['placed'].items():
        print('placed', n, e['placed']['name'], e['placed']['w'], e['placed']['h'], e['placed']['pivot_px'],
              'ppu', e['placed']['ppu'])
    print('wrote', OUT)


if __name__ == '__main__':
    main()
