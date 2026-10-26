"""Original mushroom armor scene using the shared pixel-sprite helper."""
from pathlib import Path
from PIL import ImageDraw
from make_art import cave_bg, sprite, font, text_shadow

image = cave_bg(seed=3180)
d = ImageDraw.Draw(image)
d.rectangle((0, 590, 1280, 720), fill=(35, 39, 44))
rows = [
    '.......kkkkkkkk.......',
    '.....kksssssssskk.....',
    '...kksswwsssssssskk...',
    '..ksssssssSssssssssk..',
    '.kssssssSSssSSssssssk.',
    'kSSSSSSSSSSSSSSSSSSSSk',
    'kkkkkkkkkkkkkkkkkkkkkk',
    '...kbbbbbbbbbbbbbbk...',
    '...kbbwwbbbbwwbbbbk...',
    '...kbbkkbbbbkkbbbbk...',
    '..kbbbbbbbbbbbbbbbbk..',
    '.kbbbbbkkbbbbkkbbbbbk.',
    '.kbbbbbbbbbbbbbbbbbbk.',
    '..kkbbbkkkkkkbbbkkk..',
    '....kkk......kkk.....',
]
palette = {'k': (24, 26, 34), 's': (157, 173, 184), 'S': (83, 104, 122), 'w': (225, 235, 238), 'b': (180, 151, 112)}
brute = sprite(rows, palette, 20)
image.paste(brute, (660, 295), brute)
# Original sword and a concentrated impact against the stone cap.
d.polygon([(250, 522), (475, 298), (519, 285), (507, 331), (281, 555)], fill=(196, 228, 239))
d.line([(273, 538), (506, 302)], fill=(255, 255, 245), width=9)
d.line([(237, 505), (301, 567)], fill=(242, 181, 65), width=17)
d.line([(260, 549), (214, 595)], fill=(128, 83, 63), width=21)
for a,b in [((577,363),(651,386)),((607,305),(660,361)),((652,276),(687,342))]:
    d.line((a,b), fill=(255,205,89), width=11)
text_shadow(d, (640, 50), 'MUSHROOM SLAYER', font(88), (240, 231, 205))
text_shadow(d, (640, 160), 'Strong hits break through', font(38), (167, 194, 210))
text_shadow(d, (863, 227), '200', font(59), (255, 210, 100))
text_shadow(d, (640, 640), 'GRAY: 10% AT 200+  |  RED: UNCHANGED', font(31), (235, 220, 191))
image.save(Path(__file__).with_name('mushroomslayer_logo.png'))
