using HarmonyLib;

namespace CoinPouch.Patches
{
    /// <summary>The coin slot UI reads and writes the coin slot's inventory range.</summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.GetInventoryHandler))]
    public static class CoinSlotHandlerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(InventorySlotUI __instance, ref InventoryHandler __result)
        {
            if (!CoinSlotUI.Is(__instance)) return true;
            __result = CoinSlotUI.Handler;
            return false;
        }
    }

    /// <summary>Hover text and tooltips of the coin slot (SlotUIBase switches on the slot type).</summary>
    [HarmonyPatch(typeof(SlotUIBase), "GetSlotObject")]
    public static class CoinSlotObjectPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(SlotUIBase __instance, ref ContainedObjectsBuffer __result)
        {
            if (!CoinSlotUI.Is(__instance)) return true;
            InventoryHandler handler = CoinSlotUI.Handler;
            __result = handler != null ? handler.GetContainedObjectData(0) : default;
            return false;
        }
    }

    /// <summary>Anything but Ancient Coins shakes the slot like a wrong armor piece (the server refuses it anyway).</summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.TryingToPlaceItemShouldShowError))]
    public static class CoinSlotPlaceErrorPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(InventorySlotUI __instance, ObjectDataCD objectBeingPlaced, ref bool __result)
        {
            if (!CoinSlotUI.Is(__instance)) return true;
            __result = objectBeingPlaced.objectID != ObjectID.None && objectBeingPlaced.objectID != ObjectID.AncientCoin;
            return false;
        }
    }

    /// <summary>The coin slot is kept locked (so quick stack leaves it alone); don't draw the lock on it.</summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.UpdateSlot))]
    public static class CoinSlotLockIconPatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventorySlotUI __instance)
        {
            if (!CoinSlotUI.Is(__instance) || __instance.lockedIcon == null) return;
            if (__instance.lockedIcon.gameObject.activeSelf) __instance.lockedIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Merchant buy slots decide "can afford" from the main inventory only (InventorySlotUI.CanAffordItem);
    /// count the coin slot too. The server side already counts it (it walks every inventory range).
    /// </summary>
    [HarmonyPatch(typeof(InventoryHandler), nameof(InventoryHandler.GetExistingAmountOfObject))]
    public static class CoinCountPatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryHandler __instance, ObjectID objectID, ref int __result)
        {
            if (objectID != ObjectID.AncientCoin) return;
            PlayerController player = Manager.main != null ? Manager.main.player : null;
            if (player == null || !ReferenceEquals(__instance, player.playerInventoryHandler)) return;
            __result += CoinSlotUI.CoinsInSlot(__instance);
        }
    }

    /// <summary>
    /// Server: the joining character is decoded here, right before its slots are copied into the player.
    /// Runs first, before Ender Stash's relocation (see <see cref="CoinLayout.Relocate"/>).
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetCharacterDataFromSerialized))]
    public static class CoinRelocatePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        public static void Postfix(CharacterData __result)
        {
            CoinLayout.Relocate(__result);
        }
    }
}
