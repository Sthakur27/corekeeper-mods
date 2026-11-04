using HarmonyLib;

namespace WormholePotion
{
    // AvailableRecipesFromContentBundlesSystem restores each recipe from
    // Crafting/unfilteredRecipes periodically. Editing just the live ECS
    // buffer therefore makes the potion disappear until the repair pass runs.
    // Insert before InventoryConverter caches BOTH recipes and prerequisites.
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class PotionRecipeAuthoring
    {
        public static void Prefix(CraftingAuthoring craftingAuthoring)
        {
            AddToPod(craftingAuthoring);
            if (craftingAuthoring.includeCraftedObjectsFromBuildings == null) return;
            foreach (var included in craftingAuthoring.includeCraftedObjectsFromBuildings)
                AddToPod(included);
        }

        private static void AddToPod(CraftingAuthoring crafting)
        {
            if (crafting == null) return;
            var data = crafting.GetComponent<EntityMonoBehaviourData>();
            if (data == null || data.objectInfo.objectID != ObjectID.AncientHologramPod) return;
            var recipes = crafting.canCraftObjects;
            if (recipes == null) return;
            foreach (var recipe in recipes)
                if (recipe.moddedObjectID == WormholePotionMod.PotionName) return;
            // Vanilla's pod has one empty slot at the end (index 17).
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe.objectID != ObjectID.None || !string.IsNullOrEmpty(recipe.moddedObjectID)) continue;
                recipes[i] = new CraftingAuthoring.CraftableObject
                {
                    moddedObjectID = WormholePotionMod.PotionName,
                    amount = 1
                };
                return;
            }
        }
    }
}
