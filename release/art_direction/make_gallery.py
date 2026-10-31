"""Workshop gallery images (extra previews under the cover) from Sid's in-game screenshots.

    python release/art_direction/make_gallery.py [Key ...]

Writes release/gallery/<Key>_<n>.jpg: the full 3840x2160 capture downscaled to 1920x1080, with the
NVIDIA performance overlay in the top-right corner painted over. Desktop captures come from
~/Videos/NVIDIA/Desktop; "hdr:" stamps are the tone-mapped HDR captures (see make_screenshot_covers.py).
workshop_publish.py attaches every release/gallery/<Key>_*.jpg to that item, in file-name order.
"""
from pathlib import Path
import sys
from PIL import Image, ImageEnhance, ImageFilter

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
from make_screenshot_covers import shot  # noqa: E402

OUT = REPO / 'release/gallery'
MAX_BYTES = 1_000_000

# Most telling screenshot first.
GALLERY = {
    'SlotOrganizer': ['19.56.23.75'],
    'LoadoutSharing': ['19.57.23.85', '19.57.12.56'],
    'FiveLoadouts': ['19.57.12.56', '19.57.23.85'],
    'PetEditor': ['19.57.42.05', '19.57.33.20', '19.57.17.40'],
    'ArmorDye': ['19.59.16.75', '20.00.48.30', '19.58.32.13'],
    'ModOptions': ['19.58.40.45', '19.58.32.13'],
    'GoldenChance': ['20.01.09.34'],
    'ConnectedStorage': ['20.05.30.46', '20.12.13.42', '20.12.29.01'],
    'EnderStash': ['hdr:17.51.45.25', 'hdr:17.51.19.80', 'hdr:17.51.28.10'],
    'MagicMirror': ['hdr:00.23.31.80', 'hdr:00.23.58.85'],
    'BuildTools': ['hdr:14.21.40.31', 'hdr:14.21.54.44'],
    'ItemSpawner': ['hdr:14.41.28.59', 'hdr:14.41.05.93', 'hdr:14.41.49.61'],
}

# HDR captures that still show the NVIDIA performance overlay (the Ender Stash ones do not).
HDR_WITH_OVERLAY = {'hdr:00.23.31.80', 'hdr:00.23.58.85', 'hdr:14.21.40.31', 'hdr:14.21.54.44',
                    'hdr:14.32.51.61', 'hdr:14.41.28.59', 'hdr:14.41.49.61', 'hdr:14.41.05.93'}

# Dark HDR captures shown brighter in the gallery (same factor as their cover).
GAIN = {'hdr:14.21.40.31': 1.7, 'hdr:14.21.54.44': 1.7, 'hdr:14.32.51.61': 1.7, 'hdr:14.41.28.59': 1.7,
        'hdr:14.41.49.61': 1.7, 'hdr:14.41.05.93': 1.7}

# NVIDIA "Screenshot saved" toasts caught in a capture (4K pixel boxes), painted over like the overlay.
TOASTS = {'19.57.17.40': [(3200, 190, 3840, 400)]}


def clean(stamp):
    im = Image.open(shot(stamp)).convert('RGB')
    if not stamp.startswith('hdr:') or stamp in HDR_WITH_OVERLAY:
        # NVIDIA overlay: "FPS / GPU / CPU / LAT" text in the top-right corner.
        # Fill it by stretching the strip of game pixels just below, softened a little.
        x0, h = im.width - 360, 130
        strip = im.crop((x0, h, im.width, h + 24)).resize((360, h), Image.BILINEAR)
        im.paste(strip.filter(ImageFilter.GaussianBlur(3)), (x0, 0))
    for x0, y0, x1, y1 in TOASTS.get(stamp, []):
        strip = im.crop((x0, y1, x1, y1 + 24)).resize((x1 - x0, y1 - y0), Image.BILINEAR)
        im.paste(strip.filter(ImageFilter.GaussianBlur(3)), (x0, y0))
    if stamp in GAIN:
        im = ImageEnhance.Brightness(im).enhance(GAIN[stamp])
    return im.resize((1920, 1080), Image.LANCZOS)


def build(key):
    OUT.mkdir(parents=True, exist_ok=True)
    for old in OUT.glob(f'{key}_*.jpg'):
        old.unlink()
    for n, stamp in enumerate(GALLERY[key], 1):
        im, path = clean(stamp), OUT / f'{key}_{n}.jpg'
        for q in (90, 85, 80, 75):
            im.save(path, quality=q, optimize=True, progressive=True)
            if path.stat().st_size < MAX_BYTES:
                break
        print(path.relative_to(REPO), path.stat().st_size, f'q{q}')


if __name__ == '__main__':
    for key in sys.argv[1:] or GALLERY:
        build(key)
