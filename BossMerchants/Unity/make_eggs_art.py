"""Boss Merchants art for merchant key `eggs`: Ankhra the Clutch-Keeper + the Brood Egg Idol (Ra-Akar).

    python -I BossMerchants/Unity/make_eggs_art.py

Ankhra is a squat scarab beetle-folk from Ra-Akar's brood: green spiked carapace like the Sand Titan,
a single golden eye under a curved gold horn, red-orange claws, a gold-and-lapis collar, and always one egg
cradled in front. Her emote holds the egg out, it wobbles, cracks, and a hatchling peeks out.

Rig: VoidMerchant (SpriteAsset `voidMerchant`): idle 128x35 (8 x 16x35), move 96x34 (6 x 16x34),
emote 621x37 (27 x 23x37, body pivot x = 8). Pivot is bottom centre, so the feet stand on the last row.

Everything is drawn from the hand-authored pixel grids below (no vanilla pixels are copied). The preview
sheet reads the extracted vanilla references in build/bossmerchants for side-by-side comparison only.
Outputs (paths relative to BossMerchants/):
  Unity/Art/merchants/eggs/eggs__voidMerchant_{idle,move,emote}.png + sheets.json
  Unity/Art/idols/eggs_{icon,smallIcon,placed}.png
  Unity/Art/previews/eggs.png
Validation (exit 1 on failure): sheet sizes = rig sizes, every frame has pixels and stays inside its cell,
hard alpha only, idols 16x16, feet on the ground row.
"""
import json, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.dirname(HERE)
ROOT = os.path.dirname(MOD)
ART = os.path.join(HERE, 'Art')
OUT_M = os.path.join(ART, 'merchants', 'eggs')
OUT_I = os.path.join(ART, 'idols')
OUT_P = os.path.join(ART, 'previews')
REF = os.path.join(ROOT, 'build', 'bossmerchants')

# ---------------------------------------------------------------- palette
# Ramps sampled from vanilla Ra-Akar (Scarab_boss) and the Ra-Akar urn; shadows shift cool, highlights warm.
PAL = {
    # carapace green (Scarab_boss): warm light -> teal-indigo outline
    'g': '#a3ce4a', 'G': '#5ca537', 'h': '#239029', 'H': '#0a6d27', 'k': '#073c31', 'K': '#0e2522',
    # gold horn/spikes/collar (Scarab_boss spikes)
    'y': '#f2df3a', 'Y': '#e2a12c', 'o': '#c16436',
    # red-orange claws/legs (Scarab_boss legs), maroon outline
    'l': '#c16436', 'r': '#c5460a', 'R': '#91210b', 'd': '#5e2a28',
    # lapis collar band (Ra-Akar urn blue)
    'B': '#4f7fd0', 'n': '#2c4fa6', 'N': '#1a2557',
    # egg shell (vanilla Egg icon cream ramp), outline browns
    'e': '#fffbe8', 'E': '#efe2c0', 'f': '#d6bf8e', 'q': '#ad8f62', 'F': '#6f4f35',
    # eye pupil
    'p': '#23281e',
    # sandstone (idol plinth, desert temple ramp)
    'S': '#f0deae', 's': '#d8bb7c', 't': '#b08a52', 'T': '#7c5634', 'u': '#45291f',
}
RGBA = {k: tuple(int(v[i:i + 2], 16) for i in (1, 3, 5)) + (255,) for k, v in PAL.items()}


def grid(rows):
    w = max(len(r) for r in rows)
    assert all(len(r) == w for r in rows), [len(r) for r in rows]
    return rows


def put(img, rows, x0, y0, skip='.'):
    W, H = img.size
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == skip or ch == ' ':
                continue
            x, y = x0 + i, y0 + j
            if 0 <= x < W and 0 <= y < H:
                img.putpixel((x, y), RGBA[ch])


