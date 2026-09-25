# Fast Auto Fishing (Core Keeper mod)

Fork of **mikufish** by Jiamu Sun (GPL-3.0-or-later) with a timer squash bolted on:
- mikufish part: toggle button in the inventory; auto-answers every nibble (mini game off).
- fastfish part: every wait in the fishing loop is capped (bite wait 0.05 s, nibble window 0.6 s,
  cast/throw/pull-up 0.25 s). Hit chance, loot tables, bait, XP and shoals are vanilla.
- Octopus boss fishing is left at normal pace.

License: GPL-3.0-or-later (inherited). Source ships in the mod.

Note: the manifest `name` must stay `mikufish` because the toggle-button prefab inside the asset
bundle references its script by that assembly name (the loader names the compiled assembly after
the manifest name). Because of this, it also replaces the original Autofish: disable that one.
