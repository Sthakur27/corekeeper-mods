"""Original pixel scenes for the Overhaul and individual-mod collection.

Composition references: official Core Keeper Steam screenshots (see references.json).
No screenshot pixels or extracted game artwork are used. Render 384x216, nearest 4x.
"""
from pathlib import Path
import math
import random
import sys
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from make_art import sprite

OUT = Path(__file__).resolve().parents[1]
W, H = 384, 216
GLYPHS = {
    'A':'01110/10001/10001/11111/10001/10001/10001',
    'B':'11110/10001/10001/11110/10001/10001/11110',
    'C':'01111/10000/10000/10000/10000/10000/01111',
    'D':'11110/10001/10001/10001/10001/10001/11110',
    'E':'11111/10000/10000/11110/10000/10000/11111',
    'F':'11111/10000/10000/11110/10000/10000/10000',
    'G':'01111/10000/10000/10111/10001/10001/01111',
    'H':'10001/10001/10001/11111/10001/10001/10001',
    'I':'11111/00100/00100/00100/00100/00100/11111',
    'J':'00111/00010/00010/00010/10010/10010/01100',
    'K':'10001/10010/10100/11000/10100/10010/10001',
    'L':'10000/10000/10000/10000/10000/10000/11111',
    'M':'10001/11011/10101/10101/10001/10001/10001',
    'N':'10001/11001/10101/10101/10011/10011/10001',
    'O':'01110/10001/10001/10001/10001/10001/01110',
    'P':'11110/10001/10001/11110/10000/10000/10000',
    'Q':'01110/10001/10001/10001/10101/10010/01101',
    'R':'11110/10001/10001/11110/10100/10010/10001',
    'S':'01111/10000/10000/01110/00001/00001/11110',
    'T':'11111/00100/00100/00100/00100/00100/00100',
    'U':'10001/10001/10001/10001/10001/10001/01110',
    'V':'10001/10001/10001/10001/10001/01010/00100',
    'W':'10001/10001/10001/10101/10101/10101/01010',
    'X':'10001/10001/01010/00100/01010/10001/10001',
    'Y':'10001/10001/01010/00100/00100/00100/00100',
    'Z':'11111/00001/00010/00100/01000/10000/11111',
    "'":'00100/00100/00000/00000/00000/00000/00000',
    ' ':'00000/00000/00000/00000/00000/00000/00000',
}


