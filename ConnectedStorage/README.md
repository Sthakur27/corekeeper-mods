# Runic Storage

Connect storage units to a terminal and search all their items from one inventory. You can sort everything, filter by item type, and craft at nearby stations using what's stored inside.

Copper units start at **40 slots** each. When you need more space, add another unit or upgrade one you already have, up to **360 slots** per Relucite unit. You don't have to empty a unit to upgrade it.

**New in 0.10.3.** All nine storage tiers now have gem-tinted lids and matching corner clamps.

## Getting started

1. Open a workbench, select the wooden tier, and find the **Storage Workbench** under **Base**. It costs 20 wood.
2. At your new workbench, make a **Storage Terminal** (10 copper bars + 20 wood) and a **Copper Storage Unit** (30 copper bars).
3. Place them next to each other. They need to touch along an edge; diagonals and gaps won't connect.
4. Press **E** on the terminal or a connected unit to open your storage.

Keep a terminal connected or you won't be able to open the units. You still need to walk over to your storage to use it; there's no remote access.

## Finding and moving items

Use the search box to find something by name, or the tabs on the left to show just materials, weapons, food, and so on. Click the same tab again, or All, to see everything. Seeds are under Materials; pet eggs are under Pets. Placeable tiles go under Blocks; things like chests, stations, and lights go under Furniture.

Click to pick up and place items, or Shift-left-click to move a stack. Right-click takes one item, and you can keep right-clicking to take more. The game's usual modifier keys work for taking ten or half a stack too (half stacks round up). Quick-stack puts matching items from your bag into storage. Bulk transfers leave your hotbar and locked items alone.

**Sort** combines matching stacks and groups items in tab order (Materials first, Other last), then by name within each category. You can reverse the sort order. Your arrangement stays put when you close and reopen storage, though adding a new unit triggers a sort the next time you open it. For bigger collections, you can scroll or switch between grid and list views.

Nearby crafting stations can use your stored materials, just like they can with nearby chests. You still craft at the station; the terminal doesn't have its own crafting menu.

## Upgrading units

Make an upgrade gem at the Storage Workbench, hold it in your hotbar, and press **E** on a unit. Each gem costs **40 bars of its material** and adds 40 slots. You have to go through the tiers in order.

| Tier | Slots per unit |
|---|---:|
| Copper | 40 |
| Tin | 80 |
| Iron | 120 |
| Scarlet | 160 |
| Octarine | 200 |
| Galaxite | 240 |
| Solarite | 280 |
| Pandorium | 320 |
| Relucite | 360 |

These sizes stay the same with chest-capacity mods installed. If an older version left a unit holding more than it should, you can still take the extra items out.

## Moving things around

When you mine a unit, its items move into any space left in the storage still connected to the terminal. Anything that doesn't fit drops on the ground with the unit. If the unit isn't connected, all its contents drop. Upgraded units keep their tier when you pick them up.

Breaking a connection doesn't empty the units on the other side. Reconnect them to get access to their items again.

## Installing

Install **Runic Storage** and **CoreLib** on the host and every joining player. The interface uses mouse and keyboard, with English labels.

Currently in testing. Multiplayer has not yet been verified.

**Empty your storage before uninstalling the mod.**

## Credits

Uses UI components adapted from **moorowl's Item Browser**, under the [MIT license](https://github.com/Sthakur27/corekeeper-mods/blob/main/ConnectedStorage/ThirdParty/ItemBrowser/LICENSE). You don't need to install Item Browser separately.
