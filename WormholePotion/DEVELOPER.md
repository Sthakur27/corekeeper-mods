# Potion of Return development notes


- `python -I WormholePotion/Unity/make_art.py`: regenerate the potion icon.
- `python -I WormholePotion/Unity/dump_vanilla.py`: re-dump the vanilla waypoint map marker and Recall Idol prefabs into `vanilla.json` after a game update.
- `python WormholePotion/Unity/build_bundle.py`: build the Windows/Linux bundles with Unity 6000.0.59f2 and the official Mod SDK, then run `verify_bundle.py`.
- `python WormholePotion/Unity/compile_check.py`: compile the runtime scripts against the local game assemblies.

How it is put together: the potion is a cast item with the Recall Idol's cast values. `PotionCastSystem` runs before vanilla's Burst player-state systems in both predicted worlds and requests vanilla's teleport state. On the server it consumes the potion and spawns the return marker (`WormholePotion_MarkerEntity`), a copy of the vanilla waypoint map marker that, unlike the vanilla one, is saved with the world. The map treats it as a waypoint, so the vanilla map UI sends the teleport. `ReturnMarkerSystem` (server) keeps markers active and removes one when a player's teleport targets it. `MarkerIconPatch` draws it with the potion icon.
