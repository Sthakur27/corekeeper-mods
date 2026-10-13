"""Original detailed pixel scenes, drawn from scratch. No game assets or screenshot pixels.
Run python release/art_direction/make_native_c.py. Pillow; 320x180 ->1280x720 nearest.
"""
from pathlib import Path
import math, random
from PIL import Image, ImageDraw
OUT=Path(__file__).resolve().parent.parent
GLYPHS=dict(zip('ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+ ',['01110/10001/10001/11111/10001/10001/10001','11110/10001/10001/11110/10001/10001/11110','01111/10000/10000/10000/10000/10000/01111','11110/10001/10001/10001/10001/10001/11110','11111/10000/10000/11110/10000/10000/11111','11111/10000/10000/11110/10000/10000/10000','01111/10000/10000/10111/10001/10001/01111','10001/10001/10001/11111/10001/10001/10001','111/010/010/010/010/010/111','00111/00010/00010/00010/10010/10010/01100','10001/10010/10100/11000/10100/10010/10001','10000/10000/10000/10000/10000/10000/11111','10001/11011/10101/10101/10001/10001/10001','10001/11001/10101/10011/10001/10001/10001','01110/10001/10001/10001/10001/10001/01110','11110/10001/10001/11110/10000/10000/10000','01110/10001/10001/10001/10101/10010/01101','11110/10001/10001/11110/10100/10010/10001','01111/10000/10000/01110/00001/00001/11110','11111/00100/00100/00100/00100/00100/00100','10001/10001/10001/10001/10001/10001/01110','10001/10001/10001/10001/10001/01010/00100','10001/10001/10001/10101/10101/10101/01010','10001/10001/01010/00100/01010/10001/10001','10001/10001/01010/00100/00100/00100/00100','11111/00001/00010/00100/01000/10000/11111','01110/10011/10101/10101/11001/10001/01110','010/110/010/010/010/010/111','11110/00001/00001/01110/10000/10000/11111','11110/00001/00001/01110/00001/00001/11110','10001/10001/10001/11111/00001/00001/00001','11111/10000/10000/11110/00001/00001/11110','01111/10000/10000/11110/10001/10001/01110','11111/00001/00010/00100/01000/01000/01000','01110/10001/10001/01110/10001/10001/01110','01110/10001/10001/01111/00001/00001/11110','000/010/010/111/010/010/000','000/000/000/000/000/000/000']))
class Scene:
 def __init__(self,seed):
  self.r=random.Random(seed);self.im=Image.new('RGB',(320,180),'#090c13');self.d=ImageDraw.Draw(self.im);self.lights=[]
 def rect(self,b,c):self.d.rectangle(b,fill=c)
 def line(self,p,c,w=1):self.d.line(p,fill=c,width=w)
 def spr(self,rows,pal,x,y):
  for j,row in enumerate(rows):
   for i,c in enumerate(row):
    if c in pal:self.d.point((x+i,y+j),fill=pal[c])
 def ground(self,wood=False):
  r=self.r;self.rect((20,50,300,170),'#654a36' if wood else '#665137')
  for y in range(50,171):
   for x in range(20,301):
    if r.random()<.25:self.d.point((x,y),fill=r.choice(['#735b3b','#5a4633','#82603e','#4e4131','#8f7146']))
  if wood:
   for y in range(55,170,7):
    self.line((24,y,295,y),'#4b362b');self.line((24,y+1,295,y+1),'#966239')
    for x in range(25+(y%3)*8,297,27):
     self.line((x,y,x,y+6),'#49342b');self.d.point((x+2,y+2),fill='#362b27')
     self.line((x+5,y+4,x+15,y+4),'#af7742')
  for _ in range(100):
   x=r.randrange(20,300);y=r.randrange(55,170)
   if not wood:self.pebble(x,y,r.choice([2,3,4]))
 def pebble(self,x,y,s=3):
  self.d.polygon([(x,y),(x+s,y-1),(x+s+2,y+1),(x+s,y+3),(x,y+2)],fill='#6b6653');self.line((x,y,x+s,y-1),'#989172');self.line((x+1,y+3,x+s,y+3),'#3a3833')
 def cliffs(self):
  r=self.r
  for x in range(15,300,13):
   y=r.randrange(45,52);w=r.randrange(12,20);h=r.randrange(12,20)
   self.d.polygon([(x,y),(x+3,y-6),(x+w-4,y-7),(x+w,y-3),(x+w,y+h),(x+2,y+h)],fill='#3e3832')
   self.d.polygon([(x,y),(x+3,y-6),(x+w-4,y-7),(x+w,y-3),(x+w-2,y+4),(x+3,y+4)],fill='#796b4c')
   self.line((x+3,y-6,x+w-5,y-7),'#a18a5a');self.line((x+3,y+5,x+3,y+h-3),'#67543a')
   self.line((x+w-3,y+4,x+w-4,y+h),'#292d2c')
   for _ in range(7):
    xx=x+r.randrange(1,w);yy=y+r.randrange(2,h);self.d.point((xx,yy),fill=r.choice(['#4b4837','#6c6041','#8a7447']))
   if x%3==0:self.moss(x+2,y-2)
  for x in [15,297]:
   for y in range(65,170,12):
    self.rect((x,y,x+8,y+12),'#3a3830');self.line((x,y,x+6,y),'#716849');self.line((x+6,y+1,x+6,y+10),'#4f5037')
 def moss(self,x,y):
  for _ in range(15):
   dx=self.r.randrange(-4,7);dy=self.r.randrange(-2,5);self.rect((x+dx,y+dy,x+dx+1,y+dy),self.r.choice(['#61733e','#83954b','#394f32','#9e9c4f']))
  self.line((x+2,y+2,x+3,y+7,x+1,y+11),'#485d35')
 def torch(self,x,y):
  self.rect((x-1,y+1,x+1,y+8),'#73482d');self.rect((x-2,y+1,x+2,y+2),'#b49659')
  self.d.polygon([(x-2,y),(x-3,y-3),(x,y-8),(x+1,y-4),(x+3,y-2),(x+1,y+1)],fill='#e9772e');self.rect((x-1,y-4,x+1,y),'#ffc560');self.d.point((x,y-3),fill='#ffedba');self.lights.append((x,y,56,(1,.78,.5)))
 def player(self,x,y,color='#548f96',face=True):
  self.d.ellipse((x-2,y+21,x+15,y+26),fill='#39302d')
  rows=['....kkkkk.....','...khhHHHk....','..khHHHHHHk...','..khhHHHhhk...','..khssssshk...','...sskskss....','...sSSSSSs....','....sSSSs.....','...kkbbkk.....','..kcccccck....','.ksccllccsk...','.kSccllccSk...','..kccBBcck....','...cBBBBc.....','...cbbbbc.....','...kbbbbk.....','...kbkkbk.....','...kb..bk.....','...kB..Bk.....','..kkB..Bkk....','..kkk..kkk....']
  pal={'k':'#302a2d','h':'#714a30','H':'#bb8244','s':'#e3b77c','S':'#b07850','b':'#835b3b','B':'#514338','c':color,'l':'#a9c5ad'};self.spr(rows,pal,x,y)
 def chest(self,x,y,violet=False):
  p={'k':'#292932','a':'#a57240','b':'#754b30','c':'#cb975a','d':'#dfba75','m':'#62666c','h':'#a3a6a0','g':'#d8c17e','z':'#494033'}
  rows=['..kkkkkkkkkkkkkkkk..','.kaaaaaaaaaaaaaaaak.','kaccaccaccaccacccaak','kacbacbacbacbacbcaak','kabbbbbbbbbbbbbbbbak','kmmmmmmmmmmmmmmmmmmk','khbbbbbbbbbbbbbbbbhk','khbcccbbgggbbcccbbhk','khbbbbbbgdgbbbbbbbhk','khbbbbbbgggbbbbbbbhk','khbcccbbbbbbbbccc bhk','khbbbbbbbbbbbbbbbbhk','.kzzzzzzzzzzzzzzzzk.','..kkkkkkkkkkkkkkkk..']
  if violet:p.update(a='#927aa5',b='#594363',c='#a185b3',d='#cfafdd',g='#d1a1f5',z='#382d4b')
  self.d.ellipse((x-1,y+10,x+22,y+16),fill='#302b31');self.spr(rows,p,x,y)
  if violet:
   self.line((x+2,y+5,x+18,y+5),'#e6b8ff');self.lights.append((x+10,y+6,38,(.7,.5,1)))
   for dx,dy in [(3,-3),(15,-7),(20,0)]:self.d.point((x+dx,y+dy),fill='#cfa9ed')
 def shelf(self,x,y):
  self.rect((x,y,x+42,y+28),'#412f29')
  for yy in [y+1,y+14]:
   self.rect((x+2,yy,x+40,yy+10),'#292b2a')
   for xx in range(x+4,x+38,5):
    h=self.r.randrange(5,10);c=self.r.choice(['#948556','#576e6d','#945e51','#8d6f82','#798450']);self.rect((xx,yy+10-h,xx+3,yy+9),c);self.line((xx,yy+11-h,xx,yy+8),'#bbae82');self.d.point((xx+2,yy+3),fill='#c2a36d')
   self.rect((x+1,yy+11,x+41,yy+13),'#9a6a3e');self.line((x+2,yy+11,x+40,yy+11),'#c49154')
  self.line((x,y,x,y+29),'#ba864b');self.line((x+42,y,x+42,y+29),'#a77642')
 def crate(self,x,y):
  self.rect((x,y,x+15,y+13),'#b1874c');self.rect((x+1,y+3,x+14,y+12),'#785131')
  for yy in [y+4,y+8]:self.line((x+2,yy,x+13,yy),'#ab7542')
  self.line((x+2,y+2,x+13,y+11),'#d0a465',2);self.line((x,y+13,x+15,y+13),'#3c302c')
 def barrel(self,x,y):
  self.d.ellipse((x,y,x+12,y+6),fill='#a6824c');self.rect((x,y+3,x+12,y+15),'#785b36');self.d.ellipse((x,y+11,x+12,y+17),fill='#725330')
  for xx in [x+3,x+7,x+10]:self.line((xx,y+4,xx,y+14),'#b38a4e')
  for yy in [y+5,y+13]:self.line((x,yy,x+12,yy),'#555d5c');self.d.point((x+1,yy),fill='#969383')
 def rug(self,x,y,w=76,h=34):
  self.rect((x,y,x+w,y+h),'#553648');self.rect((x+2,y+2,x+w-2,y+h-2),'#ac705f');self.rect((x+5,y+5,x+w-5,y+h-5),'#5d414e')
  for yy in range(y+9,y+h-4,8):
   for xx in range(x+9,x+w-4,10):self.line((xx-3,yy,xx,yy-3,xx+3,yy,xx,yy+3,xx-3,yy),'#8e5d5d')
  for xx in range(x+3,x+w-2,4):self.line((xx,y-1,xx,y-2),'#b29572');self.line((xx,y+h+1,xx,y+h+2),'#b29572')
 def fish(self,x,y,c='#70aeb0'):
  self.spr(['.......cc......','c....ccccccc...','ccc.chhhcccccc.','.cccchhhccckccc','ccc.cccccccccc.','c....ccccccc...','.......cc......'],{'c':c,'h':'#c5d4ba','k':'#2a3339'},x,y)
 def table(self,x,y,w=45):
  self.rect((x+2,y+8,x+5,y+19),'#4b3529');self.rect((x+w-5,y+8,x+w-2,y+19),'#4b3529');self.rect((x,y,x+w,y+12),'#91653c')
  for yy in [y+1,y+5,y+9]:self.line((x+1,yy,x+w-1,yy),'#bc8850');self.line((x+2,yy+2,x+w-2,yy+2),'#6f4c31')
 def plant(self,x,y,gold=False):
  self.rect((x-1,y-1,x+1,y+8),'#7d7e3b')
  p={'k':'#34462d','a':'#a9ac4d' if gold else '#83a157','b':'#ded077' if gold else '#b6bd70','c':'#777131' if gold else '#4c733f'}
  self.spr(['....aa....','..aabbaa..','.aabbbcaa.','aabbbbccaa','.aabbccaa.','..aaccca..','...acca...'],p,x-5,y-6)
 def title(self,text):
  scale=2;wid=sum((len(GLYPHS[c].split('/')[0])+1)*scale for c in text)
  if wid>290:scale=1;wid=sum((len(GLYPHS[c].split('/')[0])+1) for c in text)
  x=(320-wid)//2;y=16
  for c in text:
   rows=GLYPHS[c].split('/')
   for j,row in enumerate(rows):
    for i,v in enumerate(row):
     if v=='1':self.rect((x+i*scale+1,y+j*scale+2,x+(i+1)*scale,y+(j+1)*scale+1),'#30272b')
   for j,row in enumerate(rows):
    for i,v in enumerate(row):
     if v=='1':self.rect((x+i*scale,y+j*scale,x+(i+1)*scale-1,y+(j+1)*scale-1),'#e8dbb5' if j<3 else '#bfaa86')
   x+=(len(rows[0])+1)*scale
 def finish(self,title,file,wip=False):
  px=self.im.load()
  for y in range(39,180):
   for x in range(320):
    rgb=px[x,y]
    if rgb==(9,12,19):continue
    l=[.54,.53,.56]
    for cx,cy,rad,col in self.lights:
     f=max(0,1-math.hypot(x-cx,(y-cy)*1.15)/rad);f=round(f*8)/8
     for c in range(3):l[c]+=f*col[c]*.85
    px[x,y]=tuple(min(255,int(rgb[c]*min(1.3,l[c]))) for c in range(3))
  self.title(title)
  if wip:
   self.rect((95,162,225,175),'#302529');self.rect((97,164,223,173),'#d4ae69')
   # Compact explicit label, same bitmap alphabet.
   old=self.d;tmp=Scene(0);tmp.im=Image.new('RGB',(320,180),'#d4ae69');tmp.d=ImageDraw.Draw(tmp.im);tmp.title('WORK IN PROGRESS')
   # draw directly at one pixel per glyph in dark ink
   x=117
   for c in 'WORK IN PROGRESS':
    rows=GLYPHS[c].split('/')
    for j,row in enumerate(rows):
     for i,v in enumerate(row):
      if v=='1':self.d.point((x+i,165+j),fill='#302529')
    x+=len(rows[0])+1
  output=self.im.resize((1280,720),Image.Resampling.NEAREST);output.save(OUT/file,optimize=True);return output