# ---------------------------------------------------------------- Ankhra parts (coordinates on a 16x35 cell)
# Top-down 3/4 view: the carapace is seen mostly from above (big lit top plane, seam running toward the
# camera, dark front rim), the head sits low at the front under that rim, the horn leans back over the shell.
SHELL = grid([            # y = 7
    '....hhhhhhHK....',
    '..hhgggGHGGGHK..',
    '.hggeegGHGGGGHK.',
    'hgggeggGHGGGGhHK',
    'hggggGGGHGGGGhHK',
    'hgyGGGGGHGGGyhHK',
    'hgoGGGGGHGGGohHK',
    'kGGGGGGGHGGGhhHK',
    'kGGGGGGGHGGhhHHK',
    'kGyGGGGGHGhhyHHK',
    'khoGGGGhHhhhoHHK',
    'khhhhhhhHhhhHHHK',
    '.kHHHHHHHHHHHHK.',
    '..kKKKKKKKKKKK..',
])
HEAD = grid([             # y = 12: horn leaning back (lit top), head top plane, eye low on the face
    '.......dd.......',
    '......dyyd......',
    '......dyYd......',
    '......oyYd......',
    '......dyYd......',
    '.....kdyYdk.....',
    '....kgggyYgk....',
    '...kggGGGGhk....',
    '...kgGGGGGhHk...',
    '...hGGkkkkhHk...',
    '...kGkeyyYkHk...',
    '...khkyppYkHk...',
    '...khhkYokhHK...',
    '....kHhkkhHK....',
    '.....dr..rd.....',
])
HEAD_BLINK = {10: '...kGkkkkkkHk...', 11: '...khkGGGhkHk...', 12: '...khhkkkkhHK...'}
COLLAR = grid([           # y = 24: gold and lapis broad collar, lit top edge, dark underside
    '..NyYN....NYoN..',
    '.NyyYYYYYYYYYoN.',
    '.NBBBnnnnnnnnnN.',
    '..NNnNNNNNNnNN..',
])
BELLY = grid([            # y = 27: short banded abdomen, darker toward the floor
    '...hgGGGGGhHk...',
    '...khhhhhhHHK...',
    '...kgGGGGhhHK...',
    '....kHHHHHHK....',
    '.....kKKKKK.....',
])
LEG_L = grid([            # x = 0, y = 30: small feet
    '....dd..',
    '...dld..',
    '..dlrd..',
    '.dlrRd..',
    '.ddddd..',
])
LEG_R = grid([            # x = 8, y = 30
    '..dd....',
    '..dld...',
    '..drRd..',
    '..drRRd.',
    '..ddddd.',
])
EGG = grid([              # 8 x 9, idle at x = 4, y = 25: tilted toward the camera, lit top oval, dark underside
    '..qqqF..',
    '.qeeeEF.',
    'qeeeeEfF',
    'qEeeEEfF',
    'qEEEEEfF',
    'FEEEEffF',
    'FfEEffqF',
    '.FffqqF.',
    '..FFFF..',
])
EGG_CAP = EGG[:4] + ['FqeFEFfF']               # top of a cracked egg (lifted when hatching)
EGG_BASE = ['FEFFEFFF'] + EGG[5:]              # bottom half with a jagged rim (rows 4..8)
CRACK = [(1, 4), (2, 3), (3, 4), (4, 4), (5, 3), (6, 4)]
BABY = grid(['.hhhk.', 'hgyYhk', 'kGGhHK'])     # hatchling peeking (lit head top), at egg (x+1, y+2)
BABY_BLINK = grid(['.hhhk.', 'hgGGhk', 'kGGhHK'])
ARM_L = grid([            # x = 1, y = 25 (cradling); lit tops on the forearms
    'dd....',
    'dld...',
    'dld...',
    'drld..',
    '.drld.',
    '..dlld',
    '...dd.',
])
ARM_R = grid([            # x = 9, y = 25 (cradling)
    '....dd',
    '...dld',
    '...drd',
    '..dlRd',
    '.dlRd.',
    'dllRd.',
    '.dd...',
])
ARM_L_DOWN = grid([       # x = 1, y = 24 (hanging while the other claw holds the egg out)
    'dd..',
    'dld.',
    'dld.',
    'drd.',
    'dlrd',
    'dddd',
])
SPARK = grid(['.y.', 'yey', '.y.'])
SPARK_S = grid(['y'])


def draw_egg(img, x, y, state='whole', baby_blink=False):
    if state == 'whole':
        put(img, EGG, x, y)
    elif state == 'crack':
        put(img, EGG, x, y)
        for (cx, cy) in CRACK:
            img.putpixel((x + cx, y + cy), RGBA['F'])
    elif state in ('open', 'open2'):
        lift = 2 if state == 'open' else 3
        put(img, BABY_BLINK if baby_blink else BABY, x + 1, y + 2)
        put(img, EGG_BASE, x, y + 4)
        put(img, EGG_CAP, x + (1 if state == 'open' else 0), y - lift)


