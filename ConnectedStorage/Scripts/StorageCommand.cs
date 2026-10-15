using System;
using System.Globalization;
using System.Collections.Generic;
using Inventory;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.System;
using CoreLib.Submodule.Command.Util;
using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ConnectedStorage
{
    // Commands carry coordinates, never client entity indices. The server resolves the
    // connected network anew and authenticates the player through CoreLib's sender.
    public sealed class StorageCommand : IServerCommandHandler
    {
        public string[] GetTriggerNames() => new[] { "connectedstorage" };
        public string GetDescription() => "Connected Storage terminal actions (used by the terminal UI).";
        private static int Number(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        public CommandOutput Execute(string[] args, Entity sender)
        {
            var result = ExecuteInner(args, sender);
            if (!string.IsNullOrEmpty(result.feedback))
            {
                var world = API.Server.World;
                if (world != null && world.IsCreated)
                {
                    var comm = world.GetExistingSystemManaged<CommandCommSystem>();
                    if (comm != null)
                    {
                        comm.SendResponse(result.feedback, result.status, CommandFlags.None, sender);
                        return Quiet();
                    }
                }
                // Do not broadcast another player's terminal feedback if transport is unavailable.
                return Quiet();
            }
            return result;
        }
        private CommandOutput ExecuteInner(string[] args, Entity sender)
        {
            try
            {
                if (args.Length < 3) return Error("Invalid storage request.");
                var world = API.Server.World;
                if (world == null || !world.IsCreated) return Error("Server unavailable.");
                var em = world.EntityManager;
                em.CompleteAllTrackedJobs();
                var player = sender.GetPlayerEntity();
                if (player == Entity.Null || !em.HasComponent<LocalTransform>(player)) return Error("Player unavailable.");
                var terminal = StorageNetwork.FindNode(world, Number(args[0]), Number(args[1]));
                if (terminal == Entity.Null) return Error("Storage object unavailable.");
                if (math.distancesq(em.GetComponentData<LocalTransform>(player).Position,
                    em.GetComponentData<LocalTransform>(terminal).Position) > 9f) return Error("Move closer to the storage object.");
                using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<WorldInfoCD>()))
                    if (query.GetSingleton<WorldInfoCD>().guestMode && em.GetComponentData<PlayerGhost>(player).adminPrivileges <= 0)
                        return Error("Storage changes are disabled in guest mode.");
                if (args[2] == "upgrade" && args.Length == 5)
                {
                    var feedback = world.GetOrCreateSystemManaged<StorageTransferSystem>().Upgrade(
                        player, terminal, Number(args[3]), (ObjectID)Number(args[4]));
                    return new CommandOutput("[Connected Storage] " + feedback,
                        feedback.StartsWith("Upgraded to ") ? CommandStatus.Info : CommandStatus.Warning);
                }
                if (!StorageCatalog.IsTerminal(em.GetComponentData<ObjectDataCD>(terminal).objectID)) return Error("Use the storage terminal to access items.");
                var network = StorageNetwork.Scan(world, terminal);
                if (args[2] == "craft" && args.Length == 4)
                {
                    var id = (ObjectID)Number(args[3]);
                    foreach (var recipe in StorageCrafting.GetRecipes(world, terminal))
                        if (recipe.ObjectID == id)
                        {
                            var system = world.GetOrCreateSystemManaged<StorageCraftingSystem>();
                            string error = system.TryCraft(player, terminal, recipe.Station, id);
                            return error == null ? Quiet() : Error(error);
                        }
                    return Error("Recipe is no longer connected.");
                }
                var transfers = world.GetOrCreateSystemManaged<StorageTransferSystem>();
                {
                    var inventory = em.GetBuffer<InventoryBuffer>(player)[0];
                    int first = inventory.startIndex;
                    int end = first + inventory.size;
                    if (args[2] == "take" && args.Length == 9)
                    {
                        var chest = StorageNetwork.FindNode(world, Number(args[3]), Number(args[4]));
                        int slot = Number(args[5]);
                        if (!network.Chests.Contains(chest) || !Available(world, chest)) return Error("Chest is unavailable.");
                        var items = em.GetBuffer<ContainedObjectsBuffer>(chest);
                        if (slot < 0 || slot >= items.Length || items[slot].objectID != (ObjectID)Number(args[6]) || items[slot].variation != Number(args[7])) return Error("Storage changed; select the item again.");
                        int amount = Number(args[8]) == 1 ? 1 : int.MaxValue;
                        return Result(transfers.Transfer(player, chest, slot, new List<Entity> { player }, amount));
                    }
                    else if (args[2] == "put" && args.Length == 6)
                    {
                        int slot = Number(args[3]);
                        var items = em.GetBuffer<ContainedObjectsBuffer>(player);
                        if (slot < first || slot >= end || items[slot].objectID != (ObjectID)Number(args[4]) || items[slot].variation != Number(args[5])) return Error("Inventory changed; select the item again.");
                        if (Locked(em, player, slot)) return Error("That item is locked.");
                        return Result(transfers.Transfer(player, player, slot, network.Chests, int.MaxValue));
                    }
                    else if (args[2] == "stack" || args[2] == "deposit")
                    {
                        var items = em.GetBuffer<ContainedObjectsBuffer>(player);
                        string feedback = null;
                        for (int slot = first + 10; slot < end; slot++)
                        {
                            if (items[slot].objectID == ObjectID.None || Locked(em, player, slot)) continue;
                            bool matching = args[2] == "deposit";
                            if (!matching) foreach (var chest in network.Chests)
                            {
                                var contents = em.GetBuffer<ContainedObjectsBuffer>(chest);
                                for (int j = 0; j < contents.Length; j++)
                                    if (contents[j].objectID == items[slot].objectID && contents[j].variation == items[slot].variation) { matching = true; break; }
                                if (matching) break;
                            }
                            if (matching)
                            {
                                var error = transfers.Transfer(player, player, slot, network.Chests, int.MaxValue);
                                if (error != null) feedback = error;
                            }
                        }
                        return Result(feedback);
                    }
                    else if (args[2] == "collect")
                    {
                        int slot = em.GetComponentData<CraftingCD>(player).outputSlotIndex;
                        if (slot >= 0) return Result(transfers.Transfer(player, player, slot, new List<Entity> { player }, int.MaxValue));
                    }
                    else return Error("Unknown storage action.");
                }
                return Quiet();
            }
            catch (FormatException) { return Error("Invalid storage request."); }
            catch (OverflowException) { return Error("Invalid storage request."); }
        }
        private static bool Locked(EntityManager em, Entity entity, int slot) => em.HasBuffer<LockedObjectsBuffer>(entity)
            && slot < em.GetBuffer<LockedObjectsBuffer>(entity).Length && em.GetBuffer<LockedObjectsBuffer>(entity)[slot].Value;
        private static bool Available(World world, Entity chest) => !StorageRemoval.IsEvacuating(world, chest)
            && (!world.EntityManager.HasComponent<HealthCD>(chest) || world.EntityManager.GetComponentData<HealthCD>(chest).health > 0);
        private static CommandOutput Result(string error) => error == null ? Quiet() : Error(error);
        private static CommandOutput Error(string text) => new CommandOutput("[Connected Storage] " + text, CommandStatus.Warning);
        private static CommandOutput Quiet() => new CommandOutput("", CommandStatus.None);
    }
}
