using System.Collections.Generic;
using HarmonyLib;

namespace SkillXPMultiplier
{
    /// <summary>
    /// How many talent points a skill has earned in total once it reaches its max level (100).
    /// Points are handed out evenly on the way there: a skill at level L has floor(L * total / max).
    /// "Vanilla" keeps the game's own curve (1 per 5 levels, +5 at level 100 = 25).
    /// </summary>
    public static class TalentPoints
    {
        public const string VanillaToken = "Vanilla";
        public const string DefaultToken = "40";

        /// <summary>A tree holds 8 talents of 5 points each, so more than 40 points can never be placed.</summary>
        public const int MaxUseful = 40;

        /// <summary>Vanilla already gives 25, so only totals above it are offered.</summary>
        public const int MinUseful = 30;

        /// <summary>Choices offered in the Mod Options menu, in cycle order.</summary>
        public static readonly string[] Ladder =
        {
            VanillaToken, "30", "35", "40"
        };

        /// <summary>This player's own setting: total points at max level; -1 means the game's own curve.</summary>
        public static int Total = 40;

        /// <summary>The total in effect: the host's when joined to a world where the host decides, else <see cref="Total"/>.</summary>
        public static int Effective => MultiplayerSync.HostOverrides ? MultiplayerSync.HostTalentTotal : Total;

        /// <summary>"40" → 40 (above 40 is capped); "Vanilla", anything below 30 or unparseable → -1.</summary>
        public static int Parse(string token)
        {
            return int.TryParse(token, out int v) ? Clamp(v) : -1;
        }

        /// <summary>Below 30 → -1 (vanilla), above 40 → 40.</summary>
        public static int Clamp(int total)
        {
            if (total < MinUseful) return -1;
            return total > MaxUseful ? MaxUseful : total;
        }

        /// <summary>Points earned at <paramref name="level"/> out of <paramref name="maxLevel"/>.</summary>
        public static int Earned(int level, int maxLevel, int total)
        {
            if (maxLevel <= 0) return 0;
            if (level > maxLevel) level = maxLevel;
            return (int)((long)level * total / maxLevel);
        }
    }

    /// <summary>
    /// SaveManager.GetAvailableTalentPoints is the only place the game works out how many points a
    /// skill tree has left (talents themselves are capped at 5 points each by the UI). It is client-side,
    /// so nothing on the server disagrees with the new total. In multiplayer the host can decide it for
    /// everyone (<see cref="MultiplayerSync"/>).
    ///
    /// Lowering the total below what is already spent makes the result negative; the game handles that
    /// itself: opening that skill's talent tree resets it for free (SkillTalentTreeUI.UpdateTree,
    /// forceReset) and the points can be placed again.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetAvailableTalentPoints))]
    public static class TalentPointsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(SaveManager __instance, SkillID skillTreeID, ref int __result)
        {
            int total = TalentPoints.Effective;
            if (total < 0) return true;

            int level = SkillExtensions.GetLevelFromSkill(skillTreeID, __instance.GetSkillValue(skillTreeID));
            int earned = TalentPoints.Earned(level, SkillExtensions.GetMaxSkillLevel(skillTreeID), total);

            int spent = 0;
            List<int> placed = __instance.GetSkillTalentTreesPoints(skillTreeID);
            if (placed != null)
                for (int i = 0; i < placed.Count; i++) spent += placed[i];

            __result = earned - spent;
            return false;
        }
    }

    /// <summary>
    /// How many talent points a pet has in total at its max level (10). Like skills, points come in evenly:
    /// a pet at level L has floor(L * total / 10). "Vanilla" keeps the game's own 1 per 2 levels (5 at level 10).
    /// </summary>
    public static class PetTalentPoints
    {
        public const string DefaultToken = "9";

        /// <summary>A pet has 9 talent slots of 1 point each.</summary>
        public const int MaxUseful = 9;

        /// <summary>Vanilla already gives 5, so only totals above it are offered.</summary>
        public const int MinUseful = 6;

        public static readonly string[] Ladder =
        {
            TalentPoints.VanillaToken, "6", "7", "8", "9"
        };

        /// <summary>This player's own setting; -1 means the game's own curve.</summary>
        public static int Total = 9;

        /// <summary>The total in effect: the host's when joined to a world where the host decides, else <see cref="Total"/>.</summary>
        public static int Effective => MultiplayerSync.HostOverrides ? MultiplayerSync.HostPetTalentTotal : Total;

        public static int Parse(string token)
        {
            return int.TryParse(token, out int v) ? Clamp(v) : -1;
        }

        /// <summary>Below 6 → -1 (vanilla), above 9 → 9.</summary>
        public static int Clamp(int total)
        {
            if (total < MinUseful) return -1;
            return total > MaxUseful ? MaxUseful : total;
        }
    }

    /// <summary>
    /// PetExtensions.GetTotalTalentPoints is where the game turns a pet's XP into its talent points (the
    /// pet window subtracts what is placed). Client-side UI only, like skill talents. If a lower total
    /// leaves more placed than earned, the pet window's own reset button frees them.
    /// </summary>
    [HarmonyPatch(typeof(PetExtensions), nameof(PetExtensions.GetTotalTalentPoints))]
    public static class PetTalentPointsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int currentXp, ref int __result)
        {
            int total = PetTalentPoints.Effective;
            if (total < 0) return true;

            __result = TalentPoints.Earned(PetExtensions.GetLevelFromXP(currentXp), PetExtensions.maxLevel, total);
            return false;
        }
    }
}
