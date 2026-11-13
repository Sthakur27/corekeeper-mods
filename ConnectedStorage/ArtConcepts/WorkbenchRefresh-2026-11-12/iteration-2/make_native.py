"""Replace inner tabletop only; assert all other native pixels unchanged."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont
HERE=Path(__file__).resolve().parent
base=Image.open(HERE.parent/'Workbench-16.png').convert('RGBA')
colors=['20333b','35444e','4c576e','68778f','8a9fbe','b7c4dc','eee1cf',
        '38251f','623921','91502a','bd6c35','df8c4c','ffb370','956b26','e4b84c']
rgb=[tuple(bytes.fromhex(c)) for c in colors]
palette=Image.new('P',(1,1))
palette.putpalette([v for c in rgb for v in c]+list(rgb[0])*(256-len(rgb)))
crop=(161,160,1092,1135)
mask=(3,3,13,9) # exclusive right/bottom; inside tabletop, excludes braces and front
stats={}
for variant in ('a','b'):
    source=Image.open(HERE/f'source-{variant}.png').convert('RGBA')
    native=source.crop(crop).resize((16,16),Image.Resampling.NEAREST)
    if variant=='a':
        # The generated setting was smaller than the requested 5x4 cells.
        # Normalize just that inset to its intended native footprint; no new art.
        setting=source.crop((527,442,726,594)).resize((5,4),Image.Resampling.NEAREST)
        native.paste(setting,(5,4))
    native=native.convert('RGB').quantize(palette=palette,dither=Image.Dither.NONE).convert('RGBA')
    result=base.copy()
    result.paste(native.crop(mask),mask[:2])
    changed=[]
    for y in range(16):
        for x in range(16):
            if not(mask[0]<=x<mask[2] and mask[1]<=y<mask[3]):
                assert result.getpixel((x,y))==base.getpixel((x,y)),(x,y)
            elif result.getpixel((x,y))!=base.getpixel((x,y)):
                changed.append([x,y])
    assert set(result.getchannel('A').get_flattened_data()) <= {0,255}
    assert result.getchannel('A').tobytes()==base.getchannel('A').tobytes()
    result.save(HERE/f'Workbench-{variant.upper()}-16.png')
    stats[variant]={'size':[16,16],'mask_xyxy':mask,'crop_xyxy':crop,
        'outside_mask_exact':True,'alpha_exact':True,'changed_pixel_count':len(changed),
        'visible_colors':len({p[:3] for p in result.get_flattened_data() if p[3]}),
        'changed_pixels':changed}
(HERE/'measurements.json').write_text(json.dumps(stats,indent=2)+'\n')
board=Image.new('RGB',(1000,340),'#242a32');d=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
for i,(label,path) in enumerate([
    ('Previous: teal socket',HERE.parent/'Workbench-16.png'),
    ('A: recessed setting',HERE/'Workbench-A-16.png'),
    ('B: copper assembly plate',HERE/'Workbench-B-16.png')]):
    im=Image.open(path).convert('RGBA');x=25+i*330
    d.text((x,15),label,font=font,fill='#eed7ae')
    large=im.resize((192,192),Image.Resampling.NEAREST)
    board.paste(large,(x+30,55),large)
    board.paste(im,(x+30,270),im)
    d.text((x+60,267),'1x; large view 12x',font=font,fill='#b6c3cf')
d.text((25,310),'All candidates are 16x16. Outside the inner tabletop: exact original pixels.',font=font,fill='#b6c3cf')
board.save(HERE/'before-after.png')
print(json.dumps({k:{s:v for s,v in d.items() if s!='changed_pixels'} for k,d in stats.items()}))