def ender():
 s=Scene(33);s.ground(True);s.cliffs();s.rug(120,92,86,45);s.shelf(40,69);s.shelf(231,67)
 for x in [40,59,78]:s.crate(x,138)
 for x in [232,247,262]:s.barrel(x,139)
 s.table(225,112,47);s.rect((235,114,252,119),'#cab87c');s.line((238,115,247,118),'#8f7752')
 s.chest(163,101,True);s.player(151,120);s.chest(95,76);s.chest(118,76)
 s.torch(96,78);s.torch(278,112);s.torch(102,137)
 return s.finish('ENDER STASH','enderstash_logo.png')
def vehicle():
 s=Scene(44);s.ground();s.cliffs()
 s.d.polygon([(21,69),(155,69),(164,92),(146,112),(160,142),(139,170),(21,170)],fill='#294958')
 for _ in range(270):
  x=s.r.randrange(23,151);y=s.r.randrange(73,169);s.line((x,y,x+s.r.randrange(1,5),y),s.r.choice(['#35596a','#416c79','#567f83','#203d50']))
 for x,y in [(29,81),(144,83),(134,156)]:s.moss(x,y)
 # Boat with curved hull, planked interior and tiny seated explorer.
 s.d.polygon([(63,101),(69,92),(93,92),(101,101),(95,117),(69,117)],fill='#422f29');s.d.polygon([(66,100),(72,95),(92,95),(98,101),(92,113),(71,113)],fill='#b98449')
 for y in range(98,112,3):s.line((69,y,94,y),'#e0b36c');s.line((72,y+1,91,y+1),'#825637')
 s.player(76,87);s.line((95,107,109,116),'#bb915d',2);s.line((106,114,112,119),'#d1ad73',3)
 for y in [120,123,126]:s.line((65,y,97,y),'#9fb4a1')
 # Track around room, with marked kerb and fast kart.
 s.rect((181,70,276,162),'#58584c');s.rect((207,92,248,145),'#69593b')
 for y in range(72,159,8):s.rect((182,y,185,y+3),'#c2b792');s.rect((271,y,274,y+3),'#bf845b')
 for x in range(192,270,14):s.line((x,82,x+6,82),'#bfb88c')
 s.rect((243,110,266,120),'#87473c');s.rect((246,107,260,116),'#c78250');s.rect((249,110,258,114),'#e8bf75')
 for x in [243,260]:
  for y in [108,119]:s.rect((x,y,x+5,y+5),'#2c3031');s.line((x+1,y+1,x+4,y+1),'#747677')
 s.player(249,93);s.line((244,132,258,132),'#c9c09d');s.line((249,136,260,136),'#b1aa90');s.barrel(170,130);s.torch(173,91);s.torch(279,142)
 s.lights.append((88,110,72,(.55,.71,.79)))
 return s.finish('VEHICLE SPEED','vehiclespeed_logo.png')
