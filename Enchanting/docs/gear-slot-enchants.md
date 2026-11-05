# Enchants for the remaining gear slots (design, 2026-11-04)

Design only. Nothing here is implemented. Covers lantern, bag, rings (2 slots), necklace, offhand, pet and pouches.
Source roots as in `balance-model.md`: `dec/` = `...\c4e37e1d-...\scratchpad\dec` (files under `Pug.Other` unless noted),
`dec2/` = `...\2c248440-...\scratchpad\dec` (`CondExt/ConditionExtensions.cs` = vanilla per-level curves).
ConditionID numbers are enum indices (`Pug.Base/ConditionID.cs`); "→ eN" is the ConditionEffect index.
Units: every new condition needs the usual `/enchant stats` + tooltip check. Readers that divide by **1000** are
tenths of a percent (we store ×10); readers that divide by **100** are whole percent.

## TL;DR

1. **Accessories get non-combat enchants only.** Combat damage and defence stay on weapons and armor. The new
   slots get farming, fishing, cooking, crafting, light, mining loot, movement utility and pets.
   The one exception is the shield, whose enchants trigger only when you block.
2. **Every accessory enchant is "unique": only the highest instance counts** (`Stack = Max`). Two rings
   with Green Thumb III give +15 %, not +30 %. With 2 rings × 3 enchants, a player can hold 6 *different* ring
   enchants, but never a doubled one.
3. **Slot-exclusive pools.** Each enchant lives in exactly one slot type (rings share one pool), so the same
   stat cannot be collected on ring1 + ring2 + necklace + offhand + armor.
4. **Per-condition caps on our own total** for the few stats with no natural ceiling (hunger, cheat death,
   slime slow). This generalises the existing Resilience clamp in `EnchantEffectSystem.Collect`.
5. **Bag size and pet-slot enchants are not feasible** (sections 2 and 6). Pet bonuses go on the necklace instead.

## 1. Anti-stacking rule

### The problem in numbers
Today every enchant is `Sum` per ConditionID (`EnchantEffectSystem.AddItem`, `_flat[c] += value`). With 6 new worn
slots that each hold 3 enchants at level III, an enchant available everywhere could reach 6 × III. Example:
Swiftness III on pants + 2 rings + necklace + offhand + bag would give +54 % move speed, and `MovementSpeed` has
only a *lower* cap (`SummarizeConditionsSystem.cs:398`). Cheat Death at 6 × 6 % = 36 % makes you nearly unkillable
(each lethal hit rolls again, `PlayerController.cs:7104`).

### Recommended rule (three layers, all server-side in `Collect`)

| Layer | Rule | Applies to |
|---|---|---|
| A. Slot exclusivity | An enchant's `Slots` flag names one slot type. Ring1/ring2 share the `Ring` pool. | all new enchants |
| B. `Stack = Max` ("unique") | Per enchant id, take the **highest** value among all equipped items, not the sum. | every enchant in this doc. Armor keeps `Sum`, because it is 3 distinct pieces and was balanced per piece. |
| C. Caps on our total | After folding, clamp our contribution per ConditionID (table below). | the listed conditions |

Worst case under A+B: each stat reaches **one level-III value**. The total is a fixed list of numbers, independent
of how many rings you wear. Rings stay worth a second slot because you get 3 *more* different enchants.

Caps (our part only; vanilla gear and food come on top, as they do today for Resilience):

| Condition | Our cap | Why |
|---|---|---|
| `DrainLessHunger` (67 → e51) | 30 | `1 − v/100` on hunger drain (`HungerAndRunningSystem.cs:165`); 100 = never hungry |
| `CheatDeath` (108) | 6 | rolled on *every* lethal hit, no cooldown (`PlayerController.cs:7104`) |
| `ReducedImpactOfSlowedBySlime` (227 → e89) | 45 | `1 − v/100` (`EntityUtility.cs:1824`); vanilla clamps its own curve at 45 (`dec2 ConditionExtensions.cs:203`) |
| `MovementSpeedIncrease` (4) | 15 total (pants Swiftness 9 + others) | no upper cap in vanilla |
| `ReducedDamageTakenPercentage` | −25 (existing) | unchanged |

