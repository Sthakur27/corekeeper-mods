"""Original listing scenes for eight mods. Run from any directory with Pillow installed."""
from pathlib import Path
import math, random
from PIL import Image, ImageDraw, ImageFont
from make_art import sprite
OUT=Path(__file__).resolve().parent
FONT='C:/Windows/Fonts/seguisb.ttf'
P={'k':'#101b2c','a':'#af7aff','b':'#694195','c':'#70e4dc','d':'#fff3d5','g':'#ffd477','s':'#6a7e99','w':'#eaf4ff','r':'#ef777d','t':'#986744','u':'#563f3b'}
P={k:tuple(bytes.fromhex(v[1:])) for k,v in P.items()}
CHEST=['.kkkkkkkkkkkkkk.','kaaaaaaaaaaaaaak','kaddaaaaaaaaaaak','kbbbbbbbbbbbbbbk','kkkkkkggggkkkkkk','kbbbbkgdgkbbbbbk','kbbbbkgggkbbbbbk','kbbbbbbbbbbbbbbk','.kkkkkkkkkkkkkk.']
FISH=['..........cc..','....ccccccc...','..cccwcccccc..','ccccckcccccccc','..cccccccccc..','....ccccccc...','..........cc..']
ARMOR=['..aaa....aaa..','.adddaaaaddda.','adddddddddddda','abddddddddddba','.abdddddddba..','..abdddddda...','..abdddddda...','..abbbbbbaa...','...aaaaaa....']
BOAT=['.....gg.......','.....gg.......','.....ggww.....','.....ggwww....','.....ggwwww...','.....ggwwwww..','.....gg.......','kkkkkkkkkkkkkk','.tttttttttttt.','..uuuuuuuuuu..','...kkkkkkkk...']
POT=['....ww...ww....','.....ww.ww.....','......w.w......','...kkkkkkkkk...','..ksssssssssk..','kkssssssssssskk','ksssssssssssssk','.ksssssssssssk.','..kkkkkkkkkkk..','....rrrgrrr....','.....ggggg.....']

def put(im,rows,x,y,scale=4,pal=None):
    sp=sprite(rows,pal or P,scale); im.paste(sp,(x,y),sp)
def star(d,x,y,c='#fff0bd',r=4):
    d.rectangle((x-r,y-1,x+r,y+1),fill=c); d.rectangle((x-1,y-r,x+1,y+r),fill=c)
def base(seed,accent):
    im=Image.new('RGBA',(320,180),'#111b2b');d=ImageDraw.Draw(im);rnd=random.Random(seed)
    for _ in range(80):
        x,y=rnd.randrange(0,320,4),rnd.randrange(50,180,4)
        d.rectangle((x,y,x+3,y+3),fill='#1a2b3b')
    d.rectangle((0,151,320,180),fill='#0c1422')
    d.line((20,152,300,152),fill=accent,width=1)
    return im,d

def finish(im,name,title,sub,accent,wip=False):
    im=im.convert('RGB').resize((1280,720),Image.Resampling.NEAREST);d=ImageDraw.Draw(im)
    def txt(y,s,size,color):
        f=ImageFont.truetype(FONT,size)
        while d.textlength(s,font=f)>1150:
            size-=1;f=ImageFont.truetype(FONT,size)
        d.text((640,y),s,font=f,fill=color,anchor='mt')
    txt(35,title,76,'#f5f8ff');txt(130,sub,31,accent)
    if wip:
        d.rectangle((318,618,962,697),fill='#ffd477');txt(630,'WORK IN PROGRESS',43,'#142033')
    else:
        txt(640,'CORE KEEPER  /  SID’S MODS',26,'#b6c5d5')
    p=OUT/name;im.save(p,optimize=True);assert p.stat().st_size<1024*1024
    return im

