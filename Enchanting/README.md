# Enchanting (Alpha)

Put a piece of gear in an **Enchanting Table** and spend Gold Bars and Ancient Coins to give it a random enchantment. You can have up to three on an item, from extra damage on a sword to faster mining on a pickaxe.

**This is still an alpha.** It's been tested in single player only, some effects haven't been tried in a fight yet, and the balance will change.

## Using the table

Craft the table at a **Galaxite Workbench**, under stations, for **20 Octarine Bars + 15 Gold Bars**. Open it, put your gear in the slot, and press the **up arrow** to roll. It looks like an Upgrade Forge with a violet glow and uses the same window.

The first enchantment costs **5 Gold Bars + 250 Ancient Coins**, the second costs **10 + 500**, and the third costs **15 + 750**.

New enchantments usually start at level I: the chances are 60% for I, 30% for II, and 10% for III. **Figurines** placed within 5 tiles of the table improve those odds. Any enemy or boss figurine counts, up to 9. With 9 the chances are 0% for I, 40% for II, and 60% for III, and the roll message tells you how many the table sees. If you roll an enchantment the item already has, it goes up a level. Once an item has three, rolling a new one replaces a random enchantment, so you might lose one you wanted to keep.

The **red down arrow** removes the top enchantment (the one you rolled or upgraded most recently) for free. You don't get the bars or coins back.

Enchanted items have a violet border in your inventory. Hover over one to see what its enchantments do. They stay on the item when you upgrade or dye it.

## What you can enchant

Weapons and tools need to be held for their enchantments to work. Armor, rings, necklaces, and offhands need to be worn. For summoning tomes, the last tome you held keeps working as long as it's still in your inventory.

Fishing rods, bags, lanterns, pouches, pets, bait, grenades, and other stackable items can't be enchanted.

What you can roll depends on the item. A sword might get Sharpness for more damage, a pickaxe might get Efficiency for faster mining, or a ring might get Precision for extra crit chance. Check the [full enchantment list](https://github.com/Sthakur27/corekeeper-mods/blob/main/Enchanting/ENCHANTMENTS.md) for every option and its numbers.

Thunderstrike, Piercing, Flame, Static Familiars, and Soul Siphon are **experimental and haven't been tested in game yet**. Thunderstrike needs the Soul of Azeos. Piercing won't roll on beam weapons. Flame on bows and staffs depends on the game passing the burn to arrows and bolts; beams always burn.

## Stacking enchantments

Accessory enchantments are small, and they add up: two rings with Precision III give you +6% crit chance.

There are also limits across all your gear: 12% less damage taken, 15% extra max health, 8 extra dodge, 12% extra movement speed, 60% extra regeneration, and 9% less boss damage. The total reduction in damage taken from enchantments is capped at 25%. Once you hit a limit, adding more of that bonus won't help.

## Before you install

Requires **CoreLib**. Multiplayer hasn't been tested.

The table, its costs and buttons, tooltips, and violet borders have been tested in game. Melee, bow, and armor enchantments have been checked against the game's stats, but that doesn't mean every effect has been confirmed in combat. Knockback, Frost, Frostbite, Bossbane, and Thorns still need that check. Blast Protection only helps against your own and your allies' explosives.

The magic, summoning, and accessory enchantments, along with the newer damage multipliers and level scaling, still need more testing. Some have only passed automated stat checks; the experimental ones haven't been run in game yet.

**Turning the mod off or removing it can leave the last active bonuses on your character.** The enchantment tooltips disappear and you keep your items, but uninstalling may not clear the bonuses you already had.

For admin commands, testing tools, and build notes, see the [developer notes](https://github.com/Sthakur27/corekeeper-mods/blob/main/Enchanting/DEVELOPER.md).
