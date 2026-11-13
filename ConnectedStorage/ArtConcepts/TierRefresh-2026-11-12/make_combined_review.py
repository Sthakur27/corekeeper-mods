"""Compose original candidate sprites with native references, all labeled.
Output stays in ignored build because it includes extracted game art.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
TIERS = ['Copper','Tin','Iron','Scarlet','Octarine','Galaxite','Solarite','Pandorium','Relucite']
themes = ['Practical starter','Fitted reinforcement','Heavy frame','Powered channel',
          'Arcane instrument','Linked assembly','Luminous finish','Sealed precision','Integrated masterwork']
native = ROOT/'build/connectedstorage/color-audit-20261112'
out = ROOT/'build/connectedstorage/combined-review-20261112'
out.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',14)
title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',24)
board = Image.new('RGB',(1200,1270),'#242a32')
d = ImageDraw.Draw(board)
d.text((22,15),'RUNIC STORAGE | native-size combined review',font=title,fill='#f4ddb3')
d.text((22,49),'Units and gems: 16x16, enlarged 8x. Native material bars: 4x. Small candidate views: 1x file pixels.',font=small,fill='#b5c3ce')
metrics = {}
def sprite(path,x,y,scale,validate=False):
    im=Image.open(path).convert('RGBA')
    if validate:
        assert im.size==(16,16), (str(path),im.size)
        assert set(im.getchannel('A').getdata()) <= {0,255}, str(path)
        metrics[str(path.relative_to(ROOT))]={'size':list(im.size),'bbox':im.getbbox(),
          'colors':len({p[:3] for p in im.getdata() if p[3]})}
    resized=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST)
    board.paste(resized,(x,y),resized)
for i,(tier,theme) in enumerate(zip(TIERS,themes)):
    x=22+(i%3)*396; y=88+(i//3)*285
    d.text((x,y),f'{i+1}. {tier}',font=title,fill='#f4ddb3')
    d.text((x,y+32),theme,font=small,fill='#b5c3ce')
    unit=HERE/'tiers'/(tier+'Chest-16.png')
    d.text((x,y+55),'Unit 8x',font=small,fill='#e0e6eb')
    sprite(unit,x,y+79,8,True)
    sprite(unit,x,y+221,1)
    if tier!='Copper':
        gem=HERE/'tiers'/(tier+'Gem-16.png')
        d.text((x+144,y+55),'Gem 8x',font=small,fill='#e0e6eb')
        sprite(gem,x+144,y+79,8,True)
        sprite(gem,x+144,y+221,1)
    else:
        d.text((x+144,y+110),'No upgrade gem',font=small,fill='#8795a2')
    d.text((x+287,y+55),'Native bar',font=small,fill='#e0e6eb')
    sprite(native/('lootsprite_'+tier.lower()+'Bar.png'),x+287,y+104,4)
    d.line((x,y+249,x+368,y+249),fill='#47545f')
d.text((22,947),'Workbench | hammer removed; crystal-setting surface retained',font=title,fill='#f4ddb3')
bench_refs=ROOT/'build/connectedstorage/art-reference'
items=[('Vanilla wooden bench',bench_refs/'workbenchWood_down.png'),
       ('Vanilla electronics table',bench_refs/'electronicsTable_down.png'),
       ('Current mod workbench',ROOT/'ConnectedStorage/Unity/Art/Workbench.png'),
       ('Proposed workbench',ROOT/'ConnectedStorage/ArtConcepts/WorkbenchRefresh-2026-11-12/Workbench-16.png')]
for i,(label,path) in enumerate(items):
    x=22+i*295
    d.text((x,989),label,font=font,fill='#f4ddb3' if i==3 else '#e0e6eb')
    sprite(path,x+55,1022,8,i==3)
    sprite(path,x+55,1161,1)
    d.text((x+82,1159),'1x',font=small,fill='#aab8c5')
d.text((22,1213),'Written tier themes precede art. Palette evidence comes from installed native bar/ore sprites.',font=small,fill='#aab8c5')
d.text((22,1237),'Review candidates only: no runtime replacement, directional variants, bundle build or in-game validation.',font=small,fill='#aab8c5')
board.save(out/'combined-review.png')
(HERE/'combined-validation.json').write_text(json.dumps(metrics,indent=2)+'\n')
print(f'Validated {len(metrics)} candidates; wrote {out / "combined-review.png"}')
