using System;
using UnityEngine;

namespace CoinPouch
{
    /// <summary>
    /// The coin slot UI, shown in two places over the same coin range:
    ///  - <see cref="Slot"/>: in the character window, a clone of the armor pants slot one row under the
    ///    vanity pants slot (the empty corner under the vanity column);
    ///  - <see cref="Dock"/>: right of the hotbar row of the inventory panel, shown only while the character
    ///    window is hidden (merchants, chests, crafting), so the coins are always visible with the inventory.
    /// Both are ordinary InventorySlotUIs with the player-pouch slot type, so drag and drop, hover text and
    /// shift-click are vanilla; the patches in CoinPatches point them at <see cref="Handler"/>, a handler over
    /// the coin slot's inventory range. A faint coin icon shows while the slot is empty.
    /// </summary>
    public static class CoinSlotUI
    {
        /// <summary>X offset of the vanity column from the armor column (Shared Loadout Gear &amp; Vanity's VanitySlotsUI.SlotOffsetX).</summary>
        private const float VanityColumnOffsetX = -1.36f;

        /// <summary>Gap between the last hotbar slot and the docked coin slot, in inventory slot spacings.</summary>
        private const float DockOffsetSlots = 1.25f;

        /// <summary>Frame padding around the docked cell (the player inventory panel adds the same around its rows).</summary>
        private const float DockFramePadding = 0.25f;

        public static InventorySlotUI Slot { get; private set; }
        public static InventorySlotUI Dock { get; private set; }

        private static bool _injected;
        private static bool _failed;
        private static Transform _equip;      // CharacterEquipment, cached so waiting for it to open is one check per frame
        private static Transform _equipRoot;
        private static SpriteRenderer _ghost;
        private static SpriteRenderer _dockGhost;
        private static Transform _lastHotbar; // the dock sits right of this slot
        private static float _dockSpread;
        private static Transform _dockRoot;   // the inventory's slot root (sets the dock's scale)
        private static SpriteRenderer _dockFrame;
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

