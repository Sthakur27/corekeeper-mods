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
        /// <summary>
        /// Conditions whose total of OUR entries (armor Sum + accessory Max, folded together) is clamped by
        /// <see cref="Cap"/>, so stacked defence stays near the damage budget (docs/balance-combos.md: best build
        /// ~+44% DPS / ~+44% effective HP late). Vanilla gear, food and skills come on top. Sid approved 2026-11-05.
        /// </summary>
        public static readonly ConditionID[] CappedConditions =
        {
            ConditionID.ReducedDamageTakenPercentage, ConditionID.IncreasedMaxHealthPercentage, ConditionID.DodgeChance,
            ConditionID.MovementSpeedIncrease, ConditionID.IncreasedHealthRegenEffectiveness, ConditionID.ReducedDamageFromBosses,
        };

        /// <summary>Our summed raw value for a condition after the caps above (raw units).</summary>
        public static int Cap(ConditionID id, int value)
        {
            switch (id)
            {
                case ConditionID.ReducedDamageTakenPercentage: return Mathf.Max(value, -120);      // tenths: −12% damage taken
                case ConditionID.IncreasedMaxHealthPercentage: return Mathf.Min(value, 15);        // whole: Vitality + Hardiness +15%
                case ConditionID.DodgeChance: return Mathf.Min(value, 8);                          // whole: +8 dodge
                case ConditionID.MovementSpeedIncrease: return Mathf.Min(value, 120);              // tenths: Swiftness + Fleetfoot +12%
                case ConditionID.IncreasedHealthRegenEffectiveness: return Mathf.Min(value, 60);   // whole: +60% regen
                case ConditionID.ReducedDamageFromBosses: return Mathf.Min(value, 9);              // whole: −9% boss damage
                default: return value;
            }
        }

        /// <summary>Glow conditions the player light reads (ConditionsEffectsHandler.UpdateCombinedLight), for Radiance.</summary>
        public static readonly ConditionID[] GlowConditions =
        {
            ConditionID.OrangeGlow, ConditionID.BlueGlow, ConditionID.PinkGlow, ConditionID.GreenGlow, ConditionID.VoidGlow,
        };

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

        /// <summary>
        /// The item whose type decides a Relative enchant's bucket: the enchanted weapon itself, or for accessory
        /// buckets (Might, Haste) the weapon the player currently holds.
        /// </summary>
        public static ObjectID BucketWeapon(EnchantDef def, ObjectID item, ObjectID held) =>
            def.Bucket == RelBucket.AllDamage || def.Bucket == RelBucket.AllAttackSpeed ? held : item;

        /// <summary>Damage type effect the damage code adds for this weapon (EntityUtility.GetAttackerDamageIncrease).</summary>
        public static ConditionEffect DamageTypeEffect(ObjectID weapon)
        {
            if (Enchants.IsMagicDamage(weapon)) return ConditionEffect.MagicDamage;
            return Enchants.IsRangeDamage(weapon) ? ConditionEffect.PhysicalRangeDamage : ConditionEffect.PhysicalMeleeDamage;
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
                case ConditionID.AllDamageIncrease: return ConditionEffect.AllDamage;
                case ConditionID.AttackSpeed: return ConditionEffect.AttackSpeed;
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
                // Accessory Might: AllDamage (33) + type (8 / 22 / 102) + class (116 / 117) of the held weapon, the
                // same terms EntityUtility.GetAttackerDamageIncrease adds before x(1 + sum/1000).
                case RelBucket.AllDamage:
                    return Fx(fx, ConditionEffect.AllDamage) + Fx(fx, DamageTypeEffect(weapon))
                        + Fx(fx, Enchants.IsRangeDamage(weapon) ? ConditionEffect.RangeDamage : ConditionEffect.MeleeDamage);
                // Accessory Haste: AttackSpeed (65) + Melee- (40) or RangeAttackSpeed (41) of the held weapon, the two
                // terms of the cooldown divisor (MeleeWeaponSlot / RangeWeaponSlot / BeamWeaponSlot).
                case RelBucket.AllAttackSpeed:
                    return Fx(fx, ConditionEffect.AttackSpeed)
                        + Fx(fx, Enchants.UsesRangeCooldown(weapon) ? ConditionEffect.RangeAttackSpeed : ConditionEffect.MeleeAttackSpeed);
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
                case RelBucket.AllDamage: return "damage";
                case RelBucket.AllAttackSpeed: return "attack speed";
                default: return "";
            }
        }

        /// <summary>Every ConditionID an enchant can write (for the units log).</summary>
        public static IEnumerable<ConditionID> AllWrittenConditions()
        {
            foreach (var def in Enchants.All)
            {
                yield return def.Condition;
                if (def.FollowsLight)
                    foreach (var glow in GlowConditions) yield return glow;
                if (def.Bucket == RelBucket.AttackSpeed)
                {
                    yield return ConditionID.MeleeAttackSpeedIncrease;
                    yield return ConditionID.RangeAttackSpeedIncrease;
                }
            }
        }
    }
}
