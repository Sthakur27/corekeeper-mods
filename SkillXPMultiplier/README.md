# Skill XP & Talent Points

![Skill XP & Talent Points — in-game screenshot cover](../release/skillxp-logo.png)

Make slow skills level faster, or turn them down if you want a longer grind. You can set each skill separately and give yourself more talent points, including enough to fill the whole tree. Pets can get more talent points too.

## XP rates

Open **Settings > Mod Options > Skill XP & Talent Points** to change a skill's XP multiplier. Everything starts at **1x**, the normal rate.

- **0x** stops that skill earning XP. You keep the levels you've already earned.
- **0.25x–0.75x** makes it level more slowly.
- **1.5x–100x** makes it level faster. Pick one of the preset rates in the menu.

This covers all twelve skills: Mining, Running, Melee, Vitality, Crafting, Range, Gardening, Fishing, Cooking, Magic, Summoning, and Explosives. Changes take effect on the next XP you earn and save automatically. They don't directly change your level.

## More talent points

**Talent points at level 100** defaults to **40**, enough to fill every talent in a skill tree. You can also pick 30, 35, or **Vanilla** for the usual 25.

The extra points are spread across your levels. With the setting at 40, for example, a level 50 skill gets 20 points. If you raise the setting after you've already leveled up, you get the extra points straight away.

If you lower it below the number you've spent, that skill's tree resets for free the next time you open it. You can then spend your points again. Choosing Vanilla restores the game's usual point progression.

**Pet talent points at level 10** defaults to **9**, one for every talent slot. You can choose 6, 7, or 8 instead, or **Vanilla** for the normal one point every two levels (5 at level 10). If you lower the setting and your pet has too many points spent, use the reset button in its talent window.

## Playing with friends

The host chooses how settings work under **Multiplayer (when hosting)**:

- **Host decides** is the default. Everyone uses the host's XP rates and talent point settings, including pet points.
- **Each player picks** lets everyone use their own settings.

In your own world, you're the host. Players who don't have the mod still get the host's XP rates, but keep the normal talent point progression.

## Chat commands

You can check your rates with `/xp`. The host can also change them through chat:

- `/xp mining 20` sets Mining to 20x.
- `/xp all 1` puts every skill back to the normal rate.

Commands use the nearest supported rate. If someone joining your world tries to change a value, they'll get a note that only the host can do that.

## Installing

Requires **CoreLib** and **Mod Options**. Restart Core Keeper after installing.

If you've enabled CoreLib's command security, allow `xpsync` for players so the settings can sync. That syncing doesn't post messages in chat.

Avoid running other mods that change XP or talent points at the same time; the changes may stack or overwrite each other.
