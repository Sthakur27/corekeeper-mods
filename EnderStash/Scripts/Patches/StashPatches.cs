using HarmonyLib;
using Inventory;
using Unity.Entities;

namespace EnderStash.Patches
{
    /// <summary>Walking away from (or breaking) the chest closes the window even while the stash is showing.</summary>
    [HarmonyPatch(typeof(Chest), nameof(Chest.OnPlayerLeftChest))]
    public static class ChestLeftPatch
    {
        public static void Postfix(Chest __instance)
        {
            if (!EnderStashUI.Active || EnderStashUI.ChestHandler != __instance.inventoryHandler) return;
            var player = Manager.main?.player;
            Manager.ui.TryHideAllInventoryAndCraftingUI();
            if (player != null) player.SetActiveWorldLabel(null);
            EnderStashUI.Reset();
        }
    }

    /// <summary>The Sort button sorts the stash instead of the player's inventory while the stash is showing.</summary>
    [HarmonyPatch(typeof(InventoryHandler), nameof(InventoryHandler.Sort))]
    public static class StashSortPatch
    {
        public static bool Prefix(InventoryHandler __instance, PlayerController playerController)
        {
            if (!EnderStashUI.Active || __instance != EnderStashUI.StashHandler) return true;
            EnderStashUI.SortStash(playerController);
            return false;
        }
    }

    /// <summary>
    /// Quick stack (key or button) while the stash is showing would ask the server to quick stack the
    /// player into itself; turn it into a quick stack into the stash and send a harmless no-op swap.
    /// </summary>
    [HarmonyPatch(typeof(Create), nameof(Create.QuickStack))]
    public static class StashQuickStackPatch
    {
        public static bool Prefix(Entity inventoryFrom, Entity inventoryTo, ref InventoryChangeData __result)
        {
            if (!EnderStashUI.Active || inventoryFrom != inventoryTo) return true;
            var player = Manager.main?.player;
            if (player == null || player.entity != inventoryFrom) return true;
            EnderStashUI.QuickStackIntoStash(player);
            __result = Create.Swap(inventoryFrom, inventoryFrom, StashLayout.Start, StashLayout.Start);
            return false;
        }
    }

    /// <summary>After a toggle, make the chest window recompute size and buttons even if the slot count is unchanged.</summary>
    [HarmonyPatch(typeof(InventoryUI), "UpdateContainerSize")]
    public static class ForceRefreshPatch
    {
        public static void Prefix(InventoryUI __instance, ref int ___previousInventorySize)
        {
            if (!EnderStashUI.ForceRefresh || Manager.ui == null || __instance != Manager.ui.chestInventoryUI) return;
            ___previousInventorySize = -1;
            EnderStashUI.ForceRefresh = false;
        }
    }
}
