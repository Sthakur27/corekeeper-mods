"""Original Ender Stash direction study; no screenshots or game sprites used as assets.
Reference: official Steam cozy-base screenshot, viewed in browser.
Pillow only. Logical canvas 320x180; nearest-neighbor enlargement to 1280x720.
"""
from PIL import Image, ImageDraw
from pathlib import Path
import random, math
O=Path(__file__).resolve().parent
R=random.Random(891)
im=Image.new('RGB',(320,180),'#090b13');d=ImageDraw.Draw(im)
# Uneven excavated cavern silhouette.
outline=[(34,54),(55,54),(55,43),(114,43),(114,49),(175,49),(175,39),(244,39),(244,47),(281,47),(281,69),(292,69),(292,144),(278,144),(278,158),(239,158),(239,166),(183,166),(183,158),(96,158),(96,165),(43,165),(43,148),(29,148),(29,88),(34,88)]
d.polygon(outline,fill='#4b3b34')
mask=Image.new('1',im.size);ImageDraw.Draw(mask).polygon(outline,fill=1)
for y in range(43,169):
 for x in range(29,293):
  if mask.getpixel((x,y)):
   if R.random()<.19:d.point((x,y),fill=R.choice(['#554238','#614a3a','#3e3331','#72513b']))
# Rock shelves surround a squared-off storage room.
for x in range(41,281,13):
 y=R.choice([48,49,50]);w=R.randint(11,15)
 d.rectangle((x,y,x+w,y+19),fill='#342d2e')
 d.polygon([(x,y),(x+3,y-4),(x+w-2,y-4),(x+w,y),(x+w,y+7),(x+2,y+7)],fill='#665343')
 d.line((x+3,y-3,x+w-3,y-3),fill='#846549')
 d.line((x+2,y+9,x+2,y+17),fill='#72523a');d.rectangle((x+w-2,y+7,x+w,y+19),fill='#25242a')
for x in [41,269]:
 for y in range(67,144,13):
  d.rectangle((x,y,x+10,y+12),fill='#4a3c36');d.line((x,y,x+8,y),fill='#77614a');d.line((x+9,y+1,x+9,y+12),fill='#252329')
