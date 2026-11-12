# Item Spawner

Press **Z** to browse items, search by name, and spawn what you need onto your cursor or into your bag. The browser includes modded items, and a cooking view lets you choose two ingredients and preview the dish they make.

Spawning is available to the host, admins and creative-mode characters. Other players can use the browser to look at items, but cannot spawn them through it.

## Using the browser

Rebind **Z** under **Controls > Item Spawner** if it clashes with another key binding. Use the category tabs and search box to narrow the list, or switch between grid and list views.

- **Left-click** puts one item on your cursor. Click the same item again to add another.
- **Ctrl-left-click** puts a full stack on your cursor.
- **Right-click** puts one item in your bag.
- **Shift-left-click**, **Shift-right-click** or **Ctrl-right-click** puts a full stack in your bag.

Items sent to a full bag drop at your feet. Put down anything on your cursor before spawning a different item there. Items that do not stack still spawn one at a time.

The trash can remains beside your bag, so you can discard items without closing the browser. Press **Esc**, press the browser's toggle key again, or open another inventory to close it.

## Gear and tool levels

The level buttons beside the grid/list button, or the **[** and **]** keys, adjust a bonus above an item's base level. Hold **Shift** to change it by five levels at a time.

**Lv base** gives the normal item. **Lv +3** gives gear or tools three levels above their base level, up to the game's maximum. The bonus cannot go below zero. Hover over an item to preview its stats at the chosen level.

## Cooking

Press the cooking pot button at the top right to switch views. Two ingredient slots and a dish preview appear above a list of cooking ingredients; golden ingredients have their own entries.

Click an ingredient to fill the highlighted slot. Filling the first slot selects the second automatically. Click either slot to choose where the next ingredient goes, or right-click a slot to empty it.

With both ingredients selected, the preview shows the resulting dish's name, colors and stats. Use the same spawning controls as for other items. The results follow the cooking pot's recipes.

The **Gourmet** checkbox adds the cooking talent's extra rarity roll. A normal dish becomes Rare. A golden flower or legendary ingredient already makes a dish Rare; Gourmet raises that result to Epic. Press the pot button again to return to items.

## Requirements and multiplayer

For Core Keeper **1.3.0**. Requires **CoreLib** and **Runic Storage**, whose panel layout the browser uses. Item Spawner is also included in **Sid's Overhaul**; use the bundle or the standalone version.

Item Spawner itself is client-side: only the player using the browser needs it, and spawning uses the game's existing create-item request. Its required dependency Runic Storage has different multiplayer requirements: install Runic Storage and CoreLib on the host and every joining player. Runic Storage's multiplayer behavior has not yet been verified. The host/admin check for spawning runs on the spawning player's client, so it is not a server-enforced permission system.

## Cover

Cover and the first gallery image are made from in-game screenshots of the spawn browser and its cooking mode (HDR captures tone-mapped); title banners and highlights added.
