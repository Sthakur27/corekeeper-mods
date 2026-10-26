using HarmonyLib;
using UnityEngine;

namespace MagicMirror
{
    // AvailableRecipesFromContentBundlesSystem restores each recipe from
    // Crafting/unfilteredRecipes periodically. Editing just the live ECS
    // buffer therefore makes the mirror disappear until the repair pass runs.
    // Insert before InventoryConverter caches BOTH recipes and prerequisites.
    [HarmonyPatch(typeof(InventoryConverter), "SetupCanCraftBuffer")]
    public static class MirrorRecipeAuthoring
    {
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
                if (recipe.moddedObjectID == MagicMirrorMod.ObjectName) return;
            for (int i = 2; i < System.Math.Min(6, recipes.Count); i++)
            {
                var recipe = recipes[i];
                if (recipe.objectID != ObjectID.None || !string.IsNullOrEmpty(recipe.moddedObjectID)) continue;
                recipes[i] = new CraftingAuthoring.CraftableObject
                {
                    moddedObjectID = MagicMirrorMod.ObjectName,
                    amount = 1
                };
                return;
            }
        }
    }
}
