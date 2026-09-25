# Bigger Watering Cans

Core Keeper mod. The **Watering Can** waters a **2x2** area and the **Iron Watering Can** waters a
**4x4** area (vanilla areas are smaller). Nothing else changes: water per pour, refill, cooldown and the
"chance to not consume water" condition are all vanilla.

No settings. Required on client and server (`requiredOn: 3`).

## How it works

A watering can's area is just the can's **prefab tile size** (`ObjectInfo.prefabTileSize`, baked into
the `PugDatabase` blob as `EntityObjectInfo.prefabTileSize`). `WaterCanSlot.PlaceItem` waters every
`dugUpGround` tile in `prefabTileSize.x * prefabTileSize.y` tiles starting at
`PlacementCD.bestPositionToPlaceAt`, and `PlacementHandler.GetCurrentSize` uses the same value for the
aim preview. The mod changes that one number for `ObjectID.WaterCan` (2) and `ObjectID.LargeWaterCan` (4)
in three idempotent places, so it is right no matter in which order the game converts prefabs:

1. `API.Authoring.OnObjectTypeAdded`: edits the prefab's `EntityMonoBehaviourData.objectInfo` in place
   (the same instance `PugDatabase.objectsByType` holds), before `PugDatabasePostConverter` bakes the blob.
2. Harmony postfix on `ObjectAuthoring.ObjectAuthoringToObjectInfo` for SDK-style prefabs (they rebuild
   their `ObjectInfo` on every read).
3. `WateringCanSizeSystem` (client + server, managed `PugSimulationSystemBase`): on first update writes the
   sizes straight into the database blob if they are still vanilla, and once per player resets a stale
   saved size-variation (see below).

## Area anchor / preview

Even sizes need no special anchoring: the engine already supports them (it is the same code path as a
resizable hoe). With the mouse, the top-left corner is `round(mouse - (size-1)/2)`, so a 2x2 or 4x4
area is centred on the cursor and snaps by whole tiles; the preview rectangle and the watered tiles both
come from `PlacementCD.bestPositionToPlaceAt`, so what you see is what gets watered. With a controller /
facing direction the area is placed in front of the player like any other tool.

## Rotate key

If the can prefab carries `ResizableTileSizeCD` (the hoe/shovel do; the vanilla cans should too since
`WaterCanSlot` handles the rotate key), pressing **rotate** cycles the area 1x1 -> 2x2 (-> 3x3 -> 4x4 for
the iron can). The chosen size is stored per character (`PlacementSizeByEquipmentTypeBuffer[1]`); the mod
resets it to full size once per world load so a character that used a vanilla can does not get stuck on
the old size.

## Limits

- The two ObjectIDs are hard-coded (`CanSizes.cs`). No other cans exist in vanilla.
- The water-splash effect is a single effect at the pour position; it does not grow with the area.
- Vanilla prefab sizes are not readable offline; the mod logs the size it found in the blob
  (`[BiggerWateringCans] ... blob size ...`) in Player.log.
