# Build Tools

Bigger tool areas, a block brush with floor/wall modes and block swapping, and roofing gadget modes. Every tool type and block category remembers its own size, even after you quit, and nothing is printed to chat except the mode changes you make.

Inspired by **Placement Plus** and **Roofing Gadget Plus**. Build Tools is a separate implementation and shares no code with them. Uninstall both before using it, since they change the same tool sizes.

## Keys (rebind under Controls > Build Tools)

| Key | Does |
|---|---|
| Keypad + / Keypad - | Size of the held tool or block brush (hold to repeat; the brush goes down to 1x1) |
| Keypad * | Brush shape: off, square, line across, line down |
| Keypad / | Block mode: auto / floor only / walls only. Roofing gadget: flip / close roof / open roof |
| Left Alt | Swap on/off (or hold, see Settings) |
| R | Still cycles tool sizes like in vanilla |

## Tool sizes

- Shovels, hoes, the roofing gadget and paint tools can be resized up to **Max tool size** (default 10x10, range 3 to 15).
- Each tool type keeps its own size: shovel, hoe, watering can, roofing gadget, seeder and paint tool. Sizes are saved and come back after you quit and rejoin. All tiers of one tool share one size, capped at what each tier allows.
- **Watering cans** keep their own maximum (2x2 / 4x4 with Bigger Watering Cans, vanilla otherwise). The size keys can only make them smaller.

## Block brush

- Hold a block, floor, seed or other 1x1 placeable and press Keypad + (or Keypad *) to turn the brush on. Right-click places it on every free cell around where you aim; the preview shows the whole brush.
- Size and shape are remembered per category: **blocks**, **floors**, **seeds**, **other objects**.
- **Block mode** for wall blocks:
  - *Auto*: while any cell under the brush has no floor yet, a swipe only lays floor; once it is all floored, the next swipe builds walls.
  - *Floor only*: places the floor version of the block.
  - *Walls only*: places walls and never puts down floor.
- **Swap**: a different block of the same layer is swapped for the one you hold, and the old block goes to your inventory. Left Alt turns Swap on and off with a chat message (or, with the setting on Hold, Swap is on while you hold it). Outside creative it needs a pickaxe (walls) or a shovel (floors) somewhere in your inventory, and it never touches obsidian or indestructible tiles.
- Free cells follow the normal placement rules (reach, line of sight, blocked spots, tilled ground for seeds); each cell uses one item from the stack. Wall-mounted items and objects bigger than one tile always place one at a time.

## Roofing gadget

- **Keypad /** switches between *flip* (vanilla), *close roof* and *open roof*. The mode is remembered.
- **Roofing gadget never wears out** (on by default) keeps its durability full.

## Settings

Mod Options > Build Tools: Max tool size, Remember sizes per tool and block type, Swap key (Toggle or Hold, default Toggle), Roofing gadget never wears out.

## Requirements

CoreLib and Mod Options. Install on the host and every player. In multiplayer, the host's Max tool size decides the real area; use the same value so previews match.

## Limits

- Swap checks that you carry a pickaxe or shovel, not your mining power.
- No sledgehammer area, paint-tool modes or controller bindings.
