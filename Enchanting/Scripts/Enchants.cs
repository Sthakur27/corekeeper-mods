using System;
using System.Collections.Generic;

namespace Enchanting
{
    /// <summary>Which items an enchant can land on (by vanilla ObjectType, magic/range routing via HasWeaponDamageCD).</summary>
    [Flags]
    public enum EnchantSlot
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Tool = 4,
        Helm = 8,
        Chest = 16,
        Pants = 32,
        Rod = 64,
        Magic = 128,   // RangeWeapon / BeamWeapon / ThrowingWeapon with HasWeaponDamageCD.isMagic
        Summon = 256,  // SummoningWeapon (tomes): "carried" rule, see EnchantEffectSystem
        Armor = Helm | Chest | Pants,
    }

    /// <summary>How an enchant's level turns into the raw condition value (docs/balance-model.md).</summary>
    public enum EnchantMode
    {
        /// <summary>Values[level] in displayed units, ×10 for tenths conditions (EnchantUnits).</summary>
        Flat,
        /// <summary>
        /// Model B: Values[level] is a percent multiplier on the player's current bucket (12 = ×1.12). The raw
        /// value is recomputed on the server from the summed bucket without our own entry (EnchantMath.Relative).
        /// </summary>
        Relative,
        /// <summary>Model A: Values[level] is a percent of the vanilla curve at the item's level (100 = one vanilla affix).</summary>
        ItemLevel,
    }

    /// <summary>The stat bucket a Relative enchant multiplies.</summary>
    public enum RelBucket
    {
        None,
        Damage,          // AllDamage + type (physical melee / physical range / magic) + class (melee / range)
        AttackSpeed,     // MeleeAttackSpeed or RangeAttackSpeed (by the held weapon) + AttackSpeed
        MinionDamage,    // MinionDamagePercentage (+ magic-to-minion share)
        MinionAttackSpeed,
    }

    /// <summary>Vanilla per-level curves (ConditionExtensions) used by ItemLevel enchants.</summary>
    public enum ItemCurve
    {
        None,
        Burning,   // 10 + 3L
        LifeOnHit, // max(1, 0.5L)
        Thorns,    // 1 + 2L
    }

    public sealed class EnchantDef
    {
        public int Id;            // stable save id, never reuse
        public string Key;        // command name
        public string Name;       // display name
        public EnchantSlot Slots;
        public ConditionID Condition;
        public int[] Values;      // per level; meaning depends on Mode (see EnchantMode)
        public EnchantMode Mode = EnchantMode.Flat;
        public RelBucket Bucket = RelBucket.None;
        public ItemCurve Curve = ItemCurve.None;

        public int MaxLevel => Values.Length;
    }

    /// <summary>
    /// The enchant catalogue (SPEC.md "Enchant pools"). Flat values are in displayed units ("+5%" = 5); the item
    /// tooltip renders them with the game's own condition text, so a wrong unit shows up immediately in game.
    /// </summary>
    public static class Enchants
    {
        public static readonly List<EnchantDef> All = new List<EnchantDef>();
        private static readonly Dictionary<int, EnchantDef> ById = new Dictionary<int, EnchantDef>();
        private static readonly Dictionary<string, EnchantDef> ByKey = new Dictionary<string, EnchantDef>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ObjectID, EnchantSlot> SlotCache = new Dictionary<ObjectID, EnchantSlot>();

        private const EnchantSlot Weapons = EnchantSlot.Melee | EnchantSlot.Ranged | EnchantSlot.Magic;

        static Enchants()
        {
            // Melee
            Rel(1, "sharpness", "Sharpness", EnchantSlot.Melee, ConditionID.PhysicalMeleeDamageIncrease, RelBucket.Damage, 4, 8, 12);
            Lvl(2, "fireaspect", "Fire Aspect", EnchantSlot.Melee, ConditionID.ApplyBurning, ItemCurve.Burning, 50, 75, 100);
            Add(3, "knockback", "Knockback", EnchantSlot.Melee, ConditionID.ChanceOnHitToKnockback, 10, 20, 30);
            Lvl(4, "lifesteal", "Lifesteal", EnchantSlot.Melee, ConditionID.LifeOnHit, ItemCurve.LifeOnHit, 50, 100, 150);
            Add(5, "venom", "Venom", EnchantSlot.Melee, ConditionID.ChanceToApplyPoisoned, 10, 20, 30);
            Add(6, "stunning", "Stunning", EnchantSlot.Melee, ConditionID.ChanceOnMeleeHitToStun, 3, 6, 9);
            Rel(7, "swiftblade", "Swiftblade", EnchantSlot.Melee, ConditionID.MeleeAttackSpeedIncrease, RelBucket.AttackSpeed, 3, 6, 10);
            Add(8, "keen", "Keen", Weapons, ConditionID.CritChance, 3, 6, 9);
            Add(9, "bossbane", "Bossbane", Weapons, ConditionID.IncreasedDamageAgainstBosses, 5, 10, 15);

            // Ranged (bows, non-stackable throwables, non-magic range beams)
            Rel(20, "power", "Power", EnchantSlot.Ranged, ConditionID.PhysicalRangeDamageIncrease, RelBucket.Damage, 4, 8, 12);
            // Quick Draw writes Range- or MeleeAttackSpeedIncrease, whichever cooldown the held weapon uses.
            Rel(21, "quickdraw", "Quick Draw", EnchantSlot.Ranged | EnchantSlot.Magic, ConditionID.RangeAttackSpeedIncrease, RelBucket.AttackSpeed, 3, 6, 10);
            Add(22, "frost", "Frost", EnchantSlot.Ranged, ConditionID.ChanceOnRangeShotToApplySlowedBySlime, -10, -20, -30); // slow amount: must be negative (damage code applies it only when < 0)
            Add(23, "stunshot", "Stunshot", EnchantSlot.Ranged, ConditionID.ChanceOnRangeShotToStun, 3, 6, 9);

            // Tools
            Add(40, "efficiency", "Efficiency", EnchantSlot.Tool, ConditionID.MiningPercentageIncrease, 10, 20, 30); // ×(1 + v/100) on mining damage
            Add(41, "unbreaking", "Unbreaking", EnchantSlot.Tool, ConditionID.ToolDurabilityLastsLonger, 15, 30, 45);
            Add(42, "fortune", "Fortune", EnchantSlot.Tool, ConditionID.ChanceForRandomLootFromWall, 2, 4, 6);
            Add(43, "mending", "Mending", EnchantSlot.Tool, ConditionID.DurabilityIncreaseAfterMiningOre, 1, 2, 3);
            // 44 Prospector removed (2026-11-04); ids are never reused.

            // Armor
            // 60 keeps key "protection" so old items carry over; now "less damage taken" (×(1000 + v)/1000, v < 0).
            Add(60, "protection", "Resilience", EnchantSlot.Armor, ConditionID.ReducedDamageTakenPercentage, -3, -5, -8);
            Lvl(61, "thorns", "Thorns", EnchantSlot.Armor, ConditionID.ThornsDamage, ItemCurve.Thorns, 100, 200, 300);
            Add(62, "vitality", "Vitality", EnchantSlot.Armor, ConditionID.IncreasedMaxHealthPercentage, 3, 6, 10);
            Add(63, "regeneration", "Regeneration", EnchantSlot.Armor, ConditionID.IncreasedHealthRegenEffectiveness, 10, 20, 30);
            Add(64, "evasion", "Evasion", EnchantSlot.Armor, ConditionID.DodgeChance, 2, 4, 6);
            Add(65, "bosswarden", "Bosswarden", EnchantSlot.Armor, ConditionID.ReducedDamageFromBosses, 3, 6, 9);
            Add(66, "durable", "Unbreaking", EnchantSlot.Armor, ConditionID.EquipmentDurabilityLastsLonger, 15, 30, 45);
            Add(67, "swiftness", "Swiftness", EnchantSlot.Pants, ConditionID.MovementSpeedIncrease, 3, 6, 9);
            // Friendly explosions only (own/allied bombs): PlayerController damage path, vanilla cap 90.
            Add(69, "blastprotection", "Blast Protection", EnchantSlot.Armor, ConditionID.ReducedDamageFromExplosions, 10, 20, 30);
            // 68 Magnetism and 80-82 fishing rod enchants (Lure, Luck of the Sea, Reach) removed (2026-11-04).
            // Ids are never reused; leftover entries on items are skipped when read.

            // Magic (staffs, scepters, magic beams): ids 100-119
            Rel(100, "arcanepower", "Arcane Power", EnchantSlot.Magic, ConditionID.IncreasedMagicDamagePercentage, RelBucket.Damage, 4, 8, 12);
            Add(101, "frugality", "Frugality", EnchantSlot.Magic, ConditionID.ReduceManaCostByPercentage, 5, 10, 15);
            Add(102, "spellsurge", "Spellsurge", EnchantSlot.Magic, ConditionID.GainMagicDamagePercentageBoostAfterCrit, 10, 20, 30);
            Add(103, "clarity", "Clarity", EnchantSlot.Magic, ConditionID.GainManaCostOnCrit, 25, 50, 75);
            // Movement slow on every hit for 4 s; must be negative (EntityUtility applies it only when < 0).
            // Vanilla gear uses -25 raw per item level, so -50..-150 raw is mid-game strength.
            Add(104, "frostbite", "Frostbite", EnchantSlot.Magic, ConditionID.ApplySlowedByCold, -5, -10, -15);

            // Summoning tomes: ids 120-139 (carried rule)
            Rel(120, "packleader", "Pack Leader", EnchantSlot.Summon, ConditionID.IncreasedMinionDamagePercentage, RelBucket.MinionDamage, 4, 8, 12);
            Rel(121, "frenzy", "Frenzy", EnchantSlot.Summon, ConditionID.IncreasedMinionAttackSpeed, RelBucket.MinionAttackSpeed, 3, 6, 10);
            Add(122, "keenfangs", "Keen Fangs", EnchantSlot.Summon, ConditionID.IncreasedMinionCritChance, 5, 10, 15);
            Add(123, "slayer", "Slayer", EnchantSlot.Summon, ConditionID.IncreasedMinionBossDamagePercentage, 10, 20, 30);
        }

        private static EnchantDef Add(int id, string key, string name, EnchantSlot slots, ConditionID condition, params int[] values)
        {
            var def = new EnchantDef { Id = id, Key = key, Name = name, Slots = slots, Condition = condition, Values = values };
            All.Add(def);
            ById[id] = def;
            ByKey[key] = def;
            return def;
        }

        private static void Rel(int id, string key, string name, EnchantSlot slots, ConditionID condition, RelBucket bucket, params int[] percent)
        {
            var def = Add(id, key, name, slots, condition, percent);
            def.Mode = EnchantMode.Relative;
            def.Bucket = bucket;
        }

        private static void Lvl(int id, string key, string name, EnchantSlot slots, ConditionID condition, ItemCurve curve, params int[] percentOfVanilla)
        {
            var def = Add(id, key, name, slots, condition, percentOfVanilla);
            def.Mode = EnchantMode.ItemLevel;
            def.Curve = curve;
        }

        public static EnchantDef Get(int id) => ById.TryGetValue(id, out var d) ? d : null;

        public static EnchantDef Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (ByKey.TryGetValue(key, out var d)) return d;
            string flat = key.Replace(" ", "").Replace("_", "").Replace("-", "");
            return ByKey.TryGetValue(flat, out d) ? d : null;
        }

        /// <summary>The enchant slot an item type belongs to (None = not enchantable).</summary>
        public static EnchantSlot SlotOf(ObjectID id)
        {
            if (id == ObjectID.None) return EnchantSlot.None;
            if (SlotCache.TryGetValue(id, out var cached)) return cached;
            var info = PugDatabase.GetObjectInfo(id);
            if (info == null) return EnchantSlot.None;
            EnchantSlot slot;
            switch (info.objectType)
            {
                case ObjectType.MeleeWeapon: slot = EnchantSlot.Melee; break;
                case ObjectType.RangeWeapon: slot = ByDamageType(id, EnchantSlot.Ranged); break;
                case ObjectType.BeamWeapon: slot = ByDamageType(id, EnchantSlot.Ranged); break;
                // Stackable throwables (grenades, bombs) share aux data per stack: not enchantable.
                case ObjectType.ThrowingWeapon: slot = info.isStackable ? EnchantSlot.None : ByDamageType(id, EnchantSlot.Ranged); break;
                case ObjectType.SummoningWeapon: slot = EnchantSlot.Summon; break;
                case ObjectType.MiningPick:
                case ObjectType.Shovel:
                case ObjectType.Hoe:
                case ObjectType.Sledge:
                case ObjectType.DrillTool: slot = EnchantSlot.Tool; break;
                case ObjectType.Helm: slot = EnchantSlot.Helm; break;
                case ObjectType.BreastArmor: slot = EnchantSlot.Chest; break;
                case ObjectType.PantsArmor: slot = EnchantSlot.Pants; break;
                default: slot = EnchantSlot.None; break; // fishing rods: no enchants since 2026-11-04; CastingItem is a recipe scroll
            }
            SlotCache[id] = slot;
            return slot;
        }

        // Magic → Magic pool; otherwise HasWeaponDamageCD.isRange picks Ranged/Melee (beams can be either).
        private static EnchantSlot ByDamageType(ObjectID id, EnchantSlot fallback)
        {
            if (!PugDatabase.TryGetComponent<HasWeaponDamageCD>(id, out var w)) return fallback;
            if (w.isMagic) return EnchantSlot.Magic;
            return w.isRange ? EnchantSlot.Ranged : EnchantSlot.Melee;
        }

        /// <summary>
        /// True when the weapon's cooldown uses RangeAttackSpeed (bows, staffs, throwables, range beams), false for
        /// MeleeAttackSpeed (melee weapons, non-range beams). RangeWeaponSlot always reads range speed; BeamWeaponSlot
        /// picks by HasWeaponDamageCD.isRange.
        /// </summary>
        public static bool UsesRangeCooldown(ObjectID id)
        {
            var info = PugDatabase.GetObjectInfo(id);
            if (info == null) return false;
            switch (info.objectType)
            {
                case ObjectType.RangeWeapon:
                case ObjectType.ThrowingWeapon: return true;
                case ObjectType.BeamWeapon: return PugDatabase.TryGetComponent<HasWeaponDamageCD>(id, out var w) && w.isRange;
                default: return false;
            }
        }

        /// <summary>HasWeaponDamageCD.isRange of the weapon (decides the melee/range damage class effect).</summary>
        public static bool IsRangeDamage(ObjectID id)
        {
            if (PugDatabase.TryGetComponent<HasWeaponDamageCD>(id, out var w)) return w.isRange;
            var info = PugDatabase.GetObjectInfo(id);
            return info != null && (info.objectType == ObjectType.RangeWeapon || info.objectType == ObjectType.ThrowingWeapon);
        }

        /// <summary>
        /// Held-in-hand items (weapons, tools, rods) apply their enchants only while selected. Summoning tomes are
        /// not "held" items: they use the carried rule in EnchantEffectSystem.
        /// </summary>
        public static bool IsHeldSlot(EnchantSlot s) => (s & (EnchantSlot.Melee | EnchantSlot.Ranged | EnchantSlot.Magic | EnchantSlot.Tool | EnchantSlot.Rod)) != 0;

        public static List<EnchantDef> PoolFor(EnchantSlot slot)
        {
            var list = new List<EnchantDef>();
            if (slot == EnchantSlot.None) return list;
            foreach (var d in All)
                if ((d.Slots & slot) != 0) list.Add(d);
            return list;
        }

        public static string Roman(int level)
        {
            switch (level)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return level.ToString();
            }
        }
    }
}
