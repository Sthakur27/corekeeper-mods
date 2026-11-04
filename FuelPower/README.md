# Fuel & Power prototype

Development prototype, not a playable fuel requirement yet. Separate from Sid's Overhaul.

## What exists

- A game-independent C# furnace transition: wood/charcoal/coal, saved-state-shaped snapshots, partial work on fuel exhaustion, no idle or blocked-output burn, and surplus burn credit.
- Coal drop eligibility and probability rules for natural walls and allowlisted mobs. Unknown wall provenance is rejected; drill/explosion drops are off by default. These rules do not hook mining or spawn items yet.
- An **observation-only** server furnace probe using the ECS query already used by Faster Machines. It logs up to eight furnace slots per sample, twelve samples per server world, five seconds apart. It does not consume fuel, pause crafting, or add coal items.
- A standalone test/demo executable compiling the exact core sources used by the prototype.

Fuel values are provisional: wood = 10, charcoal = 20, coal = 40 baseline furnace-work seconds. These are accounting examples, not balanced recipes. No charcoal conversion recipe is implemented. Energy uses work done, so a faster furnace requires more energy per real second. Nothing burns while idle. There is no electricity allocation, battery, solar panel, fuel-box prefab, fuel UI, persistence adapter, or functioning drop event adapter in this milestone.

## Run the core prototype

Requires .NET SDK 8 or later. No game, Unity, or third-party NuGet packages required.

```sh
dotnet run --project FuelPower/Tests/FuelPower.Tests.csproj -- --demo
```

On the remote box, the task-local SDK is installed at `/home/ubuntu/.local/share/corekeeper-dotnet/dotnet`.
Tests cover fuel exhaustion/refueling, blocked output, residual energy, completion, speed scaling, tick-size independence, conservation, snapshot reconstruction and drop eligibility/chance boundaries. Snapshot reconstruction is not a test of Core Keeper save files.

## Windows handoff

The root `ModManifest.json` packages **only the observation scaffold and shared core**. It has not been compiled with game assemblies or run in Core Keeper. Build/test with the matching SDK using your existing mod workflow. See [the integration checklist](docs/windows-integration.md) before enabling any gameplay writes.

The first playable target is one ordinary furnace supplied with wood from an explicit fuel inventory. Coal drops can follow once natural-wall provenance and the authoritative destruction event are verified. Mob drops are an alternative if provenance cannot be established reliably.
