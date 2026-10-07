using System.Collections.Generic;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// The dye bucket: a button in the character window's free bottom-left cell (under the vanity column).
    /// Left-click picks the next color, right-click the previous one; while a color is picked, left-clicking
    /// an armor item in your own inventory, hotbar, equipment or vanity slots dyes that item ("/dye slot").
    /// "Remove dye" clears it. The bucket goes back to "off" when the inventory closes.
    /// </summary>
    public static class DyeUI
    {
        private struct Entry
        {
            public string name, arg;
            public Color32 swatch;
            public Entry(string name, string arg, Color32 swatch) { this.name = name; this.arg = arg; this.swatch = swatch; }
        }

        private static readonly Entry[] Palette =
        {
            new Entry("Off", null, new Color32(0, 0, 0, 0)),
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
            new Entry("Hue shift 60", "shift 60", new Color32(255, 255, 255, 255)),
            new Entry("Hue shift 120", "shift 120", new Color32(255, 255, 255, 255)),
            new Entry("Hue shift 180", "shift 180", new Color32(255, 255, 255, 255)),
            new Entry("Hue shift 240", "shift 240", new Color32(255, 255, 255, 255)),
            new Entry("Hue shift 300", "shift 300", new Color32(255, 255, 255, 255)),
            new Entry("Remove dye", "off", new Color32(255, 255, 255, 255)),
        };

        private const int PaletteColumns = 4;
        private const float SwatchStep = 0.9f;
        private const float PanelPadding = 0.3f;

        private static int _selected;   // remembered color (palette index; 0 = none yet)
        private static bool _armed;     // dye mode: left-clicks on dyeable items dye them
        private static GameObject _bucket, _palette;
        private static SpriteRenderer _paint, _rainbow, _cross;
        private static readonly List<SpriteRenderer> _bucketSprites = new List<SpriteRenderer>();
        private static readonly List<SpriteRenderer> _rings = new List<SpriteRenderer>();
        private static bool _failed;

        public static bool Active => _armed && _selected != 0;
        public static string SelectedName => Palette[_selected].name;
        public static string NameOf(int index) => Palette[index].name;
        public static bool PaletteOpen => _palette != null && _palette.activeSelf;

        public static void Tick()
        {
            if (_failed) return;
            try
            {
                if (_bucket == null) TryInject();
                // Closing the inventory ends dye mode (so clicks are normal when it opens again) but the
                // color is remembered, and the palette shows it ringed.
                if (Manager.ui == null || !Manager.ui.isPlayerInventoryShowing)
                {
                    if (_armed) SetArmed(false);
                    if (PaletteOpen) _palette.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogError($"[{ArmorDyeMod.Name}] dye bucket disabled: {e}");
            }
        }

        /// <summary>Left-click on the bucket: open/close the palette.</summary>
        public static void TogglePalette()
        {
            if (_palette != null) _palette.SetActive(!_palette.activeSelf);
        }

        /// <summary>Right-click on the bucket: stop dyeing (normal clicks again); the color is kept.</summary>
        public static void PutAway()
        {
            SetArmed(false);
            if (_palette != null) _palette.SetActive(false);
        }

        /// <summary>A swatch was clicked: remember it, turn dye mode on and close the palette.</summary>
        public static void Pick(int index)
        {
            Select(index);
            SetArmed(true);
            if (_palette != null) _palette.SetActive(false);
        }

        /// <summary>Dye mode on/off; the bucket is drawn dimmed while off.</summary>
        private static void SetArmed(bool armed)
        {
            _armed = armed;
            float a = armed ? 1f : 0.45f;
            foreach (var sr in _bucketSprites)
            {
                if (sr == null) continue;
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, a);
            }
        }

        private static void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _rings.Count; i++)
                if (_rings[i] != null) _rings[i].enabled = i + 1 == index;
            if (_paint == null) return;
            var e = Palette[index];
            bool shift = e.arg != null && e.arg.StartsWith("shift");
            bool remove = e.arg == "off";
            _paint.enabled = index != 0 && !shift && !remove;
            _paint.color = new Color32(e.swatch.r, e.swatch.g, e.swatch.b, (byte)(_armed ? 255 : 115));
            _rainbow.enabled = shift;
            _cross.enabled = remove;
        }

        /// <summary>Dyes the item at absolute index <paramref name="slot"/> of the local player's inventory with the picked color.</summary>
        public static void ApplyTo(int slot)
        {
            var e = Palette[_selected];
            if (e.arg == null) return;
            var comm = CommandModule.ClientCommSystem;
            if (comm == null)
            {
                Debug.LogWarning($"[{ArmorDyeMod.Name}] command channel not ready");
                return;
            }
            comm.SendCommand($"/dye slot {slot} {e.arg}", CommandFlags.None);
        }

        // ---------- build ----------

        private static void TryInject()
        {
            var window = Manager.ui != null ? Manager.ui.characterWindow : null;
            if (window == null) return;
            Transform windowRoot = window.transform.Find("root");
            Transform equip = windowRoot != null ? windowRoot.Find("CharacterEquipment") : null;
            Transform equipRoot = equip != null ? equip.Find("root") : null;
            if (equipRoot == null || !equip.gameObject.activeInHierarchy) return;
            Transform breast = equipRoot.Find("breastSlot");
            Transform pants = equipRoot.Find("pantsSlot");
            if (breast == null || pants == null) return;

            // The free cell is one row under the vanity pants slot (Loadout Fallback's column left of the armor).
            Transform vanityPants = equipRoot.Find("loadoutVanitypantsSlot");
            Vector3 column = vanityPants != null ? vanityPants.localPosition : pants.localPosition + new Vector3(-1.36f, 0f, 0f);
            float row = breast.localPosition.y - pants.localPosition.y;
            Vector3 pos = column + new Vector3(0f, -row, 0f);

            var iconTf = pants.Find("container/IconContainer/Icon");
            var iconSR = iconTf != null ? iconTf.GetComponent<SpriteRenderer>() : null;

            _bucket = new GameObject("ArmorDyeBucket");
            _bucket.SetActive(false);
            _bucket.layer = pants.gameObject.layer;
            _bucket.transform.SetParent(equipRoot, false);
            _bucket.transform.localPosition = pos;

            var col = _bucket.AddComponent<BoxCollider>();
            var srcCol = pants.GetComponent<BoxCollider>();
            col.size = srcCol != null ? srcCol.size : new Vector3(1.25f, 1.25f, 0.1f);
            col.center = srcCol != null ? srcCol.center : Vector3.zero;
            _bucket.AddComponent<DyeBucketButton>();

            _bucketSprites.Clear();
            _bucketSprites.Add(AddSprite(_bucket.transform, "Bucket", DyeSprites.Bucket(), Color.white, iconSR, 1));
            _paint = AddSprite(_bucket.transform, "Paint", DyeSprites.Paint(), Color.white, iconSR, 2);
            _rainbow = AddSprite(_bucket.transform, "Rainbow", DyeSprites.Rainbow(), Color.white, iconSR, 2);
            _cross = AddSprite(_bucket.transform, "Cross", DyeSprites.Cross(), new Color(1f, 0.35f, 0.35f, 1f), iconSR, 3);
            _bucketSprites.Add(_paint);
            _bucketSprites.Add(_rainbow);
            _bucketSprites.Add(_cross);
            BuildPalette(iconSR, col.size);
            Select(_selected);
            SetArmed(_armed);
            _bucket.SetActive(true);
            Debug.Log($"[{ArmorDyeMod.Name}] dye bucket added to the character window at {pos} (row step {row}, vanity column {(vanityPants != null)})");
        }

        /// <summary>
        /// The palette: a panel left of the bucket (over whatever is beside the character window) with one
        /// swatch per entry except "Off". It sits closer to the camera than the UI it covers, and its
        /// background has its own collider, so clicks inside it never reach the slots underneath.
        /// </summary>
        private static void BuildPalette(SpriteRenderer like, Vector3 bucketSize)
        {
            int count = Palette.Length - 1;
            int rows = (count + PaletteColumns - 1) / PaletteColumns;
            float w = PaletteColumns * SwatchStep + PanelPadding * 2f;
            float h = rows * SwatchStep + PanelPadding * 2f;

            _palette = new GameObject("ArmorDyePalette");
            _palette.SetActive(false);
            _palette.layer = _bucket.layer;
            _palette.transform.SetParent(_bucket.transform, false);
            // Right edge just left of the bucket, bottom edge level with the bucket's bottom, in front of other UI.
            _palette.transform.localPosition = new Vector3(-bucketSize.x / 2f - 0.1f - w / 2f, -bucketSize.y / 2f + h / 2f, -1f);

            var border = AddSprite(_palette.transform, "Border", DyeSprites.Pixel(), new Color(0.36f, 0.24f, 0.12f, 1f), like, 20);
            border.transform.localScale = new Vector3(w + 0.125f, h + 0.125f, 1f);
            var bg = AddSprite(_palette.transform, "Background", DyeSprites.Pixel(), new Color(0.93f, 0.66f, 0.33f, 1f), like, 21);
            bg.transform.localScale = new Vector3(w, h, 1f);
            var bgCol = _palette.AddComponent<BoxCollider>();
            bgCol.size = new Vector3(w, h, 0.1f);
            _palette.AddComponent<DyePaletteBlocker>();

            _rings.Clear();
            for (int i = 1; i < Palette.Length; i++)
            {
                int n = i - 1, r = n / PaletteColumns, c = n % PaletteColumns;
                var e = Palette[i];
                var sw = new GameObject("Swatch_" + e.name);
                sw.layer = _bucket.layer;
                sw.transform.SetParent(_palette.transform, false);
                sw.transform.localPosition = new Vector3(-w / 2f + PanelPadding + (c + 0.5f) * SwatchStep, h / 2f - PanelPadding - (r + 0.5f) * SwatchStep, -0.1f);
                var swCol = sw.AddComponent<BoxCollider>();
                swCol.size = new Vector3(SwatchStep * 0.95f, SwatchStep * 0.95f, 0.1f);
                sw.AddComponent<DyeSwatchButton>().index = i;

                AddSprite(sw.transform, "Frame", DyeSprites.SwatchFrame(), Color.white, like, 22);
                bool shift = e.arg.StartsWith("shift");
                bool remove = e.arg == "off";
                if (shift) AddSprite(sw.transform, "Fill", DyeSprites.SwatchRainbow(), Color.white, like, 23);
                else if (remove) AddSprite(sw.transform, "Fill", DyeSprites.SwatchFill(), new Color(0.85f, 0.85f, 0.85f, 1f), like, 23);
                else AddSprite(sw.transform, "Fill", DyeSprites.SwatchFill(), e.swatch, like, 23);
                if (remove) AddSprite(sw.transform, "Cross", DyeSprites.Cross(), new Color(0.85f, 0.15f, 0.15f, 1f), like, 24);
                var ring = AddSprite(sw.transform, "Selected", DyeSprites.SwatchRing(), Color.white, like, 25);
                ring.enabled = false;
                _rings.Add(ring);
            }
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, SpriteRenderer like, int order)
        {
            var go = new GameObject(name);
            go.layer = _bucket.layer;
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

    /// <summary>The clickable part of the bucket (found by the UI mouse's raycast through its BoxCollider).</summary>
    public class DyeBucketButton : UIelement
    {
        public override void OnLeftClicked(bool mod1, bool mod2) => DyeUI.TogglePalette();

        public override void OnRightClicked(bool mod1, bool mod2) => DyeUI.PutAway();

        public override TextAndFormatFields GetHoverTitle()
        {
            return new TextAndFormatFields { text = DyeUI.Active ? "Dyeing: " + DyeUI.SelectedName : "Armor dye (off)", dontLocalize = true };
        }

        public override List<TextAndFormatFields> GetHoverDescription()
        {
            var grey = new Color(0.8f, 0.8f, 0.8f);
            return new List<TextAndFormatFields>
            {
                new TextAndFormatFields { text = "Left-click: choose a color (turns dye mode on)", dontLocalize = true, color = grey },
                new TextAndFormatFields { text = "Then click armor, a weapon or a tool to dye it", dontLocalize = true, color = grey },
                new TextAndFormatFields { text = "Right-click: dye mode off (also when you close the inventory)", dontLocalize = true, color = grey },
            };
        }
    }

    /// <summary>One color in the palette.</summary>
    public class DyeSwatchButton : UIelement
    {
        public int index;

        public override void OnLeftClicked(bool mod1, bool mod2) => DyeUI.Pick(index);

        public override TextAndFormatFields GetHoverTitle()
        {
            return new TextAndFormatFields { text = DyeUI.NameOf(index), dontLocalize = true };
        }
    }

    /// <summary>The palette background: swallows clicks between swatches so they don't hit the slots behind.</summary>
    public class DyePaletteBlocker : UIelement
    {
    }

    /// <summary>While a dye is picked, a left-click on an armor item in the player's own slots dyes it instead.</summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.OnLeftClicked))]
    public static class DyeSlotClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(InventorySlotUI __instance)
        {
            if (!DyeUI.Active) return true;
            if (Manager.ui == null || Manager.ui.mouse == null || Manager.ui.mouse.isHoldingAnyEntity) return true;
            var player = Manager.main != null ? Manager.main.player : null;
            var handler = __instance.GetInventoryHandler();
            if (player == null || handler == null || handler.inventoryEntity != player.entity) return true;
            var item = __instance.GetContainedObjectData();
            if (!DyeColor.CanDye(item.objectData)) return true;
            DyeUI.ApplyTo(handler.startPosInBuffer + __instance.inventorySlotIndex);
            return false;
        }
    }
}