# Wooden plank floor, joins, grain and nail holes.
d.rectangle((53,68,267,146),fill='#805337')
for y in range(69,147,8):
 d.line((53,y,267,y),fill='#4f382e')
 for x in range(53+(16 if (y//8)%2 else 0),268,32):
  d.line((x,y,x,y+7),fill='#50372b')
  d.point((x+2,y+2),fill='#3e302a');d.point((x+29,y+6),fill='#3e302a')
 for _ in range(14):
  x=R.randrange(54,257);yy=y+R.randrange(2,7)
  d.line((x,yy,min(265,x+R.randrange(2,8)),yy),fill=R.choice(['#98613c','#8c5a38','#70472f']))
# Carpet, edged with small motifs; a room furnishing, not a floating panel.
d.rectangle((126,96,217,137),fill='#37253d');d.rectangle((128,98,215,135),fill='#865069');d.rectangle((132,102,211,131),fill='#593c53')
for x in range(135,211,8):
 d.rectangle((x,99,x+2,100),fill='#c08e73');d.rectangle((x,133,x+2,134),fill='#c08e73')
for y in range(106,132,8):
 for x in range(139,209,10):
  d.line((x-2,y,x,y-2,x+2,y,x,y+2,x-2,y),fill='#765069')
# Wooden rack stocked with bottles, books and rolled cloth.
def rack(x,y):
 d.rectangle((x-1,y-1,x+55,y+31),fill='#241e23');d.rectangle((x,y,x+54,y+30),fill='#65432d')
 for yy in [y+2,y+16]:
  d.rectangle((x+3,yy,x+51,yy+10),fill='#302b29')
  for xx in range(x+5,x+49,7):
   c=R.choice(['#759291','#965853','#ac8b58','#738357']);h=R.randint(5,8)
   d.rectangle((xx,yy+10-h,xx+4,yy+9),fill=c);d.line((xx,yy+11-h,xx,yy+8),fill='#b1a687')
  d.rectangle((x+1,yy+11,x+53,yy+13),fill='#956441');d.line((x+2,yy+11,x+52,yy+11),fill='#b58050')
 for xx in [x,x+52]:d.rectangle((xx,y,xx+2,y+30),fill='#ad7748')
rack(62,65)
# Simple crates, barrels, table and loose supplies.
def crate(x,y):
 d.rectangle((x,y,x+16,y+13),fill='#9a683b');d.rectangle((x+1,y+3,x+15,y+12),fill='#70472d')
 for yy in [y+4,y+8]:d.line((x+2,yy,x+14,yy),fill='#ac7541')
 d.line((x+2,y+2,x+14,y+11),fill='#c18b4e',width=2);d.line((x,y+13,x+16,y+13),fill='#33272a')
crate(60,126);crate(78,126)
for x in [236,252]:
 d.ellipse((x,71,x+12,77),fill='#9f7849');d.rectangle((x,75,x+12,89),fill='#785337');d.ellipse((x,85,x+12,92),fill='#684831')
 for xx in [x+3,x+7,x+10]:d.line((xx,77,xx,89),fill='#a47543')
 for yy in [78,87]:d.line((x,yy,x+12,yy),fill='#444b50')
d.rectangle((231,114,257,128),fill='#38272b');d.rectangle((229,108,259,123),fill='#98623a');d.line((230,109,258,109),fill='#c1884e')
# Open map, a candle and gem on table.
d.polygon([(235,112),(244,111),(251,113),(250,120),(242,118),(235,119)],fill='#d5bd7c');d.line((240,113,240,117),fill='#987647');d.line((245,114,248,116),fill='#987647')
d.rectangle((232,108,234,114),fill='#ddc891');d.point((233,106),fill='#fff3a7');d.polygon([(254,112),(257,115),(255,117),(252,115)],fill='#5bb6bc')
# Earthen foreground, pebbles and weeds outside room.
for _ in range(45):
 x=R.randrange(38,280);y=R.randrange(149,162)
 d.line((x,y,x+R.choice([1,2,3]),y),fill=R.choice(['#766043','#292831','#544234']))
for x,y in [(33,103),(35,137),(282,112),(273,154),(110,155),(49,160)]:
 d.line((x,y,x,y-5),fill='#6a7550');d.line((x,y-1,x-3,y-4),fill='#445637');d.line((x,y-2,x+3,y-6),fill='#828559')
# Original small explorer, viewed facing toward the chest.
pal={'.':None,'k':'#30232b','h':'#795442','H':'#b07742','s':'#e4b778','S':'#b57c55','t':'#65a5a1','T':'#397374','b':'#715148','B':'#423a42','g':'#d2a35d'}
rows=['....kkkk....','...kHHHHk...','..kHHhhHHk..','..kHhhhhhHk.','..khhhhhhhk.','...kSSSSSk..','...kSttSk...','..kttttttk..','.ksttttttsk.','.kStTggTtSk.','..ktTggTtk..','...TbbbbT...','...kbbbbk...','...kbkkbk...','...kB..Bk...','..kkB..Bkk..']
def spr(rows,pal,x,y):
 for yy,row in enumerate(rows):
  for xx,c in enumerate(row):
   if pal.get(c):d.point((x+xx,y+yy),fill=pal[c])
d.ellipse((156,134,173,139),fill='#302530');spr(rows,pal,159,119)
# Central chest: raised lid with thick rim, violet interior and silver hinges.
d.ellipse((173,111,207,118),fill='#33223b')
d.rectangle((173,91,205,112),fill='#242237');d.rectangle((175,96,203,111),fill='#453655')
d.rectangle((175,98,203,101),fill='#796388');d.rectangle((178,103,201,109),fill='#3a2d49')
for x in [175,201]:
 d.rectangle((x,94,x+2,111),fill='#9a91ab');d.point((x,94),fill='#e5d5da')
d.rectangle((185,99,192,105),fill='#aaa1c0');d.rectangle((187,100,190,104),fill='#dcadff');d.point((188,101),fill='#fff0fd')
# Open lid behind the box.
d.polygon([(173,93),(173,80),(177,77),(201,77),(205,81),(205,93)],fill='#292239')
d.rectangle((175,80,203,91),fill='#766581');d.rectangle((178,81,200,89),fill='#422d56');d.line((177,79,201,79),fill='#b7a2c5')
d.rectangle((176,91,202,96),fill='#cca0ed');d.rectangle((178,92,200,94),fill='#f0cbff')
for x,y in [(181,84),(196,84),(187,86)]:d.point((x,y),fill='#9c75c3')
# Wall torches: keep them small, their lighting is local.
for x,y in [(124,73),(260,98),(65,113)]:
 d.rectangle((x-1,y+3,x+1,y+8),fill='#56372b');d.rectangle((x-2,y+2,x+2,y+3),fill='#9a7954')
 d.polygon([(x-2,y+1),(x-3,y-2),(x,y-7),(x+1,y-3),(x+3,y-1),(x+1,y+2)],fill='#d66c31');d.rectangle((x-1,y-3,x+1,y+1),fill='#ffc265');d.point((x,y-2),fill='#ffeb9c')
# Pixel-quantized pools of light. No blur: six discrete levels per local source.
pix=im.load()
for y in range(43,180):
 for x in range(320):
  if not mask.getpixel((x,y)):continue
  lights=[(124,71,58,(1,.70,.40)),(261,96,48,(1,.72,.44)),(65,111,41,(1,.64,.34)),(190,95,62,(.72,.48,1)),(165,121,27,(.65,.61,.48))]
  l=[.43,.42,.47]
  for cx,cy,rad,col in lights:
   fac=max(0,1-math.hypot(x-cx,(y-cy)*1.12)/rad);fac=round(fac*5)/5
   for c in range(3):l[c]+=fac*col[c]*.9
  rgb=pix[x,y];pix[x,y]=tuple(min(255,int(rgb[c]*min(1.38,l[c]))) for c in range(3))
# Tiny motes above chest, no oversized magic symbols.
for x,y in [(183,73),(194,67),(207,82),(178,88),(201,70)]:d.point((x,y),fill='#e6baff')
# Hand-drawn 5x7 pixel alphabet, consistent with the artwork grid.
glyphs={
'E':['11111','10000','10000','11110','10000','10000','11111'],
'N':['10001','11001','11001','10101','10011','10011','10001'],
'D':['11110','10001','10001','10001','10001','10001','11110'],
'R':['11110','10001','10001','11110','10100','10010','10001'],
'S':['01111','10000','10000','01110','00001','00001','11110'],
'T':['11111','00100','00100','00100','00100','00100','00100'],
'A':['01110','10001','10001','11111','10001','10001','10001'],
'H':['10001','10001','10001','11111','10001','10001','10001'],
' ':['000']*7}
def label(text,x,y,scale=2):
 for ch in text:
  rows=glyphs[ch]
  for yy,row in enumerate(rows):
   for xx,v in enumerate(row):
    if v=='1':d.rectangle((x+xx*scale+1,y+yy*scale+2,x+(xx+1)*scale,y+(yy+1)*scale+1),fill='#302330')
  for yy,row in enumerate(rows):
   for xx,v in enumerate(row):
    if v=='1':d.rectangle((x+xx*scale,y+yy*scale,x+(xx+1)*scale-1,y+(yy+1)*scale-1),fill='#e8d6ae' if yy<3 else '#bda587')
  x+=(len(rows[0])+1)*scale
label('ENDER STASH',22,16)
# All final output pixels follow the same 4x grid.
p=O/'enderstash_native_sample.png';im.resize((1280,720),Image.Resampling.NEAREST).save(p,optimize=True)
im.save(O/'enderstash_native_thumbnail.png')
print(p)
