using System.Collections.Generic;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using Rewired;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// The dye palette: hover an armor piece, weapon or tool in your own slots (inventory, hotbar, equipment,
    /// vanity) and press the palette key (default P, rebind under Controls > Armor Dye). A panel opens next
    /// to the slot showing the item's own icon in every color; click one to dye the item ("/dye slot").
    /// The key again, right-click, or closing the inventory closes it.
    /// </summary>
    public static class DyeUI
    {
        public const string KeyBind = "ArmorDye_Palette";

        private struct Entry
        {
            public string name, arg;
            public Color32 swatch;
            public Entry(string name, string arg, Color32 swatch) { this.name = name; this.arg = arg; this.swatch = swatch; }
        }

        private static readonly Entry[] Palette =
        {
            new Entry("Red", "red", new Color32(220, 40, 40, 255)),
            new Entry("Orange", "orange", new Color32(240, 130, 30, 255)),
            new Entry("Gold", "gold", new Color32(240, 200, 50, 255)),
            new Entry("Green", "green", new Color32(60, 190, 70, 255)),
            new Entry("Teal", "teal", new Color32(40, 190, 200, 255)),
            new Entry("Blue", "blue", new Color32(50, 100, 230, 255)),
            new Entry("Purple", "purple", new Color32(150, 70, 220, 255)),
            new Entry("Pink", "pink", new Color32(240, 110, 190, 255)),
            new Entry("White", "white", new Color32(235, 235, 235, 255)),
            new Entry("Black", "black", new Color32(60, 60, 70, 255)),
            new Entry("Hue shift 60", "shift 60", default),
            new Entry("Hue shift 120", "shift 120", default),
            new Entry("Hue shift 180", "shift 180", default),
            new Entry("Hue shift 240", "shift 240", default),
            new Entry("Hue shift 300", "shift 300", default),
            new Entry("Remove dye", "off", default),
        };

        private const int Columns = 4;
        private const float CellStep = 1.25f;
        private const float Padding = 0.25f;

        private static Player _rewired;
        private static GameObject _panel;
        private static InventorySlotUI _slot;
        private static ObjectID _slotItem;
        private static int _slotIndex;
        private static bool _failed;

        public static string NameOf(int index) => Palette[index].name;
        public static bool IsOpen => _panel != null;

        /// <summary>The value a palette entry writes (same parsing as the chat command).</summary>
        private static int ValueOf(Entry e)
        {
            DyeColor.TryParse(e.arg.Split(' '), 0, out int dye);
            return dye;
        }

        public static void OnRewiredStart() => _rewired = ReInput.players.GetPlayer(0);

        public static void Tick()
        {
            if (_failed) return;
            try
            {
                bool inventory = Manager.ui != null && Manager.ui.isPlayerInventoryShowing;
                if (IsOpen && (!inventory || _slot == null || _slot.GetContainedObjectData().objectID != _slotItem))
                {
                    Close();
                    return;
                }
                if (!inventory || _rewired == null || !_rewired.GetButtonDown(KeyBind)) return;
                if (Manager.input != null && Manager.input.textInputIsActive) return;
                if (IsOpen) { Close(); return; }
                if (Manager.ui.currentSelectedUIElement is InventorySlotUI slot) TryOpen(slot);
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogError($"[{ArmorDyeMod.Name}] dye palette disabled: {e}");
            }
        }

        public static void Close()
        {
            if (_panel != null) Object.Destroy(_panel);
            _panel = null;
            _slot = null;
        }

        /// <summary>A preview was clicked: dye the item and close.</summary>
        public static void Pick(int index)
        {
            var comm = CommandModule.ClientCommSystem;
            if (comm != null) comm.SendCommand($"/dye slot {_slotIndex} {Palette[index].arg}", CommandFlags.None);
            else Debug.LogWarning($"[{ArmorDyeMod.Name}] command channel not ready");
            Close();
        }

        private static void TryOpen(InventorySlotUI slot)
        {
            var player = Manager.main != null ? Manager.main.player : null;
            var handler = slot.GetInventoryHandler();
            if (player == null || handler == null || handler.inventoryEntity != player.entity) return;
            var item = slot.GetContainedObjectData();
            if (!DyeColor.CanDye(item.objectData) || slot.icon == null || slot.icon.sprite == null) return;
            Sprite icon = DyeTextures.Original(slot.icon.sprite);

            _slot = slot;
            _slotItem = item.objectID;
            _slotIndex = handler.startPosInBuffer + slot.inventorySlotIndex;
            int current = ArmorRecolor.DyeOf(item);
            Build(slot, icon, current);
        }

        // ---------- build ----------

        private static void Build(InventorySlotUI slot, Sprite icon, int current)
        {
            int rows = (Palette.Length + Columns - 1) / Columns;
            float w = Columns * CellStep + Padding * 2f;
            float h = rows * CellStep + Padding * 2f;
            var like = slot.icon;
            int layer = slot.gameObject.layer;

            // Beside the slot, on whichever side has room (the slot's position on the UI camera's screen).
            Vector3 viewport = new Vector3(0.5f, 0.5f, 0f);
            var cam = Manager.camera != null ? Manager.camera.uiCamera : null;
            if (cam != null) viewport = cam.WorldToViewportPoint(slot.transform.position);
            float side = viewport.x > 0.6f ? -1f : 1f;
            float up = viewport.y < 0.5f ? 1f : -1f;
            Vector3 offset = new Vector3(side * (CellStep / 2f + 0.15f + w / 2f), up * (h / 2f - CellStep / 2f), -1f);

            _panel = new GameObject("ArmorDyePalette");
            _panel.SetActive(false);
            _panel.layer = layer;
            _panel.transform.SetParent(slot.transform.parent, false);
            _panel.transform.localPosition = slot.transform.localPosition + offset;

            var border = AddSprite(_panel.transform, "Border", DyeSprites.Pixel(), new Color(0.36f, 0.24f, 0.12f, 1f), like, 20, layer);
            border.transform.localScale = new Vector3(w + 0.125f, h + 0.125f, 1f);
            var bg = AddSprite(_panel.transform, "Background", DyeSprites.Pixel(), new Color(0.93f, 0.66f, 0.33f, 1f), like, 21, layer);
            bg.transform.localScale = new Vector3(w, h, 1f);
            var bgCol = _panel.AddComponent<BoxCollider>();
            bgCol.size = new Vector3(w, h, 0.1f);
            _panel.AddComponent<DyePaletteBlocker>();

            for (int i = 0; i < Palette.Length; i++)
            {
                var e = Palette[i];
                int r = i / Columns, c = i % Columns;
                var cell = new GameObject("Cell_" + e.name);
                cell.layer = layer;
                cell.transform.SetParent(_panel.transform, false);
                cell.transform.localPosition = new Vector3(-w / 2f + Padding + (c + 0.5f) * CellStep, h / 2f - Padding - (r + 0.5f) * CellStep, -0.1f);
                var col = cell.AddComponent<BoxCollider>();
                col.size = new Vector3(CellStep * 0.95f, CellStep * 0.95f, 0.1f);
                cell.AddComponent<DyeSwatchButton>().index = i;

                var tile = AddSprite(cell.transform, "Tile", DyeSprites.Pixel(), new Color(0.45f, 0.3f, 0.16f, 1f), like, 22, layer);
                tile.transform.localScale = new Vector3(CellStep * 0.92f, CellStep * 0.92f, 1f);

                int value = ValueOf(e);
                Sprite preview = value == 0 ? icon : (DyeTextures.Icon(icon, value) ?? icon);
                var iconSR = AddSprite(cell.transform, "Preview", preview, Color.white, like, 23, layer);
                iconSR.transform.localScale = like.transform.lossyScale.x > 0f && slot.transform.lossyScale.x > 0f
                    ? Vector3.one * (like.transform.lossyScale.x / slot.transform.parent.lossyScale.x)
                    : Vector3.one;

                // Color chip in the corner: the swatch color, a rainbow for hue shifts, an X for remove.
                var chipPos = new Vector3(CellStep * 0.3f, -CellStep * 0.3f, 0f);
                SpriteRenderer chip;
                if (e.arg == "off") chip = AddSprite(cell.transform, "Chip", DyeSprites.Cross(), new Color(0.85f, 0.15f, 0.15f, 1f), like, 24, layer);
                else if (e.arg.StartsWith("shift")) chip = AddSprite(cell.transform, "Chip", DyeSprites.SwatchRainbow(), Color.white, like, 24, layer);
                else chip = AddSprite(cell.transform, "Chip", DyeSprites.SwatchFill(), e.swatch, like, 24, layer);
                chip.transform.localPosition = chipPos;
                chip.transform.localScale = Vector3.one * 0.35f;

                if (value == current && (current != 0 || e.arg == "off"))
                {
                    var ring = AddSprite(cell.transform, "Current", DyeSprites.SwatchRing(), Color.white, like, 25, layer);
                    ring.transform.localScale = Vector3.one * (CellStep * 0.92f);
                }
            }
            _panel.SetActive(true);
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, SpriteRenderer like, int order, int layer)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            if (like != null)
            {
                sr.sortingLayerID = like.sortingLayerID;
                sr.sortingOrder = like.sortingOrder + order;
                sr.sharedMaterial = like.sharedMaterial;
            }
            return sr;
        }
    }

    /// <summary>One color preview in the palette.</summary>
    public class DyeSwatchButton : UIelement
    {
        public int index;

        public override void OnLeftClicked(bool mod1, bool mod2) => DyeUI.Pick(index);

        public override void OnRightClicked(bool mod1, bool mod2) => DyeUI.Close();

        public override TextAndFormatFields GetHoverTitle()
        {
            return new TextAndFormatFields { text = DyeUI.NameOf(index), dontLocalize = true };
        }
    }

    /// <summary>The palette background: swallows clicks between previews; right-click closes.</summary>
    public class DyePaletteBlocker : UIelement
    {
        public override void OnRightClicked(bool mod1, bool mod2) => DyeUI.Close();
    }
}
