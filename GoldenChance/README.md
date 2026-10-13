# Golden Chance — Work in Progress

![Golden plant and cooking pot concept art, clearly marked WORK IN PROGRESS.](../release/goldenchance_logo.png)

**WORK IN PROGRESS: this mod is still being developed and tested.** The settings below describe the current source's intended behavior, not a guarantee that every planting, cooking or multiplayer case works. The published Workshop files may differ from this development version.

Increase the odds of **golden plants** and **rarer cooked food**, with separate bonuses and an optional rarity roll for the main dish.

## Current development settings

Open **Settings > Mod Options > Golden Chance**.

| Setting | Choices | Default |
|---|---|---|
| Golden plant chance bonus | +0% to +100%, in steps of 5 | +0% |
| Golden food chance bonus | +0% to +100%, in steps of 5 | +0% |
| Apply food rarity chance to regular cooking | Off / On | Off |

These are intended as percentage-point additions: a 20% chance with a +5% bonus becomes 25%. At +0%, this mod leaves the talent value unchanged. The source attempts to apply bonuses even with no points in the relevant talent.

With regular cooking **off**, the food bonus affects only the rarity roll for **extra food produced by the Cooking skill**. With it **on**, each main dish successfully collected from a cooking pot gets a separate roll at the collecting player's effective Master Chef chance, capped at 100%. This also works without bonus food or any Cooking talent points when the additive food bonus is nonzero. The chance of producing bonus food is unchanged.

An ordinary dish upgrades to rare; a dish made rare by golden ingredients upgrades to epic. Both ingredient IDs are preserved, including the extra benefit from two golden ingredients. Upgraded main dishes drop for pickup at the collector, like bonus food; unchanged dishes follow the usual mouse/inventory transfer. A mixed-rarity result cannot share one mouse slot. Failed transfers do not roll, and ordinary inventory moves or picking up the dropped dishes do not roll again. Cooking into an existing pot-output stack does not roll the older dishes repeatedly: rolls happen on collection, including output cleared by changing cookbook recipes.

The **host/server controls the regular-cooking toggle for everyone**. Each collector's own replicated food-rarity condition supplies their chance; the host's talents are not substituted for remote players. Clients' copies of the toggle do not control the server. Additive bonuses remain individual player settings. Changes apply to subsequent collections, including food already waiting in a pot. Automatic collection without a player has no talent roll.

Already planted seeds keep their earlier outcome; automated seeders that ignore player talents are outside this approach.

## Requirements and testing status

Requires **CoreLib and Mod Options** on the host and every client. Use the standalone mod or Sid's Overhaul, not both.

The current implementation refreshes player talent conditions on joining a world, changing settings and changing talent points. It also repairs late condition overwrites after a short replication grace period, including resets while already at zero talent points.

Version 2.1.0 and the assembled Overhaul 1.3.2 pass offline compilation against the installed 1.3.0.5 game assemblies. The overhaul also passes the game's script compiler and security check, registers all three options, and initializes its Burst-disabling hook. Actual cooking, multiplayer, and Auto Replant interactions still need in-world testing. This is not a published release.

## For contributors

The 2.1.0 source retains the 2.0.0 additive config keys. A managed talent-value patch changes the plant and bonus-food conditions, then sends them through the game's existing condition commands. Old multiplier configuration keys are unused.

Regular cooking uses `BurstDisabler.DisableBurstForSystemAndJobs<InventoryUpdateSystem>()` from the installed SDK, which supports unmanaged systems. Its inventory jobs therefore run managed, even with the live toggle off; performance on busy servers needs testing. Scoped job context supplies the exact request's player (including guest-mode filtering); cookbook collection supplies its explicit player. Hooks on successful `TryMove`, output-to-empty-slot `Swap`, and output `DropItem` perform one roll per removed item. Upgraded quantities use vanilla consume/drop helpers, preserving the food's variation, variation-update count and auxiliary-data index. No component types are added to players, and the vanilla bonus-food routine is unchanged. All hook classes and initialization are gated by the overhaul's Golden Chance feature switch.
