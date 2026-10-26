# Static performance audit: settings, merchants, and work in progress

Reviewed 2026-10-14. Source inspection only: priorities below are hypotheses, not measured bottlenecks. No game settings, saves, installed mods, or mod source were changed. Working-tree WIP files can change concurrently; re-check line references before implementing fixes. Reviewed AGENTS.md first.

No `DisableBurst` calls were found in this assigned group. Managed ECS reads/writes and array snapshots can still wait for outstanding component jobs; lack of an explicit `CompleteAllTrackedJobs` call is not proof of zero synchronization. Native temporary allocations are distinct from managed garbage allocations. Timings should identify server/client world and capture total frame time alongside method timings.

## Priorities

1. **WIP Connected Storage:** measure idle destruction scanning and open-terminal refreshes before release. Full storage and crafting discovery is repeated while the UI is open; commands explicitly complete all tracked jobs. Scope should stay bounded before pursuing more features.
2. **Existing merchant mods:** quantify Potion Seller/Fish Seller two-second work and Merchant Restock one-second work, especially their combined same-frame cost. They keep doing scans after initialization; a vanilla setting does not turn the scanners off.
3. **Skill XP Multiplier:** measure grant bursts at 1x. It snapshots two arrays and tracks entities before checking whether scaling is necessary.
4. **WIP Magic Mirror / Brute Armor:** measure their per-simulation-pass managed interception. They preserve vanilla Burst, but state/event-buffer processing can run more frequently under prediction than a rendered frame.
5. **Lower likely steady-state cost:** Golden Chance 2.2, Difficulty Tuning, Quick Buff. They have bounded/event work worth measuring, but no evidence here of the former broad Golden Chance inventory slowdown.

## Golden Chance 2.2

- **Trigger and frequency:** talent result postfix, player initialization, talent-point assignment/reset, settings changes. `GoldenChance/Scripts/Patches/TalentValuePatch.cs:11`, `:53`, `:62`, `:69`; setting callbacks at `GoldenChance/Scripts/GoldenChanceMod.cs:48`. `Update` is empty at `:77`.
- **Work:** result patch switches on two condition IDs and adds a clamped bonus. `ResendTalentValues` scans the bounded talent tree and sends only the two relevant condition types (`TalentValuePatch.cs:28`, `:35`). No periodic inventory scan, Burst disabling, or inventory job hook remains in this source.
- **Defaults/off:** both bonuses default to +0% (`GoldenChanceMod.cs:17`); postfix exits when zero (`TalentValuePatch.cs:24`). Resend events still enumerate trees and issue condition updates at zero; this is small event work, not a zero-cost absence of the mod. Overhaul feature-off skips patches/init after restart.
- **Instrument/test:** count and time `ResendTalentValues`, separate join/settings/talent-change causes; record resulting command count. Test join, repeatedly assigning/resetting talents, harvest/cooking bursts, and +0% versus +30%. Capture frame time while ordinary inventory moves occur as a regression test against the old design.
- **Mitigation if needed:** restrict talent-point callbacks to affected trees once API semantics are verified; do not reintroduce an interval or disable Burst for regular food handling.

## Difficulty Tuning

- **Trigger:** authoring/conversion Harmony patches, with settings copied into profiles on settings changes. `DifficultyTuning/Scripts/Patches/ConversionPatches.cs:12`, `:27`, `:37`, `:55`; damage/speed converters in `Patches/DamagePatches.cs:20` and `Patches/SpeedPatches.cs:19`. `DifficultyTuningMod.cs:110` has empty Update.
- **Work:** prefab component checks and scalar math at conversion. Speed curve initialization scans prefab MonoBehaviours once, deduplicates and sorts speed samples (`Tuning.cs:186`, `:194`, `:204`). Boss fallback classification converts ObjectID to string (`Tuning.cs:153`); likely load-time allocations, not constant frame work.
- **Defaults/off:** normal profile is 1x; hard profile initializes regular damage/health to 1.5 (`Tuning.cs:56`, `:59`), with other multipliers 1x. Hard damage is therefore not necessarily vanilla hard damage. Overhaul-off removes Harmony participation on next restart. Conversion-time effects require world reload for a valid A/B comparison.
- **Instrument/test:** conversion patch total time/count, `ReadEnemySpeeds`, world-load duration; separately compare a combat scene at high recharge/projectile settings. Faster attacks can increase *vanilla* projectile/effect workload despite cheap mod code.
- **Mitigation:** keep conversion-based design; only optimize startup string/classification work if measured. Do not confuse unchanged mod timing with unchanged combat simulation workload.

