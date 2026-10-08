# Fish Seller

Core Keeper mod: the **Fishing Merchant sells every fish in the game** (all 44, from Orange Cave Guppy to Riftian Lampfish, including the Legendary **Starlight Nautilus**), and the **Caveling Merchant sells Shiny Larva Meat**. Their wares no longer fit in a shop window, so **every merchant's shop now scrolls**: hover over the window and use the mouse wheel.

## What you get

- **All fish, all the time.** No unlocks; they're on the Fishing Merchant's list next to his vanilla
  bait, rings and necklaces.
- **Fair prices.** A fish costs 5x what the merchant pays you for one (the game's own formula), so
  buying and reselling never makes a profit. Selling fish still pays vanilla prices.
- **Starlight Nautilus for 500 coins.** The game normally refuses to sell any Legendary item, so this
  one goes through a small server check instead. You see "Bought StarlightNautilus for 500 coins." in
  chat, and the fish drops at your feet if your bag is full.
- **Shiny Larva Meat** at the Caveling Merchant, for cooking without farming the larva hive.
- **Scrollable shops.** The window keeps its usual size (8 wide, 3 rows, so it never covers your
  inventory) and scrolls through the rest, one row per wheel notch, with a scrollbar on the left. Works
  for every merchant whose wares don't fit, including items added by other mods. While you're over the
  shop, the wheel doesn't switch your hotbar.

## Settings (Settings > Mod Options > Fish Seller)

| Setting | Options | Default |
|---|---|---|
| Price multiplier | 0.25x, 0.5x, 0.75x, 1x, 1.5x, 2x, 3x, 4x (buying only) | 1x |
| Stock per restock | 1 to 99 of each item | 10 |

In multiplayer the host's values count. Want the shops to refill faster? Use **Merchant Restock**.

## Compatibility

- **Potion Seller**: works together. Use Potion Seller 1.4.0 or newer; both widen the shop window and
  only one of them moves it.
- **Ender Stash** (the Ender Chest) and Potion Seller's Titan summons stay on the Fishing Merchant's list.
- **Merchant Restock**: this mod decides what merchants sell, that one decides how often it comes back.
- Other mods that add merchant wares keep theirs and get the extra room and scrolling for free.
- Existing saves work: a merchant restocks once right away the first time the new wares appear.
- Bug fix included: if a not-yet-unlocked item sat ahead of available ones on a merchant's list, the
  game would refill his shelves every few seconds (endless stock). Fish Seller keeps locked items at the
  end of every list.
- Not supported yet: scrolling with a controller (it shows the first three rows).

Requires [CoreLib](https://mod.io/g/corekeeper/m/corelib) and Mod Options. Needed on client and server.

## How it works (for modders)

- Lists: fish and larva meat are appended to the merchants' `MerchantItemInfoBuffer` (prefabs in every
  world via `API.Authoring.OnObjectTypeAdded`, live merchants by a server system every 2 s). Each
  merchant's `ContainedObjectsBuffer` grows to fit his whole list, because `MerchantBuyInventorySystem`
  fills slot k with the k-th available entry and drops anything past the buffer. Items the game won't
  trade (Legendary without a special price, `CantBeSoldCD`) are left out and logged.
- Scrolling: the window shows `handler.size` slots from `NPC.inventoryHandler.startPosInBuffer`, and
  clicks buy `startPosInBuffer + slot`, so scrolling just moves `startPosInBuffer` by whole rows. No
  server change is needed.
- Prices: `buyValueMultiplier` in the PugDatabase blob = vanilla x multiplier; `sellValue` is untouched.
- Legendary items: `InventoryUtility.GetCoinValue` returns 0 for Legendary, and the buy path is Burst.
  So the client patches the managed price label and `InventoryHandler.Buy` for those items, then sends
  `/fishsellerbuy <objectID> <slot>` (CoreLib command). The server checks stock and coins, then queues
  `ConsumeObjectType` + `MoveOrDropAmount`.
- Restock fix: between restocks the game compares slot i with entry i for every available entry and
  refills on a mismatch, so a gated entry ahead of others caused endless restocks. Gated entries are
  stable-moved to the end of every merchant's list.
