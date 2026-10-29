"""Update existing Workshop covers only; preserve descriptions, tags, visibility and mod files."""
import argparse
import json
from pathlib import Path
import subprocess
import sys

import workshop_publish as workshop
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--only', default='', help='Comma-separated Workshop keys; default: all existing items')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    ids = json.loads(Path(workshop.IDS).read_text())
    covers = {key: logo for key, _, logo, _ in [workshop.OVERHAUL] + workshop.MODS}
    covers[workshop.COLLECTION[0]] = workshop.COLLECTION[2]
    selected = set(filter(None, args.only.split(','))) or set(covers)
    unknown = selected - covers.keys()
    if unknown:
        parser.error('Unknown keys: ' + ', '.join(sorted(unknown)))
    items = []
    for key, logo in covers.items():
        if key not in selected:
            continue
        if key not in ids:
            parser.error(f'{key} has no existing Workshop item')
        preview = Path(workshop.REL) / logo
        with Image.open(preview) as im:
            if im.width != im.height and im.width * 9 != im.height * 16:
                parser.error(f'{preview} must be square or 16:9')
            im.verify()
        if preview.stat().st_size >= 1_000_000:
            parser.error(f'{preview} exceeds the preview size budget')
        items.append({'key': key, 'preview': str(preview), 'previewOnly': True, 'gallery': workshop.gallery(key)})
    dest = Path(workshop.STAGE) / 'artwork-items.json'
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps(items, indent=2), encoding='utf-8')
    workshop.build_tool()
    print(f'Staged {len(items)} artwork-only updates: {dest}', flush=True)
    if args.dry_run:
        return
    if 'CoreKeeper.exe' in subprocess.run(['tasklist'], capture_output=True, text=True, check=True).stdout:
        sys.exit('Close Core Keeper first; the artwork uploader uses its Steam app ID.')
    result = subprocess.run(['dotnet', str(Path(workshop.TOOL) / 'WorkshopUploader.dll'), str(dest), workshop.IDS], cwd=workshop.TOOL)
    sys.exit(result.returncode)


if __name__ == '__main__':
    main()
