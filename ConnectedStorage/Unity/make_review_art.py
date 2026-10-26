"""Native-size pixel cleanup of the approved copper/ivory Runic Kiln prototypes.
Only these two objects; palette keys separate ceramic, hardware, patina and light.
"""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).parent
P={'.':(0,0,0,0),'s':'63352b','m':'be643e','l':'ee916a','h':'ffb697',
   'd':'3c3030','c':'57535a','o':'8a8891','y':'c1c7d6','p':'36775a','q':'59a87b',
   't':'9b4a17','a':'ee981c','w':'ffe18c','b':'76512f','g':'b78643','f':'f3cd7d'}
def rows(lines,height):
 assert len(lines)==height
 assert all(len(r)<=16 for r in lines)
 return [r.center(16,'.') for r in lines]
COPPER=rows([
 '', 'coss sss soc'.replace(' ',''), 'cylhhhhhlloyc', 'omlhmmmlhlmo',
 'clhlmhhmlllc', 'yolllllllmoy', 'coooooooooc', 'cdssssssssd c'.replace(' ',''),
 'oylhllllllqyc','cmlwdwdwmlpqc','cmladadammpmc','csmtdtdtmmsmc',
 'cosmmmmmmssoc','bgcsssssscbg','byc......cyb','dd........dd'
],16)
TERMINAL=rows([
 '', 'coooooooooc', 'cylhhhhhhloyc', 'olhlllllllhmo', 'clllmhhmlllc',
 'yolllllllmoy','coooogfoooc','cdmmoyyomm dc'.replace(' ',''),'oymlcwaclmoy',
 'cmlocawacolmc','cmlocata colmc'.replace(' ',''),'cmlmocccmlmc','cmlmmbbmmmlc',
 'cmlgfmlfgmlc','cmlgbmlbgmlc','csmmbmmbmmsc','cosmmmmmm soc'.replace(' ',''),
 'bgcsssssscbg','byc......cyb','dd........dd'
],20)
def render(name,suffix=''):
 terminal=name=='Terminal';palette=P.copy();data=(TERMINAL if terminal else COPPER).copy()
 if terminal:
  palette.update(s='80715a',m='c9b58e',l='ebdbb9',h='fff2d9',t='21705b',a='59cba2',w='b9ffe0',p='938367',q='bca784')
 if suffix:
  start=8
  faces=(['cmlmmccmmmlc','cmlmcoocmmlc','cmlmctacmmlc','csmmccccmmsc'] if suffix=='_side' else
         ['cmlmmmmmmmlc','cmlmccccmmlc','cmlmcmmcmmlc','csmmccccmmsc'])
  for n,r in enumerate(faces):data[start+n]=r.center(16,'.')
 im=Image.new('RGBA',(16,20 if terminal else 16));glow=Image.new('RGBA',im.size)
 for y,row in enumerate(data):
  for x,k in enumerate(row):
   v=palette[k];color=v if isinstance(v,tuple) else tuple(bytes.fromhex(v))+(255,)
   im.putpixel((x,y),color)
   if k in 'aw':glow.putpixel((x,y),tuple(int(c*.6) for c in color[:3])+(255,))
 return im,glow
def main():
 for name in ('CopperChest','Terminal'):
  for suffix in ('','_side','_up'):
   im,glow=render(name,suffix)
   im.save(ROOT/'Art'/(name+suffix+'.png'));glow.save(ROOT/'Art'/(name+suffix+'_emissive.png'))
 # Private native-reference comparison, never packaged or published.
 sheet=Image.new('RGBA',(720,250),'#272b33');draw=ImageDraw.Draw(sheet)
 refs=ROOT.parents[1]/'build/connectedstorage/art-reference'
 if not (refs/'chestCopper.png').exists():return
 entries=[('Native copper',Image.open(refs/'chestCopper.png')),('Copper node',render('CopperChest')[0]),
          ('Native workbench',Image.open(refs/'workbenchCopper_down.png')),('Terminal',render('Terminal')[0])]
 for i,(label,im) in enumerate(entries):
  x=i*180+14;draw.text((x,10),label,fill='white');im=im.convert('RGBA')
  sheet.alpha_composite(im.resize((im.width*7,im.height*7),Image.Resampling.NEAREST),(x,40))
  sheet.alpha_composite(im,(x,205))
 sheet.save(refs/'review-runtime-comparison.png')
if __name__=='__main__':main()
