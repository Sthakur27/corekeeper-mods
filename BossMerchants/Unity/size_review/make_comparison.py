"""Render measured native/current/proposed comparisons, never builds or installs.

Run after import_approved_art.py --profile Unity/size_review/native_size_profile.json
--out BossMerchants/ArtConcepts/NativeSizeReview-2026-11-12/sprites.
Body guides are explicit manual semantic annotations, not a segmentation claim.
"""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
MOD = HERE.parents[1]
ROOT = MOD.parent
CURRENT = MOD / 'Unity/Art/final'
REVIEW = MOD / 'ArtConcepts/NativeSizeReview-2026-11-12'
PROPOSED = REVIEW / 'sprites'
REF = ROOT / 'build/bossmerchants'
PROFILE = json.loads((HERE / 'native_size_profile.json').read_text())
KEYS = list(PROFILE['npcs'])
NAMES = ['Grout', 'Ottoline', 'Fizzwick', 'Hemlock', 'Ankhra', 'Tridra', 'Wren', 'Tilly']
FONT = 'C:/Windows/Fonts/consola.ttf'
BODY = '#6bd3bf'


def font(size): return ImageFont.truetype(FONT, size)


def frame(root, key):
    info = json.loads((root / 'merchants' / key / 'sheets.json').read_text())
    t = info['textures'][0]
    im = Image.open(root / 'merchants' / t['output']).convert('RGBA')
    if info['kind'] == 'spriteAsset': return im.crop((0, 0, im.width // t['srcFrameCount'], im.height))
    x, y, w, h = map(int, t['legacySprites'][0]['rect'])
    return im.crop((x, im.height-y-h, x+w, im.height-y))


def native_frame(key):
    values = {
        'caveling': ('CavelingMerchant', 'Merchant', (0, 0, 64, 64)),
        'fishing': ('FishingMerchant', 'Fisher_merchant', (0, 0, 26, 40)),
        'slime': ('SlimeMerchant', 'Slime_merchant', (0, 0, 30, 30)),
        'void': ('VoidMerchant', 'voidMerchant_idle', (0, 0, 16, 35))}
    folder, name, box = values[key]
    return Image.open(REF / 'reference/merchants' / folder / (name+'.png')).convert('RGBA').crop(box)


def wh(box): return [box[2]-box[0], box[3]-box[1]]


def size(im): return wh(im.getbbox())


def metric(im):
    return {'visiblePx': size(im), 'opaquePixels': int(np.count_nonzero(np.array(im)[:, :, 3])),
            'visibleWorldUnits': [round(v/16, 4) for v in size(im)]}


def guide_after(box, old, new):
    a, b = old.getbbox(), new.getbbox(); aw, ah = wh(a); bw, bh = wh(b)
    return [b[0]+round((box[0]-a[0])*bw/aw), b[1]+round((box[1]-a[1])*bh/ah),
            b[0]+round((box[2]-a[0])*bw/aw), b[1]+round((box[3]-a[1])*bh/ah)]


def draw_sprite(board, im, cx, baseline, zoom, body=None):
    bb = im.getbbox(); tile = im.crop(bb); w, h = tile.size
    x = cx-w*zoom//2; y = baseline-h*zoom
    board.alpha_composite(tile.resize((w*zoom, h*zoom), Image.Resampling.NEAREST), (x, y))
    if body:
        p = [x+(body[0]-bb[0])*zoom, y+(body[1]-bb[1])*zoom,
             x+(body[2]-bb[0])*zoom-1, y+(body[3]-bb[1])*zoom-1]
        ImageDraw.Draw(board).rectangle(p, outline=BODY, width=1)


def main():
    REVIEW.mkdir(exist_ok=True)
    # All geometry in this sheet uses the verified common PPU16 / scale1.
    report = {'ppu': 16, 'prefabScale': [1,1,1],
              'guides': PROFILE['measurement'], 'npcs': {}, 'items': {}}
    board = Image.new('RGBA', (1240, 1480), '#22242d'); d = ImageDraw.Draw(board)
    d.text((24, 12), 'NPC SIZE REVIEW  |  Native / Installed / Proposed', font=font(24), fill='white')
    d.text((24, 46), 'All figures at 5x. Teal boxes: annotated bodies, excluding projecting props.', font=font(18), fill=BODY)
    d.text((24, 70), 'Scale 1,1,1 + 16 px/world unit. Shared foot line. Spawn/hit squash excluded.', font=font(17), fill='#b8bfca')
    for i, key in enumerate(KEYS):
        spec = PROFILE['npcs'][key]; refkey=spec['reference']; native_spec=PROFILE['nativeBodies'][refkey]
        a, b, c = native_frame(refkey), frame(CURRENT,key), frame(PROPOSED,key)
        guides=[native_spec['box'],spec['bodyBox'],guide_after(spec['bodyBox'],b,c)]
        x=(i%2)*620; y=110+(i//2)*340
        d.text((x+18,y), NAMES[i]+'  /  native '+refkey, font=font(21), fill='white')
        for j,(title,im,body) in enumerate(zip(['Native','Installed','Proposed'],[a,b,c],guides)):
            cx=x+105+j*200
            d.text((cx-50,y+32),title,font=font(17),fill='#bec7d0')
            draw_sprite(board,im,cx,y+293,5,body)
            d.text((cx-73,y+303),'body '+str(wh(body)[0])+'x'+str(wh(body)[1]),font=font(17),fill=BODY)
        d.line((x+20,y+294,x+600,y+294), fill='#626777')
        report['npcs'][key]={'native':metric(a),'before':metric(b),'after':metric(c),
                            'nativeBodyPx':wh(guides[0]),'beforeBodyPx':wh(guides[1]),
                            'afterBodyGuidePx':wh(guides[2]),'reason':spec['reason']}
    board.save(REVIEW/'npc-comparison.png')

    board=Image.new('RGBA',(1240,1140),'#22242d');d=ImageDraw.Draw(board)
    d.text((24,12),'SPAWN ITEM SIZE REVIEW  |  Native / Installed / Proposed',font=font(23),fill='white')
    d.text((24,45),'Upper sprites: placed / full icon at 6x. Lower sprites: small icon at 4x.',font=font(17),fill='#bec7d0')
    d.text((24,70),'Full icons equal placed art for these objects. Canvas16x16; visible bounds below.',font=font(17),fill='#bec7d0')
    for i,key in enumerate(KEYS):
        spec=PROFILE['items'][key];refkey=spec['reference']
        a=Image.open(REF/'idol_reference'/(refkey+'_icon.png')).convert('RGBA')
        native_name=refkey.split('_',1)[1]
        placed=Image.open(REF/'idol_reference'/('idol_'+native_name+'_placed.png')).convert('RGBA')
        assert a.size==placed.size and np.array_equal(np.array(a)[np.array(a)[:,:,3]>0],np.array(placed)[np.array(a)[:,:,3]>0]), refkey
        b=Image.open(CURRENT/'idols'/(key+'_icon.png')).convert('RGBA')
        c=Image.open(PROPOSED/'idols'/(key+'_icon.png')).convert('RGBA')
        for path,im in [(CURRENT/'idols'/(key+'_placed.png'),b),(PROPOSED/'idols'/(key+'_placed.png'),c)]:
            assert Image.open(path).convert('RGBA').tobytes()==im.tobytes(), str(path)
        smalls=[Image.open(REF/'idol_reference'/(refkey+'_smallIcon.png')).convert('RGBA'),
                Image.open(CURRENT/'idols'/(key+'_smallIcon.png')).convert('RGBA'),
                Image.open(PROPOSED/'idols'/(key+'_smallIcon.png')).convert('RGBA')]
        x=(i%2)*620;y=110+(i//2)*250
        d.text((x+18,y),NAMES[i]+' item  /  '+native_name,font=font(19),fill='white')
        for j,(title,im,small) in enumerate(zip(['Native','Installed','Proposed'],[a,b,c],smalls)):
            cx=x+105+j*200
            d.text((cx-50,y+30),title,font=font(16),fill='#bec7d0')
            draw_sprite(board,im,cx,y+150,6)
            dims=size(im);d.text((cx-38,y+157),str(dims[0])+'x'+str(dims[1])+' px',font=font(16),fill=BODY)
            draw_sprite(board,small,cx,y+220,4)
        report['items'][key]={'native':metric(a),'before':metric(b),'after':metric(c),
                             'smallIconPx':[size(s) for s in smalls],'reason':spec['reason']}
    board.save(REVIEW/'item-comparison.png')
    (REVIEW/'measurements.json').write_text(json.dumps(report,indent=2)+'\n',newline='\n')
    print('Saved', REVIEW/'npc-comparison.png', REVIEW/'item-comparison.png')


if __name__=='__main__':main()
