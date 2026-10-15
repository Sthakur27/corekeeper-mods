"""Regenerate original code-authored runtime sprites and their review sheets."""
from pathlib import Path
import runpy
from PIL import Image, ImageDraw
HERE = Path(__file__).parent
OUT = HERE / 'Art'
TIERS = ('Copper', 'Tin', 'Iron', 'Scarlet', 'Octarine', 'Galaxite', 'Solarite', 'Pandorium')

def previews():
    sheet = Image.new('RGBA', (960, 480), (24, 29, 36, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 14), 'CONNECTED STORAGE / ORIGINAL RUNTIME SPRITES', fill='white')
    draw.text((20, 36), '5x nearest-neighbour preview; actual assets are 16 pixels wide', fill=(170, 186, 195))
    for i, tier in enumerate(TIERS):
        x = i * 120
        art = Image.open(OUT / (tier + 'Chest.png')).convert('RGBA')
        sheet.alpha_composite(art.resize((80, 80), Image.Resampling.NEAREST), (x + 20, 75))
        draw.text((x + 16, 168), tier, fill='white')
        if i:
            gem = Image.open(OUT / (tier + 'Gem.png')).convert('RGBA')
            sheet.alpha_composite(gem.resize((80, 80), Image.Resampling.NEAREST), (x + 20, 215))
    draw.text((20, 245), 'UPGRADE GEMS', fill=(170, 186, 195))
    for i, name in enumerate(('Terminal', 'Workbench')):
        for j, suffix in enumerate(('', '_side', '_up')):
            art = Image.open(OUT / (name + suffix + '.png')).convert('RGBA')
            x = 40 + i * 450 + j * 120
            sheet.alpha_composite(art.resize((art.width * 5, art.height * 5), Image.Resampling.NEAREST), (x, 335))
        draw.text((40 + i * 450, 453), name + ' / front, side, back', fill='white')
    sheet.save(OUT / 'preview.png')
    names = [n + 'Chest' for n in TIERS] + ['Terminal', 'Workbench']
    directions = Image.new('RGBA', (1000, 600), (24, 29, 36, 255))
    d = ImageDraw.Draw(directions)
    for i, name in enumerate(names):
        x, y = (i % 4) * 250, (i // 4) * 200
        d.text((x + 12, y + 12), name.replace('Chest', ' unit'), fill='white')
        for j, suffix in enumerate(('', '_side', '_up')):
            art = Image.open(OUT / (name + suffix + '.png')).convert('RGBA')
            directions.alpha_composite(art.resize((art.width * 4, art.height * 4), Image.Resampling.NEAREST), (x + 10 + j * 80, y + 40))
            glow = Image.open(OUT / (name + suffix + '_emissive.png')).convert('RGBA')
            directions.alpha_composite(glow.resize((glow.width * 3, glow.height * 3), Image.Resampling.NEAREST), (x + 18 + j * 80, y + 135))
    directions.save(OUT / 'directions.png')

if __name__ == '__main__':
    runpy.run_path(str(HERE / 'make_unit_art.py'), run_name='__main__')
    runpy.run_path(str(HERE / 'make_device_art.py'), run_name='__main__')
    runpy.run_path(str(HERE / 'make_archive_art.py'), run_name='__main__')
    previews()
