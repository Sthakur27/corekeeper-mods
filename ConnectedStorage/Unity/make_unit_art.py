"""Original exact-pixel sealed storage units and upgrade gems.

Studied native copper/ancient/alien chests, crystal light and tier workbenches
for footprint, material ramps, top-left lighting and stepped silhouette only.
No native pixels or textures are copied. Run directly or from make_art.py.
"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).parent / 'Art'
# Deliberately hue-shifted material ramps: shade, mid, face, highlight.
TIERS = [
 ('Copper', ('663c30','a45435','db8856','fbc491')),
 ('Tin', ('3c555d','66868a','a4bcc0','deece2')),
 ('Iron', ('343d54','58657c','8e9eaf','cdd5dc')),
 ('Scarlet', ('54243d','9b3246','e65e66','ffb09a')),
 ('Octarine', ('193f65','256e94','36b5b9','a1eee0')),
 ('Galaxite', ('382d5b','6c478c','b276cc','eab8f3')),
 ('Solarite', ('71472a','c28631','f2c650','fff0a2')),
 ('Pandorium', ('272b56','444c99','7888df','c8c8ff')),
]
STONE = ['253f43','3e5a5a','667b77','9eaaa0']
def rgba(s): return tuple(bytes.fromhex(s)) + (255,)
def unit(index, facing):
 name, ramp = TIERS[index]
 shadow, dark, metal, light = map(rgba,ramp)
 st0,st1,st2,st3=map(rgba,STONE)
 im=Image.new('RGBA',(16,16)); d=ImageDraw.Draw(im)
 # Solid one-piece housing: broad top plane, deep front wall, recessed core.
 # This is a built appliance, with no lid seam, latch or floating trophy.
 d.polygon([(3,1),(12,1),(14,3),(14,12),(12,15),(2,15),(1,12),(1,3)],fill=st0)
 d.rectangle((2,3,13,11),fill=dark)
 d.polygon([(3,2),(11,2),(13,4),(13,9),(11,11),(2,10),(2,4)],fill=metal)
 d.line((3,2,10,2),fill=light)
 d.line((2,4,2,8),fill=light)
 # Thick front face is continuous with housing, not a horizontal lid border.
 d.polygon([(2,10),(11,11),(13,9),(13,12),(11,14),(3,14),(2,12)],fill=shadow)
 d.line((3,10,9,11),fill=dark)
 d.rectangle((3,12,9,13),fill=dark)
 d.point((3,12),fill=metal)
 d.rectangle((4,14,5,15),fill=st1); d.rectangle((11,13,12,14),fill=st1)
 # A stepped open socket supports a pointed prism; no concentric aperture.
 cx,cy = (6,5) if facing=='' else ((8,5) if facing=='_side' else (6,5))
 d.polygon([(cx-3,8),(cx,7),(cx+3,8),(cx+3,10),(cx-1,11),(cx-3,10)],fill=shadow)
 d.line((cx-3,8,cx-1,7),fill=dark)
 d.line((cx-2,10,cx,10),fill=light)
 glow=[(94,192,177,255),(119,220,205,255),(109,186,217,255),(238,155,107,255),(110,222,218,255),(192,149,231,255),(126,210,233,255),(152,205,237,255)][index]
 # Three uninterrupted facets, physically seated in the machine's top surface.
 d.polygon([(cx,0),(cx+2,3),(cx+2,7),(cx,9),(cx-2,7),(cx-2,3)],fill=st0)
 d.polygon([(cx,1),(cx+1,3),(cx+1,7),(cx,8),(cx-1,7),(cx-1,3)],fill=glow)
 d.line((cx-1,3,cx-1,6),fill=(200,242,218,255))
 d.line((cx+1,5,cx+1,7),fill=st1)
 if facing=='_up':
  # Near rear plate occludes the core's lower portion; no forward-facing window.
  d.polygon([(3,7),(9,8),(11,10),(11,12),(3,11)],fill=dark)
  d.line((3,7,8,8),fill=light)
  d.line((4,9,4,11),fill=shadow); d.line((8,10,8,11),fill=shadow)
 # Asymmetric conduit on right. Its bright top bends into the housing.
 px=11 if facing!='_side' else 3
 d.line((px,4,px,8),fill=shadow,width=2)
 d.line((px,4,px,7),fill=light)
 d.point((px+1,8),fill=dark)
 d.point((px,3),fill=st3)
 d.point((4,3),fill=dark); d.point((9,9),fill=light)
 # Facing-specific machining. Back carries cooling ribs, side has cable entry.
 if facing=='_up':
  for x in (4,6,8): d.line((x,12,x,13),fill=st0)
  d.line((10,11,11,12),fill=metal)
 elif facing=='_side':
  d.rectangle((3,11,5,12),fill=st0); d.line((3,11,4,11),fill=metal)
  d.line((8,12,11,12),fill=dark)
 else:
  d.line((5,12,8,12),fill=st0); d.point((5,12),fill=glow)
 # Tier hardware is structural, rather than repeated colored decoration.
 if index==0:
  d.point((12,9),fill=(72,117,80,255)); d.point((3,8),fill=(88,131,87,255))
 elif index==1:
  d.line((2,9,3,11),fill=st3); d.line((11,10,12,9),fill=light)
 elif index==2:
  d.rectangle((1,4,2,7),fill=st1); d.line((1,4,1,6),fill=st3)
  d.rectangle((12,10,13,12),fill=st1); d.point((12,10),fill=light)
 elif index==3:
  d.line((3,1,5,1),fill=light); d.line((12,5,13,6),fill=st0)
  d.line((3,13,5,13),fill=metal)
 elif index==4:
  # Heavy offset cooling bank visibly extends the outer silhouette.
  d.rectangle((13,3,15,10),fill=shadow)
  d.line((13,3,14,3),fill=light)
  for yy in (5,8): d.line((13,yy,15,yy),fill=metal)
  d.line((3,14,8,14),fill=metal)
 elif index==5:
  # Sloped mineral buttress, broad enough to read at native scale.
  d.polygon([(11,0),(14,3),(15,8),(13,11),(11,8)],fill=shadow)
  d.polygon([(11,1),(13,3),(14,7),(12,8)],fill=metal)
  d.line((11,1,12,3),fill=light)
 elif index==6:
  # Two flared golden clamps grip the core housing rather than framing a window.
  d.polygon([(0,4),(2,2),(3,4),(3,9),(1,11),(0,8)],fill=dark)
  d.line((1,4,2,3),fill=light); d.line((1,5,1,8),fill=metal)
  d.polygon([(12,2),(15,4),(15,8),(13,10),(12,8)],fill=shadow)
  d.line((13,3,14,4),fill=light); d.line((14,5,14,7),fill=metal)
 elif index==7:
  # Tall rear fins materially change both outline and visible construction.
  d.rectangle((1,0,3,6),fill=shadow); d.line((1,0,2,0),fill=light)
  d.line((2,1,2,4),fill=metal)
  d.rectangle((12,0,14,7),fill=shadow); d.line((12,0,13,0),fill=light)
  d.line((13,1,13,5),fill=metal)
  d.point((13,6),fill=glow)
 em=Image.new('RGBA',im.size); ed=ImageDraw.Draw(em)
 ed.line((cx-1,cy-1,cx-1,cy),fill=(*glow[:3],90))
 return im,em

def gem(index):
 _,ramp=TIERS[index]; shadow,dark,metal,light=map(rgba,ramp)
 im=Image.new('RGBA',(16,16)); d=ImageDraw.Draw(im)
 # Cut crystal cartridge: irregular facets and lower metal setting, not a UI glyph.
 shapes=[None,
 [(5,2),(10,2),(12,5),(11,10),(7,13),(3,10),(2,5)],
 [(7,1),(11,3),(12,8),(9,12),(5,12),(3,8),(3,4)],
 [(8,1),(11,5),(10,10),(6,14),(3,10),(3,5)],
 [(6,1),(10,3),(12,7),(9,12),(5,13),(2,9),(3,4)],
 [(7,1),(10,3),(11,7),(13,9),(10,12),(6,13),(3,10),(2,6),(5,4)],
 [(5,1),(10,2),(12,5),(11,10),(8,13),(4,11),(2,7),(3,3)],
 [(8,1),(11,3),(13,7),(11,11),(6,14),(3,10),(2,6),(5,3)],
 ]
 d.polygon(shapes[index],fill=shadow)
 d.polygon([(6,3),(9,3),(11,6),(9,10),(6,12),(4,9),(3,6)],fill=metal)
 d.polygon([(6,3),(9,3),(7,6),(4,9),(3,6)],fill=light)
 d.polygon([(9,3),(11,6),(9,10),(6,12),(7,6)],fill=dark)
 d.polygon([(7,6),(9,6),(8,9),(6,12),(5,9)],fill=metal)
 d.line((5,4,6,3),fill=(229,239,216,255))
 # Manufactured setting cradles just the bottom third, offset by perspective.
 d.line((3,9,5,12),fill=(63,83,81,255))
 d.line((5,12,8,13),fill=(109,132,125,255))
 d.line((8,13,11,10),fill=(49,67,73,255))
 d.point((4,10),fill=(169,184,163,255))
 if index==1:
  d.line((5,2,9,2),fill=light); d.point((10,6),fill=metal)
 elif index==2:
  d.line((4,7,6,7),fill=light); d.rectangle((9,10,10,11),fill=shadow)
 elif index==3:
  d.line((7,3,8,5),fill=light); d.point((8,9),fill=light)
 elif index==4:
  d.point((3,8),fill=light); d.line((9,4,10,5),fill=metal)
 elif index==5:
  d.line((11,8,12,9),fill=metal); d.point((5,6),fill=dark)
  d.point((7,8),fill=light)
 elif index==6:
  d.line((4,3,5,2),fill=light); d.line((9,9,10,8),fill=light)
 elif index==7:
  d.line((8,2,9,3),fill=light); d.line((4,7,5,8),fill=dark)
  d.line((9,6,10,7),fill=light)
 em=Image.new('RGBA',im.size); ed=ImageDraw.Draw(em)
 ed.point((5,4),fill=(*rgba(ramp[2])[:3],75))
 return im,em

def generate():
 OUT.mkdir(exist_ok=True)
 for i,(name,_) in enumerate(TIERS):
  for face in ('','_side','_up'):
   im,em=unit(i,face)
   im.save(OUT/(name+'Chest'+face+'.png'))
   em.save(OUT/(name+'Chest'+face+'_emissive.png'))
   if i:
    im,em=gem(i)
    im.save(OUT/(name+'Gem'+face+'.png'))
    em.save(OUT/(name+'Gem'+face+'_emissive.png'))
 # Preview is deliberately outside runtime Art to avoid importing a sprite sheet.
 preview=Path(__file__).parents[2]/'build/connectedstorage/art-review'
 preview.mkdir(parents=True,exist_ok=True)
 sheet=Image.new('RGBA',(8*128,510),(36,43,48,255)); d=ImageDraw.Draw(sheet)
 for i,(name,_) in enumerate(TIERS):
  d.text((i*128+18,8),name,fill='#e4dfcd')
  for row,suffix in enumerate(('Chest','Chest_side','Chest_up','Gem')):
   if row==3 and not i: continue
   sprite=Image.open(OUT/(name+suffix+'.png'))
   sheet.alpha_composite(sprite.resize((96,96),Image.Resampling.NEAREST),(i*128+16,30+row*120))
 sheet.save(preview/'units-and-gems.png')
 print('Wrote 90 original unit/gem PNGs; preview:',preview/'units-and-gems.png')

if __name__=='__main__': generate()
