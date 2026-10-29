using System.Collections.Generic;
using HarmonyLib;

namespace ConnectedStorage
{
    // Item tooltips use localization terms, which can retain older mod text.
    // Keep the displayed capacity tied to the same table as storage enforcement.
    [HarmonyPatch(typeof(SlotUIBase), nameof(SlotUIBase.GetHoverDescription))]
    public static class StorageCapacityDescriptionPatch
    {
        public static void Postfix(SlotUIBase __instance, ref List<TextAndFormatFields> __result)
        {
            var slot = __instance.GetContainedObject();
            int tier = StorageCatalog.Tier(slot.objectID);
            if (tier < 0) return;
            string text = StorageCatalog.Capacities[tier]
                + " shared storage slots. Interact to access the connected storage inventory."
                + (tier + 1 < StorageCatalog.Capacities.Length ? " Apply the next tier gem to upgrade." : " Maximum storage tier.");
            if (__result == null) __result = new List<TextAndFormatFields>();
            var description = new TextAndFormatFields { text = text, dontLocalize = true };
            if (__result.Count == 0) __result.Add(description);
            else __result[0] = description;
        }
    }

    // Keep vanilla recipe handlers/indices, but give this station one shared frame.
    // Vanilla frame values are captured once, before any widening, and every show
    // rebuilds from them. Capturing after the fact let a second copy of this patch
    // (standalone + overhaul installed together) save the widened frame as "vanilla",
    // which then stuck to every other crafting window.
    [HarmonyPatch(typeof(SimpleCraftingUIContainer), nameof(SimpleCraftingUIContainer.ShowCraftingUI))]
    public static class StorageWorkbenchSinglePanelPatch
    {
        private static SimpleCraftingUI first, second;
        private static UnityEngine.Vector2 size;
        private static UnityEngine.Vector3 position, titlePosition;
        private static bool secondBackground, secondTitle;

        public static void Prefix(SimpleCraftingUIContainer __instance)
        {
            if (__instance.simpleCraftingUIs == null || __instance.simpleCraftingUIs.Count < 2) return;
            if (first != __instance.simpleCraftingUIs[0])
            {
                first = __instance.simpleCraftingUIs[0]; second = __instance.simpleCraftingUIs[1];
                size = first.background.size;
                position = first.background.transform.localPosition;
                titlePosition = first.title.transform.localPosition;
                secondBackground = second.background.enabled;
                secondTitle = second.title.gameObject.activeSelf;
                return;
            }
            first.background.size = size;
            first.background.transform.localPosition = position;
            first.title.transform.localPosition = titlePosition;
            second.background.enabled = secondBackground;
            second.title.gameObject.SetActive(secondTitle);
        }

        public static void Postfix(SimpleCraftingUIContainer __instance)
        {
            var building = CraftingUIBase.GetCraftingBuilding();
            if (first == null || building == null || !building.entityExist || building.objectData.objectID != StorageCatalog.Workbench
                || __instance.amountOfWindowsShowing != 2) return;
            // Measure in the background parent's coordinates to respect native UI scaling.
            var parent = first.background.transform.parent;
            var delta = parent.InverseTransformPoint(second.transform.position)
                - parent.InverseTransformPoint(first.transform.position);
            first.background.size = new UnityEngine.Vector2(size.x + UnityEngine.Mathf.Abs(delta.x), size.y);
            first.background.transform.localPosition = position + delta * .5f;
            var titleParent = first.title.transform.parent;
            var titleDelta = titleParent.InverseTransformPoint(second.transform.position)
                - titleParent.InverseTransformPoint(first.transform.position);
            first.title.transform.localPosition = titlePosition + titleDelta * .5f;
            second.background.enabled = false;
            second.title.gameObject.SetActive(false);
        }
    }

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
