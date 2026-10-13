"""Original detailed Core Keeper-inspired pixel scenes; no game assets.
Run python release/art_direction/make_native_a.py. Logical resolution 320x180.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import random,math
ROOT=Path(__file__).resolve().parents[2]
FONT={
'A':'01110 10001 10001 11111 10001 10001 10001','B':'11110 10001 10001 11110 10001 10001 11110','C':'01111 10000 10000 10000 10000 10000 01111','D':'11110 10001 10001 10001 10001 10001 11110','E':'11111 10000 10000 11110 10000 10000 11111','F':'11111 10000 10000 11110 10000 10000 10000','G':'01111 10000 10000 10111 10001 10001 01111','H':'10001 10001 10001 11111 10001 10001 10001','I':'111 010 010 010 010 010 111','J':'00111 00010 00010 00010 10010 10010 01100','K':'10001 10010 10100 11000 10100 10010 10001','L':'10000 10000 10000 10000 10000 10000 11111','M':'10001 11011 10101 10101 10001 10001 10001','N':'10001 11001 11001 10101 10011 10011 10001','O':'01110 10001 10001 10001 10001 10001 01110','P':'11110 10001 10001 11110 10000 10000 10000','Q':'01110 10001 10001 10001 10101 10010 01101','R':'11110 10001 10001 11110 10100 10010 10001','S':'01111 10000 10000 01110 00001 00001 11110','T':'11111 00100 00100 00100 00100 00100 00100','U':'10001 10001 10001 10001 10001 10001 01110','V':'10001 10001 10001 10001 10001 01010 00100','W':'10001 10001 10001 10101 10101 10101 01010','X':'10001 10001 01010 00100 01010 10001 10001','Y':'10001 10001 01010 00100 00100 00100 00100','Z':'11111 00001 00010 00100 01000 10000 11111',' ':'000 000 000 000 000 000 000'}
class Scene:
 def __init__(self,seed=1,green=False):
  self.r=random.Random(seed);self.im=Image.new('RGB',(320,180),'#101019');self.d=ImageDraw.Draw(self.im);self.lights=[]
  self.land=Image.new('1',(320,180));z=ImageDraw.Draw(self.land)
  z.polygon([(8,71),(25,71),(25,54),(71,54),(71,43),(155,43),(155,28),(265,28),(265,44),(294,44),(294,67),(309,67),(309,148),(293,148),(293,166),(250,166),(250,176),(60,176),(60,164),(23,164),(23,143),(8,143)],fill=1)
  for y in range(180):
   for x in range(320):
    if self.land.getpixel((x,y)):
     colors=['#765438','#805d3b','#89663f','#916e44','#6d5238'] if not green else ['#3a4938','#48573e','#4e5b40','#596546','#424f36']
     c=colors[(x//16*7+y//16*11)%5]
     if self.r.random()<.13:c=self.r.choice(['#a4824d','#5b4834','#b99961'] if not green else ['#667848','#354632','#7b864f'])
     self.im.putpixel((x,y),tuple(int(c[i:i+2],16) for i in (1,3,5)))
  for x,y in [(25,53),(41,53),(57,53),(73,42),(89,42),(105,42),(121,42),(137,42)]+[(x,27) for x in range(155,266,16)]+[(281,43),(294,59)]:self.rock(x,y,16)
  for _ in range(90):
   x,y=self.r.randrange(28,296),self.r.randrange(52,171)
   if self.land.getpixel((x,y)):
    self.d.line((x,y,x+2,y),fill='#bc9b64');self.d.point((x+1,y+1),fill='#554633')
 def sprite(self,x,y,rows,p):
  for j,row in enumerate(rows):
   for i,k in enumerate(row):
    if k!='.':self.d.point((x+i,y+j),fill=p[k])
 def rock(self,x,y,n=14):
  d=self.d;r=self.r
  d.ellipse((x-2,y+3,x+n+2,y+12),fill='#282423')
  d.polygon([(x,y-8),(x+3,y-12),(x+n-3,y-12),(x+n,y-7),(x+n,y+8),(x+n-4,y+11),(x+2,y+8)],fill='#51402e')
  d.polygon([(x,y-8),(x+3,y-12),(x+n-3,y-12),(x+n,y-7),(x+n-3,y-3),(x+3,y-3)],fill='#8f7246')
  d.line((x+3,y-11,x+n-4,y-11),fill='#bc9659');d.line((x+3,y-2,x+3,y+7),fill='#705539')
  d.polygon([(x+n-5,y-2),(x+n-2,y-5),(x+n-2,y+6),(x+n-6,y+9)],fill='#332d26')
  for j in range(14):
   xx=x+r.randrange(2,n-2);yy=y+r.randrange(-9,7);d.line((xx,yy,xx+1,yy),fill=r.choice(['#675036','#a58450','#765a36','#45372c']))
  for dx in (4,n-5):d.line((x+dx,y-1,x+dx-1,y+5),fill='#a0783f')
 def reeds(self,x,y,col='ochre'):
  d=self.d
  cs=['#444b2b','#74783d','#afb15b','#d0c778'] if col=='green' else ['#5d3c29','#8b5930','#b88943','#d8ae61']
  d.ellipse((x-6,y-1,x+7,y+3),fill='#423a2b')
  for dx,h in [(-6,9),(-3,16),(0,12),(3,19),(6,10)]:
   d.line((x,y,x+dx,y-h),fill=cs[1]);d.line((x+dx,y-h,x+dx+1,y-h+5),fill=cs[2]);d.point((x+dx,y-h-1),fill=cs[3])
 def fern(self,x,y,purple=False,pot=False):
  d=self.d
  if pot:
   d.polygon([(x-6,y-1),(x+6,y-1),(x+4,y+6),(x-4,y+6)],fill='#8d553b');d.line((x-6,y-1,x+6,y-1),fill='#c08b59');d.line((x-4,y+4,x+3,y+4),fill='#643d2d')
  else:d.ellipse((x-10,y-2,x+11,y+4),fill='#373c2b')
  cs=['#3d3c50','#61516b','#846387','#b081a2','#d4a1b9'] if purple else ['#263e32','#3e5940','#607c4a','#8a9d58','#bbc47b']
  for dx,dy in [(-10,-5),(10,-7),(-8,-11),(7,-13),(-3,-17),(2,-12)]:
   d.polygon([(x,y),(x+dx-3,y+dy+3),(x+dx,y+dy-2),(x+dx+3,y+dy+2)],fill=cs[1]);d.line((x,y,x+dx,y+dy),fill=cs[3]);d.line((x+dx-2,y+dy+1,x+dx,y+dy-1),fill=cs[2]);d.point((x+dx,y+dy-2),fill=cs[4])
 def stepping(self,x,y):
  d=self.d;d.polygon([(x-6,y),(x-3,y-3),(x+4,y-3),(x+7,y),(x+5,y+3),(x-4,y+3)],fill='#8a8062');d.line((x-3,y-3,x+3,y-3),fill='#b0a281');d.line((x-4,y+3,x+4,y+3),fill='#564b3a');d.point((x+2,y),fill='#9e9778')
 def moss(self,x,y,n=10):
  for j in range(n):
   xx=x+self.r.randrange(-8,9); yy=y+self.r.randrange(-3,4);self.d.line((xx,yy,xx+2,yy),fill=self.r.choice(['#556039','#768249','#949651','#3b4b32']))
 def mushroom(self,x,y,col='orange',scale=1):
  p={'k':'#3a2630','s':'#bca97e','w':'#ecddb0','a':'#bd6a42','b':'#db925a','c':'#84443a'}
  if col=='blue':p.update(a='#467e91',b='#7db8bc',c='#335165')
  if col=='purple':p.update(a='#936798',b='#c49ac1',c='#5e445f')
  rows=['...bbb...','..babbb..','.abawbab.','aacaaacaa','.ccccccc.','...sws...','...sws...','...sss...']
  for j,row in enumerate(rows):
   for i,k in enumerate(row):
    if k!='.':self.d.rectangle((x+i*scale,y+j*scale,x+(i+1)*scale-1,y+(j+1)*scale-1),fill=p[k])
 def torch(self,x,y):
  d=self.d;d.ellipse((x-3,y+8,x+5,y+10),fill='#393021');d.rectangle((x,y,x+2,y+9),fill='#69472f');d.line((x,y+1,x,y+6),fill='#d09548')
  self.sprite(x-3,y-8,['...y...','..yY...','.oyYy..','ooyWYy.','.oYYyo.','..oro..'],{'o':'#cd6a27','r':'#944029','y':'#ffb343','Y':'#ffe685','W':'#fff3c0'})
  self.lights.append((x,y,80,(40,26,6)))
 def floor(self,x,y,w,h):
  d=self.d;d.rectangle((x-3,y-3,x+w+3,y+h+8),fill='#3a2822')
  for yy in range(y,y+h,7):
   for xx in range(x-8,x+w,17):
    off=8 if ((yy-y)//7)%2 else 0;a=max(x,xx+off);b=min(x+w,xx+off+15)
    if a>b:continue
    d.rectangle((a,yy,b,min(y+h,yy+5)),fill=self.r.choice(['#9c6943','#aa774b','#92603d','#b17c4b']))
    d.line((a,yy,b,yy),fill='#c3935c');d.line((a+2,yy+4,max(a+2,b-2),yy+4),fill='#835335')
 def rug(self,x,y,w,h,c='#675884'):
  d=self.d;d.rectangle((x,y,x+w,y+h),fill='#3e303a');d.rectangle((x+2,y+2,x+w-2,y+h-2),fill=c)
  d.rectangle((x+4,y+4,x+w-4,y+h-4),outline='#c2a068')
  for xx in range(x+6,x+w-5,6):d.point((xx,y+6),fill='#dbc08a');d.point((xx,y+h-6),fill='#dbc08a')
 def bench(self,x,y,w=28):
  d=self.d;d.rectangle((x+2,y+8,x+w-2,y+17),fill='#4e3224');d.rectangle((x-1,y-1,x+w+1,y+9),fill='#482e24')
  for xx in range(x,x+w,5):d.rectangle((xx,y,min(xx+3,x+w),y+6),fill='#a97743');d.line((xx,y,min(xx+3,x+w),y),fill='#d5a266')
  for xx in (x+2,x+w-4):d.rectangle((xx,y+10,xx+2,y+17),fill='#885732')
 def chest(self,x,y):
  self.sprite(x,y,['..kkkkkkkkkkkk..','.kbbbbBBBBbbbbk.','kbbbbBBBBBBbbbbk','kbwwbBBBBBBbwwbk','kbbbbbbbbbbbbbbk','kggggggggggggggk','kbBBBggggBBBbbbk','kbBBBgyygBBBbbbk','kbBBBggggBBBbbbk','kbBBBBBBBBBBbbbk','.kkkkkkkkkkkkkk.'],{'k':'#3f2d24','b':'#a9763f','B':'#855430','w':'#ce995b','g':'#bb985b','y':'#e7c47a'})
 def barrel(self,x,y):
  self.sprite(x,y,['..bbbbbbbb..','.bwwggggwwb.','bwgkkkkkkgwb','bgkkgwwgkkgb','bbggggggggbb','bBwBwwBwBwwb','bggggggggggb','bBwBwwBwBwwb','bBwBwwBwBwwb','bggggggggggb','.BBBBBBBBBB.','..kkkkkkkk..'],{'b':'#a97943','w':'#c7995b','B':'#66432e','g':'#697064','k':'#33362e'})
 def shelf(self,x,y,w=42):
  d=self.d;d.rectangle((x,y,x+w,y+23),fill='#34251f');d.rectangle((x,y,x+2,y+26),fill='#87603a');d.rectangle((x+w-2,y,x+w,y+26),fill='#87603a')
  for yy in (y+10,y+22):
   for xx in range(x+4,x+w-4,4):
    c=self.r.choice(['#77948b','#997b57','#b26449','#867394','#b09d65']);d.rectangle((xx,yy-self.r.randrange(5,9),xx+2,yy-1),fill=c);d.point((xx,yy-3),fill='#d7bc81')
   d.line((x,yy,x+w,yy),fill='#bc8d51');d.line((x,yy+1,x+w,yy+1),fill='#6a432e')
 def hero(self,x,y,color='#4d948a',hat=True,can=False):
  p={'k':'#25232a','h':'#693d2b','H':'#95603d','y':'#dfb24f','Y':'#f5d47c','s':'#b97c55','S':'#e0aa79','w':'#f5ce95','e':'#19232b','t':color,'T':'#89baa3','b':'#3b514a','B':'#5b4334','g':'#b29461'}
  self.d.ellipse((x+2,y+18,x+15,y+23),fill='#3f372a')
  rows=['.....kkkkk......','....khhhhkk.....','...khHHHHHhk....','...kyyyyyyyk....','..kyYYYYYyyyyk..','..kyyyyykkkkk...','...kHHkSSSSk....','...khhkSwSek....','....kkkSSSSk....','.....ktkkSk.....','....kTTtttk.....','...kTTTtgttSk...','...kTTtggttSSk..','....kttgtkkk....','....kbbbtk......','....kbbbbk......','....kBkkBk......','...kBBkkBBk.....','...kkkkkkkk.....']
  self.sprite(x,y,rows,p)
  if can:self.sprite(x+12,y+11,['..ggg....','ggcccg...','gwccccggg','gwcccccg.','.gggggg..'],{'g':'#556e70','w':'#b9dad0','c':'#76a5aa'})
 def armor(self,x,y,c='#a8b7b7'):
  p={'k':'#2a2830','a':c,'w':'#d3d9cb','s':'#64757d','b':'#7b573a','g':'#b89a56'}
  self.d.ellipse((x-2,y+25,x+17,y+30),fill='#46352b')
  self.sprite(x,y,['....kkkkkk....','...kaawa aak...'.replace(' ',''),'..kaaawaaaak..','..kssssssssk..','..kskkkkksk...','...kk...kk....','...gg...gg....','..kaakkkkaak..','.kaawaaaaaaak.','kaaaawaaa aaaak'.replace(' ',''),'.kkaaaaaaaakk.','..kaaagaaak...','..ksssssssk...','...kbkkbk.....','...kb..bk.....','...kb..bk.....','..kbb..bbk....','..kkkkkkkk....'],p)
  self.d.line((x+6,y+18,x+6,y+25),fill='#b38d54');self.d.line((x,y+26,x+13,y+26),fill='#92683f')
 def potion(self,x,y,c='#ac699c'):
  self.sprite(x,y,['..bbb..','..gwg..','..gwg..','.gwwwg.','gwwaaag','gwaaaag','.gsssg.','..ggg..'],{'b':'#a78053','g':'#5b6d74','w':'#c3dad5','a':c,'s':'#754e77'})
 def pool(self,pts):
  d=self.d;d.polygon([(x,y+8) for x,y in pts],fill='#342927');d.polygon(pts,fill='#253f53');mask=Image.new('1',(320,180));ImageDraw.Draw(mask).polygon(pts,fill=1)
  for j in range(420):
   x,y=self.r.randrange(320),self.r.randrange(180)
   if mask.getpixel((x,y)):
    for xx in range(x,min(x+self.r.randrange(2,6),320)):
     if mask.getpixel((xx,y)):d.point((xx,y),fill=self.r.choice(['#33546b','#2a4a60','#426577','#263e53']))
  for a,b in zip(pts,pts[1:]+pts[:1]):
   d.line([a,b],fill='#65796d')
   if a[1]==b[1]:d.line((a[0],a[1]-2,b[0],b[1]-2),fill='#b29966')
  return mask
 def finish(self,title,name):
  # Layered plants break the bare outer earth and frame the playable area.
  for x,y in [(34,92),(42,141),(70,161),(286,153),(294,103)]:self.fern(x,y,purple=(x%3==0))
  pix=self.im.load()
  for y in range(180):
   for x in range(320):
    edge=max(0,min(1,min(x/35,(319-x)/30,(179-y)/17)))
    ambient=.66+.24*edge;r,g,b=pix[x,y];r*=ambient;g*=ambient;b*=ambient
    for lx,ly,rad,col in self.lights:
     a=round(max(0,1-math.hypot(x-lx,y-ly)/rad)*8)/8;r+=a*col[0];g+=a*col[1];b+=a*col[2]
    pix[x,y]=(min(255,int(r)),min(255,int(g)),min(255,int(b)))
  # Darken the cave naturally behind titles, with a soft spatial falloff instead of a card.
  title_lines=title.split('|')
  widths=[sum((len(FONT[ch].split()[0])+1)*2 for ch in line) for line in title_lines]
  for yy in range(min(180,9+len(title_lines)*18+15)):
   for xx in range(min(320,11+max(widths)+18)):
    coverage=0
    for row,width in enumerate(widths):
     left,top,right,bottom=8,6+row*18,13+width,25+row*18
     dx=max(left-xx,0,xx-right);dy=max(top-yy,0,yy-bottom)
     coverage=max(coverage,max(0,1-math.hypot(dx,dy)/14))
    if coverage:
     rr,gg,bb=self.im.getpixel((xx,yy));a=.84*coverage
     self.im.putpixel((xx,yy),(int(rr*(1-a)+12*a),int(gg*(1-a)+12*a),int(bb*(1-a)+18*a)))
  # two lines at natural pixel resolution, clear enough at thumbnail scale
  for row,s in enumerate(title.split('|')):
   x=11;y=9+row*18
   for ch in s:
    rows=FONT[ch].split();w=len(rows[0])
    for j,bits in enumerate(rows):
     for i,v in enumerate(bits):
      if v=='1':
       self.d.rectangle((x+i*2+1,y+j*2+2,x+i*2+2,y+j*2+3),fill='#2b2325');self.d.rectangle((x+i*2,y+j*2,x+i*2+1,y+j*2+1),fill='#edcf8e' if j<3 else '#d9b26b')
    x+=(w+1)*2
  self.im.resize((1280,720),Image.Resampling.NEAREST).save(ROOT/'release'/name,optimize=True)
  return self.im.copy()

def fishing():
 s=Scene(31);d=s.d
 pts=[(161,70),(204,70),(204,60),(250,60),(250,72),(277,72),(277,130),(262,130),(262,146),(202,146),(202,134),(158,134),(158,112),(146,112),(146,84),(161,84)];s.pool(pts)
 for x,y in [(166,60),(182,62),(216,51),(250,52),(279,68),(284,123),(263,156),(200,154),(145,135)]:s.rock(x,y,12);s.moss(x+5,y-9,24)
 for x,y in [(146,77),(184,66),(261,74),(280,110),(252,154),(189,147),(135,127),(289,146)]:s.reeds(x,y,'green')
 for xx in range(120,194,6):
  d.rectangle((xx,94,xx+4,115),fill=s.r.choice(['#9c7146','#b17f4c','#a57545']));d.line((xx,94,xx+4,94),fill='#dfb679');d.line((xx+2,97,xx+2,112),fill='#805737');d.point((xx+2,95),fill='#52402e');d.point((xx+2,114),fill='#54402c')
 d.rectangle((121,117,191,119),fill='#4c3729')
 for x in (124,181):
  for y in (92,116):d.rectangle((x-2,y,x+2,y+9),fill='#735236');d.ellipse((x-2,y-2,x+2,y+1),fill='#d2ad74')
 s.hero(163,91);d.line([(179,104),(190,89),(201,83),(207,87)],fill='#e1c88e');d.line((207,87,223,107),fill='#bbc8b8')
 d.ellipse((215,109,230,113),outline='#7ea6ac');d.ellipse((211,111,234,116),outline='#486e80');d.rectangle((222,106,224,108),fill='#eec783');d.rectangle((222,109,224,111),fill='#c86852')
 s.sprite(231,95,['......ss....','...ssswws...','ss.swwwews..','ssswwwwsss..','.ssssss.....','ss..ss......'],{'s':'#5597a9','w':'#bddbce','e':'#182b35'})
 for x,y in [(234,111),(242,112),(246,106)]:d.line((x,y,x+1,y-3),fill='#9cc5c7')
 s.chest(100,103);s.barrel(99,130);s.bench(59,92,30);s.potion(63,86,'#8b9b65')
 d.rectangle((76,88,83,94),fill='#c1af83');d.line((77,90,81,90),fill='#665b46')
 for x,y in [(56,121),(64,129),(88,153),(113,157),(224,160),(285,87)]:s.mushroom(x,y);s.moss(x,y,25)
 for x,y in [(40,103),(43,146),(79,74),(124,65),(122,148),(293,132)]:s.reeds(x,y)
 for x,y in [(54,137),(61,115),(78,104),(93,96),(107,91)]:s.stepping(x,y)
 for x,y in [(268,67),(282,118),(239,157),(156,138),(134,73),(66,148),(76,68)]:s.fern(x,y,purple=x%2==0)
 s.barrel(80,136);s.chest(48,89);s.mushroom(127,140,'purple',2)
 s.torch(116,79);s.torch(242,150)
 result=s.finish('FAST AUTO|FISHING','fastfish_logo.png');result.resize((1280,720),Image.Resampling.NEAREST).save(ROOT/'release/art_direction/fastfish_native_sample.png');result.save(ROOT/'release/art_direction/fastfish_native_thumbnail.png');return result

def options():
 s=Scene(44);d=s.d;s.floor(87,61,164,97);s.rug(123,112,78,33,'#496c69');s.shelf(91,60,53);s.shelf(189,60,51)
 s.bench(155,86,52)
 # physical brass controls on a workbench, small not interface panels
 for x,h in [(163,6),(179,10),(195,4)]:
  d.rectangle((x-3,88,x+3,93),fill='#676856');d.line((x,90,x+3,90-h),fill='#cba467');d.rectangle((x+1,87-h,x+5,90-h),fill='#d6ba7e')
 s.fern(96,91,pot=True);s.fern(231,94,purple=True,pot=True);s.fern(210,149,pot=True)
 s.hero(176,108);s.chest(101,140);s.barrel(220,136);s.potion(222,130,'#678d8c')
 s.bench(213,108,27);s.potion(217,101);s.potion(226,101,'#649095')
 # scroll laid on writing desk
 s.bench(99,105,25);d.rectangle((103,102,119,108),fill='#dfcea2');d.line((105,104,116,104),fill='#9f8158');d.line((105,106,112,106),fill='#9f8158')
 for x,y in [(58,81),(68,133),(270,101),(271,148),(89,168)]:s.rock(x,y,12);s.moss(x+3,y-5,25);s.reeds(x-7,y+5)
 s.torch(147,80);s.torch(231,118);return s.finish('MOD OPTIONS','modoptions_logo.png')

def loadouts(five=False):
 s=Scene(56 if five else 55);d=s.d;s.floor(60,69,219,88);s.shelf(68,62,33);s.chest(253,138);s.barrel(65,139)
 if five:
  for i,(x,c) in enumerate([(79,'#b7c4c0'),(119,'#9bbaa3'),(159,'#c9a580'),(199,'#d2b867'),(239,'#ac9abe')]):s.rug(x-6,95,27,41,['#725549','#5c6d52','#755b43','#70614a','#665477'][i]);s.armor(x,85,c)
  s.hero(170,138);s.fern(109,141,pot=True);s.fern(220,143,pot=True)
 else:
  for x,c in [(112,'#d4c7a2'),(168,'#9cb9c3'),(225,'#b29bc4')]:s.rug(x-7,95,31,43,'#6f5159');s.armor(x,85,c)
  s.hero(145,139);s.fern(97,136,pot=True);s.fern(248,135,purple=True,pot=True)
  # shared gear laid on a small central bench, strong environment storytelling
  s.bench(176,145,31);s.potion(197,138,'#689a81')
 for x in (110,157,203,250):
  d.line((x,59,x,76),fill='#48352c');d.line((x-1,60,x-1,71),fill='#bcc4ba');d.line((x-4,71,x+3,71),fill='#bd995a')
 for x,y in [(34,112),(49,153),(293,121),(280,62)]:s.rock(x,y,13);s.moss(x,y-10,22)
 s.torch(104,65);s.torch(260,102)
 return s.finish('FIVE LOADOUTS' if five else 'LOADOUT|FALLBACK','fiveloadouts_logo.png' if five else 'loadout_logo.png')

def skill():
 s=Scene(75);d=s.d
 # Mining alcove connected to garden, crafting bench and combat training yard.
 for x,y in [(196,60),(210,62),(229,67),(249,64),(267,73)]:
  s.rock(x,y,16)
  for dx,dy in [(3,-8),(9,-5),(7,2)]:d.polygon([(x+dx,y+dy-3),(x+dx+3,y+dy),(x+dx,y+dy+3),(x+dx-2,y+dy)],fill='#8ab9bb');d.point((x+dx,y+dy-2),fill='#d0dfcf')
 s.hero(196,86);d.line((209,98,222,84),fill='#c59e60');d.line((216,81,227,87),fill='#adc2ba')
 s.floor(61,96,66,45);s.bench(68,105,38);s.chest(68,128);s.shelf(70,78,38)
 for x in (152,178):
  d.ellipse((x-9,133,x+10,139),fill='#493d2c');d.line((x,108,x,136),fill='#845837');d.line((x-9,116,x+9,116),fill='#ae7c43');d.ellipse((x-5,108,x+5,119),fill='#bd995b');d.line((x-4,111,x+4,116),fill='#776b40')
 for y in (128,142,156):
  for x in (239,253,267):
   d.rectangle((x-5,y-2,x+5,y+5),fill='#574231');s.reeds(x,y,'green')
 for x,y in [(38,104),(42,142),(123,157),(283,117),(289,151)]:s.moss(x,y,22);s.mushroom(x,y)
 for x,y in [(88,152),(109,158),(216,143),(275,105),(137,67)]:s.fern(x,y,purple=x%2==0)
 s.barrel(113,129);s.chest(180,151)
 s.torch(131,93);s.torch(225,127);return s.finish('SKILL XP|MULTIPLIER','skillxp-logo.png')

def durability():
 s=Scene(82);d=s.d;s.floor(92,69,169,93)
 # Detailed stone forge at back with bright coals and chimney.
 for y in (53,60,67):
  for x in range(189,224,9):d.rectangle((x,y,x+7,y+5),fill=s.r.choice(['#5b6264','#747a76','#4b5257']));d.line((x,y,x+7,y),fill='#91918a')
 d.rectangle((187,74,225,100),fill='#363239');d.rectangle((193,76,220,93),fill='#251e24')
 for j in range(70):
  x,y=s.r.randrange(194,220),s.r.randrange(84,95);d.point((x,y),fill=s.r.choice(['#d2542c','#f39b3e','#ffda71','#854132']))
 s.lights.append((208,88,110,(58,27,3)))
 d.rectangle((184,96,227,101),fill='#707370');d.line((185,96,226,96),fill='#b1a592')
 s.bench(110,76,40);s.shelf(109,49,40);s.armor(239,108,'#afb9b5');s.barrel(224,143);s.chest(98,146)
 # anvil supported by a wood stump
 d.ellipse((162,134,188,144),fill='#5b392a');d.rectangle((165,130,187,139),fill='#815532');d.polygon([(158,122),(190,122),(183,128),(180,128),(180,133),(185,135),(165,135),(170,132),(170,127),(163,127)],fill='#677a80');d.line((158,122,189,122),fill='#b7c7c3')
 s.hero(142,110,color='#977154');d.line((156,122,165,116),fill='#a37d51');d.rectangle((163,111,169,115),fill='#9eaaa4')
 for x,y in [(43,126),(67,152),(283,145),(279,84),(83,88)]:s.rock(x,y,13);s.moss(x,y-6,15)
 s.barrel(243,145);s.chest(113,146);s.fern(104,100,pot=True);s.fern(251,75,pot=True)
 s.torch(115,112);return s.finish('DURABILITY|MULTIPLIER','durability_logo.png')

def buff():
 s=Scene(95);d=s.d;s.floor(62,70,214,92);s.shelf(71,56,68);s.shelf(200,56,57);s.rug(121,117,54,35,'#69506d')
 for x in (73,89,105,122,204,220,237,249):s.potion(x,57,s.r.choice(['#9b659d','#b66d48','#718f68','#619299']))
 s.bench(110,88,104)
 for x in (118,137,158,178,196):s.potion(x,82,s.r.choice(['#a478a7','#a6ae76','#679899','#c59d57']))
 # leafy ingredients and ceramic plates on kitchen counter
 for x in (124,152,180):d.ellipse((x,95,x+11,100),fill='#d1c09b');d.ellipse((x+2,96,x+9,98),fill='#827357')
 s.hero(163,115,color='#946d97');s.chest(77,143);s.barrel(227,139);s.barrel(242,143)
 # hanging cooking pot with red coals
 d.line((223,114,233,92),fill='#8d795a');d.line((245,114,233,92),fill='#8d795a');d.ellipse((225,108,243,119),fill='#d77932');d.ellipse((225,99,243,113),fill='#465657');d.arc((224,93,244,111),180,360,fill='#a2b0a0',width=1);d.line((227,101,240,101),fill='#899c8b');s.lights.append((233,114,72,(38,21,4)))
 for x,y in [(35,133),(50,155),(285,106),(285,148),(149,165)]:s.mushroom(x,y,'purple');s.moss(x,y,20)
 s.fern(80,121,pot=True);s.fern(254,120,purple=True,pot=True);s.fern(180,147,pot=True)
 s.torch(97,106);s.torch(263,97);return s.finish('BUFF DURATION|FLOOR','buffduration_logo.png')

def mushrooms():
 s=Scene(116,True);d=s.d;s.pool([(198,121),(243,121),(243,133),(264,133),(264,152),(215,152),(215,144),(198,144)])
 # fungal bed clusters of varied height with overlapping caps and hanging roots
 for x,y in [(48,101),(73,79),(112,74),(155,71),(193,55),(228,70),(266,86),(280,124),(91,134),(134,135),(164,109),(48,146)]:
  s.moss(x,y+15,45);s.mushroom(x,y,s.r.choice(['blue','purple','orange']),2 if x%3 else 1)
 for j in range(45):
  x,y=s.r.randrange(42,288),s.r.randrange(78,165)
  if not (195<x<267 and y>117):s.mushroom(x,y,s.r.choice(['blue','purple','orange']));s.moss(x,y+8,10)
 for x,y in [(159,55),(180,48),(246,55),(34,112),(280,155)]:s.rock(x,y,14);s.moss(x,y-9,35);s.reeds(x+6,y+8,'green')
 for x,y in [(71,98),(104,116),(148,95),(228,97),(270,132),(164,164)]:s.fern(x,y,purple=True)
 s.hero(179,129);s.barrel(152,147);s.lights.append((131,116,94,(21,30,37)));s.lights.append((246,100,77,(14,28,37)));s.torch(71,119)
 return s.finish('FASTER|MUSHROOMS','fastermushrooms_logo.png')

def watering():
 s=Scene(131);d=s.d
 # terraced prepared garden, wet left beds and rich mature crops behind.
 for row in range(5):
  for col in range(7):
   x=141+col*18;y=74+row*16;wet=col<4 and row>0
   d.rectangle((x,y,x+15,y+12),fill='#3d4d43' if wet else '#694c32')
   for k in (2,6,10):d.line((x+1,y+k,x+14,y+k),fill='#617164' if wet else '#987047')
   d.line((x+2,y+13,x+15,y+13),fill='#342e25')
   if row==0 or col>=4:
    d.line((x+7,y+9,x+7,y+2),fill='#86a269');d.polygon([(x+7,y+6),(x+2,y+3),(x+3,y),(x+7,y+3),(x+11,y),(x+13,y+3)],fill='#72914f');d.point((x+4,y+1),fill='#b9bf6e')
   else:
    d.line((x+7,y+9,x+7,y+5),fill='#8aa875');d.line((x+7,y+7,x+4,y+4),fill='#b3c58a')
 s.hero(113,101,can=True)
 for x,y in [(135,118),(142,121),(148,123),(153,125)]:d.line((x,y,x+1,y+3),fill='#aacdc0')
 # well, seed shelf, storage crates, fence
 d.ellipse((64,109,94,126),fill='#39362f');d.rectangle((64,97,94,113),fill='#777c70');d.ellipse((64,90,94,102),fill='#b0ad8c');d.ellipse((68,93,90,100),fill='#284b58')
 for x in (63,95):d.rectangle((x,73,x+2,108),fill='#865d38')
 d.line((61,76,99,76),fill='#cda26a');d.line((80,76,80,94),fill='#d0b77f');d.rectangle((77,91,83,97),fill='#987549')
 s.chest(77,142);s.barrel(98,139);s.bench(100,67,29)
 for x in (104,113,122):s.potion(x,61,'#85965b')
 for x in range(143,274,13):d.rectangle((x,155,x+2,163),fill='#846239');d.line((x,157,min(x+13,279),157),fill='#b38d51')
 for x,y in [(46,131),(57,155),(280,83),(290,148),(138,59)]:s.reeds(x,y,'green');s.moss(x,y,20)
 s.fern(67,75,pot=True);s.fern(282,60);s.chest(111,149)
 for x,y in [(112,128),(112,88),(124,72),(124,158)]:s.stepping(x,y)
 s.torch(116,148);s.torch(263,60);return s.finish('BIGGER|WATERING CANS','wateringcans_logo.png')

if __name__=='__main__':
 imgs=[options(),loadouts(),loadouts(True),skill(),fishing(),durability(),buff(),mushrooms(),watering()]
 sheet=Image.new('RGB',(960,540))
 for i,im in enumerate(imgs):sheet.paste(im,((i%3)*320,(i//3)*180))
 (ROOT/'build').mkdir(exist_ok=True);sheet.save(ROOT/'build/native_a_montage.png')
 print(ROOT/'build/native_a_montage.png')
