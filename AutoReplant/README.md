# Auto Replant (Core Keeper mod)

Harvest a ripe crop and the same crop is planted back into that tile, using one seed of that
plant from your inventory. No seed = no replant (vanilla behaviour). Every auto-replant has a
configurable chance to come up **golden**.

- The seed the harvest just dropped counts: as soon as it is pulled into your inventory it is
  used. If you already carry seeds of that plant, one of those is used instead (toggleable).
- Gardening XP is unchanged: vanilla grants Gardening XP when you **harvest**, never when you
  plant, and this mod leaves the harvest untouched. Replanting through the vanilla placement
  action means nothing else changes either (growth stages, watering, seeders, harvesters all
  see a normally planted seed).
- Only player harvests trigger it (a plant you hit while it is ripe). Automated harvesters and
  unripe plants you destroy are ignored. Root-type plants (Root/Coral Root) replant when the
  plant is finally destroyed, since they are harvested by damage.
- Multiplayer safe: runs in the server world; the harvesting player's own inventory is used.

## Settings (Mod Settings Menu)

| Setting | Default | Meaning |
|---|---|---|
| Auto replant | on | Master switch. Off = vanilla. |
| Golden plant chance (%) | 5 | Chance an auto-replant is the golden variant. Replaces vanilla's 3% base roll; your Gardening "rare plant" talent bonus still adds on top, exactly like vanilla. |
| Use seeds from inventory | on | On: any seed of that plant you carry may be used. Off: only the seed the harvest itself dropped (if any) is used. |

Settings apply live and persist via CoreLib config (`AutoReplant/config.cfg`).

## How it works

Server system `AutoReplantSystem` (managed `PugSimulationSystemBase`, no Harmony):

1. **Detect the harvest.** A harvested plant is an entity with `PlantCD` + `GrowingCD` whose
   `EntityDestroyedCD` is enabled and whose `KilledByPlayerCD.playerEntity` is set (the game's
   attack code sets both). Only plants at their final growth stage are considered.
2. **Find the seed.** At startup the mod scans `PugDatabase` for every seed prefab (an object with
   `GrowingCD` whose properties name the plant it grows into, property `-1534320058`) and builds
   a plant -> seed map, together with the seed's golden variation index (property `1273594437`).
   No hardcoded item list, so modded/new crops work as long as they follow the vanilla pattern.
3. **Wait for the tile to clear** (the old plant entity is gone; otherwise the game's
   `DestroyEntityIfPlacementNotValidSystem` would delete the new seed) and for a seed to be
   available in the player's `ContainedObjectsBuffer` (up to 6 s, which covers the loot pull).
4. **Plant it the vanilla way.** Enqueue
   `Create.ConsumeEntityAt(player, slot, 1, destroy:false, dontConsume:false, tilePos, variation)`
   on the `InventoryChangeBuffer`, which is byte-for-byte what `PlaceObjectSlot.PlaceItem` and the
   seeder (`SeederSlot`) enqueue when you plant by hand. The game's `InventoryUpdateSystem` then
   removes one seed from that slot and instantiates the seed prefab on the tile.
5. **Golden roll.** Vanilla computes `3% + ChanceToGainRarePlant` and passes the golden variation
   to the same call; this mod does the same with the configured base chance. Golden seeds grow
   into the golden plant variation and drop the golden ("Rare") crop as usual.

## Limits

- Seeds must be in the player's own inventory/hotbar buffer. Seeds stored in a bag are not seen.
- If the inventory is full the dropped seed never arrives and nothing is replanted (as vanilla).
- Chained harvests are handled per tile; each replant uses exactly one seed.

Install: run `..\install.bat AutoReplant` (copies into `CoreKeeper_Data\StreamingAssets\Mods\AutoReplant`),
restart the game. Requires CoreLib and ModSettingsMenu. Check
`%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log` for `Successfully compiled AutoReplant`,
`[AutoReplant] Loaded.` and `[AutoReplant] seed map built: N plants.`


## 1.1.0
- New toggle **Override golden chance** (default off). Off = the golden roll is exactly vanilla (3% base + your Gardening rare-plant bonus). On = the "Golden plant chance (%)" value replaces the 3% base; the bonus still adds on top.
