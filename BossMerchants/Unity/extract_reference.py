"""Export the vanilla merchant textures the Boss Merchants skins replace, plus palette-source sprites.

    python -I BossMerchants/Unity/extract_reference.py [out_dir]

Writes into build/bossmerchants/reference (ignored, never shipped):
  merchants/<Base>/<texture>.png   every texture used by the base merchant's graphics prefab
  palette/<texture>.png            boss / enemy / pet / chest sprites the recolours take colours from
  reference.json                   which texture fills which slot, with rects + pivots (see below)

Two kinds of merchant graphics exist in the game:
  * SpriteAsset ("spriteAsset"): VoidMerchant. Its SpriteObject references a SpriteAsset by address; the
    json lists every animation (guid, texture, emissive, pivot, frame count) exactly like the skins need.
  * Legacy SpriteRenderer + Animator ("legacy"): SlimeMerchant, CavelingMerchant, FishingMerchant. Their
    sheets are sliced into Sprite objects animated by AnimationClips; there is no SpriteAsset, so
    SpriteAssetSkins cannot target them. The json lists every Sprite (name, rect, pivot, pixelsPerUnit)
    per sheet so a runtime swap can rebuild identical Sprites from our sheet.
"""
import glob, json, os, sys
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'build', 'bossmerchants', 'reference')

# graphics prefab MonoBehaviour class (= GameObject name) -> ObjectID
MERCHANTS = {'SlimeMerchant': 3902, 'CavelingMerchant': 3900, 'FishingMerchant': 3903, 'VoidMerchant': 3936}

# palette-source textures, by name
PALETTE_TEXTURES = [
    # Morpha / slippery slime
    'boss_slime_blue', 'slimeBlob_slippery_idle', 'bossChestMorpha', 'slippery_sword_slime',
    # Igneous / lava slime
    'boss_slime_lava', 'slimeBlob_lava_idle', 'slimeBlob_lava_idle_emissive', 'bossChestIgneous',
    # Ivy / poison slime
    'boss_slime_poison', 'slimeBlob_poison_idle', 'bossChestIvy', 'poisonSlimeDestructible',
    # Omoroth / sea
    'Octopus_boss_body', 'omorothTentacle_idle', 'bossChestOmoroth', 'bossMural_Omoroth', 'omorothUrn',
    # Ra-Akar / desert
    'Scarab_boss', 'Scarab_boss_blue_eyes', 'bossChestRaAkar', 'bossMural_RaAkar', 'raAkarUrn',
]
# objects whose inventory icons are also palette sources (ObjectID -> label)
ICON_IDS = {3024: 'SlipperySlimeBoss', 1632: 'SlipperySlime', 1237: 'PetSlipperySlimeBlob',
            3028: 'LavaSlimeBoss', 1633: 'LavaSlime', 1243: 'PetLavaSlimeBlob',
            3023: 'PoisonSlimeBoss', 3110: 'OctopusBoss', 5127: 'OmorothTrophy', 8354: 'OmorothBeak',
            3131: 'ScarabBoss', 5161: 'RaAkarTrophy'}


def addr(a):
    return (a['m_address']['m_low'], a['m_address']['m_high'])


