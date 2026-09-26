namespace BuffDurationFloor
{
    /// <summary>Which floor (if any) applies to a condition entry.</summary>
    public enum ConditionGroup
    {
        /// <summary>Never extended: None, zero/infinite duration, permanent or negative.</summary>
        Skip,
        /// <summary>Ordinary positive timed buff; governed by "Minimum buff duration".</summary>
        Buff,
        /// <summary>Heal-over-time / mana-regen; governed by "Minimum healing duration".</summary>
        Healing
    }

    /// <summary>
    /// Decides which conditions the floors may extend and which of the two floors applies.
    /// </summary>
    public static class ConditionFilter
    {
        /// <summary>
        /// Classifies a condition entry using the game's ConditionsTable (effect, isPermanent,
        /// isNegative). Zero, infinite, permanent and negative entries are <see cref="ConditionGroup.Skip"/>;
        /// regeneration effects are <see cref="ConditionGroup.Healing"/>; everything else is <see cref="ConditionGroup.Buff"/>.
        /// </summary>
        public static ConditionGroup Classify(ConditionID id, float originalDuration, in ConditionsTableCD table)
        {
            if (id == ConditionID.None) return ConditionGroup.Skip;
            if (!(originalDuration > 0f) || float.IsInfinity(originalDuration)) return ConditionGroup.Skip;
            var info = table.GetConditionInfo(id);
            if (info.isPermanent || info.isNegative) return ConditionGroup.Skip;
            return IsRegeneration(id, info.effect) ? ConditionGroup.Healing : ConditionGroup.Buff;
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
