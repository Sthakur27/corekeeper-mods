"""Deterministic front/down native candidate from the imagegen edit."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
colors = ['20333b','35444e','4c576e','68778f','8a9fbe','b7c4dc','eee1cf',
          '38251f','623921','91502a','bd6c35','df8c4c','ffb370',
          '063e43','075b5d','0c8984','21bba6','5bdfbe','99f1d5',
          '956b26','e4b84c']
rgb = [tuple(bytes.fromhex(c)) for c in colors]
palette = Image.new('P', (1, 1))
palette.putpalette([v for c in rgb for v in c] + list(rgb[0])*(256-len(rgb)))
crop = (161,160,1092,1135)  # Preserve prior candidate's normalization profile.
im = Image.open(HERE/'source-no-hammer.png').convert('RGBA').crop(crop)
im = im.resize((16,16),Image.Resampling.NEAREST)
alpha = im.getchannel('A').point(lambda a: 255 if a>=240 else 0)
im = im.convert('RGB').quantize(palette=palette,dither=Image.Dither.NONE).convert('RGBA')
im.putalpha(alpha)
im.save(HERE/'Workbench-16.png')
assert im.size==(16,16) and set(alpha.get_flattened_data()) <= {0,255}
metrics={'size':[16,16],'alpha_levels':sorted(set(alpha.get_flattened_data())),
         'opaque_pixels':sum(a==255 for a in alpha.get_flattened_data()),
         'visible_colors':len({p[:3] for p in im.get_flattened_data() if p[3]}),
         'crop':crop,'alpha_threshold':240,'state':'review-only; front/down only'}
(HERE/'measurements.json').write_text(json.dumps(metrics,indent=2)+'\n')
board=Image.new('RGB',(700,330),'#242a32'); d=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
for i,(label,path) in enumerate([
    ('Previous: with hammer',HERE.parent/'NativeReview-2026-11-12/Workbench-16.png'),
    ('Revised: crystal-setting surface',HERE/'Workbench-16.png')]):
    item=Image.open(path).convert('RGBA'); x=30+i*340
    d.text((x,15),label,font=font,fill='#eed7ae')
    large=item.resize((192,192),Image.Resampling.NEAREST)
    board.paste(large,(x+35,55),large)
    board.paste(item,(x+35,270),item)
    d.text((x+65,267),'1x; large view 12x',font=font,fill='#b6c3cf')
board.save(HERE/'before-after.png')
print(json.dumps(metrics))
