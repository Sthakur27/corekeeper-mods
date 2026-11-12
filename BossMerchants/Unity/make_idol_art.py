"""Draw the five Boss Merchants idols into Unity/Art/idols (original pixel art, hand-authored grids).

    python -I BossMerchants/Unity/make_idol_art.py

Per idol <theme> in (slippery_slime, omoroth, fire_slime, ivy, ra_akar):
  <theme>_icon.png       16x16 inventory icon, object fills the canvas bottom-anchored (like Mysterious Idol)
  <theme>_smallIcon.png  16x16 canvas, ~10x11 px version of the same object, centred (like the vanilla smallIcons)
  <theme>_placed.png     16x16 sprite for the placed object's SR renderer. Vanilla idols (Mysterious Idol 3930,
                         Slime Oil 3931, Pile of Chum 3932, Void Merchant Idol 3937) reuse their icon sprite for
                         SR and for the black-tinted Shadow renderer, so this is the icon pixels again.
All sprites: 16 px per unit, pivot at the centre (0.5, 0.5); in the vanilla graphic prefab SRPivot sits at
(0, 0.0625, -0.5) and SR at y 0.5 (0.4375 for Slime Oil / Void idol), so the sprite's bottom row touches the tile.

Style: no black outlines. Every 'o' in a grid becomes the outline shade of the material it borders most
(tint_outline), colours come from the game's shared item palette, light from the top-left, hard alpha only.
preview.png is a labelled 8x contact sheet: vanilla reference idol, new icon, smallIcon and placed sprite.
The vanilla reference images are read from build/bossmerchants/idol_reference (extract_idol_reference.py) for
the preview only; none of their pixels go into the idols.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
OUT = HERE / 'Art' / 'idols'
REF = ROOT / 'build' / 'bossmerchants' / 'idol_reference'
SIZE = 16

# Shared game palette pieces (all values occur in vanilla item icons).
STONE = {  # steel-blue stone, as on Slime Oil's jar
    'W': (215, 220, 251), 'S': (161, 174, 221), 's': (103, 127, 174), 'd': (71, 103, 126),
}
STONE_OUT = (42, 78, 74)
SAND = {  # sandstone, as on the temple urns
    'A': (233, 217, 162), 'a': (201, 171, 81), 'q': (181, 128, 71), 'Q': (145, 91, 38),
}
SAND_OUT = (97, 72, 34)


def material(colours, outline):
    return {ch: (rgb, outline) for ch, rgb in colours.items()}


IDOLS = {}

# ---------------------------------------------------------------- 1. Slippery Slime (Morpha) -> blocks
# A blue slime statuette (eyes, glossy top-left highlight) slumped on a stone plinth, a drip running down it.
IDOLS['slippery_slime'] = dict(
    boss='Morpha (SlipperySlimeBoss 3024)', ref='3930_MysteriousIdol_icon.png', ref_name='Mysterious Idol 3930',
    palette={
        **material({'w': (255, 255, 255), 'c': (25, 189, 198), 'B': (24, 133, 216), 'b': (22, 93, 217),
                    'n': (20, 62, 171), 'k': (20, 43, 92)}, (20, 43, 92)),
        **material(STONE, STONE_OUT),
    },
    icon=[
        '................',
        '......oooo......',
        '....oocwBBoo....',
        '...ocwcBBBBbo...',
        '..ocwcBBBBBBbo..',
        '..ocBBBBBBBBbo..',
        '.ocBBkkBBkkbbno.',
        '.oBBBkwBBkwbbno.',
        '.oBBBBBBBBbbbno.',
        '.obBBBBBBbbbnno.',
        '.onbbbbbbbbnnno.',
        '..onbnnnnnnnno..',
        '..oSbWSSSSSSdo..',
        '..oSbSSSSSSsdo..',
        '.oWSnSSSSSSssdo.',
        '.oooooooooooooo.',
    ],
    small=[
        '................',
        '................',
        '................',
        '......oooo......',
        '.....owcBBo.....',
        '....ocBBBBbo....',
        '....oBkBBkbo....',
        '....oBBBBbno....',
        '....obbbbnno....',
        '....onbnnnno....',
        '....oSbSSSdo....',
        '...oWSnSSSsdo...',
        '...oooooooooo...',
        '................',
        '................',
        '................',
    ],
)

# ---------------------------------------------------------------- 2. Omoroth -> decor
# A carved octopus totem: dark sea-green mantle with purple spots, pale eyes, hooked beak, curling tentacles.
IDOLS['omoroth'] = dict(
    boss='Omoroth (OctopusBoss 3110)', ref='3937_VoidMerchantIdol_icon.png', ref_name='Void Merchant Idol 3937',
    palette={
        **material({'G': (35, 144, 41), 'g': (10, 109, 39), 'f': (15, 86, 48), 'F': (7, 60, 49)}, (14, 37, 34)),
        **material({'p': (165, 85, 164), 'P': (114, 62, 138), 'u': (78, 60, 109)}, (56, 52, 71)),
        **material({'E': (165, 202, 218), 'e': (89, 157, 187), 'k': (11, 25, 23)}, (14, 37, 34)),
    },
    icon=[
        '................',
        '.....oooooo.....',
        '....oGGggggo....',
        '...oGGgpggffo...',
        '...oGgggggPfo...',
        '..oGgpgggggffo..',
        '..ogEEeggEEefo..',
        '..ogEkeggEkefo..',
        '..oggffPPfffFo..',
        '...ofgoppoffo...',
        '..oGgfoPuofgFo..',
        '.oGgfffoofffgFo.',
        'oGgoGgffffgFofFo',
        'ogpogfoffofgopFo',
        'opo.opo..opo.opo',
        '.o...o....o...o.',
    ],
    small=[
        '................',
        '................',
        '................',
        '......oooo......',
        '.....oGggfo.....',
        '....oGgpgffo....',
        '....oEkgEkfo....',
        '....oggppfFo....',
        '.....ofpufo.....',
        '....oGgoofFo....',
        '...oGofggfoFo...',
        '...opoofFoopo...',
        '....o..oo..o....',
        '................',
        '................',
        '................',
    ],
)

# ---------------------------------------------------------------- 3. Fire Slime (Igneous) -> explosives
# A molten slime bubbling out of a basalt brazier, with a fuse-like flame flicking off its crown.
IDOLS['fire_slime'] = dict(
    boss='Igneous (LavaSlimeBoss 3028)', ref='3931_SlimeOil_icon.png', ref_name='Slime Oil 3931',
    palette={
        **material({'h': (247, 238, 199), 'Y': (242, 223, 58), 'y': (226, 161, 44), 'r': (218, 98, 16),
                    'R': (197, 70, 10), 'z': (180, 50, 33), 'Z': (145, 33, 11)}, (94, 42, 40)),
        **material({'k': (56, 52, 71)}, (94, 42, 40)),
        **material({'X': (143, 123, 119), 'x': (100, 93, 83), 'm': (72, 68, 84), 'M': (56, 52, 71)}, (35, 41, 48)),
    },
    icon=[
        '.......oo.......',
        '......oYo.......',
        '......oyho......',
        '.....oorYoo.....',
        '...oorYhhyroo...',
        '..oryYhyyyyrRo..',
        '..oyYkkyykkRRo..',
        '.oryykhyykhrRZo.',
        '.orryyyyyrrRZZo.',
        'oXXXXXXXXXXXxxmo',
        'oxXrRzRZzRzZxmMo',
        '.oxXXXXXXXXxxmo.',
        '..oxxxxxxxxxmo..',
        '...omxxxxxmMo...',
        '...oXxxxxxmMo...',
        '...oooooooooo...',
    ],
    small=[
        '................',
        '................',
        '.......oo.......',
        '.......oYo......',
        '.....oorhoo.....',
        '....orYhyyRo....',
        '....oykyykRo....',
        '....oryyyrZo....',
        '...oXXXXXXxmo...',
        '...oxrzRzZxMo...',
        '....oxxxxxmo....',
        '.....oxxxmo.....',
        '.....oXxmMo.....',
        '.....oooooo.....',
        '................',
        '................',
    ],
)

# ---------------------------------------------------------------- 4. Ivy -> potions
# Ivy's poison bloom (pink petals, purple heart, green poison bead) growing out of a stoppered poison flask.
IDOLS['ivy'] = dict(
    boss='Ivy (PoisonSlimeBoss 3023)', ref='3931_SlimeOil_icon.png', ref_name='Slime Oil 3931',
    palette={
        **material({'L': (222, 142, 154), 'l': (199, 99, 105)}, (97, 38, 36)),
        **material({'v': (149, 77, 148), 'V': (114, 62, 138), 'u': (78, 60, 109)}, (78, 60, 109)),
        **material({'G': (163, 206, 74), 'g': (92, 165, 55), 'f': (35, 144, 41), 'F': (10, 109, 39)}, (15, 86, 48)),
        **material({'W': (215, 220, 251), 'S': (161, 174, 221)}, STONE_OUT),
    },
    icon=[
        '.....oo..oo.....',
        '....oLLooLlo....',
        '.oo.oLLllllo.oo.',
        'oLLooLlvvlloolLo',
        'oLLLlovVVvollllo',
        '.oLLlvVGgVvlllo.',
        '..ollvVgfVvllo..',
        '.oLLlovVVvolllo.',
        'oLllooovvoooollo',
        '.ooofGoFFogfooo.',
        '....ooWSSdoo....',
        '...oWvvvvvVuo...',
        '..oWvLvvvvVVuo..',
        '..oSvvvvvVVVuo..',
        '..oSVvvvVVVuuo..',
        '...oooooooooo...',
    ],
    small=[
        '................',
        '................',
        '.....oo..oo.....',
        '....oLLooLlo....',
        '...oLLlvvllo....',
        '....olvGVlo.....',
        '...oLlovvolo....',
        '....ooofFoo.....',
        '......oWdo......',
        '.....oWvvVo.....',
        '....oWLvvVuo....',
        '....oSvvVVuo....',
        '.....oooooo.....',
        '................',
        '................',
        '................',
    ],
)
IDOLS['ivy']['palette']['d'] = (STONE['d'], STONE_OUT)

# ---------------------------------------------------------------- 5. Ra-Akar -> eggs
# A gold-and-jade scarab carving on a sandstone plinth, holding a pale egg up between its front legs.
IDOLS['ra_akar'] = dict(
    boss='Ra-Akar (ScarabBoss 3131)', ref='3930_MysteriousIdol_icon.png', ref_name='Mysterious Idol 3930',
    palette={
        **material({'Y': (242, 223, 58), 'y': (226, 161, 44), 'Q': (145, 91, 38)}, (57, 52, 31)),
        **material({'G': (163, 206, 74), 'g': (92, 165, 55), 'f': (35, 144, 41), 'F': (10, 109, 39)}, (7, 60, 49)),
        **material({'z': (180, 50, 33)}, (57, 52, 31)),
        **material({'h': (255, 255, 255), 'e': (247, 238, 199), 'E': (210, 154, 124)}, (130, 66, 35)),
        **material({'T': (233, 217, 162), 't': (201, 171, 81), 'q': (181, 128, 71), 'U': (145, 91, 38)}, SAND_OUT),
    },
    icon=[
        '......oooo......',
        '.....oheeEo.....',
        '..oo.oeeeEo.oo..',
        '.oYyo.oEEo.oyQo.',
        '..oyYooyyooyQo..',
        '....oyYzzyQo....',
        '...oyYyyyyyQo...',
        '...oGGgoggfo....',
        '.ooGGggogfffoo..',
        'oyoGgggogfffoQo.',
        '.ooGgggogfffoo..',
        'oyoGgggogffFoQo.',
        '...ooGgoffoo....',
        '..oTTooooooTtUo.',
        '.oTTttttttttUUo.',
        '.ooooooooooooooo',
    ],
    small=[
        '................',
        '................',
        '................',
        '.......oo.......',
        '......oheo......',
        '....oyoEEoQo....',
        '.....oyzzQo.....',
        '....oGgogffo....',
        '..oyoGgogffoQo..',
        '....oGgogffo....',
        '..oyoGgogfFoQo..',
        '....ooGoffoo....',
        '...oTTTTTTtUo...',
        '...oTttttttUo...',
        '...oooooooooo...',
        '................',
    ],
)


def tint_outline(grid, palette):
    """Each 'o' becomes the outline colour of the material it borders most (4-neighbours, then diagonals)."""
    h, w = len(grid), len(grid[0])
    out = [[None] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            ch = grid[y][x]
            if ch == '.':
                continue
            if ch != 'o':
                out[y][x] = palette[ch][0]
                continue
            for ring in (((1, 0), (-1, 0), (0, 1), (0, -1)), ((1, 1), (-1, 1), (1, -1), (-1, -1))):
                votes = {}
                for dx, dy in ring:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] in palette:
                        k = palette[grid[ny][nx]][1]
                        votes[k] = votes.get(k, 0) + 1
                if votes:
                    out[y][x] = max(votes, key=lambda k: (votes[k], -sum(k)))
                    break
    # Outline pixels touching only other outline pixels (corners) copy a resolved outline neighbour.
    pending = [(x, y) for y in range(h) for x in range(w) if grid[y][x] == 'o' and out[y][x] is None]
    while pending:
        left = []
        for x, y in pending:
            near = [out[y + dy][x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and grid[y + dy][x + dx] == 'o' and out[y + dy][x + dx]]
            if near:
                out[y][x] = near[0]
            else:
                left.append((x, y))
        if len(left) == len(pending):
            raise ValueError(f'isolated outline pixels at {left}')
        pending = left
    return out


def draw(rows, palette):
    assert len(rows) == SIZE and all(len(r) == SIZE for r in rows), [len(r) for r in rows]
    for r in rows:
        for ch in r:
            assert ch in '.o' or ch in palette, f'unknown colour {ch!r}'
    img = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    for y, row in enumerate(tint_outline(rows, palette)):
        for x, rgb in enumerate(row):
            if rgb is not None:
                img.putpixel((x, y), rgb + (255,))
    return img


def render():
    out = {}
    for theme, spec in IDOLS.items():
        icon = draw(spec['icon'], spec['palette'])
        out[theme] = {'icon': icon, 'smallIcon': draw(spec['small'], spec['palette']), 'placed': icon.copy()}
    return out


def validate(images):
    """Sizes match the vanilla idols (16x16 icon / smallIcon / placed), hard alpha only, sensible framing."""
    vanilla = {}
    for name in ('3930_MysteriousIdol', '3931_SlimeOil', '3932_PileOfChum', '3937_VoidMerchantIdol'):
        for kind in ('icon', 'smallIcon'):
            p = REF / f'{name}_{kind}.png'
            if p.exists():
                vanilla.setdefault(kind, set()).add(Image.open(p).size)
    for kind in ('icon', 'smallIcon'):
        expect = vanilla.get(kind, {(SIZE, SIZE)})
        assert expect == {(SIZE, SIZE)}, f'vanilla {kind} sizes changed: {expect}'
    for theme, imgs in images.items():
        for kind, img in imgs.items():
            assert img.size == (SIZE, SIZE), (theme, kind, img.size)
            alphas = {a for *_, a in img.get_flattened_data()}
            assert alphas <= {0, 255}, (theme, kind, alphas)
            assert all(px[:3] != (0, 0, 0) for px in img.get_flattened_data() if px[3]), (theme, kind, 'pure black')
            x0, y0, x1, y1 = img.getbbox()
            if kind in ('icon', 'placed'):
                assert y1 == SIZE and x1 - x0 >= 12, (theme, kind, 'icon must fill the tile, bottom-anchored')
            else:
                assert 8 <= x1 - x0 <= 12 and 8 <= y1 - y0 <= 12, (theme, kind, 'smallIcon ~10x11', (x0, y0, x1, y1))
        assert list(imgs['placed'].get_flattened_data()) == list(imgs['icon'].get_flattened_data())
    print('validation ok:', len(images), 'idols, 16x16, hard alpha, no black')


def preview(images, path, scale=8):
    labels = ['vanilla reference', 'icon', 'smallIcon', 'placed (in world)']
    cell = SIZE * scale
    pad, label_h, left = 12, 18, 290
    rows = list(images)
    w = left + len(labels) * (cell + pad) + pad
    h = label_h + pad + len(rows) * (cell + pad + label_h)
    sheet = Image.new('RGBA', (w, h), (38, 34, 44, 255))
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype('consola.ttf', 14)
    except OSError:
        font = ImageFont.load_default()
    for i, lab in enumerate(labels):
        d.text((left + pad + i * (cell + pad), 4), lab, fill=(235, 225, 200, 255), font=font)
    floor = Image.new('RGBA', (cell, cell), (70, 58, 50, 255))
    fd = ImageDraw.Draw(floor)
    for t in range(0, cell, scale * 4):  # faint tile grid behind the in-world sprite
        fd.line([(t, 0), (t, cell)], fill=(78, 66, 57, 255))
        fd.line([(0, t), (cell, t)], fill=(78, 66, 57, 255))
    for r, theme in enumerate(rows):
        spec = IDOLS[theme]
        y = label_h + pad + r * (cell + pad + label_h)
        d.text((pad, y + 4), theme, fill=(242, 223, 58, 255), font=font)
        d.text((pad, y + 24), spec['boss'], fill=(200, 200, 210, 255), font=font)
        d.text((pad, y + 44), 'modelled on:', fill=(160, 160, 170, 255), font=font)
        d.text((pad, y + 62), spec['ref_name'], fill=(200, 200, 210, 255), font=font)
        tiles = []
        refp = REF / spec['ref']
        tiles.append(Image.open(refp).convert('RGBA') if refp.exists() else None)
        tiles += [images[theme]['icon'], images[theme]['smallIcon'], images[theme]['placed']]
        for i, img in enumerate(tiles):
            x = left + pad + i * (cell + pad)
            box = Image.new('RGBA', (cell, cell), (52, 47, 60, 255)) if i < 3 else floor.copy()
            if img is not None:
                big = img.resize((cell, cell), Image.NEAREST)
                if i == 3:  # vanilla Shadow renderer: same sprite, black at 70 % alpha, 1 px lower
                    sh = Image.new('RGBA', big.size, (0, 0, 0, 0))
                    mask = big.getchannel('A').point(lambda a: 179 if a else 0)
                    sh.paste((0, 0, 0, 255), (0, scale), mask.crop((0, 0, cell, cell - scale)))
                    box.alpha_composite(sh)
                box.alpha_composite(big)
            sheet.alpha_composite(box, (x, y))
    sheet.save(path)


def main():
    images = render()
    validate(images)
    OUT.mkdir(parents=True, exist_ok=True)
    for theme, imgs in images.items():
        for kind, img in imgs.items():
            img.save(OUT / f'{theme}_{kind}.png')
    preview(images, OUT / 'preview.png')
    print('wrote', OUT)


if __name__ == '__main__':
    main()
