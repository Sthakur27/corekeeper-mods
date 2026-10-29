using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlotOrganizer
{
    /// <summary>
    /// Up/down arrow buttons on each world and character slot, right of the trash can. They are
    /// clones of the slot's delete button (so hover, highlight and click behave like vanilla) with
    /// an arrow icon; their activation is intercepted below and moves the slot instead.
    /// </summary>
    internal static class SlotArrows
    {
        private const string NamePrefix = "SlotOrganizer_";

        private sealed class Arrow
        {
            public RadicalMenuOption option;
            public RadicalMenuOption source;   // the row's vanilla delete button
            public Transform row;              // WorldSlot / SaveSlot
            public int dir;
        }

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
            if (SlotOrganizerMod.Move(menu, slot, a.dir))
            {
                Sync(menu);
                // Keep the highlight on the same arrow of the moved slot, so Enter (or a click
                // after following it with the mouse) keeps moving the same world/character.
                Arrow next = Find(menu, slot + a.dir, a.dir);
                if (next != null && next.option.enabled)
                {
                    int index = menu.menuOptions.IndexOf(next.option);
                    if (index >= 0) menu.SelectOptionIndex(index);
                }
            }
            return true;
        }

        private static Arrow Find(RadicalMenu menu, int slot, int dir)
        {
            foreach (Arrow a in byMenu[menu])
                if (a.dir == dir && RowIndex(menu, a.row) == slot) return a;
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
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        var clone = (WorldSlotDeleteOption)Clone(menu, src, src.icon, src.background, dir);
                        clone.Init(worldMenu);
                        clone.animator.enabled = false;
                        Register(arrows, clone, src, slot.transform, dir);
                    }
                }
                return;
            }

            var charMenu = (ChooseCharacterMenu)menu;
            foreach (SaveSlot slot in SlotOrganizerMod.CharacterSlots(charMenu))
            {
                if (done.Contains(slot.transform)) continue;
                SaveSlotDeleteOption src = slot.saveSlotDeleteOption;
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    var clone = (SaveSlotDeleteOption)Clone(menu, src, src.icon, src.background, dir);
                    clone.Init(charMenu);
                    clone.animator.enabled = false;
                    Register(arrows, clone, src, slot.transform, dir);
                }
            }
        }

        private static void Register(List<Arrow> arrows, RadicalMenuOption clone, RadicalMenuOption src, Transform row, int dir)
        {
            var a = new Arrow { option = clone, source = src, row = row, dir = dir };
            arrows.Add(a);
            byOption[clone] = a;
        }

        private static RadicalMenuOption Clone(RadicalMenu menu, RadicalMenuOption src, SpriteRenderer srcIcon, SpriteRenderer srcBackground, int dir)
        {
            GameObject go = Object.Instantiate(src.gameObject, src.transform.parent);
            go.name = NamePrefix + (dir < 0 ? "Up" : "Down");
            var option = go.GetComponent<RadicalMenuOption>();
            option.SetParentMenu(menu);

            // Up arrow right of the trash can, down arrow right of that.
            float step = ButtonWidth(src, srcBackground);
            go.transform.localPosition = src.transform.localPosition + new Vector3(step * (dir < 0 ? 1 : 2), 0f, 0f);

            SpriteRenderer icon = FindIcon(option);
            if (icon != null && srcIcon.sprite != null) icon.sprite = ArrowSprite(srcIcon.sprite, dir);
            return option;
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

        // A white filled triangle the size of the trash-can icon (same pixels per unit and pivot).
        private static Sprite ArrowSprite(Sprite like, int dir)
        {
            int key = dir * 1000003 + like.GetInstanceID();
            if (sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = Mathf.Max(5, Mathf.RoundToInt(like.rect.width));
            int h = Mathf.Max(5, Mathf.RoundToInt(like.rect.height));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            int height = Mathf.Max(3, (Mathf.Min(w, h) + 1) / 2);
            float cx = (w - 1) / 2f;
            int y0 = (h - height) / 2;
            for (int i = 0; i < height; i++)
            {
                int half = dir < 0 ? height - 1 - i : i;   // up: wide at the bottom
                int y = y0 + i;
                for (int x = 0; x < w; x++)
                    if (Mathf.Abs(x - cx) <= half + 0.01f) pixels[y * w + x] = new Color32(255, 255, 255, 255);
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