class Scene:
    def __init__(self, seed):
        self.r = random.Random(seed)
        self.im = Image.new('RGB', (W,H), '#0d1016')
        self.d = ImageDraw.Draw(self.im)
        self.lights = []
        self.mask = Image.new('1', (W,H))

    def rect(self, box, fill):
        self.d.rectangle(box, fill=fill)

    def chamber(self, polygon):
        ImageDraw.Draw(self.mask).polygon(polygon, fill=1)
        for y in range(H):
            for x in range(W):
                if self.mask.getpixel((x,y)):
                    self.d.point((x,y), self.r.choice(['#58412f']*7 + ['#5f4834']*4 + ['#634a32','#4b392c']))
        for _ in range(2800):
            x,y = self.r.randrange(W),self.r.randrange(H)
            if self.mask.getpixel((x,y)):
                self.rect((x,y,x+self.r.choice([0,1,2]),y), self.r.choice(['#785d3e','#806446','#3e3027','#6e5137']))
        # Exposed, irregular strata follow the back of each excavated tile column.
        for x in range(8,W-8,8):
            ys = [y for y in range(45,H-4) if self.mask.getpixel((x,y))]
            if not ys:
                continue
            y=min(ys)
            self.d.polygon([(x,y),(x,y-11),(x+2,y-15),(x+7,y-14),(x+9,y-9),(x+9,y+2)],fill='#302820')
            self.d.polygon([(x+1,y-11),(x+3,y-14),(x+7,y-13),(x+8,y-9),(x+7,y-6),(x+2,y-6)],fill=self.r.choice(['#72604a','#67513b','#786147']))
            self.rect((x+2,y-5,x+6,y-1),'#493a2c')
            self.d.line((x+2,y-12,x+6,y-12), fill='#8c7755')
            self.d.line((x+6,y-6,x+6,y),fill='#241f1c')
            self.d.point((x+3,y-3),'#7b6040')

    def floor(self,box,wood=False):
        x0,y0,x1,y1=box
        for y in range(y0,y1,8):
            for x in range(x0-(12 if y//8%2 else 0),x1,24):
                left=max(x,x0); right=min(x+23,x1)
                self.rect((left,y,right,y+7),'#31241e' if wood else '#272e30')
                self.rect((left+1,y,right-1,y+5),self.r.choice(['#895633','#7e4e32','#70462f']) if wood else self.r.choice(['#4c5550','#56605a','#454e4c']))
                self.d.line((left+2,y,right-2,y),fill='#ac7646' if wood else '#758075')
                for _ in range(3):
                    px=self.r.randint(left+1,max(left+1,right-3))
                    self.d.line((px,y+3,px+2,y+3),fill='#503524' if wood else '#384240')

    def grass(self,x,y):
        for _ in range(self.r.randrange(5,10)):
            dx=self.r.randrange(-6,7);h=self.r.randrange(2,7)
            self.d.line((x+dx,y,x+dx-1,y-h),fill=self.r.choice(['#546335','#758044','#95934e','#38482e']))

    def mushroom(self,x,y,blue=False):
        self.d.ellipse((x-4,y,x+5,y+2),fill='#292920')
        self.rect((x,y-5,x+1,y),'#aea784')
        self.d.polygon([(x-5,y-5),(x-4,y-8),(x-2,y-10),(x+2,y-10),(x+5,y-7),(x+5,y-4),(x-4,y-4)],fill='#235c78' if blue else '#9b4933')
        self.d.line((x-3,y-8,x+2,y-9),fill='#8ebccd' if blue else '#d7804a',width=2)
        for dx,dy in [(-3,-6),(1,-8),(3,-5)]:
            self.rect((x+dx,y+dy,x+dx+1,y+dy),'#bcd8c7' if blue else '#eed0a0')
        if blue:self.lights.append((x,y,24,(.4,.9,1.2)))

    def chest(self,x,y,purple=False):
        self.d.ellipse((x-1,y+11,x+19,y+15),fill='#242124')
        self.rect((x,y+3,x+18,y+12),'#302021')
        self.rect((x+1,y+1,x+17,y+6),'#5f497d' if purple else '#a27643')
        self.rect((x+1,y+7,x+17,y+11),'#392f54' if purple else '#805032')
        for xx in [x+3,x+14]:self.rect((xx,y+1,xx+1,y+11),'#a68bc9' if purple else '#cf9c57')
        self.d.line((x+2,y,x+16,y),fill='#bc9ce4' if purple else '#dbb577')
        self.rect((x+8,y+5,x+11,y+9),'#c9a2fe' if purple else '#e5c383')
        self.d.point((x+9,y+7),'#4d3466' if purple else '#725532')
        if purple:self.lights.append((x+9,y+6,36,(.8,.5,1.3)))

    def person(self,x,y,tunic=(71,112,114)):
        rows=['.....kkkk......','....khhhhk.....','...khHHHHhk....','...kHYYYYHk....','..kYYYYYYYYk...','...kkffffkk....','....kfkffk.....','....kffffk.....','.....kffk......','...kkTTTTkk....','..kfTTttTTfk...','..kfTttttTfk...','...kTttttTk....','....kttttk.....','....kbbbbk.....','....kbbkbk.....','...kBBkkBBk....','...kkkkkkkk....']
        p={'k':(33,26,27),'h':(100,61,39),'H':(152,104,50),'Y':(208,169,89),'f':(225,165,111),'T':tuple(min(255,c+35) for c in tunic),'t':tunic,'b':(66,57,43),'B':(102,79,52)}
        self.d.ellipse((x+1,y+16,x+14,y+20),fill='#272821')
        sp=sprite(rows,p,1);self.im.paste(sp,(x,y),sp)

    def torch(self,x,y):
        self.rect((x,y,x+1,y+9),'#71432c')
        self.rect((x-1,y-1,x+2,y+2),'#b6783e')
        self.d.polygon([(x-2,y),(x-1,y-5),(x+1,y-8),(x+2,y-3),(x+3,y),(x+1,y+2)],fill='#ed8a37')
        self.rect((x,y-4,x+1,y),'#ffcf67')
        self.d.point((x,y-3),'#fff0be')
        self.lights.append((x,y,65,(1.1,.79,.41)))

    def plant(self,x,y):
        self.rect((x-7,y-6,x+7,y+9),'#30271c')
        self.rect((x-6,y-5,x+6,y+8),'#715035')
        for yy in (y-3,y+2,y+7):self.d.line((x-5,yy,x+5,yy),fill='#392b20')
        self.d.ellipse((x-4,y-1,x+5,y+6),fill='#8b3928')
        self.d.ellipse((x-3,y-2,x+3,y+3),fill='#dc7440')
        self.d.line((x-1,y-1,x+1,y-1),fill='#f4a759',width=2)
        for dx,dy in [(-3,-4),(0,-5),(3,-5),(-1,-2)]:
            self.d.polygon([(x,y),(x+dx-2,y+dy),(x+dx,y+dy-2),(x+dx+3,y+dy+1)],fill='#688739')
            self.d.point((x+dx,y+dy),'#bac26b')

    def pond(self,box):
        x0,y0,x1,y1=box
        self.rect((x0-2,y0-2,x1+2,y1+5),'#2c2926')
        self.rect(box,'#1c3849')
        self.d.line((x0,y0-2,x1,y0-2),fill='#a18650')
        self.d.line((x0,y1+1,x1,y1+1),fill='#647e6b')
        for _ in range(180):
            x,y=self.r.randrange(x0+2,x1-5),self.r.randrange(y0+2,y1-1)
            self.d.line((x,y,x+self.r.randrange(1,5),y),fill=self.r.choice(['#234653','#284e60','#3c6671','#416c77']))
        for x in range(x0+8,x1,13):
            self.d.line((x,y1,x-2,y1-7),fill='#8b8545')
            self.d.line((x+2,y1,x+3,y1-5),fill='#556a3b')

    def barrel(self,x,y):
        self.d.ellipse((x,y,x+12,y+6),fill='#b88347')
        self.rect((x,y+3,x+12,y+13),'#845934')
        self.d.ellipse((x,y+10,x+12,y+15),fill='#66482d')
        for xx in range(x+2,x+12,3):self.d.line((xx,y+3,xx,y+12),fill='#b08246')
        self.d.line((x,y+4,x+12,y+4),fill='#525751')
        self.d.line((x,y+11,x+12,y+11),fill='#454c46')
        self.d.ellipse((x+2,y+1,x+10,y+4),fill='#684731')

    def fern(self,x,y):
        self.d.ellipse((x-8,y-1,x+8,y+3),fill='#252d23')
        for dx,dy in [(-7,-6),(7,-6),(-5,-10),(5,-10),(0,-13),(-8,-2),(8,-2)]:
            self.d.line((x,y,x+dx,y+dy),fill='#374b2e')
            for n in range(2,6):
                xx=x+int(dx*n/6); yy=y+int(dy*n/6)
                self.d.line((xx-2,yy-1,xx+2,yy),fill='#527041')
                self.d.point((xx-1,yy-1),'#87944e')

    def rocks(self,x,y):
        for dx,dy,size in [(0,0,8),(8,5,5),(-5,6,4)]:
            xx,yy=x+dx,y+dy
            self.d.polygon([(xx-size//2,yy),(xx-size//3,yy-size),(xx+2,yy-size-1),(xx+size//2,yy-3),(xx+size//2,yy+1)],fill='#474a3d')
            self.d.polygon([(xx-size//3,yy-size),(xx+2,yy-size-1),(xx+size//2,yy-3),(xx,yy-4)],fill='#81806a')
            self.d.line((xx-size//3,yy-size,xx+1,yy-size),fill='#b1a184')

    def bookshelf(self,x,y):
        self.rect((x,y,x+29,y+22),'#34231f')
        for row in (2,12):
            xx=x+3
            while xx<x+26:
                height=self.r.randrange(5,9)
                color=self.r.choice(['#7a5f37','#8b5243','#446c67','#73825a','#a08755'])
                self.rect((xx,y+row+8-height,xx+2,y+row+8),color)
                self.d.point((xx+1,y+row+9-height),'#c6aa75')
                xx+=self.r.choice([3,4])
        for yy in (0,10,21):
            self.rect((x,y+yy,x+29,y+yy+1),'#9e7144')
            self.d.line((x+1,y+yy,x+28,y+yy),fill='#bd9256')
        self.rect((x,y,x+1,y+22),'#91613a');self.rect((x+28,y,x+29,y+22),'#63452d')

    def pot(self,x,y):
        self.d.ellipse((x-5,y-3,x+5,y+5),fill='#604734')
        self.d.ellipse((x-5,y-6,x+5,y),fill='#a27a4b')
        self.d.ellipse((x-3,y-5,x+3,y-1),fill='#332d24')
        self.d.line((x-3,y+1,x-2,y+4),fill='#bb9462')

    def bench(self,x,y):
        self.rect((x+2,y+8,x+4,y+15),'#4b3126');self.rect((x+25,y+8,x+27,y+15),'#4b3126')
        self.rect((x,y,x+30,y+9),'#805130')
        self.d.line((x,y,x+30,y),fill='#c49357',width=2)
        for yy in (3,7):self.d.line((x+2,y+yy,x+28,y+yy),fill='#4e3328')
        self.d.line((x+7,y-2,x+16,y+3),fill='#c19a6b',width=2)
        self.d.line((x+13,y-2,x+18,y+2),fill='#a8b1a0',width=2)
        self.rect((x+22,y-3,x+26,y+3),'#777d77');self.rect((x+23,y-4,x+25,y-3),'#b7beab')

    def light(self):
        pix=self.im.load()
        for y in range(38,H):
            for x in range(W):
                amounts=[max(0,1-math.hypot(x-lx,y-ly)/radius) for lx,ly,radius,_ in self.lights]
                light=max(amounts,default=0)
                shade=.36+.75*light
                base=pix[x,y]
                warm=[0.,0.,0.]
                for amount,(_,_,_,color) in zip(amounts,self.lights):
                    for i in range(3):warm[i]=max(warm[i],amount*color[i]*18)
                pix[x,y]=tuple(min(255,int(base[i]*shade+warm[i])) for i in range(3))

    def title(self,lines):
        for row,text in enumerate(lines):
            size=2
            x=(W-(len(text)*6-1)*size)//2
            y=9+row*19
            for char in text:
                for yy,bits in enumerate(GLYPHS[char].split('/')):
                    for xx,bit in enumerate(bits):
                        if bit=='1':
                            self.rect((x+xx*size+1,y+yy*size+2,x+xx*size+size,y+yy*size+size+1),'#453026')
                            self.rect((x+xx*size,y+yy*size,x+xx*size+size-1,y+yy*size+size-1),'#f2d291' if yy<3 else '#c39759')
                x+=6*size

    def save(self,name):
        self.im.resize((1536,864),Image.Resampling.NEAREST).save(OUT/name,optimize=True)


def overhaul():
    s=Scene(902)
    s.chamber([(20,86),(38,86),(38,65),(116,65),(116,75),(151,75),(151,57),(236,57),(236,73),(340,73),(340,88),(365,88),(365,179),(345,179),(345,203),(267,203),(267,194),(128,194),(128,202),(31,202),(31,182),(20,182)])
    s.floor((143,89,242,181))
    # Workbench and stores occupy a wooden side room.
    s.floor((268,89,343,158),True)
    s.bench(276,99);s.chest(319,99);s.barrel(318,132);s.person(278,121,(96,116,143))
    s.bookshelf(304,74);s.pot(297,144);s.pot(337,139)
    # Raised central ancient monument, drawn as rock rather than a floating icon.
    s.d.ellipse((164,108,220,158),fill='#2c3535')
    s.d.ellipse((165,105,218,151),fill='#5e6861')
    s.d.ellipse((170,109,214,145),outline='#9eaaa0',width=2)
    for a in range(0,360,30):
        x=192+int(math.cos(math.radians(a))*20);y=127+int(math.sin(math.radians(a))*15)
        s.rect((x,y,x+2,y+1),'#7cb9bc')
    s.d.polygon([(180,128),(177,106),(181,93),(193,86),(205,96),(208,118),(201,132),(188,136)],fill='#285269')
    s.d.polygon([(181,106),(192,91),(197,104),(192,130),(183,127)],fill='#69c4cc')
    s.d.polygon([(197,103),(202,98),(206,116),(198,128),(194,131)],fill='#368eaa')
    s.d.line((185,105,191,96),fill='#c6eddb',width=2)
    s.lights.append((192,117,87,(.3,.9,1.3)))
    s.person(205,152,(129,72,61))
    # Farming and fishing appear as working corners of the same base.
    for y in (104,125,146):
        for x in (55,73,91,109):s.plant(x,y)
    s.person(106,155,(77,117,66));s.chest(44,164)
    s.barrel(67,171);s.pot(87,173)
    s.pond((267,175,340,197));s.person(252,175,(139,104,50))
    s.d.line((264,184,278,173,291,178),fill='#cdac6c')
    s.d.line((291,178,304,184),fill='#c5d6cd');s.rect((303,183,305,185),'#d98360')
    for x,y in [(35,113),(35,157),(118,86),(125,179),(254,166),(342,164),(245,83)]:s.grass(x,y)
    for x,y in [(32,183),(39,186),(129,92),(249,189),(350,134)]:s.mushroom(x,y,blue=x>200)
    for x,y in [(27,131),(125,164),(241,181),(352,155),(261,88),(110,82)]:s.fern(x,y)
    for x,y in [(139,78),(352,113),(28,165),(231,69),(271,196)]:s.rocks(x,y)
    for x,y in [(42,90),(118,146),(274,85),(337,147),(174,177)]:s.torch(x,y)
    s.light();s.title(["SID'S OVERHAUL"]);s.save('overhaul_logo.png')


def collection():
    s=Scene(561)
    s.chamber([(19,80),(36,80),(36,58),(135,58),(135,70),(224,70),(224,59),(339,59),(339,79),(363,79),(363,174),(343,174),(343,201),(254,201),(254,191),(123,191),(123,201),(32,201),(32,177),(19,177)])
    # A branching base: distinct workshop, garden and storeroom joined by paths.
    s.floor((148,80,239,192))
    s.floor((45,78,127,129),True)
    s.floor((257,78,339,134),True)
    s.bench(49,86);s.bench(88,107);s.person(71,103,(119,79,94))
    s.barrel(111,83);s.chest(48,110)
    s.bookshelf(87,62);s.pot(52,133);s.pot(78,130)
    for x,y in [(263,87),(290,87),(316,87),(265,115),(293,115)]:s.chest(x,y,purple=x==293)
    s.person(309,113,(89,92,147))
    s.barrel(342,110);s.pot(250,126)
    for y in (152,173):
        for x in (51,69,87,105):s.plant(x,y)
    s.person(115,155,(67,120,83))
    s.pond((262,159,337,187));s.person(248,164,(131,108,62))
    s.d.line((259,173,276,161,285,168),fill='#cbae76');s.d.line((285,168,292,176),fill='#b9c6b2')
    # Central wayfinding post and two explorers at the crossroads.
    s.rect((189,109,192,144),'#523829')
    for yy,left in [(109,True),(118,False),(127,True)]:
        pts=[(180,yy),(201,yy),(205,yy+3),(201,yy+6),(180,yy+6)]
        if left:pts=[(176,yy+3),(181,yy),(201,yy),(201,yy+6),(181,yy+6)]
        s.d.polygon(pts,fill='#aa804e');s.d.line((181,yy+1,199,yy+1),fill='#d4ad73')
        s.d.line((184,yy+4,195,yy+4),fill='#77532f')
    s.person(173,153,(86,127,129));s.person(208,143,(137,82,58))
    for x,y in [(31,99),(134,102),(243,98),(347,130),(39,190),(239,178),(141,175),(349,184)]:s.grass(x,y)
    for x,y in [(25,154),(31,158),(239,84),(244,89),(341,196)]:s.mushroom(x,y,blue=x>200)
    for x,y in [(25,118),(135,133),(230,189),(349,169),(126,75),(254,144)]:s.fern(x,y)
    for x,y in [(144,83),(351,91),(24,179),(236,66),(239,193)]:s.rocks(x,y)
    for x,y in [(39,78),(124,130),(165,96),(228,132),(339,145),(124,181)]:s.torch(x,y)
    s.light();s.title(["SID'S CORE KEEPER MODS"]);s.save('collection_logo.png')


if __name__=='__main__':
    overhaul();collection()