def ore():
 s=Scene(52);s.ground();s.cliffs()
 # Boulder has actual faceted material clusters rather than a giant icon.
 s.d.polygon([(144,104),(145,83),(156,73),(180,75),(192,88),(189,106),(176,116),(153,113)],fill='#434d4f')
 s.d.polygon([(145,83),(156,73),(180,75),(183,89),(162,92)],fill='#929985');s.d.polygon([(147,84),(162,92),(155,110),(144,104)],fill='#697365');s.d.polygon([(163,93),(183,89),(187,103),(176,112),(156,110)],fill='#58685f')
 for x,y in [(154,82),(175,81),(164,100),(182,97)]:
  s.d.polygon([(x,y),(x+5,y-3),(x+9,y+2),(x+6,y+7),(x+1,y+5)],fill='#4daba3');s.line((x+1,y,x+5,y-2,x+7,y+1),'#a4dbc1');s.line((x+2,y+5,x+6,y+6),'#327d81')
 for _ in range(50):
  x=s.r.randrange(148,187);y=s.r.randrange(80,108)
  if s.r.random()<.5:s.d.point((x,y),fill='#7a8475')
 for x,y,flip in [(116,89,False),(158,121,True)]:
  s.rect((x,y,x+20,y+15),'#655544');s.rect((x+2,y+1,x+18,y+10),'#b18849');s.line((x+3,y+2,x+16,y+2),'#e0bc6e');s.rect((x+5,y+4,x+12,y+9),'#69716b')
  if not flip:s.d.polygon([(x+20,y+3),(x+28,y+7),(x+20,y+12)],fill='#a8b6ab')
  else:s.d.polygon([(x+3,y),(x+9,y-10),(x+15,y)],fill='#a8b6ab')
  for xx in [x+2,x+15]:s.rect((xx,y+13,xx+4,y+17),'#303835')
 # Conveyor, sorted chunks, workbench, cables.
 for x in range(48,155,10):
  s.rect((x,126,x+9,139),'#333c37');s.line((x+1,129,x+8,129),'#787b5d');s.line((x+1,136,x+8,136),'#787b5d')
  if x%3:s.d.polygon([(x+2,131),(x+5,129),(x+8,132),(x+5,135)],fill='#66b4a0')
 s.chest(34,124);s.table(226,78);s.crate(239,139);s.crate(256,139);s.player(202,116);s.torch(90,79);s.torch(265,117);s.lights.append((169,100,60,(.53,.8,.68)))
 return s.finish('INFINITE ORE BOULDERS','infiniteore_logo.png')
