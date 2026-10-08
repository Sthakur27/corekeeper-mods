# Fish Seller

Core Keeper mod. The **Fishing Merchant** sells **every fish in the game** (44, from Orange Cave Guppy
to Riftian Lampfish, including the Legendary Starlight Nautilus) and the **Caveling Merchant** sells **Shiny Larva Meat**. And because that no longer
fits in a merchant's window, **every merchant's buy window scrolls**: mouse wheel over the window, one row
per notch, with a small scrollbar on its left.

Required on client and server (`requiredOn: 3`). Depends on **CoreLib** and **Mod Options**.

## Prices and stock

- Prices are the game's own formula: a fish costs **5x what the merchant pays you for it**. The price
  multiplier (Mod Options, 0.25x to 4x, default 1x) scales buying only; selling fish still pays vanilla.
- **Stock per restock** (default 10) is how many of each item the merchant carries. Items that don't
  stack come one per slot.
- **Starlight Nautilus** costs a fixed **500 coins** (x the multiplier). The game prices every Legendary
  item at 0 and its buy code (Burst) can't be patched, so this one is bought through a small CoreLib
  command instead: clicking it sends `/fishsellerbuy`, the server checks stock and the coins in your main
  inventory, then takes the coins and gives you the fish (dropped at your feet if your bag is full).
  You'll see a "Bought ..." line in chat.
- Items the game refuses to trade (Legendary rarity, or "can't be sold") are left out automatically;
  Player.log lists them (`[FishSeller] not sold: ...`).
- A merchant from an existing save restocks right away the first time the new wares are missing from
  his shelves; after that, on the normal schedule (25-35 min, or whatever the Merchant Restock mod is set to).

## Scrolling

- Works for every merchant whose wares don't fit (not vending machines). The window keeps its size
  (8 columns x 3 rows for the merchants this mod stocks, so it never covers your inventory) and you
  scroll through the rest. The range ends at the last stocked slot, so bought-out items at the end
  don't leave empty pages.
- While the pointer is over a scrollable shop the wheel does not switch hotbar slots.
- Controller: not supported yet (the window shows the first rows only).

## Compatibility

- **Potion Seller**: both widen the buy window with the same layout; only one of them moves it (a
  marker on the window decides, and a separately installed Potion Seller always wins). Potion Seller
  1.4.0+ also no longer resets the window to exactly 8x3. Use Potion Seller 1.4.0 or newer.
- **Ender Stash** (Ender Chest on the Fishing Merchant) and Potion Seller's Titan summons stay on his list.
- Merchant lists are only ever added to and slot buffers only grow, so other mods that add wares keep
  theirs, and get the extra room and scrolling for free.
- Restock fix: the game restocks a merchant on every tick if an entry it can't sell yet (unmet unlock
  requirement) sits before items it can, which would mean endless stock. Fish Seller keeps those gated
  entries at the end of every merchant's list.
- Included in Sid's Overhaul (page "Fish Seller", switchable under Features On/Off).
