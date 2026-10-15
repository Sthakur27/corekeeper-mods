using System;
using System.Collections.Generic;
using Inventory;
using Pug.Automation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace ConnectedStorage
{
    public sealed class StorageRecipe
    {
        public Entity Station;
        public ObjectID ObjectID;
        public string Name;
    }

    public static class StorageCrafting
    {
        public static bool IsSimpleStation(EntityManager em, Entity station)
        {
            return em.Exists(station) && em.HasComponent<CraftingCD>(station)
                && em.HasComponent<CanCraftObjectsBuffer>(station)
                && (!em.HasComponent<HealthCD>(station) || em.GetComponentData<HealthCD>(station).health > 0)
                && em.GetComponentData<CraftingCD>(station).craftingType == CraftingType.Simple;
        }

        // Read recipes from the actual placed stations: the game has already applied
        // progression prerequisites to these buffers. Do not use prefab recipe lists.
        public static List<StorageRecipe> GetRecipes(World world, Entity terminal)
        {
            var result = new List<StorageRecipe>();
            var seen = new HashSet<ObjectID>();
            var em = world.EntityManager;
            var network = StorageNetwork.Scan(world, terminal);
            foreach (var station in StorageNetwork.GetStations(world, network))
            {
                if (!IsSimpleStation(em, station)) continue;
                var recipes = em.GetBuffer<CanCraftObjectsBuffer>(station);
                for (int i = 0; i < recipes.Length; i++)
                {
                    var recipe = recipes[i];
                    if (recipe.objectID == ObjectID.None || recipe.amount <= 0 || !seen.Add(recipe.objectID)) continue;
                    result.Add(new StorageRecipe { Station = station, ObjectID = recipe.objectID, Name = recipe.objectID.ToString() });
                }
            }
            return result;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class StorageCraftingSystem : PugSimulationSystemBase
    {
        private readonly Dictionary<Type, EntityQuery> singletonQueries = new Dictionary<Type, EntityQuery>();
        protected override void OnUpdate() { }

        private T Singleton<T>() where T : unmanaged, IComponentData
        {
            if (!singletonQueries.TryGetValue(typeof(T), out var query))
            {
                query = GetEntityQuery(ComponentType.ReadOnly<T>());
                singletonQueries.Add(typeof(T), query);
            }
            return query.GetSingleton<T>();
        }

        // Called only by the authenticated server command handler. All validation and
        // vanilla crafting execute synchronously; no consume/create transaction is copied.
        public string TryCraft(Entity player, Entity terminal, Entity station, ObjectID recipeID)
        {
            if (!World.IsServer()) return "Crafting must run on the server.";
            EntityManager.CompleteAllTrackedJobs();
            var em = EntityManager;
            if (!em.Exists(player) || !em.HasComponent<PlayerGhost>(player)
                || !em.HasComponent<LocalTransform>(player) || !em.Exists(terminal)
                || !em.HasComponent<LocalTransform>(terminal)) return "Storage terminal is unavailable.";
            if (em.HasComponent<HealthCD>(player) && em.GetComponentData<HealthCD>(player).health <= 0)
                return "You cannot craft while defeated.";
            if (math.distancesq(em.GetComponentData<LocalTransform>(player).Position,
                em.GetComponentData<LocalTransform>(terminal).Position) > 36f)
                return "Move closer to the storage terminal.";
            var network = StorageNetwork.Scan(World, terminal);
            if (!network.Terminals.Contains(terminal)) return "This is not a storage terminal.";
            if (!StorageNetwork.GetStations(World, network).Contains(station)
                || !StorageCrafting.IsSimpleStation(em, station)) return "That crafting station is no longer connected.";
            bool allowed = false;
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(station);
            for (int i = 0; i < recipes.Length; i++)
                if (recipes[i].objectID == recipeID && recipeID != ObjectID.None && recipes[i].amount > 0)
                    allowed = true;
            if (!allowed) return "That recipe is not available at the connected station.";
            if (Singleton<WorldInfoCD>().guestMode && em.GetComponentData<PlayerGhost>(player).adminPrivileges <= 0)
                return "Crafting is disabled in this world.";
            if (em.HasComponent<ElectricityCD>(station) && !em.GetComponentData<ElectricityCD>(station).hasEnoughElectricityToPowerStuff)
                return "The crafting station needs electricity.";
            if (!em.HasComponent<CraftingCD>(player) || !em.HasComponent<ContainedObjectsBuffer>(player)
                || !em.HasComponent<ObjectDataCD>(player) || !em.HasBuffer<InventoryBuffer>(player))
                return "Player inventory is unavailable.";
            int output = em.GetComponentData<CraftingCD>(player).outputSlotIndex;
            if (output < 0 || output >= em.GetBuffer<ContainedObjectsBuffer>(player).Length)
                return "Player crafting output is unavailable.";

            using (var inventories = new NativeList<Entity>(Allocator.Temp))
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            {
                inventories.Add(player);
                foreach (var chest in network.Chests)
                    if (chest != player && em.Exists(chest) && em.HasComponent<ContainedObjectsBuffer>(chest)
                        && !StorageRemoval.IsEvacuating(World, chest)
                        && (!em.HasComponent<HealthCD>(chest) || em.GetComponentData<HealthCD>(chest).health > 0)
                        && !inventories.Contains(chest)) inventories.Add(chest);
                var shared = new InventoryHandlerShared(ref CheckedStateRef,
                    Singleton<PugDatabase.DatabaseBankCD>(), Singleton<SkillTalentsTableCD>(),
                    Singleton<UpgradeCostsTableCD>(), Singleton<InventoryAuxDataSystemDataCD>());
                shared.Update(ref CheckedStateRef, ecb, Singleton<NetworkTime>());
                bool crafted = InventoryUtility.Craft(in shared, player, em.GetComponentData<ObjectDataCD>(player),
                    inventories, new CanCraftObjectsBuffer { objectID = recipeID, amount = 1 }, 0,
                    em.GetComponentData<LocalTransform>(player).Position, true, player, station);
                if (crafted) CollectOutput(in shared, player, output);
                ecb.Playback(em);
                if (!crafted) return "Missing materials or the crafting output slot is occupied.";
                return em.GetBuffer<ContainedObjectsBuffer>(player)[output].objectID == ObjectID.None
                    ? null : "Crafted. Your bag is full; the item remains in the output slot.";
            }
        }

        private static void CollectOutput(in InventoryHandlerShared shared, Entity player, int output)
        {
            var ranges = shared.inventoryLookup[player];
            if (ranges.Length == 0) return;
            var main = ranges[0];
            var slots = shared.containedObjectsBufferLookup[player];
            for (int pass = 0; pass < 2; pass++)
            for (int destination = main.startIndex; destination < main.startIndex + main.size; destination++)
            {
                var item = slots[output];
                if (item.objectID == ObjectID.None) return;
                if (destination == output) continue;
                var candidate = slots[destination];
                if (pass == 0 ? candidate.objectID == ObjectID.None : candidate.objectID != ObjectID.None) continue;
                if (shared.lockedObjectsBufferLookup.TryGetBuffer(player, out var locks)
                    && destination < locks.Length && locks[destination].Value) continue;
                int chosen = InventoryUtility.TryFindSlotToAddTo(in shared, item.objectID,
                    player, destination, -1, item.variation);
                if (chosen != destination) continue;
                InventoryUtility.MoveAmount(in shared, player, output, player, destination, -1, item.amount);
            }
        }
    }
}
