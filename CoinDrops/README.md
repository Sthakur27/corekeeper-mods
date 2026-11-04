# Coin Drops

Every enemy drops **Ancient Coins** when it dies. Tougher enemies drop more, and bosses drop a big pile.

## How many coins

An enemy drops coins equal to the square root of its max health. Tougher enemies pay more, but late-game enemies don't flood you with coins:

| Enemy max HP | Coins (default) |
|---|---|
| 25 | 5 |
| 100 | 10 |
| 1,000 | 32 |
| 2,500 | 50 |
| 8,300 | 91 |

Each drop varies by about 20% either way, and every enemy drops at least 1 coin. Bosses multiply that again. Coins land where the enemy died and are pulled towards the player who killed it, like normal loot.

## Settings

Open **Settings > Mod Options > Coin Drops**.

| Setting | Choices | Default |
|---|---|---|
| Coin amount | Off, 0.5x, 0.75x, 1x, 1.5x, 2x, 3x, 5x | 1x |
| Boss multiplier | 1x, 2x, 3x, 5x, 10x | 3x |

Changes apply right away.

## Requirements and limits

Requires **CoreLib and Mod Options**. In multiplayer, the host's settings decide the drops.

Mods that raise enemy health also raise coin drops. Removing the mod is safe: the coins are ordinary items.
