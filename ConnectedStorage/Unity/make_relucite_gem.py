"""Relucite upgrade gem: the Galaxite crystal mirrored and shifted from violet to magenta.

    python ConnectedStorage/Unity/make_relucite_gem.py

Writes ArtConcepts/UpgradeGems/Relucite.png (approved-size source, checked by verify_bundle.py) and the
runtime Art/ReluciteGem*.png views. Only saturated violet crystal pixels change hue; the grey metal
setting and brass rivet stay as drawn, so the gem matches the rest of the set.
"""
import colorsys
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path(__file__).parent
GEMS = ROOT.parent / 'ArtConcepts' / 'UpgradeGems'


def relucite(image):
    image = ImageOps.mirror(image.convert('RGBA'))
    px = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if s > 0.25 and 0.6 < h < 0.85:
                r, g, b = (round(c * 255) for c in colorsys.hsv_to_rgb((h + 0.14) % 1, min(1, s * 1.05), v))
            px[x, y] = (r, g, b, a)
    return image


def main():
    relucite(Image.open(GEMS / 'Galaxite.png')).save(GEMS / 'Relucite.png')
    for suffix in ('', '_side', '_up', '_emissive', '_side_emissive', '_up_emissive'):
        relucite(Image.open(ROOT / 'Art' / ('GalaxiteGem' + suffix + '.png'))).save(ROOT / 'Art' / ('ReluciteGem' + suffix + '.png'))
    print('Relucite gem written')


if __name__ == '__main__':
    main()
