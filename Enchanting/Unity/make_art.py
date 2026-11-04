"""Enchanting Table icons: the vanilla Upgrade Forge icon (Art/forge_*.png from dump_vanilla.py) with its red
glow recolored violet and a few sparkle pixels.

    python -I Enchanting/Unity/make_art.py
"""
import colorsys, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, 'Art')
SPARKLE = (255, 236, 255, 255)
SPARKLES = [(3, 0), (12, 1), (8, 3)]  # (x, y) on the 16x16 icon, only placed on transparent pixels


def recolor(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            # Reds/pinks (the forge's hot glow) -> violet; leave stone, wood and metal alone.
            if s > 0.25 and (h < 0.06 or h > 0.9):
                h = 0.78
                s = min(1.0, s * 1.05)
                v = min(1.0, v * 1.08)
                r2, g2, b2 = colorsys.hsv_to_rgb(h, s, v)
                px[x, y] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), a)
    return im


def main():
    for src, dst, sparkle in (('forge_icon', 'EnchantingTable_icon', True), ('forge_smallIcon', 'EnchantingTable_smallIcon', False)):
        im = recolor(Image.open(os.path.join(ART, src + '.png')).convert('RGBA'))
        if sparkle:
            px = im.load()
            for x, y in SPARKLES:
                if x < im.width and y < im.height and px[x, y][3] == 0:
                    px[x, y] = SPARKLE
        im.save(os.path.join(ART, dst + '.png'))
        print('wrote', dst)


if __name__ == '__main__':
    main()
