"""Runic Kiln: original exact-pixel redraws of the approved concept silhouettes.
Editable pixel maps, not scaled-down concept images. Terminal and all storage tiers.
"""
from pathlib import Path
from PIL import Image, ImageDraw
OUT=Path(__file__).parent/'Art'
P={'.':(0,0,0,0),'#':'#272327','s':'#73392e','m':'#ac5334','l':'#db8950','h':'#f6bb77',
   'r':'#303039','c':'#4a4548','o':'#77716b','y':'#b4a48a','d':'#503026','t':'#af5b16','a':'#f6a923','w':'#fff0a0'}
# Front elevation: centred silhouette, horizontal rim and base, no visible side wall.
def centred(rows, height):
 assert len(rows)==height
 return [row.center(16,'.') for row in rows]
unit=centred([
 '', '########', '#hlllllm#', '#hlloodllm#', '#hllododllm#',
 '#yoooooooooc#', '#cmmmmmmmmmc#', '#cddmddmddmc#',
 '#cdwmdwmdwmc#', '#cdamdamdamc#', '#cdtmdtmdtmc#',
 '#cmmmmmmmmmc#', '#yoooooooooc#', '#rccccccccrr#', '############', ''
],16)
terminal=centred([
 '', '########', '#hlllllm#', '#loooooc#', '#ocddddco#',
 '#octaatco#', '#ocaw atco#'.replace(' ',''), '#octwatco#', '#octaatco#',
 '#ocddddco#', '#lcccccm#', '#hlllllllllm#', '#loyomoyom#',
 '#lccmmccmm#', '#lmmmmmmms#', '#lmmdamms#', '#yoooooooc#',
 '#rr#..#rr#', '####..####', ''
],20)
ART={'Terminal':terminal,'CopperChest':unit}
# Side/rear facings retain the same centred footprint; only the face details change.
for suffix,face in [('_side',['#cmmmmmmmmmc#','#cmmoyocmmmc#','#cmmcdacmmmc#','#cmmcccccmmc#']),
                    ('_up',['#cmmmmmmmmmc#','#cmmccccmmmc#','#cmmctacmmmc#','#cmmccccmmmc#'])]:
 rows=unit.copy()
 for y,row in enumerate(face,7): rows[y]=row.center(16,'.')
 ART['CopperChest'+suffix]=rows
for suffix in ('_side','_up'):
 rows=terminal.copy()
 face=['#ocmmmmco#','#ocmccmco#','#ocmcamco#','#ocmccmco#','#ocmmmmco#'] if suffix=='_side' else ['#ocmmmmco#','#ocmccmco#','#ocmccmco#','#ocmccmco#','#ocmmmmco#']
 for y,row in enumerate(face,5): rows[y]=row.center(16,'.')
 ART['Terminal'+suffix]=rows
from make_unit_art import TIERS
# Body shadow/base/light/highlight; metal shadow/base/light/highlight; glow shade/base/hotspot.
PALETTES={
 'Terminal':('787568 b6b6a0 e3dfbd fff4d5','3d3840 65574e a68a5f e5c68d','206356 52d6ad c8ffe4'),
 'Copper':('73392e ac5334 db8950 f6bb77','29272e 4a4548 77716b b4a48a','af5b16 f6a923 fff0a0'),
 'Tin':('475968 7e99a8 b7d1d7 e9f5ee','303e49 4b6574 74959e c1d9d6','28647c 70d4e5 d4ffff'),
 'Iron':('252b3b 424d63 65758c 98a8b7','24242b 565862 9399a0 d0d6d6','8c4d26 eaa160 ffe3b3'),
 'Scarlet':('591e35 a32f4d e65765 ffaaa0','302731 60414e 9c6772 dba3a2','a7393e ff8c82 ffe0c1'),
 'Octarine':('154a4b 207f7a 43b9a1 a0edc6','20383c 3f6467 80a7a0 d0ded0','166b67 6af5c4 dbffe4'),
 'Galaxite':('3f255c 744393 b37ac6 e8b4ed','302737 68516b aa88a5 e4c5d6','78377d e790ee ffe0ff'),
 'Solarite':('815423 c08c2c f0c64b fff0a1','433330 766052 b69b77 e8d5aa','ba6d10 ffcb43 fff7c5'),
 'Pandorium':('202d66 3a54b5 7398eb bcd8ff','292a40 505377 8b91b8 cfd8ed','3c5baa 8fcaff e7faff')
}
def render(name):
 palette=P.copy();source=name;kind='Terminal'
 for tier,_ in TIERS:
  if name.startswith(tier+'Chest'):
   source=name.replace(tier+'Chest','CopperChest');kind=tier;break
 body,metal,light=PALETTES[kind]
 for keys,colors in [('smlh',body),('rcoy',metal),('taw',light)]:
  for key,color in zip(keys,colors.split()):palette[key]='#'+color
 palette['d']=palette['s']
 rows=ART[source];size=(16,20 if kind=='Terminal' else 16)
 im=Image.new('RGBA',size);glow=Image.new('RGBA',size)
 emission=tuple(int(c*.6) for c in bytes.fromhex(palette['a'][1:]))+(255,)
 for y,row in enumerate(rows):
  assert len(row)==16,(name,y,len(row))
  for x,key in enumerate(row):
   color=palette[key]
   if isinstance(color,str):color=tuple(bytes.fromhex(color[1:]))+(255,)
   im.putpixel((x,y),color)
   if key in 'aw':glow.putpixel((x,y),emission)
 return im,glow
def main():
 names=['Terminal'+suffix for suffix in ('','_side','_up')]
 names += [tier+'Chest'+suffix for tier,_ in TIERS for suffix in ('','_side','_up')]
 for name in names:
  im,glow=render(name);im.save(OUT/(name+'.png'));glow.save(OUT/(name+'_emissive.png'))
 preview=Image.new('RGBA',(640,390),'#20292e');d=ImageDraw.Draw(preview)
 d.text((20,12),'RUNIC KILN / runtime sprites - front, side, rear',fill='#eee2c5')
 for row,name in enumerate(('Terminal','CopperChest')):
  d.text((20,40+row*175),name,fill='#eee2c5')
  for col,suffix in enumerate(('','_side','_up')):
   im,_=render(name+suffix)
   preview.alpha_composite(im.resize((96,im.height*6),Image.Resampling.NEAREST),(25+col*170,62+row*175))
   preview.alpha_composite(im,(135+col*170,135+row*175))
 dest=Path(__file__).parents[1]/'ArtConcepts/RunicKiln';dest.mkdir(parents=True,exist_ok=True)
 preview.save(dest/'runtime-sprites.png')
 sheet=Image.new('RGBA',(960,400),'#20292e');draw=ImageDraw.Draw(sheet)
 draw.text((20,12),'RUNIC KILN / all storage tiers - front, side, rear',fill='#eee2c5')
 for col,(tier,_) in enumerate(TIERS):
  draw.text((col*120+12,40),tier,fill='#eee2c5')
  for row,suffix in enumerate(('','_side','_up')):
   im,_=render(tier+'Chest'+suffix)
   sheet.alpha_composite(im.resize((96,96),Image.Resampling.NEAREST),(col*120+12,65+row*105))
 sheet.save(dest/'all-tiers.png')
if __name__=='__main__':main()
