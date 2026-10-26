# Live checks (pending)

0. Confirm the name Mushroom Slayer in the loader and Mod Options; set threshold 500 and percentage 25, verify 499 → 1 and 1200 → 300, then reset to 200/10 and verify 1200 → 120. Restart to verify persistence. Use matching settings on host and client. Check the standalone page and the bundled Overhaul page separately.

1. Restart with Mushroom Slayer on both host and client; confirm loader compile success and `[MushroomSlayer]` log, no ECS ordering warnings.
2. Against gray Shrooman Brutes, confirm resolved normal hits of 199/200/209/2000 produce 1/20/20/200 health loss and matching numbers. Use crit-disabled equipment or account for crits.
3. A fresh 810-HP Hard brute takes five 2000-normal-damage hits: 610, 410, 210, 10, dead. Verify loot, kill credit, and no premature death on the first hit.
4. Repeat melee, ranged, magic, crits, simultaneous hits, and high attack speed. Verify rings with more than three events do not swap damage numbers.
5. Bait a wall collision; verify full vanilla damage while red and scaling after returning gray. Compare another enemy with mod disabled/enabled.
6. Check miss/immunity/zero hits do not cause damage; healing, armor-bypass and creative kills remain unchanged. Pets retain vanilla caps.
7. Repeat host and remote-client tests; verify health sync and no duplicate predicted damage. Save/reload and overhaul feature-off should restore vanilla without save migration.
