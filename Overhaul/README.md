# Sid's Overhaul

![Sid's Overhaul — illustrated mod cover](../release/overhaul_logo.png)

Spend less time repeating chores and more time exploring. Sid's Overhaul brings the individual mods in this repository into one package: easier farming and fishing, equipment tools, merchant supplies, and adjustable combat and progression.

**New in 1.16.0: more merchants.** More NPCs (Alpha) adds eight boss merchants and replaces Potion Seller in the Overhaul. New features: Auto Loadout Switch (Ctrl+L links the item you're holding to the current loadout) and Valuable Value (valuables sell to merchants for 10x, adjustable from 1x to 100x; buy prices unchanged). Enchanting 0.3.0 adds experimental Thunderstrike, Piercing, Flame, Static Familiars and Soul Siphon. Coin Drops also pays for enemies that burn, bleed or are poisoned to death after you hit them. Build Tools: Swap says in chat why nothing was swapped, and a new setting turns off its shovel/pickaxe requirement.

**New in 1.15.1:** Potion of Return now costs 25 Gold Bars to craft (was 10), and 2,500 coins at the Caveling Merchant (was 800).

**New in 1.15.0: coins and prices.** Coin Drops now only pays for your kills (including your pets, minions, arrows and bombs; traps and enemy-on-enemy kills drop nothing) and drops half as many coins by default. The Coin Pouch slot now also shows next to the hotbar at merchants, chests and crafting stations. Potion Seller: the Caveling Merchant also sells the Potion of Return (800 coins); Healing Potions now cost 100 and Recall Idols 500.

**New in 1.14.1:** Auto Replant: replanting from seeds on the ground no longer floods the game log with conveyor warnings.

**New in 1.14.0: performance pass.** Many features do less work per tick: fewer scans of the world, read-only access where nothing changes, and staggered timers. Enchanting can now roll small enchantments onto rings, necklaces and offhands, and totals from all your gear are capped so stacking one defence everywhere stops paying off. Fixes: Fast Auto Fishing now really stops when you switch it off in the Overhaul settings, Armor Dye no longer leaks your weapon dye onto enemy projectiles, and the Coin Pouch slot shows up again after changing worlds. The Wormhole Potion is now called the Potion of Return and the Drill Unit is now the Drill Pod; existing items and saves carry over.

**New in 1.13.0: Enchanting (Alpha).** Build an Enchanting Table (Galaxite Workbench: 20 Octarine + 15 Gold Bars), put a weapon, tool or armor piece in it and pay Gold Bars + Ancient Coins for a random enchantment, up to three per item. Alpha: tested in single player only; balance will change.

**New in 1.12.0: Drill Unit (Alpha), now named Drill Pod.** An end-game hover craft from the Rift Statue (300 Pandorium + 100 Solarite Bars): it crosses water, lava and pits, drills through walls while you hold left click, and can lay floor or dig pits around you (G cycles the mode). Alpha: tested in single player only.

**New in 1.11.0: Coin Pouch, Coin Drops and Wormhole Potion.** Coin Pouch adds a coin slot under the vanity pants: picked-up Ancient Coins go there automatically, up to 99,999, and merchants take payment from it. Coin Drops makes every enemy drop Ancient Coins (more for tougher enemies, a big pile from bosses). The Wormhole Potion (10 Gold Bars at an Ancient Hologram Pod) recalls you to the Core and marks a one-use return point you can teleport back to from any waypoint.

**1.10.4: Relucite storage and bigger units.** Relucite storage and bigger units.** Runic Storage 0.10.0 adds a ninth tier, Relucite, and every tier now holds 40 more slots than the last: Copper units start at 40 and Relucite units hold 360. Upgrade gems cost 40 bars of their material (Copper units still cost 30 Copper Bars). Units you already placed grow to their new size and keep their items.

**1.10.5:** Vehicle Speed 1.2.0: boats and go-karts now default to 1.5x. Speeds you already chose are kept.

**1.10.3:** Build Tools 0.6.4: Swap no longer removes the protected floor in front of the Core (worlds where it already did get that floor back on load), a swapped-out floor gives the block you'd get by digging it up (Dirt, not Dirt Ground), and "Roofing gadget never wears out" also covers reinforced gadgets. Vehicle Speed 1.1.0 adds 1.25x, 1.5x and 1.75x steps for boats and go-karts.

**1.10.2:** Build Tools 0.6.3 fixes the block brush leaving an empty, unusable cell when aimed at a pit or water in walls-only, floor-only or Swap mode, and never puts walls on water, lava or pits.

**1.10.1:** Movable Ore Boulders 1.2.1 brings back the hit wiggle when you mine an ore boulder with a pickaxe (the hits counted, but the boulder didn't react).

**New in 1.10.0: Solarite Drill, and drills that actually pay off on ore boulders.** A gold Solarite Drill one tier above the Scarlet Drill: 10 Galaxite + 10 Solarite Bars at the Solarite Workbench, 3x the Scarlet Drill's mining damage. Infinite Ore Boulders 1.0.2 fixes drills getting no ore from a protected boulder (it reset their progress toward the next ore every tick). Faster Machines 1.1.0 raises the game's ore-boulder limit on fast drilling along with your Drills speed, so 5x drills really get 5x the ore. Movable Ore Boulders 1.2.0: optional setting to move drilled boulders too (they're placed back full; off by default).

**1.9.5: Movable Ore Boulders and pet talent points.** Mine an ore boulder loose with a pickaxe strong enough to break a Maze Block, and carry it to your drills; mining boulders never gives ore, drills still do. Skill XP & Talent Points now also sets how many talent points a pet has at level 10: default 9, enough for all nine slots (Vanilla is 5).

**1.9.4:** Item Spawner 1.1.1: the trash can is usable while the spawner is open, and the item on your cursor no longer hides behind the Gourmet checkbox.

**1.9.0: Skill XP & Talent Points.** Choose how many talent points each skill has at level 100 (Vanilla 25, or 30, 35 or 40, enough to fill a whole tree; default 40). In multiplayer the host picks whether their XP multipliers and talent points apply to everyone or each player uses their own, and only the host can change values with `/xp`.

**1.8.0: cooking in Item Spawner.** Press the cooking pot button in the spawner to build any dish: pick two ingredients (golden ones included), see the real dish with its stats, and tick Gourmet for the rarer roll. Every dish is one the cooking pot can make.

**1.7.0: Item Spawner.** Press Z (rebindable under Controls) to search every item in the game, modded ones included. Left-click puts one on your cursor (Ctrl: a full stack), right-click puts one in your bag (Shift or Ctrl: a full stack), and the - / + buttons spawn gear and tools up to the max level above their base. Host and admins only.

**1.6.1:** Build Tools 0.6.0: watering cans go from 1x1 up to Max tool size and remember their size when the Bigger Watering Cans switch is off; with it on, its 2x2 / 4x4 stay the limit unless you turn on "Override Bigger Watering Cans sizes".

**New in 1.6.0: Build Tools.** Bigger shovel, hoe, roofing gadget and paint tool areas (up to 10x10 by default), each tool type and block category remembers its own size even after you quit, a block brush (Keypad + / -, shape on Keypad *), block modes auto / floor only / walls only and roofing modes flip / close roof / open roof (Keypad /), and Swap on Left Alt to exchange blocks. Watering cans keep Bigger Watering Cans' sizes. Inspired by Placement Plus and Roofing Gadget Plus.

**1.5.0: Faster Machines and no more intro logos.** Faster Machines speeds up furnaces, kilns, the glass smelter, fury forge, cooking pot, table saw, egg incubator and drills, each with its own setting (default 5x; Instant available). The studio logos at startup are skipped.

**1.4.8:** Fixes 1.4.7, which failed to load. Slot Organizer is a separate mod again; subscribe to it on its own if you want it.

**New in 1.4.0: Runic Storage and smoother bases.** Craft its workbench from the Base section of a workbench. Storage units and sequential upgrade gems cost 30 bars of their tier. Empty its units before removing the mod, and disable the standalone Runic Storage mod when using this bundle. This release also removes per-tick inventory, merchant and farm work that grew with base size, and fixes a chest-skin mix-up that caused about 80 ms hitches near storage.

**From 1.3.9:** Magic Mirror crafting now also requires 10 Recall Idols, in addition to 50 Solarite Bars. Existing mirrors and the 20% Omoroth drop are unchanged.

**Included from 1.3.8: Mushroom Slayer.** Strong hits break through gray Shrooman Brute armor. Mod Options defaults: threshold 200, damage 10%; red phase unchanged. Bow combat and multiplayer await live testing. Match settings on host and clients. Includes Magic Mirror and the Golden Chance performance fix. Restart after updating.

## Install and get started

1. Subscribe to [Sid's Overhaul](https://steamcommunity.com/sharedfiles/filedetails/?id=3813043354) and [CoreLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3673516180) on Steam Workshop.
2. Disable the separate versions of the included mods so the same features do not run twice.
3. Restart Core Keeper, then open **Settings > Mod Options** from the main menu or pause menu.

**Mod Options is built in.** You do not need a separate Mod Options subscription for the Overhaul. For multiplayer, install matching versions and CoreLib on the host and every client. World-changing settings generally use the host's values; local controls and cosmetic options are configured by each player.

Prefer to pick individual mods? Use [Sid's Core Keeper Mods collection](https://steamcommunity.com/sharedfiles/filedetails/?id=3814775238). For manual installation, see the [latest GitHub release](https://github.com/Sthakur27/corekeeper-mods/releases/latest) and the instructions inside its zip.

## What's included

| Feature | What it does |
|---|---|
| Shared Loadout Gear & Vanity + Five Loadouts | Fill empty equipment slots from lower loadouts, use separate vanity outfits, and add two loadout tabs. |
| Auto Loadout Switch | Link an item to a loadout with Ctrl+L, and the game switches to that loadout whenever you start holding it. |
| Ender Stash | A personal 40-slot stash accessed through an Ender Chest bought from the Fishing Merchant; travels with your character and stays safe on death. |
| Coin Pouch | A coin slot under the vanity pants: picked-up Ancient Coins go there automatically, up to 99,999, shared by every loadout; merchants take payment from it, and it shows next to the hotbar while you shop. |
| Runic Storage | Craft a storage workbench, connect storage units, and upgrade them through nine tiers from 40 to 360 slots per unit. One-click category tabs (Materials, Weapons, Tools, Armor, Blocks, Furniture and more) filter the shared inventory, and Sort groups it by category. Inspired by Magic Storage for Terraria. |
| Solarite Drill | A gold stationary drill one tier above the Scarlet Drill with 3x its mining damage. Craft for 10 Galaxite + 10 Solarite Bars at a Solarite Workbench. |
| Magic Mirror | Reusable three-second return to the Core. Craft for 50 Solarite Bars + 10 Recall Idols at a Solarite Workbench, or get one from Omoroth with a 20% chance per defeat. |
| Potion of Return | (formerly Wormhole Potion) One-use recall to the Core that marks your spot on the map; teleport back to it once from any waypoint. Craft for 25 Gold Bars at an Ancient Hologram Pod. |
| Drill Pod (Alpha) | End-game hover craft (Rift Statue: 300 Pandorium + 100 Solarite Bars) that crosses water, lava and pits, drills through walls and lays floor or digs pits around you. Tested in single player only. |
| Enchanting (Alpha) | Random enchantments for weapons, tools and armor from an Enchanting Table (Galaxite Workbench), paid with Gold Bars and Ancient Coins; up to three per item. Tested in single player only. |
| Armor Dye | Color individual armor, weapons and tools with an inventory palette. |
| Pet Editor | Change pet levels, talents and colors through the pet window. |
| Item Spawner | Host/admin spawn browser (Z): search every item, one or a full stack to your cursor or bag, gear and tools spawned above their base level, and any cooked dish from two ingredients. |
| Auto Replant | Replant player-harvested crops on suitable planting ground using a matching nearby dropped seed or one you carry. |
| Bigger Watering Cans | Water a 2x2 or 4x4 patch with supported cans. |
| Faster Mushrooms | Tune spread and mushroom regrowth near players. |
| Fast Auto Fishing | Automate fishing and shorten waiting and animations; special boss fishing keeps its bite timing. |
| Better Fishing Loot | Adjust the chance of valuable non-fish fishing loot. |
| More NPCs (Alpha) | Eight new merchants, one per boss: beat the boss, place the idol it drops in a room with a free bed, and its merchant moves in. They sell blocks, decor, grenades and bombs, potions, pet eggs, hydra goods, farm animals and seeds. Replaces Potion Seller: its leftover items are cleared from the vanilla merchants' shops. Not fully tested yet. |
| Fish Seller | Buy fish and Shiny Larva Meat, and scroll through expanded shops. |
| Valuable Value | Valuable items sell to merchants for 10x their normal price (Mod Options: 1x to 100x); buy prices stay the same. |
| Merchant Restock | Choose a restock interval for all merchants or individual merchant types. |
| Infinite Ore Boulders | Keep drilling ore boulders without depleting them. |
| Movable Ore Boulders | Mine an untouched ore boulder loose (Maze Block mining power) and place it next to your drills. Mining boulders gives no ore. Optional setting to move drilled boulders too (refills them, off by default). May not work with other mods that change ore boulder mining or destruction. |
| Build Tools | Bigger tool areas remembered per tool, a block brush with floor/wall modes and Swap, and roofing gadget modes. Inspired by Placement Plus and Roofing Gadget Plus. |
| Faster Machines | Per-machine speed for furnaces, kilns, glass smelter, fury forge, cooking pot, table saw, egg incubator and drills (default 5x, up to 100x or Instant). |
| No intro logos | Skips the studio logos when the game starts. Always on. |
| Quick Buff | Shortcuts for buffs (B), food only (F), a healing potion (H) and a mana potion (J), including items in pouches. |
| Longer Buffs | Set minimum durations for food and potion buffs. |
| Durability Multiplier | Choose how fast equipment wears out. |
| Difficulty Tuning | Separate enemy and boss settings for Hard and Normal worlds. |
| Mushroom Slayer | Configurable gray brute armor: hits below 200 deal 1; hits of 200 or more deal 10%. Red phase unchanged. |
| Boss Bonus Loot | Extra drops from selected bosses and enemies. |
| Coin Drops | Every enemy you kill (yourself, or your pets, minions, arrows and bombs) drops Ancient Coins, scaled by its max health; bosses drop a big pile. |
| Skill XP & Talent Points | Adjust experience gains per skill and the talent points each skill gives. |
| Keep Minions On Teleport | Bring summoned minions with you through teleports. |
| Vehicle Speed | Adjust boat and go-kart speeds. |
| Hit Sound Effects | Pick hit, kill and hurt sounds, including custom audio. |
| Golden Chance | Additive golden-plant and bonus-food rarity bonuses using native rolls. Regular main dishes are unchanged. |

The individual mod folders on [GitHub](https://github.com/Sthakur27/corekeeper-mods) explain controls, defaults and limits in more detail. Features change game balance in different ways; check their settings before starting a new playthrough.

## Choose your settings

Features with adjustable settings have their own pages under **Settings > Mod Options**. Use the arrows to change values. On these authored pages, **Reset to defaults** asks for a second click to confirm.

**Overhaul 1.3.9 includes Mod Options 1.2.2.** Compatible third-party CoreLib client settings can now appear automatically in the same list, with readable names and help text below each option. Reopen the menu to discover settings initialized later. These edits use the original mod's configuration; discovered pages have no bulk reset, and the original mod determines whether a restart is needed. Discovery is limited: it does not expose every installed mod, server/admin settings, keybindings, or custom JSON settings. See [Mod Options](../ModOptions/README.md) for supported types and limits.

Version 1.3.7 adds Magic Mirror 1.0.2: a permanent Recall Idol alternative, crafted in the Solarite Workbench Gear category or dropped by Omoroth. The boss drop is one shared roll per defeat, alongside normal loot. Its feature switch takes effect after restarting; remove mirrors before uninstalling.

Version 1.3.6 includes Golden Chance 2.2.0: native inventory processing keeps Burst optimization, and talent updates run only on relevant events. Regular-main-dish upgrades have been removed to prioritize performance. Existing upgraded food is unchanged.

Version 1.3.5 fixed repeated Longer Buffs (then Buff Duration Floor) scans caused by live items loading and unloading. Duration updates now target item templates only. This update does not change the game's automation or sound-pool behavior.

The **Turn Sub-Mods On/Off** page disables whole features after a restart. Shared Loadout Gear & Vanity, Five Loadouts, Ender Stash, Coin Pouch and Runic Storage stay enabled because they hold items in added inventory slots. Empty the stash and extra equipment slots before removing mods that provide them.

## Credits and license

Fast Auto Fishing is a fork of mikufish (GPL-3.0-or-later). The combined Overhaul is distributed under **GPL-3.0-or-later**. Master Pet Plus is not included; Pet Editor is the replacement in this package.
