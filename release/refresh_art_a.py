"""Original accessible listing artwork, group A. Run: python release/refresh_art_a.py"""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont
from make_art import sprite
OUT=Path(__file__).resolve().parent
W,H=1280,720
INK='#101b2b'; WHITE='#f5f7ef'
def font(n): return ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',n)
def base(title,caption,accent):
    im=Image.new('RGB',(W,H),INK); d=ImageDraw.Draw(im)
    d.rectangle((40,38,48,172),fill=accent)
    size=72
    while d.textlength(title,font=font(size))>1150: size-=2
    d.text((76,40),title,font=font(size),fill=WHITE)
    d.text((80,135),caption,font=font(30),fill='#c9d8e4')
    for x in range(0,W,80):
        d.rectangle((x,612,x+78,720),fill='#1b2d3b')
        d.rectangle((x+8,618,x+60,629),fill='#2b4350')
    return im,d
def text(d,x,y,s,n=30,c=WHITE): d.text((x,y),s,font=font(n),fill=c,anchor='mm')
def panel(d,b,c): d.rectangle(b,fill='#203548',outline=c,width=5)
def arrow(d,x,y,col):
    d.rectangle((x,y-6,x+55,y+6),fill=col); d.polygon([(x+47,y-23),(x+72,y),(x+47,y+23)],fill=col)
def put(im,rows,x,y,scale,col):
    p={'k':(12,20,32),'a':col,'s':(110,142,161),'w':(241,249,242),'b':(146,95,62),'g':(255,210,105)}
    sp=sprite(rows,p,scale); im.paste(sp,(x,y),sp)
HELM=['...kkkkkk...','..kaaaaaak..','.kaaawaaaak.','kaaaaaaaa aak'.replace(' ',''),'kssssssssssk','kskkk..kkksk','kk........kk']
PICK=['..kkkkkkkk..','.kaaaawaaaak','kaaaakbkaak.','.kkk.kbk....','.....kbk....','.....kbk....','.....kbk....','.....kbk....','.....kkk....']
FISH=['........kk....','...kkkkkaaak..','k.kaaaawkaaaak','kakaaaaaaaaaak','kaaaaaaaaaaak.','kakaaaaaaakk..','k..kkkkkkk....']
MUSH=['...kkkkkk...','..kaaawaak..','.kwaaaaaaak.','kaaaawaaaaak','kssssssssssk','....kwwk....','....kwwk....','....kkkk....']
POTION=['....kkkk....','....kbbk....','....kwwk....','...kwwwwk...','..kwaaaawk..','.kwaaaaaawk.','.kaaaaaaaak.','.kaawaaaaak.','..kssssssk..','...kkkkkk...']
CAN=['..kkkkkk......','..kaaaak......','kkkaaaakk.....','kwaaaaaaak..kk','kwaaaaaaakkkak','kaaaaaaaakaak.','ksssssssskk...','.kkkkkkkk.....']
def save(im,name):
    path=OUT/name; im.save(path,optimize=True)
    assert path.stat().st_size<1000000
    return im
imgs=[]
# A tangible menu with large controls: no faux screenshot or tiny labels.
im,d=base('MOD OPTIONS','Your mod settings, inside the game.','#8bdad1')
panel(d,(250,224,1030,565),'#8bdad1')
for yy,word,pos in [(300,'SPEED',780),(395,'DURATION',620),(490,'CHANCE',860)]:
    text(d,405,yy,word,28); d.rectangle((555,yy-5,940,yy+5),fill='#8bdad1'); d.rectangle((pos-17,yy-23,pos+17,yy+23),fill=WHITE)
imgs.append(save(im,'modoptions_logo.png'))
# Gear paths, with one source and two clearly labelled borrowed helmets.
im,d=base('LOADOUT FALLBACK','Empty slots borrow your earlier gear.','#f2ce7c')
for i in range(3):
    x=152+i*385; panel(d,(x,250,x+235,525),'#f2ce7c'); text(d,x+117,280,str(i+1),34)
    put(im,HELM,x+57,331,10,(242,206,124) if i==0 else (141,171,177))
    text(d,x+117,492,'YOUR ITEM' if i==0 else 'BORROWED',22)
    if i<2: arrow(d,x+270,390,'#f2ce7c')
imgs.append(save(im,'loadout_logo.png'))
# Five outfits, not the old repeated three-slot logo.
im,d=base('FIVE LOADOUTS','Five setups. Ready when you are.','#bfa5fa')
for i,col in enumerate([(143,184,220),(142,216,171),(239,167,129),(248,214,127),(192,165,250)]):
    x=92+i*227; panel(d,(x,250,x+190,550),col); text(d,x+95,285,['I','II','III','IV','V'][i],34)
    put(im,HELM,x+35,335,10,col)
    d.polygon([(x+46,435),(x+73,420),(x+118,420),(x+145,435),(x+131,493),(x+60,493)],fill=col)
    d.rectangle((x+87,430,x+102,475),fill=WHITE)
