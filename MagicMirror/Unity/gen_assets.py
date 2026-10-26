from pathlib import Path
import sys,struct,re,zipfile,shutil
sdk=Path(sys.argv[1]) if len(sys.argv)>1 else Path(r'C:/Users/Sid/CoreKeeperModSDK')
root=sdk/'Assets/MagicMirror'
for p in ('Textures','Data/TextDataBlock/Items','Data/EntityAuthoringDataBlock','Prefabs'): (root/p).mkdir(parents=True,exist_ok=True)
shutil.copy2(Path(__file__).parent/'Art/MagicMirror_icon.png',root/'Textures/MagicMirror_icon.png')
g='f24d9b6e721a4cc4a650c8de17923101'
(root/'Textures/MagicMirror_icon.png.meta').write_text('fileFormatVersion: 2\nguid: '+g+'\n')
g='f24d9b6e721a4cc4a650c8de17923102'
w1=int(g[:8],16);w2=int(g[12:16]+g[8:12],16)
lo,hi=struct.unpack('<qq',struct.pack('<II',w1,w2)+bytes.fromhex(g[16:]))
with zipfile.ZipFile(sdk/'Assets/Examples.zip') as z: t=z.read('Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset').decode()
t=t.replace('m_Name: MyNewWorkbench1','m_Name: MagicMirror_Mirror').replace('title: MyNewWorkbench1','title: Magic Mirror').replace('description: A workbench crafted from chaos.','description: A glimmer of home. Returns you to the Core after a short cast. Never consumed.')
t=re.sub(r'(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+',lambda m:f'{m[1]}{lo}{m[2]}{hi}',t,count=1)
p=root/'Data/TextDataBlock/Items/MagicMirror_Mirror.asset';p.write_text(t)
p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: '+g+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')
