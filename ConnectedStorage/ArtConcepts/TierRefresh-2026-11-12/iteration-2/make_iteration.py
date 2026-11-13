"""Replace five top motifs with fitted details; lock all other pixels/assets."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
SOURCE=HERE.parent/'tiers'
AUDIT=json.loads((HERE.parent/'color-audit/native-palettes.json').read_text())['tiers']
TIERS=list(AUDIT)
CHANGED=['Copper','Tin','Scarlet','Octarine','Solarite']
LOCKED=['Iron','Galaxite','Pandorium','Relucite']
def rgba(h):return tuple(bytes.fromhex(h.lstrip('#')))+(255,)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

# Exact 6x5 patch. All colored fittings have visible seating/shadow within the
# shared stone surface. Dot=unchanged neutral top plane, not transparency.
# A top highlight, recessed center and lower lip convey construction at 16px.
PATCHES={
 'Copper':[
  '......',
  '.tttt.',
  '.dmmk.',
  '.ssss.',
  '......',
 ],
 'Tin':[
  '..tt..',
  '.tddt.',
  '.mkkd.',
  '..ss..',
  '......',
 ],
 'Scarlet':[
  '..tt..',
  '.tdmd.',
  '.dmmk.',
  '..ss..',
  '......',
 ],
 'Octarine':[
  '..tt..',
  '.tmmk.',
  '.dmbk.',
  '..bs..',
  '......',
 ],
 'Solarite':[
  '.tttt.',
  '.dwmd.',
  '.dmck.',
  '.ssss.',
  '......',
 ],
}
THEMES={
 'Copper':'Small plain copper plate, upper lip and dark lower seam; starter hardware, no glow.',
 'Tin':'Pewter socket rim around a recessed dark center, visually seated rather than two loose grooves.',
 'Scarlet':'Beveled red fitting with shaded recess and lower contact edge; first powered inset.',
 'Octarine':'Compact rose-purple lens with cobalt lower bevel, seated in a dark oval fitting.',
 'Solarite':'Golden framed inset with one pale highlight and a cool inner glint; compact radiant window.',
}

def revise(name):
 im=Image.open(SOURCE/f'{name}Chest-16.png').convert('RGBA')
 ramp=list(map(rgba,AUDIT[name]['main_dark_to_light']))
 ac=list(map(rgba,AUDIT[name]['accent_colors']))
 pal={'.':rgba('#8a9fbe'),'s':rgba('#4c576e'),
      'k':rgba('#35444e'),'d':ramp[0],'m':ramp[1],
      't':ramp[2],'b':ramp[0],'w':ramp[3],'c':ramp[2]}
 if name=='Tin':pal['t']=ramp[3];pal['m']=ramp[2]
 if name=='Scarlet':pal['m']=ramp[2];pal['t']=ramp[3]
 if name=='Octarine':
  pal['t']=ramp[3];pal['m']=ramp[2];pal['b']=ac[2];pal['s']=ac[0]
 if name=='Solarite':
  pal['m']=ramp[2];pal['t']=ramp[3];pal['w']=ac[0];pal['c']=ac[2]
 for y,row in enumerate(PATCHES[name]):
  for x,key in enumerate(row):im.putpixel((x+5,y+3),pal[key])
 return im

def main():
 metrics={'changed_units':{},'locked_assets':{},'edit_region_inclusive':[5,3,10,7]}
 for name in TIERS:
  for kind in ['Chest']+([] if name=='Copper' else ['Gem']):
   filename=f'{name}{kind}-16.png';src=SOURCE/filename;dest=HERE/filename
   if kind=='Chest' and name in CHANGED:
    im=revise(name);before=Image.open(src).convert('RGBA')
    im.save(dest)
    edits=[(x,y) for y in range(16) for x in range(16) if before.getpixel((x,y))!=im.getpixel((x,y))]
    assert edits and all(5<=x<=10 and 3<=y<=7 for x,y in edits)
    assert im.getchannel('A').tobytes()==before.getchannel('A').tobytes()
    assert im.crop((0,8,16,16)).tobytes()==before.crop((0,8,16,16)).tobytes()
    assert im.size==(16,16) and set(im.getchannel('A').getdata())<={0,255}
    metrics['changed_units'][name]={'changed_pixels':len(edits),'coordinates':edits,
      'alpha_identical':True,'bottom_half_identical':True,
      'source_sha256':sha(src),'candidate_sha256':sha(dest)}
   else:
    shutil.copyfile(src,dest)
    assert sha(src)==sha(dest)
    metrics['locked_assets'][filename]={'byte_identical':True,'sha256':sha(dest)}
 assert len(metrics['locked_assets'])==12 # four units plus eight gems
 (HERE/'metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
 (HERE/'themes.json').write_text(json.dumps(THEMES,indent=2)+'\n')
 font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
 small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',14)
 title=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',22)
 board=Image.new('RGB',(1120,635),'#252b33');d=ImageDraw.Draw(board)
 d.text((20,12),'TOP FITTING REVISIONS | native 16 x 16',font=title,fill='#efdbb4')
 d.text((20,45),'Before above, revised below. Exact 10x nearest-neighbor + 1x. Only the central lid patch changed.',font=small,fill='#bbc5ce')
 for i,name in enumerate(CHANGED):
  x=20+i*220;d.text((x,80),name,font=font,fill='#efdbb4')
  for row,(folder,label) in enumerate([(SOURCE,'Before'),(HERE,'Revised')]):
   y=113+row*245;im=Image.open(folder/f'{name}Chest-16.png').convert('RGBA')
   big=im.resize((160,160),Image.Resampling.NEAREST)
   board.paste(big,(x+14,y),big);d.line((x+10,y+161,x+178,y+161),fill='#526871')
   board.paste(im,(x+14,y+176),im);d.text((x+40,y+176),label+' | 1x',font=small,fill='#aab8c7')
 d.text((20,605),'Iron, Galaxite, Pandorium, Relucite and every upgrade gem are byte-identical to the previous candidates.',font=small,fill='#bbc5ce')
 board.save(HERE/'before-after.png')
 line=Image.new('RGB',(1440,320),'#252b33');ld=ImageDraw.Draw(line)
 ld.text((20,12),'ITERATION 2 | complete ordered unit lineup',font=title,fill='#efdbb4')
 for i,name in enumerate(TIERS):
  x=12+i*158;ld.text((x+10,55),name,font=font,fill='#efdbb4')
  im=Image.open(HERE/f'{name}Chest-16.png').convert('RGBA');big=im.resize((128,128),Image.Resampling.NEAREST)
  line.paste(big,(x+10,90),big);line.paste(im,(x+10,233),im)
  ld.text((x+36,233),'1x',font=small,fill='#aab8c7')
  ld.text((x+10,268),'Revised top' if name in CHANGED else 'Locked',font=small,fill='#9cbdb7')
 line.save(HERE/'unit-lineup.png')
 print(json.dumps({n:v['changed_pixels'] for n,v in metrics['changed_units'].items()}))
 print('12 locked assets byte-identical; all edits in central top patch.')
if __name__=='__main__':main()
