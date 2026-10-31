"""Faster Machines cover: the screenshot cover plus an animated speed dial on the right-hand grass.

    python release/art_direction/make_fastermachines_cover.py [--out DIR]

Writes FasterMachines.gif (animated: the number counts up 1x -> 100x while a slider knob slides along,
like the Mod Options choice it stands for) and FasterMachines.png (still frame at 5x, the default).
The dial is drawn at quarter scale with the banner's palette and scaled up with nearest-neighbour, so
it reads as part of the same pixel banner set. Steam previews must stay under 1 MB.
"""
from pathlib import Path
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_screenshot_covers as covers  # noqa: E402

P = 4
VALUES = ['1x', '2x', '5x', '10x', '50x', '100x']
STILL = '5x'
DIAL_W, DIAL_H = 60, 64            # quarter-scale pixels
DIAL_POS = (946, 292)              # canvas position (right-hand grass of the platform)
TRACK_X0, TRACK_X1, TRACK_Y = 8, 51, 50


def dial(value, knob_t):
    """One dial at quarter scale. knob_t: 0..1 position along the track."""
    img = Image.new('RGBA', (DIAL_W, DIAL_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # plate: teal cloth with the gold rule, same colours as the title banner
    d.rectangle((1, 3, DIAL_W - 2, DIAL_H - 2), fill='#164a46', outline='#2a8a80')
    d.line((0, 1, DIAL_W - 1, 1), fill='#b8863a')
    d.line((0, 2, DIAL_W - 1, 2), fill='#7a5520')
    for x in range(4, DIAL_W - 3, 6):
        d.point((x, 5), fill='#1d6a64')

    # big number, Impact 1-bit with dark outline and a light top band (banner title treatment)
    size = 24
    layer = Image.new('L', img.size)
    ld = ImageDraw.Draw(layer)
    ld.fontmode = '1'
    while True:
        f = ImageFont.truetype('C:/Windows/Fonts/impact.ttf', size)
        if ld.textlength(value, font=f) <= DIAL_W - 22 or size <= 10:
            break
        size -= 1
    tw = ld.textlength(value, font=f)
    top = 9 + (24 - size) // 2
    ld.text(((DIAL_W - tw) / 2, top), value, font=f, fill=255)
    img.paste('#1a0f08', (0, 0), layer.filter(ImageFilter.MaxFilter(3)))
    img.paste('#ffd36a', (0, 0), layer)
    band = Image.new('L', img.size)
    band.paste(255, (0, 0, DIAL_W, top + 12))
    img.paste('#fff0b8', (0, 0), ImageChops.multiply(layer, band))

    # < > arrows like the in-game option selector
    cy = 24
    d.polygon([(4, cy), (8, cy - 4), (8, cy + 4)], fill='#94f7dd')
    d.polygon([(DIAL_W - 5, cy), (DIAL_W - 9, cy - 4), (DIAL_W - 9, cy + 4)], fill='#94f7dd')

    # slider: dark groove, filled part in gold up to the knob, one notch per value
    d.rectangle((TRACK_X0, TRACK_Y - 1, TRACK_X1, TRACK_Y + 1), fill='#0c2a28')
    kx = round(TRACK_X0 + knob_t * (TRACK_X1 - TRACK_X0))
    d.rectangle((TRACK_X0, TRACK_Y - 1, kx, TRACK_Y + 1), fill='#e8b85a')
    for i in range(len(VALUES)):
        nx = round(TRACK_X0 + i * (TRACK_X1 - TRACK_X0) / (len(VALUES) - 1))
        d.point((nx, TRACK_Y + 3), fill='#2a8a80')
    d.rectangle((kx - 2, TRACK_Y - 4, kx + 2, TRACK_Y + 4), fill='#ffd36a', outline='#1a0f08')
    d.line((kx - 1, TRACK_Y - 3, kx + 1, TRACK_Y - 3), fill='#fff0b8')
    return img


def frame(base, value, knob_t):
    canvas = base.copy()
    big = dial(value, knob_t).resize((DIAL_W * P, DIAL_H * P), Image.NEAREST)
    shadow = Image.new('L', canvas.size, 0)
    x, y = DIAL_POS
    shadow.paste(big.getchannel('A').point(lambda a: 150 if a else 0), (x + 8, y + 10))
    canvas.paste('#000000', (0, 0), shadow.filter(ImageFilter.GaussianBlur(10)))
    canvas.paste(big, DIAL_POS, big)
    return canvas


def main():
    out = covers.REPO / 'build/artwork-review/2026-10-31/screens'
    if '--out' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--out') + 1])
    out.mkdir(parents=True, exist_ok=True)
    base = covers.compose('FasterMachines')

    n = len(VALUES) - 1
    still = frame(base, STILL, VALUES.index(STILL) / n)
    still.save(out / 'FasterMachines.png', optimize=True)

    frames, durations = [], []
    for i, v in enumerate(VALUES):
        if i:   # knob glides from the previous notch
            for s in (0.25, 0.5, 0.75):
                frames.append(frame(base, VALUES[i - 1], (i - 1 + s) / n))
                durations.append(60)
        frames.append(frame(base, v, i / n))
        durations.append(1400 if v == '100x' else 700)
    for s in (0.8, 0.6, 0.4, 0.2):   # quick slide back to 1x
        frames.append(frame(base, '100x', s))
        durations.append(50)

    # One shared palette so unchanged pixels stay identical between frames (keeps the GIF small).
    pal = still.quantize(colors=255, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    q = [f.quantize(palette=pal, dither=Image.Dither.NONE) for f in frames]
    gif = out / 'FasterMachines.gif'
    q[0].save(gif, save_all=True, append_images=q[1:], duration=durations, loop=0, optimize=False, disposal=1)
    for p in (out / 'FasterMachines.png', gif):
        print(p, p.stat().st_size)


if __name__ == '__main__':
    main()
