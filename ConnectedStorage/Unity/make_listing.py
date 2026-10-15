"""Compose an original connected-storage scene from this mod's own pixel sprites."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root = Path(__file__).resolve().parents[2]
art = Path(__file__).parent / 'Art'
out = Image.new('RGB', (960, 540), '#101c29')
d = ImageDraw.Draw(out)
for y in range(150, 540, 24):
    for x in range(0, 960, 24):
        d.rectangle((x+1,y+1,x+22,y+22), fill=('#152331' if (x//24+y//24)%3 else '#182936'))
bold = 'C:/Windows/Fonts/seguisb.ttf'
regular = 'C:/Windows/Fonts/segoeui.ttf'
d.text((48,28),'CONNECTED STORAGE',font=ImageFont.truetype(bold,48),fill='#e2fffc')
d.text((50,95),'One terminal. Every connected unit.',font=ImageFont.truetype(regular,24),fill='#95b9c3')
positions = [('Terminal',100,270),('CopperChest',300,270),('IronChest',470,270),('SolariteChest',640,270),('PandoriumChest',810,270)]
for _,x,y in positions[1:]:
    d.line((x-170,y+70,x,y+70), fill='#4ccfc6',width=9)
for name,x,y in positions:
    sprite=Image.open(art/(name+'.png')).convert('RGBA')
    sprite=sprite.resize((sprite.width*6,sprite.height*6),Image.Resampling.NEAREST)
    out.paste(sprite,(x-sprite.width//2,y-sprite.height//2),sprite)
d.text((48,451),'SEARCH  /  CRAFT  /  EXPAND',font=ImageFont.truetype(bold,26),fill='#b7eee8')
d.text((49,492),'Eight storage tiers · Sequential upgrades · Shared storage',font=ImageFont.truetype(regular,19),fill='#8eb2bf')
destination = root / 'release' / 'connectedstorage_logo.png'
out.save(destination)
print(destination)
