"""Core Archive: original exact-pixel redraws of the approved concept silhouettes.
Editable pixel maps, not scaled-down concept images. Terminal and all storage tiers.
"""
from pathlib import Path
from PIL import Image, ImageDraw
OUT=Path(__file__).parent/'Art'
P={'.':(0,0,0,0),'#':'#253137','s':'#354953','m':'#536c79','l':'#8298a0','h':'#b2c2bb',
   'r':'#663e32','c':'#a45d3b','o':'#d48b53','y':'#f2c285','d':'#173f48','t':'#287c83','a':'#51c8c1','w':'#b2f2d9'}
ART={
'Terminal':[
'................','....########....','...#ylmmmloy#...','..#yolhhhlooc#..',
'..#ocddddddcc#..','..#ocdtaatdcc#..','..#ocdatwtdcc#..','..#ocdtwatdcc#..',
'.#locdtaatdccms#','.#locddddddccms#','.#yocmmmmmmocms#','.#oclhyhyhyocms#',
'.#ocrccccccrcms#','..#lmmmmmmmms#..','..#msstastssm#..','..#ssmdtdmsss#..',
'..#lm######ms#..','..#oc#....#rc#..','..#ss#....#ss#..','...##......##...'],
'Terminal_side':[
'................','.....######.....','....#ylmmmms#...','...#yolhhmmms#..',
'...#oclmhmmms#..','..#yocdmhmmms#..','..#ocadmhmhms#..','..#ocatmhmhms#..',
'.#locdtmhmhms#..','.#yocdtmhmhms#..','.#ocddlmhmhms#..','.#ocylmmhmhms#..',
'.#ocrcmmmmmms#..','..#lmmcccccms#..','..#msmcmmmcms#..','..#ssmcssscms#..',
'..#lm######ms#..','..#oc#....#rc#..','..#ss#....#ss#..','...##......##...'],
'Terminal_up':[
'................','....########....','...#ylmmmloy#...','..#yolhhhlooc#..',
'..#ocllllllcc#..','..#ocmmmmmmcc#..','..#ocmssmsmcc#..','..#ocmlmmsmcc#..',
'.#locmssmsmccms#','.#locmmmmmmccms#','.#yocllllllocms#','.#oclmmmmmmocms#',
'.#ocrccccccrcms#','..#lmmmmmmmms#..','..#mscccccsm#..','..#ssctatcsss#..',
'..#lm######ms#..','..#oc#....#rc#..','..#ss#....#ss#..','...##......##...'],
'CopperChest':[
'................','....########....','..##yolllloy##..','.#myolhwlllocm#.',
'.#locmlwalmmccs#','.#ocmmmtmmmmlcs#','.#ycllllllllocs#','.#ocmmmmmmmccss#',
'.#ocssataasscss#','.#ocsdmdmdsccss#','.#ocsdmdmdsccss#','.#rcsdmdmdsccss#',
'.#lcmsssssmmrms#','..#lmmmmmmmsss#.','...##########...','................'],
'CopperChest_side':[
'................','....########....','..##yolllloy##..','.#myolhwlllocm#.',
'.#locmlwalmmccs#','.#ocmmmtmmmmlcs#','.#ycllllllllocs#','.#ocmmmmmmmccss#',
'.#ocssmmmmsccss#','.#ocsmmmmmsccss#','.#ocsmssssmccss#','.#rcsmssssmccss#',
'.#lcmsssssmmrms#','..#lmmmmmmmsss#.','...##########...','................'],
'CopperChest_up':[
'................','....########....','..##yolllloy##..','.#myolhwlllocm#.',
'.#locmlwalmmccs#','.#ocmmmtmmmmlcs#','.#ycllllllllocs#','.#ocmmmmmmmccss#',
'.#ocsslllssccss#','.#ocsmdtdmsccss#','.#ocsmdadmsccss#','.#rcsmsssmsc css#'.replace(' ',''),
'.#lcmsssssmmrms#','..#lmmmmmmmsss#.','...##########...','................']}
from make_unit_art import TIERS

def render(name):
 palette=P.copy()
 source=name
 for index,(tier,ramp) in enumerate(TIERS):
  if name.startswith(tier+'Chest'):
   source=name.replace(tier+'Chest','CopperChest')
   if index:
    for key,color in zip('rcoy',ramp):palette[key]='#'+color
   break
 rows=ART[source]; size=(16,20 if name.startswith('Terminal') else 16)
 im=Image.new('RGBA',size); glow=Image.new('RGBA',size)
 for y,row in enumerate(rows):
  assert len(row)<=16,(name,y,len(row))
  for x,key in enumerate(row):
   color=palette[key]
   if isinstance(color,str):color=tuple(bytes.fromhex(color[1:]))+(255,)
   im.putpixel((x,y),color)
   if key in 'aw':glow.putpixel((x,y),(42,100,93,255))
 return im,glow
def main():
 names=['Terminal'+suffix for suffix in ('','_side','_up')]
 names += [tier+'Chest'+suffix for tier,_ in TIERS for suffix in ('','_side','_up')]
 for name in names:
  im,glow=render(name);im.save(OUT/(name+'.png'));glow.save(OUT/(name+'_emissive.png'))
 preview=Image.new('RGBA',(640,390),'#20292e');d=ImageDraw.Draw(preview)
 d.text((20,12),'CORE ARCHIVE / runtime sprites - front, side, rear',fill='#eee2c5')
 for row,name in enumerate(('Terminal','CopperChest')):
  d.text((20,40+row*175),name,fill='#eee2c5')
  for col,suffix in enumerate(('','_side','_up')):
   im,_=render(name+suffix)
   preview.alpha_composite(im.resize((96,im.height*6),Image.Resampling.NEAREST),(25+col*170,62+row*175))
   preview.alpha_composite(im,(135+col*170,135+row*175))
 dest=Path(__file__).parents[1]/'ArtConcepts/CoreArchive';dest.mkdir(parents=True,exist_ok=True)
 preview.save(dest/'runtime-sprites.png')
 sheet=Image.new('RGBA',(960,400),'#20292e');draw=ImageDraw.Draw(sheet)
 draw.text((20,12),'CORE ARCHIVE / all storage tiers - front, side, rear',fill='#eee2c5')
 for col,(tier,_) in enumerate(TIERS):
  draw.text((col*120+12,40),tier,fill='#eee2c5')
  for row,suffix in enumerate(('','_side','_up')):
   im,_=render(tier+'Chest'+suffix)
   sheet.alpha_composite(im.resize((96,96),Image.Resampling.NEAREST),(col*120+12,65+row*105))
 sheet.save(dest/'all-tiers.png')
if __name__=='__main__':main()
