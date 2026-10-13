using System;
using System.Collections.Generic;
using HarmonyLib;
using Inventory;
using Unity.Entities;
using Unity.Mathematics;

namespace GoldenChance.Patches
{
    // Context is scoped to a managed inventory job, not the local/host player. The SDK's
    // BurstDisabler keeps these jobs managed; thread-local state also isolates client worlds.
    internal static class CookingCollection
    {
        [ThreadStatic] internal static Queue<Entity> Players;
        [ThreadStatic] internal static Entity Player;

        internal struct Transfer
        {
            internal ContainedObjectsBuffer Food;
            internal ObjectID Upgrade;
            internal Entity Player;
            internal int Chance;
            internal int DestinationSlot;
            internal bool Valid;
        }

        internal static Transfer Capture(in InventoryHandlerShared shared, Entity source, int slot)
        {
            if (!shared.isServer || !shared.isFirstTimeFullyPredictingTick || !GoldenChanceMod.RegularCooking
                || Player == Entity.Null || !shared.randomLookup.HasComponent(Player)
                || !shared.localTransformLookup.HasComponent(Player)
                || !shared.craftingLookup.TryGetComponent(source, out var crafting)
                || crafting.craftingType != CraftingType.Cooking || crafting.outputSlotIndex != slot
                || !shared.containedObjectsBufferLookup.TryGetBuffer(source, out var contents)
                || slot < 0 || slot >= contents.Length) return default;

            var food = contents[slot];
            if (food.amount <= 0 || food.objectID == ObjectID.None) return default;
            var bank = shared.databaseBankCD.databaseBankBlob;
            var prefab = PugDatabase.GetPrimaryPrefabEntity(food.objectID, bank, food.variation);
            if (!shared.cookedFoodLookup.TryGetComponent(prefab, out var cooked)) return default;
            var rarity = PugDatabase.GetEntityObjectInfo(food.objectID, bank).rarity;
            if (rarity >= Rarity.Epic) return default;

            // The same ingredient test as vanilla Master Chef. Variation encodes BOTH original
            // ingredients, so the additional benefit from two golden ingredients is preserved.
            bool golden = GoldenIngredient(shared, CookedFoodCD.GetPrimaryIngredientFromVariation(food.variation))
                || GoldenIngredient(shared, CookedFoodCD.GetSecondaryIngredientFromVariation(food.variation));
            var upgrade = golden || rarity == Rarity.Rare ? cooked.epicVersion : cooked.rareVersion;
            if (upgrade == ObjectID.None || upgrade == food.objectID
                || !shared.cookedFoodLookup.HasComponent(PugDatabase.GetPrimaryPrefabEntity(upgrade, bank))) return default;
            int chance = math.clamp(EntityUtility.GetConditionValue(ConditionID.ChanceForExtraCookedFoodToBeRare,
                Player, shared.summarizedConditionsBufferLookup), 0, 100);
            return new Transfer { Food = food, Upgrade = upgrade, Player = Player, Chance = chance,
                DestinationSlot = -1, Valid = chance > 0 };
        }

        private static bool GoldenIngredient(in InventoryHandlerShared shared, ObjectID ingredient)
        {
            var bank = shared.databaseBankCD.databaseBankBlob;
            var rarity = PugDatabase.GetEntityObjectInfo(ingredient, bank).rarity;
            return rarity == Rarity.Legendary || (rarity == Rarity.Rare && shared.flowerLookup.HasComponent(
                PugDatabase.GetPrimaryPrefabEntity(ingredient, bank)));
        }

        internal static int Roll(in InventoryHandlerShared shared, Transfer transfer, int amount)
        {
            if (!transfer.Valid || amount <= 0) return 0;
            if (transfer.Chance >= 100) return amount;
            int upgraded = 0;
            ref var random = ref shared.randomLookup.GetRefRW(transfer.Player).ValueRW;
            for (int i = 0; i < amount; i++)
                if (random.Value.NextFloat() < transfer.Chance / 100f) upgraded++;
            return upgraded;
        }

