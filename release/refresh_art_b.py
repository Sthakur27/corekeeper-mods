"""Rebuild eight original, accessible listing illustrations. No external art assets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from make_art import sprite
OUT=Path(__file__).resolve().parent
W,H=1280,720
WHITE='#f5f6ed'; INK='#101b2a'; GOLD='#ffd66d'; TEAL='#80e3d3'
def font(n): return ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',n)
def base(title,tag,accent):
    im=Image.new('RGB',(W,H),INK); d=ImageDraw.Draw(im)
    d.rectangle((0,0,W,12),fill=accent)
    for x in range(20,W,64):
        d.rectangle((x,256,x+32,264),fill='#263747')
    n=82
    while d.textlength(title,font=font(n))>1170:n-=2
    d.text((64,49),title,font=font(n),fill=WHITE)
    d.text((68,157),tag,font=font(32),fill=accent)
    d.rectangle((48,653,1232,659),fill=accent)
    return im,d

def spr(im,rows,x,y,scale=12,palette=None):
    pal={'k':INK,'w':WHITE,'a':TEAL,'b':'#288e91','g':GOLD,'s':'#bf8741','r':'#ee776b','t':'#b77afb','d':'#685581'}
    if palette:pal.update(palette)
    # make_art.sprite expects numeric RGB tuples
    from PIL.ImageColor import getrgb
    pal={k:getrgb(v) if isinstance(v,str) else v for k,v in pal.items()}
    s=sprite(rows,pal,scale);im.paste(s,(x,y),s)

def sparkle(d,x,y,c=GOLD,s=18):
    d.polygon([(x,y-s),(x+6,y-6),(x+s,y),(x+6,y+6),(x,y+s),(x-6,y+6),(x-s,y),(x-6,y-6)],fill=c)

def arrow(d,x,y,length=120,c=WHITE):
    d.rectangle((x,y-8,x+length-25,y+8),fill=c)
    d.polygon([(x+length-28,y-24),(x+length,y),(x+length-28,y+24)],fill=c)

POTION=['...ssss...','...gggg...','....ww....','...wwww...','..wwaaww..','.waaaaaaw.','.waawaabw.','.waaaaabw.','..wbbbbw..','...wwww...']
PET=['.aa....aa.','aaaa..aaaa','abaaaaaaba','aaaaaaaaaa','aakwaakwaa','aaaaaaaaaa','.aawwwwaa.','..aaaaaa..','.aaabbaaa.','aab....baa']
GEM=['...ww...','..waaw..','.waaabw.','waaaabbw','.waabbw.','..wbbw..','...ww...']
SEED=['..gg..','.gssg.','gssssg','gssssg','.gssg.','..gg..']
SPROUT=['aa....aa','abbaabba','.abbbba.','...bb...','...bb...']
BAG=['..gggg..','...ss...','..ssss..','.ssssss.','ssssssss','sssggsss','sssggsss','.ssssss.']

def fishing():
    im,d=base('BETTER FISHING LOOT','More chances at rare catches',TEAL)
    d.rectangle((48,497,1232,636),fill='#123d53')
    for x in range(80,1220,115):d.rectangle((x,546,x+65,553),fill='#28869a')
    d.line((175,592,415,289),fill='#c08b55',width=16);d.line((415,289,880,332,880,476),fill=WHITE,width=5)
    d.arc((855,446,895,486),0,180,fill=WHITE,width=6)
    spr(im,GEM,828,350,13)
    spr(im,['..gggggg..','.ggwwwwgg.','ggwwwwwwgg','gggggggggg','gs..gg..sg','gg......gg'],581,434,17)
    for x,y in [(783,349),(1008,412),(544,404)]:sparkle(d,x,y)
    return im

def replant():
    im,d=base('AUTO REPLANT','Harvest. Plant. Grow again.',TEAL)
    for x in [104,503,902]:
        d.rectangle((x,495,x+266,604),fill='#705040');d.rectangle((x,495,x+266,511),fill='#ad8055')
        for dx in [25,87,156,219]:d.rectangle((x+dx,542,x+dx+23,550),fill='#ad8055')
    spr(im,['..aa..aa..','.aaaaaaaa.','aaaaraaaaa','aaraaaaraa','aaaraaaraa','.aaaaaaaa.','...bbbb...','....bb....'],148,339,18)
    spr(im,SEED,572,342,21)
    spr(im,SPROUT,951,399,22)
    arrow(d,389,446,89);arrow(d,788,446,89)
    return im

def quick():
    im,d=base('QUICK BUFF','Buffs, healing and food at a key press',GOLD)
    for x,key,col in [(86,'B',TEAL),(493,'H','#ee776b'),(900,'F',GOLD)]:
        d.rounded_rectangle((x,456,x+284,610),radius=18,fill='#304254',outline=WHITE,width=4)
        d.text((x+142,526),key,anchor='mm',font=font(87),fill=WHITE)
        if key!='F':spr(im,POTION,x+80,287,12,{'a':col,'b':col})
        else:
            d.ellipse((x+45,331,x+235,410),fill=WHITE)
            spr(im,['...gggggg...','..gssssssg..','.gsssssssgg.','gsswsssssssg','gsssswsssssg','.gggggggggg.'],x+67,310,12)
    return im

def seller():
    im,d=base('POTION SELLER','Stock up at your merchants',GOLD)
    d.rectangle((110,294,1165,335),fill='#be735f')
    for x in range(110,1165,105):d.rectangle((x,294,x+51,347),fill=GOLD)
    d.rectangle((120,340,143,619),fill='#94683e');d.rectangle((1134,340,1157,619),fill='#94683e')
    spr(im,['...tttttt...','..ttddddtt..','.ttddddddtt.','.tdwwddwwdt.','.tddddddddt.','..ttddddtt..','...tttttt...','..tttttttt..','.tttttttttt.'],226,369,19)
    for x,col in [(535,TEAL),(742,'#ee776b'),(949,'#b99afa')]:spr(im,POTION,x,368,14,{'a':col,'b':col})
    d.rectangle((109,526,1169,615),fill='#9c6d42');d.rectangle((109,526,1169,547),fill='#d3a56b')
    for x in range(159,1130,180):d.rectangle((x,554,x+5,607),fill='#67472e')
    return im

def tuning():
    im,d=base('DIFFICULTY TUNING','Set the challenge for your world',GOLD)
    # Illustrated shield and three substantial control tracks; no tiny mock UI text.
    d.polygon([(168,314),(355,314),(400,343),(381,503),(263,596),(148,503),(128,343)],fill='#4d6682',outline=WHITE,width=7)
    d.polygon([(263,335),(263,565),(355,490),(374,355)],fill='#283d58')
    spr(im,['...gg...','..gggg..','...gg...','...gg...','...gg...','.gggggg.','...ss...','...ss...'],211,370,13)
    for y,pos,col in [(345,781,TEAL),(444,1001,GOLD),(543,867,'#bc9bff')]:
        d.rounded_rectangle((532,y,1140,y+15),radius=7,fill='#526172')
        d.rounded_rectangle((532,y,pos,y+15),radius=7,fill=col)
        d.rectangle((pos-18,y-22,pos+18,y+37),fill=col)
    return im

def minions():
    im,d=base('KEEP MINIONS','Bring your summons through teleports',TEAL)
    for cx in [249,1011]:
        d.ellipse((cx-112,283,cx+112,620),fill='#57438a',outline='#bf9bff',width=12)
        d.ellipse((cx-81,317,cx+81,586),fill='#202a48',outline=TEAL,width=8)
    arrow(d,423,384,392)
    for x,y,col in [(362,454,TEAL),(556,476,'#bc9bff'),(752,450,GOLD)]:
        spr(im,['aa......aa','abaa..aaba','.abaaaaba.','..aawwaa..','..aakwka..','...aaaa...','....aa....'],x,y,12,{'a':col})
    for x,y in [(210,371),(1100,430),(962,545)]:sparkle(d,x,y,TEAL)
    return im

def pet():
    im,d=base('PET EDITOR','Choose talents, level and color', '#bc9bff')
    spr(im,PET,159,336,24)
    for x,col in [(181,TEAL),(265,GOLD),(349,'#bc9bff')]:d.ellipse((x,596,x+39,635),fill=col)
    for row in range(2):
        for col in range(3):
            x,y=641+170*col,312+152*row
            d.rounded_rectangle((x,y,x+110,y+110),radius=12,fill='#2b374c',outline='#bc9bff',width=5)
            sparkle(d,x+55,y+55,TEAL if row==0 else GOLD,29)
    d.line((567,336,567,594),fill='#4b425f',width=5)
    return im

def boss():
    im,d=base('BOSS BONUS LOOT','Extra rewards for selected victories',GOLD)
    # Fallen crown, treasure chest and three separately colored gems.
    d.polygon([(153,400),(140,316),(203,352),(263,288),(318,352),(385,316),(370,400)],fill=GOLD)
    d.rectangle((151,411,372,434),fill='#b88840')
    d.rounded_rectangle((126,449,403,613),radius=20,fill='#775136',outline=GOLD,width=7)
    d.rectangle((127,495,402,519),fill='#c39553');d.rectangle((247,482,283,538),fill=GOLD)
    for x,y,col in [(548,415,TEAL),(775,352,'#8fc4ff'),(1002,418,'#ee776b')]:
        spr(im,GEM,x,y,20,{'a':col,'b':col})
        sparkle(d,x+70,y-32)
    return im

SCENES=[('betterfishingloot',fishing),('autoreplant',replant),('quickbuff',quick),('potionseller',seller),('difficultytuning',tuning),('keepminions',minions),('peteditor',pet),('bossloot',boss)]
if __name__=='__main__':
    montage=Image.new('RGB',(1280,1440),INK)
    for i,(name,fn) in enumerate(SCENES):
        im=fn();path=OUT/(name+'_logo.png');im.save(path,optimize=True)
        assert path.stat().st_size<1_000_000
        montage.paste(im.resize((640,360)),((i%2)*640,(i//2)*360))
        print(name,im.size,path.stat().st_size)
    # Review image is intentionally outside the repository's tracked release assets.
    montage.save(OUT.parent/'build'/'art_b_montage.png')
