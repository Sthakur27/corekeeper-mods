# Enchanting (Alpha)

Random enchantments for your gear. Build an **Enchanting Table**, put a weapon, tool, armor piece, ring,
necklace or offhand in it,
pay **Gold Bars and Ancient Coins**, and the item gains a random enchantment — up to three per item.

**Alpha:** it works in single player and is still being tuned. Expect balance changes, and report anything odd.

- **The table:** crafted at the **Galaxite Workbench** (stations tab) for **20 Octarine Bars + 15 Gold Bars**.
  Interact with it, place an item in the slot and press the **up arrow** to roll an enchantment.
- **Cost per roll:** 5 Gold Bars + 250 Ancient Coins, then 10 + 500, then 15 + 750 for the third enchantment.
- **Rolls:** a new enchantment usually lands at level I (60%), sometimes II (30%) or III (10%). Rolling one the
  item already has raises it a level. An item with three enchantments swaps a random one for a new one.
- **Removing:** the **red down arrow** removes the item's top enchantment (free, no refund).
- **Where they apply:** weapons, tools and staffs while **held**; armor, rings, necklaces and offhands while
  **worn**; summoning tomes while they are **in your inventory** (the last tome you held).
- **Accessories don't stack with themselves:** if the same enchantment is on several rings/necklace/offhand,
  only your highest copy counts (two rings with Precision III give +3%, not +6%).
- **What you see:** enchanted items get a **violet slot border**, and their tooltips list each enchantment and
  its effect. Enchantments stay on the item through upgrades and dyes.

## Enchantments

Damage and speed enchantments multiply what you already have, so they matter at every stage of the game
(x1.04 / x1.08 / x1.12 at levels I / II / III).

| Item | Enchantments |
|---|---|
| Melee weapons | Sharpness (x1.04-1.12 melee damage), Swiftblade (x1.03-1.10 attack speed), Fire Aspect, Venom, Stunning, Knockback, Lifesteal, Keen (crit chance), Bossbane |
| Bows, throwing weapons | Power (x1.04-1.12 ranged damage), Quick Draw (x1.03-1.10 attack speed), Frost (slow), Stunshot, Keen, Bossbane |
| Staffs and magic weapons | Arcane Power (x1.04-1.12 magic damage), Quick Draw, Frugality (less mana cost), Spellsurge (magic damage after a crit), Clarity (mana back on crit), Frostbite (chill), Keen, Bossbane |
| Summoning tomes | Pack Leader (x1.04-1.12 minion damage), Frenzy (minion attack speed), Keen Fangs (minion crit), Slayer (minion boss damage) |
| Pickaxes, shovels, hoes, sledges, drills | Efficiency (+10-30% mining), Unbreaking, Fortune, Mending |
| Helm, chest, pants | Resilience (less damage taken), Vitality (+3-10% max health), Regeneration, Evasion, Bosswarden, Thorns, Unbreaking, Blast Protection; Swiftness on pants |
| Rings, necklaces, offhands (one shared pool) | Might (x1.01-1.03 all damage), Haste (x1.01-1.03 attack speed), Precision (+1-3% crit chance), Ferocity (+2-6% crit damage), Fleetfoot (+1-3% movement speed), Hardiness (+1-3% max health), Green Thumb (+5-15% extra harvest), Angler (+10-30 fishing), Gourmet (+5-15% extra cooked dish), Radiance (+1-3 light range) |
| Offhands only (extra) | Vitality (+2-6% max health), Resilience (2-5% less damage taken), Swiftness (+2-6% movement speed) |

Fire Aspect, Lifesteal and Thorns grow with the item's level.

**Caps:** enchantments from all your gear together add at most −12% damage taken, +15% max health, +8 dodge,
+12% move speed, +60% regeneration and −9% boss damage, so stacking the same defence on every piece stops paying off. Accessory enchantments are small on purpose and
only your highest copy of each one counts. Your total from enchantments is capped at 25% less damage taken.

## What's tested (alpha)

- **Tested in game:** the table (crafting, rolling, costs, the remove button), tooltips and violet borders, and
  that every melee, bow and armor enchantment reaches your stats (the mod's own self-test passed). Sharpness was
  measured on a training dummy (+10% per hit on a late-game build, before this version's x1.12 rework).
- **Work in progress:** the new magic and summoning enchantments, the multiplier rework, and the level scaling
  are new in this version and have only passed the self-test (or not yet run in game). Knockback, Frost,
  Frostbite, Bossbane and Thorns haven't been confirmed in a fight yet. Blast Protection only reduces damage from
  your own and your allies' explosives. Numbers will change.

## Requirements

- CoreLib

## Limits

- Alpha: tested in single player only. Multiplayer is untested.
- Fishing rods, bags, lanterns, pouches, pets, bait, grenades and other stackable items can't be enchanted.
- The ring, necklace and offhand enchantments are new and only stat-checked by the self-test so far.
- Switching Enchanting off or removing it can leave the last active bonuses on the character.
- Summoning tome enchantments work while that tome is the last one you held and stays in your inventory.
- In the world the table uses the Upgrade Forge's shape with a violet glow, and its window is the Upgrade
  Forge's window.
- Uninstalling: enchantments stop working and their tooltips disappear; the items themselves are kept.

## Commands (host/admin)

- `/enchant` — show the held item's enchantments; add `helm`, `chest`, `pants`, `ring1`, `ring2`, `necklace`
  or `offhand` for worn gear.
- `/enchant set <name> <level>`, `/enchant remove <name>`, `/enchant clear`, `/enchant roll [n]` — testing.
- `/enchant selftest` — checks every enchantment of what you hold and wear against the game's stat totals.
- `/enchant combattest [seconds]` — stand next to a training dummy: attacks it with each enchantment in turn
  and reports damage, crit rate and status effects. `/enchant debug on` + `/enchant report` measure by hand.

## Development

- Enchantments are stored on the item in vanilla per-item data (the pet-talent list, unused on gear), and
  applied as permanent player conditions that the game sums into your stats (`EnchantEffectSystem`).
- `Unity/dump_vanilla.py`, `Unity/extract_forge_skin.py`, `Unity/make_art.py` and `Unity/build_bundle.py` build
  the table (a copy of the Upgrade Forge with a violet skin); `Unity/verify_bundle.py` checks the bundle.
- Design notes: `SPEC.md`, `docs/balance-model.md`, `docs/other-weapon-enchants.md`, `docs/test-plan.md`.