def golden():
 s=Scene(65);s.ground();s.cliffs()
 for y in [87,109,131]:
  for x in [49,72,95,118]:
   s.rect((x-8,y-5,x+9,y+12),'#4b3a2d')
   for _ in range(15):s.d.point((x+s.r.randrange(-7,8),y+s.r.randrange(-4,11)),fill='#816342')
   s.plant(x,y,gold=(x in [72,95]))
 # Cooking alcove, hanging pans and pantry.
 s.shelf(228,67);s.table(194,123,65)
 for x in [201,220,239]:
  s.d.ellipse((x,126,x+12,131),fill='#cfbe98');s.d.ellipse((x+2,126,x+10,129),fill='#b7a359');s.d.point((x+4,127),fill='#ead685')
 s.rect((178,94,199,113),'#5b5347');s.d.ellipse((175,84,202,99),fill='#8b9689');s.d.ellipse((178,85,199,95),fill='#b7a56b');s.d.ellipse((180,88,197,94),fill='#75654a')
 s.rect((184,110,192,115),'#e59842');s.rect((187,109,189,114),'#ffd272')
 s.line((183,79,185,74,183,70),'#b9b8a0');s.line((193,79,191,74,194,68),'#a7aa95');s.player(149,113);s.barrel(247,142);s.torch(37,82);s.torch(271,112);s.lights.append((95,114,65,(.9,.77,.4)))
 return s.finish('GOLDEN CHANCE','goldenchance_logo.png',True)
