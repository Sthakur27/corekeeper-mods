using HarmonyLib;
using Inventory;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MerchantRestock.Patches
{
    /// <summary>
    /// Shift- or Ctrl-click on a merchant slot buys the whole stack in that slot, or as many as you can afford,
    /// in one go.
    ///
    /// Vanilla (client, managed): InventorySlotUI.AttemptToBuyItem calls mouseInventoryHandler.Buy, which queues
    /// ONE InventoryAction.Buy; the server (InventoryUtility.Buy) moves one item per action, and the input queue
    /// sends one action per tick, so N vanilla buys trickle in one at a time.
    ///
    /// Instead we queue a single InventoryAction.Sell from the merchant to the mouse slot. The server runs
    /// InventoryUtility.SellObject (the same routine Buy ends in), which in one step checks that the merchant
    /// slot still holds the item and at least N of it, and that the buyer has the coins, then moves N items and
    /// takes N x price coins. Nothing here can over-buy or spend coins you lack.
    ///
    /// Only for merchants (MerchantCD + ContainedObjectsBuffer) and only when the mouse slot is empty or holds
    /// the same item; otherwise the click falls back to vanilla (one item). Holding the button repeats
    /// AttemptToBuyItem(playErrorOnFail: false) every 0.1 s; with the modifier held those repeats are swallowed.
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotUI), "AttemptToBuyItem")]
    public static class QuickBuyPatch
    {
        /// <summary>Safety cap per click (merchant stacks are at most 99 with our settings).</summary>
        private const int MaxPerClick = 999;
        private const int MaxStack = 9999;

        public static bool Enabled = true;

        private static bool ModifierHeld()
            => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        [HarmonyPrefix]
        public static bool Prefix(InventorySlotUI __instance, bool playErrorOnFail)
        {
            if (!Enabled || !ModifierHeld()) return true;
            if (!playErrorOnFail) return false; // hold-to-repeat: the click already bought the stack
            return !TryBuyStack(__instance);
        }

        /// <summary>Queues the bulk buy; false lets vanilla handle the click (one item, or its error sound).</summary>
        private static bool TryBuyStack(InventorySlotUI slot)
        {
            var player = Manager.main != null ? Manager.main.player : null;
            if (player == null || player.mouseInventoryHandler == null || player.playerInventoryHandler == null) return false;
            var handler = slot.GetInventoryHandler();
            if (handler == null) return false;

            var em = player.world.EntityManager;
            var seller = handler.inventoryEntity;
            if (!em.Exists(seller) || !em.HasComponent<MerchantCD>(seller) || !em.HasBuffer<ContainedObjectsBuffer>(seller)) return false;

            var data = slot.GetObjectData();
            if (data.objectID == ObjectID.None || data.amount <= 1) return false;
            var info = PugDatabase.GetObjectInfo(data.objectID, data.variation);
            if (info == null || !info.isStackable) return false;

            var mouse = player.mouseInventoryHandler;
            var held = mouse.GetObjectData(0);
            if (held.objectID != ObjectID.None && (held.objectID != data.objectID || held.variation != data.variation)) return false;

            int price = slot.GetCoinValue();
            if (price <= 0) return false;
            int coins = player.playerInventoryHandler.GetExistingAmountOfObject(ObjectID.AncientCoin);
            int count = Mathf.Min(data.amount, coins / price, MaxPerClick, MaxStack - math.max(0, held.amount));
            if (count <= 1) return false;

            float3 position = player.WorldPosition;
            player.QueueInputAction(new UIInputActionData
            {
                action = UIInputAction.InventoryChange,
                inventoryChangeData = Create.Sell(seller, handler.startPosInBuffer + slot.inventorySlotIndex,
                    mouse.inventoryEntity, mouse.startPosInBuffer, mouse.startPosInBuffer + mouse.size,
                    data.objectID, count, count * price, position)
            });
            AudioManager.SfxUI(SfxID.uiPickup, 0.4f, reuse: true, 1f, 0.1f);
            Debug.Log($"[{MerchantRestockMod.Name}] quick buy: {count} x {data.objectID} for {count * price} coins.");
            return true;
        }
    }
}
