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
        private static object _lastPlayer;
        private static object _lastCommands;
        private static string _lastSignature;

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

        public static void ClearRefreshState()
        {
            _lastPlayer = null;
            _lastCommands = null;
            _lastSignature = null;
        }

        public static void ResendTalentValues() => RefreshTalentValues(true);

        /// <summary>
        /// Re-send on joining, changed settings, or changed talent points (including resets).
        /// Read the raw table again every time; never add a bonus to an already modified buffer value.
        /// </summary>
        public static void RefreshTalentValues(bool force = false)
        {
            var player = Manager.main?.player;
            var table = Manager.mod?.SkillTalentsTable;
            if (player == null || table == null || Manager.saves == null || player.playerCommandSystem == null)
            {
                ClearRefreshState();
                return;
            }

            var conditions = new System.Collections.Generic.List<ConditionData>();
            string signature = player.entity.ToString() + ":" + GoldenChanceMod.PlantBonus + ":" + GoldenChanceMod.CookingBonus;
            foreach (var tree in table.skillTalentTrees)
            {
                var points = Manager.saves.GetSkillTalentTreesPoints(tree.skillID);
                for (int i = 0; i < tree.skillTalents.Count; i++)
                {
                    if (!IsGolden(tree.skillTalents[i].givesCondition)) continue;
                    if (points == null || i >= points.Count) return; // character still loading
                    signature += ":" + tree.skillID + ":" + i + ":" + points[i];
                    conditions.Add(table.GetConditionDataForSkillTalent(tree.skillID, i, points[i]));
                }
            }
            if (!force && ReferenceEquals(_lastPlayer, player) && ReferenceEquals(_lastCommands, player.playerCommandSystem)
                && _lastSignature == signature) return;

            foreach (var data in conditions)
                player.playerCommandSystem.SetSkillTalentCondition(player.entity, data);
            _lastPlayer = player;
            _lastCommands = player.playerCommandSystem;
            _lastSignature = signature;
        }

        private static bool IsGolden(ConditionID id) =>
            id == ConditionID.ChanceToGainRarePlant || id == ConditionID.ChanceForExtraCookedFoodToBeRare;
    }
}
