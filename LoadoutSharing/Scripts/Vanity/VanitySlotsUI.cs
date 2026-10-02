using System;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Adds the three vanilla vanity slots (helm / breast / pants) to the character window, to the left of
    /// the armor column, so vanity can be set per loadout without the vanity furniture. Idea from the
    /// mod.io mod "Vanity Slots"; this is an independent implementation. The clones are ordinary
    /// InventorySlotUI objects with the game's own vanity slot types, so drag and drop, the
    /// click-to-hide toggle and server-side placement are all vanilla code; VanitySync makes them show
    /// the active loadout's vanity. The window background widens to the left and the loadout tabs,
    /// skills and souls panels shift with it (Five Loadouts only moves tabs vertically, so it stays aligned).
    /// </summary>
    public static class VanitySlotsUI
    {
        private const float SlotOffsetX = -1.36f;
        private const float WidenBy = 1.35f;
        private const float TabsOffsetX = -1.35f;
        private const float SidePanelsOffsetX = -0.68f;

        private static bool _injected;
        private static Transform _windowRoot;
        private static Vector3 _appliedRootPos;
        private static bool _shiftActive;
        private static bool _failed;
        private static Transform[] _slots;
        private static SpriteRenderer[] _eyes;
        private static Sprite _eyeSprite;

        public static void Update()
        {
            if (_failed) return;
            try
            {
                if (!_injected) TryInject();
                else
                {
                    UpdateEyes();
                    KeepShifted();
                }
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError($"[{LoadoutSharingMod.Name}] Vanity slots UI disabled: {ex}");
            }
        }

        private static void TryInject()
        {
            var window = Manager.ui != null ? Manager.ui.characterWindow : null;
            if (window == null) return;
            Transform windowRoot = window.transform.Find("root");
            Transform equip = windowRoot != null ? windowRoot.Find("CharacterEquipment") : null;
            Transform equipRoot = equip != null ? equip.Find("root") : null;
            if (equipRoot == null || !equip.gameObject.activeInHierarchy) return;
            if (equipRoot.Find("vanityHelmSlot") != null)
            {
                // Another vanity-slot mod (e.g. mod.io "Vanity Slots") already added them; ours would duplicate.
                Debug.Log($"[{LoadoutSharingMod.Name}] Vanity slots already present in the character window; not adding ours.");
                _injected = true;
                _slots = new Transform[0];
                return;
            }

            string[] armor = { "helmSlot", "breastSlot", "pantsSlot" };
            var types = new[] { ItemSlotsUIType.HelmVanitySlot, ItemSlotsUIType.BreastVanitySlot, ItemSlotsUIType.PantsVanitySlot };
            _slots = new Transform[3];
            _eyes = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                Transform original = equipRoot.Find(armor[i]);
                if (original == null) return; // layout not ready yet; retry next frame
            }
            for (int i = 0; i < 3; i++)
            {
                Transform original = equipRoot.Find(armor[i]);
                Transform clone = UnityEngine.Object.Instantiate(original.gameObject, equipRoot).transform;
                clone.name = "loadoutVanity" + armor[i];
                clone.localPosition = original.localPosition + new Vector3(SlotOffsetX, 0f, 0f);
                var ui = clone.GetComponent<InventorySlotUI>();
                if (ui != null) ui.slotType = types[i];
                _slots[i] = clone;
                _eyes[i] = AddEye(clone);
            }

            Transform bg = windowRoot.Find("Background");
            var bgSR = bg != null ? bg.GetComponent<SpriteRenderer>() : null;
            if (bgSR != null)
            {
                if (bgSR.drawMode == SpriteDrawMode.Sliced || bgSR.drawMode == SpriteDrawMode.Tiled)
                    bgSR.size = new Vector2(bgSR.size.x + WidenBy, bgSR.size.y);
                else
                    bg.localScale = new Vector3(bg.localScale.x + WidenBy, bg.localScale.y, bg.localScale.z);
                bg.localPosition += new Vector3(-WidenBy / 2f, 0f, 0f);
            }
            Transform tabs = equip.Find("PresetTabs");
            if (tabs != null) tabs.localPosition += new Vector3(TabsOffsetX, 0f, 0f);
            foreach (var name in new[] { "SkillsWindow", "SoulsWindow" })
            {
                Transform panel = windowRoot.Find(name);
                if (panel == null) continue;
                panel.localPosition += new Vector3(SidePanelsOffsetX, 0f, 0f);
                var panelBg = panel.Find("Background");
                var sr = panelBg != null ? panelBg.GetComponent<SpriteRenderer>() : null;
                if (sr != null && name == "SoulsWindow" && (sr.drawMode == SpriteDrawMode.Sliced || sr.drawMode == SpriteDrawMode.Tiled))
                    sr.size = new Vector2(sr.size.x + WidenBy, sr.size.y);
            }
            // The extra column widens the window to the left, over panels the game places left of it
            // (repair / salvage, crafting). Shift the whole window right by the same amount so its left
            // edge stays where vanilla puts it.
            _windowRoot = windowRoot;
            _shiftActive = true;
            KeepShifted();
            _injected = true;
            Debug.Log($"[{LoadoutSharingMod.Name}] Per-loadout vanity slots added to the character window.");
        }

        /// <summary>
        /// Keeps the window root offset by WidenBy to the right. If the game moves the root itself (its own
        /// layout), the new position is taken as the vanilla one and the offset is applied again.
        /// </summary>
        private static void KeepShifted()
        {
            if (!_shiftActive || _windowRoot == null) return;
            Vector3 p = _windowRoot.localPosition;
            if (p == _appliedRootPos) return;
            _appliedRootPos = p + new Vector3(WidenBy, 0f, 0f);
            _windowRoot.localPosition = _appliedRootPos;
        }

        /// <summary>A small eye on each vanity slot, shown while the slot is empty, so they read as vanity.</summary>
        private static SpriteRenderer AddEye(Transform slot)
        {
            Transform container = slot.Find("container") ?? slot;
            var icon = container.Find("IconContainer/Icon");
            var iconSR = icon != null ? icon.GetComponent<SpriteRenderer>() : null;

            var go = new GameObject("LoadoutVanityEye");
            go.transform.SetParent(container, false);
            go.transform.localPosition = Vector3.zero;
            go.layer = slot.gameObject.layer;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EyeSprite();
            sr.color = new Color(0.82f, 0.7f, 1f, 0.85f);
            if (iconSR != null)
            {
                sr.sortingLayerID = iconSR.sortingLayerID;
                sr.sortingOrder = iconSR.sortingOrder + 1;
                sr.sharedMaterial = iconSR.sharedMaterial;
            }

            // Hide the armor hint silhouette so the slot does not look like a second armor slot.
            var hint = container.Find("HintIconContainer");
            if (hint != null) hint.gameObject.SetActive(false);
            return sr;
        }

        private static void UpdateEyes()
        {
            if (_slots == null || _eyes == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null || _eyes[i] == null) continue;
                var ui = _slots[i].GetComponent<InventorySlotUI>();
                bool empty = ui == null || ui.GetObjectData().objectID == ObjectID.None;
                if (_eyes[i].enabled != empty) _eyes[i].enabled = empty;
            }
        }

        private static Sprite EyeSprite()
        {
            if (_eyeSprite != null) return _eyeSprite;
            string[] rows =
            {
                "....XXXXX....",
                "..XX.....XX..",
                ".X...XXX...X.",
                "X...XXOXX...X",
                ".X...XXX...X.",
                "..XX.....XX..",
                "....XXXXX....",
            };
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = rows[h - 1 - y][x];
                    tex.SetPixel(x, y, c == 'X' ? Color.white : c == 'O' ? new Color(0.3f, 0.2f, 0.5f, 1f) : new Color(0, 0, 0, 0));
                }
            tex.Apply();
            _eyeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            return _eyeSprite;
        }
    }
}