def held_arm(img, ox, oy, ex, ey):
    """Right claw holding the egg out: upper arm down from the collar, forearm level under the egg (lit top
    edge), claw tips hooked up around its base."""
    py = ey + 9                        # palm row, just under the egg
    sx = ox + 13
    put(img, ['dd'], sx, oy + 23)
    for y in range(oy + 24, py + 1):   # upper arm
        put(img, ['drd'], sx, y)
    x0, x1 = sorted((sx + 1, ex + 7))
    for x in range(x0 - 1, x1 + 2):     # forearm
        put(img, ['d', 'l', 'R', 'd'], x, py - 1)
    put(img, ['d', 'l'], ex, py - 2)    # claw tips
    put(img, ['d', 'r'], ex + 7, py - 2)


def ankhra(W=16, H=35, ox=0, oy=0, up=0, egg_up=0, blink=False, feet=(0, 0), egg=None,
           egg_state='whole', baby_blink=False, sparks=()):
    """Compose one frame. oy moves the whole figure down (taller emote cell). up / egg_up raise the
    shell+head / egg+arms. feet = vertical offset of each leg (negative = lifted). egg=(dx, dy): held out."""
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    put(img, LEG_L, ox, oy + 30 + feet[0])
    put(img, LEG_R, ox + 8, oy + 30 + feet[1])
    put(img, BELLY, ox, oy + 27)
    put(img, SHELL, ox, oy + 7 - up)
    head = list(HEAD)
    if blink:
        for r, row in HEAD_BLINK.items():
            head[r] = row
    if egg is None:
        put(img, ARM_L, ox + 1, oy + 25 - egg_up)
        put(img, ARM_R, ox + 9, oy + 25 - egg_up)
        put(img, COLLAR, ox, oy + 24 - min(up, egg_up))
        put(img, head, ox, oy + 12 - up)
        draw_egg(img, ox + 4, oy + 25 - egg_up, egg_state, baby_blink)
        put(img, ARM_L[4:], ox + 1, oy + 29 - egg_up)     # claws in front of the egg
        put(img, ARM_R[4:], ox + 9, oy + 29 - egg_up)
    else:
        dx, dy = egg
        ex, ey = ox + 4 + dx, oy + 25 + dy
        put(img, ARM_L_DOWN, ox + 1, oy + 24)
        put(img, COLLAR, ox, oy + 24 - up)
        put(img, head, ox, oy + 12 - up)
        draw_egg(img, ex, ey, egg_state, baby_blink)
        held_arm(img, ox, oy, ex, ey)
    for (sx, sy, big) in sparks:
        put(img, SPARK if big else SPARK_S, sx, sy)
    return img


# ---------------------------------------------------------------- animations
IDLE_UP = [0, 0, 1, 1, 1, 1, 0, 0]
IDLE_EGG = [0, 0, 0, 1, 1, 1, 1, 0]


def idle_frame(i, **kw):
    return ankhra(up=IDLE_UP[i], egg_up=IDLE_EGG[i], blink=(i == 6), **kw)


def make_idle():
    return [idle_frame(i) for i in range(8)]


def make_move():
    feet = [(-1, 0), (-2, 0), (-1, 0), (0, -1), (0, -2), (0, -1)]
    bob = [0, 1, 0, 0, 1, 0]
    frames = []
    for i in range(6):
        f = ankhra(up=bob[i], egg_up=bob[i], feet=feet[i])
        frames.append(f.crop((0, 1, 16, 35)))      # move cell is 34 tall, same ground row
    return frames


