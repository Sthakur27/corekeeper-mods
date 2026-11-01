using Pug.Sprite;
using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace SolariteDrill
{
    /// <summary>
    /// Solarite Drill: a stationary drill one tier above the Scarlet Drill, crafted at the Solarite Workbench.
    /// The object itself (four direction prefabs copied from the vanilla Scarlet Drill, 3x its mining damage)
    /// lives in the asset bundle; it draws with the vanilla drill graphics and our own skins
    /// (<see cref="DrillReskinPatch"/>). This class registers the recipe.
    /// </summary>
    public sealed class SolariteDrillMod : IMod
    {
        public const string Version = "1.0.0";
        public const string ObjectName = "SolariteDrill_Drill";
        // SpriteAssetSkin data blocks in the bundle, in the order of the vanilla drill's spritesToReskin.
        public const string DrillSkinName = "SolariteDrill_DrillSkin";
        public const string BaseSkinName = "SolariteDrill_BaseSkin";

        internal static SpriteAssetSkin DrillSkin, BaseSkin;
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
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log("[SolariteDrill] v" + Version + " loaded: Solarite Drill at the Solarite Workbench, 3x Scarlet Drill mining.");
        }
        public void Init() { }
        public void Update() { }
        public void Shutdown() { API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded; }

        public void ModObjectLoaded(Object obj)
        {
            if (!(obj is SpriteAssetSkin skin)) return;
            if (skin.name == DrillSkinName) DrillSkin = skin;
            else if (skin.name == BaseSkinName) BaseSkin = skin;
        }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            EnsureRecipe(entity, em);
        }

        internal static void EnsureRecipe(Entity entity, EntityManager em)
        {
            var drill = Id;
            if (drill == ObjectID.None || !em.HasComponent<ObjectDataCD>(entity)
                || !em.HasBuffer<CanCraftObjectsBuffer>(entity)) return;
            // Read-only first: a writable GetBuffer is a sync point, and nearly every
            // station already has the recipe or is not a Solarite-tier bench.
            int start = 0, count = -1;
            // Higher benches copy lower-tier categories. Keep the recipe in Solarite's
            // stations category on both Solarite and higher-tier workbenches.
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
            int slot = FindStationSlot(recipes, start, count, drill);
            if (slot < 0) return;
            recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            recipes[slot] = new CanCraftObjectsBuffer { objectID = drill, amount = 1 };
            Debug.Log("[SolariteDrill] Solarite Workbench recipe registered.");
        }

        private static int FindStationSlot(DynamicBuffer<CanCraftObjectsBuffer> recipes, int start, int count, ObjectID drill)
        {
            int end = System.Math.Min(start + count, recipes.Length);
            for (int i = start; i < end; i++) if (recipes[i].objectID == drill) return -1;
            // Vanilla Solarite stations (anvil, ...) has four empty slots (8..11). Do not shift
            // any recipe indices or overwrite another mod's recipe.
            for (int i = start + DrillRecipeAuthoring.FirstSlot; i < System.Math.Min(start + DrillRecipeAuthoring.EndSlot, end); i++)
                if (recipes[i].objectID == ObjectID.None) return i;
            return -1;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class DrillRecipeSystem : PugSimulationSystemBase
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
            // Once a second: placed benches loaded from a save keep their saved recipe buffer.
            if (World.Time.ElapsedTime < next || SolariteDrillMod.Id == ObjectID.None) return;
            next = World.Time.ElapsedTime + 1;
            using (var entities = stations.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) SolariteDrillMod.EnsureRecipe(entity, EntityManager);
        }
    }
}
