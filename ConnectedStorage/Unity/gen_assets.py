"""Generate SDK source assets for every storage object, with stable GUIDs."""
from pathlib import Path
import hashlib, re
HERE=Path(__file__).parent
source=(HERE/'asset_template.py.txt').read_text()
for name,capacity in [('Workbench',-2),('Terminal',0),('CopperChest',20),('TinChest',40),('IronChest',60),('ScarletChest',80),('OctarineChest',100),('GalaxiteChest',120),('SolariteChest',140),('PandoriumChest',160)]+[(tier+'Gem',-1) for tier in ('Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium')]:
 s=source.replace('EnderStash','ConnectedStorage').replace('EnderChest',name)
 for old in set(re.findall(r'6e1d0a3b9c7f4e21a5b8c3d2e1f00[ab][0-9]{2}',s)):
  s=s.replace(old,hashlib.md5(('ConnectedStorage/'+name+'/'+old).encode()).hexdigest())
 title='Storage Terminal' if not capacity else name.replace('Chest',' Storage Unit').replace('Gem',' Upgrade Gem')
 desc=('Search and use the adjacent connected storage network.' if capacity==0 else ('Select this gem in your hotbar, then interact with a storage unit of the immediately preceding tier. Costs 30 '+name.replace('Gem','')+' bars.' if capacity<0 else str(capacity)+' shared storage slots. Interact to access the connected storage inventory. Apply the next tier gem to upgrade.'))
 if name=='Workbench':
  title='Storage Workbench'; desc='Craft the Storage Terminal, Copper Storage Unit and every sequential upgrade gem here. Costs 20 wood.'
 s=re.sub(r'TITLE = .*',lambda m:'TITLE = '+repr(title),s)
 s=re.sub(r'DESC = .*',lambda m:'DESC = '+repr(desc),s)
 s=s.replace('0.0625, 4)', '(1 / 20 if '+repr(name)+' == "Terminal" else 0.0625), 4)').replace('1 / 19, 4)', '(1 / 20 if '+repr(name)+' == "Terminal" else 0.0625), 4)')
 exec(compile(s,str(HERE/'asset_template.py.txt'),'exec'),{'__file__':str(HERE/'gen_assets.py')})