        internal static Entity DropUpgraded(in InventoryHandlerShared shared, Transfer transfer, int amount,
            float3 position, Entity pullTowards)
        {
            var upgraded = transfer.Food;
            upgraded.objectData.objectID = transfer.Upgrade;
            upgraded.objectData.amount = amount;
            // Keep variation, variationUpdateCount and auxDataIndex; do not reconstruct the food.
            return EntityUtility.DropNewEntity(shared.ecb, upgraded, position,
                shared.databaseBankCD.databaseBankBlob, pullTowards);
        }

        internal static void UpgradeMoved(in InventoryHandlerShared shared, Transfer transfer,
            Entity destination, int slot, int moved)
        {
            if (!transfer.Valid || moved <= 0 || slot < 0) return;
            var food = shared.containedObjectsBufferLookup[destination][slot];
            if (food.objectID != transfer.Food.objectID || food.variation != transfer.Food.variation) return;
            int upgraded = Roll(shared, transfer, math.min(moved, food.amount));
            if (upgraded <= 0) return;
            var position = shared.localTransformLookup[transfer.Player].Position;
            // Vanilla already moved the items, including locks/aux data. Consume only the newly
            // moved successes, then drop their upgraded equivalents as vanilla bonus food does.
            // A mixed-rarity result cannot fit in the single mouse slot.
            if (InventoryUtility.ConsumeEntityAt(shared, destination, slot, upgraded, true, position,
                optionalTargetObjectID: food.objectID))
                DropUpgraded(shared, transfer, upgraded, position, transfer.Player);
        }
    }

    [HarmonyPatch("Inventory.InventoryUpdateSystem+ProcessInventoryChangesJob", "Execute")]
    internal static class InventoryCollectionContextPatch
    {
        private static bool _logged;
        public static void Prefix(Entity ___inventoryChangeBufferEntity,
            BufferLookup<InventoryChangeBuffer> ___inventoryChangeBufferLookup,
            InventoryHandlerShared ___inventoryHandlerShared, bool ___worldIsReadOnly)
        {
            CookingCollection.Player = Entity.Null;
            CookingCollection.Players = null;
            if (___inventoryHandlerShared.isServer && !_logged)
            {
                _logged = true;
                UnityEngine.Debug.Log("[GoldenChance] Managed server inventory collection hook active.");
            }
            if (!___inventoryHandlerShared.isServer || !GoldenChanceMod.RegularCooking) return;
            var queue = new Queue<Entity>();
            var changes = ___inventoryChangeBufferLookup[___inventoryChangeBufferEntity];
            for (int i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                // Exactly mirror the dispatcher's guest-mode gate, so skipped requests cannot
                // shift ownership onto another player's request later in the same batch.
                if (!___worldIsReadOnly || !___inventoryHandlerShared.playerGhostLookup.TryGetComponent(change.playerEntity, out var ghost)
                    || ghost.adminPrivileges > 0 || change.inventoryChangeData.inventoryAction == InventoryAction.MoveOrDropItemIgnoreGuestMode)
                    queue.Enqueue(change.playerEntity);
            }
            CookingCollection.Players = queue;
        }

        public static void Finalizer()
        {
            CookingCollection.Player = Entity.Null;
            CookingCollection.Players = null;
        }
    }

    [HarmonyPatch(typeof(InventoryUpdateSystem), "ProcessInventoryChange")]
    internal static class InventoryCollectionPlayerPatch
    {
        public static void Prefix()
        {
            var queue = CookingCollection.Players;
            CookingCollection.Player = queue != null && queue.Count > 0 ? queue.Dequeue() : Entity.Null;
        }
    }

    // Recipe changes collect the old output inside the separate crafting job. Its explicit
    // playerEntity must override (and restore) the inventory-job context.
    [HarmonyPatch(typeof(InventoryUtility), nameof(InventoryUtility.SetupCookBookRecipe))]
    internal static class CookbookCollectionContextPatch
    {
        public static void Prefix(Entity playerEntity, out Entity __state)
        {
            __state = CookingCollection.Player;
            CookingCollection.Player = playerEntity;
        }
        public static void Finalizer(Entity __state) { CookingCollection.Player = __state; }
    }

