using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace MagicMirror
{
    public sealed class MagicMirrorMod : IMod
    {
        public const string Version = "1.0.3";
        public const string ObjectName = "MagicMirror_Mirror";
        internal static bool Active;
        public static ObjectID Id => API.Authoring.GetObjectID(ObjectName);
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
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            // Higher benches copy lower-tier categories. Keep the recipe in Solarite's
            // Gear category on both Solarite and higher-tier workbenches.
            if (em.HasBuffer<IncludedCraftingBuildingsBuffer>(entity))
            {
                var included = em.GetBuffer<IncludedCraftingBuildingsBuffer>(entity);
                int start = 0;
                for (int i = 0; i < included.Length; i++)
                {
                    var entry = included[i];
                    if (entry.objectID == ObjectID.SolariteWorkbench)
                    {
                        FillGearSlot(recipes, start, entry.amountOfCraftingOptions, mirror);
                        return;
                    }
                    start += entry.amountOfCraftingOptions;
                }
            }
            else if (em.GetComponentData<ObjectDataCD>(entity).objectID == ObjectID.SolariteWorkbench)
                FillGearSlot(recipes, 0, recipes.Length, mirror);
        }
        private static void FillGearSlot(DynamicBuffer<CanCraftObjectsBuffer> recipes, int start, int count, ObjectID mirror)
        {
            int end = System.Math.Min(start + count, recipes.Length);
            for (int i = start; i < end; i++) if (recipes[i].objectID == mirror) return;
            // Vanilla Solarite Gear has four empty slots (2..5). Do not shift any
            // recipe indices or overwrite another mod's recipe.
            for (int i = start + 2; i < System.Math.Min(start + 6, end); i++)
                if (recipes[i].objectID == ObjectID.None)
                {
                    recipes[i] = new CanCraftObjectsBuffer { objectID = mirror, amount = 1 };
                    Debug.Log("[MagicMirror] Solarite Workbench recipe registered.");
                    return;
                }
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
            stations = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadWrite<CanCraftObjectsBuffer>());
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
