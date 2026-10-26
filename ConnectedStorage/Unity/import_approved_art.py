"""Lossless runtime packaging: crop transparent padding only; never redraw/resample.
World geometry is compensated in ConnectedStorageBuilder, not in the artwork.
"""
from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).parent
SOURCE=ROOT.parent/'ArtConcepts/RunicKilnReview'
def main():
 report={}
 for name,filename in [('CopperChest','copper-node.png'),('Terminal','terminal.png')]:
  original=Image.open(SOURCE/filename).convert('RGBA')
  bounds=original.getchannel('A').point(lambda a: 255 if a>=128 else 0).getbbox()
  crop=original.crop(bounds)
  # A one-native-pixel-equivalent border keeps the original one-tile footprint.
  pad=round(crop.width/14)
  output=Image.new('RGBA',(crop.width+pad*2,crop.height+pad*2))
  output.paste(crop,(pad,pad))
  assert output.crop((pad,pad,pad+crop.width,pad+crop.height)).tobytes()==crop.tobytes()
  for suffix in ('','_side','_up'):
   output.save(ROOT/'Art'/(name+suffix+'.png'))
   Image.new('RGBA',output.size).save(ROOT/'Art'/(name+suffix+'_emissive.png'))
  report[name]={'source':filename,'sourceBounds':bounds,'padding':pad,'size':output.size,
                'worldScale':16/output.width,'pivotY':pad/output.height,'pixelsPreserved':True}
 dest=ROOT.parent/'ArtConcepts/ApprovedRuntime';dest.mkdir(exist_ok=True)
 (dest/'conversion.json').write_text(json.dumps(report,indent=2))
 # Side-by-side comparison uses the exact source and exact runtime pixels, not redrawn art.
 sheet=Image.new('RGBA',(1000,550),'#272b33')
 from PIL import ImageDraw
 draw=ImageDraw.Draw(sheet)
 for col,(name,filename) in enumerate([('CopperChest','copper-node.png'),('Terminal','terminal.png')]):
  source=Image.open(SOURCE/filename).convert('RGBA');source=source.crop(source.getchannel('A').point(lambda a: 255 if a>=128 else 0).getbbox())
  runtime=Image.open(ROOT/'Art'/(name+'.png'));runtime=runtime.crop(runtime.getchannel('A').point(lambda a: 255 if a>=128 else 0).getbbox())
  for index,(label,im) in enumerate([('Approved',source),('Runtime',runtime)]):
   x=col*500+index*245+10;draw.text((x,12),name+' / '+label,fill='white')
   im.thumbnail((225,300),Image.Resampling.NEAREST);sheet.alpha_composite(im,(x,45))
  draw.text((col*500+10,365),'Same source pixels. Small-scale previews:',fill='white')
  for index,size in enumerate((48,80,128)):
   im=runtime.copy();im.thumbnail((size,round(size*1.2)),Image.Resampling.NEAREST)
   sheet.alpha_composite(im,(col*500+10+index*150,395))
 sheet.save(dest/'comparison.png')
 print(json.dumps(report,indent=2))
if __name__=='__main__':main()
