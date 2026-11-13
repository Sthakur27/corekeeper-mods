"""Apply the approved Workbench B, upgrade gems and Octarine palette correction."""
from pathlib import Path
from PIL import Image

ART = Path(__file__).resolve().parent / 'Art'
CONCEPTS = ART.parents[1] / 'ArtConcepts'
TIERS = ('Tin', 'Iron', 'Scarlet', 'Octarine', 'Galaxite', 'Solarite', 'Pandorium', 'Relucite')
VIEWS = ('', '_side', '_up')


def write(name, views, emissive=None):
    for suffix, image in zip(VIEWS, views, strict=True):
        image.save(ART / f'{name}{suffix}.png')
        glow = emissive[suffix] if emissive else Image.new('RGBA', image.size)
        glow.save(ART / f'{name}{suffix}_emissive.png')


def main():
    bench = Image.open(CONCEPTS / 'WorkbenchRefresh-2026-11-12/iteration-2/Workbench-B-16.png').convert('RGBA')
    # Side: a longer work surface with the assembly plate turned a quarter turn.
    side = Image.new('RGBA', (16, 20))
    side.paste(bench.crop((0, 0, 16, 6)), (0, 0))
    side.paste(bench.crop((0, 6, 16, 16)), (0, 10))
    for y in range(6, 10):
        side.paste(bench.crop((0, 5, 16, 6)), (0, y))
    stone = bench.getpixel((11, 4))
    side.paste(stone, (4, 3, 11, 11))
    plate = bench.crop((5, 4, 11, 8)).transpose(Image.Transpose.ROTATE_90)
    side.paste(plate, (6, 4))
    # Rear: retain the tabletop and use a plain timber crossbar behind it.
    rear = bench.copy()
    rear.paste(bench.getpixel((4, 14)), (4, 11, 12, 14))
    write('Workbench', (bench, side, rear))
    for tier in TIERS:
        icon = Image.open(CONCEPTS / 'TierRefresh-2026-11-12/iteration-2' / f'{tier}Gem-16.png').convert('RGBA')
        write(tier + 'Gem', (icon, icon, icon))

    # Change only green accent RGB on the original unit and its glow masks.
    # Slate body colors, alpha, positions and dimensions are retained verbatim.
    palette = dict(zip(
        ('073c31','0a3a2e','106050','18a07a','1a7f86','3ee0a8','9cffd8','e4fff4'),
        ('282851','142b5c','4e3c6d','954d94','665294','c76369','e88b69','ffe4d6'),
    ))
    palette = {tuple(bytes.fromhex(a)): tuple(bytes.fromhex(b)) for a, b in palette.items()}
    for view in VIEWS:
        for emission in ('', '_emissive'):
            path = ART / f'OctarineChest{view}{emission}.png'
            image = Image.open(path).convert('RGBA')
            image.putdata([(*palette.get(p[:3], p[:3]), p[3]) if p[3] else p for p in image.getdata()])
            image.save(path)


if __name__ == '__main__':
    main()