def make_emote():
    W, H, oy = 23, 37, 2
    F = []
    for i in range(5):                                         # 0-4: idle lead-in
        F.append(idle_frame(i, W=W, H=H, oy=oy))
    for dx, dy in [(2, -1), (4, -2), (6, -3), (8, -4)]:        # 5-8: hold the egg out
        F.append(ankhra(W, H, oy=oy, up=1, egg=(dx, dy)))
    out = (9, -4)
    spark_sets = [[(19, 19, 1)], [(12, 21, 0), (21, 20, 0)], [(11, 23, 1)], [(21, 25, 0), (12, 23, 0)]]
    for s in spark_sets:                                       # 9-12: it glows
        F.append(ankhra(W, H, oy=oy, up=1, egg=out, sparks=s))
    F.append(ankhra(W, H, oy=oy, up=1, egg=(8, -4)))           # 13: wobble
    F.append(ankhra(W, H, oy=oy, up=1, egg=(10, -4)))          # 14: wobble
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, egg_state='crack'))            # 15: crack
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, egg_state='open2'))            # 16: cap pops
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open'))             # 17: hatchling peeks
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open', baby_blink=True))  # 18: it blinks
    F.append(ankhra(W, H, oy=oy, up=0, egg=out, egg_state='open'))             # 19
    F.append(ankhra(W, H, oy=oy, up=1, egg=out, sparks=[(19, 18, 1), (12, 24, 0)]))  # 20: cap shut, sparkle
    for dx, dy in [(8, -4), (6, -3), (4, -2), (2, -1)]:         # 21-24: bring it back
        F.append(ankhra(W, H, oy=oy, up=1, egg=(dx, dy)))
    F.append(idle_frame(6, W=W, H=H, oy=oy))                   # 25-26: settle
    F.append(idle_frame(7, W=W, H=H, oy=oy))
    assert len(F) == 27
    return F


