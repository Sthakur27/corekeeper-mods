# Enchanting (working title) — design spec

Status: prototype 0.1.0 built 2026-11-03, not yet tested in game. Numbers marked *tune* are first
guesses meant to go into Mod Options.

## Prototype 0.1.0 deviations from the plan below

- **Bench**: the Octarine Workbench recipe grid is full (18/18) and vanilla only shows recipes inside its fixed
  6-slot tab windows, so the table sits in the **Galaxite Workbench** stations category (slot 11, empty in
  vanilla). The recipe still costs 20 Octarine Bars + 15 Gold Bars. `EnchantRecipe.Bench/FirstSlot`.
  Sid confirmed this placement (2026-11-03).
- **Cost source**: Gold Bars / Ancient Coins are counted and taken from the player's own inventory only
  (no nearby chests yet). Creative (god mode) characters roll free.
- **Looks**: in the world the table is the vanilla Upgrade Forge sprite (shared graphics); only the item icon
  is the violet recolor. A real skin comes later.
- **Debug**: `/enchant` (show), `list`, `set <key> <lvl>`, `remove <key>`, `clear`, `roll [n]`, `cost`, each with
  an optional trailing `helm|chest|pants` (default: held item). Changing commands need admin privileges.

## Player-facing summary

- New station: **Enchanting Table**, crafted at the **Octarine Workbench**.
- Put a weapon, tool or armor piece in the table's slot, pay **Gold Bars + Ancient Coins**,
  press the button: the item gets a **random enchant** from the pool for its item type.
- Enchants are per item (two identical swords can differ), survive save/reload and upgrades,
  and work in multiplayer.
- Weapon/tool/rod enchants apply while the item is **held**; armor enchants while **equipped**.

## Station

- Recipe (Octarine Workbench, empty recipe slot found at load like SolariteDrill):
  20 Octarine Bar + 15 Gold Bar. *tune*
- Prefab: copy of the vanilla **Upgrade Forge** (`ObjectID.UpgradeForge` 4046) — same vanilla
  MonoBehaviours, its 1-slot inventory and `UpgradeForgeUI` (item slot, materials list, button,
  preview slot). Own name, icon and sprites (recolor of the forge; art pass later).
- Only vanilla MonoBehaviours in the prefab (bundle MonoScripts bind by assembly name; breaks in the
  Overhaul otherwise). Behaviour is redirected with Harmony when the open building is ours.

## Cost per roll *tune*

`gold = 5 × (1 + enchants already on item)`, `coins = 250 × (1 + enchants already on item)`

| Enchants already on item | Gold Bars | Ancient Coins |
|---|---|---|
| 0 | 5 | 250 |
| 1 | 10 | 500 |
| 2 | 15 | 750 |

Materials are taken from the player's inventory and nearby chests, like vanilla crafting
(`CraftingHandler.GetNearbyChests`). Shown in the vanilla materials list under the button.

## Roll rules *tune*

- Max **3 enchants** per item, each level **I–III**.
- A roll picks a random enchant from the item's pool (equal weights):
  - not on the item yet → added at level I (60%), II (30%), III (10%);
  - already on the item → +1 level (capped at III; a capped roll re-picks).
- Item already full (3 enchants): the roll **replaces one random enchant** with a new one
  (gamble to fish for a better set). *Alternative to decide: full items cannot be rolled.*
- The roll happens on the server only.

## Enchant pools (v1)

