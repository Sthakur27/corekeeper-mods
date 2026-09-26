namespace BuffDurationFloor
{
    /// <summary>
    /// Decides which conditions the floor may extend. Shared by the prefab patch system and the
    /// runtime system for talent-injected conditions so both agree exactly.
    /// </summary>
    public static class ConditionFilter
    {
        /// <summary>
        /// True when a positive, timed buff with this id may be raised to the floor. Uses the game's
        /// ConditionsTable (effect, isPermanent, isNegative); regeneration effects are excluded
        /// unless <see cref="FloorSettings.ExtendHealing"/> is on.
        /// </summary>
        public static bool Qualifies(ConditionID id, float originalDuration, in ConditionsTableCD table)
        {
            if (id == ConditionID.None) return false;
            if (!(originalDuration > 0f) || float.IsInfinity(originalDuration)) return false;
            var info = table.GetConditionInfo(id);
            if (info.isPermanent || info.isNegative) return false;
            if (!FloorSettings.ExtendHealing && IsRegeneration(id, info.effect)) return false;
            return true;
        }

        /// <summary>
        /// Health or mana regeneration over time. Primary signal is the condition's effect in the
        /// game's table; the id-name check is a fallback for ids whose table entry is missing.
        /// </summary>
        public static bool IsRegeneration(ConditionID id, ConditionEffect effect)
        {
            switch (effect)
            {
                case ConditionEffect.HealOverTime:
                case ConditionEffect.HealOverTimePercentage:
                case ConditionEffect.ManaRegen:
                    return true;
            }
            string name = id.ToString();
            return name.Contains("HealOverTime")
                || name.Contains("HealingOverTime")
                || name.Contains("HealthRegen")
                || name.Contains("ManaRegen");
        }
    }
}