imgs.append(save(im,'fiveloadouts_logo.png'))
# Three different activity icons and legible XP progress tracks.
im,d=base('SKILL XP MULTIPLIER','Level each skill at your own pace.','#91d7ef')
for i,(rows,col,length) in enumerate([(PICK,(242,206,124),250),(FISH,(126,211,236),400),(MUSH,(222,153,209),545)]):
    yy=245+i*119; put(im,rows,160,yy,9,col); d.rectangle((350,yy+22,1080,yy+65),fill='#294254'); d.rectangle((356,yy+28,350+length,yy+59),fill=col)
    text(d,1140,yy+44,'XP',28)
imgs.append(save(im,'skillxp-logo.png'))
# Rod, bobber, leaping fish and speed streaks.
im,d=base('FAST AUTO FISHING','Less waiting. More fishing.','#81dbe3')
d.rectangle((120,490,1160,607),fill='#20506b')
for x in range(145,1140,120): d.rectangle((x,525,x+62,531),fill='#5898b3')
d.line([(190,504),(430,248)],fill='#e5b981',width=18); d.line([(430,248),(730,458)],fill='#eaf5f1',width=4)
d.rectangle((714,446,746,479),fill='#f48477'); d.rectangle((714,479,746,494),fill=WHITE)
put(im,FISH,780,302,17,(129,219,227))
for i in range(3): d.rectangle((580-i*25,326+i*37,740-i*25,333+i*37),fill='#81dbe3')
imgs.append(save(im,'fastfish_logo.png'))
# A large pickaxe beside a protected durability bar.
im,d=base('DURABILITY MULTIPLIER','Keep your favorite gear working longer.','#a8deae')
put(im,PICK,222,255,28,(197,214,224))
d.polygon([(828,248),(1018,307),(988,467),(828,548),(668,467),(638,307)],fill='#355c55',outline='#a8deae')
d.line([(731,388),(798,448),(927,331)],fill='#d6f3cf',width=22)
panel(d,(170,561,1108,595),'#a8deae'); d.rectangle((181,571,1020,585),fill='#a8deae')
imgs.append(save(im,'durability_logo.png'))
# Potion beside a clock; three minutes is the documented buff default.
im,d=base('BUFF DURATION FLOOR','Give short food and potion buffs more time.','#dcafea')
put(im,POTION,205,247,30,(183,125,226))
d.ellipse((657,248,1007,598),fill='#27364e',outline='#dcafea',width=10)
for a in range(0,360,30):
    t=math.radians(a); d.line([(832+142*math.sin(t),423-142*math.cos(t)),(832+158*math.sin(t),423-158*math.cos(t))],fill=WHITE,width=5)
d.line([(832,307),(832,423),(923,423)],fill=WHITE,width=13); d.ellipse((818,409,846,437),fill='#dcafea')
imgs.append(save(im,'buffduration_logo.png'))
# A distinct underground mushroom patch with increasing sizes.
im,d=base('FASTER MUSHROOMS','More chances for your mushroom patch to return.','#edb894')
for x,y,n,col in [(158,426,14,(201,137,99)),(426,345,23,(227,166,121)),(826,270,31,(247,192,142))]:
    d.ellipse((x-20,568,x+12*n+20,604),fill='#364d46'); put(im,MUSH,x,y,n,col)
for x,y in [(400,350),(780,300),(1100,248)]:
    d.rectangle((x-4,y-14,x+4,y+14),fill='#f8deb4');d.rectangle((x-14,y-4,x+14,y+4),fill='#f8deb4')
imgs.append(save(im,'fastermushrooms_logo.png'))
# Exact visible 2x2 and 4x4 watered gardens.
im,d=base('BIGGER WATERING CANS','Water more tiles with every pour.','#8dcdf0')
for x,n,step,scale in [(158,2,72,13),(710,4,52,16)]:
    put(im,CAN,x+15,233,scale,(137,197,228))
    yy=426
    for r in range(n):
        for c in range(n):
            xx=x+c*step; y=yy+r*40; d.polygon([(xx,y),(xx+step-4,y),(xx+step-4,y+34),(xx,y+34)],fill='#345e78',outline='#81bfd9');d.line([(xx+step//2,y+25),(xx+step//2,y+10)],fill='#b3e2aa',width=4)
    text(d,x+n*step+78,478,f'{n}×{n}',44)
imgs.append(save(im,'wateringcans_logo.png'))
montage=Image.new('RGB',(1280,720),'#101b2b')
for i,im in enumerate(imgs): montage.paste(im.resize((426,240)),((i%3)*426,(i//3)*240))
preview=OUT.parent/'build'/'art_refresh_a_montage.jpg';preview.parent.mkdir(exist_ok=True);montage.save(preview,quality=94)
print(preview)
