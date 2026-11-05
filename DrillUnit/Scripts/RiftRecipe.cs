using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace DrillUnit
{
    /// <summary>
    /// Puts the Drill Unit on the Rift Statue (PassageCraftingStatue). Vanilla leaves slots 10 and 11 of its
    /// recipe list empty; the unit takes the first free one there, never shifting or overwriting a recipe.
    /// The authoring prefix covers new conversions (AvailableRecipesFromContentBundlesSystem restores recipes
    /// from the authoring list, so the live buffer alone would be undone); <see cref="RiftRecipeSystem"/>
    /// fixes statues loaded from a save.
    /// </summary>
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class RiftRecipe
    {
        internal const int FirstSlot = 10, EndSlot = 12;
        private const ObjectID Statue = ObjectID.PassageCraftingStatue;

        public static void Prefix(CraftingAuthoring craftingAuthoring)
        {
            if (craftingAuthoring == null) return;
            var data = craftingAuthoring.GetComponent<EntityMonoBehaviourData>();
            if (data == null || data.objectInfo.objectID != Statue) return;
            var recipes = craftingAuthoring.canCraftObjects;
            if (recipes == null) return;
            foreach (var recipe in recipes)
                if (recipe.moddedObjectID == DrillUnitMod.ObjectName) return;
            for (int i = FirstSlot; i < System.Math.Min(EndSlot, recipes.Count); i++)
            {
                var recipe = recipes[i];
                if (recipe.objectID != ObjectID.None || !string.IsNullOrEmpty(recipe.moddedObjectID)) continue;
                recipes[i] = new CraftingAuthoring.CraftableObject { moddedObjectID = DrillUnitMod.ObjectName, amount = 1 };
                return;
            }
        }

        internal static void EnsureRecipe(Entity entity, EntityManager em)
        {
            var unit = DrillUnitMod.Id;
            if (unit == ObjectID.None || !em.HasComponent<ObjectDataCD>(entity) || !em.HasBuffer<CanCraftObjectsBuffer>(entity)) return;
            if (em.GetComponentData<ObjectDataCD>(entity).objectID != Statue) return;
            // Read-only first: a writable GetBuffer is a sync point.
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity, true);
            int slot = -1;
            for (int i = 0; i < recipes.Length; i++) if (recipes[i].objectID == unit) return;
            for (int i = FirstSlot; i < System.Math.Min(EndSlot, recipes.Length); i++)
                if (recipes[i].objectID == ObjectID.None) { slot = i; break; }
            if (slot < 0) return;
            recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            recipes[slot] = new CanCraftObjectsBuffer { objectID = unit, amount = 1 };
            Debug.Log($"[{DrillUnitMod.Name}] Rift Statue recipe registered.");
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class RiftRecipeSystem : PugSimulationSystemBase
    {
        private EntityQuery _stations;
        private double _next;
        private int _scannedOrder = -1;
        private double _nextFull;

        protected override void OnCreate()
        {
            base.OnCreate();
            _stations = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<CanCraftObjectsBuffer>());
            RequireForUpdate(_stations);
        }

        protected override void OnUpdate()
        {
            // Placed statues loaded from a save keep their saved recipe buffer. Checked once a second, but the
            // stations are only walked when the set of crafting stations changed (one placed, loaded or removed),
            // plus every 30 s in case a recipe buffer was rewritten in place.
            if (World.Time.ElapsedTime < _next || DrillUnitMod.Id == ObjectID.None) return;
            _next = World.Time.ElapsedTime + 1;
            int order = _stations.GetCombinedComponentOrderVersion();
            if (order == _scannedOrder && World.Time.ElapsedTime < _nextFull) return;
            _scannedOrder = order;
            _nextFull = World.Time.ElapsedTime + 30;
            using (var entities = _stations.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) RiftRecipe.EnsureRecipe(entity, EntityManager);
        }
    }
}
