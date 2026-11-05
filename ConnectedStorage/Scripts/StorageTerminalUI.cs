using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using Unity.Entities;

namespace ConnectedStorage
{
    // The old native-chest terminal screen (a StorageTerminalUI MonoBehaviour plus ~11 Harmony patches on hot
    // inventory/slot/text getters) was replaced by StorageBrowserUI and was never instantiated any more.
    // Its patches still ran on every call, so they were removed in 0.10.1. Close() stays as a no-op for callers.
    public static class StorageTerminalUI
    {
        public static void Close() { }
    }

    // Live: opening a Runic Storage unit or terminal routes to the browser (or a gem upgrade / server reply).
    [HarmonyPatch(typeof(Chest), nameof(Chest.Use))]
    public static class StorageTerminalUsePatch
    {
        public static bool Prefix(Chest __instance)
        {
            if (StorageCatalog.IsChest(__instance.objectData.objectID))
            {
                var player = Manager.main.player;
                var comm = CommandModule.ClientCommSystem;
                if (player != null && comm != null)
                {
                    var em=player.world.EntityManager;
                    var items=em.GetBuffer<ContainedObjectsBuffer>(player.entity, true);
                    int slot=player.equippedSlotIndex;
                    var tile = StorageNetwork.Tile(__instance.world.EntityManager, __instance.entity);
                    bool holdingGem=false;
                    if(slot>=0 && slot<items.Length)
                        for(int tier=1;tier<StorageCatalog.Tiers.Length;tier++)
                            if(items[slot].objectID!=ObjectID.None && items[slot].objectID==StorageCatalog.Gem(tier))holdingGem=true;
                    if(!holdingGem)
                    {
                        // Units are only accessible through a network that contains a terminal.
                        // The server replies with the explanation in chat.
                        if(StorageNetwork.Scan(__instance.world,__instance.entity).Terminals.Count>0) StorageBrowserUI.Open(__instance);
                        else comm.SendCommand("/connectedstorage " + tile.x + " " + tile.y + " open", CommandFlags.None);
                        return false;
                    }
                    comm.SendCommand("/connectedstorage " + tile.x + " " + tile.y + " upgrade "
                        + player.equippedSlotIndex + " " + (int)__instance.objectData.objectID, CommandFlags.None);
                }
                return false;
            }
            if (!StorageCatalog.IsTerminal(__instance.objectData.objectID)) return true;
            StorageBrowserUI.Open(__instance); return false;
        }
    }
}
