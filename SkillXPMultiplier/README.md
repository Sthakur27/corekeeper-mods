# Skill XP & Talent Points

![Skill XP & Talent Points — in-game screenshot cover](../release/skillxp-logo.png)

Choose how quickly each skill levels up and how many talent points each skill gives you. Speed up the skills you find slow, slow down others, pause XP gains entirely, or raise the talent point total so you can fill more of each tree.

## Set your pace

Open **Settings > Mod Options > Skill XP & Talent Points** and choose a multiplier for each skill. All skills start at **1x**, the normal game rate.

- **0x** pauses new XP for that skill; it does not remove earned levels.
- **0.25x–0.75x** slow progress down.
- **1.5x–100x** speed it up, using the menu's preset choices.

Supports Mining, Running, Melee, Vitality, Crafting, Range, Gardening, Fishing, Cooking, Magic, Summoning and Explosives. Changes apply to newly earned XP immediately and save automatically.

## Talent points

**Talent points at level 100** sets how many talent points every skill has in total once it reaches level 100. The points are spread evenly over the levels: with 40, a level 50 skill has 20 points. The default is **40**, enough to fill every talent in a tree. **Vanilla** keeps the normal game curve of 25 points.

- Raising the number gives you the extra points right away for levels you have already earned.
- Lowering it (or going back to Vanilla) below what you have already spent resets that skill's talent tree for free the next time you open it, so you can place the points again.
- Choices are **Vanilla**, 30, 35 and 40: a tree has 8 talents of 5 points each, so 40 fills it completely.

**Pet talent points at level 10** does the same for pets. The default is **9**, one for every talent slot, so a fully leveled pet can fill all of them. **Vanilla** keeps the normal 1 point every 2 levels (5 at level 10); the other choices are 6, 7 and 8. If a lower setting leaves a pet with more points placed than it has, use the reset button in its talent window.

## Chat shortcuts

- `/xp` — show every multiplier.
- `/xp mining 20` — set Mining to 20x.
- `/xp all 1` — restore every skill to the normal rate.

Only the host can change values with `/xp`.

Commands choose the nearest supported multiplier. This changes XP earned from playing; it does not directly set skill levels.

## Multiplayer

The host picks **Multiplayer (when hosting)**:

- **Host decides** (default): the host's XP multipliers, talent points and pet talent points apply to every player. Other players' own settings are ignored while they are on that world.
- **Each player picks**: every player uses the multipliers and talent points from their own menu.

Only the host can use `/xp` to change values; other players get a short note instead. On your own world you are the host, so your settings always apply. Players without the mod use the host's XP multipliers and the normal talent curve.

## Requirements and compatibility

Requires **CoreLib** and **Mod Options**. Install, then restart Core Keeper. Settings sync over CoreLib's chat-command channel without showing in chat. If CoreLib command security is turned on, allow `xpsync` for players.

Don't run it alongside other mods that change skill XP or talent points: their effects can stack with this one or override it.
