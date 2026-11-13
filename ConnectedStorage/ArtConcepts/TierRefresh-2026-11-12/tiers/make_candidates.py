"""Native 16x16 review variants of the existing generated Octarine candidate.

Extends the repository's palette-keyed native art pipeline. No vanilla pixels
are copied. Color ramps come from audited native materials; geometry is ours.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
AUDIT=json.loads((HERE.parent/'color-audit/native-palettes.json').read_text())['tiers']
TIERS=list(AUDIT)
BASE=ROOT/'ConnectedStorage/ArtConcepts/NativeReview-2026-11-12/OctarineChest-16.png'
def rgba(h):return tuple(bytes.fromhex(h.lstrip('#')))+(255,)
STONE=['#20333b','#35444e','#4c576e','#68778f','#8a9fbe','#b7c4dc','#eee1cf']
MOTIFS={
 'Copper':['....','####','....'],
 'Tin':['##..','....','..##'],
 'Iron':['####','.##.','####'],
 'Scarlet':['..##','.##.','##..'],
 'Octarine':['.###','.##.','##..'],
 'Galaxite':['##.#','#.##','....'],
 'Solarite':['.#.#','####','.#.#'],
 'Pandorium':['###.','#.##','.###'],
 'Relucite':['.##.','####','.##.'],
}
THEMES={
 'Copper':'plain assembled brackets and a single inset line',
 'Tin':'fitted square caps and paired inset grooves',
 'Iron':'reinforced front band and squared clamp',
 'Scarlet':'angular channel with beveled shoulder corners',
 'Octarine':'stepped rune, rose-purple fittings with cobalt lower bevel',
 'Galaxite':'paired dark segments and balanced shoulders with sparse iridescent flecks',
 'Solarite':'split radiant sigil, short bright edge and gold bezel',
 'Pandorium':'continuous stepped rune and interlocking green corners with purple recess',
 'Relucite':'unified violet seal, integrated gold bezel and balanced end-tier highlights',
}

def unit(name,index):
 p=AUDIT[name]; ramp=list(map(rgba,p['main_dark_to_light'])); ac=list(map(rgba,p['accent_colors']))
 source=Image.open(BASE).convert('RGBA'); im=Image.new('RGBA',(16,16))
 # Retain exact common housing geometry. Replace semantic teal ramp with material.
 mapping={rgba('#063e43'):ramp[0],rgba('#075b5d'):ramp[0],rgba('#0c8984'):ramp[1],
          rgba('#21bba6'):ramp[2],rgba('#5bdfbe'):ramp[2] if index<3 else ramp[3]}
 for y in range(16):
  for x in range(16):
   c=source.getpixel((x,y))
   if not c[3]:continue
   if c==rgba('#eee1cf'):
    c=rgba(STONE[4] if index<2 else STONE[5] if index<6 else STONE[6])
   im.putpixel((x,y),mapping.get(c,c))
 d=ImageDraw.Draw(im)
 # Replace the shared zigzag with one compact inset rune, not additional clutter.
 d.rectangle((5,3,10,7),fill=rgba(STONE[4]))
 for y,row in enumerate(MOTIFS[name]):
  for x,c in enumerate(row):
   if c=='#':d.point((x+6,y+4),fill=ramp[1] if index<3 else ramp[2])
 # Per-tier structural treatment replaces selected existing bracket/band pixels.
 if index<2:
  # No gold ornament on the starter tiers.
  d.point((7,14),fill=ramp[1]);d.point((8,14),fill=ramp[0])
 if name=='Tin':
  d.line((1,1,2,1),fill=ramp[2]);d.line((13,1,14,1),fill=ramp[2])
 elif name=='Iron':
  d.line((1,10,14,10),fill=ramp[1]);d.line((2,10,4,10),fill=ramp[2])
 elif name=='Scarlet':
  d.point((2,2),fill=ramp[2]);d.point((13,2),fill=ramp[2])
 elif name=='Octarine':
  blue=ac[1]
  d.line((1,15,14,15),fill=blue)
  d.point((6,13),fill=ac[2]);d.point((9,13),fill=ac[1])
 elif name=='Galaxite':
  # Tiny native inclusions, not star decorations or a purple body.
  for xy,c in [((2,1),ac[0]),((13,1),ac[2]),((6,12),ac[1]),((9,13),ac[3])]:d.point(xy,fill=c)
  d.point((7,14),fill=rgba(STONE[5]));d.point((8,14),fill=ramp[1])
 elif name=='Solarite':
  d.line((5,1,9,1),fill=ac[0]);d.point((7,4),fill=ac[0]);d.point((9,4),fill=ac[0])
  d.point((7,12),fill=ac[2]);d.point((7,14),fill=ramp[3])
 elif name=='Pandorium':
  for xy in [(3,2),(12,2),(2,9),(13,9)]:d.point(xy,fill=ramp[2])
  d.line((5,11,10,11),fill=ac[0]);d.point((7,12),fill=ac[3]);d.point((8,12),fill=ac[2])
  d.point((6,4),fill=ac[3]);d.point((9,6),fill=ac[2])
  d.point((7,14),fill=ac[0]);d.point((8,14),fill=ramp[1])
 elif name=='Relucite':
  gold=ac[2]
  for xy in [(1,1),(2,1),(13,1),(14,1),(1,9),(14,9)]:d.point(xy,fill=gold)
  for xy in [(6,12),(9,12),(6,13),(9,13),(7,14),(8,14)]:d.point(xy,fill=gold)
  d.point((7,12),fill=ac[4]);d.point((8,12),fill=ramp[3])
  d.point((7,4),fill=ac[4]);d.point((8,4),fill=ramp[3])
 return im

# Silhouettes retain the approved board's squat/square/diamond/cluster identities.
SHAPES={
 'Tin':[(7,1),(11,4),(11,9),(8,12),(4,10),(3,5)],
 'Iron':[(4,2),(11,2),(12,4),(11,11),(4,11),(3,4)],
 'Scarlet':[(7,1),(10,4),(10,9),(7,13),(4,9),(4,4)],
 'Octarine':[(6,1),(10,2),(12,5),(11,10),(8,12),(4,11),(2,7),(3,3)],
 'Galaxite':[(8,1),(10,4),(10,7),(13,6),(12,11),(8,13),(4,11),(2,5),(5,6),(5,3)],
 'Solarite':[(6,1),(10,2),(12,5),(11,10),(8,12),(4,11),(2,7),(3,3)],
 'Pandorium':[(7,1),(10,4),(11,8),(8,13),(5,11),(3,7),(4,4)],
 'Relucite':[(6,1),(10,4),(10,6),(13,4),(12,10),(8,13),(4,11),(2,7),(5,8)],
}
def gem(name,index):
 p=AUDIT[name]; ramp=list(map(rgba,p['main_dark_to_light']));ac=list(map(rgba,p['accent_colors']))
 im=Image.new('RGBA',(16,16));mask=Image.new('1',(16,16));md=ImageDraw.Draw(mask);md.polygon(SHAPES[name],fill=1)
 # Facets constrained to silhouette; no anti-aliasing or scattered noise.
 for y in range(16):
  for x in range(16):
   if not mask.getpixel((x,y)):continue
   edge=any(not(0<=xx<16 and 0<=yy<16) or not mask.getpixel((xx,yy)) for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
   shade=0 if edge else 3 if x<7 and y<6 else 2 if x<8 else 1
   im.putpixel((x,y),ramp[shade])
 d=ImageDraw.Draw(im)
 # Common compact mounting family; later materials integrate more of their color.
 mount=[rgba(STONE[0]),rgba(STONE[2]),rgba(STONE[4])]
 if name=='Solarite':mount=[ramp[0],ramp[1],ramp[3]]
 if name=='Pandorium':mount=[ac[0],ramp[0],ramp[2]]
 if name=='Relucite':mount=[ac[0],ac[1],ac[3]]
 d.line((3,10,5,13),fill=mount[0],width=2);d.line((11,10,10,13),fill=mount[0],width=2)
 d.line((5,13,10,13),fill=mount[1],width=2)
 d.point((3,10),fill=mount[2]);d.point((11,10),fill=mount[2])
 d.point((6,14),fill=mount[0]);d.point((9,14),fill=mount[0])
 d.point((7,13),fill=mount[2]);d.point((8,13),fill=mount[1])
 if name=='Octarine':
  d.line((5,11,8,12),fill=ac[2]);d.point((10,8),fill=ac[1]);d.point((4,6),fill=ac[4])
 if name=='Galaxite':
  # Broad dark facets need a boundary on cave-dark backgrounds; audited native
  # cool steel reflection, not a purple replacement for the material itself.
  d.line((7,3,6,6),fill=rgba('#47677e'))
  d.line((6,6,7,10),fill=ramp[3])
  for xy,c in [((7,4),ac[0]),((4,7),ac[2]),((9,9),ac[1]),((8,11),ac[3])]:d.point(xy,fill=c)
 if name=='Solarite':d.point((5,4),fill=ac[0]);d.point((7,10),fill=ac[2])
 if name=='Pandorium':d.point((6,4),fill=ac[3]);d.point((7,5),fill=ac[2]);d.point((9,8),fill=ac[0])
 if name=='Relucite':d.point((6,3),fill=ac[4]);d.point((7,4),fill=ramp[3]);d.point((11,6),fill=ramp[3])
 return im

def main():
 stats={};(HERE/'sprites').mkdir(exist_ok=True)
 for i,name in enumerate(TIERS):
  for kind,im in [('Chest',unit(name,i))]+([] if i==0 else [('Gem',gem(name,i))]):
   assert im.size==(16,16) and set(im.getchannel('A').getdata())<={0,255}
   im.save(HERE/'sprites'/f'{name}{kind}.png')
   im.save(HERE/f'{name}{kind}-16.png')
   stats[name+kind]={'size':[16,16],'alpha':[0,255],'visible_colors':len({c for c in im.getdata() if c[3]}),'opaque_pixels':sum(c[3]>0 for c in im.getdata())}
 board=Image.new('RGB',(1440,570),'#252b33');d=ImageDraw.Draw(board)
 font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',15);title=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',22)
 d.text((20,12),'RUNIC STORAGE | native tier progression candidates',font=title,fill='#efdbb4')
 d.text((20,45),'Actual 16 x 16 sprites. Enlargements 8x nearest-neighbor. 1x strip below each row.',font=font,fill='#bbc5ce')
 for i,name in enumerate(TIERS):
  x=12+i*158;d.text((x+10,82),name,font=font,fill='#efdbb4')
  for kind,y in [('Chest',115),('Gem',335)]:
   if kind=='Gem' and i==0:
    d.text((x+10,y+50),'No upgrade gem',font=font,fill='#8b99a7');continue
   im=Image.open(HERE/'sprites'/f'{name}{kind}.png')
   board.paste(im.resize((128,128),Image.Resampling.NEAREST),(x+10,y),im.resize((128,128),Image.Resampling.NEAREST))
   d.line((x+7,y+129,x+141,y+129),fill='#5b6b74')
   board.paste(im,(x+10,y+145),im);d.text((x+35,y+145),'1x',font=font,fill='#8b99a7')
 d.text((20,535),'Front/down review only. Palette identity verified against native material sprites; candidate geometry is original.',font=font,fill='#bbc5ce')
 board.save(HERE/'tier-lineup.png')
 (HERE/'measurements.json').write_text(json.dumps(stats,indent=2)+'\n')
 (HERE/'themes.json').write_text(json.dumps(THEMES,indent=2)+'\n')
 print(HERE/'tier-lineup.png')
if __name__=='__main__':main()
