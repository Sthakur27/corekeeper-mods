Summary (<=250):
Set a separate XP multiplier for each of the 12 skills, from 0x (frozen) to 100x. Change it in Mod Settings or with /xp in chat. Applies instantly, saved between sessions.

Description:
Choose an XP multiplier PER SKILL: Mining, Running, Melee, Vitality, Crafting, Range, Gardening, Fishing, Cooking, Magic, Summoning and Explosives each get their own value.

- Set them in the Mod Settings menu (requires the Mod Settings Menu mod). Each skill cycles through 0x, 0.25x, 0.5x, 0.75x, 1x, 1.5x, 2x, 3x, 4x, 5x, 6x, 8x, 10x, 12x, 15x, 20x, 25x, 30x, 40x, 50x, 75x and 100x. Left/right arrow steps down/up.
- 0x freezes a skill, 1x is vanilla. Everything starts at 1x.
- Chat: /xp shows all values, /xp mining 20 sets one skill, /xp all 5 sets every skill.
- Changes apply instantly and are saved between sessions.

How it works: every XP gain in the game is a pending grant that the game's own skill system consumes. This mod scales the amount of that grant right before the game applies it, so only the XP you are actually earning is multiplied, fractional multipliers work, and nothing is counted twice or lags behind.

Requires CoreLib and Mod Settings Menu. Do not run together with other XP multiplier mods; they would stack on top of this one.

Changelog 1.0.0:
- Initial release
