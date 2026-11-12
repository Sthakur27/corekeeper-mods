# Coin Drops

Enemies you kill drop Ancient Coins alongside their usual loot. Tougher enemies give more coins, and bosses get an extra multiplier. Coins appear where the enemy died and are pulled towards the credited player like normal loot.

Your pets, minions, projectiles and bombs can earn drops too. The mod also remembers your last hit for 20 seconds, so an enemy that dies from burning, bleeding or poison shortly afterwards still pays out. That grace period also counts other causes of death: a trap or environmental finish can pay if you recently hit the enemy. Enemies killed without player credit or a recent player-owned hit give no coins, so unattended farms do not earn them.

## Coin amounts

At the default **0.5x** setting, a 100 HP enemy gives about **5 coins** and a 2,500 HP enemy about **25**. Bosses get **3x** the amount. Drops vary by roughly 20% either way, with at least 1 coin for an eligible kill.

The calculation is: square root of maximum health × Coin amount × Boss multiplier (bosses only) × a random factor from 0.8 to 1.2. Fractional coins round up by chance. Anything that raises enemy maximum health also raises the reward.

## Settings

Open **Settings > Mod Options > Coin Drops**. Changes apply right away; choose **Off** to stop the extra drops.

| Setting | Choices | Default |
|---|---|---|
| Coin amount | Off, 0.5x, 0.75x, 1x, 1.5x, 2x, 3x, 5x | 0.5x |
| Boss multiplier | 1x, 2x, 3x, 5x, 10x | 3x |

## Requirements and multiplayer

For Core Keeper **1.3.0**. Requires **CoreLib** and **Mod Options**. Install the mod and its dependencies on the host/server and all joining clients. The host's settings decide the drops.

Removing the mod leaves any coins you collected intact: they are ordinary Ancient Coins.

## Cover artwork

The cover is assembled from game art, not a gameplay screenshot. It combines the game's orange slime sprite and Copper Sword icon with an Ancient Coin and stack-count digits taken from screenshots, over a blurred cave screenshot. HDR captures were tone-mapped, the coin was reconstructed from its unobscured quarter, and the title banner and plus/equals signs were added.
