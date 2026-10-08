using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using UnityEngine;

namespace FishSeller.Patches
{
    /// <summary>
    /// Buying Legendary items (<see cref="ShopItems.IsSpecial"/>, i.e. the Starlight Nautilus).
    ///
    /// The game prices every Legendary item at 0, and its buy path (InventoryUtility.Buy inside the Burst
    /// InventoryUpdateSystem) does nothing for a 0 price; Harmony cannot reach Burst code. The client side
    /// is managed, though: the price label and the "can afford" check go through InventoryHandler.GetCoinValue,
    /// and a click goes through InventoryHandler.Buy. So on the client we show our price and, for these
    /// items only, send "/fishsellerbuy &lt;objectID&gt; &lt;slot&gt;" over CoreLib's command RPC instead
    /// of the vanilla request; <see cref="SpecialBuyCommand"/> checks stock and coins on the server and
    /// moves the item. Every other item keeps the vanilla buy path.
    /// </summary>
    public static class SpecialBuy
    {
        public const string Trigger = "fishsellerbuy";

        private static float _nextSend;

        /// <summary>The special item in a merchant buy slot, or None.</summary>
        public static ObjectID SpecialAt(InventoryHandler merchant, int index)
        {
            if (merchant == null || !merchant.isBuyInventory || !(merchant.entityMonoBehaviour is NPC)) return ObjectID.None;
            var id = merchant.GetContainedObjectData(index).objectID;
            return ShopItems.IsSpecial(id) ? id : ObjectID.None;
        }

        public static void Send(ObjectID id, int slot)
        {
            // Holding the mouse repeats every 0.1 s; the server re-checks coins and stock each time anyway.
            if (Time.unscaledTime < _nextSend) return;
            _nextSend = Time.unscaledTime + 0.25f;
            var comm = CommandModule.ClientCommSystem;
            if (comm == null)
            {
                Debug.LogWarning($"[{FishSellerMod.Name}] command channel not ready; can't buy {id}.");
                return;
            }
            comm.SendCommand($"/{Trigger} {(int)id} {slot}", CommandFlags.None);
        }
    }

    /// <summary>Price label and affordability for special items (client; managed callers only).</summary>
    [HarmonyPatch(typeof(InventoryHandler), "GetCoinValue", new[] { typeof(PlayerController), typeof(bool), typeof(int) })]
    public static class SpecialPricePatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryHandler __instance, bool buy, int index, ref int __result)
        {
            if (!buy) return;
            var id = SpecialBuy.SpecialAt(__instance, index);
            if (id != ObjectID.None) __result = ShopItems.SpecialPrice(id);
        }
    }

    /// <summary>Clicking a special item sends our command instead of the vanilla buy request.</summary>
    [HarmonyPatch(typeof(InventoryHandler), "Buy")]
    public static class SpecialBuyClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(InventoryHandler other, int index)
        {
            var id = SpecialBuy.SpecialAt(other, index);
            if (id == ObjectID.None) return true;
            SpecialBuy.Send(id, other.startPosInBuffer + index);
            return false;
        }
    }
}
