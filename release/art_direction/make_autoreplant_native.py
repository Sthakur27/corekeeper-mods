"""Original pixel illustration, drawn from primitives and authored sprites.
Reference: official Core Keeper Steam cozy-base screenshot, viewed in browser.
No screenshot pixels, extracted sprites, external artwork or antialiasing used.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import random, math
R=random.Random(216)
OUT=Path(__file__).resolve().parent
im=Image.new('RGB',(320,180),'#100e16');d=ImageDraw.Draw(im)
def rect(box,c):d.rectangle(box,fill=c)
def dot(x,y,c):d.point((x,y),fill=c)
# Irregular excavated chamber, with squared off tile-scale edges.
outline=[(19,54),(37,54),(37,43),(78,43),(78,38),(250,38),(250,46),(288,46),(288,61),(303,61),(303,144),(287,144),(287,165),(265,165),(265,173),(73,173),(73,165),(33,165),(33,151),(18,151)]
d.polygon(outline,fill='#59402e')
mask=Image.new('1',im.size);md=ImageDraw.Draw(mask);md.polygon(outline,fill=1)
# Broken earth texture with small discrete flecks, not uniform noise.
for y in range(40,174):
 for x in range(18,304):
  if mask.getpixel((x,y)):
   base=R.choice(['#775238']*5+['#705035']*3+['#815b3b']*2)
   dot(x,y,base)
for _ in range(1250):
 x,y=R.randrange(19,303),R.randrange(42,174)
 if mask.getpixel((x,y)):
  c=R.choice(['#976c46','#9d744a','#5e412e','#62472f'])
  rect((x,y,x+R.choice([0,1,2]),y),c)
# Outer raised rock wall follows chamber edge; top ledge and vertical strata.
for x in range(17,304,8):
 ys=[y for y in range(36,174) if mask.getpixel((x,y))]
 if not ys:continue
 top=min(ys)
 rect((x,top-7,x+7,top+1),'#382c25');rect((x+1,top-8,x+6,top-5),'#65513a')
 rect((x+1,top-4,x+6,top+3),'#4c3b2b');rect((x+2,top-5,x+3,top),'#7d6140')
 dot(x+5,top,'#8c6740');rect((x,top+3,x+7,top+5),'#36271f')
# Small pond carved into west side.
pond=[(26,88),(32,79),(65,79),(72,86),(72,116),(63,125),(31,125),(24,117)]
d.polygon(pond,fill='#332e29');d.polygon([(30,90),(35,83),(62,83),(67,89),(67,116),(60,121),(32,121),(28,114)],fill='#243c47')
for _ in range(65):
 x,y=R.randrange(32,64),R.randrange(86,118)
 rect((x,y,x+R.randrange(1,4),y),R.choice(['#355560','#3c6268','#2a4652']))
# Farm tiles: discreet raised rims and dug furrows.
for row in range(4):
 for col in range(8):
  x,y=111+col*17,66+row*20
  rect((x,y,x+15,y+17),'#38251d');rect((x,y,x+15,y+1),'#ac7b48')
  rect((x+1,y+2,x+14,y+15),'#67452a')
  for yy in [y+5,y+10,y+15]:
   rect((x+2,yy,x+13,yy),'#3f2c20');rect((x+3,yy-1,x+12,yy-1),'#805735')
  for _ in range(9):
   px,py=x+R.randrange(1,15),y+R.randrange(3,16);dot(px,py,R.choice(['#976b41','#3c2a20','#785138']))
# Authored sprites, no borrowed game pixels.
P={'.':None,'o':'#251e1c','a':'#334726','b':'#52672a','c':'#7f9238','d':'#b9b454','e':'#d3c46b','r':'#963d2d','s':'#c65d33','t':'#ed894b','u':'#ffd277','w':'#c9c4a0','k':'#32271e','h':'#65482b','i':'#987044','j':'#c59a5d','n':'#3e5551','m':'#678780','f':'#e0a374','g':'#f5c28b','v':'#6c4139','q':'#96514a','z':'#cf7060'}
def sprite(rows,x,y):
 for dy,row in enumerate(rows):
  for dx,c in enumerate(row):
   if P.get(c):dot(x+dx,y+dy,P[c])
CROP=['.......b.......','....bbbc..bb...','...bccdc.bccb..','....bccbbccb...','..bbbccbccbbb..','.bcccdccdcccb..','.bbccccccccbb..','..bbrssrsrbb...','..orsstrtsso...','.orssttttrrso..','.orsstttrrrso..','..orsssrrrso...','...orrssrro....','....ooooo.....']
LEAF=['......b......','...bbbcbb....','..bcdccccb...','.bcddccbccb..','.bccbbccccb..','..bcccccbb...','...abbbba....','....abba.....','.....aa......']
SPROUT=['..bc....cb..','..bdc..cdb..','...bccccb...','....abba....','.....aa.....','.....ao.....']
# Crops overlap dug beds, with grounded dark shadows and varied maturity.
for row in range(4):
 for col in range(8):
  if (row,col) in [(1,2),(1,3),(2,2),(2,3)]:continue
  x,y=112+col*17,65+row*20
  d.ellipse((x+2,y+10,x+13,y+15),fill='#38291e')
  if row<2:sprite(CROP,x,y-2+R.choice([0,1]))
  elif row==2:sprite(LEAF,x+1,y+3)
  else:sprite(SPROUT,x+2,y+7)
# Harvested fruit and seed next to explorer, small enough to belong to the world.
sprite(['..bb..','.rsst.','rsuttr','rsstsr','.rrro.','..oo..'],158,95)
sprite(['.jh.','jij','hih','.h.'],153,101)
# Explorer is ~14x25 pixels with hat, face, tunic, gloves and boots.
PLAYER=['....oooo......','...ohhjjo.....','..ohhjjjjo....','.ohhjjjjjjo...','.ojjjjjujjjo..','..oooffggo....','....ofoggo....','....offggo....','....oqqqo.....','...onmmnno....','..onmmmmnfo...','..ofnmmnnffo..','..offnnnnoo...','...oonnno.....','...ovvvvo.....','...ovvovo.....','...ohhoho.....','...ojhojho....','....oo.oo.....']
d.ellipse((143,109,159,116),fill='#39281e');sprite(PLAYER,143,93)
# Hand-held hoe leaning towards harvested patch.
d.line((154,101,166,94),fill='#4a3024',width=2);d.line((155,100,166,94),fill='#bd8951');rect((164,91,169,93),'#707b76');rect((164,91,170,91),'#b4b9a0')
# Fresh seedlings where the harvest has just occurred, subtle green glints.
sprite(SPROUT,148,122);sprite(SPROUT,165,122)
for x,y in [(147,119),(177,124)]:
 dot(x,y-1,'#c7cf88');rect((x-1,y,x+1,y),'#c7cf88');dot(x,y+1,'#c7cf88')
# Raised wood path and storage corner.
for x in range(84,266,9):
 rect((x,148,x+8,157),'#453020');rect((x+1,148,x+7,155),'#8e643b');rect((x+2,149,x+7,149),'#b8894d');rect((x+2,153,x+6,153),'#a27542');dot(x+2,150,'#463020')
# Fences integrate planting into a lived-in chamber.
for y in [59,143]:
 rect((102,y,260,y+2),'#4b3323');rect((102,y,260,y),'#a07541')
 for x in range(102,261,15):
  rect((x,y-3,x+3,y+6),'#4c3323');rect((x,y-3,x+2,y+4),'#9f7443');dot(x+1,y-3,'#c39a59')
# Crates by water, with wood grain, dark recesses, iron hinges.
for x,y in [(79,60),(79,74)]:
 rect((x,y,x+17,y+12),'#34251d');rect((x+1,y,x+16,y+9),'#94603a')
 for xx in range(x+3,x+16,4):rect((xx,y+1,xx,y+8),'#643e2a')
 rect((x+1,y,x+16,y+1),'#c68d4d');rect((x+1,y+7,x+16,y+8),'#bb8148');rect((x+3,y+2,x+4,y+6),'#53544a');rect((x+13,y+2,x+14,y+6),'#53544a')
# Reeds, stones and mushrooms break the grid around the cultivated area.
for x,y in [(33,131),(65,132),(40,68),(271,87),(277,139),(65,157),(263,155),(279,60)]:
 for i in range(6):
  xx=x+R.randrange(-3,5);h=R.randrange(3,9)
  d.line((xx,y,xx+R.randrange(-2,3),y-h),fill=R.choice(['#72643b','#9c8240','#4d5630']))
for x,y in [(42,148),(71,58),(279,111),(269,50),(91,130),(289,95)]:
 d.ellipse((x,y,x+5,y+3),fill='#44372b');rect((x+1,y,x+4,y+1),'#8a7960');dot(x+2,y,'#b1a083')
for x,y in [(43,57),(281,127),(29,141)]:
 rect((x+2,y+3,x+3,y+6),'#c0a67a');rect((x,y,x+5,y+3),'#845344');rect((x+1,y,x+4,y),'#c28460');dot(x+1,y+1,'#d7a17a')
# Local warm pools, quantized per pixel. Material detail remains pixel sharp.
lights=[(96,96,70),(254,74,64),(226,151,53)]
px=im.load()
for y in range(36,180):
 for x in range(320):
  amount=max(max(0,1-math.hypot(x-lx,y-ly)/rad) for lx,ly,rad in lights+[(154,108,67)])
  amount=round(amount*9)/9
  ambient=.38+amount*.93
  r,g,b=px[x,y];px[x,y]=(min(255,int(r*ambient+amount*12)),min(255,int(g*ambient+amount*4)),min(255,int(b*(.42+amount*.58))))
# Torches rendered after light, bright cores and iron/wood stands.
for x,y,_ in lights:
 rect((x-1,y+2,x+1,y+9),'#39271d');rect((x,y+2,x,y+8),'#bf894c')
 sprite(['..t..','.tut.','tuuut','tuuut','.tut.','..s..'],x-2,y-5)
# Explorer lantern: tiny bright amber pixels at the belt.
rect((154,106,155,108),'#bd7a36');dot(154,106,'#ffe4a1')
# Native-style compact lettering. Handmade 5x7 bitmap alphabet, logical pixel grid.
letters={
'A':['01110','11011','11011','11111','11011','11011','11011'],
'U':['11011','11011','11011','11011','11011','11011','01110'],
'T':['11111','11111','00100','00100','00100','00100','00100'],
'O':['01110','11011','11011','11011','11011','11011','01110'],
'R':['11110','11011','11011','11110','11100','11010','11011'],
'E':['11111','11000','11000','11110','11000','11000','11111'],
'P':['11110','11011','11011','11110','11000','11000','11000'],
'L':['11000','11000','11000','11000','11000','11000','11111'],
'N':['11011','11111','11111','11111','11011','11011','11011']}
text='AUTO REPLANT';s=2;tw=len(text)*12-2;x0=(320-tw)//2
for i,c in enumerate(text):
 if c==' ':continue
 for yy,row in enumerate(letters[c]):
  for xx,v in enumerate(row):
   if v=='1':
    x,y=x0+i*12+xx*2,13+yy*2
    rect((x+1,y+2,x+2,y+3),'#67462e');rect((x,y,x+1,y+1),'#f0cc83' if yy<4 else '#c89b57')
im.save(OUT/'autoreplant_native_thumbnail.png')
im.resize((1280,720),Image.Resampling.NEAREST).save(OUT/'autoreplant_native_sample.png',optimize=True)
print(OUT/'autoreplant_native_sample.png')
