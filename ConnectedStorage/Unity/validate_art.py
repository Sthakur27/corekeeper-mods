"""Validate runtime sprite transparency, dimensions and emissive alignment."""
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).parent / 'Art'
TIERS = ('Copper', 'Tin', 'Iron', 'Scarlet', 'Octarine', 'Galaxite', 'Solarite', 'Pandorium')
NAMES = ['Terminal', 'Workbench'] + [t + 'Chest' for t in TIERS] + [t + 'Gem' for t in TIERS[1:]]
count = 0
for name in NAMES:
    views = []
    for suffix in ('', '_side', '_up'):
        path = ROOT / (name + suffix + '.png')
        image = Image.open(path)
        glow = Image.open(ROOT / (name + suffix + '_emissive.png'))
        expected = (16, 20 if name == 'Terminal' else 16)
        assert image.mode == glow.mode == 'RGBA', str(path) + ': RGBA required'
        assert image.size == glow.size == expected, str(path) + ': unexpected size'
        alpha = image.getchannel('A')
        assert set(alpha.getdata()) <= {0, 255}, str(path) + ': soft alpha breaks pixel edges'
        assert alpha.getbbox() and alpha.getextrema() == (0, 255), str(path) + ': opaque rectangle or empty sprite'
        for pixel, emission in zip(image.getdata(), glow.getdata()):
            assert not emission[3] or pixel[3], str(path) + ': glow outside sprite silhouette'
        views.append(image.tobytes())
        count += 2
    if not name.endswith('Gem'):
        assert len(set(views)) == 3, name + ': repeated directional art'
print(f'PASS: {count} RGBA sprites; pixel edges, dimensions, glow masks and directional views')
