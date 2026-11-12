# Coin Pouch

![Coin Pouch — equipment panel screenshot with an enlarged coin slot](../release/coinpouch_logo.png)

Coin Pouch gives your Ancient Coins their own slot below the vanity pants in the equipment window. It holds **99,999 coins**, compared with the usual 9,999 per stack, and fills automatically as you collect coins.

Coins from selling to merchants or taking them out of chests move there too.

You can buy from merchants as usual: prices count the coins in the pouch, and payment comes straight out of it. When a shop, chest or crafting station hides the equipment window, the pouch appears beside your inventory's hotbar row so you can still see your balance.

The same pouch is shared across all your loadouts. It accepts only Ancient Coins; trying to put anything else in it makes the slot shake. Once it reaches 99,999, any extra coins stay in your inventory.

Coins in the pouch stay with you when you die, because the game only drops your main inventory. Quick stack to nearby chests also leaves them alone. To store coins in a chest, drag them there yourself. If you drop them into your inventory instead, the pouch collects them again.

## Install and use

For Core Keeper 1.3.0. There are no settings, extra keys or library dependencies. Install Coin Pouch on both the host and every client for multiplayer.

Subscribe on the Workshop, or extract the manual archive into `CoreKeeper_Data/StreamingAssets/Mods/CoinPouch` with `ModManifest.json` directly inside that folder, then restart the game. Existing characters get the pouch when they join a world, and coins already in their inventory move into it. Use either the standalone mod or a version of Sid's Overhaul that includes it; do not install both.

## Changing mods or uninstalling

If you add or remove mods that add player inventory slots, your coins may not follow the pouch to its new position when you rejoin the last world you played. Joining a different world moves them correctly.

**Empty the pouch before uninstalling. Coins left in it are lost when the slot is removed.** Drag them directly into a chest, splitting amounts above 9,999 across several stacks. Coins placed in your inventory are collected again while the mod is active, so check that the pouch is empty before removing it.

[Developer notes](https://github.com/Sthakur27/corekeeper-mods/blob/main/CoinPouch/DEVELOPER.md) describe how the slot works.

Cover made from an in-game screenshot of the equipment panel with 20,405 coins, plus an enlarged view of the coin slot. The HDR capture was tone-mapped, with a title banner and highlight added.
