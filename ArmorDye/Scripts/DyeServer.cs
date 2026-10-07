using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ArmorDye
{
    public enum ArmorPiece { Helm, Chest, Pants }

    /// <summary>
    /// Server-side dye writes. The dye lives in the armor item's MealsEatenCD inventory aux data (a vanilla
    /// per-item component the game only reads on live cattle), so it is saved and replicated like a pet's skin.
    /// </summary>
    public static class DyeServer
    {
        public static string SetDye(EntityManager em, Entity player, ArmorPiece piece, int dye)
        {
            if (!TryGetDisplayedSlot(em, player, piece, out int slot, out string err)) return err;
            return Write(em, player, slot, dye, piece.ToString());
        }

        /// <summary>Dyes the armor item at absolute index <paramref name="slot"/> of the player's own inventory (the UI's click target).</summary>
        public static string SetDyeAt(EntityManager em, Entity player, int slot, int dye)
        {
            if (player == Entity.Null || !em.Exists(player) || !em.HasBuffer<ContainedObjectsBuffer>(player)) return "No player found.";
            var inventory = em.GetBuffer<ContainedObjectsBuffer>(player);
            if (slot < 0 || slot >= inventory.Length || inventory[slot].objectID == ObjectID.None) return "That slot is empty.";
            if (!DyeColor.CanDye(inventory[slot].objectData)) return $"{inventory[slot].objectID} can't be dyed.";
            return Write(em, player, slot, dye, "Item");
        }

        /// <summary>Dyes the item in the player's hand (the selected hotbar slot, EquippedObjectCD).</summary>
        public static string SetHeld(EntityManager em, Entity player, int dye)
        {
            if (player == Entity.Null || !em.Exists(player) || !em.HasComponent<EquippedObjectCD>(player)) return "No player found.";
            return SetDyeAt(em, player, em.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex, dye);
        }

        private static string Write(EntityManager em, Entity player, int slot, int dye, string what)
        {
            if (!TryGetAux(em, out var aux)) return "Inventory aux data not ready.";

            var item = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
            int index = item.auxDataIndex;
            if (dye == 0 && index == 0) return $"{what}: no dye.";
            int before = index;
            aux.SetOrAllocateComponentData(em, ref index, new MealsEatenCD { Value = dye });

            // Allocating instantiates an entity (structural change), so fetch the buffer again.
            var inventory = em.GetBuffer<ContainedObjectsBuffer>(player);
            var again = inventory[slot];
            if (again.objectID != item.objectID) return $"{what}: the item moved, try again.";
            if (again.auxDataIndex != index)
            {
                again.auxDataIndex = index;
                inventory[slot] = again;
            }
            Debug.Log($"[{ArmorDyeMod.Name}] {what} slot {slot} {item.objectID} aux {before}->{index}: {DyeColor.Describe(dye)}");
            return $"{what} ({item.objectID}): {DyeColor.Describe(dye)}.";
        }

        public static string Info(EntityManager em, Entity player)
        {
            var sb = new StringBuilder();
            if (TryGetAux(em, out var aux))
            {
                LogPrefabLayout(em, aux);
                foreach (ArmorPiece piece in new[] { ArmorPiece.Helm, ArmorPiece.Chest, ArmorPiece.Pants })
                {
                    if (!TryGetDisplayedSlot(em, player, piece, out int slot, out string err))
                    {
                        sb.Append($"{piece}: {err} ");
                        continue;
                    }
                    var item = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
                    int dye = 0;
                    aux.TryGetExtraInventoryData(em, item.auxDataIndex, out MealsEatenCD d);
                    dye = d.Value;
                    sb.Append($"{piece}: {item.objectID} slot {slot} aux {item.auxDataIndex} {DyeColor.Describe(dye)}. ");
                }
            }
            string s = sb.ToString();
            Debug.Log($"[{ArmorDyeMod.Name}] info: {s}");
            return s.Length > 0 ? s : "Inventory aux data not ready.";
        }

        /// <summary>
        /// Logs which components each aux prefab carries, so we can see whether MealsEatenCD shares its prefab
        /// with other per-item data (allocating it then creates those too).
        /// </summary>
        public static void LogPrefabLayout(EntityManager em, InventoryAuxDataSystemDataCD aux)
        {
            int mealsIndex = TypeManager.GetTypeIndex<MealsEatenCD>();
            aux._typeIndexToTypeHash.TryGetValue(mealsIndex, out uint mealsHash);
            var pairs = aux._typeHashToPrefabEntity.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < pairs.Length; i++)
            {
                Entity prefab = pairs.Values[i];
                var sb = new StringBuilder();
                if (em.Exists(prefab))
                {
                    var types = em.GetComponentTypes(prefab, Allocator.Temp);
                    foreach (var t in types) sb.Append(t.ToString()).Append(' ');
                    types.Dispose();
                }
                string mark = pairs.Keys[i] == mealsHash ? " <- MealsEatenCD" : "";
                Debug.Log($"[{ArmorDyeMod.Name}] aux prefab {pairs.Keys[i]:x8}{mark}: {sb}");
            }
            pairs.Dispose();
        }

        /// <summary>The slot whose armor the character shows: the vanity slot if it holds an item, else the equipped one.</summary>
        public static bool TryGetDisplayedSlot(EntityManager em, Entity player, ArmorPiece piece, out int slot, out string error)
        {
            slot = -1;
            error = null;
            if (player == Entity.Null || !em.Exists(player) || !em.HasBuffer<ContainedObjectsBuffer>(player) || !em.HasComponent<EquipmentCD>(player))
            {
                error = "No player found.";
                return false;
            }
            var inventory = em.GetBuffer<ContainedObjectsBuffer>(player);
            var equipment = em.GetComponentData<EquipmentCD>(player);
            int equipped = piece == ArmorPiece.Helm ? equipment.helmSlotIndex : piece == ArmorPiece.Chest ? equipment.breastSlotIndex : equipment.pantsSlotIndex;
            int vanity = -1;
            if (em.HasComponent<VanitySlotsCD>(player))
            {
                var v = em.GetComponentData<VanitySlotsCD>(player);
                vanity = piece == ArmorPiece.Helm ? v.helmVanitySlotIndex : piece == ArmorPiece.Chest ? v.breastVanitySlotIndex : v.pantsVanitySlotIndex;
            }
            if (vanity >= 0 && vanity < inventory.Length && inventory[vanity].objectID != ObjectID.None)
                slot = vanity;
            else if (equipped >= 0 && equipped < inventory.Length && inventory[equipped].objectID != ObjectID.None)
                slot = equipped;
            if (slot < 0)
            {
                error = $"no {piece.ToString().ToLowerInvariant()} armor equipped";
                return false;
            }
            if (!PugDatabase.HasComponent<EquipmentSkinCD>(inventory[slot].objectData))
            {
                error = $"{inventory[slot].objectID} has no armor look to dye";
                slot = -1;
                return false;
            }
            return true;
        }

        private static bool TryGetAux(EntityManager em, out InventoryAuxDataSystemDataCD aux)
        {
            aux = default;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<InventoryAuxDataSystemDataCD>());
            if (query.IsEmptyIgnoreFilter) return false;
            aux = query.GetSingleton<InventoryAuxDataSystemDataCD>();
            return true;
        }
    }
}
