# Build Tools

Dig, plant, paint, and place blocks over a bigger area. You can build a row of walls in one go, lay down flooring, or swap blocks without digging them out first. Each tool and block type remembers the size you last used, even after you quit.

## Controls

Hold a tool or something you want to place, then use these keys. You can rebind them under **Controls > Build Tools**.

| Key | What it does |
|---|---|
| Keypad + / Keypad - | Make the tool area or block brush bigger or smaller. Hold to keep changing it. |
| Keypad * | Change the brush shape: off, square, horizontal line, or vertical line. |
| Keypad / | Switch block modes, or roofing modes while holding the roofing gadget. |
| Left Alt | Turn block swapping on or off. You can change this to hold instead. |
| R | Cycle tool sizes as usual. |

## Bigger tool areas

Shovels, hoes, watering cans, roofing gadgets, and paint tools can cover up to **10x10** by default. Change **Max tool size** to anything from 3 to 15 if you want a different limit. The brush can go down to 1x1.

Your shovel, hoe, watering can, roofing gadget, seeder, and paint tool each remember their own size. Different tiers of the same tool share that choice, but a lower-tier tool still stops at its own size limit.

If you also use **Bigger Watering Cans**, its 2x2 and 4x4 limits still apply. You can shrink the area with the size keys, or turn on **Override Bigger Watering Cans sizes** to use Build Tools' maximum instead. This also applies when both features are enabled in Sid's Overhaul.

## Placing blocks and seeds

Hold a block, floor, seed, or another one-tile object and press **Keypad +** or **Keypad multiply (*)** to turn on the brush. Aim where you want it and right-click. The brush sits in front of you in the direction your mouse points (up, down, left or right), starting at the tile next to you. Sideways it follows your mouse a little (one tile, or up to half the brush for big ones), and it never covers your character. The preview shows the area you'll fill, and each free spot uses one item from your stack.

Blocks, floors, seeds, and other objects each remember their own brush size and shape. Placement still follows the usual rules: you need to be in reach, nothing can be in the way, and seeds need tilled ground. Wall-mounted items and objects larger than one tile still place one at a time.

When you're holding a wall block, **Keypad /** cycles between:

- **Auto:** fill any missing floor first. Once the whole area has floor, the next swipe builds walls.
- **Floor only:** lay the floor version of the block.
- **Walls only:** place walls without laying floor.

## Swapping blocks

Press **Left Alt** to turn Swap on, then place the block you want over the old one. The old block goes into your inventory. Floors replace floors and walls replace walls.

Outside creative mode, you'll need a shovel in your inventory for floors or a pickaxe for walls. You can turn that requirement off in settings. Swap only checks that you have the tool, not how much mining power it has, but it won't replace obsidian or indestructible tiles.

If nothing gets swapped, chat tells you why—for example, you're out of reach, missing a shovel, or trying to swap a floor with a wall on it. You can also set the Swap key to **Hold** if you'd rather only swap while holding Left Alt.

## Roofing

With the roofing gadget equipped, **Keypad /** cycles between the usual roof toggle, **close roof**, and **open roof**. It remembers your last mode.

The roofing gadget doesn't wear out by default. Turn off **Roofing gadget never wears out** if you want normal durability.

## Settings and installation

Open **Mod Options > Build Tools** for the size limit, remembering sizes, Swap key behavior, tool requirement, watering-can override, and roofing durability. Swap defaults to Toggle, the shovel/pickaxe requirement is on, and the watering-can override is off.

Requires **CoreLib** and **Mod Options**, installed on the host and everyone joining. In multiplayer, the host's **Max tool size** decides how much you actually affect. Set yours to the same value so your preview matches.

Don't run this alongside other mods that change the same tool sizes. There are no sledgehammer area changes, extra paint-tool modes, or controller bindings.

Inspired by **Placement Plus** and **Roofing Gadget Plus**. Build Tools was made separately and doesn't use their code.
