# Drill Pod (Alpha)

Drill Pod adds an end-game hovercraft that drills through walls while you drive. Craft one at the **Rift Statue** with **300 Pandorium Bars and 100 Solarite Bars**, place it on land or water, and use interact to get in.

It travels over land, water, lava and pits at **1.6x boat speed** (the Speeder Boat is 1.3x). Hovering over lava does not hurt the rider. Use interact to get out; you need a walkable tile beside you.

This is still an alpha, tested in single player only. Multiplayer is untested, and tuning may change. It was previously called Drill Unit; existing placed units and saves carry over.

## Drilling and riding

Hold **left click** and steer into a wall to drill a **3-wide, 2-deep** area ahead of the pod, plus the blocks beside it. The blocks and ore drop to you.

You can use a pickaxe, drill, laser drill or shovel while riding, and you can eat food. Weapons, off-hand items and bare-handed attacks are disabled while seated.

## Ground modes

Press **G** while riding to cycle **off → lay floor → dig → off**. Ground mode starts off and resets to off when you get out. You can rebind the key under **Controls → Drill Pod**.

Both modes only work while you hold **left click**. The pod still drills ahead as you steer.

- **Lay floor:** fills water, lava and pits in a **3x3** area around you. Each tile uses one block from your inventory, taking the first block that has a floor version: Dirt, for example, makes dirt floor. This costs no blocks in creative worlds.
- **Dig:** digs a **3x3** area around you. Placed floors, rugs and rails come up first, then the ground, leaving pits that nearby water can flow into. Dug-up items come to you. Tiles supporting placed objects, such as chests, tables and plants, are left alone.

Dig mode also breaks walls, farmland and rails within that area. It works quickly, so keep it off around parts of your base you want to keep.

## Requirements and limits

Requires **CoreLib** and **Core Keeper 1.3.0**. The host and all joining players need the mod and CoreLib. Multiplayer has not been tested; the weapon restriction is applied on the rider's own client.

Before uninstalling, pick up placed pods and back up your save. Removing the mod can cause its items to be lost; picking a pod up does not make it a vanilla item.

## Cover and source

The cover uses in-game screenshots of drilling a tunnel, hovering over lava, and a dug trench beside a laid floor. The HDR captures were tone-mapped and brightened, the cursor was painted out, and the rider was moved onto the trench end in the third image. A title banner and labels were added.

[Source and development notes](https://github.com/Sthakur27/corekeeper-mods/blob/main/DrillUnit/DEVELOPER.md).