## Skill XP Multiplier

- **Trigger:** `ScaleSkillXPSystem` before `AddSkillValueSystem`, both worlds; `RequireForUpdate(_grants)` avoids running without matching grant entities (`SkillXPMultiplier/Scripts/Systems/ScaleSkillXPSystem.cs:16`, `:29`). This consumes native event entities rather than scanning all players.
- **Work:** copies entity and AddSkillValueCD arrays with Allocator.Temp each active update (`:34`), consults two reused HashSets, writes non-1x grants, then swaps/clears sets. Allocation and set work happen even when multiplier is 1x (`:48`). HashSets can grow with burst workload; native snapshots can synchronize component jobs.
- **Defaults/off:** default 1x (`SkillXPTable.cs:28`) still incurs event-batch bookkeeping. Overhaul-off disables system at its first update. No explicit global job completion or Burst disabling found.
- **Instrument/test:** OnUpdate time; grant count; skipped-already-scaled, 1x, and modified counts; snapshot time separately. Compare no XP, normal movement XP, mass harvest/combat XP, 1x versus non-1x; measure both client/server.
- **Mitigation:** consider a config-derived all-vanilla fast path before snapshots, carefully resetting stale dedup state on transitions. Preserve exactly-once scaling when grants survive multiple frames; do not make allocations smaller at the expense of correctness.

## Potion Seller

- **Trigger:** authoring edits plus server merchant fix-up every two seconds (`PotionSeller/Scripts/Systems/MerchantStockSystem.cs:21`, `:96`). Includes disabled entities and prefabs (`:90`). Price system checks the setting each tick but only traverses price entries initially or after a change (`Systems/PotionPriceSystem.cs:27`).
- **Work:** `RefreshBeaten` rebuilds the killed-boss set and reads connected players' soul buffers each pass (`MerchantStockSystem.cs:53`); temporary player and merchant arrays at `:59` and `:105`; per-merchant item/list/inventory checks and one-time stocked checks. Changed merchants are logged, not each unchanged pass (`:130`). UI layout patch runs each BuyUI LateUpdate (`Patches/BuyUIPatches.cs:178`), with early return for closed UI (`:80`).
- **Defaults/off:** default feature-enabled behavior adds stock even at price 1x; pricing is not an enable switch. No polling-disabled path inside standalone stock system. Overhaul switch skips init/patches and disables update system after restart.
- **Instrument/test:** `MerchantStockSystem.OnUpdate`, `RefreshBeaten`, `MerchantStock.Apply`, price changed-pass and UI layout; count merchants/prefabs, killed entries, souls, changed items and log lines. Benchmark idle with merchants, boss/soul unlock, restock, new-world load, open shop, and Potion Seller + Fish Seller together.
- **Mitigation:** move persistent migration to verified spawn/load/config/progression events; cache progression and process only newly loaded/dirty merchants. If the runtime API lacks reliable events, use a documented bounded reconciliation fallback, not a promise that polling can be eliminated without correctness work.

## Fish Seller

- **Trigger:** server stock pass every two seconds over merchants including prefabs (`FishSeller/Scripts/Systems/ShopStockSystem.cs:21`, `:44`, `:50`); price system cheap setting comparison each tick and full loop only on change (`Systems/ShopPriceSystem.cs:32`).
- **Work:** `RefreshAmounts` clears/rebuilds catalog dictionary and checks prefab tradeability on every pass (`ShopStockSystem.cs:58`, `:100`). Applies items with linear `IndexOf` per catalog item (`MerchantRoom.cs:41`, `:137`), partitions gated items only when needed (`:72`, native copy at `:85`). Room/reorder work also applies to merchants outside its own item targets (`ShopStockSystem.cs:77`). `_checked` entity set is retained for the world's life. UI scroll scans backwards through inventory each visible LateUpdate (`Patches/BuyScroll.cs:47`, `:78`) and redraws scrollbar; actual slot refresh occurs when scroll offset changes (`:106`).
- **Defaults/off:** 1x price, 10 stock (`ShopItems.cs:90`, `:92`) still perform stock work; feature-off is restart-scoped in overhaul. Logging mostly one-time/change-triggered. No explicit Burst disabling.
- **Instrument/test:** stock pass, RefreshAmounts, ApplyItems/KeepGatedLast, BuyScroll.Update; merchant count, catalog count, entries inspected/changed, trailing empty slots examined, restocks forced. Test enlarged mixed-mod merchant inventories, scroll with no input, repeated restock/unlock and large merchant populations.
- **Mitigation:** cache immutable tradeability/catalog results until database/config changes; invalidate merchant work on relevant list changes. Verify Potion Seller and Fish Seller converge rather than continually undoing each other's ordering/stock. Separate necessary per-frame scale/layout compensation from inventory scans that can be dirty-driven.