        // Called for every slot UI every frame: the reference compares reject every other slot before the
        // (slower) Unity null checks.
        public static bool Is(object slot)
        {
            if (ReferenceEquals(slot, Slot)) return Slot != null;
            if (ReferenceEquals(slot, Dock)) return Dock != null;
            return false;
        }

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
                else UpdateGhost(Slot, _ghost);
                UpdateDock();
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError($"[{CoinPouchMod.Name}] Coin slot UI disabled: {ex}");
            }
        }

        /// <summary>CharacterEquipment/root of the character window (exists while the window is hidden too).</summary>
        private static bool FindEquipRoot()
        {
            if (_equipRoot != null) return true;
            var window = Manager.ui != null ? Manager.ui.characterWindow : null;
            if (window == null) return false;
            Transform windowRoot = window.transform.Find("root");
            _equip = windowRoot != null ? windowRoot.Find("CharacterEquipment") : null;
            _equipRoot = _equip != null ? _equip.Find("root") : null;
            return _equipRoot != null;
        }

        private static void TryInject()
        {
            if (!CoinLayout.Ready || !FindEquipRoot()) return;
            if (!_equip.gameObject.activeInHierarchy) return; // wait until the window is first shown
            Transform equipRoot = _equipRoot;
            Transform pants = equipRoot.Find("pantsSlot");
            Transform breast = equipRoot.Find("breastSlot");
            if (pants == null || breast == null) return; // layout not ready yet; retry next frame

            // Under the vanity pants slot, wherever a vanity column put it; else where it would be.
            Transform vanityPants = equipRoot.Find("loadoutVanitypantsSlot") ?? equipRoot.Find("vanityPantsSlot");
            Vector3 above = vanityPants != null ? vanityPants.localPosition : pants.localPosition + new Vector3(VanityColumnOffsetX, 0f, 0f);
            Vector3 row = pants.localPosition - breast.localPosition;

            var ui = MakeSlot(pants, equipRoot, "coinPouchSlot", out _ghost);
            if (ui == null) return;
            ui.transform.localPosition = above + row;
            Slot = ui;
            _injected = true;
            Debug.Log($"[{CoinPouchMod.Name}] Coin slot added to the character window.");
        }

        /// <summary>
        /// The docked copy right of the hotbar row. Shown only while the character window is hidden, so the coins
        /// never appear twice. Parented to the inventory's slot root (not the scrolling main-inventory container),
        /// so it stays put while the inventory scrolls.
        /// </summary>
        private static void UpdateDock()
        {
            if (!CoinLayout.Ready) return;
            if (Dock == null)
            {
                _dockGhost = null;
                if (!TryCreateDock()) return;
            }
            // The frame is not under the slot root, so it also needs the inventory itself to be showing.
            bool show = _dockRoot != null && _dockRoot.gameObject.activeInHierarchy
                && (_equip == null || !_equip.gameObject.activeInHierarchy);
            if (Dock.gameObject.activeSelf != show) Dock.gameObject.SetActive(show);
            if (_dockFrame != null && _dockFrame.gameObject.activeSelf != show) _dockFrame.gameObject.SetActive(show);
            if (!show) return;
            // Follow the last hotbar slot: the inventory lays its slots out only when it is first shown (and
            // again when its size changes), so its position at creation time is not the final one.
            if (_lastHotbar != null)
            {
                Vector3 target = _lastHotbar.localPosition + new Vector3(_dockSpread * DockOffsetSlots, 0f, 0f);
                if (Dock.transform.localPosition != target) Dock.transform.localPosition = target;
            }
            UpdateDockFrame();
            UpdateGhost(Dock, _dockGhost);
        }

        private static bool TryCreateDock()
        {
            var inventory = Manager.ui != null ? Manager.ui.playerInventoryUI : null;
            if (inventory == null || inventory.itemSlotsRoot == null || inventory.itemSlots == null || inventory.itemSlots.Count < 10) return false;

            // A copy of the last hotbar cell, so it looks like the row it sits next to. (Main-inventory cells only
            // draw inside the inventory's scroll mask, so a copy of one is invisible out here.) Its key number
            // label is dropped: with the field cleared, RenderButtonNumber leaves it alone. Index 0 of the coin
            // range: inventorySlotIndex = visibleSlotIndex (+ the player inventory's start index, 0).
            var ui = MakeSlot(inventory.itemSlots[9].transform, inventory.itemSlotsRoot.transform, "coinPouchDock", out _dockGhost);
            if (ui == null) return false;
            if (ui.buttonNumber != null)
            {
                ui.buttonNumber.gameObject.SetActive(false);
                ui.buttonNumber = null;
            }
            ui.visibleSlotIndex = 0;
            _lastHotbar = inventory.itemSlots[9].transform; // positioned in UpdateDock
            _dockSpread = inventory.spread;
            _dockRoot = inventory.itemSlotsRoot.transform;
            Dock = ui;

            // Behind it, a one-cell copy of the inventory panel's frame, so it reads as a small panel of its own.
            if (_dockFrame != null) UnityEngine.Object.Destroy(_dockFrame.gameObject);
            _dockFrame = null;
            SpriteRenderer panel = inventory.backgroundSR;
            if (panel != null && panel.drawMode != SpriteDrawMode.Simple)
            {
                var frameGo = UnityEngine.Object.Instantiate(panel.gameObject, panel.transform.parent);
                frameGo.name = "coinPouchDockFrame";
                foreach (var col in frameGo.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(col); // never block clicks
                _dockFrame = frameGo.GetComponent<SpriteRenderer>();
            }
            Debug.Log($"[{CoinPouchMod.Name}] Coin slot added next to the inventory.");
            return true;
        }

        /// <summary>Keeps the frame centred on the dock and one cell (plus the panel's padding) in size.</summary>
        private static void UpdateDockFrame()
        {
            if (_dockFrame == null || _dockRoot == null) return;
            Transform frame = _dockFrame.transform;
            Vector3 p = Dock.transform.position;
            Vector3 target = new Vector3(p.x, p.y, frame.position.z);
            if (frame.position != target) frame.position = target;
            float parentScale = frame.parent != null ? frame.parent.lossyScale.x : 1f;
            if (parentScale <= 0f) return;
            float side = (_dockSpread + DockFramePadding) * _dockRoot.lossyScale.x / parentScale;
            var size = new Vector2(side, side);
            if (_dockFrame.size != size) _dockFrame.size = size;
        }

        /// <summary>A coin slot cloned from <paramref name="template"/> under <paramref name="parent"/>, with its ghost coin.</summary>
        private static InventorySlotUI MakeSlot(Transform template, Transform parent, string name, out SpriteRenderer ghost)
        {
            ghost = null;
            Transform clone = UnityEngine.Object.Instantiate(template.gameObject, parent).transform;
            clone.name = name;
            var ui = clone.GetComponent<InventorySlotUI>();
            if (ui == null)
            {
                UnityEngine.Object.Destroy(clone.gameObject);
                _failed = true;
                Debug.LogError($"[{CoinPouchMod.Name}] The {template.name} slot has no InventorySlotUI; coin slot UI disabled.");
                return null;
            }
            ui.slotType = ItemSlotsUIType.PouchInventorySlot;
            ghost = AddGhost(clone);
            return ui;
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

        private static void UpdateGhost(InventorySlotUI slot, SpriteRenderer ghost)
        {
            if (slot == null || ghost == null || !slot.isActiveAndEnabled) return; // nothing to show while hidden
            bool empty = slot.GetObjectData().objectID == ObjectID.None;
            if (ghost.enabled != empty) ghost.enabled = empty;
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
