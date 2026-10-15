using System.Collections.Generic;
using HarmonyLib;

namespace ConnectedStorage
{
    // Patch managed conversion, before recipes, prerequisites and included-tier counts
    // are generated together. The vanilla prerequisite system restores from this source.
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class StorageRecipeAuthoringPatch
    {
        public static void Prefix(CraftingAuthoring craftingAuthoring)
        {
            EnsureWoodRecipe(craftingAuthoring);
            if (craftingAuthoring.includeCraftedObjectsFromBuildings != null)
                foreach (var included in craftingAuthoring.includeCraftedObjectsFromBuildings)
                    EnsureWoodRecipe(included);
        }
        internal static void EnsureWoodRecipe(CraftingAuthoring authoring)
        {
            if (authoring == null) return;
            var data = authoring.GetComponent<EntityMonoBehaviourData>();
            if (data == null || data.objectInfo == null || data.objectInfo.objectID != ObjectID.WoodenWorkBench) return;
            var recipes = authoring.canCraftObjects;
            foreach (var recipe in recipes)
                if (recipe.moddedObjectID == "ConnectedStorage_Workbench") return;
            var station = new CraftingAuthoring.CraftableObject {
                objectID = ObjectID.None, moddedObjectID = "ConnectedStorage_Workbench", amount = 1
            };
            for (int i = 12; i < 18; i++)
            {
                if (i < recipes.Count && (recipes[i].objectID != ObjectID.None || !string.IsNullOrEmpty(recipes[i].moddedObjectID))) continue;
                while (recipes.Count <= i) recipes.Add(default(CraftingAuthoring.CraftableObject));
                recipes[i] = station;
                return;
            }
            recipes.Add(station);
        }
    }

    // Titles are localization keys. Use the game's existing crafting term for both
    // panels: these are consecutive chunks of one list, not separate categories.
    [HarmonyPatch(typeof(CraftingBuilding), nameof(CraftingBuilding.GetCraftingUISettings))]
    public static class StorageWorkbenchTitlesPatch
    {
        public static void Postfix(CraftingBuilding __instance, ref CraftingBuilding.CraftingUISettings __result)
        {
            if (!__instance.entityExist || __instance.objectData.objectID != StorageCatalog.Workbench) return;
            __result = new CraftingBuilding.CraftingUISettings(UIManager.CraftingUIThemeType.Wood, "crafting", "crafting");
        }
    }

    [HarmonyPatch(typeof(CraftingBuilding), nameof(CraftingBuilding.Use))]
    public static class StorageWorkbenchOpenPatch
    {
        public static void Prefix(CraftingBuilding __instance)
        {
            if (__instance.entityExist)
                ConnectedStorageMod.EnsureRecipes(__instance.entity, __instance.world.EntityManager);
        }
    }

    // The wooden bench's Base window is its third group of six. Extend its normal
    // category through our last Base slot, instead of inventing a separate gem category.
    [HarmonyPatch(typeof(CraftingBuilding), nameof(CraftingBuilding.GetCraftingCategoryWindowInfos))]
    public static class StorageRecipeTabsPatch
    {
        public static void Postfix(CraftingBuilding __instance,
            ref List<CraftingBuilding.CraftingCategoryWindowInfo> __result)
        {
            if (!__instance.entityExist) return;
            var em = __instance.world.EntityManager;
            if (em.HasBuffer<IncludedCraftingBuildingsBuffer>(__instance.entity))
            {
                // OnSpawn caches these ranges before migration can add the wood recipe.
                // Rebuild from current counts so wood and every following tier stay aligned.
                var included = em.GetBuffer<IncludedCraftingBuildingsBuffer>(__instance.entity);
                var categories = new List<CraftingBuilding.CraftingCategoryWindowInfo>();
                int start = 0;
                foreach (var entry in included)
                {
                    var info = PugDatabase.GetObjectInfo(entry.objectID);
                    categories.Add(new CraftingBuilding.CraftingCategoryWindowInfo {
                        startSlotIndex = start, endSlotIndex = start + entry.amountOfCraftingOptions - 1,
                        icon = info == null ? null : info.smallIcon
                    });
                    start += entry.amountOfCraftingOptions;
                }
                __result = categories;
                return;
            }
            if (__instance.objectData.objectID != ObjectID.WoodenWorkBench || __result == null) return;
            var recipes = __instance.world.EntityManager.GetBuffer<CanCraftObjectsBuffer>(__instance.entity);
            int index = -1;
            for (int i = 0; i < recipes.Length; i++) if (recipes[i].objectID == StorageCatalog.Workbench) { index = i; break; }
            if (index < 0) return;
            var result = new List<CraftingBuilding.CraftingCategoryWindowInfo>();
            bool covered = false;
            foreach (var category in __result)
            {
                int end = category.endSlotIndex;
                if (index < 18 && category.startSlotIndex == 0 && end >= 12) end = System.Math.Max(end, index);
                if (index >= category.startSlotIndex && index <= end) covered = true;
                result.Add(new CraftingBuilding.CraftingCategoryWindowInfo {
                    startSlotIndex = category.startSlotIndex, endSlotIndex = end,
                    icon = category.icon, showAsDefaultDuringSeason = category.showAsDefaultDuringSeason
                });
            }
            if (!covered) result.Add(new CraftingBuilding.CraftingCategoryWindowInfo {
                startSlotIndex = index, endSlotIndex = index,
                icon = PugDatabase.GetObjectInfo(StorageCatalog.Workbench).smallIcon
            });
            __result = result;
        }
    }

    [HarmonyPatch(typeof(CraftingBuilding), nameof(CraftingBuilding.GetCraftingCategoryWindowInfo))]
    public static class StorageRecipeTabSelectionPatch
    {
        public static void Postfix(CraftingBuilding __instance, int currentCategoryIndex,
            ref CraftingBuilding.CraftingCategoryWindowInfo __result)
        {
            if (!__instance.entityExist || (__instance.objectData.objectID != ObjectID.WoodenWorkBench
                && !__instance.world.EntityManager.HasBuffer<IncludedCraftingBuildingsBuffer>(__instance.entity))) return;
            var categories = __instance.GetCraftingCategoryWindowInfos();
            if (categories != null && currentCategoryIndex >= 0 && currentCategoryIndex < categories.Count)
                __result = categories[currentCategoryIndex];
        }
    }
}
