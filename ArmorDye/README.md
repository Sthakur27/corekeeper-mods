# Armor Dye

Core Keeper mod: dye your armor, weapons and tools any color. The dye belongs to the item, so two
copies of the same chestplate can be different colors, and a piece keeps its color when you move,
store, trade or upgrade it.

- **Dye bucket**: in the character window, in the free cell under the vanity pants slot. Left-click
  it to open the palette (10 colors, 5 hue shifts, Remove dye), pick a color, then click armor, a
  weapon or a tool in your inventory, hotbar, equipment or vanity slots to dye it. Right-click the
  bucket to put it away (clicks work normally again). The picked color stays until you put it away.
- **Shows everywhere**: on your character (armor and the item in your hand), in the character and
  vanity previews, and on the item icons in every slot, chests included. Other players see your dyes.
- **Vanity**: a dyed vanity piece shows its own dye; an undyed vanity piece shows the dye of the
  armor equipped under it.

Chat commands (any color, also what the bucket sends):
`/dye <helm|chest|pants|all|hand> <red|orange|gold|green|teal|blue|purple|pink|white|black|#rrggbb|shift <degrees>|off>`,
`/dye info`.

Dyeable: armor, and single non-stacking items with durability (weapons, tools, fishing rods).

Glowing parts are dyed too. Limits: off-hand items and thrown weapons in flight keep their vanilla look.

How it works: the color is stored in a vanilla per-item data slot the game only uses on live cattle
(`MealsEatenCD`), so it is saved and synced like a pet's color; every client recolors the armor and
held-item sheets and icons from it. Details in SPEC.md.

Requires **CoreLib**. Client + server (`requiredOn: 3`).
