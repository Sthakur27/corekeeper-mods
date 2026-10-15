"""Original, deterministic 16-pixel device artwork for Connected Storage.

Native workbenches were studied for top-facing surfaces, colored edge shadows,
two-pixel legs, clustered highlights and material separation. No native pixels
are embedded here. Each orientation is drawn separately, not merely mirrored.
"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).parent / 'Art'
P = dict(ink='#343c3d', shadow='#4d4535', wood='#926137', grain='#b57d43',
         edge='#e1ad68', light='#ffe0a0', copper='#bb704b', rust='#764a3f',
         metal='#6a8890', steel='#acc1c3', pale='#dce3d5', dark='#264e55',
         teal='#328c91', aqua='#57c4bb', glow='#abf0d5', core='#e2ffe8')


def device(name, direction):
    im = Image.new('RGBA', (16, 20 if name == 'Terminal' else 16))
    d = ImageDraw.Draw(im)
    em = Image.new('RGBA', im.size)
    e = ImageDraw.Draw(em)
    def box(rect, color): d.rectangle(rect, fill=P[color])
    def line(points, color): d.line(points, fill=P[color])
    def dot(x, y, color): d.point((x,y), fill=P[color])
    def poly(points, color): d.polygon(points, fill=P[color])
    def lit(points, color='glow'):
        d.line(points, fill=P[color]); e.line(points, fill=(42,102,78,255))

    if name == 'Terminal':
        # A full cabinet console seen from above: deep sloped work surface,
        # brass control rail, shaded front apron and short feet. No monitor stem.
        box((2,15,4,18),'shadow'); box((11,15,13,18),'shadow')
        box((2,16,3,17),'wood'); dot(2,16,'edge')
        box((11,16,12,17),'wood'); dot(11,16,'grain')
        box((1,4,14,14),'ink'); box((2,3,13,13),'wood')
        box((3,2,12,3),'grain'); line((3,2,11,2),'edge')
        box((2,4,3,11),'copper'); line((2,4,2,10),'edge')
        box((12,4,13,11),'rust'); line((12,4,12,10),'copper')
        box((2,12,13,15),'shadow'); box((3,12,12,14),'rust')
        line((3,12,12,12),'copper'); line((3,15,12,15),'ink')
        box((4,13,6,14),'wood'); box((9,13,11,14),'wood')
        dot(4,13,'grain'); dot(9,13,'grain')
        # Steel corners echo native workbench construction, without its layout.
        for x in (1,13):
            box((x,4,x+1,5),'metal'); dot(x,4,'pale')
            box((x,11,x+1,12),'metal'); dot(x,11,'steel')
        if direction == '':
            box((4,3,10,8),'metal'); line((4,3,10,3),'steel')
            line((4,2,9,2),'pale'); line((3,3,3,7),'metal')
            line((11,4,11,8),'ink')
            box((4,4,10,7),'dark'); line((4,4,9,4),'teal')
            lit((5,5,9,5),'aqua'); lit((5,5,5,6),'glow')
            lit((7,5,7,6),'aqua'); lit((9,5,9,6),'glow')
            line((4,8,10,8),'ink')
            # Copper wiring runs into a raised wheel on the right.
            line((11,6,11,9,8,9),'copper'); dot(11,6,'light')
            box((3,9,7,10),'wood'); line((3,9,7,9),'edge')
            dot(4,10,'shadow'); dot(6,10,'shadow')
            box((10,9,11,10),'dark'); dot(10,9,'steel')
            lit((8,11,8,11),'aqua')
        elif direction == '_side':
            # Narrow illuminated edge, broad opaque housing on its right.
            box((4,4,8,9),'metal'); line((4,3,8,3),'steel')
            box((4,5,5,8),'dark'); lit((4,6,4,7),'aqua')
            box((6,5,8,8),'shadow'); line((6,4,8,4),'metal')
            line((6,5,6,8),'ink'); dot(8,5,'steel')
            line((10,4,11,4,11,8,9,8),'copper'); dot(10,4,'edge')
            box((10,9,11,10),'metal'); dot(10,9,'pale')
            line((4,10,8,10),'edge'); dot(5,11,'shadow'); dot(7,11,'shadow')
            for x in (5,7,9): line((x,13,x,14),'ink')
        else:
            # Rear enclosure: no display, just a vent and one cable gland.
            box((4,5,10,10),'metal'); line((4,5,10,5),'steel')
            box((5,6,9,9),'shadow'); line((5,6,8,6),'wood')
            line((6,7,8,7),'ink'); line((6,9,8,9),'ink')
            line((10,8,10,11),'rust'); dot(10,8,'copper')
            lit((11,10,11,10),'aqua')
            line((3,3,12,3),'edge'); line((4,4,10,4),'wood')
            box((11,5,11,9),'copper'); dot(11,5,'light')
            line((3,11,12,11),'copper'); dot(4,11,'steel')
            for x in (5,7,9): line((x,13,x,14),'ink')
    else:
        # Top surface occupies most of the sprite, as on native workbenches;
        # the four-pixel front/feet establish height without an upright icon.
        box((1,11,4,14),'shadow'); box((11,11,14,14),'shadow')
        box((2,12,3,13),'wood'); box((12,12,13,13),'wood')
        dot(2,12,'grain'); dot(12,12,'grain')
        line((4,12,11,12),'shadow')
        box((1,2,14,10),'shadow'); box((2,1,13,9),'wood')
        line((2,1,13,1),'edge'); line((2,2,13,2),'grain')
        # Offset plank seams and short grain clusters, not uniform stripes.
        line((6,2,6,8),'shadow'); line((7,3,7,7),'grain')
        line((2,6,5,6),'grain'); line((9,4,12,4),'grain')
        line((10,5,10,6),'shadow'); dot(4,3,'edge')
        line((2,9,13,9),'edge'); line((2,10,13,10),'wood')
        box((2,11,13,11),'shadow'); dot(3,10,'grain'); dot(12,10,'grain')
        for x in (1,13):
            box((x,1,x+1,2),'steel'); dot(x,1,'pale')
            box((x,8,x+1,9),'metal'); dot(x,8,'steel')
        if direction == '':
            # Open two-jaw vise holds a faceted gem above the left tabletop.
            box((3,5,6,7),'dark'); line((3,7,6,7),'metal')
            poly([(4,2),(6,4),(5,6),(3,5),(3,4)],'teal')
            lit((4,3,4,4)); dot(5,4,'aqua')
            dot(2,6,'steel'); dot(7,6,'steel'); dot(4,7,'pale')
            line((9,3,11,5),'shadow'); line((10,3,12,5),'copper')
            line((9,2,10,2),'pale'); dot(9,3,'metal')
            line((10,7,12,7),'edge'); line((11,6,12,6),'rust')
        elif direction == '_side':
            box((3,4,5,7),'dark'); line((3,7,5,7),'metal')
            poly([(4,1),(6,3),(5,5),(3,4),(3,3)],'teal')
            lit((4,2,4,3)); dot(5,3,'aqua'); dot(3,6,'pale')
            line((9,3,9,6),'rust'); line((8,3,10,3),'pale')
            line((11,7,12,7),'copper'); dot(12,6,'edge')
            box((4,9,7,10),'metal'); dot(5,9,'steel')
        else:
            box((9,5,12,7),'dark'); line((9,7,12,7),'metal')
            poly([(10,2),(12,4),(11,6),(9,5),(9,4)],'teal')
            lit((10,3,10,4)); dot(11,4,'aqua')
            dot(8,6,'steel'); dot(13,6,'steel')
            line((3,4,5,6),'rust'); line((3,3,4,3),'pale')
            dot(3,4,'metal'); line((3,7,5,7),'edge')
            line((3,10,12,10),'grain'); dot(7,10,'shadow')
    return im, em


def main():
    OUT.mkdir(exist_ok=True)
    for name in ('Terminal', 'Workbench'):
        for direction in ('', '_side', '_up'):
            im, em = device(name, direction)
            im.save(OUT / f'{name}{direction}.png')
            em.save(OUT / f'{name}{direction}_emissive.png')


if __name__ == '__main__':
    main()
