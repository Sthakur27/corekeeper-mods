# Golden Chance

![Golden Chance — illustrated mod cover](../release/goldenchance_logo.png)

Increase the odds of **golden plants** and **rarer bonus cooked food**, using the game's native talent conditions and rolls.

## Settings

Open **Settings > Mod Options > Golden Chance**.

| Setting | Choices | Default |
|---|---|---|
| Golden plant chance bonus | +0% to +100%, in steps of 5 | +0% |
| Golden food chance bonus | +0% to +100%, in steps of 5 | +0% |

Bonuses add percentage points, capped at 100%. A 20% chance with a +5% bonus becomes 25%. At +0%, the mod preserves the native talent value. Bonuses also apply with zero talent points.

The food bonus affects the rarity of **extra food produced by the Cooking skill**. It does not increase the chance of producing extra food. Regular main dishes use vanilla behavior. Already planted seeds keep their earlier outcome; automated seeders that ignore player talents are outside this approach. Auto Replant's base-chance override still applies.

## Performance fix in 2.2.0

The regular-main-dish upgrade option from 2.1.0 has been removed. That implementation disabled Burst optimization for the inventory system and its jobs, even when the option was off. Version 2.2.0 removes the inventory hooks and leaves Burst enabled. Any old saved regular-cooking setting is ignored; existing upgraded items are unchanged.

There is no timer, recurring talent scan, or frame-update work. Talent conditions are sent through the native player-command path only on local-player initialization/rejoin, saved talent-point changes, confirmed talent resets, and changes to the bonus settings. A confirmed reset works even when the saved point list is empty. Each client uses its own talent points; the host does not substitute its points for other players.

Regular-main-dish upgrades are deferred until a separate implementation can preserve performance and inventory correctness. The existing collection and crafting request paths are unchanged in this release.

## Requirements and validation

Requires **CoreLib and Mod Options** on the host and every client. Use the standalone mod or Sid's Overhaul, not both. Restart the game after installation; an already running session retains the old patches.

The event callbacks and talent calculations pass 20 isolated checks covering zero-point joins, talent changes, repeated events, confirmed resets, settings changes, clamping, remote players and unavailable worlds. Both standalone and overhaul builds are compile-checked against the installed 1.3.0.6 assemblies. These tests do not simulate Harmony patch registration or NetCode. The overhaul passed the in-game loader and safety check on October 14, 2026; Sid confirmed movement felt good after restarting with 2.2.0. Reset/rejoin, multiplayer, and standalone in-game checks remain unverified. No instrumented FPS benchmark was recorded.