def stim():
 s=Scene(79);s.ground();s.cliffs()
 # Ruined flagstone arena, roots and broken columns.
 for y in range(83,154,15):
  for x in range(72+(y%2)*8,252,19):
   s.d.polygon([(x,y),(x+15,y-1),(x+17,y+9),(x+13,y+12),(x,y+11)],fill=s.r.choice(['#66695a','#777965','#5c6458']))
   s.line((x+1,y+1,x+13,y),'#90917a');s.line((x+5,y+4,x+7,y+7,x+5,y+10),'#4b524b')
 for x,y in [(47,84),(266,136),(245,76)]:
  s.rect((x,y,x+12,y+24),'#5d6358');s.rect((x-2,y,x+14,y+4),'#94957a');s.line((x+3,y+5,x+3,y+21),'#868a72');s.moss(x+5,y)
 s.player(138,116,'#759ba8')
 # Small red creature and contact spark; motion sound arcs integrated into scene.
 s.d.ellipse((173,126,193,139),fill='#743d39');s.d.ellipse((175,125,190,134),fill='#b56b52');s.d.ellipse((180,127,187,132),fill='#d99c6e');s.d.point((187,127),fill='#e0d2a2')
 s.line((150,124,169,116),'#bac6b1',2);s.line((148,122,153,129),'#bd9256',2)
 for x,y in [(171,115),(173,119),(176,113)]:s.line((x-2,y,x+2,y),'#ffe2a2');s.line((x,y-2,x,y+2),'#ffe2a2')
 for r in [15,21,27]:s.d.arc((168-r,115-r,168+r,115+r),215,295,fill='#e2c49a',width=1)
 s.crate(77,142);s.barrel(244,113);s.torch(82,79);s.torch(229,138);s.lights.append((161,119,45,(.9,.77,.58)))
 return s.finish('STIM HITS','stimhits_logo.png')
