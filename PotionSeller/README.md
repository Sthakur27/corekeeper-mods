# Potion Seller

Core Keeper mod. The **Caveling Merchant** sells **every potion**, 20 of each per restock, at fixed
Ancient Coin prices from 50 to 500. Nothing is gated: the potions are on his list from the first visit,
next to his vanilla wares.

Required on client and server (`requiredOn: 3`). Depends on **CoreLib** and **Mod Settings Menu**.

## Prices

| Potion | Buy (1x) | You get when selling one |
|---|---|---|
| Healing Potion | 50 | 10 |
| Mana Potion | 50 | 10 |
| Poison Aid Potion | 75 | 15 |
| Burn Resistance Potion | 75 | 15 |
| Keen Potion | 150 | 30 |
| Magic Potion | 150 | 30 |
| Stoneskin Potion | 200 | 40 |
| Enrage Potion | 200 | 40 |
| Guardian's Potion | 250 | 50 |
| Minion Potion | 250 | 50 |
| Greater Healing Potion | 300 | 60 |
| Greater Mana Potion | 300 | 60 |
| Unusual Potion | 500 | 100 |

Edit `Scripts/PotionPrices.cs` to change a price (keep multiples of 5, see below) or the list.

## Settings (Mod Settings Menu)

- **Potion price multiplier**: 0.25x, 0.5x, 0.75x, 1x (default), 1.5x, 2x, 3x, 4x. Scales the *buy*
  price only (e.g. Healing Potion at 2x costs 100). Applies instantly.
- **Potions per restock**: 1..99, default 20. Applies to the merchant's list instantly; the shelf itself
  only changes at his next restock (the game restocks every 25-35 minutes of play).

In multiplayer the host's settings are what the server charges; clients with a different multiplier
would only see a wrong price label.

## Sell-value side effect (read this)

Core Keeper has one price field per item, `sellValue`, and derives both directions from it
(`InventoryUtility.GetCoinValue`):

- buy price = `round(sellValue * 5 * buyValueMultiplier)`
- sell price = `sellValue` per potion (times stack size)

So to make a potion cost P coins the mod sets `sellValue = P / 5` and uses `buyValueMultiplier` for
the global multiplier. Consequence: **selling a potion to the merchant pays exactly one fifth of its
1x buy price** (table above), whatever the multiplier is. Vanilla potions have no explicit sellValue;
the game computes one from rarity and crafting ingredients (with a +-10% per-item jitter), so the
vanilla sell prices are replaced by the fixed ones. You can never profit from buying and reselling.

## How it works

Merchant stock:
- `MerchantConverter` copies the merchant prefab's item list into a `MerchantItemInfoBuffer`
  (`objectID`, `amount`, `requirementToBeAvailable`). `MerchantBuyInventorySystem` (Burst, server)
  restocks the merchant every 1500-2100 s by walking his `ContainedObjectsBuffer` and filling slot k
  with the k-th available list entry, so the list only matters up to the inventory size. The vanilla
  merchant inventory is 3x3.
- `MerchantStock.Apply` appends the 13 potions (amount = stock setting, requirement None) and grows the
  inventory to **5x5** (`InventoryBuffer[0].sizeX/sizeY/maxSize` + `ContainedObjectsBuffer` padded to
  25 slots). It runs on the `CavelingMerchant` prefab entity in every world through
  `API.Authoring.OnObjectTypeAdded` (fired by `ModPostConverter` after conversion, so the buffers
  exist; `InventoryBuffer.sizeX/sizeY` are not replicated, which is why the client prefab must match),
  and `MerchantStockSystem` (server, every 2 s) does the same for merchants already saved in a world.
  When potions get added to a live merchant his restock timer (`ObjectDataCD.amount`) is zeroed so the
  shelf refreshes within seconds instead of at the next 25-35 minute restock (that one-off refresh
  also re-rolls his vanilla stock).
- Restock order is list order: vanilla items first, then potions cheapest first. Vanilla items that are
  not yet unlocked (boss statue / core requirements) are skipped, so the potions move up.

Buy window:
- `BuyInventoryUI` instantiates a fixed `MAX_ROWS x MAX_COLUMNS` (3x3) grid in `Init` and never
  shows more slots than that. Harmony postfixes on the two getters raise them to 5x5, and a postfix on
  `BuyUI.ShowContainerUI` grows the background sprite by the extra rows/columns and keeps the window's
  top edge where vanilla puts it (the grid is centred on the container origin). The background patch
  sizes from the live `InventoryHandler`, so vending machines, which share the window, look vanilla.

Prices:
- `PotionPrices.Apply` writes `sellValue` / `buyValueMultiplier` in three idempotent places, same
  layering as Bigger Watering Cans: the prefab `ObjectInfo` (authoring hook + `PugDatabase.objectsByType`
  so `PugDatabasePostConverter` bakes them into the blob), SDK-style `ObjectAuthoring` results
  (Harmony postfix on `ObjectAuthoringToObjectInfo`), and the built `PugDatabase` blob itself
  (`PotionPriceSystem`, client + server, re-run whenever the multiplier changes).

## Limits

- The buy window grid is 5x5 = 25 slots (`MerchantStock.Columns/Rows`). The vanilla list plus 13
  potions must fit; a warning is logged if it does not. The window is roughly 2.25 tiles wider and
  taller than vanilla; if it overlaps the sell window or the player inventory on your resolution,
  lower `Rows`/`Columns` (e.g. 6x4).
- Whether the background sprite grows cleanly depends on its draw mode (sliced/tiled: `size` is set;
  simple: the transform is scaled).
- Only `ObjectID.CavelingMerchant` is changed. Vending machines and other vendors are untouched.
- Prices are per item type; the potions have no variations, so that is fine.
- Not verified in-game yet: the merchant prefab's exact vanilla list/size and the buy window layout.
  Player.log lines to look for: `[PotionSeller] CavelingMerchant prefab: N items, 25 slots`,
  `[PotionSeller] blob prices at 1x: 13/13 potions found`, `[PotionSeller] merchant ...: restock forced`.
