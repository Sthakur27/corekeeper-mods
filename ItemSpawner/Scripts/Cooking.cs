namespace ItemSpawner
{
    /// <summary>
    /// Cooked dishes the way the cooking pot makes them (InventoryUtility's cooking result): the primary
    /// ingredient's CookingIngredientCD.turnsIntoFood is the dish, CookedFoodCD.GetFoodVariation(a, b)
    /// encodes both ingredients, and the tier is one step for a rare flower or legendary ingredient plus
    /// one step for the cooking talent roll ("Gourmet"): 0 = dish, 1 = rareVersion, 2 = epicVersion.
    /// </summary>
    public static class Cooking
    {
        public static bool IsIngredient(SpawnEntry e) =>
            PugDatabase.TryGetComponent<CookingIngredientCD>(Data(e.Id, e.Variation), out var ingredient) && ingredient.turnsIntoFood != ObjectID.None;

        /// <summary>The dish two ingredients make, or null when the pot can't cook them.</summary>
        public static SpawnEntry Dish(ObjectID a, ObjectID b, bool gourmet)
        {
            var primary = CookedFoodCD.GetPrimaryIngredient(a, b);
            if (!PugDatabase.TryGetComponent<CookingIngredientCD>(primary, out var ingredient) || ingredient.turnsIntoFood == ObjectID.None) return null;
            var id = ingredient.turnsIntoFood;
            int tier = (Bumps(a) || Bumps(b) ? 1 : 0) + (gourmet ? 1 : 0);
            if (tier > 0 && PugDatabase.TryGetComponent<CookedFoodCD>(id, out var food))
            {
                var better = tier >= 2 && food.epicVersion != ObjectID.None ? food.epicVersion : food.rareVersion;
                if (better != ObjectID.None) id = better;
            }
            var info = PugDatabase.GetObjectInfo(id);
            return new SpawnEntry
            {
                Id = id, Variation = CookedFoodCD.GetFoodVariation(a, b),
                Stackable = info == null || info.isStackable, Category = -1
            };
        }

        // Same test as the game: a rare flower or any legendary ingredient raises the dish one tier.
        private static bool Bumps(ObjectID id)
        {
            var info = PugDatabase.GetObjectInfo(id);
            if (info == null) return false;
            if (info.rarity == Rarity.Legendary) return true;
            return info.rarity == Rarity.Rare && PugDatabase.HasComponent<FlowerCD>(Data(id, 0));
        }

        private static ObjectDataCD Data(ObjectID id, int variation) => new ObjectDataCD { objectID = id, amount = 1, variation = variation };
    }
}
