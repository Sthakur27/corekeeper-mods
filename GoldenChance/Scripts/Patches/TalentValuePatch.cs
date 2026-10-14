using HarmonyLib;
using UnityEngine;

namespace GoldenChance.Patches
{
    /// <summary>
    /// Adds a flat bonus once to the raw talent condition, including a zero-point talent.
    /// The existing replicated condition path feeds the Burst plant/cooking rolls; no Burst patching
    /// is needed. Cooking changes the rarity roll for extra food, not the extra-food spawn chance.
    /// </summary>
    [HarmonyPatch(typeof(SkillTalentsTable), nameof(SkillTalentsTable.GetConditionDataForSkillTalent))]
    public static class TalentValuePatch
    {
        public static void Postfix(ref ConditionData __result)
        {
            int bonus;
            switch (__result.conditionID)
            {
                case ConditionID.ChanceToGainRarePlant: bonus = GoldenChanceMod.PlantBonus; break;
                case ConditionID.ChanceForExtraCookedFoodToBeRare: bonus = GoldenChanceMod.CookingBonus; break;
                default: return;
            }
            // Leave vanilla and other mods' values untouched when the setting is +0%.
            if (bonus == 0) return;
            __result.value = Mathf.Clamp(__result.value + bonus, 0, 100);
        }

        public static void ResendTalentValues()
        {
            var player = Manager.main?.player;
            var table = Manager.mod?.SkillTalentsTable;
            if (player == null || !player.isLocal || table == null || Manager.saves == null
                || player.playerCommandSystem == null || player.world == null || !player.world.IsCreated
                || !player.world.EntityManager.Exists(player.entity)) return;
            foreach (var tree in table.skillTalentTrees)
            {
                var points = Manager.saves.GetSkillTalentTreesPoints(tree.skillID);
                for (int i = 0; i < tree.skillTalents.Count; i++)
                {
                    if (!IsGolden(tree.skillTalents[i].givesCondition)) continue;
                    // ResetTalentTree clears the list; missing entries mean zero points.
                    int count = points != null && i < points.Count ? points[i] : 0;
                    var data = table.GetConditionDataForSkillTalent(tree.skillID, i, count);
                    player.playerCommandSystem.SetSkillTalentCondition(player.entity, data);
                }
            }
        }

        private static bool IsGolden(ConditionID id) =>
            id == ConditionID.ChanceToGainRarePlant || id == ConditionID.ChanceForExtraCookedFoodToBeRare;
    }

    [HarmonyPatch(typeof(PlayerController), "PlayerInit")]
    internal static class TalentJoinPatch
    {
        public static void Postfix(PlayerController __instance)
        {
            if (__instance.isLocal) TalentValuePatch.ResendTalentValues();
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SetSkillTalentPoint))]
    internal static class TalentPointChangedPatch
    {
        public static void Postfix() => TalentValuePatch.ResendTalentValues();
    }

    // This is the server-confirmed reset callback, not the reset button click.
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.ResetTalentTree))]
    internal static class TalentResetConfirmedPatch
    {
        public static void Postfix() => TalentValuePatch.ResendTalentValues();
    }
}
