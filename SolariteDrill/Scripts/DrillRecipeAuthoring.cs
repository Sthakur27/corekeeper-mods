using HarmonyLib;

namespace SolariteDrill
{
    // AvailableRecipesFromContentBundlesSystem restores each recipe from
    // Crafting/unfilteredRecipes periodically. Editing just the live ECS
    // buffer therefore makes the drill disappear until the repair pass runs.
    // Insert before InventoryConverter caches BOTH recipes and prerequisites.
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class DrillRecipeAuthoring
    {
        // Solarite Workbench stations category: anvil and one more station, then four empty slots.
        internal const int FirstSlot = 8, EndSlot = 12;

        public static void Prefix(CraftingAuthoring craftingAuthoring)
        {
            AddToSolarite(craftingAuthoring);
            if (craftingAuthoring.includeCraftedObjectsFromBuildings == null) return;
            foreach (var included in craftingAuthoring.includeCraftedObjectsFromBuildings)
                AddToSolarite(included);
        }

        private static void AddToSolarite(CraftingAuthoring crafting)
        {
            if (crafting == null) return;
            var data = crafting.GetComponent<EntityMonoBehaviourData>();
            if (data == null || data.objectInfo.objectID != ObjectID.SolariteWorkbench) return;
            var recipes = crafting.canCraftObjects;
            if (recipes == null) return;
            foreach (var recipe in recipes)
                if (recipe.moddedObjectID == SolariteDrillMod.ObjectName) return;
            for (int i = FirstSlot; i < System.Math.Min(EndSlot, recipes.Count); i++)
            {
                var recipe = recipes[i];
                if (recipe.objectID != ObjectID.None || !string.IsNullOrEmpty(recipe.moddedObjectID)) continue;
                recipes[i] = new CraftingAuthoring.CraftableObject
                {
                    moddedObjectID = SolariteDrillMod.ObjectName,
                    amount = 1
                };
                return;
            }
        }
    }
}
