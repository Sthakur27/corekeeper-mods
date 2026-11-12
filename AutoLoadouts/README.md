# Auto Loadout Switch

Link an item to a loadout and the game switches to that loadout whenever you start holding it:
pickaxe -> mining gear, sword -> melee gear, bow -> ranged gear.

- **Link:** hold the item, pick the loadout you want for it, press **Ctrl+L** (rebind under Controls,
  "Auto Loadout Switch"). "Linked to loadout II" pops up above your character in the game's own
  speech-bubble text, and the item's tooltip shows "Switches to loadout II when held".
- **Unlink:** press the key again while holding the item in the same loadout.
- Links are per exact item and per character, saved on your PC (`AutoLoadouts/bindings.cfg`).
- The switch only happens at the moment you change the item in your hand. Choose another loadout by hand
  and it stays until you next switch to a different linked item. Unlinked items (food, blocks, torches)
  never change your loadout.
- Works with Five Loadouts and Shared Loadout Gear & Vanity. Client side: other players don't need it.
