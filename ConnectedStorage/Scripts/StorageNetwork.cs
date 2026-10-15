using System.Collections.Generic;
using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ConnectedStorage
{
    public struct StorageNodeTag : IComponentData { }
    public static class StorageCatalog
    {
        public static readonly string[] Tiers = { "Copper", "Tin", "Iron", "Scarlet", "Octarine", "Galaxite", "Solarite", "Pandorium" };
        public static readonly int[] Capacities = { 20, 40, 60, 80, 100, 120, 140, 160 };
        private static ObjectID terminal;
        public static ObjectID Workbench => API.Authoring.GetObjectID("ConnectedStorage_Workbench");
        private static readonly ObjectID[] chests = new ObjectID[8];
        public static ObjectID Terminal => terminal != ObjectID.None ? terminal : (terminal = API.Authoring.GetObjectID("ConnectedStorage_Terminal"));
        public static ObjectID Chest(int tier) => chests[tier] != ObjectID.None ? chests[tier] : (chests[tier] = API.Authoring.GetObjectID("ConnectedStorage_" + Tiers[tier] + "Chest"));
        public static ObjectID Gem(int tier) => tier > 0 && tier < Tiers.Length ? API.Authoring.GetObjectID("ConnectedStorage_" + Tiers[tier] + "Gem") : ObjectID.None;
        public static int Tier(ObjectID id)
        {
            if (id != ObjectID.None) for (int i = 0; i < Tiers.Length; i++) if (id == Chest(i)) return i;
            return -1;
        }
        public static bool CanUpgrade(ObjectID unit, ObjectID gem)
        {
            int tier = Tier(unit);
            return tier >= 0 && tier < Tiers.Length - 1 && gem != ObjectID.None && gem == Gem(tier + 1);
        }
        public static bool IsRecipe(ObjectID id)
        {
            if (IsNode(id) || (id != ObjectID.None && id == Workbench)) return true;
            if (id != ObjectID.None) for (int i = 1; i < Tiers.Length; i++) if (id == Gem(i)) return true;
            return false;
        }
        public static bool IsTerminal(ObjectID id) => id != ObjectID.None && id == Terminal;
        public static int Capacity(ObjectID id)
        {
            if (id == ObjectID.None) return 0;
            for (int i = 0; i < Tiers.Length; i++) if (id == Chest(i)) return Capacities[i];
            return 0;
        }
        public static bool IsChest(ObjectID id) => Capacity(id) > 0;
        public static bool IsNode(ObjectID id) => IsTerminal(id) || IsChest(id);
    }

    // Items remain in vanilla inventories. This is a view of a graph, never another item store.
    public static class StorageNetwork
    {
        public sealed class Snapshot
        {
            public readonly List<Entity> Nodes = new List<Entity>();
            public readonly List<Entity> Chests = new List<Entity>();
            public readonly List<Entity> Terminals = new List<Entity>();
        }
        public static int2 Tile(EntityManager em, Entity entity)
        {
            float3 p = em.GetComponentData<LocalTransform>(entity).Position;
            return (int2)math.round(p.xz);
        }
        private static int CompareNodes(EntityManager em, Entity a, Entity b)
        {
            var at=Tile(em,a);var bt=Tile(em,b);
            return Browser.StorageNodeOrder.Compare(at.x,at.y,bt.x,bt.y);
        }
        public static bool Adjacent(int2 a, int2 b) => math.csum(math.abs(a - b)) == 1;
        public static Snapshot Scan(World world, Entity root, Entity excluded = default(Entity))
        {
            var result = new Snapshot();
            if (world == null || !world.IsCreated || root == excluded) return result;
            var em = world.EntityManager;
            if (!em.Exists(root) || !em.HasComponent<LocalTransform>(root)) return result;
            var candidates = new Dictionary<int2, Entity>();
            // Loaded client static ghosts may use a vanilla prefab without our custom tag.
            // The replicated ObjectDataCD ID is the reliable identity on both worlds.
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    if ((em.HasComponent<EntityDestroyedCD>(entity) && em.IsComponentEnabled<EntityDestroyedCD>(entity)) || entity == excluded || !StorageCatalog.IsNode(em.GetComponentData<ObjectDataCD>(entity).objectID)) continue;
                    candidates[Tile(em, entity)] = entity;
                }
            if (!candidates.ContainsValue(root)) return result;
            var visited = new HashSet<Entity>();
            var queue = new Queue<Entity>();
            queue.Enqueue(root); visited.Add(root);
            int2[] offsets = { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) };
            while (queue.Count > 0)
            {
                Entity entity = queue.Dequeue();
                result.Nodes.Add(entity);
                var id = em.GetComponentData<ObjectDataCD>(entity).objectID;
                if (StorageCatalog.IsTerminal(id)) result.Terminals.Add(entity);
                else if (em.HasBuffer<ContainedObjectsBuffer>(entity)) result.Chests.Add(entity);
                var tile = Tile(em, entity);
                foreach (var offset in offsets)
                    if (candidates.TryGetValue(tile + offset, out var next) && visited.Add(next)) queue.Enqueue(next);
            }
            result.Chests.Sort((a, b) => CompareNodes(em,a,b));
            result.Terminals.Sort((a, b) => CompareNodes(em,a,b));
            return result;
        }
        public static Entity FindNode(World world, int x, int z)
        {
            var em = world.EntityManager;
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                    if ((!em.HasComponent<EntityDestroyedCD>(entity) || !em.IsComponentEnabled<EntityDestroyedCD>(entity)) && StorageCatalog.IsNode(em.GetComponentData<ObjectDataCD>(entity).objectID) && Tile(em, entity).Equals(new int2(x, z))) return entity;
            return Entity.Null;
        }
        public static List<Entity> GetStations(World world, Snapshot network)
        {
            var result = new List<Entity>();
            var em = world.EntityManager;
            var tiles = new HashSet<int2>();
            foreach (var node in network.Nodes) tiles.Add(Tile(em, node));
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<CraftingCD>(), ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    var data = em.GetComponentData<ObjectDataCD>(entity);
                    var info = PugDatabase.GetObjectInfo(data.objectID, data.variation);
                    if (info == null) continue;
                    var size = info.prefabTileSize;
                    var offset = info.prefabCornerOffset;
                    if (em.HasComponent<DirectionCD>(entity))
                    {
                        var direction = em.GetComponentData<DirectionCD>(entity);
                        size = direction.GetPrefabTileSize(size);
                        offset = direction.GetPrefabOffset(offset);
                    }
                    var origin = Tile(em, entity) + new int2(offset.x, offset.y);
                    bool touches = false;
                    for (int x = 0; x < size.x && !touches; x++)
                        for (int z = 0; z < size.y && !touches; z++)
                        {
                            var tile = origin + new int2(x, z);
                            touches = tiles.Contains(tile + new int2(1, 0)) || tiles.Contains(tile + new int2(-1, 0))
                                || tiles.Contains(tile + new int2(0, 1)) || tiles.Contains(tile + new int2(0, -1));
                        }
                    if (touches) result.Add(entity);
                }
            result.Sort((a, b) => CompareNodes(em,a,b));
            return result;
        }
    }
}
