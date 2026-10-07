# Armor Dye: spec

Status: 0.3.0 (2026-10-07). Goal: dye individual armor pieces (helm, chest, pants), weapons and tools
any color. The dye belongs to the item, so two copies of the same armor can have different colors.

## Requirements

1. The dye is stored on the **item**, not the character: it follows the piece through moving,
   equipping, chests, dropping, trading and upgrading.
2. The dye shows on whatever armor the character is **displaying**: the vanity item when one is
   set, otherwise the equipped item (same rule as vanilla `PlayerController.UpdateGearCustomization`).
3. It works in multiplayer: every player sees everyone's dyes.
4. It works with Loadout Fallback's per-loadout vanity slots and FiveLoadouts.
5. Removing the dye restores the exact vanilla look. Uninstalling the mod leaves only an unused
   number on dyed items (no save corruption).

## Storage (findings from the decompiled game, 1.3.x)

- Per-item data in Core Keeper is "inventory aux data": `ContainedObjectsBuffer.auxDataIndex`
  points at entities created from baked ghost prefabs, one prefab per aux type
  (`InventoryAuxDataSystem`, `InventoryAuxDataSystemExtensions`). Aux entities are saved generically
  (`GetDataAsJson` / `SetDataFromJson`) and replicated by NetCode (pet skins show on other players' pets).
- A mod cannot add its own aux type: the prefabs and NetCode serializers are baked into the game.
- `ObjectDataCD.variation` is **not** free on armor: it is the upgrade level
  (`InventoryUtility.Upgrade` -> `SetVariation(level + 1)`).
- Chosen slot: vanilla `MealsEatenCD.Value` (int, `[GhostField]`, `[InventoryAuxDataComponent]`).
  The game only reads it on live cattle entities (`BreedStateSystem`, `ChaseStateSystem`); no UI or
  item code reads it from an item. Fallback if it turns out to be bundled with other data: `PetSkinCD`.
- Upgrading keeps the dye (`Upgrade` only rewrites `variation`; `auxDataIndex` is untouched).

### Encoding of `MealsEatenCD.Value`

| Value | Meaning |
|---|---|
| `0` | no dye (vanilla look) |
| `0x01RRGGBB` | **Colorize**: every pixel takes the hue/saturation of `#RRGGBB`, keeps its own lightness |
| `0x020000HH` + hue in low 9 bits | **Hue shift** by 0-359 degrees (keeps the armor's own color variety) |

Top byte = mode, so later modes (palettes, two-tone) can be added without breaking saved dyes.

## Writing (server)

Chat command (CoreLib command channel, runs on the server, same pattern as Pet Editor):

```
/dye <helm|chest|pants|all> <color>      color = red | #3080ff | 3080ff | shift 120 | off
/dye info                                 log the aux prefab layout + the slot's current dye
```

The server resolves the displayed slot from `VanitySlotsCD` (kept pointed at the active loadout by
Loadout Fallback) and `EquipmentCD`, checks the item has `EquipmentSkinCD`, then
`InventoryAuxDataSystemDataCD.SetOrAllocateComponentData<MealsEatenCD>(ref index, ...)` and writes
the (possibly new) `auxDataIndex` back into the slot.

## Rendering (every client)

- Armor layers are `PlayerController.helmSkin / breastArmorSkin / pantsArmorSkin`
  (`SpriteSheetSkin`); the shader reads the sheet from the material's `_ReplacementTex`.
- Vanilla gives skin/hair/eyes/shirt/pants a `ColorReplacer` (palette swap via
  `_colorReplaceTexture`) but never the armor layers.
- Prototype route = **CPU recolor**: a Harmony postfix on `PlayerController.ManagedLateUpdate`
  (runs for every player on every client) reads the displayed item's dye, makes a recolored copy
  of `SpriteSheetSkin.skin` (Blit to a RenderTexture, ReadPixels, recolor, cache by
  texture + dye) and sets it as the material's `_ReplacementTex`. It never calls `SetSkin`, so the
  game's Addressables bookkeeping is untouched; when the game re-applies the original texture the
  next frame puts the dyed one back.
- Probe: logs whether each layer's shader has `_colorReplaceTexture` (would allow the cheaper
  palette-swap route later).

## Weapons and tools (0.3.0)

- Dyeable = armor (`EquipmentSkinCD`) or a non-stacking item with `DurabilityCD` (`DyeColor.CanDye`).
  Stackables are excluded (per-item data would stop them stacking).
- The held item is `PlayerController.visuallyEquippedContainedObject` (set for every player); the game
  puts its sheet on one of the carryable `SpriteSheetSkin`s (swing, range, shield, big spear, big swing,
  drill, fishing rod, instrument), recolored the same way as armor. `/dye hand <color>` dyes the
  selected hotbar item (`EquippedObjectCD.equippedSlotIndex`).

## UI (0.2.0+)

- Dye bucket in the character window's free cell under the vanity pants slot (0.4.0). Left-click opens
  a palette panel left of the bucket (4x4 swatches: 10 colors, 5 hue shifts, Remove dye; drawn in
  front of the UI it covers, with a click-blocking background). Picking a swatch closes it. With a
  color picked, left-clicking a dyeable item in the player's own slots sends
  `/dye slot <absolute index> <color>` instead of the normal click. Right-click the bucket to turn
  dye mode off. Closing the inventory always turns dye mode off (so normal clicks/drags work when it
  reopens); the color is remembered and ringed in the palette. The bucket is dimmed while off.
- Item icons in every `InventorySlotUI` show their own dye (sprite region copied from the icon atlas).
- Character-window and vanity-window previews show the local player's dyes.

## Known limits (prototype)

- Emissive (glowing) parts keep their original color (the `_EmissiveTex` is not recolored).
- Crafting a new piece gives an undyed item (it's a new item).
- Off-hand items are not recolored yet (only the main-hand item).
- With Loadout Fallback's per-loadout vanity, the dye on the **equipped** piece recolors whatever
  look is displayed, including a vanity piece on top (seen in test 2026-10-07: Ninja/Soaring vanity
  chests took the equipped Scholar chest's dye). Sid is fine with this; leave it unless he asks.

## Test plan (one launch)

1. Player.log: `Successfully compiled ArmorDye`, `[ArmorDye]` probe lines (aux prefab layout,
   shader properties per layer).
2. `/dye chest red` -> chest turns red; `/dye chest shift 120`; `/dye chest off` -> vanilla.
3. Swap to an identical undyed chest -> vanilla; swap back -> red (dye is per item).
4. Upgrade the dyed piece -> still red.
5. Quit to menu and reload the world -> still red (save round trip).
6. Vanity: put a dyed piece in a vanity slot -> shows dyed; change loadout.
7. Multiplayer (later, with a friend): they see the dye.
