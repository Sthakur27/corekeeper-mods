# Armor Dye

Core Keeper mod: dye your armor, weapons and tools any color. The dye belongs to the item, so two
copies of the same chestplate can be different colors, and a piece keeps its color when you move,
store, trade or upgrade it.

- **Dye palette**: hover an armor piece, weapon or tool in your inventory, hotbar, equipment or vanity
  slots and press **P** (rebind under Controls > Armor Dye). A panel shows that item's icon in every
  color (10 colors, 5 hue shifts, Remove dye); click one to dye it. P again or right-click closes it.
- **Shows everywhere**: on your character (armor and the item in your hand), on its projectiles (arrows, chakrams, rockets; particle effects are tinted), in the character and
  vanity previews, and on the item icons in every slot, chests included. Other players see your dyes.
- **Vanity**: a dyed vanity piece shows its own dye; an undyed vanity piece shows the dye of the
  armor equipped under it.

Chat commands (any color, also what the bucket sends):
`/dye <helm|chest|pants|all|hand> <red|orange|gold|green|teal|blue|purple|pink|white|black|#rrggbb|shift <degrees>|off>`,
`/dye info`.

Dyeable: armor, and single non-stacking items with durability (weapons, tools, fishing rods).

Glowing parts are dyed too. Settings (Mod Options > Armor Dye): dye weapons in hand, dye projectiles,
dye item icons (all on by default, client-side only). Limit: off-hand items keep their vanilla look.

How it works: the color is stored in a vanilla per-item data slot the game only uses on live cattle
(`MealsEatenCD`), so it is saved and synced like a pet's color; every client recolors the armor and
held-item sheets and icons from it. Details in SPEC.md.

Requires **CoreLib** and **Mod Options**. Client + server (`requiredOn: 3`).
