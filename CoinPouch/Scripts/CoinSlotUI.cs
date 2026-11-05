using System;
using UnityEngine;

namespace CoinPouch
{
    /// <summary>
    /// The coin slot in the character window: a clone of the armor pants slot, placed one row under the
    /// vanity pants slot (the empty corner under the vanity column). It is an ordinary InventorySlotUI with
    /// the player-pouch slot type, so drag and drop, hover text and shift-click are vanilla; the patches in
    /// CoinPatches point it at <see cref="Handler"/>, a handler over the coin slot's inventory range.
    /// A faint coin icon shows while the slot is empty.
    /// </summary>
    public static class CoinSlotUI
    {
        /// <summary>X offset of the vanity column from the armor column (Shared Loadout Gear &amp; Vanity's VanitySlotsUI.SlotOffsetX).</summary>
        private const float VanityColumnOffsetX = -1.36f;

        public static InventorySlotUI Slot { get; private set; }

        private static bool _injected;
        private static bool _failed;
        private static Transform _equip;      // CharacterEquipment, cached so waiting for it to open is one check per frame
        private static Transform _equipRoot;
        private static SpriteRenderer _ghost;
        private static InventoryHandler _handler;
        private static PlayerController _handlerPlayer;

        /// <summary>Handler over the local player's coin slot; null before the player and the slot exist.</summary>
        public static InventoryHandler Handler
        {
            get
            {
                PlayerController player = Manager.main != null ? Manager.main.player : null;
                if (player == null || !CoinLayout.Ready) return null;
                if (_handler == null || _handlerPlayer != player)
                {
                    var inventories = EntityUtility.GetBuffer<InventoryBuffer>(player.entity, player.world);
                    if (inventories.Length <= CoinLayout.InventoryIndex) return null;
                    _handler = new InventoryHandler(player, player.world, false, CoinLayout.InventoryIndex);
                    _handlerPlayer = player;
                }
                return _handler;
            }
        }

        // Called for every slot UI every frame: the reference compare rejects every other slot before the
        // (slower) Unity null check.
        public static bool Is(object slot) => ReferenceEquals(slot, Slot) && Slot != null;

        public static void Update()
        {
            if (_failed) return;
            try
            {
                if (_injected && Slot == null)
                {
                    // The character window was rebuilt and our clone destroyed with it: add it again.
                    _injected = false;
                    Slot = null;
                    _ghost = null;
                    _equip = _equipRoot = null;
                }
                if (!_injected) TryInject();
                else UpdateGhost();
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError($"[{CoinPouchMod.Name}] Coin slot UI disabled: {ex}");
            }
        }

        private static void TryInject()
        {
            if (!CoinLayout.Ready) return;
            if (_equipRoot == null)
            {
                var window = Manager.ui != null ? Manager.ui.characterWindow : null;
                if (window == null) return;
                Transform windowRoot = window.transform.Find("root");
                _equip = windowRoot != null ? windowRoot.Find("CharacterEquipment") : null;
                _equipRoot = _equip != null ? _equip.Find("root") : null;
                if (_equipRoot == null) return;
            }
            if (!_equip.gameObject.activeInHierarchy) return; // wait until the window is first shown
            Transform equipRoot = _equipRoot;
            Transform pants = equipRoot.Find("pantsSlot");
            Transform breast = equipRoot.Find("breastSlot");
            if (pants == null || breast == null) return; // layout not ready yet; retry next frame

            // Under the vanity pants slot, wherever a vanity column put it; else where it would be.
            Transform vanityPants = equipRoot.Find("loadoutVanitypantsSlot") ?? equipRoot.Find("vanityPantsSlot");
            Vector3 above = vanityPants != null ? vanityPants.localPosition : pants.localPosition + new Vector3(VanityColumnOffsetX, 0f, 0f);
            Vector3 row = pants.localPosition - breast.localPosition;

            Transform clone = UnityEngine.Object.Instantiate(pants.gameObject, equipRoot).transform;
            clone.name = "coinPouchSlot";
            clone.localPosition = above + row;
            var ui = clone.GetComponent<InventorySlotUI>();
            if (ui == null)
            {
                UnityEngine.Object.Destroy(clone.gameObject);
                _failed = true;
                Debug.LogError($"[{CoinPouchMod.Name}] The pants slot has no InventorySlotUI; coin slot UI disabled.");
                return;
            }
            ui.slotType = ItemSlotsUIType.PouchInventorySlot;
            Slot = ui;
            _ghost = AddGhost(clone);
            _injected = true;
            Debug.Log($"[{CoinPouchMod.Name}] Coin slot added to the character window.");
        }

        /// <summary>A faint Ancient Coin icon on the slot, shown while it is empty.</summary>
        private static SpriteRenderer AddGhost(Transform slot)
        {
            Transform container = slot.Find("container") ?? slot;
            var icon = container.Find("IconContainer/Icon");
            var iconSR = icon != null ? icon.GetComponent<SpriteRenderer>() : null;

            // Hide the pants hint silhouette so the slot does not look like an armor slot.
            var hint = container.Find("HintIconContainer");
            if (hint != null) hint.gameObject.SetActive(false);

            var info = PugDatabase.GetObjectInfo(ObjectID.AncientCoin);
            Sprite sprite = info != null ? (info.smallIcon != null ? info.smallIcon : info.icon) : null;
            if (sprite == null) return null;

            var go = new GameObject("CoinPouchGhost");
            go.transform.SetParent(icon != null ? icon.parent : container, false);
            go.transform.localPosition = icon != null ? icon.localPosition : Vector3.zero;
            go.layer = slot.gameObject.layer;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0f, 0f, 0f, 0.35f);
            if (iconSR != null)
            {
                sr.sortingLayerID = iconSR.sortingLayerID;
                sr.sortingOrder = iconSR.sortingOrder;
                sr.sharedMaterial = iconSR.sharedMaterial;
            }
            return sr;
        }

        private static void UpdateGhost()
        {
            if (Slot == null || _ghost == null || !Slot.isActiveAndEnabled) return; // nothing to show while the window is closed
            bool empty = Slot.GetObjectData().objectID == ObjectID.None;
            if (_ghost.enabled != empty) _ghost.enabled = empty;
        }

        /// <summary>Coins in the local player's coin slot (0 when it is empty or holds something else).</summary>
        public static int CoinsInSlot(InventoryHandler playerInventory)
        {
            if (!CoinLayout.Ready || playerInventory == null) return 0;
            var contained = EntityUtility.GetBuffer<ContainedObjectsBuffer>(playerInventory.inventoryEntity, playerInventory.entityMonoBehaviour.world);
            if (contained.Length <= CoinLayout.Slot) return 0;
            ObjectDataCD data = contained[CoinLayout.Slot].objectData;
            return data.objectID == ObjectID.AncientCoin ? data.amount : 0;
        }
    }
}
