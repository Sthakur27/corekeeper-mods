using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Raw values for the balance model (docs/balance-model.md). Shared by EnchantEffectSystem (what gets
    /// written), EnchantSelfTest (what is expected) and EnchantTooltip (what the player sees), so all three agree.
    ///   Flat:      EnchantUnits.Raw (displayed value, ×10 for tenths conditions).
    ///   ItemLevel: Model A, percent of the vanilla curve at the enchanted item's level.
    ///   Relative:  Model B, raw = p × (1000 + others), others = the player's bucket without our own entry, so
    ///              the bucket's factor becomes (1 + others/1000) × (1 + p).
    /// </summary>
    public static class EnchantMath
    {
        /// <summary>Lower clamp for our total ReducedDamageTakenPercentage (vanilla has no lower cap): −25%.</summary>
        public const int MinDamageTaken = -250;

        /// <summary>
        /// The item's level as the stat system uses it (SummarizeConditionsSystem): variation if set (capped at
        /// the max level), else the prefab's LevelCD.
        /// </summary>
        public static int ItemLevel(ObjectDataCD data)
        {
            if (data.variation > 0) return Mathf.Min(data.variation, LevelScaling.GetMaxLevel());
            return PugDatabase.TryGetComponent<LevelCD>(data.objectID, out var lvl) ? lvl.level : 0;
        }

        /// <summary>Flat and ItemLevel enchants (Relative ones need the player's summary: use <see cref="Relative"/>).</summary>
        public static int Raw(EnchantDef def, int level, ObjectDataCD item)
        {
            if (def.Mode == EnchantMode.ItemLevel) return LevelScaled(def, level, ItemLevel(item));
            return EnchantUnits.Raw(def, level);
        }

        public static int LevelScaled(EnchantDef def, int level, int itemLevel)
        {
            int lvl = Mathf.Clamp(level, 1, def.MaxLevel);
            float vanilla = Curve(def.Curve, itemLevel);
            return Mathf.Max(1, Mathf.RoundToInt(def.Values[lvl - 1] / 100f * vanilla));
        }

        // ConditionExtensions per-level values for player gear (decompiled, docs/balance-model.md §1.4).
        private static float Curve(ItemCurve curve, int L)
        {
            switch (curve)
            {
                case ItemCurve.Burning: return 10f + 3f * L;
                case ItemCurve.LifeOnHit: return Mathf.Max(1f, 0.5f * L);
                case ItemCurve.Thorns: return 1f + 2f * L;
                default: return 0f;
            }
        }

        /// <summary>Model B multiplier in permille (12% → 120).</summary>
        public static int Permille(EnchantDef def, int level) => def.Values[Mathf.Clamp(level, 1, def.MaxLevel) - 1] * 10;

        /// <summary>
        /// The ConditionID a Relative enchant writes. Attack speed follows the held weapon's cooldown (melee or range),
        /// everything else writes the enchant's own condition.
        /// </summary>
        public static ConditionID WriteCondition(EnchantDef def, ObjectID weapon)
        {
            if (def.Bucket == RelBucket.AttackSpeed)
                return Enchants.UsesRangeCooldown(weapon) ? ConditionID.RangeAttackSpeedIncrease : ConditionID.MeleeAttackSpeedIncrease;
            return def.Condition;
        }

        /// <summary>The ConditionEffect a write condition lands in (from the ConditionsTable by name; selftest checks it).</summary>
        public static ConditionEffect EffectOf(ConditionID id)
        {
            switch (id)
            {
                case ConditionID.PhysicalMeleeDamageIncrease: return ConditionEffect.PhysicalMeleeDamage;
                case ConditionID.PhysicalRangeDamageIncrease: return ConditionEffect.PhysicalRangeDamage;
                case ConditionID.IncreasedMagicDamagePercentage: return ConditionEffect.MagicDamage;
                case ConditionID.MeleeAttackSpeedIncrease: return ConditionEffect.MeleeAttackSpeed;
                case ConditionID.RangeAttackSpeedIncrease: return ConditionEffect.RangeAttackSpeed;
                case ConditionID.IncreasedMinionDamagePercentage: return ConditionEffect.MinionDamagePercentage;
                case ConditionID.IncreasedMinionAttackSpeed: return ConditionEffect.MinionAttackSpeed;
                default: return ConditionEffect.None;
            }
        }

        /// <summary>
        /// The player's current bucket total (raw, tenths) that a Relative enchant multiplies, read from the vanilla
        /// summary. Mirrors EntityUtility.GetAttackerDamageIncrease, the attack-speed cooldown code and
        /// MinionExtensions.GetMinionDamage / TouchAttackStateSystem.
        /// </summary>
        public static int BucketSum(EnchantDef def, ConditionID write, ObjectID weapon,
            DynamicBuffer<SummarizedConditionEffectsBuffer> fx, DynamicBuffer<SummarizedConditionsBuffer> conds)
        {
            switch (def.Bucket)
            {
                case RelBucket.Damage:
                    return Fx(fx, ConditionEffect.AllDamage) + Fx(fx, EffectOf(write))
                        + Fx(fx, Enchants.IsRangeDamage(weapon) ? ConditionEffect.RangeDamage : ConditionEffect.MeleeDamage);
                case RelBucket.AttackSpeed:
                    return Fx(fx, EffectOf(write)) + Fx(fx, ConditionEffect.AttackSpeed);
                case RelBucket.MinionDamage:
                {
                    int share = (int)ConditionID.IncreasedMagicDamageToMinionDamage < conds.Length ? conds[(int)ConditionID.IncreasedMagicDamageToMinionDamage].value : 0;
                    return Fx(fx, ConditionEffect.MinionDamagePercentage) + Mathf.RoundToInt(share / 100f * Fx(fx, ConditionEffect.MagicDamage));
                }
                case RelBucket.MinionAttackSpeed:
                    return Fx(fx, ConditionEffect.MinionAttackSpeed);
                default:
                    return 0;
            }
        }

        private static int Fx(DynamicBuffer<SummarizedConditionEffectsBuffer> fx, ConditionEffect e)
        {
            int i = (int)e;
            return e != ConditionEffect.None && i < fx.Length ? fx[i].value : 0;
        }

        /// <summary>
        /// Model B: raw value so that the bucket's factor is multiplied by (1 + permille/1000).
        /// <paramref name="others"/> = bucket total without our own entry. A debuffed bucket counts as 0, so the
        /// enchant never gives less than its nominal points.
        /// </summary>
        public static int Relative(int permille, int others)
        {
            others = Mathf.Max(0, others);
            return Mathf.RoundToInt(permille * (1000f + others) / 1000f);
        }

        /// <summary>
        /// Hysteresis for Relative values: keep the written value unless the new one differs by at least
        /// max(2 raw, 2%). Stacking on-hit buffs move the bucket every hit; each ConditionsBuffer write costs a
        /// re-summary and a ghost update.
        /// </summary>
        public static bool CloseEnough(int current, int wanted)
        {
            int diff = Mathf.Abs(wanted - current);
            return diff < Mathf.Max(2, Mathf.Abs(current) * 2 / 100);
        }

        /// <summary>Tooltip line for a Relative enchant: "x1.12 melee damage".</summary>
        public static string MultiplierText(EnchantDef def, int level, ObjectID weapon)
        {
            int pct = def.Values[Mathf.Clamp(level, 1, def.MaxLevel) - 1];
            string factor = (1f + pct / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            return $"x{factor} {Label(def, weapon)}";
        }

        private static string Label(EnchantDef def, ObjectID weapon)
        {
            switch (def.Bucket)
            {
                case RelBucket.Damage:
                    switch (def.Condition)
                    {
                        case ConditionID.PhysicalMeleeDamageIncrease: return "melee damage";
                        case ConditionID.PhysicalRangeDamageIncrease: return "ranged damage";
                        case ConditionID.IncreasedMagicDamagePercentage: return "magic damage";
                        default: return "damage";
                    }
                case RelBucket.AttackSpeed: return Enchants.UsesRangeCooldown(weapon) ? "attack speed (ranged)" : "attack speed (melee)";
                case RelBucket.MinionDamage: return "minion damage";
                case RelBucket.MinionAttackSpeed: return "minion attack speed";
                default: return "";
            }
        }

        /// <summary>Every ConditionID an enchant can write (for the units log).</summary>
        public static IEnumerable<ConditionID> AllWrittenConditions()
        {
            foreach (var def in Enchants.All)
            {
                yield return def.Condition;
                if (def.Bucket == RelBucket.AttackSpeed)
                {
                    yield return ConditionID.MeleeAttackSpeedIncrease;
                    yield return ConditionID.RangeAttackSpeedIncrease;
                }
            }
        }
    }
}
