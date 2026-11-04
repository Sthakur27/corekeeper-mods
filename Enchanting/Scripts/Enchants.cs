using System;
using System.Collections.Generic;

namespace Enchanting
{
    /// <summary>Which items an enchant can land on (by vanilla ObjectType).</summary>
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
        Armor = Helm | Chest | Pants,
    }

    public sealed class EnchantDef
    {
        public int Id;            // stable save id, never reuse
        public string Key;        // command name
        public string Name;       // display name
        public EnchantSlot Slots;
        public ConditionID Condition;
        public int[] Values;      // per level, in the units the game's tooltip shows (EnchantUnits converts)

        public int MaxLevel => Values.Length;
    }

    /// <summary>
    /// The enchant catalogue. Values are first guesses in displayed units ("+5%" = 5); the item tooltip renders them
    /// with the game's own condition text, so a wrong unit shows up immediately in game.
    /// </summary>
    public static class Enchants
    {
        public static readonly List<EnchantDef> All = new List<EnchantDef>();
        private static readonly Dictionary<int, EnchantDef> ById = new Dictionary<int, EnchantDef>();
        private static readonly Dictionary<string, EnchantDef> ByKey = new Dictionary<string, EnchantDef>(StringComparer.OrdinalIgnoreCase);

        static Enchants()
        {
            // Melee
            Add(1, "sharpness", "Sharpness", EnchantSlot.Melee, ConditionID.PhysicalMeleeDamageIncrease, 5, 10, 15);
            Add(2, "fireaspect", "Fire Aspect", EnchantSlot.Melee, ConditionID.ApplyBurning, 10, 20, 30);
            Add(3, "knockback", "Knockback", EnchantSlot.Melee, ConditionID.ChanceOnHitToKnockback, 10, 20, 30);
            Add(4, "lifesteal", "Lifesteal", EnchantSlot.Melee, ConditionID.LifeOnHit, 2, 4, 6);
            Add(5, "venom", "Venom", EnchantSlot.Melee, ConditionID.ChanceToApplyPoisoned, 10, 20, 30);
            Add(6, "stunning", "Stunning", EnchantSlot.Melee, ConditionID.ChanceOnMeleeHitToStun, 5, 10, 15);
            Add(7, "swiftblade", "Swiftblade", EnchantSlot.Melee, ConditionID.MeleeAttackSpeedIncrease, 5, 10, 15);
            Add(8, "keen", "Keen", EnchantSlot.Melee | EnchantSlot.Ranged, ConditionID.CritChance, 3, 6, 9);
            Add(9, "bossbane", "Bossbane", EnchantSlot.Melee | EnchantSlot.Ranged, ConditionID.IncreasedDamageAgainstBosses, 5, 10, 15);

            // Ranged
            Add(20, "power", "Power", EnchantSlot.Ranged, ConditionID.PhysicalRangeDamageIncrease, 5, 10, 15);
            Add(21, "quickdraw", "Quick Draw", EnchantSlot.Ranged, ConditionID.RangeAttackSpeedIncrease, 5, 10, 15);
            Add(22, "frost", "Frost", EnchantSlot.Ranged, ConditionID.ChanceOnRangeShotToApplySlowedBySlime, 10, 20, 30);
            Add(23, "stunshot", "Stunshot", EnchantSlot.Ranged, ConditionID.ChanceOnRangeShotToStun, 5, 10, 15);

            // Tools
            Add(40, "efficiency", "Efficiency", EnchantSlot.Tool, ConditionID.MiningIncrease, 10, 20, 30);
            Add(41, "unbreaking", "Unbreaking", EnchantSlot.Tool, ConditionID.ToolDurabilityLastsLonger, 15, 30, 45);
            Add(42, "fortune", "Fortune", EnchantSlot.Tool, ConditionID.ChanceForRandomLootFromWall, 2, 4, 6);
            Add(43, "mending", "Mending", EnchantSlot.Tool, ConditionID.DurabilityIncreaseAfterMiningOre, 1, 2, 3);
            // 44 Prospector removed (2026-11-04); ids are never reused.

            // Armor
            Add(60, "protection", "Protection", EnchantSlot.Armor, ConditionID.ArmorIncrease, 2, 4, 6);
            Add(61, "thorns", "Thorns", EnchantSlot.Armor, ConditionID.ThornsDamage, 10, 20, 30);
            Add(62, "vitality", "Vitality", EnchantSlot.Armor, ConditionID.IncreasedMaxHealth, 10, 20, 30);
            Add(63, "regeneration", "Regeneration", EnchantSlot.Armor, ConditionID.IncreasedHealthRegenEffectiveness, 10, 20, 30);
            Add(64, "evasion", "Evasion", EnchantSlot.Armor, ConditionID.DodgeChance, 2, 4, 6);
            Add(65, "bosswarden", "Bosswarden", EnchantSlot.Armor, ConditionID.ReducedDamageFromBosses, 3, 6, 9);
            Add(66, "durable", "Unbreaking", EnchantSlot.Armor, ConditionID.EquipmentDurabilityLastsLonger, 15, 30, 45);
            Add(67, "swiftness", "Swiftness", EnchantSlot.Pants, ConditionID.MovementSpeedIncrease, 3, 6, 9);
            // 68 Magnetism and 80-82 fishing rod enchants (Lure, Luck of the Sea, Reach) removed (2026-11-04).
            // Ids are never reused; leftover entries on items are skipped when read.
        }

        private static void Add(int id, string key, string name, EnchantSlot slots, ConditionID condition, params int[] values)
        {
            var def = new EnchantDef { Id = id, Key = key, Name = name, Slots = slots, Condition = condition, Values = values };
            All.Add(def);
            ById[id] = def;
            ByKey[key] = def;
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
            var info = PugDatabase.GetObjectInfo(id);
            if (info == null) return EnchantSlot.None;
            switch (info.objectType)
            {
                case ObjectType.MeleeWeapon: return EnchantSlot.Melee;
                case ObjectType.RangeWeapon: return EnchantSlot.Ranged;
                case ObjectType.MiningPick:
                case ObjectType.Shovel:
                case ObjectType.Hoe:
                case ObjectType.Sledge:
                case ObjectType.DrillTool: return EnchantSlot.Tool;
                case ObjectType.Helm: return EnchantSlot.Helm;
                case ObjectType.BreastArmor: return EnchantSlot.Chest;
                case ObjectType.PantsArmor: return EnchantSlot.Pants;
                default: return EnchantSlot.None; // fishing rods: no enchants since 2026-11-04
            }
        }

        /// <summary>Held-in-hand items (weapons, tools, rods) apply their enchants only while selected.</summary>
        public static bool IsHeldSlot(EnchantSlot s) => (s & (EnchantSlot.Melee | EnchantSlot.Ranged | EnchantSlot.Tool | EnchantSlot.Rod)) != 0;

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