def strip(frames, cw, ch):
    img = Image.new('RGBA', (cw * len(frames), ch), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        assert f.size == (cw, ch), (f.size, cw, ch)
        img.alpha_composite(f, (i * cw, 0))
    return img


# ---------------------------------------------------------------- idol: Brood Egg Idol
IDOL_ICON = grid([        # 3/4 top-down: brood egg (lit top) clasped by a sand scarab seen from above, on a slab
    '......qqqF......',
    '.....qeeeEF.....',
    '....qeeeeEfF....',
    '....qEeeEEfF....',
    '....qEEEEEfF....',
    '...dFEEEEEfFd...',
    '..dlhhggggHkRd..',
    '..dlhgeegGHHRd..',
    '..ddkGGGHhHKdd..',
    '.uSSSkKKKKKSStu.',
    '.uSSSSSSSSSSstu.',
    '.usssssssssstTu.',
    '.uBBBnnnnnnnnNu.',
    '.uttttttttttTTu.',
    '..uTTTTTTTTTTu..',
    '...uuuuuuuuuu...',
])
IDOL_SMALL = grid([
    '................',
    '................',
    '......qqqF......',
    '.....qeeEfF.....',
    '.....qEEEfF.....',
    '....dhggGHkd....',
    '....lhgeGHkR....',
    '....uSkKKKSu....',
    '....uSSSSStu....',
    '....uBBnnnNu....',
    '....utttttTu....',
    '.....uuuuuu.....',
    '................',
    '................',
    '................',
    '................',
])


def make_idols():
    icon = Image.new('RGBA', (16, 16), (0, 0, 0, 0)); put(icon, IDOL_ICON, 0, 0)
    small = Image.new('RGBA', (16, 16), (0, 0, 0, 0)); put(small, IDOL_SMALL, 0, 0)
    return icon, small, icon.copy()


# ---------------------------------------------------------------- outputs
RIG = {
    'idle': ('voidMerchant_idle', (128, 35), 16, 8, '580fae3f-66dc-4b7a-900c-ee7986f742e6', [0.5, 0.0], 10.0, 1),
    'move': ('voidMerchant_move', (96, 34), 16, 6, '7128f66e-0c6d-4aed-949e-93fdcd9f5138', [0.5, 0.0], 11.0, 1),
    'emote': ('voidMerchant_emote', (621, 37), 23, 27, 'e1e86c38-70e3-402a-a63f-ecb6d1f3edf0',
              [0.3478260934352875, 0.0], 14.0, 0),
}
SPRITE_ASSET_ADDRESS = [9202072890546798702, -5828687261374344469]


def validate(sheets, idols):
    errs = []
    for key, img in sheets.items():
        tex, size, cw, n, *_ = RIG[key]
        if img.size != size:
            errs.append(f'{key}: size {img.size} != {size}')
        if cw * n != size[0]:
            errs.append(f'{key}: cell layout mismatch')
        alphas = {p[3] for p in img.get_flattened_data()}
        if not alphas <= {0, 255}:
            errs.append(f'{key}: soft alpha {sorted(alphas)[:5]}')
        for i in range(n):
            cell = img.crop((i * cw, 0, i * cw + cw, size[1]))
            bb = cell.getbbox()
            if bb is None:
                errs.append(f'{key} frame {i}: empty')
                continue
            if bb[3] != size[1]:
                errs.append(f'{key} frame {i}: feet not on the ground row (bbox {bb})')
            if key == 'emote':
                body = cell.crop((0, 0, 16, size[1])).getbbox()
                if body is None or body[0] > 1:
                    errs.append(f'emote frame {i}: body not at the pivot column')
    for name, img in idols.items():
        if img.size != (16, 16):
            errs.append(f'idol {name}: size {img.size}')
        if not {p[3] for p in img.get_flattened_data()} <= {0, 255}:
            errs.append(f'idol {name}: soft alpha')
        if img.getbbox() is None:
            errs.append(f'idol {name}: empty')
    icon = idols['icon']
    if icon.getbbox()[3] != 16:
        errs.append('idol icon does not sit on the bottom edge')
    return errs


def load_ref(path):
    return Image.open(path).convert('RGBA') if os.path.exists(path) else None


def ref_frame(merchant, texture, sprite):
    ref = os.path.join(REF, 'reference', 'reference.json')
    img = load_ref(os.path.join(REF, 'reference', 'merchants', merchant, texture + '.png'))
    if img is None or not os.path.exists(ref):
        return None
    meta = json.load(open(ref))['merchants'][merchant]['sheets'][texture]
    for s in meta:
        if s['name'] == sprite:
            x, y, w, h = [int(v) for v in s['rect']]
            return img.crop((x, img.height - y - h, x + w, img.height - y))
    return None


def scale(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def floor_mock(icon, tiles=3, s=6):
    """Idol placed in the middle of a dark stone floor (own procedural tiles)."""
    T = 16
    fl = Image.new('RGBA', (T * tiles, T * tiles), (0, 0, 0, 255))
    stone = [(54, 50, 58), (47, 43, 51), (40, 37, 45), (33, 30, 38), (63, 58, 66)]
    for ty in range(tiles):
        for tx in range(tiles):
            for y in range(T):
                for x in range(T):
                    v = (x * 7 + y * 13 + tx * 5 + ty * 11 + (x * y) % 5) % 9
                    c = stone[0] if v < 4 else stone[1] if v < 7 else stone[2]
                    if x == 0 or y == 0:
                        c = stone[3]
                    elif x == 1 or y == 1:
                        c = stone[4]
                    fl.putpixel((tx * T + x, ty * T + y), c + (255,))
    mid = T * (tiles // 2)
    shadow = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((2, 12, 13, 15), fill=(16, 14, 20, 255))
    fl.alpha_composite(shadow, (mid, mid))
    fl.alpha_composite(icon, (mid, mid - 1))
    return scale(fl, s)


def label(d, xy, text, fill=(233, 217, 162, 255)):
    d.text(xy, text, fill=fill)


def make_preview(idle, move, emote, icon, small):
    BG = (28, 26, 32, 255)
    P = Image.new('RGBA', (1500, 1060), BG)
    d = ImageDraw.Draw(P)
    label(d, (16, 8), 'Boss Merchants  |  eggs: Ankhra the Clutch-Keeper (VoidMerchant rig)  |  idol: Brood Egg Idol (Ra-Akar)')
    # row 1: idle frames at 6x next to vanilla merchants, bottom aligned
    S = 6
    base_y = 20 + 40 * S
    x = 16
    vanilla = [('Caveling', ref_frame('CavelingMerchant', 'Merchant', 'Merchant_0')),
               ('Slime', ref_frame('SlimeMerchant', 'Slime_merchant', 'Slime_merchant_0')),
               ('Fishing', ref_frame('FishingMerchant', 'Fisher_merchant', 'Fisher_merchant_0')),
               ('Void', (load_ref(os.path.join(REF, 'reference', 'merchants', 'VoidMerchant',
                                                'voidMerchant_idle.png')) or Image.new('RGBA', (16, 35))).crop((0, 0, 16, 35)))]
    for name, fr in vanilla + [('Ankhra (new)', idle[0])]:
        if fr is None:
            continue
        bb = fr.getbbox() or (0, 0, fr.width, fr.height)
        fr = fr.crop((0, 0, fr.width, bb[3]))
        im = scale(fr, S)
        P.alpha_composite(im, (x, base_y - im.height))
        label(d, (x, base_y + 4), name)
        x += im.width + 24
    # same merchants at 1x, bottom aligned
    label(d, (x, base_y - 60), '1x')
    for name, fr in vanilla + [('Ankhra (new)', idle[0])]:
        if fr is None:
            continue
        bb = fr.getbbox() or (0, 0, fr.width, fr.height)
        fr = fr.crop((0, 0, fr.width, bb[3]))
        P.alpha_composite(fr, (x, base_y - fr.height))
        x += fr.width + 4
    # row 2: animation strips at 4x
    y = base_y + 30
    S2 = 4
    label(d, (16, y), 'idle (8 @10fps)'); y += 14
    P.alpha_composite(scale(strip(idle, 16, 35), S2), (16, y))
    label(d, (16 + 128 * S2 + 30, y - 14), 'move (6 @11fps)')
    P.alpha_composite(scale(strip(move, 16, 34), S2), (16 + 128 * S2 + 30, y))
    y += 35 * S2 + 10
    label(d, (16, y), 'emote (27 @14fps): hold out the egg, it glows, cracks, a hatchling peeks, back in the claws')
    y += 14
    em = strip(emote, 23, 37)
    P.alpha_composite(scale(em.crop((0, 0, 23 * 14, 37)), 4), (16, y))
    y += 37 * 4 + 6
    P.alpha_composite(scale(em.crop((23 * 14, 0, 621, 37)), 4), (16, y))
    y += 37 * 4 + 16
    # row 3: idols 1x and 8x beside vanilla
    label(d, (16, y), 'PERSPECTIVE SELF-CHECK  idols at 1x and 8x (vanilla: Mysterious Idol, Slime Oil, Pile of Chum, Void Merchant Idol; ours last, with smallIcon)')
    y += 16
    idref = os.path.join(REF, 'idol_reference')
    ids = [load_ref(os.path.join(idref, f)) for f in ('3930_MysteriousIdol_icon.png', '3931_SlimeOil_icon.png',
                                                     '3932_PileOfChum_icon.png', '3937_VoidMerchantIdol_icon.png')]
    ids = [i for i in ids if i is not None] + [icon, small]
    x = 16
    for im in ids:
        P.alpha_composite(im, (x, y))
        x += 22
    x += 10
    for im in ids:
        P.alpha_composite(scale(im, 8), (x, y))
        x += 16 * 8 + 8
    # row 4: placed on a floor mock
    fm = floor_mock(icon)
    P.alpha_composite(fm, (P.width - fm.width - 16, 760))
    label(d, (P.width - fm.width - 16, 760 + fm.height + 4), 'placed on a stone floor (mock)')
    return P


def main():
    idle, move, emote = make_idle(), make_move(), make_emote()
    sheets = {'idle': strip(idle, 16, 35), 'move': strip(move, 16, 34), 'emote': strip(emote, 23, 37)}
    icon, small, placed = make_idols()
    idols = {'icon': icon, 'smallIcon': small, 'placed': placed}
    errs = validate(sheets, idols)
    if errs:
        print('VALIDATION FAILED'); [print(' -', e) for e in errs]
        sys.exit(1)
    os.makedirs(OUT_M, exist_ok=True); os.makedirs(OUT_I, exist_ok=True); os.makedirs(OUT_P, exist_ok=True)
    textures = []
    for key, img in sheets.items():
        tex, size, cw, n, guid, pivot, fps, loop = RIG[key]
        fn = f'eggs__{tex}.png'
        img.save(os.path.join(OUT_M, fn))
        textures.append({'output': f'eggs/{fn}', 'replacesTexture': tex, 'size': list(size),
                         'spriteAsset': 'voidMerchant', 'spriteAssetAddress': SPRITE_ASSET_ADDRESS,
                         'animationGuid': guid, 'slot': 'm_animations[].m_spriteData.texture',
                         'pivot': pivot, 'srcFrameCount': n, 'fps': fps, 'loop': loop})
    json.dump({'key': 'eggs', 'rig': 'VoidMerchantEntity', 'kind': 'spriteAsset', 'textures': textures},
              open(os.path.join(OUT_M, 'sheets.json'), 'w'), indent=1)
    for name, img in idols.items():
        img.save(os.path.join(OUT_I, f'eggs_{name}.png'))
    make_preview(idle, move, emote, icon, small).save(os.path.join(OUT_P, 'eggs.png'))
    print('ok: wrote', OUT_M, OUT_I, os.path.join(OUT_P, 'eggs.png'))


if __name__ == '__main__':
    main()
