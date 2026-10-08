# Merchant Restock

Core Keeper mod: choose how often merchants refill their stock, in Settings > Mod Options > Merchant Restock.

| Setting | Options | Default |
|---|---|---|
| All merchants | Vanilla (25-35 min), 15, 10, 5, 2, 1 min | Vanilla |
| Caveling, Slime, Fishing, Crystal, Void, Seasonal Merchant | Same as all, or any of the above | Same as all |

Changes apply right away, including to a merchant's current countdown. The game's random spread is
kept (5 min = about 3.5 to 5 min). With everything on Vanilla the mod changes nothing.
In multiplayer the host's values count.

Works with Potion Seller and Fish Seller: they decide what merchants sell, this mod decides how often
it comes back.

Requires [CoreLib](https://mod.io/g/corekeeper/m/corelib) and Mod Options.

## How it works (for modders)

The game keeps each merchant's restock countdown in `ObjectDataCD.amount` (seconds).
`MerchantBuyInventorySystem` (Burst, server) counts it down every 4 s and, below 1, refills the shelves
and sets it to a random 1500-2100. `MerchantRestockSystem` (server) scales any countdown above the
chosen cap into 0..cap once per second; countdowns at or below the cap are left alone.
