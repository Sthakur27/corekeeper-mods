# Golden Chance: PC agent handoff

Written October 8, 2026. This is unfinished implementation work, not a release announcement.

## User request and publishing boundary

Sid requested separate **additive percentage-point bonuses** for golden plants and golden cooked-food chance, with vanilla maximums explained in Mod Options. That change is committed and pushed.

The next request is:

> can we make it a setting the cook epic chance applies to regular cooking as well as the bonus food

Implement an optional setting that extends the existing food rarity roll to the **main cooked dish**, while retaining the existing roll for **bonus cooked food**. Include it in the standalone Golden Chance mod and Sid's Overhaul, then commit and push for Sid to test on his PC.

Sid explicitly wants to test after pulling Git **before publishing to Steam**. Do not publish to Steam or mod.io as part of this handoff. Keep an already running game open; coordinate its restart with Sid rather than killing it.

## Current Git state

Repository: `Sthakur27/corekeeper-mods`, branch `main`.

Completed implementation commit: `28ff922` — `Add additive golden chance bonuses to standalone and overhaul`.

- Golden Chance version: **2.0.0**.
- Sid's Overhaul version: **1.3.1**.
- Separate choices: `+0%`, `+5%`, …, `+100%`, default `+0%`.
- The original 1x–3x multiplier settings were replaced with new additive config keys.
- The proposed regular-cooking toggle has **not** been added. There is no unfinished hook or placeholder toggle to enable.

The Linux agent had no game DLLs, decompiled game sources, or C# compiler. Checks completed here were C# syntax parsing, manifest-file checks, overhaul assembly (including asset-bundle rewriting), generated feature-guard checks, and standalone ZIP checks. **No compile check against real game assemblies or in-game test has passed yet.** Existing README statements about behavior describe intended implementation and still need verification.

## Read these files first

Read root `AGENTS.md` before implementing; it contains loader, ECS, compile, installation, settings, and publishing instructions.

| File | Purpose |
|---|---|
| `GoldenChance/Scripts/GoldenChanceMod.cs` | Settings, additive values, live changes, periodic talent refresh |
| `GoldenChance/Scripts/Patches/TalentValuePatch.cs` | Existing managed Harmony postfix and local-player condition resend |
| `GoldenChance/ModManifest.json` | Standalone source list and CoreLib/ModOptions dependencies |
| `GoldenChance/README.md` | Current documented behavior and limits |
| `ModOptions/Scripts/SettingsPage.cs` | `.Toggle(out Setting<bool>, label, default)` and settings API |
| `Overhaul/Scripts/SidsOverhaulMod.cs` | Golden Chance settings registration; overhaul version |
| `Overhaul/Scripts/Features.cs` | Existing GoldenChance on/off feature entry |
| `release/build_overhaul.py` | Builds GoldenChance sources into overhaul; injects feature guards |
| `release/workshop_publish.py` | Existing standalone and overhaul Workshop registrations; publishing deferred |
| `release/make_goldenchance_logo.py` | Original art source, updated for additive settings |

GoldenChance is **already** in the overhaul builder, settings registration, feature-switch table, and Workshop uploader. Extend that existing feature rather than adding a duplicate mod or settings page.

## Existing mechanism

`TalentValuePatch.Postfix` patches managed `SkillTalentsTable.GetConditionDataForSkillTalent` and adds the selected bonus to:

- `ConditionID.ChanceToGainRarePlant`.
- `ConditionID.ChanceForExtraCookedFoodToBeRare`.

It leaves values unchanged at +0%; otherwise it clamps the modified condition to 0–100. Bonuses apply to zero-point talents as well as trained talents.

The existing path is talent condition → replicated `SkillTalentConditionsBuffer` → `SummarizedConditionsBuffer` → game roll. Settings changes send conditions using `player.playerCommandSystem.SetSkillTalentCondition(player.entity, data)`.

