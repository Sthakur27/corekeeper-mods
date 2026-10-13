"""Original collection/overhaul cover scenes. Run from any directory with Python + Pillow."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from make_art import sprite

OUT = Path(__file__).resolve().parent

def font(size):
    return ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf', size)

def draw_cover(collection=False):
    im = Image.new('RGB', (1280, 720), '#101b2c')
    d = ImageDraw.Draw(im)
    accent = '#81e4cb' if collection else '#ffcf78'
    for x in range(0, 1280, 40):
        for y in range(0, 720, 40):
            if (x // 40 + y // 40) % 3 == 0:
                d.rectangle((x+2, y+2, x+38, y+38), fill='#142238')
    d.rectangle((0, 0, 1280, 235), fill='#101b2c')
    d.text((64, 34), "SID'S CORE KEEPER MODS", font=font(26), fill=accent)
    d.text((60, 76), 'Choose your mods' if collection else "Sid's Overhaul", font=font(78), fill='#ffffff')
    d.text((64, 181), 'Build a collection that fits how you play.' if collection else 'Everyday comforts. One mod.', font=font(31), fill='#dde8f5')
    # Purpose-built symbols: seedling, potion, gear and chest. No game assets.
    icons = [
        ['..gg...gg..','..GGg.gGG..','...gGgGg...','.....g.....','.....g.....','...bbbbb...','....bbb....'],
        ['....ss....','....ss....','...ssss...','...swws...','..sccccs..','..sccccs..','...ssss...'],
        ['..ss..ss..','...ssss...','..ssssss..','..ss..ss..','..ssssss..','...ssss...','..ss..ss..'],
        ['.bbbbbbbb.','bggggggggb','bggggggggb','bbbbbbbbbb','bgggssgggb','bgggssgggb','.bbbbbbbb.'],
    ]
    palette = {'g':(119,224,159),'G':(51,155,117),'b':(152,96,52),'s':(213,226,245),'w':(255,255,255),'c':(98,199,239)}
    labels = ['Grow', 'Prepare', 'Customize', 'Explore']
    if collection:
        # A separate scene: a shelf of selectable mod cards, not the Overhaul's linked toolkit.
        icons += [
            ['...cccc....','..cccccc...','.ccccccccc.','ccwcccccccc','.ccccccccc.','..cccccc...','...cccc....'],
            ['....ss.....','...ssss....','..ssssss...','.ssssssss..','....bb.....','....bb.....','....bb.....'],
        ]
        labels = ['Farming', 'Supplies', 'Settings', 'Storage', 'Fishing', 'Equipment']
        for i in range(6):
            x = 72 + (i % 3) * 390
            y = 273 + (i // 3) * 170
            d.rounded_rectangle((x,y,x+354,y+145), radius=14, fill='#20354d', outline=accent, width=2)
            sp = sprite(icons[i], palette, 11)
            im.paste(sp, (x+18,y+30), sp)
            d.text((x+152,y+67), labels[i], font=font(27), fill='#ffffff', anchor='lm')
            d.rectangle((x+306,y+16,x+333,y+43),fill=accent)
            d.line((x+311,y+29,x+318,y+36,x+329,y+23),fill='#101b2c',width=3)
        d.text((640, 637), 'INDIVIDUAL MODS • PICK WHAT YOU WANT', font=font(27), fill=accent, anchor='mt')
        im.save(OUT / 'collection_logo.png', optimize=True)
        return
    xs = [72, 366, 660, 954]
    for i,x in enumerate(xs):
        d.rounded_rectangle((x, 290, x+254, 558), radius=18, fill='#20354d', outline=accent, width=3)
        sp = sprite(icons[i], palette, 16)
        im.paste(sp, (x+(254-sp.width)//2, 337), sp)
        d.text((x+127, 500), labels[i], font=font(29), fill='#ffffff', anchor='mt')
        if collection:
            d.rounded_rectangle((x+204,303,x+239,338), radius=6, fill=accent)
            d.line((x+212,320,x+220,328,x+233,311), fill='#101b2c', width=4)
    if not collection:
        d.line((199,578,199,597,1081,597,1081,578), fill=accent, width=4)
        d.line((640,597,640,621), fill=accent, width=4)
    d.text((640, 637), 'INDIVIDUAL MODS • PICK WHAT YOU WANT' if collection else 'ALL-IN-ONE MOD • OPTIONS BUILT IN', font=font(27), fill=accent, anchor='mt')
    im.save(OUT / ('collection_logo.png' if collection else 'overhaul_logo.png'), optimize=True)

if __name__ == '__main__':
    draw_cover()
    draw_cover(True)
