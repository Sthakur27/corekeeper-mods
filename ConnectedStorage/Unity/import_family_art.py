"""Package generated family art without resampling its pixels."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).parent
NAMES=[t+'Chest' for t in ('Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium')]+['Workbench']
def main():
 sheet=Image.new('RGBA',(1200,250),'#272b33');draw=ImageDraw.Draw(sheet)
 for i,name in enumerate(NAMES):
  original=Image.open(ROOT.parent/'ArtConcepts/StorageFamily'/(name+'.png')).convert('RGBA')
  crop=original.crop(original.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox())
  pad=round(crop.width/14)
  canvas=Image.new('RGBA',(crop.width+2*pad,crop.height+2*pad));canvas.paste(crop,(pad,pad))
  assert canvas.crop((pad,pad,pad+crop.width,pad+crop.height)).tobytes()==crop.tobytes()
  for suffix in ('','_side','_up'):
   canvas.save(ROOT/'Art'/(name+suffix+'.png'))
   Image.new('RGBA',canvas.size).save(ROOT/'Art'/(name+suffix+'_emissive.png'))
  draw.text((i*150+8,10),name.replace('Chest',''),fill='white')
  icon=canvas.copy();icon.thumbnail((140,150),Image.Resampling.NEAREST);sheet.alpha_composite(icon,(i*150+5,35))
  icon=canvas.copy();icon.thumbnail((40,45),Image.Resampling.NEAREST);sheet.alpha_composite(icon,(i*150+55,195))
 sheet.save(ROOT.parent/'ArtConcepts/StorageFamily/preview.png')
if __name__=='__main__':main()
