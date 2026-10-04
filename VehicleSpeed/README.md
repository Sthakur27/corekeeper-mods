# Vehicle Speed

Core Keeper mod: boat and go-kart speed multipliers in Settings > Sid's Mods.

| Setting | Options | Default |
|---|---|---|
| Boat speed | 1x, 2x, 3x, 5x, 10x | 1x |
| Go-kart speed | 1x, 2x, 3x, 5x, 10x | 1x |

Applies instantly to every boat and go-kart. Replaces the third-party Boat Turbo mod: disable that
one, since both set the boat's speed. In multiplayer everyone should use the host's values (each
client predicts its own ride).

## How it works (for modders)

- Boats: `PlayerVelocityCalculationSystem` multiplies the rider's velocity by `BoatCD.speedMultiplier`.
- Go-karts (Primitive, Renegade, Speeder): `VehicleRiding` and `PlayerVelocityCalculationSystem`
  multiply the kart's speed by `VehicleCD.speedMultiplier`.
- `VehicleSpeedSystem` (client + server) sets both fields to prefab value x multiplier on every boat
  and kart, every half second and immediately when a setting changes. Neither field is replicated
  nor written by vanilla code after conversion.

Limit: steering speed is vanilla, so at 5x-10x karts turn wide.
