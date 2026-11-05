using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlotOrganizer
{
    /// <summary>
    /// Buttons on each world and character slot, right of the trash can: move up, move down, copy,
    /// and (characters only; worlds have the game's own "..." settings) edit name and looks. They are clones of
    /// the slot's delete button (so hover, highlight and click behave like vanilla) with their own
    /// icon; their activation is intercepted below.
    /// </summary>
    internal static class SlotArrows
    {
        private const string NamePrefix = "SlotOrganizer_";

        private sealed class Arrow
        {
            public RadicalMenuOption option;
            public RadicalMenuOption source;   // the row's vanilla delete button
            public Transform row;              // WorldSlot / SaveSlot
            public Kind kind;
            public int dir => kind == Kind.Up ? -1 : kind == Kind.Down ? 1 : 0;
        }

        private enum Kind { Up, Down, Copy, Edit }

        private static readonly Dictionary<RadicalMenu, List<Arrow>> byMenu = new Dictionary<RadicalMenu, List<Arrow>>();
        private static readonly Dictionary<RadicalMenuOption, Arrow> byOption = new Dictionary<RadicalMenuOption, Arrow>();
        private static readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();

        /// <summary>Creates missing arrows and mirrors each row's delete button visibility. Called every frame for the open slot menu.</summary>
        public static void Sync(RadicalMenu menu)
        {
            if (!byMenu.TryGetValue(menu, out List<Arrow> arrows))
            {
                arrows = new List<Arrow>();
                byMenu[menu] = arrows;
            }
            Prune(arrows);
            CreateMissing(menu, arrows);

            int count = SlotOrganizerMod.SlotCount(menu);
            foreach (Arrow a in arrows)
            {
                bool active = a.source.gameObject.activeSelf;
                if (a.option.gameObject.activeSelf != active) a.option.gameObject.SetActive(active);
                int slot = RowIndex(menu, a.row);
                int target = slot + a.dir;
                bool show = active && a.source.enabled && slot >= 0 && target >= 0 && target < count;
                if (a.option.enabled != show) SetEnabled(a.option, show);
                if (!menu.menuOptions.Contains(a.option)) menu.menuOptions.Add(a.option);
            }
        }

        /// <summary>
        /// ChooseCharacterMenu indexes menuOptions as (play, delete) pairs and lays out every entry,
        /// so the arrows must be out of the list while it does that; Sync puts them back next frame.
        /// </summary>
        public static void RemoveFromList(RadicalMenu menu)
        {
            if (!byMenu.TryGetValue(menu, out List<Arrow> arrows)) return;
            foreach (Arrow a in arrows) menu.menuOptions.Remove(a.option);
        }

        /// <summary>Returns true if <paramref name="option"/> is one of our arrows (and handles the click).</summary>
        public static bool TryActivate(RadicalMenuOption option)
        {
            if (option == null || !option.name.StartsWith(NamePrefix)) return false;
            if (!byOption.TryGetValue(option, out Arrow a)) return true;   // ours but stale: just swallow the click
            RadicalMenu menu = Manager.menu.GetTopMenu();
            if (menu == null || !SlotOrganizerMod.IsSlotMenu(menu)) return true;
            int slot = RowIndex(menu, a.row);
            if (a.kind == Kind.Copy)
            {
                int copy = SlotCopy.Copy(menu, slot);
                if (copy >= 0)
                {
                    Sync(menu);
                    SlotOrganizerMod.Select(menu, copy);
                }
                return true;
            }
            if (a.kind == Kind.Edit)
            {
                if (menu is ChooseCharacterMenu charMenu) SlotEdit.Begin(charMenu, slot);
                return true;
            }
            if (SlotOrganizerMod.Move(menu, slot, a.dir))
            {
                Sync(menu);
                // Keep the highlight on the same arrow of the moved slot, so Enter (or a click
                // after following it with the mouse) keeps moving the same world/character.
                Arrow next = Find(menu, slot + a.dir, a.kind);
                if (next != null && next.option.enabled)
                {
                    int index = menu.menuOptions.IndexOf(next.option);
                    if (index >= 0) menu.SelectOptionIndex(index);
                }
            }
            return true;
        }

        private static Arrow Find(RadicalMenu menu, int slot, Kind kind)
        {
            foreach (Arrow a in byMenu[menu])
                if (a.kind == kind && RowIndex(menu, a.row) == slot) return a;
            return null;
        }

        private static void Prune(List<Arrow> arrows)
        {
            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                if (arrows[i].option != null && arrows[i].source != null) continue;
                if (arrows[i].option != null) byOption.Remove(arrows[i].option);
                arrows.RemoveAt(i);
            }
        }

        private static int RowIndex(RadicalMenu menu, Transform row)
        {
            if (menu is SelectWorldMenu worldMenu)
                return System.Array.IndexOf(SlotOrganizerMod.WorldSlots(worldMenu), row.GetComponent<WorldSlot>());
            return System.Array.IndexOf(SlotOrganizerMod.CharacterSlots((ChooseCharacterMenu)menu), row.GetComponent<SaveSlot>());
        }

        private static void CreateMissing(RadicalMenu menu, List<Arrow> arrows)
        {
            var done = new HashSet<Transform>();
            foreach (Arrow a in arrows) done.Add(a.row);

            if (menu is SelectWorldMenu worldMenu)
            {
                foreach (WorldSlot slot in SlotOrganizerMod.WorldSlots(worldMenu))
                {
                    if (done.Contains(slot.transform)) continue;
                    WorldSlotDeleteOption src = slot.deleteOption;
                    foreach (Kind kind in new[] { Kind.Up, Kind.Down, Kind.Copy })
                    {
                        var clone = (WorldSlotDeleteOption)Clone(menu, src, src.icon, src.background, kind, RowCentre(src, slot.moreOption));
                        clone.Init(worldMenu);
                        clone.animator.enabled = false;
                        Register(arrows, clone, src, slot.transform, kind);
                    }
                }
                return;
            }

            var charMenu = (ChooseCharacterMenu)menu;
            foreach (SaveSlot slot in SlotOrganizerMod.CharacterSlots(charMenu))
            {
                if (done.Contains(slot.transform)) continue;
                SaveSlotDeleteOption src = slot.saveSlotDeleteOption;
                foreach (Kind kind in new[] { Kind.Up, Kind.Down, Kind.Copy, Kind.Edit })
                {
                    var clone = (SaveSlotDeleteOption)Clone(menu, src, src.icon, src.background, kind, 0f);
                    clone.Init(charMenu);
                    clone.animator.enabled = false;
                    Register(arrows, clone, src, slot.transform, kind);
                }
            }
        }

        private static void Register(List<Arrow> arrows, RadicalMenuOption clone, RadicalMenuOption src, Transform row, Kind kind)
        {
            var a = new Arrow { option = clone, source = src, row = row, kind = kind };
            arrows.Add(a);
            byOption[clone] = a;
        }

        private static RadicalMenuOption Clone(RadicalMenu menu, RadicalMenuOption src, SpriteRenderer srcIcon, SpriteRenderer srcBackground, Kind kind, float centreY)
        {
            GameObject go = Object.Instantiate(src.gameObject, src.transform.parent);
            go.name = NamePrefix + kind;
            var option = go.GetComponent<RadicalMenuOption>();
            option.SetParentMenu(menu);

            // A 2x2 grid right of the trash can (one row of four ran off the screen): up above down
            // in the first column, copy above edit in the second, centred on the row. Buttons keep
            // full size (scaled pixel art smudges); the visible button is about half of `step`
            // tall, so +-0.26 steps leaves a small gap. World slots have no edit button, so their
            // copy button is centred vertically.
            float step = ButtonWidth(src, srcBackground);
            float x = kind == Kind.Up || kind == Kind.Down ? step : step * 2f;
            bool alone = kind == Kind.Copy && menu is SelectWorldMenu;
            float y = centreY + (alone ? 0f : (kind == Kind.Up || kind == Kind.Copy ? 1f : -1f) * step * 0.26f);
            go.transform.localPosition = src.transform.localPosition + new Vector3(x, y, 0f);

            SpriteRenderer icon = FindIcon(option);
            if (icon != null && srcIcon.sprite != null) icon.sprite = IconSprite(srcIcon.sprite, kind);
            return option;
        }

        // World rows stack "..." above the trash can; centre our grid on the pair, not the trash can.
        private static float RowCentre(RadicalMenuOption delete, Component more)
        {
            Transform parent = delete.transform.parent;
            if (more == null || parent == null) return 0f;
            Vector3 a = parent.InverseTransformPoint(delete.transform.position);
            Vector3 b = parent.InverseTransformPoint(more.transform.position);
            return (b.y - a.y) / 2f;
        }

        /// <summary>Hover text for our buttons, null for anything else.</summary>
        public static string Tooltip(UIelement element)
        {
            if (!(element is RadicalMenuOption option) || !byOption.TryGetValue(option, out Arrow a)) return null;
            switch (a.kind)
            {
                case Kind.Up: return "Move up";
                case Kind.Down: return "Move down";
                case Kind.Copy: return "Copy to an empty slot";
                default: return "Edit name and looks";
            }
        }

        private static SpriteRenderer FindIcon(RadicalMenuOption option)
        {
            if (option is WorldSlotDeleteOption w) return w.icon;
            if (option is SaveSlotDeleteOption s) return s.icon;
            return null;
        }

        private static void SetEnabled(RadicalMenuOption option, bool on)
        {
            if (option is WorldSlotDeleteOption w) w.SetEnabled(on);
            else if (option is SaveSlotDeleteOption s) s.SetEnabled(on);
        }

        // Width of the delete button's background in the parent's local units, plus a 1-pixel gap.
        private static float ButtonWidth(RadicalMenuOption src, SpriteRenderer background)
        {
            Transform parent = src.transform.parent;
            if (background != null && background.sprite != null && parent != null && parent.lossyScale.x != 0f)
            {
                Sprite s = background.sprite;
                float width = s.bounds.size.x * background.transform.lossyScale.x / parent.lossyScale.x;
                float pixel = background.transform.lossyScale.x / parent.lossyScale.x / s.pixelsPerUnit;
                if (width > 0f) return width + pixel;
            }
            return 1.25f;
        }

        // White pixel icons the size of the trash-can icon (same pixels per unit and pivot):
        // a filled triangle for up/down, two overlapping sheets for copy, "..." for edit.
        private static Sprite IconSprite(Sprite like, Kind kind)
        {
            int key = (int)kind * 1000003 + like.GetInstanceID();
            if (sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = Mathf.Max(7, Mathf.RoundToInt(like.rect.width));
            int h = Mathf.Max(7, Mathf.RoundToInt(like.rect.height));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            var white = new Color32(255, 255, 255, 255);
            void Set(int x, int y) { if (x >= 0 && x < w && y >= 0 && y < h) pixels[y * w + x] = white; }

            int size = Mathf.Min(w, h);
            int x0 = (w - size) / 2, y0 = (h - size) / 2;
            int m = Mathf.Max(1, size / 8), n = size - 2 * m;
            if (kind == Kind.Up || kind == Kind.Down)
            {
                int height = Mathf.Max(3, (size + 1) / 2);
                float cx = (w - 1) / 2f;
                int ty = (h - height) / 2;
                for (int i = 0; i < height; i++)
                {
                    int half = kind == Kind.Up ? height - 1 - i : i;   // up: wide at the bottom
                    for (int x = 0; x < w; x++)
                        if (Mathf.Abs(x - cx) <= half + 0.01f) Set(x, ty + i);
                }
            }
            else if (kind == Kind.Copy)
            {
                // Two outlined sheets: the back one top left, the front one bottom right drawn over it.
                int b = Mathf.Max(4, (n * 3 + 3) / 4);
                int bx = x0 + m, by = y0 + m + n - b;
                int fx = x0 + m + n - b, fy = y0 + m;
                for (int i = 0; i < b; i++) { Set(bx + i, by + b - 1); Set(bx, by + i); }
                for (int i = 0; i < b; i++) { if (bx + i < fx) Set(bx + i, by); if (by + i > fy + b - 1) Set(bx + b - 1, by + i); }
                for (int i = 0; i < b; i++) { Set(fx + i, fy); Set(fx + i, fy + b - 1); Set(fx, fy + i); Set(fx + b - 1, fy + i); }
            }
            else
            {
                // "..." like the game's own world settings button: three square dots.
                int d = Mathf.Max(1, size / 6);
                int gap = Mathf.Max(1, (size - 3 * d) / 4);
                int start = (w - (3 * d + 2 * gap)) / 2, dy = (h - d) / 2;
                for (int k = 0; k < 3; k++)
                    for (int x = 0; x < d; x++)
                        for (int y = 0; y < d; y++) Set(start + k * (d + gap) + x, dy + y);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            Vector2 pivot = new Vector2(like.pivot.x / like.rect.width, like.pivot.y / like.rect.height);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, like.pixelsPerUnit);
            sprites[key] = sprite;
            return sprite;
        }
    }

    [HarmonyPatch(typeof(WorldSlotDeleteOption), nameof(WorldSlotDeleteOption.OnActivated))]
    internal static class WorldArrowClickPatch
    {
        private static bool Prefix(WorldSlotDeleteOption __instance) => !SlotArrows.TryActivate(__instance);
    }

    [HarmonyPatch(typeof(SaveSlotDeleteOption), nameof(SaveSlotDeleteOption.OnActivated))]
    internal static class CharacterArrowClickPatch
    {
        private static bool Prefix(SaveSlotDeleteOption __instance) => !SlotArrows.TryActivate(__instance);
    }

    [HarmonyPatch(typeof(ChooseCharacterMenu), nameof(ChooseCharacterMenu.SetupSaveSlots))]
    internal static class CharacterSetupPatch
    {
        private static void Prefix(ChooseCharacterMenu __instance) => SlotArrows.RemoveFromList(__instance);
    }

    [HarmonyPatch(typeof(ChooseCharacterMenu), nameof(ChooseCharacterMenu.UpdatePosition))]
    internal static class CharacterLayoutPatch
    {
        private static void Prefix(ChooseCharacterMenu __instance) => SlotArrows.RemoveFromList(__instance);
    }
}
