# Potion of Return

![Potion of Return — cover made from in-game screenshots](../release/wormholepotion_logo.png)

Potion of Return takes you to the Core and leaves a map marker you can use to get back. Craft one at an **Ancient Hologram Pod for 25 Gold Bars**. If you also use Potion Seller, the Caveling Merchant sells it for **2,500 Ancient Coins**.

Use the potion like a Recall Idol. After a three-second cast, you teleport to the Core and use up the potion. Moving, switching items or taking enough damage interrupts the cast; an interrupted cast does not consume it.

To return, stand on any active waypoint, open the map and click the potion icon labelled **Return Point**. It works like travelling between waypoints, but the marker disappears after one trip.

## Return points

- Drinking within 12 tiles of the Core still takes you home, but leaves no return point.
- Drinking another potion in the same session replaces your previous return point. Markers from other players or earlier sessions remain until someone uses them.
- Any player can use any return point, including one left by someone else.
- Return points are saved with the world, though keeping one across a save and reload has not yet been tested.

Previously released as Wormhole Potion. Only the name changed; existing potions and markers still work.

## Installation and limits

No settings or library dependencies are required. In multiplayer, install the mod on both the host and every client.

Subscribe on the Workshop, or extract the manual archive into `CoreKeeper_Data/StreamingAssets/Mods/WormholePotion` with `ModManifest.json` directly inside that folder. Restart Core Keeper. Existing Ancient Hologram Pods gain the recipe automatically.

The recipe uses the pod's empty recipe slot. If another mod has filled that slot, Potion of Return leaves the existing recipe alone and cannot add its own. Use either the standalone mod or an Overhaul version that includes it; do not install both.

Crafting, drinking, the map marker and the return trip have been tested in single player, including removal of the marker after use. Multiplayer has not yet been tested.

Before uninstalling, use your return points and remove the potions from inventories and storage.

[Development notes](https://github.com/Sthakur27/corekeeper-mods/blob/main/WormholePotion/DEVELOPER.md)

Cover made from in-game screenshots of casting, the return marker, the waypoint teleport line and the recipe, with the potion's inventory sprite enlarged. HDR captures were tone-mapped; the title banner, step numbers and labels were added.
