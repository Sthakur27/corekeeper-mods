# In-world checks

These are manual acceptance checks, not claims of completed gameplay testing.

- Defeat Omoroth repeatedly. Each death gets one independent 20% roll for one mirror dropped at the boss position; misses must not reroll during the death animation. Normal boss loot remains intact. Check multiplayer produces one shared roll, and other bosses/tentacles do not drop mirrors. A short run is not expected to hit exactly 20%.

- At an existing and a freshly placed Solarite Workbench, find Magic Mirror in Gear; recipe shows exactly 50 Solarite Bars and produces one mirror. Verify 49 bars cannot craft it.
- Leave the Solarite Gear tab open for at least one minute. The icon must remain visible across periodic recipe refreshes; the log must not continuously report recipe re-registration. Repeat on a higher-tier bench.
- Check the Solarite category of a higher-tier workbench. Other workbench recipes must remain unchanged.
- Equip the mirror away from the Core. Cast for three seconds, finish the normal teleport sequence, and verify return to the Core with the same mirror in the same inventory slot.
- Use it three times; verify no consumption, duplication, durability loss, or extra ingredient cost.
- Move during casting; swap the held item during casting and immediately before completion. Verify cancellation without teleport or inventory changes.
- Take damage during casting; compare the cast behavior with a Recall Idol.
- Use a Recall Idol and verify it still consumes one idol.
- Save/reload with a mirror in inventory and another in a chest; check identity, name and icon.
- Repeat crafting and use as a remote multiplayer client and as the host. Verify the client sees the correct cast, teleport, recipe and unchanged item count.
- With the Overhaul build, verify Ender Chest art/name/behavior still load. Switch Magic Mirror off, restart, and attempt to use an existing mirror: the cast should cancel safely. Re-enable and restart to restore use.
- Inspect Player.log for MagicMirror errors, player-state ordering warnings, unknown casting IDs, safety-check failures, and duplicate data-block loader warnings.
