"""Check the port has real authored UI and no upstream executable behaviours."""
from pathlib import Path
import UnityPy

paths = list((Path(__file__).parents[1] / 'Bundles').glob('ConnectedStorageBrowser_*.assetbundle'))
assert len(paths) == 2, 'Expected Windows and Linux browser bundles'
for path in paths:
    env = UnityPy.load(str(path))
    scripts = [o.read() for o in env.objects if o.type.name == 'MonoScript']
    assert scripts and all(s.m_AssemblyName == 'Pug.Other.dll' for s in scripts)
    names = {o.read().m_Name for o in env.objects if o.type.name == 'GameObject'}
    assert {'StorageGrid', 'StorageSearch', 'StorageSort', 'StorageOrder', 'StorageLayout',
            'StorageFilters', 'StorageGridSlot', 'StorageListSlot', 'StorageMetrics'} <= names
    classes = {s.m_ClassName for s in scripts}
    assert {'PugText', 'TextInputField', 'UIScrollWindow', 'ScrollBar', 'SlotUIBase', 'ButtonUIElement'} <= classes
    textures = {o.read().m_Name for o in env.objects if o.type.name == 'Texture2D'}
    assert {'ui_classic', 'ui_icon'} <= textures
    print(path.name, 'PASS: authored controls, fonts/components, atlas; no Item Browser executable code')