def armor():
 s=Scene(87);s.ground(True);s.cliffs();s.rug(119,112,91,40);s.shelf(42,69);s.table(224,111,57)
 # Armor mannequins on wooden bases, six-tone original sprites.
 for x,c,hi,lo in [(111,'#985c65','#d6948e','#653e50'),(161,'#538f97','#9ac5b8','#37606d'),(211,'#8c71a4','#c1a2cb','#5f4b76')]:
  s.rect((x+5,83,x+7,110),'#795631');s.rect((x,109,x+13,112),'#ad8851')
  rows=['....hh....','...haah...','..habbah..','...baab...','..kaaaak..','.kahaahak.','kaaaahaaak','kkabaabbkk','..abaaab..','..abbbab..','..abbbab..','...kkkk...']
  s.spr(rows,{'k':'#30333a','a':c,'h':hi,'b':lo},x+1,84)
  s.line((x+3,78,x+10,78),'#a89270');s.line((x+6,75,x+6,82),'#c9b18a')
 for i,c in enumerate(['#bd6971','#d5a362','#91a46c','#6ba7ac','#a384bd']):
  x=228+i*10;s.rect((x,108,x+6,115),c);s.line((x+1,107,x+5,107),'#dccb97');s.d.point((x+1,109),fill='#e0cfb7')
 s.player(156,123,'#b084b3');s.barrel(64,137);s.crate(85,139);s.torch(99,80);s.torch(275,97);s.lights.append((165,119,65,(.8,.7,.8)))
 return s.finish('ARMOR DYE','armordye_logo.png')
