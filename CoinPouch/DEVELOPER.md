# Coin Pouch developer notes

The player prefab gets one extra slot at the end of its `ContainedObjectsBuffer`, a 1x1 inventory range over it, and a slot requirement that accepts only Ancient Coins. As a real inventory range, it is available to the game's pickup and merchant payment code.

`CoinPouchSystem` runs on the server every 6 ticks. It adds the slot to older characters, keeps it locked so quick stack skips it, and moves coins from the main inventory into it beyond the normal 9,999 stack limit. It checks buffers read-only until a write is needed.

`CoinSlotUI` clones the armor pants slot into the character window. Harmony patches point it at the coin range and include its coins in the merchant affordability check.
