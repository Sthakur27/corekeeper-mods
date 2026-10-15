# Core Archive sprite implementation

The concept PNGs are visual direction references. Runtime sprites are original editable pixel maps in `../../Unity/make_archive_art.py`; they are not downscaled crops of the concept pictures.

- Terminal: 16 x 20 pixels, rune tablet, copper rails, three control keys, short stone legs.
- Copper unit: 16 x 16 pixels, sealed slate vault, copper bands, inset crystal, three front recesses.
- Each has front, side, and rear artwork plus a corresponding emissive mask.
- Transparent pixels use alpha 0; solid pixels use alpha 255; no filtered edges.
- `runtime-sprites.png` shows the three views at 6x and at 1x.
- All storage tiers now use the vault silhouette with their own metal bands. Gems and workbench retain their existing artwork.

Run `python ConnectedStorage/Unity/make_archive_art.py` to regenerate the terminal and all eight units. The full `make_art.py` pipeline also reapplies this direction after generating the other devices and tiers.
