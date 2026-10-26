"""Original cave fishing sample. Reference: official Core Keeper cozy-base screenshot.
No game artwork, screenshots, or extracted sprites are incorporated. Pillow only.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import random,math
R=random.Random(1945); W,H=320,180
im=Image.new('RGB',(W,H),(12,12,18));d=ImageDraw.Draw(im)
# Orthogonal carved cavern, each tile sixteen logical pixels.
land=Image.new('1',(W,H));ld=ImageDraw.Draw(land)
ld.polygon([(8,85),(24,85),(24,68),(55,68),(55,52),(96,52),(96,42),(160,42),(160,30),(255,30),(255,45),(288,45),(288,63),(304,63),(304,139),(286,139),(286,159),(251,159),(251,171),(87,171),(87,160),(39,160),(39,143),(8,143)],fill=1)
# Ground broken granular earth, avoiding checkerboard outlines.
for y in range(H):
 for x in range(W):
  if land.getpixel((x,y)):
   n=R.random();tile=((x//16)*7+(y//16)*11)%5
   cols=[(88,64,43),(93,68,44),(99,73,48),(105,78,50),(110,81,53)]
   c=cols[tile]
   if n<.035:c=(133,99,61)
   elif n<.09:c=(71,52,39)
   im.putpixel((x,y),c)
d=ImageDraw.Draw(im)
# Squared rocky wall faces along cavern boundary.
for y in range(20,168,16):
 for x in range(0,320,16):
  if land.getpixel((min(319,x+8),min(179,y+8))) and not land.getpixel((min(319,x+8),max(0,y-5))):
   d.rectangle((x,y-13,x+15,y+3),fill='#3d3029');d.rectangle((x+1,y-12,x+14,y-5),fill='#6e5639')
   d.line((x+2,y-12,x+12,y-12),fill='#977348');d.line((x+2,y-4,x+2,y+1),fill='#2c2424')
   for j in range(7):
    xx=x+R.randrange(2,14); yy=y+R.randrange(-10,2);d.rectangle((xx,yy,xx+R.randrange(1,3),yy+1),fill=R.choice(['#59452f','#80603e','#342b26']))
# Pond orthogonal bank, with exposed stratified dirt face.
pond=[(167,67),(207,67),(207,58),(257,58),(257,72),(281,72),(281,130),(264,130),(264,144),(201,144),(201,133),(163,133),(163,112),(151,112),(151,84),(167,84)]
d.polygon([(x,y+7) for x,y in pond],fill='#251e21');d.polygon(pond,fill='#263c49')
mask=Image.new('1',(W,H));ImageDraw.Draw(mask).polygon(pond,fill=1)
for y in range(58,145):
 for x in range(151,282):
  if mask.getpixel((x,y)):
   c=R.choice([(25,46,61),(26,47,63),(26,47,63),(29,51,65),(30,52,67)])
   im.putpixel((x,y),c)
for a,b in zip(pond,pond[1:]+pond[:1]):
 d.line([a,b],fill='#697363',width=1)
 if a[1]==b[1]:
  d.line([(a[0],a[1]-2),(b[0],b[1]-2)],fill='#907448')
  if a[0]<b[0]:d.line([(a[0],a[1]+2),(b[0],b[1]+2)],fill='#151f2a')
for j in range(170):
 x,y=R.randrange(155,280),R.randrange(62,142)
 if mask.getpixel((x,y)) and mask.getpixel((min(x+4,319),y)):
  d.line((x,y,x+R.randrange(1,5),y),fill=R.choice(['#35556a','#304e62','#496774']))
# Planked jetty points right into pond. Side posts cast submerged shadows.
d.rectangle((119,98,192,123),fill='#171d24');d.rectangle((119,92,190,113),fill='#563923')
for x in range(120,190,7):
 d.rectangle((x,92,x+5,111),fill=R.choice(['#9a7044','#a97b49','#8d623a']))
 d.line((x,92,x+5,92),fill='#c09257');d.line((x+2,95,x+2,108),fill='#785231')
 d.point((x+3,94),fill='#d2ac73');d.point((x+3,110),fill='#49352a')
for x in (124,181):
 for y in (91,113):
  d.rectangle((x-2,y-1,x+2,y+8),fill='#473424');d.rectangle((x-2,y-3,x+2,y),fill='#bd9056');d.point((x,y-2),fill='#e1b574')
# Original tiny sprite helper, 1 source pixel = 1 logical canvas pixel.
def sp(x,y,rows,p):
 for yy,row in enumerate(rows):
  for xx,k in enumerate(row):
   if k!='.':d.point((x+xx,y+yy),fill=p[k])
pal={'k':'#211d23','h':'#6c3827','H':'#9b5530','y':'#e4b44d','Y':'#ffdc79','s':'#ca865a','S':'#f0b778','w':'#ffdfaa','e':'#171a23','t':'#41776a','T':'#69a18a','b':'#304840','B':'#443932','g':'#957352'}
# Facing right, head and body proportions close to a small top-down explorer.
hero=['.....kkkkk......','....khhhhkk.....','...khHHHHHhk....','...kyyyyyyyk....','..kyYYYYYyyyyk..','..kyyyyykkkkk...','...kHHkSSSSk....','...khhkSwSek....','....kkkSSSSk....','.....ktkkSk.....','....kTTtttk.....','...kTTTtttSk....','...kTTttttSSk...','....kttttkkk....','....kbbbtk......','....kbbbbk......','....kBkkBk......','...kBBkkBBk.....','...kkkkkkkk.....']
d.ellipse((162,108,179,114),fill='#443927');sp(161,88,hero,pal)
# Bent rod and tight fishing line leading to bobber. Leaping silver-blue catch.
d.line([(176,100),(187,88),(198,81),(204,85)],fill='#e1b768',width=1);d.line([(204,85),(221,106)],fill='#b5bcb1');d.point((177,101),fill='#8d7350')
d.ellipse((215,108,227,112),outline='#69848a');d.ellipse((210,110,232,114),outline='#405b6d')
d.rectangle((220,105,222,107),fill='#f6cc8a');d.rectangle((220,108,222,110),fill='#cb6853')
sp(228,91,['......ss....','...ssswws...','ss.swwwews..','ssswwwwsss..','.ssssss.....','ss..ss......'],{'s':'#509baf','w':'#b3d8cf','e':'#192b35'})
for x,y in [(230,106),(239,109),(246,101)]:d.line((x,y,x+1,y-3),fill='#9dc5c7')
# Furniture, supplies, and loose natural details sit on ground, in proper scale.
def chest(x,y):
 d.rectangle((x,y+3,x+17,y+13),fill='#34271e');d.rectangle((x+1,y,x+16,y+10),fill='#9c6134');d.line((x+1,y+5,x+16,y+5),fill='#432b21')
 for xx in (x+3,x+13):d.line((xx,y,xx,y+10),fill='#c49a59')
 d.rectangle((x+7,y+5,x+10,y+8),fill='#e5bd6c');d.line((x+3,y+1,x+12,y+1),fill='#c68949')
chest(100,105)
# Barrel full of caught fish.
d.ellipse((99,131,113,137),fill='#2b241f');d.rectangle((99,123,113,132),fill='#956840');d.ellipse((99,119,113,126),fill='#c49455');d.ellipse((101,120,111,124),fill='#243b3b')
for x in (100,105,110):d.line((x,126,x,132),fill='#69452c')
d.line((99,128,113,128),fill='#494a43');d.line((103,122,109,120),fill='#a2c3b1')
# Grass and mushrooms around the shore, warm torch on both bank approaches.
def grass(x,y):
 for dx,h in [(-3,4),(0,7),(3,5)]:d.line((x,y,x+dx,y-h),fill=R.choice(['#788049','#97924d','#4c603f']))
def mush(x,y):
 d.line((x,y,x,y-5),fill='#d3bb85');d.rectangle((x-3,y-7,x+3,y-5),fill='#b86d40');d.line((x-2,y-8,x+2,y-8),fill='#da9153');d.point((x-1,y-7),fill='#eccd8c')
for x,y in [(68,106),(82,133),(128,147),(244,157),(287,118),(263,50),(181,51),(115,64),(40,129),(145,61),(293,87)]:grass(x,y)
for x,y in [(59,124),(65,127),(138,156),(146,159),(266,148),(272,151)]:mush(x,y)
for j in range(60):
 x,y=R.randrange(35,290),R.randrange(55,166)
 if land.getpixel((x,y)) and not mask.getpixel((x,y)) and not (90<x<194 and 85<y<140):
  d.rectangle((x,y,x+2,y+1),fill=R.choice(['#534b39','#b59a6d','#63553d']));d.point((x,y-1),fill='#ad976a')
def torch(x,y):
 d.ellipse((x-3,y+3,x+4,y+5),fill='#403124');d.rectangle((x,y-5,x+2,y+4),fill='#725035');d.rectangle((x-2,y-9,x+3,y-5),fill='#d6792f');d.rectangle((x-1,y-12,x+2,y-7),fill='#ffb649');d.line((x,y-11,x,y-8),fill='#fff2ac')
torch(119,83);torch(243,151)
# Pixel-quantized local light and cave falloff. No blur or resampling.
pix=im.load()
for y in range(H):
 for x in range(W):
  edge=max(0,min(1,min(x/38,(319-x)/35,(179-y)/20)))
  lights=max(0,1-math.hypot(x-120,y-76)/91),max(0,1-math.hypot(x-244,y-144)/72)
  a=round(max(lights)*7)/7
  ambient=.48+.36*edge
  r,g,b=pix[x,y];pix[x,y]=(min(255,int(r*ambient+a*38)),min(255,int(g*ambient+a*25)),min(255,int(b*ambient+a*7)))
# Hand-authored 5x7 title font. Dark upper-left cave provides natural contrast.
GLYPHS={'A':['01110','10001','10001','11111','10001','10001','10001'],'F':['11111','10000','10000','11110','10000','10000','10000'],'S':['01111','10000','10000','01110','00001','00001','11110'],'T':['11111','00100','00100','00100','00100','00100','00100'],'U':['10001','10001','10001','10001','10001','10001','01110'],'O':['01110','10001','10001','10001','10001','10001','01110'],'I':['11111','00100','00100','00100','00100','00100','11111'],'H':['10001','10001','10001','11111','10001','10001','10001'],'N':['10001','11001','11001','10101','10011','10011','10001'],'G':['01111','10000','10000','10111','10001','10001','01111'],' ':['00000']*7}
def label(s,x,y):
 for ch in s:
  for yy,row in enumerate(GLYPHS[ch]):
   for xx,v in enumerate(row):
    if v=='1':
     d.rectangle((x+xx*2+1,y+yy*2+2,x+xx*2+2,y+yy*2+3),fill='#271d20');d.rectangle((x+xx*2,y+yy*2,x+xx*2+1,y+yy*2+1),fill='#efcc82' if yy<3 else '#d8ac65')
  x+=12
label('FAST AUTO',14,13);label('FISHING',14,32)
# A little mineral seam, fully original, in the dark wall beside title.
for x,y in [(293,36),(295,34),(298,38)]:d.rectangle((x,y,x+1,y+2),fill='#527675')
out=Path(__file__).resolve().parent
im.resize((1280,720),Image.Resampling.NEAREST).save(out/'fastfish_native_sample.png',optimize=True)
im.save(out/'fastfish_native_thumbnail.png',optimize=True)
print(out/'fastfish_native_sample.png')
