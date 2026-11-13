using System.Collections.Generic;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// The Enchanting Table uses the vanilla Upgrade Forge graphics and UI (one item slot, a materials list, one
    /// button). While the open building is the table, these patches swap the forge's level cost for the enchant
    /// cost, hide the level-up preview, retitle the button and send "/enchant table x z" to the server instead of
    /// the vanilla Upgrade craft action. The server (<see cref="EnchantTableServer"/>) checks and takes the cost
    /// from the player's inventory, rolls and writes the enchant onto the item in the table's slot.
    /// </summary>
    public static class EnchantTableUI
    {
        public static bool IsTableOpen(out CraftingBuilding building)
        {
            building = null;
            if (EnchantRecipe.TableId == ObjectID.None) return false;
            try
            {
                building = UIManager.GetCraftingBuilding();
                return building != null && building.entityExist && building.objectData.objectID == EnchantRecipe.TableId;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public static List<PugDatabase.MaterialInfo> Materials(ContainedObjectsBuffer item)
        {
            if (item.objectID == ObjectID.None || Enchants.SlotOf(item.objectID) == EnchantSlot.None) return null;
            EnchantRoll.Cost(EnchantStorage.CountClient(item), out int gold, out int coins);
            return new List<PugDatabase.MaterialInfo>
            {
                new PugDatabase.MaterialInfo(ObjectID.GoldBar, gold, CountOnPlayer(ObjectID.GoldBar), Entity.Null, null),
                new PugDatabase.MaterialInfo(ObjectID.AncientCoin, coins, CountOnPlayer(ObjectID.AncientCoin), Entity.Null, null),
            };
        }

        private static int CountOnPlayer(ObjectID id)
        {
            var player = Manager.main.player;
            if (player == null || !player.entityExist) return 0;
            var em = player.world.EntityManager;
            if (!em.HasBuffer<ContainedObjectsBuffer>(player.entity)) return 0;
            var items = em.GetBuffer<ContainedObjectsBuffer>(player.entity, true);
            int total = 0;
            for (int i = 0; i < items.Length; i++)
                if (items[i].objectID == id) total += items[i].amount;
            return total;
        }

        public static bool CanPay(List<PugDatabase.MaterialInfo> materials)
        {
            if (materials == null) return false;
            if (Manager.ui.craftingMaterialsAreNotRequired) return true;
            foreach (var m in materials)
                if (m.amountAvailable < m.amountNeeded) return false;
            return true;
        }

        /// <summary>"/enchant table x z" (roll) or "/enchant tableclear x z" (remove the top enchant) for this table.</summary>
        public static void SendCommand(CraftingBuilding building, string sub)
        {
            var comm = CommandModule.ClientCommSystem;
            if (comm == null)
            {
                Debug.LogWarning($"[{EnchantingMod.Name}] command channel not ready");
                return;
            }
            Vector3 p = building.WorldPosition;
            comm.SendCommand($"/enchant {sub} {Mathf.RoundToInt(p.x)} {Mathf.RoundToInt(p.z)}", CommandFlags.None);
        }
    }

    // UpgradeForgeUI.LateUpdate hands the slot's item to the button every frame. Keep our own copy: the mod
    // loader's verifier rejects reflection (Traverse) on the button's private field.
    [HarmonyPatch(typeof(UpgradeForgeButton), nameof(UpgradeForgeButton.UpdateSlotItem))]
    public static class EnchantTableItemPatch
    {
        internal static ContainedObjectsBuffer Item;

        public static void Prefix(ContainedObjectsBuffer item)
        {
            Item = item;
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeButton), nameof(UpgradeForgeButton.GetRequiredMaterials))]
    public static class EnchantTableMaterialsPatch
    {
        public static bool Prefix(UpgradeForgeButton __instance, ref List<PugDatabase.MaterialInfo> __result)
        {
            if (!EnchantTableUI.IsTableOpen(out _)) return true;
            // Removing is free: no materials list.
            __result = EnchantRemoveButton.IsRemove(__instance) ? null : EnchantTableUI.Materials(EnchantTableItemPatch.Item);
            return false;
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeButton), nameof(UpgradeForgeButton.ShouldBeActive))]
    public static class EnchantTableActivePatch
    {
        public static bool Prefix(UpgradeForgeButton __instance, ref bool __result)
        {
            if (!EnchantTableUI.IsTableOpen(out _)) return true;
            __result = EnchantRemoveButton.IsRemove(__instance)
                ? EnchantRemoveButton.CanClick()
                : EnchantTableUI.CanPay(EnchantTableUI.Materials(EnchantTableItemPatch.Item));
            return false;
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeButton), nameof(UpgradeForgeButton.GetHoverTitle))]
    public static class EnchantTableTitlePatch
    {
        public static void Postfix(UpgradeForgeButton __instance, ref TextAndFormatFields __result)
        {
            if (!EnchantTableUI.IsTableOpen(out _)) return;
            // Hover titles are always localized, so these use terms registered in EnchantingMod.EarlyInit.
            if (EnchantRemoveButton.IsRemove(__instance))
                __result = EnchantRemoveButton.CanClick() ? new TextAndFormatFields { text = EnchantRemoveButton.TitleTerm } : null;
            else if (__result != null)
                __result = new TextAndFormatFields { text = EnchantingMod.ButtonTerm };
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeUI), nameof(UpgradeForgeUI.Upgrade))]
    public static class EnchantTableButtonPatch
    {
        public static bool Prefix()
        {
            if (!EnchantTableUI.IsTableOpen(out var building)) return true;
            EnchantTableUI.SendCommand(building, "table");
            return false;
        }
    }

    [HarmonyPatch(typeof(UpgradeForgeUI), "LateUpdate")]
    public static class EnchantTablePreviewPatch
    {
        public static void Postfix(UpgradeForgeUI __instance)
        {
            if (__instance.isShowing && __instance.previewSlot != null && EnchantTableUI.IsTableOpen(out _))
                __instance.previewSlot.Show(false);
        }
    }

    /// <summary>Server side of the table button.</summary>
    public static class EnchantTableServer
    {
        // The client sends the table graphic's rounded position; the 2-tile-wide forge's entity position can sit
        // a tile or more away from it, so match generously and fall back to the table nearest the player.
        private const float TableSearchRadius = 3.5f;
        private const float MaxPlayerDistance = 10f;

        public static string Roll(EntityManager em, Entity player, int x, int z)
        {
            string error = FindTableItem(em, player, x, z, out int upgradeSlot, out var item, out var aux);
            if (error != null) return error;
            var kind = Enchants.SlotOf(item.objectID);
            if (kind == EnchantSlot.None) return $"{item.objectID} can't be enchanted.";

            var current = EnchantStorage.Read(em, aux, item);
            EnchantRoll.Cost(current.Count, out int gold, out int coins);
            bool free = em.HasComponent<PlayerGhost>(player) && IsCreative(em, player);
            if (!free && (Count(em, player, ObjectID.GoldBar) < gold || Count(em, player, ObjectID.AncientCoin) < coins))
                return $"you need {gold} Gold Bars and {coins} Ancient Coins.";

            string result = EnchantRoll.Roll(current, kind, item.objectID);
            if (result == null) return "nothing can be rolled for this item.";
            string err = EnchantStorage.Write(em, player, upgradeSlot, current);
            if (err != null) return err;
            if (!free)
            {
                Take(em, player, ObjectID.GoldBar, gold);
                Take(em, player, ObjectID.AncientCoin, coins);
            }
            return result + " Now: " + EnchantStorage.Describe(current) + ".";
        }

        /// <summary>
        /// The table's remove button: pops the item's top enchant, the most recently rolled one (stored last, shown
        /// first in the tooltip; free).
        /// </summary>
        public static string Clear(EntityManager em, Entity player, int x, int z)
        {
            string error = FindTableItem(em, player, x, z, out int upgradeSlot, out var item, out var aux);
            if (error != null) return error;
            var current = EnchantStorage.Read(em, aux, item);
            if (current.Count == 0) return $"{item.objectID} has no enchantments.";
            int top = current.Count - 1;
            string removed = EnchantStorage.Describe(new List<EnchantEntry> { current[top] });
            current.RemoveAt(top);
            string err = EnchantStorage.Write(em, player, upgradeSlot, current);
            return err ?? $"removed {removed} from {item.objectID}.";
        }

        /// <summary>
        /// Checks there is an Enchanting Table near the player (matching the client-sent position generously) and
        /// returns the item in the forge slot, which vanilla keeps in a hidden slot of the player's own inventory
        /// (UpgradeSlotCD.slotIndex, see UpgradeSlotHandler). Returns an error text, or null.
        /// </summary>
        private static string FindTableItem(EntityManager em, Entity player, int x, int z, out int upgradeSlot,
            out ContainedObjectsBuffer item, out InventoryAuxDataSystemDataCD aux)
        {
            upgradeSlot = -1;
            item = default;
            aux = default;
            var tableId = EnchantRecipe.TableId;
            if (tableId == ObjectID.None) return "the Enchanting Table is not loaded.";
            if (!em.HasComponent<LocalTransform>(player)) return "no player position.";
            float2 playerPos = em.GetComponentData<LocalTransform>(player).Position.xz;
            var target = new float2(x, z);

            Entity table = Entity.Null, nearPlayer = Entity.Null;
            int seen = 0;
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                float bestTarget = TableSearchRadius, bestPlayer = MaxPlayerDistance;
                foreach (var e in entities)
                {
                    if (em.GetComponentData<ObjectDataCD>(e).objectID != tableId) continue;
                    seen++;
                    float2 p = em.GetComponentData<LocalTransform>(e).Position.xz;
                    float d = math.distance(p, target);
                    if (d <= bestTarget) { bestTarget = d; table = e; }
                    float dp = math.distance(p, playerPos);
                    if (dp <= bestPlayer) { bestPlayer = dp; nearPlayer = e; }
                }
            }
            if (table == Entity.Null) table = nearPlayer;
            if (table == Entity.Null)
            {
                Debug.Log($"[{EnchantingMod.Name}] table lookup failed: sent ({x},{z}), player {playerPos}, {seen} table entities loaded");
                return "no Enchanting Table near you.";
            }
            if (math.distance(em.GetComponentData<LocalTransform>(table).Position.xz, playerPos) > MaxPlayerDistance)
                return "you are too far from the Enchanting Table.";
            if (!em.HasComponent<UpgradeSlotCD>(player)) return "no upgrade slot on the player (please report).";
            upgradeSlot = em.GetComponentData<UpgradeSlotCD>(player).slotIndex;
            if (!EnchantStorage.TryGetAux(em, out aux)) return "inventory aux data not ready.";

            var slots = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            if (upgradeSlot < 0 || upgradeSlot >= slots.Length || slots[upgradeSlot].objectID == ObjectID.None)
                return "put an item in the table first.";
            item = slots[upgradeSlot];
            return null;
        }

        private static bool IsCreative(EntityManager em, Entity player)
        {
            // Creative characters build with free materials in vanilla too.
            return em.HasComponent<GodModeCD>(player) && em.IsComponentEnabled<GodModeCD>(player);
        }

        private static int Count(EntityManager em, Entity player, ObjectID id)
        {
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            int total = 0;
            for (int i = 0; i < items.Length; i++)
                if (items[i].objectID == id) total += items[i].amount;
            return total;
        }

        private static void Take(EntityManager em, Entity player, ObjectID id, int amount)
        {
            var items = em.GetBuffer<ContainedObjectsBuffer>(player);
            for (int i = items.Length - 1; i >= 0 && amount > 0; i--)
            {
                if (items[i].objectID != id) continue;
                var slot = items[i];
                int take = System.Math.Min(amount, slot.amount);
                slot.objectData.amount -= take;
                amount -= take;
                items[i] = slot.objectData.amount > 0 ? slot : default;
            }
        }
    }
}
