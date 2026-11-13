"""Extract installed native material references; never package those game textures."""
from pathlib import Path
from collections import Counter
import json
import UnityPy
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
BUNDLE = Path('C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_all.bundle')
OUT = ROOT / 'build/connectedstorage/color-audit-20261112'
OUT.mkdir(parents=True, exist_ok=True)
TIERS = ['Copper','Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium','Relucite']
DESIGN = {
 'Copper': ('warm copper/salmon metal', ['#824223','#c16436','#e88b69','#fca895'], ['#915b26','#f7eec7'], 'plain copper brackets and one broad rivet; foundation tier'),
 'Tin': ('muted taupe/lavender pewter', ['#645d53','#8f7b77','#a69298','#c0bacf'], ['#d7dcfb'], 'soft pewter cap bands; avoid icy blue identity'),
 'Iron': ('cool blue-gray polished steel', ['#142b5c','#47677e','#677fae','#a1aedd'], ['#d7dcfb','#1885d8'], 'square reinforcing clamps and cool steel glints'),
 'Scarlet': ('red/coral metal', ['#5e2a28','#91210b','#b43221','#c76369'], ['#de8e9a','#ecc1bf'], 'red angular brace shoulders; restrained pink highlights'),
 'Octarine': ('purple/rose face with vivid cobalt-blue edge', ['#4e3c6d','#723e8a','#954d94','#c76369'], ['#142b5c','#143eab','#165dd9','#1885d8','#ecc1bf'], 'purple rose socket and cap, blue lower bevel; small cyan glint only'),
 'Galaxite': ('near-black iridescent metal with sparse multicolor inclusions', ['#0b1917','#232930','#383447','#484454'], ['#c9ab51','#239029','#1885d8','#a555a4'], 'dark cap and tiny separated colored inclusions, never a solid purple body'),
 'Solarite': ('golden orange metal with cyan/mint glints', ['#915b26','#da7910','#e2a12c','#e0c961'], ['#f7eec7','#19bdc6','#4ad8ae','#94f7dd'], 'golden stepped shoulders with one small cool glint'),
 'Pandorium': ('green emerald metal with purple shadow', ['#073c31','#0f5630','#0a6d27','#239029'], ['#4e3c6d','#723e8a','#0f9e88','#4ad8ae','#94f7dd'], 'green inset/bands with purple recess and mint glint; not cobalt blue'),
 'Relucite': ('violet material paired with orange/gold metal', ['#2b215f','#5722c2','#8539d1','#a555a4'], ['#824223','#c76b23','#e2a12c','#e0c961','#de8e9a'], 'violet cap or crystal, orange-gold prongs; pink is a highlight, not the dominant identity'),
}
env = UnityPy.load(str(BUNDLE))
wanted_sprites = {f'lootsprite_{t.lower()}{k}' for t in TIERS for k in ('Bar','Ore','Bar_small','Ore_small')}
wanted_textures = {'chest'+t for t in TIERS}
found = {}
for obj in env.objects:
    if obj.type.name not in ('Sprite', 'Texture2D'):
        continue
    data = obj.read()
    if (obj.type.name == 'Sprite' and data.m_Name in wanted_sprites) or (obj.type.name == 'Texture2D' and data.m_Name in wanted_textures):
        data.image.save(OUT / (data.m_Name + '.png'))
        found[data.m_Name] = {'type':obj.type.name,'path_id':obj.path_id,'size':list(data.image.size)}
result = {'date':'2026-11-12','evidence':'Fresh UnityPy extraction from installed game; no web or name-based color inference.',
          'source_bundle':str(BUNDLE),'reference_only':True,
          'palette_rule':'Listed palette hex values occur in the exact native bar sprite. Main/accent classification and proposed motif are human-readable design judgments, not game metadata. Preserve the overall Runic Storage housing.',
          'uncertainty':'Bar/ore/chest pixels verified. Palette reflects source sprite colors before in-game lighting. Proposed upgrade gems are original mod objects, so use material identity rather than claiming vanilla has the same gem. A single 16x16 design cannot retain every native accent.',
          'tiers':{}}
board=Image.new('RGB',(1100,900),'#262d36');draw=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',18)
for i,tier in enumerate(TIERS):
    low=tier.lower(); name='lootsprite_'+low+'Bar'
    im=Image.open(OUT/(name+'.png')).convert('RGBA')
    counts=Counter(px[:3] for px in im.getdata() if px[3]>128)
    color_counts={'#%02x%02x%02x'%rgb:n for rgb,n in counts.most_common()}
    identity,main,accents,motif=DESIGN[tier]
    assert all(c in color_counts for c in main+accents), (tier,main,accents)
    sources=[n for n in ['lootsprite_'+low+'Bar','lootsprite_'+low+'Ore','chest'+tier] if n in found]
    result['tiers'][tier]={'identity':identity,'main_dark_to_light':main,'accent_colors':accents,
                           'suggested_unit_motif':motif,'source_assets':{n:found[n] for n in sources},
                           'native_bar_color_counts':color_counts,
                           'missing_reference':[] if 'chest'+tier in found else ['No exact chest'+tier+' Texture2D found in inspected bundle; use bar/ore as material authority.']}
    x=15+(i%3)*365;y=15+(i//3)*295;draw.text((x,y),tier.upper(),font=font,fill='white')
    for j,(kind,n) in enumerate([('Bar',name),('Ore','lootsprite_'+low+'Ore'),('Chest','chest'+tier)]):
        if n not in found: continue
        sample=Image.open(OUT/(n+'.png')).convert('RGBA').resize((96,96),Image.Resampling.NEAREST)
        board.paste(sample,(x+j*112,y+35),sample)
        draw.text((x+j*112,y+135),kind,font=font,fill='white')
    for k,(rgb,count) in enumerate(counts.most_common(8)):
        xx=x+(k%4)*85;yy=y+170+(k//4)*48
        draw.rectangle((xx,yy,xx+65,yy+22),fill=rgb)
        draw.text((xx,yy+24),'#%02x%02x%02x'%rgb,fill='white')
board.save(OUT/'native-material-evidence.png')
(HERE/'native-palettes.json').write_text(json.dumps(result,indent=2)+'\n')
print(OUT/'native-material-evidence.png')
print(HERE/'native-palettes.json')
