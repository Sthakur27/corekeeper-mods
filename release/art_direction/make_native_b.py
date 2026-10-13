"""Eight original Core Keeper-inspired world illustrations, reproducible with Pillow.
All sprites, lettering and scenery are authored here. No game assets or screenshot pixels.
Native canvas 384x216, nearest-neighbor 4x export. Run from any directory.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageColor
import random,math
OUT=Path(__file__).resolve().parents[1]
W,H=384,216
FONT={
'A':'01110/11011/11011/11111/11011/11011/11011','B':'11110/11011/11011/11110/11011/11011/11110','C':'01111/11000/11000/11000/11000/11000/01111','D':'11110/11011/11011/11011/11011/11011/11110','E':'11111/11000/11000/11110/11000/11000/11111','F':'11111/11000/11000/11110/11000/11000/11000','G':'01111/11000/11000/11011/11011/11011/01111','H':'11011/11011/11011/11111/11011/11011/11011','I':'11111/00100/00100/00100/00100/00100/11111','J':'00111/00011/00011/00011/11011/11011/01110','K':'11011/11010/11100/11100/11100/11010/11011','L':'11000/11000/11000/11000/11000/11000/11111','M':'11011/11111/11111/11011/11011/11011/11011','N':'11011/11111/11111/11011/11011/11011/11011','O':'01110/11011/11011/11011/11011/11011/01110','P':'11110/11011/11011/11110/11000/11000/11000','Q':'01110/11011/11011/11011/11011/01110/00011','R':'11110/11011/11011/11110/11100/11010/11011','S':'01111/11000/11000/01110/00011/00011/11110','T':'11111/00100/00100/00100/00100/00100/00100','U':'11011/11011/11011/11011/11011/11011/01110','V':'11011/11011/11011/11011/11011/01010/00100','W':'11011/11011/11011/11011/11111/11111/01010','X':'11011/11011/01010/00100/01010/11011/11011','Y':'11011/11011/01010/00100/00100/00100/00100','Z':'11111/00011/00110/00100/01100/11000/11111'}
P={'o':'#271e21','a':'#304627','b':'#526c31','c':'#79933f','d':'#a8b458','e':'#d5ce81','r':'#913c32','s':'#bd583b','t':'#e8824c','u':'#f5b66e','w':'#d6c8a1','h':'#60432d','i':'#8d623d','j':'#b9864a','k':'#ddb769','n':'#384f54','m':'#5c7d79','f':'#dea177','g':'#ffd29d','v':'#583b57','q':'#835774','z':'#bf80a0','1':'#b7c3b5','2':'#82978e','3':'#4e6867','4':'#30454b','5':'#e9e6c2'}
PLAYER=['.....oooo.......','....ohhhio......','...ohhijjjo.....','..ohhjjkjjjo....','..ojjkkkjjjjo...','...oooffggo.....','.....ofoggo.....','.....offggo.....','....onmmno......','...onmmmniho....','..ofnmmnnijho...','..offnmnnoo.....','...oonnno.......','....ovvvvo......','....ovvovo......','....ohhoho......','....ojhojho.....','.....oo.oo......']
CROP=['.......b........','....bbbc..bb....','...bccdc.bccb...','....bcdbbccb....','..bbbccbccbbb...','.bcccdccdcccb...','.bbccccccccbb...','..bbrssrsrbb....','..orsstrtsso....','.orsstuttrrso...','.orstttrrrsso...','..orstssrrso....','...orrssrro.....','....ooooo.......']
SPROUT=['..bc....cb..','..bdc..cdb..','...bccccb...','....abba....','.....aa.....','.....ao.....']
PET=['..oo....oo....','..obbo..obo...','.obccbobbbo...','.obdcbbbbbo...','.obb5obb5bo...','.obboobbooo...','..obbbwwbo....','...obbbbbo....','..obbbbbbbo...','..obobbbbobo..','..oo.oooo.oo..']
POTION=['...hjjh...','...hiih...','....ww....','...w55w...','..w5mm5w..','.w5mmmn5w.','.wmm5mnnw.','.wmmmmnnw.','..wnnnnw..','...wwww...']
BAT=['oo.........oo','.qzo.....ozq.','..qzqo.oqzq..','...qzqqqzq...','....q5q5q....','.....qqq.....','......q......']
GEM=['...55...','..5mm5..','.5mmmm5.','5mmmnmn5','.5mnnn5.','..5nn5..','...55...']
BAG=['...hjjh...','....ii....','...ijji...','..ijjjii..','.ijjjjjii.','.ijjkkjii.','.ijjkkjii.','..iiiiii..','...oooo...']
class Scene:
 def __init__(self,title,seed,cool=False):
  self.r=random.Random(seed);self.im=Image.new('RGB',(W,H),'#100e15');self.d=ImageDraw.Draw(self.im);self.title=title;self.lights=[]
  self.mask=Image.new('1',(W,H));md=ImageDraw.Draw(self.mask)
  poly=[(15,72),(31,72),(31,51),(61,51),(61,42),(304,42),(304,51),(349,51),(349,64),(369,64),(369,178),(350,178),(350,198),(298,198),(298,208),(70,208),(70,198),(33,198),(33,183),(15,183)]
  md.polygon(poly,fill=1)
  pal=['#4d4b43','#565044','#655b49','#5a5548'] if cool else ['#775537','#785437','#725035','#825b3b']
  for y in range(40,H):
   for x in range(W):
    if self.mask.getpixel((x,y)):self.d.point((x,y),fill=self.r.choice(pal))
  for _ in range(1700):
   x,y=self.r.randrange(17,368),self.r.randrange(44,206)
   if self.mask.getpixel((x,y)):
    self.rect((x,y,x+self.r.choice([0,1,2]),y),self.r.choice(['#ad804e','#4d382a','#8d6a43','#9a784b'] if not cool else ['#8f8566','#383f39','#6c7054']))
  # Jagged layered cliff rim, each rock block has its own profile and mineral flecks.
  for x in range(15,369,9):
   ys=[y for y in range(36,209) if self.mask.getpixel((x,y))]
   if ys:self.rock(x,min(ys)-9,9,15)
  for y in range(78,179,11):
   self.rock(11,y,10,13);self.rock(361,y,10,13)
 def rect(self,b,c):
  x,y,xx,yy=b;self.d.rectangle((min(x,xx),min(y,yy),max(x,xx),max(y,yy)),fill=c)
 def dot(self,x,y,c):self.d.point((x,y),fill=c)
 def rock(self,x,y,w=12,h=15):
  self.rect((x,y+3,x+w-1,y+h),'#312922');self.rect((x+1,y+1,x+w-2,y+h-3),'#514230');self.rect((x+2,y,x+w-3,y+4),'#7b6747')
  self.rect((x+2,y+5,x+4,y+h-4),'#8b6f45');self.rect((x+w-4,y+6,x+w-2,y+h-2),'#3e3126')
  self.rect((x+4,y+2,x+w-3,y+2),'#ad8a53');self.dot(x+5,y+8,'#745738');self.rect((x+1,y+h-1,x+w-1,y+h+1),'#241e1c')
 def spr(self,rows,x,y,colors=None,shadow=True):
  pal=dict(P);pal.update(colors or {})
  if shadow:self.d.ellipse((x+1,y+len(rows)-3,x+len(rows[0])-1,y+len(rows)),fill='#3b2b25')
  for yy,row in enumerate(rows):
   for xx,c in enumerate(row):
    if c in pal:self.dot(x+xx,y+yy,pal[c])
 def human(self,x,y,colors=None,tool=False):
  self.spr(PLAYER,x,y,colors)
  # Belt buckle, steel tool strap and knapsack clasp.
  self.dot(x+7,y+12,'#d4b56f');self.rect((x+10,y+9,x+10,y+11),'#c39c62')
  if tool:
   self.d.line((x+11,y+9,x+24,y+3),fill='#503a27',width=2);self.d.line((x+12,y+8,x+24,y+3),fill='#bc9256');self.rect((x+23,y,x+29,y+2),'#81958a');self.rect((x+23,y,x+29,y),'#d2d4ba')
 def torch(self,x,y,rad=65):
  self.lights.append((x,y,rad,'warm'));self.rect((x-1,y+1,x+1,y+10),'#3b291f');self.rect((x,y+2,x,y+9),'#ae773f')
  self.spr(['..t..','.tut.','tu5ut','tuuut','.tut.','..s..'],x-2,y-5,shadow=False)
 def grass(self,x,y):
  for _ in range(9):
   xx=x+self.r.randrange(-4,5);h=self.r.randrange(3,9)
   self.d.line((xx,y,xx+self.r.randrange(-2,3),y-h),fill=self.r.choice(['#626238','#8b823d','#454e2b','#a1954e']))
 def details(self):
  for x,y in [(31,68),(60,188),(333,188),(355,78),(348,165),(27,170),(88,48),(287,49)]:self.grass(x,y)
  for x,y in [(46,173),(32,85),(338,71),(321,192),(79,192),(351,147)]:
   self.d.ellipse((x,y,x+6,y+4),fill='#3c3428');self.rect((x+1,y,x+5,y+1),'#a28c63');self.dot(x+2,y,'#c4ad79')
  for x,y in [(54,56),(343,175),(28,139),(320,51)]:
   self.spr(['..ss...','.stuts.','stuttts','.rrrro.','..wi...','..wi...','..oo...'],x,y)
  # Fine roots hanging from the caprock.
  for x in [51,94,269,323]:
   self.d.line((x,51,x+2,57,x,62,x+2,67),fill='#3b3028');self.d.line((x+1,53,x+5,57,x+6,62),fill='#735435')
 def floor(self,x,y,w,h):
  for yy in range(y,y+h,8):
   for xx in range(x,x+w,18):
    offset=0 if (yy-y)//8%2==0 else -9
    a=max(x,xx+offset);b=min(x+w-1,xx+offset+16)
    if b<a:continue
    self.rect((a,yy,b,yy+6),'#7b5334');self.rect((a+1,yy,b,yy),'#b1844e');self.rect((a+2,yy+3,b,yy+3),'#8c623c');self.dot(a+2,yy+5,'#3d2c24')
 def fence(self,x,y,w):
  self.rect((x,y,x+w,y+2),'#4a3124');self.rect((x,y,x+w,y),'#a17a45')
  for xx in range(x,x+w+1,13):
   self.rect((xx,y-4,xx+3,y+6),'#4b3324');self.rect((xx,y-4,xx+2,y+4),'#a17a45');self.dot(xx+1,y-4,'#d0a861')
 def chest(self,x,y,opened=False):
  self.rect((x,y,x+20,y+13),'#2d251e');self.rect((x+1,y+1,x+19,y+11),'#99683b')
  for xx in [x+3,x+8,x+13,x+17]:self.rect((xx,y+2,xx,y+10),'#62462e')
  self.rect((x+1,y+1,x+19,y+2),'#c59b58');self.rect((x,y+8,x+20,y+9),'#c09651');self.rect((x+8,y+6,x+11,y+10),'#e2bd62')
  if opened:
   self.rect((x,y-8,x+20,y-1),'#5f432b');self.rect((x+1,y-7,x+19,y-3),'#a8773e');self.rect((x+1,y,x+19,y+4),'#231e1d');self.rect((x+2,y+3,x+18,y+3),'#f2cb6d')
 def barrel(self,x,y):
  self.d.ellipse((x,y,x+13,y+7),fill='#ba8749');self.rect((x,y+3,x+13,y+14),'#7b5435')
  for xx in range(x+2,x+13,3):self.rect((xx,y+3,xx,y+12),'#a47743')
  self.rect((x,y+5,x+13,y+6),'#444940');self.rect((x,y+11,x+13,y+12),'#444940');self.d.ellipse((x+1,y,x+12,y+4),fill='#4b3b2b');self.rect((x+3,y+1,x+10,y+2),'#725638')
 def pond(self,poly):
  self.d.polygon(poly,fill='#293642');pm=Image.new('1',(W,H));ImageDraw.Draw(pm).polygon(poly,fill=1)
  for _ in range(850):
   x,y=self.r.randrange(W),self.r.randrange(H)
   if pm.getpixel((x,y)):self.rect((x,y,x+self.r.randrange(1,5),y),self.r.choice(['#355263','#467084','#2d4757','#527e8a']))
  for x,y in poly:self.rock(x-2,y,7,7)
 def crop(self,x,y,stage=2):
  self.rect((x,y,x+15,y+17),'#37271f');self.rect((x,y,x+15,y),'#b4804a');self.rect((x+1,y+2,x+14,y+16),'#6c492c')
  for yy in [y+5,y+10,y+15]:
   self.rect((x+2,yy,x+13,yy),'#442f21');self.rect((x+3,yy-1,x+12,yy-1),'#90653a')
  for _ in range(8):self.dot(x+self.r.randrange(1,15),y+self.r.randrange(2,16),self.r.choice(['#bb8c50','#36271d','#9a733f']))
  if stage==2:self.spr(CROP,x,y-2)
  elif stage==1:self.spr(SPROUT,x+2,y+7)
 def title_text(self):
  text=self.title;s=2
  if len(text)*12>360:s=1
  width=len(text)*6*s-s;x0=(W-width)//2;y0=15 if s==2 else 19
  for i,c in enumerate(text):
   if c not in FONT:continue
   for yy,row in enumerate(FONT[c].split('/')):
    for xx,v in enumerate(row):
     if v=='1':
      x,y=x0+i*6*s+xx*s,y0+yy*s
      self.rect((x+1,y+2,x+s,y+s+2),'#604329');self.rect((x,y,x+s-1,y+s-1),'#efd093' if yy<4 else '#cda565')
 def bush(self,x,y,flower=False):
  # Overlapping hand-shaped leaf clusters, each with a lit rim and deep underside.
  self.d.ellipse((x-3,y+11,x+24,y+20),fill='#342c22')
  for dx,dy in [(3,7),(14,7),(8,2),(0,12),(16,12),(8,10)]:
   self.d.polygon([(x+dx-4,y+dy),(x+dx-2,y+dy-4),(x+dx+3,y+dy-5),(x+dx+7,y+dy-1),(x+dx+6,y+dy+5),(x+dx+1,y+dy+7),(x+dx-4,y+dy+4)],fill='#34462a')
   self.d.polygon([(x+dx-3,y+dy),(x+dx-1,y+dy-3),(x+dx+3,y+dy-3),(x+dx+5,y+dy),(x+dx+2,y+dy+3),(x+dx-2,y+dy+2)],fill='#65773b')
   self.rect((x+dx-1,y+dy-3,x+dx+2,y+dy-2),'#9b9e50');self.dot(x+dx+3,y+dy,'#bcc16a');self.rect((x+dx,y+dy+4,x+dx+3,y+dy+4),'#455b30')
   if flower and self.r.random()<.6:
    self.rect((x+dx,y+dy,x+dx+2,y+dy+1),'#b77672');self.dot(x+dx+1,y+dy,'#e6b39b')
 def shelves(self,x,y,w=41):
  self.rect((x,y,x+w,y+30),'#302723');self.rect((x+2,y+2,x+w-2,y+27),'#4f3d2d')
  for yy in [y+3,y+16,y+28]:self.rect((x,yy,x+w,yy+2),'#b3864e')
  for i in range(3):
   self.spr(POTION,x+4+i*12,y+6,{'m':['#698c74','#a47767','#8b80a4'][i]})
  for xx in range(x+4,x+w-4,5):
   self.rect((xx,y+20,xx+3,y+27),self.r.choice(['#758379','#a88b5c','#8d635d']));self.dot(xx+1,y+21,'#dac593')
 def stool(self,x,y):
  self.rect((x+1,y+5,x+2,y+10),'#4d3526');self.rect((x+9,y+5,x+10,y+10),'#4d3526');self.rect((x,y,x+11,y+6),'#835a35');self.rect((x+1,y,x+10,y+1),'#c39756');self.rect((x+2,y+3,x+9,y+3),'#9d733f')
 def rug(self,x,y,w,h):
  self.rect((x,y,x+w,y+h),'#534640');self.rect((x+1,y+1,x+w-1,y+h-1),'#9a7152');self.rect((x+3,y+3,x+w-3,y+h-3),'#6c4d3a')
  for xx in range(x+5,x+w-4,6):
   for yy in range(y+5,y+h-4,6):self.d.polygon([(xx,yy-2),(xx+2,yy),(xx,yy+2),(xx-2,yy)],fill='#b08c57')
 def finish(self,name):
  self.details();px=self.im.load()
  for y in range(36,H):
   for x in range(W):
    strengths=[(max(0,1-math.hypot(x-lx,y-ly)/rad),kind) for lx,ly,rad,kind in self.lights]
    strength,kind=max(strengths,default=(0,'warm'));strength=round(strength*12)/12
    r,g,b=px[x,y];v=.46+strength*.79
    px[x,y]=(min(255,int(r*v+strength*(10 if kind=='warm' else 0))),min(255,int(g*v)),min(255,int(b*(.50+strength*.65))))
  # Bright flame cores stay visibly luminous after material lighting.
  for x,y,rad,kind in self.lights:
   if kind=='warm' and (x,y) in getattr(self,'torches',[]):self.dot(x,y-2,'#fff0b7')
  self.title_text();path=OUT/(name+'_logo.png');self.im.resize((1536,864),Image.Resampling.NEAREST).save(path,optimize=True)
  assert path.stat().st_size<1_000_000
  return self.im

def farm():
 s=Scene('AUTO REPLANT',216)
 s.pond([(27,93),(35,78),(72,78),(82,89),(82,124),(68,138),(34,137),(24,124)])
 s.bush(33,151,True);s.bush(47,55);s.bush(333,100);s.bush(329,152);s.floor(91,170,227,17);s.fence(111,65,201);s.fence(111,166,201)
 for row in range(4):
  for col in range(10):
   stage=2 if row<2 else 1
   if (row,col) in [(1,3),(1,4),(2,3)]:stage=0
   s.crop(118+col*19,76+row*21,stage)
 s.human(176,102,tool=True);s.spr(SPROUT,179,130);s.spr(['.bb..','rsst.','suttr','rssrr','.oo..'],199,112)
 s.chest(85,71);s.barrel(84,103);s.torch(101,132);s.torch(315,79);s.torch(274,187)
 s.lights.append((184,118,76,'warm'))
 return s.finish('autoreplant')

def fishing():
 s=Scene('BETTER FISHING LOOT',239,True)
 s.pond([(151,64),(218,56),(304,64),(344,92),(345,159),(308,183),(200,186),(155,167),(145,115)])
 s.bush(46,59);s.bush(35,139,True);s.bush(113,63);s.bush(88,178);s.bush(327,173);s.floor(89,122,120,16)
 for x in range(91,210,18):s.rect((x,135,x+3,147),'#3c2d24')
 s.human(131,110);s.d.line((143,118,164,91),fill='#bfa469');s.d.line((164,91,241,111,243,138),fill='#b7c4b1')
 s.spr(GEM,239,129,{'m':'#6ca9b7','n':'#447d97'})
 # A small caught helmet and fishing bag on shore, believable inventory scale.
 s.spr(['..22222..','.2111112.','211511112','211111112','222222222','23..2..32','.o.....o.'],114,143)
 s.spr(BAG,132,145);s.chest(69,106,True);s.barrel(77,157)
 s.spr(['.....cc..','..mmmmm..','mmwmmmnn.','..nnnnn..','.....nn..'],251,155)
 s.torch(94,89);s.torch(117,179);s.lights.extend([(163,116,97,'cool'),(282,121,80,'cool')])
 # Bioluminescent mineral clusters on the far bank.
 for x,y in [(317,77),(329,168),(216,57)]:
  s.spr(GEM,x,y,{'m':'#608c99','n':'#36627d','5':'#8fc2c8'});s.lights.append((x,y,38,'cool'))
 return s.finish('betterfishingloot')

def quick():
 s=Scene('QUICK BUFF',249)
 s.floor(65,69,253,115);s.rug(153,113,94,51);s.shelves(230,62);s.stool(149,105);s.stool(195,108);s.bush(31,83);s.bush(325,129,True);s.chest(68,175);s.barrel(301,164)
 # A cozy prep camp with bed, cooking hearth and table laid with potions and meals.
 s.rect((78,76,112,102),'#49392c');s.rect((80,76,109,99),'#73817a');s.rect((82,78,106,83),'#cdc6aa')
 for yy in range(85,99,3):s.rect((81,yy,109,yy),'#577069')
 s.rect((124,82,220,106),'#422d22');s.rect((123,80,221,100),'#ae7743')
 for yy in [83,89,95]:s.rect((125,yy,219,yy),'#734e31')
 for x in [133,152,174,195]:s.spr(POTION,x,83,{'m':'#9e5764' if x<160 else '#689773','n':'#673d4b' if x<160 else '#45614c'})
 s.human(171,115);s.human(204,123,{'n':'#725039','m':'#a17a4c'})
 for x,y in [(151,125),(230,109)]:
  s.d.ellipse((x,y,x+15,y+7),fill='#bcb89f');s.spr(['..jjjj..','.jkjujj.','jjujjjjj','.iiiihh.'],x+3,y-1)
 # Firepit and cooking pot, with split logs and stones.
 for x,y in [(108,137),(114,134),(120,137),(121,145),(108,147)]:s.rock(x,y,6,5)
 s.spr(['..tt..','.tuut.','tu55ut','tuuuut','.ssss.'],111,135);s.lights.append((115,140,70,'warm'))
 s.d.arc((109,133,125,146),0,180,fill='#2b2928',width=2);s.rect((111,133,123,137),'#3b4441')
 s.chest(272,87);s.barrel(275,147);s.spr(BAG,249,156)
 s.torch(69,124);s.torch(295,70);s.torch(234,173);s.lights.append((177,124,86,'warm'))
 return s.finish('quickbuff')

def seller():
 s=Scene('POTION SELLER',269)
 s.floor(56,64,278,122);s.bush(33,161);s.bush(327,103,True);s.chest(61,171);s.barrel(317,174);s.shelves(211,62);s.stool(184,166)
 # Two stalls, bottle shelves and customers, rather than detached product icons.
 for x,y,colors in [(82,78,['#805246','#a96b52','#d4a577']),(216,104,['#496465','#638684','#b4bbb0'])]:
  s.rect((x,y,x+81,y+60),'#312823');s.rect((x+3,y+4,x+78,y+50),'#51402e')
  for xx in range(x,x+82,10):
   c=colors[(xx-x)//10%2];s.rect((xx,y-8,xx+9,y+1),c);s.rect((xx+1,y+2,xx+8,y+4),c)
  s.rect((x,y,x+2,y+63),'#ac7948');s.rect((x+79,y,x+81,y+63),'#ac7948')
  s.human(x+32,y+18,{'h':'#463948','i':'#685469','j':'#876982','k':'#b699a4','n':'#4b3c56','m':'#705a77'})
  s.rect((x+1,y+39,x+80,y+57),'#795030');s.rect((x+1,y+39,x+80,y+41),'#c19152')
  for xx in range(x+6,x+80,16):s.rect((xx,y+43,xx,y+55),'#483324')
  for i in range(5):s.spr(POTION,x+7+i*13,y+29,{'m':['#ab5360','#649385','#8874a8','#a49153','#56758a'][i]})
 s.human(144,145);s.human(183,109,{'n':'#604b62','m':'#98779b'})
 s.chest(46,95);s.barrel(49,138);s.chest(299,72);s.spr(BAG,321,157)
 for x,y in [(160,138),(170,137),(165,142)]:s.spr(['.kk.','kjjk','kjjk','.kk.'],x,y)
 s.torch(62,72);s.torch(191,79);s.torch(310,169);s.lights.extend([(127,109,78,'warm'),(254,139,73,'warm')])
 return s.finish('potionseller')

def tuning():
 s=Scene('DIFFICULTY TUNING',276,True)
 # Ruined practice arena: stone paving, target, approaching armored enemy.
 for y in range(75,179,13):
  for x in range(80,309,16):
   s.rect((x,y,x+14,y+11),s.r.choice(['#666756','#73715b','#5c6154']));s.rect((x+1,y,x+13,y),'#929078');s.rect((x+1,y+10,x+13,y+10),'#454b43')
   if s.r.random()<.3:s.d.line((x+3,y+1,x+6,y+5,x+4,y+7),fill='#444c43')
 for x,y in [(69,69),(305,67),(67,173),(307,172)]:
  for yy in range(y,y+24,7):s.rock(x,yy,13,7)
 s.bush(35,93);s.bush(328,139);s.bush(40,172);s.chest(107,176);s.barrel(274,58);s.human(166,136,tool=True)
 # Original horned practice enemy, ~23px, detailed armor and spear.
 enemy=['.....ooo.....','..1o122o1....','..1211121....','...12222.....','...2o2o2.....','....232......','...32223.....','..3211123....','.o3212123o...','.o3221223o...','..o33333o....','...34343.....','...34343.....','...ooo.ooo...']
 s.spr(enemy,233,108);s.d.line((248,127,251,99),fill='#b18b56');s.spr(['..1..','.121.','12321','..3..'],249,93)
 # Three wood training targets with progressively battered shields, no UI controls.
 for x in [103,157,211]:
  s.rect((x+6,84,x+8,109),'#7f613b');s.d.ellipse((x,72,x+15,88),fill='#9f8451',outline='#44392c');s.d.ellipse((x+3,75,x+12,85),fill='#765440');s.d.ellipse((x+6,78,x+9,82),fill='#c8ad67')
 s.chest(275,153);s.torch(84,125);s.torch(287,86);s.torch(260,181);s.lights.append((173,139,89,'warm'))
 return s.finish('difficultytuning')

def minions():
 s=Scene('KEEP MINIONS ON TELEPORT',286,True)
 # Broken underground route bridged by a worked-stone waygate.
 for y in range(89,196,12):
  for x in range(133,224,15):
   s.rect((x,y,x+13,y+10),'#676b5b');s.rect((x+1,y,x+12,y),'#9b9c7b');s.rect((x+2,y+8,x+12,y+8),'#474f47')
 for x,y in [(102,96),(239,139),(274,79),(78,162)]:s.rock(x,y,18,14)
 # Gate opening and rune-lit faceted uprights, scaled to the small explorer.
 s.d.ellipse((163,70,210,118),fill='#383b65');s.d.ellipse((168,75,205,115),fill='#466284');s.d.ellipse((173,80,200,114),fill='#5e849a')
 for x in [157,211]:
  for y in range(72,121,9):
   s.rock(x,y,9,10);s.rect((x+3,y+3,x+5,y+5),'#8cc1bf')
 for x in range(162,215,9):s.rock(x,65,9,10)
 for x,y in [(177,80),(190,97),(184,107)]:s.dot(x,y,'#b3d5c7')
 s.bush(42,146);s.bush(264,96);s.bush(296,168);s.bush(68,66);s.human(178,132,{'n':'#4d4364','m':'#88789a'})
 for x,y in [(151,149),(208,156),(188,172)]:s.spr(BAT,x,y);s.lights.append((x,y,32,'cool'))
 s.chest(58,107);s.spr(BAG,265,153)
 s.torch(115,83);s.torch(261,175);s.lights.extend([(185,91,105,'cool'),(184,146,65,'warm')])
 return s.finish('keepminions')

def pet():
 s=Scene('PET EDITOR',293)
 s.floor(77,64,233,129);s.bush(35,82,True);s.bush(329,131);s.bush(54,168);s.shelves(240,61);s.fence(90,76,211);s.fence(90,181,211)
 for x in [87,302]:
  for y in range(81,177,13):s.fence(x,y,2)
 # Straw beds, water dishes and a little pet-care workbench.
 for x,y in [(108,98),(266,137),(119,154)]:
  s.d.ellipse((x,y,x+25,y+13),fill='#6d5b34')
  for _ in range(60):
   xx,yy=x+s.r.randrange(2,24),y+s.r.randrange(2,12);s.rect((xx,yy,xx+2,yy),'#b29856')
 s.spr(PET,116,99);s.spr(PET,272,138,{'b':'#8c5d49','c':'#b48254','d':'#d5ac79'})
 s.spr(PET,188,140,{'b':'#776693','c':'#a18bb7','d':'#c7b4ca'})
 s.human(180,116)
 # Small pet brush and open book beside the selected pet.
 s.rect((223,101,241,115),'#4d332c');s.rect((224,101,240,112),'#d5bf8b');s.rect((231,101,232,112),'#90714c')
 for yy in [104,107,110]:s.rect((225,yy,229,yy),'#8f805e');s.rect((234,yy,238,yy),'#8f805e')
 s.spr(['..ii...','..jj...','.2222..','111111.','..oo...'],218,130)
 for x,y in [(145,98),(248,156)]:s.d.ellipse((x,y,x+11,y+6),fill='#9f956f');s.d.ellipse((x+2,y+1,x+9,y+4),fill='#49707c')
 s.barrel(57,140);s.chest(312,92);s.torch(67,80);s.torch(290,111);s.torch(219,185);s.lights.append((190,140,99,'warm'))
 return s.finish('peteditor')

def boss():
 s=Scene('BOSS BONUS LOOT',304,True)
 # A fallen original cave beast in an arena, treasure scattered by the victorious explorer.
 for x,y in [(80,67),(279,66),(67,166),(301,171)]:
  for yy in range(y,y+21,7):s.rock(x,yy,14,7)
 # Low segmented fallen shell; visible scale texture, limbs and horns.
 for i in range(5):
  x,y=204+i*11,101+int(math.sin(i)*4)
  s.d.ellipse((x,y,x+20,y+24),fill='#463c43',outline='#241f23');s.d.ellipse((x+2,y,x+17,y+18),fill='#75606a');s.d.arc((x+3,y+2,x+15,y+15),190,310,fill='#a18080',width=2)
  for _ in range(9):s.dot(x+s.r.randrange(4,16),y+s.r.randrange(4,18),'#534650')
  s.d.line((x+8,y+21,x+4,y+27,x+1,y+28),fill='#806a60',width=2)
 s.spr(['oo.........oo','.1o.......o1.','..12ooooo21..','...2qqqqq2...','...qoqoqoq...','....qwwwq....','.....ooo.....'],264,112)
 s.bush(39,91);s.bush(307,101);s.bush(307,181);s.bush(57,180);s.rock(112,74,21,12);s.rock(300,143,18,13);s.chest(158,142,True);s.human(130,129,tool=True)
 for x,y,col in [(183,157,'#72a281'),(211,146,'#668daa'),(202,173,'#ba685b')]:s.spr(GEM,x,y,{'m':col,'n':'#526367'})
 for x,y in [(144,163),(228,159)]:s.spr(BAG,x,y)
 for x,y in [(172,170),(177,174),(163,174)]:s.spr(['.kk.','kjjk','.kk.'],x,y)
 s.torch(97,89);s.torch(290,150);s.torch(110,181);s.lights.append((179,150,111,'warm'))
 return s.finish('bossloot')

if __name__=='__main__':
 scenes=[fishing,farm,quick,seller,tuning,minions,pet,boss]
 montage=Image.new('RGB',(768,864))
 for i,fn in enumerate(scenes):
  im=fn();montage.paste(im,((i%2)*384,(i//2)*216));print(fn.__name__)
 (OUT.parent/'build').mkdir(exist_ok=True)
 montage.resize((1536,1728),Image.Resampling.NEAREST).save(OUT.parent/'build'/'native_b_montage.png')
 montage.save(OUT.parent/'build'/'native_b_thumbnails.png')
