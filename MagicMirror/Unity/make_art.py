from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
ART=Path(__file__).resolve().parent/'Art'
ART.mkdir(exist_ok=True)
# Original 16x24 sprite, hand-placed pixels; no borrowed game textures.
rows=[
'................','......kkkk......','....kkhhhHkk....','...khHHHHHhhk...',
'..khHbcccbHhhk..','..kHbcwwwccbHk..','.khbcwwcccccbhk.',
'.kHbwwcccccbbHk.','.kHbcwccccbbbHk.','.kHbccccbbbbbHk.',
'.kHbcccbbbbbbHk.','.kHbcccbbppbbHk.','.khbbbbbppppbhk.',
'..kHbbbbppbbHk..','..khHbbbbbbHhk..','...khHHHHHhhk...',
'....kkhHhhkk....','......kHhk......','......kHhk......',
'.....khHHhk.....','.....khHHhk.....','......khhk......','.......kk.......','................']
# The rim is shaded gold, not a dark ink outline. Cyan glass and warm highlights
# sit beside Solarite equipment without the previous purple/black contour.
p={'k':'#c58b40','h':'#edbb59','H':'#fff0a6','b':'#368cbe','c':'#64d8e3','w':'#d9fff6','p':'#659fd0'}
im=Image.new('RGBA',(16,24))
for y,row in enumerate(rows):
 for x,c in enumerate(row):
  if c in p: im.putpixel((x,y),tuple(bytes.fromhex(p[c][1:]))+(255,))
im.save(ART/'MagicMirror_art.png')
# Inventory slots render sprites at their native size; use a separate 16x16
# drawing rather than overflowing the slot with the larger cover illustration.
icon_rows=[
'......kkkk......','.....khHHhk.....','....kHbccbHk....','...kHbwwccbHk...',
'...kHbwcccbHk...','...kHbccbbbHk...','...kHbcbbpbHk...','...kHbbbppbHk...',
'....kHbbbbHk....','.....khHHhk.....','......kHhk......','......kHhk......',
'.....khHHhk.....','......khhk......','.......kk.......','................']
icon=Image.new('RGBA',(16,16))
for y,row in enumerate(icon_rows):
 for x,c in enumerate(row):
  if c in p:icon.putpixel((x,y),tuple(bytes.fromhex(p[c][1:]))+(255,))
icon.save(ART/'MagicMirror_icon.png')
inventory_preview=Image.new('RGBA',(320,320),'#121827')
inventory_preview.alpha_composite(icon.resize((240,240),Image.Resampling.NEAREST),(40,40))
inventory_preview.save(ART/'MagicMirror_inventory_preview.png')
preview=Image.new('RGBA',(320,320),'#121827'); preview.alpha_composite(im.resize((160,240),Image.Resampling.NEAREST),(80,40)); preview.convert('RGB').save(ART/'MagicMirror_preview.png')
# Original listing art: mirror suspended over a subterranean return portal.
cover=Image.new('RGB',(960,540),'#121827'); d=ImageDraw.Draw(cover)
for y in range(360,540,16):
 for x in range(-16,960,48):
  offset=24 if (y//16)%2 else 0
  d.rectangle((x+offset,y,x+offset+44,y+12), fill=('#23263d' if (x+y)%3 else '#2c2d43'))
for r in range(110,10,-8): d.ellipse((265-r,434-r//3,265+r,434+r//3),outline='#38a3bb',width=3)
cover.paste(im.resize((224,336),Image.Resampling.NEAREST),(153,70),im.resize((224,336),Image.Resampling.NEAREST))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',n)
d.text((455,150),'MAGIC',font=font(57),fill='#fff0c7'); d.text((455,212),'MIRROR',font=font(57),fill='#fff0c7')
d.text((458,295),'One craft. Every journey home.',font=font(23),fill='#8cddd9')
d.text((458,337),'50 SOLARITE BARS',font=font(22),fill='#d9b66e')
cover.save(ART/'MagicMirror_cover_reference.png')
