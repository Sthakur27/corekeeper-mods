# Enchanting developer notes

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


## Earlier combat check

Sharpness was measured on a training dummy at +10% per hit on a late-game build, before the x1.12 multiplier rework. That result does not verify the current balance.
