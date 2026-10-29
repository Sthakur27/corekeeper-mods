"""Convert NVIDIA HDR captures (.jxr, scRGB float) to tone-mapped PNGs for the cover/gallery scripts.

    python release/art_direction/hdr_to_png.py <stamp or .jxr path> [...] [--out DIR]

A stamp like "2026.10.29 - 00.23.19.74" (or just "00.23.19.74" for today's date prefix search) is looked
up in ~/Videos/NVIDIA/Desktop. Decoding uses Windows' own JPEG XR codec through jxr_to_raw.ps1
(PresentationCore WmpBitmapDecoder -> Rgba128Float); no extra codec install needed. Needs numpy.

Tone map (same as the Ender Stash captures): value / 10 (game UI white is ~9.5 in scRGB), clamp to
0..1, sRGB transfer curve. Output: <out>/<HH.MM.SS.cc>.png at full 3840x2160, which
make_screenshot_covers.py reads through its "hdr:<HH.MM.SS.cc>" stamps (HDR_DIR).
"""
from pathlib import Path
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
NVIDIA = Path.home() / 'Videos/NVIDIA/Desktop'
DEFAULT_OUT = REPO / 'build/artwork-review/hdr'


def find(arg):
    p = Path(arg)
    if p.suffix.lower() == '.jxr' and p.exists():
        return p
    hits = sorted(NVIDIA.glob(f'*{arg}*.jxr'))
    if len(hits) != 1:
        sys.exit(f'{arg}: expected one .jxr in {NVIDIA}, found {len(hits)}')
    return hits[0]


def convert(jxr, out_dir):
    with tempfile.TemporaryDirectory() as tmp:
        raw = Path(tmp) / 'img.raw'
        res = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
                              str(HERE / 'jxr_to_raw.ps1'), str(jxr), str(raw)],
                             capture_output=True, text=True, check=True)
        w, h = map(int, res.stdout.split())
        rgb = np.fromfile(raw, dtype='<f4').reshape(h, w, 4)[..., :3]
    v = np.clip(rgb / 10, 0, 1)
    v = np.where(v <= 0.0031308, 12.92 * v, 1.055 * np.power(v, 1 / 2.4) - 0.055)
    stamp = jxr.stem.rsplit(' - ', 1)[-1]
    out = out_dir / f'{stamp}.png'
    Image.fromarray((v * 255 + 0.5).astype(np.uint8)).save(out)
    print(out, f'{w}x{h}')
    return out


if __name__ == '__main__':
    args = sys.argv[1:]
    out_dir = DEFAULT_OUT
    if '--out' in args:
        i = args.index('--out')
        out_dir = Path(args[i + 1])
        del args[i:i + 2]
    if not args:
        sys.exit(__doc__)
    out_dir.mkdir(parents=True, exist_ok=True)
    for a in args:
        convert(find(a), out_dir)