### Implementation sketch (fits the current system)
- `EnchantDef`: add `public EnchantStack Stack = EnchantStack.Sum;` (`Sum | Max`). New helper `Uniq(...)` = `Add` + `Stack = Max`.
- `EnchantMath`: replace the single `MinDamageTaken` clamp with a `Dictionary<ConditionID, (int min, int max)> Caps`.
- `Collect`: also `AddFrom(..., equipment.necklaceSlotIndex / ring1SlotIndex / ring2SlotIndex / offHandIndex /
  bagIndex / lanternIndex, heldSlot: false)`. All are on `EquipmentCD` (`dec/Pug.ECS.Components/EquipmentCD.cs`),
  the same indexes the vanilla summary reads (`SummarizeConditionsSystem.cs:226-243`).
- `AddItem`: accumulate first into `_perDef[def.Id]` (Max or Sum by `def.Stack`), then fold per condition
  (`_flat[cond] += perDef`), then apply `Caps`. Two different defs that write the same condition still sum
  (e.g. tool and armor Unbreaking use different conditions anyway).
- Tooltip: suffix "(unique: only the highest counts)" on `Max` enchants.
- No new sync: still plain `ConditionsBuffer` entries, same marker, same write throttle.

### New `EnchantSlot` bits and `SlotOf`
`Ring = 512, Necklace = 1024, Shield = 2048, OffhandOther = 4096, Bag = 8192, Lantern = 16384`.
`SlotOf`: `ObjectType.Ring` (104), `Necklace` (103), `Bag` (106), `Lantern` (107) map directly. For `Offhand` (105),
read `OffHandCD.mechanic` from the prefab (`dec/Pug.ECS.Components/OffHandCD.cs`, `Pug.Base/OffHandMechanic.cs`):
`Shield` → Shield pool, `Dash`/`Teleport`/`GrapplingHook`/detonators → OffhandOther pool, `Bait` or any
`isStackable` item → None (aux data is shared per stack, the same rule as grenades). `Pouch` (108) and `Pet` (802) → None.
These are worn slots: `IsHeldSlot` stays false for them.

## 2. Lantern: light and finding things (5)

Vanilla light: `ConditionsEffectsHandler.UpdateCombinedLight` (`:297-345`) sums each glow ConditionID (BlueGlow 1,
OrangeGlow 45, PinkGlow 217, GreenGlow 321, VoidGlow 355) and uses the **largest** per-colour value as the light range
(`CombineLightSources`, `:427-448`). A lantern's glow is `clamp(0.6·L, 2, 10)` (`dec2 ConditionExtensions.cs:197`).
This is client-side visuals only. Clients build their summary from the replicated `ConditionsBuffer`, so it works in multiplayer.

| Id | Enchant | Condition | I / II / III | Why it is safe |
|---|---|---|---|---|
| 140 | **Radiance** | the lantern's own glow ID (read from its level entity's `GivesConditionsWhenEquippedBuffer`; fallback OrangeGlow 45) | +2 / +3 / +4 light range | Visual only, no gameplay stat. Must use the **same colour** as the lantern, because ranges take the max across colours: another colour adds nothing unless it is larger. Reuse the `EnchantMath.WriteCondition` route (as Quick Draw does). |
| 141 | **Ore Sight** | `VisibleOreDistanceIncrease` (53 → e42) | +2 / +4 / +6 | Client shader read (`ShaderTexturesFinalizeSystem.cs:114`). Vanilla gives 0.5·L (10 at L20). Pure information. |
| 142 | **Spelunker** | `ChanceForAdditionalOre` (56 → e45) | 4 / 8 / 12 % | +1 ore per ore drop on a roll (`DropLootSystem.cs:5205`, `/100`). Ceiling of +1 per drop. Vanilla 5 + L. |
| 143 | **Trailblazer** | `MovementSpeedBoostAfterMining` (58) | 5 / 10 / 15 (→ +5/10/15 % speed for 5 s after mining a wall) | Applied as a timed `ShortMovementSpeedBoost` of value × 10 (`PlayerController.cs:6169-6178`). It refreshes and does not stack. Vanilla = L (20 % at L20). |
| 144 | **Dreamer** *(filler)* | `ExtraHealFromSleepPercentage` (335) | 10 / 20 / 30 % | Bed healing only (`PlayerState/Sleep.cs:16`). Harmless. |

**Sid note:** "Prospector" was removed on 2026-11-04 and I could not find which condition it used. If it was Ore Sight
or Spelunker on tools, ask before adding it back on the lantern.

## 3. Bag: carry and conserve (5). Bag size is NOT feasible

