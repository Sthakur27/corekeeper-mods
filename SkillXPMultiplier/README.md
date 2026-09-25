# Skill XP Multiplier (Core Keeper mod)

Choose an XP multiplier **per skill**: Mining, Running, Melee, Vitality, Crafting, Range,
Gardening, Fishing, Cooking, Magic, Summoning, Explosives.

- Set them in **Mod Settings** (needs the Mod Settings Menu mod). Each skill cycles through
  0x, 0.25x, 0.5x, 0.75x, 1x, 1.5x, 2x, 3x, 4x, 5x, 6x, 8x, 10x, 12x, 15x, 20x, 25x, 30x, 40x, 50x, 75x, 100x.
  0x freezes a skill; 1x is vanilla. Default is 1x for every skill.
- Or in chat: `/xp` shows all, `/xp mining 20` sets one, `/xp all 1` sets every skill.
  Values snap to the nearest choice above.
- Changes apply instantly and are saved in CoreLib's config (`SkillXPMultiplier/config.cfg`).

How it works: every XP grant in the game is an `AddSkillValueCD` entity consumed by the game's
`AddSkillValueSystem`. This mod scales the amount on those entities one system earlier, so only
the XP actually being earned is multiplied, fractional multipliers work, and nothing is counted twice.

Install: run `..\install.bat SkillXPMultiplier` (copies into
`CoreKeeper_Data\StreamingAssets\Mods\SkillXPMultiplier`), then restart the game.
Requires CoreLib and ModSettingsMenu. **Disable the mod.io "XP Multiplier" mod**: it multiplies
any XP it sees land, including this mod's, so both together would stack (x10 on top).
Check `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` for
`Successfully compiled SkillXPMultiplier` and `[SkillXPMultiplier] Loaded. Multipliers: ...`.