The mod polls every 0.5 seconds and resends when the local player/command system, entity, golden talent points, or additive settings change. This was added because vanilla talent resets reportedly use a blob table rather than the managed method. Audit the real initialization/reset paths: confirm zero-point talents are included, commands are accepted, rejoining initializes correctly, and unchanged polling does not accumulate bonuses or resend unnecessarily.

The current hook changes only the food **rarity** condition. It does not remove the gate that vanilla applies before producing **extra food**. Extending the main dish needs a separate verified hook at main-food creation/collection; changing the talent value alone cannot accomplish it.

## Target toggle semantics

Suggested label: **Apply food rarity chance to regular cooking**.

The remote agent proposed **off by default** to preserve vanilla behavior; Sid has not specified a different default.

- **Off:** retain current behavior: only bonus food receives the modified Master Chef roll.
- **On:** the main cooked dish receives one rarity-upgrade roll at the same effective chance as bonus food: the player's vanilla talent value plus the configured food bonus, capped at 100%.
- The main roll must work even when no bonus food is produced, including Cooking level zero with a nonzero additive food bonus.
- Retain the existing bonus-food roll exactly once. Do not reroll or double-upgrade bonus food.
- Use the relevant player's effective condition; do not add the bonus again to an already patched condition or substitute the host's talent level for a remote player's level.
- Keep ingredient-derived rarity and the extra benefit of two golden ingredients intact.
- Intended extension follows vanilla's **one-tier rarity upgrade**: ordinary ingredients can yield a rare main dish; a dish already rare from golden ingredients can become epic. Do not describe all food rolls as an unconditional epic chance. Verify the game's actual rarity representation and maximum tier before implementing.
- Existing already cooked stacks must not reroll when moved, loaded, or collected again. Roll exactly once at the correct game event, preserving item auxiliary data and inventory behavior.
- Settings should apply live to subsequent cooking operations. Update the hint so it distinguishes the new toggle from the vanilla bonus-food-only behavior.

If multiplayer requires an additional way to transmit the toggle, design that explicitly after inspecting the actual command path. Existing additive values travel through talent conditions; the new boolean does not automatically do so. Do not claim per-player multiplayer support for the toggle unless verified. Avoid adding player component types: the player archetype is close to its chunk-size limit (see AGENTS.md).

## Research on the installed PC

Expected checkout: `C:\Users\Sid\CoreKeeperMods`.