**More bag space: infeasible / unsafe.**
- Size comes from `ExtraInventoryCD.size` on the **bag prefab's level entity** (`Inventory/InventoryUpdateSystem.cs:757-773`),
  not per item. It is read inside the Burst `InventoryUpdateSystem` (unpatchable, AGENTS.md §3) and limited by the player's
  fixed `InventoryBuffer.maxSize` (`Pug.ECS.Components/InventoryBuffer.cs`).
- Even if we could change it: when the size shrinks (bag removed, enchant replaced by a re-roll, bag broken),
  `InventoryUtility.UpdateInventorySize` **drops every item past the new size on the ground** (`InventoryUtility.cs:2602-2623`,
  `DropItem`). Over lava, in water, or with a full map that means lost items. Do not do this.

So the bag pool themes "the bag keeps things":

| Id | Enchant | Condition | I / II / III | Why it is safe |
|---|---|---|---|---|
| 160 | **Magnetism** | `IncreasedPickUpRadius` (181) | +20 / 40 / 60 % pickup radius | `1.4 × (1 + v/100)` tiles (`PickUpItemSystem.cs:146-150`). QoL only. Vanilla 30 + 7L. **Sid removed Magnetism from armor on 2026-11-04. Re-add it here only if he agrees.** |
| 161 | **Provisions** | `DrainLessHunger` (67 → e51) | 10 / 20 / 30 % slower hunger | Capped at 30 (section 1). |
| 162 | **Bait Saver** | `ChanceToPreserveBait` (254) | 10 / 20 / 30 % | `PlayerState/Fishing.cs:328`. Ceiling 100 %. Bait is cheap. |
| 163 | **Demolitionist** | `ChanceToNotConsumeExplosives` (305) | 10 / 20 / 30 % | **Placed** explosives only (`PlaceObjectSlot.cs:186-191`). Thrown grenades: verify in game. |
| 164 | **Rainmaker** *(or on rings)* | `ChanceToNotConsumeWaterCan` (128) | 15 / 30 / 45 % | `WaterCanSlot.cs:133`. Ceiling 100 %. Put it in the bag **or** the ring pool, not both (slot exclusivity). |

## 4. Rings (2 slots, one shared pool): trades (8)

Rule: `Max` per enchant, so matching rings never double. 6 of these 8 can be active at once.

| Id | Enchant | Condition | I / II / III | Why it is safe |
|---|---|---|---|---|
| 180 | **Green Thumb** | `ExtraHarvestChance` (23 → e23 HarvestChance) | 5 / 10 / 15 % (raw 50/100/150, `/1000`) | Extra crop drops (`UpdateHealthFromBufferSystem.cs:352`, `EntityUtility.cs:671`). Vanilla 1 + 2L % (41 % at L20), so ours is below a vanilla ring. |
| 181 | **Seedkeeper** | `SeedDropChance` (127) | +10 / 20 / 30 % | Added to the plant's seed drop chance (`DropLootSystem.cs:1720`). Ceiling 100 %. |
| 182 | **Rare Bloom** | `ChanceToGainRarePlant` (126) | +1 / +2 / +3 % (base 3 %) | Golden/rare plant on planting (`PlaceObjectSlot.cs:160`, `SeederSlot.cs:122`). |
| 183 | **Angler** | `IncreasedFishing` (46 → e36 Fishing skill) | +10 / 20 / 30 skill | Skill is a threshold: at or above a pool's `skillNeeded` it only shortens bite time down to ×0.5 (`PlayerState/Fishing.cs:583-585`). Natural ceiling. Vanilla worn gear ≈ 6L. |
| 184 | **Double Catch** | `ChanceToGetDoubleFish` (178) | 5 / 10 / 15 % | `Fishing.cs:370`. Ceiling 100 %. Vanilla 10 + 2L. |
| 185 | **Artisan** | `ChanceToGainExtraItemFromBaseBuilding` (119) | 10 / 20 / 30 % per crafted tile/block | `CraftingUIBase.cs:203` (tiles only). The roll is client-side, as in vanilla. Building blocks are cheap. |
| 186 | **Alchemist** | `ChanceToGainExtraItemFromAlchemy` (120) | 10 / 20 / 30 % | `CraftingUIBase.cs:229`. Potions are cheap relative to the table cost. |
| 187 | **Jeweler** | `ChanceForPolishedJewelry` (122) | 5 / 10 / 15 % | `CraftingUIBase.cs:255`, only items with `JewelryCanBePolishedCD`. Self-referential for rings. |

Fishing note: Sid removed the **rod** enchants (Lure, Luck of the Sea, Reach). Angler and Double Catch are worn
stats on another slot. Confirm he wants fishing on rings at all; if not, swap in Rainmaker and Bait Saver.

