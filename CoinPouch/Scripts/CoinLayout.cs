using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CoinPouch
{
    /// <summary>
    /// The coin slot is one slot appended to the end of the player prefab's ContainedObjectsBuffer, plus
    /// an InventoryBuffer entry (a 1x1 inventory range over it) and a slot requirement that only accepts
    /// Ancient Coins. Because it is an inventory range:
    ///  - pickup prefers it (an empty slot whose requirement the item fulfils wins over the inventory);
    ///  - merchants count it (GetTotalAmount walks the InventoryBuffer ranges) and take coins from it
    ///    (ConsumeObject / DestroyUpToAmountOfEntity walk every slot);
    ///  - it is not dropped on death (only InventoryBuffer[0] is moved to the grave).
    /// The character save stores the whole buffer, so the coins follow the character into every world.
    /// </summary>
    public static class CoinLayout
    {
        /// <summary>Most coins the slot holds. Vanilla stacks stop at 9999; the server tops the slot up past that.</summary>
        public const int MaxCoins = 99999;

        /// <summary>Index of the coin slot in the player's ContainedObjectsBuffer; -1 until the player prefab is seen.</summary>
        public static int Slot { get; private set; } = -1;

        /// <summary>Index of the coin slot's entry in the player's InventoryBuffer.</summary>
        public static int InventoryIndex { get; private set; } = -1;

        public static bool Ready => Slot >= 0 && InventoryIndex >= 0;

        public static void OnObjectTypeAdded(Entity entity, GameObject authoringData, EntityManager em)
        {
            if (authoringData == null || !authoringData.TryGetComponent<PlayerAuthoring>(out _)) return;
            if (!em.HasBuffer<ContainedObjectsBuffer>(entity) || !em.HasBuffer<InventoryBuffer>(entity)) return;

            var contained = em.GetBuffer<ContainedObjectsBuffer>(entity);
            int slot = contained.Length;
            contained.Add(default);

            var inventories = em.GetBuffer<InventoryBuffer>(entity);
            int index = inventories.Length;
            inventories.Add(Range(slot));

            if (!em.HasBuffer<InventorySlotRequirementBuffer>(entity)) em.AddBuffer<InventorySlotRequirementBuffer>(entity);
            em.GetBuffer<InventorySlotRequirementBuffer>(entity).Add(Requirement(index));

            if (Ready && (slot != Slot || index != InventoryIndex))
                Debug.LogWarning($"[{CoinPouchMod.Name}] Coin slot differs between worlds (had {Slot}/{InventoryIndex}, now {slot}/{index}). Another mod may be adding player slots later.");
            Slot = slot;
            InventoryIndex = index;
            Debug.Log($"[{CoinPouchMod.Name}] Coin slot {Slot}, inventory range {InventoryIndex} ({em.World.Name}).");
        }

        public static InventoryBuffer Range(int slot) => new InventoryBuffer
        {
            sizeX = 1,
            sizeY = 1,
            maxSize = 1,
            startIndex = slot,
            extraInventorySizeSlot = -1,
        };

        public static InventorySlotRequirementBuffer Requirement(int inventoryIndex)
        {
            var ids = new FixedList32Bytes<ObjectID>();
            ids.Add(ObjectID.AncientCoin);
            return new InventorySlotRequirementBuffer
            {
                requirementAppliesToAllSlots = true,
                inventoryIndex = inventoryIndex,
                dontShowAnyHint = true,
                acceptsObjectIds = ids,
            };
        }

        public static bool IsRequirement(InventorySlotRequirementBuffer r) =>
            r.inventoryIndex == InventoryIndex && r.acceptsObjectIds.Length == 1 && r.acceptsObjectIds[0] == ObjectID.AncientCoin;

        /// <summary>
        /// Server: fixes a decoded character before its slots are copied into the player. The coin slot is
        /// always the last player slot, so if another mod's slots were added or removed since the save,
        /// the saved coins are in the last saved slot instead of <see cref="Slot"/>; move them. Runs before
        /// Ender Stash's own relocation, which may write its stash over the old coin position.
        /// </summary>
        public static void Relocate(CharacterData data)
        {
            if (data == null || data.inventory == null || !Ready) return;
            List<ObjectDataCD> inventory = data.inventory;
            int last = inventory.Count - 1;
            if (last < 0 || last == Slot || inventory[last].objectID != ObjectID.AncientCoin) return;

            List<string> names = data.inventoryObjectNames;
            List<CharacterInventoryAuxData> aux = data.inventoryAuxData;
            while (inventory.Count <= Slot) inventory.Add(default);
            if (names != null) while (names.Count < inventory.Count) names.Add(null);
            if (aux != null) while (aux.Count < inventory.Count) aux.Add(default);

            ObjectDataCD coins = inventory[last];
            ObjectDataCD target = inventory[Slot];
            if (target.objectID != ObjectID.None && target.objectID != ObjectID.AncientCoin) return; // never overwrite an item
            if (target.objectID == ObjectID.AncientCoin) coins.amount = System.Math.Min(MaxCoins, coins.amount + target.amount);
            inventory[Slot] = coins;
            if (names != null) names[Slot] = names[last];
            inventory[last] = default;
            if (names != null) names[last] = null;
            if (aux != null) aux[last] = default;
            Debug.Log($"[{CoinPouchMod.Name}] Player slot layout changed: moved {coins.amount} coins from slot {last} to {Slot} in the saved character {data.characterGuid}.");
        }
    }
}
