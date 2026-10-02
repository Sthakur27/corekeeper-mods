# Infinite Ore Boulders

Core Keeper mod: ore boulders never break. Keep mining one and it keeps dropping ore at the normal
rate, forever. Works for every ore boulder type (copper to pandorium and relucite). No settings.

Server side (the host decides in multiplayer); client + server (`requiredOn: 3`).

## How it works (for modders)

Vanilla boulders drop one ore each time a hit takes their health across another multiple of
`DropsLootWhenDamagedCD.damageToDealToDropLoot`, and are destroyed at 0 health.
`InfiniteOreBouldersSystem` runs on the server right before `UpdateHealthFromBufferSystem`: it refills
every ore boulder (ObjectID ending in `OreBoulder`) to full health and caps that tick's queued
`HealthChangeBuffer` damage so it cannot reach 0. The game then applies the damage and drops ore as
usual. Written from the game's own code.