## 5. Necklace: food, wards, pet bond (7)

| Id | Enchant | Condition | I / II / III | Why it is safe |
|---|---|---|---|---|
| 200 | **Gourmet** | `ChanceToGainExtraCookedFood` (79 → e54) | 5 / 10 / 15 % (raw ×10, `/1000`) | Cooking pot extra dish (`Inventory/InventoryUtility.cs:2809`). Ceiling of +1 dish. |
| 201 | **Epicure** | `ChanceForExtraCookedFoodToBeRare` (80) | 10 / 20 / 30 % | Upgrades only the *extra* dish (`InventoryUtility.cs:2810`), so it pairs with Gourmet. |
| 202 | **Lasting Flavor** | `LongerCookedFoodBuffs` (138) | 10 / 20 / 30 % longer cooked-food buffs | Snapshot when eaten (`ConditionUIExtensions.cs:143-157`). Changes duration only, not strength. |
| 203 | **Sure-footed** | `ReducedImpactOfSlowedBySlime` (227 → e89) | 15 / 30 / 45 % | Capped at 45 (vanilla's own clamp). Never full immunity. |
| 204 | **Beast Bond** | `ApplyPetDamageIncrease` (235 → e90) | +5 / 10 / 15 % pet damage (tenths, raw 50/100/150) | Forwarded owner → pet by `ApplyOwnerBuffsToPetJob` (`SummarizeConditionsSystem.cs:576-605`). Vanilla 10L raw (20 % at L20). This is the "pet enchant" without touching the pet item. |
| 205 | **Kindred** | `ApplyPetBuffsIncrease` (239 → e94) | +5 / 10 / 15 % | Becomes the pet's `PetBuffsIncrease`, then multiplies the pet talents' owner buffs by 1 + v/100 (`SummarizeConditionsSystem.cs:259, 605`). Vanilla 0.87L. |
| 206 | **Second Wind** *(optional, balance-risky)* | `CheatDeath` (108) | 2 / 4 / 6 % | Survive a lethal hit at 20 % HP (`PlayerController.cs:7104-7108`). Hard cap 6 (section 1). Vanilla 0.5L (10 % at L20). Leave it out if Sid wants the necklace to be non-combat. |

## 6. Offhand: by mechanic

### Shield (`OffHandMechanic.Shield`) (3)
Block handling: `AttackPlayerSystem.cs:1613-1648`. These trigger only on a block, so they are dead on any other build.

| Id | Enchant | Condition | I / II / III | Why it is safe |
|---|---|---|---|---|
| 220 | **Venom Guard** | `ChanceToApplyPoisonOnBlock` (156) | 10 / 20 / 30 % | Poisons the attacker for 15 s unless it is immune (`:1624-1633`). Vanilla 5 + 2L. |
| 221 | **Shockwave Guard** | `ChanceToShootShockWaveOnBlock` (184) | 5 / 10 / 15 % (×2 on a parry) | 3 shockwave projectiles (`:1636-1643`). Vanilla 5 + 2L. |
| 222 | **Flame Guard** | `DealAoeFireDamageOnBlock` (192) | ItemLevel 50 / 75 / 100 % of vanilla (10 + 5L) | Fire AoE whose value is the damage (`:1645-1648`). Needs a new `ItemCurve.BlockFire`. |

### Other offhands (`Dash`, `Teleport`, `GrapplingHook`, detonators) (1, thin on purpose)
| Id | Enchant | Condition | I / II / III | Notes |
|---|---|---|---|---|
| 230 | **Long Stride** | `IncreasedDashDuration` (205) | 10 / 20 / 30 → dash ×1.07 / 1.14 / 1.21 | `PlayerState/UseOffHand.cs:145` (`1 + v/100 × 0.7`). Does nothing on non-dash offhands, so offer it only when the mechanic is `Dash`. |

Teleport distance, grapple range and detonator values come from `OffHandCD.mechanicValue` on the prefab
(`UseOffHand.cs:166`, `num = 4f`). No ConditionID exists for them, so those offhands get no enchants for now.
**Shield durability**: a block does `SetAmount(amount − 1)` directly (`AttackPlayerSystem.cs:1614-1622`). No condition
reduces that, so "Unbreaking" for shields is not possible.

## 7. Pet slot: NOT feasible with the current storage

Our enchants are stored in the item's `PetTalentBuffer` with ids `1000 + enchantId`. On a pet, that buffer is the pet's
**real talent list**, and the vanilla summary reads it for the equipped pet every time it runs
(`SummarizeConditionsSystem.cs:244-268`). It calls `PetExtensions.GetNativeActiveConditions`, which indexes
`petTalentsLookUp[(int)petTalentID]` with no `TryGetValue` (`PetExtensions.cs:119-125`). An id of 1000+ is not in that
map, so the Burst job would throw on every summary for that player, and their stats would break. The non-owner pet
branch (`:353-377`) does the same. **Never enchant pets.** `SlotOf(Pet) = None` must stay.
Use Beast Bond and Kindred on the necklace instead (section 5).

## 8. Pouches (`pouch1..4Index`): skip
Four slots would turn one enchant into ×4 under `Sum`. Pouches are `ExtraInventoryCD.isPouch` containers, with the same
size risk as bags. They add nothing the bag pool does not already cover.

## 9. Feasibility list (everything considered)

| Idea | Hook | Rating |
|---|---|---|
| Radiance (lantern light) | glow ConditionIDs → `ConditionsEffectsHandler.cs:297-345` | **feasible** (visual only; use the lantern's own colour) |
| Ore Sight / Spelunker / Trailblazer / Dreamer | plain conditions, readers above | **feasible** |
| Magnetism (bag) | `PickUpItemSystem.cs:146` | **feasible** (Sid removed it once; ask first) |
| Provisions / Bait Saver / Demolitionist / Rainmaker | plain, readers above | **feasible** (Provisions capped) |
| **Bag size** | `ExtraInventoryCD` on the prefab level entity, Burst `InventoryUpdateSystem`; shrink drops items | **infeasible**, and unsafe (item loss) |
| Ring farming / fishing / crafting pool | plain, readers above | **feasible**. Crafting rolls are client-side, as in vanilla. |
| Necklace food pool, Sure-footed | plain | **feasible** |
| Beast Bond / Kindred | owner `ApplyPet*` → `ApplyOwnerBuffsToPetJob` | **feasible** (check the pet stats window) |
| Second Wind (CheatDeath) | `PlayerController.cs:7104` | **feasible, balance-risky** (cap 6) |
| Immunities (poison 34, burning 159, stun 103, slime slow 20, cold 370) | binary effects read in `EntityUtility.cs:1741, 1802, 1905, 1819` | **feasible, but don't**. They trivialise status-heavy bosses. Sure-footed is the soft version. |
| Shield block procs | `AttackPlayerSystem.cs:1624-1648` | **feasible** |
| Long Stride (dash) | `UseOffHand.cs:145` | **feasible** |
| Teleport / grapple range, shield durability | prefab `OffHandCD`, direct `SetAmount` | **infeasible** (no condition) |
| Enchanting pets | `PetTalentBuffer` collision, Burst throw | **infeasible** (would break the player's summary) |
| Pouches | ×4 stacking, container sizing | **skip** |
| Ghost on kill (`ChanceOnKillToSummonGhost` 175) | no reader found in decompiled Pug.Other | **risky** (unknown if it does anything) |
| Boat speed (`IncreasedBoatSpeed` 182, `PlayerVelocityCalculationSystem.cs:318`) | plain | feasible, niche (reserve for the lantern or bag) |
| Combat stats on rings/necklace (damage %, crit, dodge, speed) | plain | **rejected by design**: this is the stacking problem. Weapons and armor already cover combat. |

## 10. Ship next (top 5)

0. **Plumbing first:** `EnchantStack.Max`, the per-condition `Caps` table, worn-slot collection for
   necklace/rings/offhand/bag/lantern, new `EnchantSlot` bits plus the offhand-mechanic split in `SlotOf`.
   `/enchant` commands get `ring1|ring2|neck|offhand|bag|lantern` targets.
1. **Radiance (lantern).** Sid's own idea. Visual only, so no balance risk. Proves the lantern slot works. Test: walk in the dark, compare light radius.
2. **Green Thumb (rings).** Proves the ring pair under `Max`. Test: two rings with Green Thumb III, `/enchant stats`
   must show 150 raw, not 300.
3. **Gourmet (necklace).** Clean, visible, natural ceiling. Test: cook 20 dishes, count extras.
4. **Shockwave Guard (shield).** First offhand enchant and visibly fun. Test: block and parry a melee enemy.
5. **Provisions (bag)**, or **Magnetism** if Sid wants it back. Proves the bag slot and the cap table
   (Provisions capped at 30).
