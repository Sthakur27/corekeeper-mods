using HarmonyLib;
using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Puts the Enchanting Table recipe into an empty slot of a vanilla bench's recipe grid (same approach as the
    /// Solarite Drill): the authoring list before InventoryConverter caches recipes and prerequisites, the live
    /// CanCraftObjectsBuffer for benches loaded from saves, and copies of the bench inside higher-tier benches.
    /// The Octarine Workbench grid is full (18/18), so the table sits in the Galaxite Workbench's stations
    /// category (slots 6..11, slot 11 empty in vanilla) while still costing Octarine Bars.
    /// </summary>
    public static class EnchantRecipe
    {
        public const ObjectID Bench = ObjectID.GalaxiteWorkbench;
        internal const int FirstSlot = 11, EndSlot = 12;

        private static ObjectID _id;
        public static ObjectID TableId
        {
            get
            {
                if (_id == ObjectID.None) _id = API.Authoring.GetObjectID(EnchantingMod.TableObjectName);
                return _id;
            }
        }

        internal static void EnsureRecipe(Entity entity, EntityManager em)
        {
            var table = TableId;
            if (table == ObjectID.None || !em.HasComponent<ObjectDataCD>(entity) || !em.HasBuffer<CanCraftObjectsBuffer>(entity)) return;
            // Read-only first: a writable GetBuffer is a sync point.
            int start = 0, count = -1;
            if (em.HasBuffer<IncludedCraftingBuildingsBuffer>(entity))
            {
                var included = em.GetBuffer<IncludedCraftingBuildingsBuffer>(entity, true);
                for (int i = 0; i < included.Length; i++)
                {
                    if (included[i].objectID == Bench)
                    {
                        count = included[i].amountOfCraftingOptions;
                        break;
                    }
                    start += included[i].amountOfCraftingOptions;
                }
            }
            else if (em.GetComponentData<ObjectDataCD>(entity).objectID == Bench)
                count = int.MaxValue;
            if (count < 0) return;
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity, true);
            if (count == int.MaxValue) count = recipes.Length;
            int end = System.Math.Min(start + count, recipes.Length);
            for (int i = start; i < end; i++) if (recipes[i].objectID == table) return;
            for (int i = start + FirstSlot; i < System.Math.Min(start + EndSlot, end); i++)
            {
                if (recipes[i].objectID != ObjectID.None) continue;
                recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
                recipes[i] = new CanCraftObjectsBuffer { objectID = table, amount = 1 };
                Debug.Log($"[{EnchantingMod.Name}] Enchanting Table recipe registered at {Bench} slot {i - start}.");
                return;
            }
        }
    }

    // AvailableRecipesFromContentBundlesSystem restores recipes from the authoring lists periodically, so the
    // recipe must also be in the authoring list (with its prerequisites) before InventoryConverter caches it.
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class EnchantRecipeAuthoring
    {
        public static void Prefix(CraftingAuthoring craftingAuthoring)
        {
            Add(craftingAuthoring);
            if (craftingAuthoring.includeCraftedObjectsFromBuildings == null) return;
            foreach (var included in craftingAuthoring.includeCraftedObjectsFromBuildings)
                Add(included);
        }

        private static void Add(CraftingAuthoring crafting)
        {
            if (crafting == null) return;
            var data = crafting.GetComponent<EntityMonoBehaviourData>();
            if (data == null || data.objectInfo.objectID != EnchantRecipe.Bench) return;
            var recipes = crafting.canCraftObjects;
            if (recipes == null) return;
            foreach (var recipe in recipes)
                if (recipe.moddedObjectID == EnchantingMod.TableObjectName) return;
            for (int i = EnchantRecipe.FirstSlot; i < System.Math.Min(EnchantRecipe.EndSlot, recipes.Count); i++)
            {
                var recipe = recipes[i];
                if (recipe.objectID != ObjectID.None || !string.IsNullOrEmpty(recipe.moddedObjectID)) continue;
                recipes[i] = new CraftingAuthoring.CraftableObject { moddedObjectID = EnchantingMod.TableObjectName, amount = 1 };
                return;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class EnchantRecipeSystem : PugSimulationSystemBase
    {
        private EntityQuery _stations;
        private double _next;

        protected override void OnCreate()
        {
            base.OnCreate();
            _stations = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<CanCraftObjectsBuffer>());
        }

        protected override void OnUpdate()
        {
            // Once a second: placed benches loaded from a save keep their saved recipe buffer.
            if (World.Time.ElapsedTime < _next || EnchantRecipe.TableId == ObjectID.None) return;
            _next = World.Time.ElapsedTime + 1;
            using (var entities = _stations.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) EnchantRecipe.EnsureRecipe(entity, EntityManager);
        }
    }
}
