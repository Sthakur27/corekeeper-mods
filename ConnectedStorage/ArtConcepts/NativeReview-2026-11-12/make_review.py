"""Reproduce 16x16 review candidates and equal-scale native comparison.

Native reference textures stay in ignored build output, never packaged.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / 'build/connectedstorage/native-review-20261112'
OUT.mkdir(parents=True, exist_ok=True)
profiles = {
    'Workbench': ('workbench.png', (161, 160, 1092, 1135)),
    'OctarineChest': ('octarine.png', (480, 199, 1056, 831)),
}
stats = {}
palette_hex = ['20333b','35444e','4c576e','68778f','8a9fbe','b7c4dc','eee1cf',
               '38251f','623921','91502a','bd6c35','df8c4c','ffb370',
               '063e43','075b5d','0c8984','21bba6','5bdfbe','99f1d5',
               '956b26','e4b84c']
palette = Image.new('P', (1, 1))
colors = [tuple(bytes.fromhex(c)) for c in palette_hex]
palette.putpalette([v for c in colors for v in c] + list(colors[0])*(256-len(colors)))
for name, (source, bounds) in profiles.items():
    original = Image.open(HERE / 'sources' / source).convert('RGBA')
    # Deliberately normalize each isolated front view to the 16x16 canvas.
    # Crops exclude the generated surrounding halo; alpha threshold rejects soft pixels.
    im = original.crop(bounds).resize((16, 16), Image.Resampling.NEAREST)
    alpha = im.getchannel('A').point(lambda a: 255 if a >= 240 else 0)
    # A shared material palette retains rare teal/gold accents that adaptive
    # quantization otherwise merged into the much larger stone surface.
    im = im.convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE).convert('RGBA')
    im.putalpha(alpha)
    im.save(HERE / (name + '-16.png'))
    assert im.size == (16, 16) and set(alpha.getdata()) <= {0, 255}
    stats[name] = {'size': list(im.size), 'bbox': alpha.getbbox(),
                   'opaque_pixels': sum(a == 255 for a in alpha.getdata()),
                   'visible_colors': len({p[:3] for p in im.getdata() if p[3]}),
                   'source_crop': bounds, 'alpha_threshold': 240}

fontpath = 'C:/Windows/Fonts/segoeui.ttf'
font = ImageFont.truetype(fontpath, 17)
small = ImageFont.truetype(fontpath, 14)
title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 23)
ref = ROOT / 'build/connectedstorage/art-reference'
current = ROOT / 'ConnectedStorage/Unity/Art'
rows = [
    [('Vanilla wooden bench', ref/'workbenchWood_down.png'),
     ('Vanilla electronics table', ref/'electronicsTable_down.png'),
     ('Current workbench', current/'Workbench.png'),
     ('Proposed workbench', HERE/'Workbench-16.png')],
    [('Vanilla copper chest', ref/'chestCopper.png'),
     ('Vanilla ancient chest', ref/'chestAncient.png'),
     ('Current Octarine unit', current/'OctarineChest.png'),
     ('Proposed Octarine unit', HERE/'OctarineChest-16.png')],
]
board = Image.new('RGB', (1080, 820), '#242a32')
d = ImageDraw.Draw(board)
d.text((24, 15), 'NATIVE-SIZE REVIEW | front/down views', font=title, fill='#f4ddb3')
d.text((24, 50), 'Every sprite is 16 x 16. Large views: exact 12x nearest-neighbor. Small views: 1x file pixels.', font=small, fill='#c0c8d2')
for row, entries in enumerate(rows):
    y = 100 + row*340
    for col, (label, path) in enumerate(entries):
        x = 24+col*266
        im = Image.open(path).convert('RGBA')
        assert im.size == (16, 16), (label, im.size)
        d.text((x, y), label, font=font, fill='#f4ddb3' if col==3 else '#e1e6eb')
        d.text((x, y+26), '16 x 16 | 12x view', font=small, fill='#9daab9')
        pos=(x+20,y+53)
        # Identical backdrop and canvas baseline for all candidates and references.
        d.rectangle((pos[0],pos[1],pos[0]+191,pos[1]+191), fill='#303a43')
        enlarged=im.resize((192,192),Image.Resampling.NEAREST)
        board.paste(enlarged,pos,enlarged)
        d.line((x+12,y+246,x+220,y+246), fill='#5d7376')
        board.paste(im,(x+20,y+267),im)
        d.text((x+48,y+265),'1x',font=small,fill='#a9b5c3')
d.text((24,790),'Review candidates only. No production replacement. Native references are comparison-only.',font=small,fill='#a9b5c3')
board.save(OUT/'comparison.png')
(HERE/'measurements.json').write_text(json.dumps(stats,indent=2)+'\n')
print(json.dumps(stats,indent=2))
print(OUT/'comparison.png')
