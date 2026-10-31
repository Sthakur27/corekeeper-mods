# Item Spawner

A searchable spawn browser for the host and admins. Press **Z** (rebind under Controls > Item Spawner) to open a grid of every item in the game, including items from other mods (Magic Mirror, Ender Chest, every Runic Storage tier).

## Controls

- **Left-click** an item: one on your cursor. Click again to add one more. **Ctrl-left-click**: a full stack on your cursor.
- **Right-click**: one straight into your bag. **Ctrl- or Shift-right-click**: a full stack into your bag.
- **Shift-left-click**: a full stack into your bag. Bag spawns drop at your feet if the bag is full.
- Category tabs on the left and the search box narrow the list. The grid/list button switches views.
- **Level bonus** (the two buttons next to grid/list, or the `[` and `]` keys): adds levels on top of each gear piece's or tool's base level. "Lv base" means the normal level; "Lv +3" means three levels above it. Hold Shift to step by 5. It never goes below base, and an item stops at the game's max level instead of overshooting. Hover an item to see its stats at that level.
- Esc, Z or opening another inventory closes the browser.

## Who can spawn

Only the host or an admin (or a creative-mode character). Other players can open the browser to look but cannot spawn.

## Requirements

CoreLib, and Runic Storage (the browser uses its panel layout). Included in Sid's Overhaul. Only the player using it needs the mod; the server needs nothing extra.

## Notes

If another mod also uses Z, rebind one of them under Controls.

Spawning uses the game's own create-item request (the one that hands out starter items). The admin check runs on the spawning player's game.
