# Build Tools

Bigger tool areas, a block brush and roofing gadget modes, and every tool type and block category remembers its own size. Replaces **Placement Plus** and **Roofing Gadget Plus**; uninstall both before using it.

## Keys (rebind under Controls > Build Tools)

| Key | Does |
|---|---|
| Keypad + / Keypad - | Size of the held tool or block brush (hold to repeat; the brush goes down to 1x1) |
| C | Brush shape: off, square, row, column |
| V | Blocks: toggle (vanilla) / ground only / walls only. Roofing gadget: toggle / add roof only / remove roof only |
| Left Alt (hold) | Replace: swap the blocks under the brush for the one you hold |
| R | Still cycles tool sizes like in vanilla |

## Tool sizes

- Shovels, hoes, the roofing gadget and paint tools can be resized up to **Max tool size** (default 9x9, range 3 to 15).
- Each tool type keeps its own size: shovel, hoe, watering can, roofing gadget, seeder and paint tool. Sizes are saved and come back after you quit and rejoin. All tiers of one tool share one size, capped at what each tier allows.
- **Watering cans** keep their own maximum (2x2 / 4x4 with Bigger Watering Cans, vanilla otherwise). The size keys can only make them smaller.

## Block brush

- Hold a block, floor, seed or other 1x1 placeable and press Keypad + (or C) to turn the brush on. Right-click places it on every free cell around where you aim; the preview shows the whole brush.
- Size and shape are remembered per category: **blocks**, **floors**, **seeds**, **other objects**.
- **Block mode (V)** for wall blocks: *toggle* is vanilla (floor on bare ground, wall on floor), *ground only* places the floor version of the block, *walls only* places walls and never puts down floor.
- **Replace (hold Left Alt)**: a different block of the same layer is swapped for the held one and the old block goes to your inventory. Outside creative it needs a pickaxe (walls) or a shovel (floors) somewhere in your inventory, and it never touches obsidian or indestructible tiles.
- Free cells follow the normal placement rules (reach, line of sight, blocked spots, tilled ground for seeds); each cell uses one item from the stack. Wall-mounted items and objects bigger than one tile always place one at a time.

## Roofing gadget

- **V** switches between vanilla toggle, add roof only and remove roof only. The mode is remembered.
- **Roofing gadget never wears out** (on by default) keeps its durability full.

## Settings

Mod Options > Build Tools: Max tool size, Remember sizes per tool and block type, Roofing gadget never wears out.

## Requirements

CoreLib and Mod Options. Install on the host and every player. In multiplayer, the host's Max tool size decides the real area; use the same value so previews match.