def shop(fish=False):
 s=Scene(100 if fish else 101);s.ground(not fish);s.cliffs()
 if fish:
  s.d.polygon([(20,122),(68,121),(80,127),(130,123),(137,148),(121,171),(20,171)],fill='#2d4e5b')
  for _ in range(150):
   x=s.r.randrange(24,122);y=s.r.randrange(129,169);s.line((x,y,x+s.r.randrange(1,5),y),s.r.choice(['#477281','#365b6b','#769792']))
  for x in range(141,280,12):
   s.rect((x,137,x+10,167),'#8a6540');s.line((x+1,138,x+1,165),'#b5894b');s.line((x+7,139,x+7,165),'#624b31');s.d.point((x+3,140),fill='#403f33')
  for x in [38,77]:s.rect((x,74,x+2,103),'#8e7349')
  s.line((38,78,79,78),'#c3ac78')
  for x in [44,57,70]:s.line((x,78,x,84),'#c3ac78');s.fish(x-3,84,'#82a7a4')
  s.barrel(246,92);s.barrel(263,96);s.crate(249,113)
 else:
  s.shelf(39,72);s.shelf(240,73)
 # Merchant's decorated counter and small shop clock.
 s.table(98,113,126)
 for x in range(99,224,7):s.rect((x,62,x+6,72),'#b17c52' if (x//7)%2 else '#b6b293');s.line((x,73,x+3,76,x+6,73),'#90704a')
 for x in [97,224]:s.rect((x,72,x+2,130),'#865c36');s.line((x,74,x,126),'#c59759')
 s.player(149,88,'#948169');s.player(159,139,'#628b96')
 if fish:
  for x,c in [(108,'#80b3af'),(131,'#cba56e'),(155,'#a28da9'),(179,'#8ea266'),(202,'#ce8d76')]:
   s.d.ellipse((x-2,116,x+15,122),fill='#bdaf88');s.fish(x,111,c)
  s.barrel(230,144);s.barrel(246,149);s.line((52,126,53,151),'#b99b69');s.line((53,127,65,122,71,126),'#b99b69')
  s.moss(130,158);s.moss(30,118)
 else:
  for x in [109,129,179,199]:
   s.rect((x,104,x+9,115),'#aa8760');s.rect((x+1,106,x+8,113),'#788361');s.line((x+2,105,x+7,105),'#d4c49b')
  s.crate(56,133);s.crate(74,139);s.crate(92,143)
  s.d.ellipse((174,78,191,95),fill='#bda773',outline='#65553d');s.line((183,81,183,87,187,89),'#4c4940');s.d.point((183,80),fill='#e1cf9b')
  # Filled restock crates beside the shelves, no giant floating clock.
  for x in [243,261]:s.crate(x,135);s.rect((x+3,132,x+8,137),'#929569')
 s.torch(87,84);s.torch(233,133);s.lights.append((160,113,65,(.89,.79,.56)))
 return s.finish('FISH SELLER' if fish else 'MERCHANT RESTOCK','fishseller_logo.png' if fish else 'merchantrestock_logo.png')
if __name__=='__main__':
 images=[ender(),vehicle(),ore(),golden(),stim(),armor(),shop(),shop(True)]
 montage=Image.new('RGB',(1280,1440))
 for i,im in enumerate(images):montage.paste(im.resize((640,360),Image.Resampling.NEAREST),((i%2)*640,(i//2)*360))
 dst=OUT.parent/'build/native_c_montage.png';dst.parent.mkdir(parents=True,exist_ok=True);montage.save(dst);montage.resize((640,720),Image.Resampling.NEAREST).save(OUT.parent/'build/native_c_thumbnails.png');print(dst)
