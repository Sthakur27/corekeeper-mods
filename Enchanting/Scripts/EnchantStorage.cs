using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Enchanting
{
    public struct EnchantEntry
    {
        public int Id;
        public int Level;

        public EnchantDef Def => Enchants.Get(Id);
    }

    /// <summary>
    /// Enchants live in the item's vanilla PetTalentBuffer inventory aux data (a per-item list of (id, points) that is
    /// saved and replicated generically). The game only reads it for pet items, so on weapons/armor it is free.
    /// Encoding: petTalentID = <see cref="IdOffset"/> + enchant id, points = level. Shares the aux index with other
    /// per-item data (e.g. Armor Dye's MealsEatenCD), so both coexist.
    /// </summary>
    public static class EnchantStorage
    {
        public const int IdOffset = 1000;

        public static bool TryGetAux(EntityManager em, out InventoryAuxDataSystemDataCD aux)
        {
            aux = default;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<InventoryAuxDataSystemDataCD>());
            if (query.IsEmptyIgnoreFilter) return false;
            aux = query.GetSingleton<InventoryAuxDataSystemDataCD>();
            return true;
        }

        /// <summary>Server (or any world with the aux singleton): enchants of an item.</summary>
        public static List<EnchantEntry> Read(EntityManager em, InventoryAuxDataSystemDataCD aux, ContainedObjectsBuffer item)
        {
            var list = new List<EnchantEntry>();
            if (item.objectID == ObjectID.None || item.auxDataIndex == 0) return list;
            if (!aux.TryGetExtraInventoryBufferData(em, item.auxDataIndex, out DynamicBuffer<PetTalentBuffer> buffer)) return list;
            Decode(buffer, list);
            return list;
        }

        /// <summary>Client UI: enchants of an item shown in a slot.</summary>
        public static List<EnchantEntry> ReadClient(ContainedObjectsBuffer item)
        {
            var list = new List<EnchantEntry>();
            if (item.objectID == ObjectID.None || item.auxDataIndex == 0) return list;
            if (Enchants.SlotOf(item.objectID) == EnchantSlot.None) return list;
            try
            {
                if (!InventoryHandler.TryGetExtraInventoryBuffer(item, out DynamicBuffer<PetTalentBuffer> buffer)) return list;
                Decode(buffer, list);
            }
            catch (System.Exception)
            {
                // client world not ready (menus)
            }
            return list;
        }

        private static void Decode(DynamicBuffer<PetTalentBuffer> buffer, List<EnchantEntry> list)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                int id = (int)buffer[i].petTalentID - IdOffset;
                if (id <= 0 || Enchants.Get(id) == null || buffer[i].points <= 0) continue;
                list.Add(new EnchantEntry { Id = id, Level = buffer[i].points });
            }
        }

        /// <summary>
        /// Server: replaces the enchants of the item at absolute index <paramref name="slot"/> of
        /// <paramref name="inventory"/>'s ContainedObjectsBuffer. Returns null on success, else an error.
        /// </summary>
        public static string Write(EntityManager em, Entity inventory, int slot, List<EnchantEntry> enchants)
        {
            if (!TryGetAux(em, out var aux)) return "Inventory aux data not ready.";
            if (inventory == Entity.Null || !em.Exists(inventory) || !em.HasBuffer<ContainedObjectsBuffer>(inventory)) return "No inventory.";
            var items = em.GetBuffer<ContainedObjectsBuffer>(inventory);
            if (slot < 0 || slot >= items.Length || items[slot].objectID == ObjectID.None) return "That slot is empty.";
            var item = items[slot];
            if (Enchants.SlotOf(item.objectID) == EnchantSlot.None) return $"{item.objectID} can't be enchanted.";
            if (enchants.Count == 0 && item.auxDataIndex == 0) return null;

            int index = item.auxDataIndex;
            var buffer = aux.GetOrAllocateBuffer<PetTalentBuffer>(em, ref index);
            if (!buffer.IsCreated) return "Could not allocate enchant data.";
            buffer.Clear();
            foreach (var e in enchants)
                buffer.Add(new PetTalentBuffer { petTalentID = (PetTalent)(IdOffset + e.Id), points = e.Level });

            // Allocating instantiates an entity (structural change), so fetch the inventory again.
            items = em.GetBuffer<ContainedObjectsBuffer>(inventory);
            var again = items[slot];
            if (again.objectID != item.objectID) return "The item moved, try again.";
            if (again.auxDataIndex != index)
            {
                again.auxDataIndex = index;
                items[slot] = again;
            }
            Debug.Log($"[{EnchantingMod.Name}] {item.objectID} slot {slot} aux {item.auxDataIndex}->{index}: {Describe(enchants)}");
            return null;
        }

        public static string Describe(List<EnchantEntry> enchants)
        {
            if (enchants.Count == 0) return "no enchants";
            var parts = new List<string>();
            foreach (var e in enchants)
            {
                var d = e.Def;
                parts.Add(d == null ? $"#{e.Id}" : $"{d.Name} {Enchants.Roman(e.Level)}");
            }
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Converts displayed values to raw condition values for Flat enchants. Conditions the game applies as tenths
    /// of a percent (value/1000 in the damage, cooldown, armor and damage-taken code) store "+1.5%" as 15.
    /// <see cref="TenthsWithoutUI"/> is authoritative (it comes from the code that applies each stat, so a dedicated
    /// server without UI gets the same values). The UI's showDecimal flag is only cross-checked and logged once as
    /// "[Enchanting] units: ..." so a wrong entry shows up in Player.log.
    /// </summary>
    public static class EnchantUnits
    {
        private static bool _logged;

        // Tenths conditions among our enchants. Evidence: tooltips "+15.0%", "+5.0%", "by 10.0%" and Swiftness raw 90
        // (2026-11-04); EntityUtility.CalculateDamage /1000 (damage %, magic 102), ×(1000 + v)/1000 (DamageTaken 97);
        // cooldowns /1000 (40/41/65); MinionExtensions /1000 (105) and TouchAttackStateSystem /1000 (114);
        // movement-speed effects (cap −900) for the slow conditions.
        // Whole-percent (not listed): MiningPercentageIncrease (/100), ReduceManaCostByPercentage (/100),
        // GainManaCostOnCrit, GainMagicDamagePercentageBoostAfterCrit (game ×10s it), minion crit/boss %,
        // ReducedDamageFromExplosions (/100), crit, dodge, boss %, chances.
        private static readonly HashSet<ConditionID> TenthsWithoutUI = new HashSet<ConditionID>
        {
            ConditionID.PhysicalMeleeDamageIncrease, ConditionID.PhysicalRangeDamageIncrease,
            ConditionID.MeleeAttackSpeedIncrease, ConditionID.RangeAttackSpeedIncrease,
            ConditionID.ArmorPercentageIncrease, ConditionID.MovementSpeedIncrease,
            ConditionID.ChanceOnRangeShotToApplySlowedBySlime,
            ConditionID.IncreasedMagicDamagePercentage, ConditionID.ReducedDamageTakenPercentage,
            ConditionID.IncreasedMinionDamagePercentage, ConditionID.IncreasedMinionAttackSpeed,
            ConditionID.ApplySlowedByCold,
            ConditionID.ChanceForRandomLootFromWall, // Fortune: confirmed tenths by the in-game units check 2026-11-04
        };

        public static bool IsTenths(ConditionID id) => TenthsWithoutUI.Contains(id);

        /// <summary>Flat enchants: displayed value × unit scale. (ItemLevel/Relative: see EnchantMath.)</summary>
        public static int Raw(EnchantDef def, int level)
        {
            int lvl = Mathf.Clamp(level, 1, def.MaxLevel);
            LogTableOnce();
            return def.Values[lvl - 1] * (IsTenths(def.Condition) ? 10 : 1);
        }

        // Logs the UI's showDecimal flag for every condition an enchant can write, once, and any mismatch with the
        // built-in table. Silent on a dedicated server (no UI).
        private static void LogTableOnce()
        {
            if (_logged) return;
            try
            {
                if (Manager.ui == null || Manager.ui.conditionsIconsTable == null) return;
                _logged = true;
                var tenths = new List<string>();
                var mismatch = new List<string>();
                var seen = new HashSet<ConditionID>();
                foreach (var id in EnchantMath.AllWrittenConditions())
                {
                    if (!seen.Add(id)) continue;
                    bool dec = Manager.ui.conditionsIconsTable.GetConditionInfo(id).showDecimal;
                    if (dec) tenths.Add(id.ToString());
                    if (dec != IsTenths(id)) mismatch.Add($"{id} (UI {(dec ? "tenths" : "whole")}, built-in {(IsTenths(id) ? "tenths" : "whole")})");
                }
                Debug.Log($"[{EnchantingMod.Name}] units: tenths = {string.Join(", ", tenths)}; built-in table mismatches: {(mismatch.Count == 0 ? "none" : string.Join(", ", mismatch))}");
            }
            catch (System.Exception)
            {
                // UI not ready: try again next time
            }
        }
    }
}
