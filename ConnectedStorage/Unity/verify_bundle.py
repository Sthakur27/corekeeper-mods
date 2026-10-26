"""Inspect shipped prefab contents independently using UnityPy."""
from pathlib import Path
import sys
import UnityPy
from PIL import Image
CAPACITY=dict(zip(('Copper','Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium'),(20,40,60,80,100,120,140,160)))
paths=[Path(x) for x in sys.argv[1:]] or list((Path(__file__).parents[1]/'Bundles').glob('ConnectedStorage_*.assetbundle'))
assert paths, 'No bundles found'
for path in paths:
 env=UnityPy.load(str(path)); prefabs={}; vanilla=set()
 # Compare visible pixels, not transparent RGB (Unity dilates transparent padding).
 texture_count = 0
 for obj in env.objects:
  if obj.type.name != 'Texture2D': continue
  data = obj.read()
  source = Path(__file__).parent / 'Art' / (data.m_Name + '.png')
  if not source.exists() or data.m_Name in ('preview', 'directions'): continue
  actual, expected = data.image.convert('RGBA'), Image.open(source).convert('RGBA')
  assert actual.size == expected.size, data.m_Name + ': bundled texture dimensions differ'
  for color in ('black', 'white'):
   backdrop = Image.new('RGBA', actual.size, color)
   assert Image.alpha_composite(backdrop, actual).tobytes() == Image.alpha_composite(backdrop, expected).tobytes(), data.m_Name + ': bundled artwork differs from source'
  texture_count += 1
 assert texture_count == 102, f'Expected 102 art textures, found {texture_count}' 
 # Prove the approved image crop is unchanged and high-resolution meshes stay one tile wide.
 for name,filename in [('CopperChest','RunicKilnReview/copper-node.png'),('Terminal','RunicKilnReview/terminal.png')] + [(n, 'StorageFamily/'+n+'.png') for n in ['Workbench']+[t+'Chest' for t in CAPACITY if t!='Copper']]:
  art=Image.open(Path(__file__).parent/'Art'/(name+'.png')).convert('RGBA')
  if art.width<=32: continue
  original=Image.open(Path(__file__).parents[1]/'ArtConcepts'/filename).convert('RGBA')
  bbox=original.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
  crop=original.crop(bbox);pad=round(crop.width/14)
  assert art.crop((pad,pad,pad+crop.width,pad+crop.height)).tobytes()==crop.tobytes(), name+': approved pixels changed'
  found=False
  for obj in env.objects:
   if obj.type.name!='Transform':continue
   tr=obj.read()
   if tr.m_GameObject.read().m_Name!='Sprite Object':continue
   parent=tr.m_Father.read()
   if not parent.m_Father:continue
   if parent.m_GameObject.read().m_Name!='ArtScale':continue
   xscaler=parent.m_Father.read()
   if not xscaler.m_Father or xscaler.m_Father.read().m_GameObject.read().m_Name!=name+'Graphics':continue
   scale=parent.m_LocalScale
   assert abs(tr.m_LocalScale.x-1)<0.000001 and abs(tr.m_LocalScale.y-1)<0.000001, name+': direction-controlled child must have unit scale'
   # Runtime direction updates reset the child to (1,1,1) or (-1,1,1).
   for facing_x in (-1,1):
    assert abs(abs(scale.x*facing_x)*art.width/16-1)<0.0001, name+': direction reset changes rendered width'
   assert abs(scale.x*art.width/16-1)<0.0001, name+': high-resolution world width must be one tile'
   assert abs(scale.x-scale.y)<0.000001, name+': distorted aspect ratio'
   found=True
  assert found, name+': scaled visual missing'
 for tier in ('Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium'):
  source=Image.open(Path(__file__).parents[1]/'ArtConcepts/UpgradeGems'/(tier+'.png')).convert('RGBA')
  crop=source.crop(source.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox())
  art=Image.open(Path(__file__).parent/'Art'/(tier+'Gem.png')).convert('RGBA')
  x=(art.width-crop.width)//2;y=(art.height-crop.height)//2
  assert art.crop((x,y,x+crop.width,y+crop.height)).tobytes()==crop.tobytes(),tier+': generated gem pixels changed'
 for o in env.objects:
  if o.type.name != 'MonoBehaviour': continue
  data=o.read(); tree=o.read_typetree()
  if not data.m_GameObject: continue
  name=data.m_GameObject.read().m_Name
  script=data.m_Script.read()
  prefabs.setdefault(name,{})[script.m_ClassName]=tree
  vanilla.add(script.m_AssemblyName)
 for tier,capacity in CAPACITY.items():
  name=tier+'Chest'; c=prefabs[name+'Entity']
  assert c['ObjectAuthoring']['objectName']=='ConnectedStorage_'+name
  assert 'DontDestroyOnZeroHealthAuthoring' in c
  assert 'DontDropContainedAuthoring' not in c
  inv=c['InventoryAuthoring']; assert inv['sizeX']*inv['sizeY']==capacity
  assert inv['sizeX']==2, 'Storage must not match Double Chest Inventory dimensions'
  assert inv['maxExtraSize']==0 and not inv['autoTransferEnabled']
  assert c['InventoryItemAuthoring']['requiredObjectsToCraft']==([{'objectName':tier+'Bar','amount':30}] if tier=='Copper' else [])
  if tier != 'Copper':
   gem=prefabs[tier+'GemEntity']
   assert gem['ObjectAuthoring']['objectName']=='ConnectedStorage_'+tier+'Gem'
   assert gem['ObjectAuthoring']['objectType']==0
   assert 'InventoryAuthoring' not in gem and 'PlaceableObjectAuthoring' not in gem
   assert gem['InventoryItemAuthoring']['requiredObjectsToCraft']==[{'objectName':tier+'Bar','amount':30}]
  assert 'Chest' in prefabs[name+'Graphics']
 w=prefabs['WorkbenchEntity']
 assert w['ObjectAuthoring']['objectName']=='ConnectedStorage_Workbench'
 assert w['InventoryItemAuthoring']['requiredObjectsToCraft']==[{'objectName':'Wood','amount':20}]
 assert 'InventoryAuthoring' not in w
 assert w['CraftingAuthoring']['craftingType']==0
 expected=['Terminal','CopperChest','TinGem','IronGem','ScarletGem','OctarineGem','GalaxiteGem','SolariteGem','PandoriumGem']
 recipes=w['CraftingAuthoring']['canCraftObjects']
 assert [r['moddedObjectID'] for r in recipes]==['ConnectedStorage_'+n for n in expected]
 assert all(r['amount']==1 for r in recipes)
 graphics=prefabs['WorkbenchGraphics']['CraftingBuilding']
 assert graphics['craftingCategoryWindowInfos']==[]
 assert [t['mTerm'] for t in graphics['defaultUISettings']['titles']]==['Storage','Upgrades']
 t=prefabs['TerminalEntity']; assert 'InventoryAuthoring' not in t
 assert t['ObjectAuthoring']['objectName']=='ConnectedStorage_Terminal'
 assert t['InventoryItemAuthoring']['requiredObjectsToCraft']==[{'objectName':'CopperBar','amount':10},{'objectName':'Wood','amount':20}]
 assert 'ConnectedStorage.dll' not in vanilla
 print(path.name+': PASS - storage workbench with nine recipes, eight units, seven gems, crafting costs and native UI')

