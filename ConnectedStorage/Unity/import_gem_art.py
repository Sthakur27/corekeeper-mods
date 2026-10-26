"""Preserve generated gem pixels; only crop padding and centre in equal-size canvases."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).parent
TIERS=('Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium')
def main():
 sheet=Image.new('RGBA',(1050,220),'#272b33');draw=ImageDraw.Draw(sheet)
 for n,tier in enumerate(TIERS):
  source=ROOT.parent/'ArtConcepts/UpgradeGems'/(tier+'.png')
  im=Image.open(source).convert('RGBA')
  crop=im.crop(im.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox())
  edge=max(crop.size);size=edge+2*round(edge/14)
  canvas=Image.new('RGBA',(size,size));x=(size-crop.width)//2;y=(size-crop.height)//2
  canvas.paste(crop,(x,y))
  assert canvas.crop((x,y,x+crop.width,y+crop.height)).tobytes()==crop.tobytes()
  for suffix in ('','_side','_up'):
   canvas.save(ROOT/'Art'/(tier+'Gem'+suffix+'.png'))
   Image.new('RGBA',canvas.size).save(ROOT/'Art'/(tier+'Gem'+suffix+'_emissive.png'))
  draw.text((n*150+10,10),tier,fill='white')
  icon=canvas.copy();icon.thumbnail((128,128),Image.Resampling.NEAREST)
  sheet.alpha_composite(icon,(n*150+10,32))
  icon=canvas.copy();icon.thumbnail((40,40),Image.Resampling.NEAREST)
  sheet.alpha_composite(icon,(n*150+54,170))
 sheet.save(ROOT.parent/'ArtConcepts/UpgradeGems/preview.png')
if __name__=='__main__':main()
