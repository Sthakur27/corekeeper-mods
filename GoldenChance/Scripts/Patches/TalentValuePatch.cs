using HarmonyLib;
using UnityEngine;

namespace GoldenChance.Patches
{
    /// <summary>
    /// A talent's effect is ConditionData { givesCondition, conditionValuePerPoint * points }, built by
    /// SkillTalentsTable.GetConditionDataForSkillTalent (managed) when the character loads
    /// (StartGameRPCSystem) and when a point is spent (SkillTalentUIElement -> SetSkillTalentCondition
    /// command). It lands in the player's replicated SkillTalentConditionsBuffer, SummarizeConditionsSystem
    /// folds it into SummarizedConditionsBuffer, and the golden rolls read that:
    ///   plants  (PlaceObjectSlot / SeederSlot / Auto Replant): 3% + [ChanceToGainRarePlant]
    ///   cooking (InventoryUtility.IncreaseCookingSkillAndSpawnExtraFoodIfWeShould): [ChanceForExtraCookedFoodToBeRare] %
    /// Scaling the talent total here multiplies only what the talent gives (0 points stays 0), with one
    /// rounding per talent. Talent resets use the blob table with 0 points, so they are unaffected.
    /// </summary>
    [HarmonyPatch(typeof(SkillTalentsTable), nameof(SkillTalentsTable.GetConditionDataForSkillTalent))]
    public static class TalentValuePatch
    {
        public static void Postfix(ref ConditionData __result)
        {
            float mult = MultiplierFor(__result.conditionID);
            if (mult == 1f || __result.value <= 0) return;
            __result.value = Mathf.RoundToInt(__result.value * mult);
        }

        private static float MultiplierFor(ConditionID id)
        {
            switch (id)
            {
                case ConditionID.ChanceToGainRarePlant: return GoldenChanceMod.PlantMultiplier;
                case ConditionID.ChanceForExtraCookedFoodToBeRare: return GoldenChanceMod.CookingMultiplier;
                default: return 1f;
            }
        }

        /// <summary>
        /// After a settings change, re-send the local player's golden talents with the new multiplier,
        /// exactly as spending a talent point does, so it applies without rejoining.
        /// </summary>
        public static void ResendTalentValues()
        {
            var player = Manager.main?.player;
            var table = Manager.mod?.SkillTalentsTable;
            if (player == null || table == null || Manager.saves == null || player.playerCommandSystem == null) return;
            foreach (var tree in table.skillTalentTrees)
            {
                var points = Manager.saves.GetSkillTalentTreesPoints(tree.skillID);
                if (points == null) continue;
                for (int i = 0; i < tree.skillTalents.Count && i < points.Count; i++)
                {
                    if (!IsGolden(tree.skillTalents[i].givesCondition)) continue;
                    var data = table.GetConditionDataForSkillTalent(tree.skillID, i, points[i]); // patched above
                    player.playerCommandSystem.SetSkillTalentCondition(player.entity, data);
                    Debug.Log($"[{GoldenChanceMod.Name}] {tree.skillID} talent {i} ({data.conditionID}): {points[i]} pts -> {data.value}");
                }
            }
        }

        private static bool IsGolden(ConditionID id) =>
            id == ConditionID.ChanceToGainRarePlant || id == ConditionID.ChanceForExtraCookedFoodToBeRare;
    }
}