    [HarmonyPatch(typeof(InventoryUtility), nameof(InventoryUtility.TryMove))]
    internal static class MoveCookedFoodPatch
    {
        public static void Prefix(in InventoryHandlerShared inventoryHandlerShared, Entity inventoryFrom,
            int indexFrom, Entity inventoryTo, int indexToHint, int endIndex, bool isQuickStacking,
            bool allowMoveToEmptyBackupSlot, out CookingCollection.Transfer __state)
        {
            __state = inventoryFrom == inventoryTo ? default : CookingCollection.Capture(inventoryHandlerShared, inventoryFrom, indexFrom);
            if (__state.Valid)
                __state.DestinationSlot = InventoryUtility.TryFindSlotToAddTo(inventoryHandlerShared,
                    __state.Food.objectID, inventoryTo, indexToHint, endIndex, __state.Food.variation,
                    isQuickStacking, allowMoveToEmptyBackupSlot);
        }

        public static void Postfix(in InventoryHandlerShared inventoryHandlerShared, Entity inventoryFrom,
            int indexFrom, Entity inventoryTo, CookingCollection.Transfer __state, bool __result)
        {
            if (!__state.Valid || !__result) return;
            int remaining = inventoryHandlerShared.containedObjectsBufferLookup[inventoryFrom][indexFrom].amount;
            CookingCollection.UpgradeMoved(inventoryHandlerShared, __state, inventoryTo,
                __state.DestinationSlot, __state.Food.amount - remaining);
        }
    }

    [HarmonyPatch(typeof(InventoryUtility), nameof(InventoryUtility.Swap))]
    internal static class SwapCookedFoodPatch
    {
        public static void Prefix(in InventoryHandlerShared inventoryHandlerShared, Entity inventory1,
            Entity inventory2, int index1, int index2, out CookingCollection.Transfer __state)
        {
            __state = inventory1 == inventory2 ? default : CookingCollection.Capture(inventoryHandlerShared, inventory1, index1);
            // UI collection swaps into an empty mouse slot. Never roll objects placed INTO a pot.
            if (__state.Valid && inventoryHandlerShared.containedObjectsBufferLookup[inventory2][index2].objectID != ObjectID.None)
                __state = default;
        }
        public static void Postfix(in InventoryHandlerShared inventoryHandlerShared, Entity inventory2,
            int index2, CookingCollection.Transfer __state)
        {
            CookingCollection.UpgradeMoved(inventoryHandlerShared, __state, inventory2, index2, __state.Food.amount);
        }
    }

    [HarmonyPatch(typeof(InventoryUtility), nameof(InventoryUtility.DropItem))]
    internal static class DropCookedFoodPatch
    {
        public static bool Prefix(in InventoryHandlerShared inventoryHandlerShared, Entity inventory, int index,
            ref int amount, float3 position, Entity blockPickupFor, Entity pullTowardsEntity, bool ignoreRayChecksForPickup)
        {
            var transfer = CookingCollection.Capture(inventoryHandlerShared, inventory, index);
            if (!transfer.Valid) return true;
            int selected = math.min(math.max(amount, 0), transfer.Food.amount);
            int upgraded = CookingCollection.Roll(inventoryHandlerShared, transfer, selected);
            if (upgraded == 0) return true;
            if (!InventoryUtility.ConsumeEntityAt(inventoryHandlerShared, inventory, index, upgraded, true, position,
                optionalTargetObjectID: transfer.Food.objectID)) return true;
            var food = transfer.Food;
            food.objectData.objectID = transfer.Upgrade;
            food.objectData.amount = upgraded;
            var dropped = EntityUtility.DropNewEntity(inventoryHandlerShared.ecb, food, position,
                inventoryHandlerShared.databaseBankCD.databaseBankBlob, pullTowardsEntity, false, ignoreRayChecksForPickup);
            if (blockPickupFor != Entity.Null)
                inventoryHandlerShared.ecb.SetComponent(dropped, new PickUpItemCD
                { state = PickUpItemState.BlockPickupUntilReEnterStart, targetEntity = blockPickupFor });
            amount = selected - upgraded;
            return amount > 0;
        }
    }
}
