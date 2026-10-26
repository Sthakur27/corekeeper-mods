using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace MagicMirror
{
    public sealed class MagicMirrorMod : IMod
    {
        public const string Version = "1.0.4";
        public const string ObjectName = "MagicMirror_Mirror";
        internal static bool Active;
        private static ObjectID _id;
        // Mod object IDs are fixed once registered; cache to skip the string lookup.
        public static ObjectID Id
        {
            get
            {
                if (_id == ObjectID.None) _id = API.Authoring.GetObjectID(ObjectName);
                return _id;
            }
        }
        public void EarlyInit()
        {
            Active = true;
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log("[MagicMirror] v" + Version + " loaded: reusable recall, 50 Solarite Bars + 10 Recall Idols or 20% Omoroth drop.");
        }
        public void Init() { }
        public void Update() { }
        public void ModObjectLoaded(Object obj) { }
        public void Shutdown() { API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded; }
        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            EnsureRecipe(entity, em);
        }
        internal static void EnsureRecipe(Entity entity, EntityManager em)
        {
            var mirror = Id;
            if (mirror == ObjectID.None || !em.HasComponent<ObjectDataCD>(entity)
                || !em.HasBuffer<CanCraftObjectsBuffer>(entity)) return;
            // Read-only first: a writable GetBuffer is a sync point, and nearly every
            // station already has the recipe or is not a Solarite-tier bench.
            int start = 0, count = -1;
            // Higher benches copy lower-tier categories. Keep the recipe in Solarite's
            // Gear category on both Solarite and higher-tier workbenches.
            if (em.HasBuffer<IncludedCraftingBuildingsBuffer>(entity))
            {
                var included = em.GetBuffer<IncludedCraftingBuildingsBuffer>(entity, true);
                for (int i = 0; i < included.Length; i++)
                {
                    var entry = included[i];
                    if (entry.objectID == ObjectID.SolariteWorkbench)
                    {
                        count = entry.amountOfCraftingOptions;
                        break;
                    }
                    start += entry.amountOfCraftingOptions;
                }
            }
            else if (em.GetComponentData<ObjectDataCD>(entity).objectID == ObjectID.SolariteWorkbench)
                count = int.MaxValue;
            if (count < 0) return;
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity, true);
            if (count == int.MaxValue) count = recipes.Length;
            int slot = FindGearSlot(recipes, start, count, mirror);
            if (slot < 0) return;
            recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            recipes[slot] = new CanCraftObjectsBuffer { objectID = mirror, amount = 1 };
            Debug.Log("[MagicMirror] Solarite Workbench recipe registered.");
        }
        private static int FindGearSlot(DynamicBuffer<CanCraftObjectsBuffer> recipes, int start, int count, ObjectID mirror)
        {
            int end = System.Math.Min(start + count, recipes.Length);
            for (int i = start; i < end; i++) if (recipes[i].objectID == mirror) return -1;
            // Vanilla Solarite Gear has four empty slots (2..5). Do not shift any
            // recipe indices or overwrite another mod's recipe.
            for (int i = start + 2; i < System.Math.Min(start + 6, end); i++)
                if (recipes[i].objectID == ObjectID.None) return i;
            return -1;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class MirrorRecipeSystem : PugSimulationSystemBase
    {
        private EntityQuery stations;
        private double next;
        protected override void OnCreate()
        {
            base.OnCreate();
            stations = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<CanCraftObjectsBuffer>());
        }
        protected override void OnUpdate()
        {
            if (World.Time.ElapsedTime < next || MagicMirrorMod.Id == ObjectID.None) return;
            next = World.Time.ElapsedTime + 1;
            using (var entities = stations.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) MagicMirrorMod.EnsureRecipe(entity, EntityManager);
        }
    }
}