## Merchant Restock

- **Trigger/work:** server pass every second (`MerchantRestock/Scripts/Systems/MerchantRestockSystem.cs:21`, `:32`), snapshots merchants and reads ObjectDataCD before checking cap (`:40`). Writes only when countdown exceeds configured cap (`:45`). Vanilla Burst restock remains intact.
- **Defaults/off:** default global setting is Vanilla, individual setting SameAsAll (`MerchantRestockMod.cs:41`); scanner still runs and snapshots when no merchant has an active cap. Overhaul-off disables system after restart.
- **Instrument/test:** pass/snapshot time, merchants inspected, countdowns actually changed; idle vanilla, lowered cap, first restock and many merchants. Correlate with sellers' two-second passes.
- **Mitigation:** config-derived `AnyActiveCaps` early return is a straightforward first optimization candidate. For event-driven countdown adjustment, investigate a managed completion boundary; do not disable the native Burst restock job merely to remove a small bounded scanner.

## Quick Buff

- **Trigger:** client IMod.Update polls input (`QuickBuff/Scripts/QuickBuffMod.cs:140`); expensive work only after a key press/command. Server system explicitly sets Enabled=false and `Consume` is called by command (`Systems/QuickBuffServerSystem.cs:91`, `:101`). Request cooldown is 0.4s (`:60`).
- **Work:** caller's inventory/pouches only; creates range list, deduplicating HashSet and slot snapshot per accepted request (`:179`, `:194`), allocates/disposes native condition data per candidate (`:233`). Applies conditions and queues vanilla inventory consumption, not direct stack mutation. Per-player cooldown dictionaries persist for the world; check long-session churn if many players join/leave.
- **Defaults/off:** key polling is expected enabled behavior even with skip-active settings. No idle inventory scan. Overhaul-off guards IMod and commands, and init does not register inputs. No explicit CompleteAllTrackedJobs or Burst disabling found here; EntityManager access may still synchronize.
- **Instrument/test:** Consume total, candidate/slot/condition counts, accepted/rejected requests, commands queued. Compare one heal potion to full expanded pouch inventory, already-active buffs, multiple clients pressing together. Measure per-request peak, not just amortized average.
- **Mitigation:** reusable scratch collections if request allocation is measured; prune cooldown state on disconnect if growth is observed. Preserve vanilla consumption and avoid duplicating runtime work in an automatic polling feature.

## Overhaul guards

- Snapshot config read occurs once then `Features.On` is a dictionary lookup (`Overhaul/Scripts/Features.cs:64`, `:80`). Defaults are on (`:67`, `:77`). It is a restart switch, not live disable.
- Builder injects Harmony Prepare, IMod guards, and OnUpdate self-disable (`release/build_overhaul.py:67`, `:102`, `:106`). It does **not** generally gate OnCreate, constructors, static initialization, or arbitrary callbacks. Disabled systems can allocate queries in OnCreate and execute one guarded update. Verify actual generated output, not just builder counts or switch labels.
- Magic Mirror has an explicit exception: its cast system is copied without a guard to cancel use safely while disabled (`release/build_overhaul.py:114`). This means a disabled mirror still retains player scan overhead if its item is registered.
- Inventory-layout features are intentionally not switchable (`Features.cs:25`). Do not disable/uninstall those on the user's live character for performance isolation.
- Instrument feature-on/off resolved snapshot, system first-update counts and steady-state execution counts. Verify cold-start disabled cases and a restart after a switch change; do not claim a UI toggle immediately removes work.

## WIP Connected Storage (read-only; not a release recommendation)

