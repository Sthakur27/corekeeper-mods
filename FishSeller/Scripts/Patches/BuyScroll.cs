using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace FishSeller.Patches
{
    /// <summary>
    /// Scrollable merchant buy window (client only, any merchant NPC, not vending machines).
    ///
    /// The window shows handler.size slots (sizeX x sizeY of InventoryBuffer[0]) starting at
    /// handler.startPosInBuffer, and every slot reads and buys through that same handler
    /// (SlotUIBase.GetContainedObject, InventorySlotUI price label, InventoryHandler.Buy sends
    /// startPosInBuffer + slot index to the server, which indexes ContainedObjectsBuffer directly). So
    /// scrolling is just moving NPC.inventoryHandler.startPosInBuffer by whole rows over the merchant's
    /// longer buffer (<see cref="MerchantRoom.EnsureRoom"/>) and refreshing the slots. The scroll range ends
    /// at the last stocked slot, so bought-out tails and spare slots never show as empty pages.
    ///
    /// Input: mouse wheel while the pointer is over the window (one row per notch). The hotbar would
    /// also react to the wheel, so PlayerController.HandleHotBarSlotNavigation is skipped while the
    /// pointer is over a scrollable shop. A thin scrollbar is drawn left of the window.
    /// </summary>
    public static class BuyScroll
    {
        private const float StepCooldown = 0.06f;

        private static InventoryHandler _handler;
        private static int _baseStart;
        private static int _offsetRows;
        private static float _nextStep;
        private static bool _logged;

        /// <summary>True while the pointer is over a buy window that can scroll (read by the hotbar patch).</summary>
        public static bool PointerOverScrollableShop { get; private set; }

        private static GameObject _bar;
        private static SpriteRenderer _track;
        private static SpriteRenderer _thumb;
        private static Sprite _pixel;

        /// <summary>Window opened: start at the top.</summary>
        public static void OnShown(BuyUI ui)
        {
            Release();
            Update(ui);
        }

        public static void Update(BuyUI ui)
        {
            PointerOverScrollableShop = false;
            var player = Manager.main != null ? Manager.main.player : null;
            var handler = player != null ? player.activeBuyInventoryHandler : null;
            if (ui == null || ui.root == null || !ui.root.activeInHierarchy || handler == null || !(handler.entityMonoBehaviour is NPC npc) || npc.world == null)
            {
                Release();
                ShowBar(false);
                return;
            }

            var world = npc.world;
            var entity = handler.inventoryEntity;
            if (!EntityUtility.TryGetBuffer(entity, world, out DynamicBuffer<ContainedObjectsBuffer> contained)
                || !EntityUtility.TryGetBuffer(entity, world, out DynamicBuffer<InventoryBuffer> inventories)
                || inventories.Length == 0)
            {
                ShowBar(false);
                return;
            }

            if (_handler != handler)
            {
                Release();
                _handler = handler;
                _baseStart = inventories[0].startIndex;
                _offsetRows = 0;
            }

            int cols = Mathf.Max(1, handler.columns);
            int visibleRows = Mathf.Max(1, Mathf.CeilToInt((float)handler.size / cols));
            int last = -1;
            for (int i = contained.Length - 1; i >= _baseStart; i--)
            {
                if (contained[i].objectID != ObjectID.None) { last = i - _baseStart; break; }
            }
            int totalRows = Mathf.Max(visibleRows, (last + cols) / cols);
            int maxOffset = totalRows - visibleRows;

            if (maxOffset > 0)
            {
                bool over = PointerOver(ui);
                PointerOverScrollableShop = over;
                if (over && Manager.input != null && Manager.input.SystemPrefersKeyboardAndMouse())
                {
                    float wheel = Manager.input.GetScrollValue();
                    if (wheel != 0f && Time.unscaledTime >= _nextStep)
                    {
                        _offsetRows += wheel > 0f ? -1 : 1;
                        _nextStep = Time.unscaledTime + StepCooldown;
                    }
                }
            }
            _offsetRows = Mathf.Clamp(_offsetRows, 0, Mathf.Max(0, maxOffset));

            int start = _baseStart + _offsetRows * cols;
            if (handler.startPosInBuffer != start)
            {
                handler.SetStartPosInBuffer(start);
                Refresh(ui);
            }

            if (!_logged && maxOffset > 0)
            {
                _logged = true;
                Debug.Log($"[{FishSellerMod.Name}] scrollable buy window: {cols} columns, {visibleRows} visible rows of {totalRows}, buffer {contained.Length} slots.");
            }
            DrawBar(ui, maxOffset > 0, visibleRows, totalRows);
        }

        /// <summary>Puts a handler we scrolled back at its first row (it lives on the NPC and is reused next visit).</summary>
        private static void Release()
        {
            if (_handler != null && _handler.startPosInBuffer != _baseStart) _handler.SetStartPosInBuffer(_baseStart);
            _handler = null;
            _offsetRows = 0;
        }

        private static void Refresh(BuyUI ui)
        {
            var slots = ui.buyInventory != null ? ui.buyInventory.itemSlots : null;
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot == null || !slot.gameObject.activeInHierarchy) continue;
                if (slot is InventorySlotUI inv) inv.dirty = true;
                slot.UpdateSlot();
            }
        }

        private static bool PointerOver(BuyUI ui)
        {
            var mouse = Manager.ui != null ? Manager.ui.mouse : null;
            if (mouse == null || mouse.pointer == null || ui.background == null) return false;
            var b = ui.background.bounds;
            Vector3 p = mouse.pointer.position;
            return p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y;
        }

        // ---------------------------------------------------------------- scrollbar

        private static void ShowBar(bool show)
        {
            if (_bar != null && _bar.activeSelf != show) _bar.SetActive(show);
        }

        private static void DrawBar(BuyUI ui, bool show, int visibleRows, int totalRows)
        {
            var bg = ui.background;
            if (!show || bg == null)
            {
                ShowBar(false);
                return;
            }
            if (_bar == null && !CreateBar(ui)) return;
            ShowBar(true);

            var b = bg.bounds;
            float scale = ui.root.transform.lossyScale.y;
            float width = 0.125f * scale;
            float height = b.size.y - 0.5f * scale;
            float x = b.min.x - width * 1.5f;
            float z = bg.transform.position.z;
            Place(_track, new Vector3(x, b.center.y, z), new Vector2(width, height));

            float thumbHeight = height * visibleRows / totalRows;
            float maxOffset = totalRows - visibleRows;
            float t = maxOffset > 0 ? _offsetRows / maxOffset : 0f;
            float top = b.center.y + height / 2f;
            Place(_thumb, new Vector3(x, top - thumbHeight / 2f - t * (height - thumbHeight), z), new Vector2(width, thumbHeight));
        }

        private static void Place(SpriteRenderer sr, Vector3 worldPos, Vector2 worldSize)
        {
            var t = sr.transform;
            t.position = worldPos;
            var parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(
                parentScale.x != 0f ? worldSize.x / parentScale.x : worldSize.x,
                parentScale.y != 0f ? worldSize.y / parentScale.y : worldSize.y,
                1f);
        }

        private static bool CreateBar(BuyUI ui)
        {
            var bg = ui.background;
            if (_pixel == null)
            {
                // 1x1 world unit white sprite, tinted per renderer.
                var tex = Texture2D.whiteTexture;
                _pixel = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            _bar = new GameObject("FishSeller_ScrollBar");
            _bar.transform.SetParent(ui.root.transform, false);
            _track = MakeRenderer("Track", bg, 1, new Color(0f, 0f, 0f, 0.45f));
            _thumb = MakeRenderer("Thumb", bg, 2, new Color(0.95f, 0.85f, 0.6f, 0.95f));
            return true;
        }

        private static SpriteRenderer MakeRenderer(string name, SpriteRenderer like, int orderOffset, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_bar.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _pixel;
            sr.color = color;
            sr.sortingLayerID = like.sortingLayerID;
            sr.sortingOrder = like.sortingOrder + orderOffset;
            if (like.sharedMaterial != null) sr.sharedMaterial = like.sharedMaterial;
            go.layer = like.gameObject.layer;
            return sr;
        }
    }

    [HarmonyPatch(typeof(BuyUI), "ShowContainerUI")]
    public static class BuyScrollShowPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuyUI __instance)
        {
            BuyScroll.OnShown(__instance);
        }
    }

    [HarmonyPatch(typeof(BuyUI), "LateUpdate")]
    public static class BuyScrollLateUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuyUI __instance)
        {
            BuyScroll.Update(__instance);
        }
    }

    /// <summary>The mouse wheel also cycles the hotbar; not while it is scrolling a shop.</summary>
    [HarmonyPatch(typeof(PlayerController), "HandleHotBarSlotNavigation")]
    public static class HotbarWheelPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool swappedItem)
        {
            if (!BuyScroll.PointerOverScrollableShop) return true;
            swappedItem = false;
            return false;
        }
    }
}
