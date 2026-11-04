"""Export the vanilla boat skin textures (Boat.boatSkins on the "Boat" graphics prefab) to Unity/Art/vanilla/.

    python -I DrillUnit/Unity/extract_skin.py

Writes <boatID>_front.png / <boatID>_back.png for every skin plus the prefab's default sprite sheets, and
6581_icon.png / 6581_smallIcon.png (the Speeder Boat item's ObjectInfo icon / smallIcon sprites, which live in
the bottom row of its back sheet), as references for make_skin.py / make_art.py. Re-run after a game update.
"""
import glob, os
import UnityPy

GAME = r'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\aa\StandaloneWindows64'
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'Art', 'vanilla')


def main():
    os.makedirs(OUT, exist_ok=True)
    env = UnityPy.load(*glob.glob(GAME + '/*monoscripts*'), *glob.glob(GAME + '/defaultlocalgroup_*.bundle'))
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            mb = o.read()
            if mb.m_Script.read().m_ClassName != 'Boat':
                continue
            tree = o.read_typetree()
        except Exception:
            continue
        print('Boat MonoBehaviour', o.path_id, 'skins', len(tree.get('boatSkins', [])))
        for skin in mb.boatSkins:
            for key, side in (('textureFront', 'front'), ('textureBack', 'back')):
                tex = getattr(skin, key).read()
                name = f"{skin.boatID}_{side}.png"
                tex.image.save(os.path.join(OUT, name))
                print('  wrote', name, tex.m_Width, tex.m_Height)
    export_icons(env)


def export_icons(env, prefabs={'SpeederBoatEntity': 6581}):
    for o in env.objects:
        if o.type.name != 'GameObject':
            continue
        go = o.read()
        if go.m_Name not in prefabs:
            continue
        for c in go.m_Component:
            p = c.component
            if p.type.name != 'MonoBehaviour':
                continue
            try:
                mb = p.read()
                if mb.m_Script.read().m_ClassName != 'EntityMonoBehaviourData':
                    continue
            except Exception:
                continue
            for field in ('icon', 'smallIcon'):
                sprite = getattr(mb.objectInfo, field).read()
                name = f"{prefabs[go.m_Name]}_{field}.png"
                sprite.image.save(os.path.join(OUT, name))
                print('  wrote', name, sprite.m_Name)


if __name__ == '__main__':
    main()
