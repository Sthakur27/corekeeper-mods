# Golden Chance

Core Keeper mod: multiplies what your talents give to golden plants and golden cooked food.

| Setting | Options | Default |
|---|---|---|
| Golden plant talent | 1x, 1.5x, 2x, 3x | 2x |
| Golden cooking talent | 1x, 1.5x, 2x, 3x | 2x |

Only the talent bonus is multiplied, so it never gives you something you have not earned:

- Golden plants: planting a seed rolls 3% + your Gardening talent bonus. The 3% base stays vanilla;
  only the talent part is multiplied. No points in the talent = vanilla 3%.
- Golden cooking: the Cooking talent's "chance for extra cooked food to be rare" is multiplied.
  No points = no golden food, same as vanilla.

The talent tooltip values are not changed (they show the vanilla per-point value). Changing a
setting applies right away. Works with Auto Replant (its replants use the same talent value).
In multiplayer each player's own setting applies to their own talents.

## How it works (for modders)

Harmony postfix on `SkillTalentsTable.GetConditionDataForSkillTalent` (managed). That is what builds
a talent's `ConditionData` when the character loads (`StartGameRPCSystem`) and when a point is spent
(`SkillTalentUIElement` -> `SetSkillTalentCondition` command), so the multiplied total lands in the
replicated `SkillTalentConditionsBuffer` and from there in `SummarizedConditionsBuffer`, which the
Burst golden rolls read (`PlaceObjectSlot` / `SeederSlot` [`ChanceToGainRarePlant`],
`InventoryUtility.IncreaseCookingSkillAndSpawnExtraFoodIfWeShould` [`ChanceForExtraCookedFoodToBeRare`]).
On a settings change the local player's golden talents are re-sent the same way.
