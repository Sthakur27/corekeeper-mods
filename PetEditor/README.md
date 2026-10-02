# Pet Editor

Core Keeper mod: edit your equipped pet right in the vanilla pet talent window.

- **Level**: a "Level X/10" row with - and + buttons under the reset button.
- **Any talent in any slot**: right-click a talent slot and the talent tree turns into a grid of every
  pet talent in the game (hover for the usual description). Left-click one to put it in that slot;
  right-click to close the grid.
- **Free point removal**: Shift + left-click a talent that has a point to take the point back.

Placing points still works the vanilla way, and the number of points follows the game's own formula
(so mods that change it, like AllSkills, are respected). Lowering the level resets the points if the
pet would have more placed than it owns.

Chat commands (what the buttons send; work in multiplayer for each player's own pet):
`/pet level <1-10>`, `/pet talent <slot 1-9> <talent name or id>`, `/pet unpoint <slot 1-9>`.

Requires **CoreLib**. Client + server (`requiredOn: 3`).

Written from scratch against the game's own code (PetTalentsWindow, PetExtensions, inventory aux
data). Not based on any other pet mod.