def main():
    os.makedirs(os.path.join(OUT, 'palette'), exist_ok=True)
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), GAME + '/defaultlocalgroup_assets_all.bundle')
    by_path = {o.path_id: o for o in env.objects}
    script_name = {o.path_id: o.read().m_ClassName for o in env.objects if o.type.name == 'MonoScript'}

    def cls(o):
        t = o.read_typetree()
        return script_name.get(t['m_Script']['m_PathID']), t

    saved = {}

    def save_tex(pid, folder):
        o = by_path.get(pid) if pid else None
        if o is None:
            return None
        if (folder, pid) not in saved:
            tex = o.read()
            d = os.path.join(OUT, folder)
            os.makedirs(d, exist_ok=True)
            tex.image.save(os.path.join(d, tex.m_Name + '.png'))
            saved[(folder, pid)] = tex.m_Name
        return saved[(folder, pid)]

    # Sprites grouped by texture (for the legacy sheets) and SpriteAssets by address.
    sprites_by_tex, sprite_assets, mbs = {}, {}, []
    for o in env.objects:
        if o.type.name == 'Sprite':
            sprites_by_tex.setdefault(o.read().m_RD.texture.path_id, []).append(o.path_id)
        elif o.type.name == 'MonoBehaviour':
            mbs.append(o)
    for o in mbs:
        try:
            c, t = cls(o)
        except Exception:
            continue
        if c == 'SpriteAsset' and 'm_address' in t:
            sprite_assets[addr(t)] = t

    def sprite_info(pid):
        s = by_path[pid].read()
        r = s.m_Rect
        return {'name': s.m_Name, 'rect': [r.x, r.y, r.width, r.height],
                'pivot': [s.m_Pivot.x, s.m_Pivot.y], 'pixelsPerUnit': s.m_PixelsToUnits}

    def components(go):
        for c in go.m_Components:
            cp = c.component if hasattr(c, 'component') else c
            o = by_path.get(cp.path_id)
            if o is not None:
                yield o

    def walk_go(go, path=''):
        p = path + '/' + go.m_Name
        for o in components(go):
            yield p, o
            if o.type.name == 'Transform':
                for ch in o.read().m_Children:
                    yield from walk_go(by_path[ch.path_id].read().m_GameObject.read(), p)

    report = {'merchants': {}, 'palette': {}, 'icons': {}}
    for o in env.objects:
        if o.type.name != 'GameObject':
            continue
        go = o.read()
        if go.m_Name not in MERCHANTS:
            continue
        # the graphics prefab carries the merchant MonoBehaviour itself
        if not any(c.type.name == 'MonoBehaviour' and cls(c)[0] == go.m_Name for c in components(go)):
            continue
        base = go.m_Name
        folder = 'merchants/' + base
        entry = {'objectID': MERCHANTS[base], 'prefabPathID': o.path_id, 'renderers': [], 'sheets': {}}
        sheet_tex = set()
        for p, c in walk_go(go):
            if c.type.name == 'SpriteRenderer':
                sp = c.read().m_Sprite.path_id
                if sp:
                    s = by_path[sp].read()
                    tid = s.m_RD.texture.path_id
                    entry['renderers'].append({'path': p, 'sprite': s.m_Name,
                                               'texture': by_path[tid].read().m_Name if tid in by_path else 'atlas'})
                    if tid in by_path and '/Shadow' not in p and 'Glow' not in p:  # shared shadow / flare sprites are not ours
                        sheet_tex.add(s.m_RD.texture.path_id)
            elif c.type.name == 'Animator':
                ctrl = by_path.get(c.read().m_Controller.path_id)
                if ctrl is None:
                    continue
                ct = ctrl.read()
                clips = []
                for cp in ct.m_AnimationClips:
                    clip = by_path.get(cp.path_id)
                    if clip is None:
                        continue
                    cl = clip.read()
                    used = set()
                    for pp in cl.m_ClipBindingConstant.pptrCurveMapping:
                        so = by_path.get(pp.path_id)
                        if so is not None and so.type.name == 'Sprite':
                            tid = so.read().m_RD.texture.path_id
                            if tid not in by_path:
                                continue
                            tn = by_path[tid].read().m_Name
                            used.add(tn)
                            if not tn.startswith('FlareElements'):
                                sheet_tex.add(tid)
                    clips.append({'clip': cl.m_Name, 'sheets': sorted(used)})
                entry['animator'] = {'controller': ct.m_Name, 'clips': clips}
            elif c.type.name == 'MonoBehaviour':
                cn, t = cls(c)
                # IndirectLightSprite / ShadowSprite use shared vanilla SpriteAssets (white16x16, Shadow)
                if cn == 'SpriteObject' and 'm_assetRef' in t and 'IndirectLight' not in p and 'Shadow' not in p:
                    sa = sprite_assets.get(addr(t['m_assetRef']))
                    if sa is None:
                        continue

                    def sd(d):
                        return {'texture': save_tex(d['texture']['m_PathID'], folder),
                                'emissive': save_tex(d['emissiveTexture']['m_PathID'], folder),
                                'normal': save_tex(d['normalTexture']['m_PathID'], folder),
                                'pivot': [d['pivot']['x'], d['pivot']['y']]}
                    anims = [{'guid': a['m_guid'], 'sprite': sd(a['m_spriteData']),
                              'srcFrameCount': a['srcFrameCount'], 'fps': a['fps'], 'loop': a['loop'],
                              'variants': [sd(v) for v in a['m_variants']]} for a in sa['m_animations']]
                    entry.setdefault('spriteAssets', []).append({
                        'renderer': p, 'spriteAsset': sa['m_Name'], 'address': list(addr(t['m_assetRef'])),
                        'static': sd(sa['m_staticSpriteData']), 'staticVariants': [sd(v) for v in sa['m_staticVariants']],
                        'animations': anims})
        for tid in sorted(sheet_tex):
            name = save_tex(tid, folder)
            entry['sheets'][name] = sorted((sprite_info(s) for s in sprites_by_tex.get(tid, [])),
                                           key=lambda s: (len(s['name']), s['name']))
        entry['kind'] = 'spriteAsset' if entry.get('spriteAssets') else 'legacy'
        report['merchants'][base] = entry

    names = {}
    for o in env.objects:
        if o.type.name == 'Texture2D':
            names.setdefault(o.read().m_Name, o.path_id)
    for n in PALETTE_TEXTURES:
        if n in names:
            report['palette'][n] = save_tex(names[n], 'palette')
        else:
            print('missing palette texture', n)

    for o in mbs:
        try:
            t = o.read_typetree()
        except Exception:
            continue
        info = t.get('objectInfo')
        if isinstance(info, dict) and info.get('objectID') in ICON_IDS and info.get('variation') == 0:
            oid = info['objectID']
            report['icons'][ICON_IDS[oid]] = {'objectID': oid,
                                              'icon': sprite_png(by_path, info.get('icon'), 'icon_' + ICON_IDS[oid])}
    json.dump(report, open(os.path.join(OUT, 'reference.json'), 'w'), indent=1)
    for b, e in report['merchants'].items():
        print(b, e['kind'], list(e['sheets']), [a['spriteAsset'] for a in e.get('spriteAssets', [])])
    print('icons', {k: v['icon'] for k, v in report['icons'].items()})
    print('wrote', OUT)


def sprite_png(by_path, pptr, name):
    if not pptr or not pptr.get('m_PathID'):
        return None
    o = by_path.get(pptr['m_PathID'])
    if o is None:
        return None
    s = o.read()
    s.image.save(os.path.join(OUT, 'palette', name + '.png'))
    return s.m_Name


if __name__ == '__main__':
    main()