- **Open UI:** every 0.4s Refresh scans storage graph, all chest slots, sorts items, then gets/sorts recipes (`ConnectedStorage/Scripts/StorageTerminalUI.cs:127`, `:134`). `StorageCrafting.GetRecipes` independently scans the graph again (`StorageCrafting.cs:32`, `:37`). Graph Scan creates a query, native entity snapshot, dictionary, queue, HashSet and lists (`StorageNetwork.cs:63`, `:69`); GetStations scans world crafting entities (`:106`, `:112`). Larger off-network storage/stations still increase discovery cost.
- **Idle:** removal system snapshots candidate chests every simulation update while no evacuation is active (`StorageRemoval.cs:68`, `:75`); recipe system checks all recipe-bearing stations each second (`ConnectedStorageMod.cs:103`). UI returns immediately when inactive (`StorageTerminalUI.cs:99`).
- **Actions:** command entry explicitly completes all tracked jobs (`StorageCommand.cs:51`); transfer/craft workers do so again (`StorageTransfer.cs:46`, `:125`; `StorageCrafting.cs:76`). Removal evacuation also completes all jobs (`StorageRemoval.cs:92`) and constructs slot/item/move plans with nested source/destination loops (`:117`, `:143`). These are explicit main-thread synchronization points; cost is not established by source inspection.
- **Default/off:** no ordinary enable setting; UI closed does not stop removal/recipe systems. Not listed as switchable in current Features.All. Do not remove storage-bearing mods for A/B on live saves.
- **Instrument/test:** Scan, GetStations, Refresh, GetRecipes, removal OnUpdate, command validation/worker/CompleteAllTrackedJobs separately. Count all candidate nodes versus connected nodes, slots, recipes, queued moves and query constructions. Use copied world: 1/10/100 chests, disconnected networks, many nearby/unrelated crafting stations; idle closed/open tabs, deposit-all, bulk crafting and breaking full chest during transfers. Host/client separately.
- **Mitigation:** per-world topology index with placement/removal invalidation; reuse the same snapshot for item and recipe views; dirty inventory/recipe updates; restrict removal query to actual storage nodes and changed/failed health candidates if semantics allow. Review redundant global barriers and replace only after dependency correctness is understood. Retain server-side authoritative validation even with UI caches.

## WIP Magic Mirror (read-only)

- Cast system runs in player-state group on both client/server, snapshots simulated players before checking Casting state and mirror ID (`MagicMirror/Scripts/MirrorCastSystem.cs:13`, `:33`, `:40`). No Burst disabling; native teleport state is reused. Item ID lookup occurs per invocation. Prediction may run several simulation passes per rendered frame.
- Recipe migration scans every recipe-bearing station once a second in both worlds (`MagicMirrorMod.cs:83`), although authoring callback already applies recipe (`:25`). Repeated scans become no-ops per station after insertion.
- Feature-off exception deliberately retains cast handling (`release/build_overhaul.py:114`) with `Active` check/cancel (`MirrorCastSystem.cs:67`). Item lookup None returns early; merely never using the item does not avoid player/recipe scans.
- Instrument OnUpdate, players inspected/casting/mirror matches, predicted invocations, recipe stations scanned/modified. Test idle, successful/cancelled/hit-interrupted cast, repeated use, higher-latency client, disabled feature with existing mirror and large station count.
- Mitigation: dirty/new-station migration; narrower event/state selection if available without new replicated data or disabling vanilla Burst. Preserve off-state cancellation semantics.

## WIP Brute Armor (read-only)

- Before vanilla health-buffer application, both worlds (`BruteArmor/Scripts/BruteArmorSystem.cs:8`); RequireForUpdate requires a buffer entity, not nonempty hit contents (`:29`). Every update allocates temporary entity array and scans all queued hits (`:35`). Reuses number-change list.
- Only matching MushroomBrute armor damage is rewritten (`ArmorRule.cs:13`); damage number correction creates a new HashSet per nonempty correction batch and scans effect rings with matching-number searches (`BruteArmorSystem.cs:59`, `:62`, `:73`). Worst work grows with hit batch and affected targets/ring lengths; ordinary unrelated combat still pays filtering.
- Standalone is active by presence; overhaul-off guards update after restart. Native health/death/loot path remains intact, no Burst disabling in inspected source.
- Instrument hits inspected/rewritten, targets and effect entries searched, OnUpdate and CorrectPlayerNumbers separately. Test idle, ordinary combat without brutes, single brute, large mixed combat/AoE, client prediction and repeated hits in one tick.
- Mitigation: reuse visited HashSet if allocations show up; measure singleton fast access if buffer ownership guarantees it. Keep ordering and matching correctness; do not replace queued health handling with direct health writes to save time.