def run():
    images=[]
    im,d=base(1,'#b895ff')
    d.ellipse((122,70,198,145),outline='#9160ca',width=3)
    for x in range(98,226,9):star(d,x,107,'#b895ff',2)
    put(im,CHEST,35,91,4);put(im,CHEST,225,91,4)
    for y in range(86,131,12):
        for x in range(140,180,10):d.rectangle((x,y,x+7,y+7),fill='#d8b9ff',outline='#704caa')
    images.append(finish(im,'enderstash_logo.png','ENDER STASH','One character. One stash. Any Ender Chest.','#d0b2ff'))
    im,d=base(2,'#77dbff')
    d.polygon([(0,92),(162,66),(194,145),(0,149)],fill='#204e67')
    for x,y in [(18,100),(40,125),(120,131),(10,139)]:d.line((x,y,x+22,y),fill='#63cadd',width=2)
    put(im,BOAT,51,77,5)
    d.polygon([(170,77),(320,93),(320,143),(188,135)],fill='#34405a')
    for x in range(198,300,26):d.line((x,131,x+15,133),fill='#eece7b',width=2)
    d.rectangle((224,107,282,122),fill='#d2795f');d.rectangle((233,97,257,110),fill='#f2b279')
    for x in [230,267]:d.rectangle((x,119,x+10,131),fill='#101b2c');d.rectangle((x+3,122,x+7,128),fill='#a0b8cf')
    for y in [103,110,117]:d.line((192,y,216,y),fill='#ffde92',width=2)
    images.append(finish(im,'vehiclespeed_logo.png','VEHICLE SPEED','Choose your pace on water and land.','#9bdff5'))
    im,d=base(3,'#74e4cc')
    d.polygon([(128,83),(177,70),(217,96),(210,137),(144,143),(120,119)],fill='#60758a',outline='#9fb9c8')
    for x,y in [(147,90),(175,82),(189,107),(150,123),(171,110)]:d.polygon([(x,y),(x+9,y-4),(x+14,y+8),(x+4,y+13)],fill='#6fe1ca')
    d.rectangle((43,112,96,139),fill='#c58d5c');d.rectangle((53,99,85,114),fill='#edd39a')
    d.polygon([(96,111),(125,119),(96,129)],fill='#c6dae7')
    for x in [50,76]:d.rectangle((x,136,x+14,144),fill='#465366')
    for x in [238,251,264]:d.polygon([(x,134),(x+6,128),(x+11,136),(x+5,140)],fill='#6fe1ca')
    d.line([(231,88),(240,80),(253,89),(267,80),(277,88),(267,97),(253,89),(240,97),(231,88)],fill='#fff0b9',width=4)
    images.append(finish(im,'infiniteore_logo.png','INFINITE ORE BOULDERS','Keep the drills running.','#97edda'))
    im,d=base(4,'#ffe194')
    d.rectangle((47,132,146,143),fill='#725044');d.rectangle((91,99,96,133),fill='#d3a448')
    for x,y in [(75,100),(93,92),(104,105)]:d.polygon([(x,y),(x-9,y-8),(x+1,y-13),(x+12,y-5)],fill='#ffe095')
    for x,y in [(61,89),(119,79),(139,111)]:star(d,x,y)
    put(im,POT,190,84,4)
    d.line((152,111,176,111),fill='#ffe095',width=3);d.polygon([(172,106),(182,111),(172,116)],fill='#ffe095')
    images.append(finish(im,'goldenchance_logo.png','GOLDEN CHANCE','Golden plants + rarer bonus food: in development.','#ffe194',True))
    im,d=base(5,'#ffadbd')
    d.polygon([(58,139),(63,125),(114,76),(121,81),(74,133)],fill='#e9f7ff')
    d.line((59,119,79,139),fill='#d6a66c',width=5)
    star(d,130,94,'#ffce7c',17)
    for r in [25,40,55]:d.arc((129-r,94-r,129+r,94+r),-65,65,fill='#ff91ad',width=3)
    d.rectangle((221,91,239,122),fill='#d3a3bf');d.polygon([(239,91),(261,75),(261,138),(239,122)],fill='#f2d8ed')
    for r in [17,27]:d.arc((247-r,106-r,247+r,106+r),-60,60,fill='#ff91ad',width=2)
    images.append(finish(im,'stimhits_logo.png','STIM HITS','Hear every hit. Pick your sound.','#ffc0d0'))
    im,d=base(6,'#c6b5ff')
    for x,col,shade in [(46,(239,119,125),(152,62,91)),(135,(108,218,221),(42,127,150)),(224,(183,137,244),(105,70,166))]:
        pal=dict(P,a=shade,b=shade,d=col);put(im,ARMOR,x,77,4,pal)
    for i,col in enumerate(['#ef777d','#efb478','#ffd477','#86d68f','#70e4dc','#70b6ef','#ba89f4','#f0a2d7']):
        d.rectangle((85+i*20,128,100+i*20,142),fill=col)
    images.append(finish(im,'armordye_logo.png','ARMOR DYE','Your gear. Your colors.','#dac7ff'))
    im,d=base(7,'#a4e8bc')
    d.rectangle((160,75,285,142),fill='#3a3c40');d.rectangle((158,103,287,110),fill='#ae7c54');d.rectangle((158,136,287,143),fill='#ae7c54')
    for x in [171,207,243]:
        d.rectangle((x,82,x+20,101),fill='#83c0a4');d.rectangle((x+4,78,x+16,82),fill='#e2d6a7');d.rectangle((x,117,x+20,134),fill='#ddbb7a')
    d.ellipse((40,79,113,145),fill='#e8e6cb',outline='#78c5aa',width=3);d.line((76,90,76,113,93,119),fill='#25354a',width=4)
    d.arc((30,68,124,151),205,340,fill='#a4e8bc',width=3);d.polygon([(121,81),(124,95),(110,90)],fill='#a4e8bc')
    images.append(finish(im,'merchantrestock_logo.png','MERCHANT RESTOCK','Shorter waits. Refilled shelves.','#b9efd0'))
    im,d=base(8,'#9fe4fa')
    for i in range(10):d.rectangle((36+i*25,70,60+i*25,82),fill='#73c0d8' if i%2==0 else '#e9dec0')
    d.rectangle((36,82,44,146),fill='#77513f');d.rectangle((279,82,287,146),fill='#77513f');d.rectangle((36,131,287,146),fill='#ac7953')
    for x,col in [(63,(116,215,211)),(128,(248,182,113)),(196,(196,154,237))]:put(im,FISH,x,101,4,dict(P,c=col))
    for x in [79,146,214]:d.rectangle((x,139,x+15,143),fill='#ecd999')
    star(d,261,100,'#ffe09e',4)
    images.append(finish(im,'fishseller_logo.png','FISH SELLER','Fish for your next recipe. Ready to buy.','#b2e7f7'))
    montage=Image.new('RGB',(1280,1440))
    for i,im in enumerate(images):montage.paste(im.resize((640,360)),((i%2)*640,(i//2)*360))
    check=OUT.parent/'build'/'art_refresh_c_montage.jpg';check.parent.mkdir(exist_ok=True);montage.save(check,quality=90)
    print(check)
    for p in ['enderstash','vehiclespeed','infiniteore','goldenchance','stimhits','armordye','merchantrestock','fishseller']:print(p,(OUT/(p+'_logo.png')).stat().st_size)
if __name__=='__main__':run()
