"""Runtime art for placed objects: ancient-gem storage units and workbench, clockwork terminal.

    python ConnectedStorage/Unity/make_gem_art.py

Native resolution (one texel per game pixel, vanilla sizes): units 16x16 front/back and 16x19 side,
terminal and workbench 16x16 front/back and 16x20 side. The palette-keyed grids live in
ArtConcepts/GemSet (ancient_views.py, terminal_views.py; chosen from ArtConcepts/StyleExploration).
Storage tiers recolour only the gem keys '1'-'6' (darkest to brightest). Original pixels; vanilla
sprites were references only. Previous sets: ArtConcepts/RunicKilnNative, ArtConcepts/Previous96px.
"""
import runpy
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).parent
ART = ROOT / 'Art'
SETS = ROOT.parent / 'ArtConcepts' / 'GemSet'

# Gem ramps, keys '1'..'6' from darkest to brightest. Copper is the approved base.
TIERS = {
    'Copper':    ['5e2a28', '824223', 'c16436', 'e88b69', 'fca895', 'ffe4d6'],
    'Tin':       ['3a4658', '5a6c84', '8aa0bc', 'bcd0e6', 'e2eef8', 'ffffff'],
    'Iron':      ['4a3236', '6e4c52', '9a7078', 'c49ca4', 'e4c8cc', 'fff0f2'],
    'Scarlet':   ['4a0812', '7a1020', 'b81828', 'f03848', 'ff8090', 'ffd4d8'],
    'Octarine':  ['0a3a2e', '106050', '18a07a', '3ee0a8', '9cffd8', 'e4fff4'],
    'Galaxite':  ['2a0e50', '46207e', '7a3ad0', 'b070ff', 'dcb4ff', 'f6ecff'],
    'Solarite':  ['5a3a04', '8a5c06', 'd09810', 'ffd030', 'ffec90', 'fffbe0'],
    'Pandorium': ['0a1450', '14287a', '2050d0', '4a8aff', 'a8ccff', 'e8f2ff'],
}
HEIGHTS = {'unit': {'': 16, '_side': 19, '_up': 16}, 'bench': {'': 16, '_side': 20, '_up': 16}}


def render(grid, pal, glow_keys, height):
    assert len(grid) == height, (len(grid), height)
    im, glow = Image.new('RGBA', (16, height)), Image.new('RGBA', (16, height))
    for y, row in enumerate(grid):
        assert len(row) == 16, (y, row)
        for x, key in enumerate(row):
            value = pal[key]
            if not value:
                continue
            color = tuple(bytes.fromhex(value)) + (255,)
            im.putpixel((x, y), color)
            if key in glow_keys:
                glow.putpixel((x, y), color)
    return im, glow


def objects():
    ancient = runpy.run_path(str(SETS / 'ancient_views.py'))
    terminal = runpy.run_path(str(SETS / 'terminal_views.py'))
    for tier, ramp in TIERS.items():
        pal = {**ancient['PAL'], **{str(i + 1): c for i, c in enumerate(ramp)}}
        for suffix, grid in ancient['CHEST'].items():
            yield tier + 'Chest', suffix, render(grid, pal, ancient['GLOW_CHEST'], HEIGHTS['unit'][suffix])
    for suffix, grid in ancient['WORKSTATION'].items():
        yield 'Workbench', suffix, render(grid, ancient['PAL'], ancient['GLOW_WORKSTATION'], HEIGHTS['bench'][suffix])
    for suffix, grid in terminal['TERMINAL'].items():
        yield 'Terminal', suffix, render(grid, terminal['PAL'], terminal['GLOW_TERMINAL'], HEIGHTS['bench'][suffix])


def main():
    count = 0
    for name, suffix, (im, glow) in objects():
        im.save(ART / (name + suffix + '.png'))
        glow.save(ART / (name + suffix + '_emissive.png'))
        count += 1
    print(f'{count} views written')


if __name__ == '__main__':
    main()