Values are per level (I / II / III). Condition value units still have to be checked against
vanilla items (e.g. a ring's "+X% melee damage" value) before numbers are final.

### Melee weapons (`MeleeWeapon`)
| Enchant | Condition | I / II / III *tune* |
|---|---|---|
| Sharpness | `PhysicalMeleeDamageIncrease` | 5 / 10 / 15 % |
| Fire Aspect | `ApplyBurning` | burn on hit |
| Knockback | `ChanceOnHitToKnockback` | 10 / 20 / 30 % |
| Lifesteal | `LifeOnHit` | 2 / 4 / 6 hp |
| Venom | `ChanceToApplyPoisoned` | 10 / 20 / 30 % |
| Stunning | `ChanceOnMeleeHitToStun` | 5 / 10 / 15 % |
| Swiftblade | `MeleeAttackSpeedIncrease` | 5 / 10 / 15 % |
| Keen | `CritChance` | 3 / 6 / 9 % |
| Bossbane | `IncreasedDamageAgainstBosses` | 5 / 10 / 15 % |

### Ranged weapons (`RangeWeapon`)
| Enchant | Condition | I / II / III *tune* |
|---|---|---|
| Power | `PhysicalRangeDamageIncrease` | 5 / 10 / 15 % |
| Quick Draw | `RangeAttackSpeedIncrease` | 5 / 10 / 15 % |
| Frost | `ChanceOnRangeShotToApplySlowedBySlime` | 10 / 20 / 30 % |
| Stunshot | `ChanceOnRangeShotToStun` | 5 / 10 / 15 % |
| Keen | `CritChance` | 3 / 6 / 9 % |
| Bossbane | `IncreasedDamageAgainstBosses` | 5 / 10 / 15 % |

### Tools (`MiningPick`, `Shovel`, `Sledge`, `DrillTool`, `Hoe`)
| Enchant | Condition | I / II / III *tune* |
|---|---|---|
| Efficiency | `MiningIncrease` (mining damage) | 10 / 20 / 30 % |
| Unbreaking | `ToolDurabilityLastsLonger` | 15 / 30 / 45 % |
| Fortune | `ChanceForRandomLootFromWall` | 2 / 4 / 6 % |
| Mending | `DurabilityIncreaseAfterMiningOre` | small / med / large |

### Armor (`Helm`, `BreastArmor`, `PantsArmor`)
| Enchant | Condition | I / II / III *tune* |
|---|---|---|
| Protection | `ArmorIncrease` | 2 / 4 / 6 |
| Thorns | `ThornsDamage` | 10 / 20 / 30 |
| Vitality | `IncreasedMaxHealth` | 10 / 20 / 30 hp |
| Regeneration | `IncreasedHealthRegenEffectiveness` | 10 / 20 / 30 % |
| Evasion | `DodgeChance` | 2 / 4 / 6 % |
| Bosswarden | `ReducedDamageFromBosses` | 3 / 6 / 9 % |
| Unbreaking | `EquipmentDurabilityLastsLonger` | 15 / 30 / 45 % |
| Swiftness (pants only) | `MovementSpeedIncrease` | 3 / 6 / 9 % |

Removed 2026-11-04 (Sid): Lure, Luck of the Sea, Reach (so fishing rods take no enchants), Magnetism, Prospector.

Not in v1: fishing rods, rings, necklaces, offhands, magic/summon/beam weapons, Minecraft enchants that need
Burst-only logic (Multishot, Piercing, Infinity, Silk Touch, Looting, Loyalty, Vanishing).

## How it works (implementation)

- **Storage**: vanilla aux data `PetTalentBuffer` on the item (list of `(id, points)`, `[GhostField]`,
  saved generically). Encoding: `petTalentID = 1000 + enchantId` (out of vanilla talent range),
  `points = level`. Written server-side via
  `InventoryAuxDataSystemDataCD.SetOrAllocate…` like ArmorDye's `MealsEatenCD`, so it coexists with dyes.
  Vanilla only reads `PetTalentBuffer` for pets (PetExtensions, PetInitSystem, PetTalentsWindow,
  SummarizeConditions pet slot) — **must verify** nothing chokes on unknown talent IDs.
- **Effects**: server system (`PugSimulationSystemBase`, server world) each tick reads the held item and
  the equipped armor, sums enchant conditions per `ConditionID`, and keeps one entry per ID in the
  player's `ConditionsBuffer` (refreshing `removeTick`; removing entries when the item changes).
  `SummarizeConditionsSystem` (Burst, unpatched) then folds them into stats, and NetCode replicates
  `ConditionsBuffer` to clients.
- **Enchant button**: Harmony on `UpgradeForgeButton.GetRequiredMaterials` / `ShouldBeActive` and
  `UpgradeForgeUI.Upgrade` / `LateUpdate` when the open building is the Enchanting Table: show our cost,
  hide the level-up preview (or show the enchanted preview), send a CoreLib server command
  (`/enchant roll`, like ArmorDye's `DyeCommand`) instead of the vanilla Upgrade craft action.
- **Server roll**: validate the player has the table open/in range and the cost (inventory + nearby
  chests), consume materials, roll, write aux data.
- **UI**: item tooltip lines ("Sharpness II") via a hover-stats patch; hide our entries from the buff
  bar (or show one "Enchanted" icon) via a UI patch.
- Mod Options: costs, max enchants, level weights, full-item rule.
- requiredOn: 3 (server rolls + applies; clients need tooltips and the station prefab).

## Risks to check first (prototype order)

1. `PetTalentBuffer` on a sword: allocate, save/reload, no pet-system errors.
2. Permanent `ConditionsBuffer` entry: stat actually changes in the character window; survives
   death/respawn handling (`ConditionsSystem` shortens timers on death unless `isPermanent`).
3. Condition value units per ID (percent vs. tenths) against vanilla items.
4. Upgrade Forge prefab copy: UI opens, slot works, our Harmony redirect fires.
5. On-hit conditions (`ApplyBurning` etc.) trigger from conditions on the player, not only from gear.