Game assemblies: `C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\Managed\`.

Player log: `%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Player.log`.

1. Pull the current repo and read AGENTS.md. Confirm the installed game version from Player.log and inspect existing local tools/scratch decompilation before downloading tools again.
2. Decompile `Pug.Other.dll`; inspect `InventoryUtility.IncreaseCookingSkillAndSpawnExtraFoodIfWeShould` and all its callers. Search for `ChanceForExtraCookedFoodToBeRare`, cooked-food creation, ingredient rarity, cooking output, and collection commands.
3. Confirm exact types, signatures, item auxiliary-data structure, and the player who owns each roll. Determine when the main dish is created versus when extra food is awarded. The main output may exist before collection; do not assume which stage owns the upgrade.
4. Check Burst annotations and generated job callers. Harmony cannot patch Burst code. Use a verified managed hook or the repo's supported managed ECS pattern; do not guess a method name or assume a patch on a utility called by Burst will run.
5. Verify the recorded vanilla numbers below directly in the current game. The remote agent used repository notes plus wiki descriptions; it did not inspect the current installed DLLs.

Relevant reference pages for context, not substitutes for the installed game's code:

- https://corekeeper.atma.gg/en/Expert_gardener
- https://corekeeper.atma.gg/en/Master_chef
- https://corekeeper.atma.gg/en/Cooking

## Probability context behind the request

Numbers assumed in the conversation (verify against current game):

- Golden plants: 3% base + 15% max Expert Gardener = **18%**.
- Max Master Chef: **25%** chance to upgrade bonus food one rarity tier.
- Max Cooking: **20%** chance to produce extra food. This is a separate gate, not the Master Chef rarity chance.

With both additive bonuses set to +30%, at max talents:

| Event | Chance |
|---|---:|
| Each newly planted crop is golden | 48% |
| Both of two harvested crops are golden, assuming independent rolls | 48% × 48% = 23.04% |
| Master Chef upgrade on food eligible for the roll | 55% |
| Current pipeline: both golden, then epic bonus food | 23.04% × 20% × 55% = 2.5344% per pair |
| Requested toggle on: both golden, then epic **main dish** | 23.04% × 55% = 12.672% per pair |

Sid's “ultra max food” means **two golden ingredients cooked into an epic dish**, preserving the two-golden-ingredient benefit. He wants the main dish to have a chance without depending on the 20% bonus-food gate.

The 12.672% figure counts epic **main dishes**. It does not include additional epic bonus dishes. If main and bonus upgrade rolls are independent and the main upgrade does not alter the bonus roll's starting rarity, the probability of at least one epic dish from a two-golden pair is `1 - (1 - 0.55) × (1 - 0.20 × 0.55) = 59.95%`; including the two-golden harvest condition gives **13.81248% per harvested pair**. Verify those independence assumptions before presenting that latter figure as actual game behavior.

## Implementation and verification checklist

1. Add a persisted `Setting<bool>` in `GoldenChanceMod.RegisterSettings`, expose its value for the new hook, and update SettingsHint. Preserve the existing additive config keys.
2. Implement the verified main-food roll and register every new `.cs` in the standalone manifest. Use the game inventory APIs and preserve auxiliary data; never directly poke inventory buffers for item moves.
3. Ensure the overhaul build's existing GoldenChance feature switch gates every new hook/system. A disabled feature must leave vanilla cooking intact.
4. Update GoldenChance and Overhaul READMEs, and increment versions from 2.0.0 / 1.3.1 as appropriate. The central overhaul registration already calls GoldenChance.RegisterSettings and will include the toggle.
5. Offline compile standalone and assembled overhaul against actual game DLLs, following AGENTS.md's Roslyn compiler workflow and minimal CoreLib config stubs. Fix all `error CS` results before launching.
6. Install/test standalone and overhaul separately. Standalone: `install.bat GoldenChance` (requires CoreLib and ModOptions). Overhaul: `python release/build_overhaul.py --install` (requires CoreLib, includes ModOptions). Ensure duplicate standalone GoldenChance is not active alongside the overhaul.
7. At a user-coordinated restart, inspect Player.log for loader compile success, safety-check success, settings page registration, and errors.
8. Verify these behaviors with real cooking operations, using deterministic 0%/100% boundaries where possible rather than judging one random dish:
   - Toggle off reproduces the existing main/bonus behavior.
   - Toggle on upgrades the main dish even with no extra food, including level-zero Cooking with +100% bonus.
   - Ordinary ingredients upgrade one tier; one and two golden ingredients produce epic main dishes at a 100% effective upgrade chance.
   - Bonus food remains correct; it is not upgraded twice and remains capped at the allowed rarity.
   - +0%, +30%, and +100% food bonuses, live toggle changes, talent resets, world rejoin, and config persistence behave correctly.
   - Moving/recollecting cooked food and cooking into an existing output stack do not reroll old food or corrupt stacks/ingredient data.
   - Multiplayer tests use two players with different talents/settings and verify server-authoritative results and toggle semantics.
   - Overhaul feature switch off leaves vanilla behavior intact after restart.
9. Commit and push the completed sources/docs/art changes. Do not commit game DLLs, decompiled game sources, compiler tools, tokens, generated build folders, or release ZIPs.
10. Report mechanism, versions, compile/log evidence, test results, and remaining limitations to Sid. Steam publishing remains deferred until he confirms the test outcome and asks to publish.
