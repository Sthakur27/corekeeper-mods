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
    /// Converts displayed values to raw condition values. Conditions flagged showDecimal store tenths
    /// ("+1.5%" = 15). The flag lives in the UI table, so a dedicated server without UI uses the values as-is.
    /// </summary>
    public static class EnchantUnits
    {
        private static readonly Dictionary<ConditionID, int> Scale = new Dictionary<ConditionID, int>();

        public static int Raw(EnchantDef def, int level)
        {
            int lvl = Mathf.Clamp(level, 1, def.MaxLevel);
            return def.Values[lvl - 1] * ScaleOf(def.Condition);
        }

        private static int ScaleOf(ConditionID id)
        {
            if (Scale.TryGetValue(id, out int s)) return s;
            s = 1;
            bool known = false;
            try
            {
                if (Manager.ui != null && Manager.ui.conditionsIconsTable != null)
                {
                    s = Manager.ui.conditionsIconsTable.GetConditionInfo(id).showDecimal ? 10 : 1;
                    known = true;
                }
            }
            catch (System.Exception)
            {
                // no UI (dedicated server)
            }
            if (known) Scale[id] = s;
            return s;
        }
    }
}
