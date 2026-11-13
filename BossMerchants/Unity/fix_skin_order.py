"""Repair only the egg merchant's skin animation order in an existing bundle.

Usage: python fix_skin_order.py INPUT OUTPUT
Reopens and checks every unrelated serialized object and resource stream unchanged.
"""
from pathlib import Path
import copy
import sys
import UnityPy
from update_combined_art import object_map, resources, digest

ORDER = ['580fae3f-66dc-4b7a-900c-ee7986f742e6',
         'e1e86c38-70e3-402a-a63f-ecb6d1f3edf0',
         '7128f66e-0c6d-4aed-949e-93fdcd9f5138']

def repair(source, output):
    assert source.resolve() != output.resolve(), 'Write to a new output; preserve the original.'
    env = UnityPy.load(source.read_bytes())
    before = object_map(env)
    hashes = {k: digest(o.get_raw_data()) for k, o in before.items()}
    streams = resources(env)
    targets = [(k,o,o.read_typetree()) for k,o in before.items() if o.type.name == 'MonoBehaviour'
               and o.read_typetree().get('m_Name') == 'bossMerchant_Eggs_skin']
    assert len(targets) == 1
    key, obj, tree = targets[0]
    fixed = copy.deepcopy(tree)
    old = fixed['m_replacementData']
    assert sorted(r['m_guid'] for r in old) == sorted(ORDER)
    fixed['m_replacementData'] = sorted(old, key=lambda r: ORDER.index(r['m_guid']))
    obj.save_typetree(fixed)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(env.file.save(packer='lz4'))
    check = UnityPy.load(output.read_bytes())
    after = object_map(check)
    assert set(before) == set(after) and resources(check) == streams
    for k, o in after.items():
        if k == key: assert o.read_typetree() == fixed
        else: assert digest(o.get_raw_data()) == hashes[k], k
    print(f'{output}: native idle/emote/move order verified; every other object/stream unchanged')

if __name__ == '__main__':
    repair(Path(sys.argv[1]), Path(sys.argv[2]))
