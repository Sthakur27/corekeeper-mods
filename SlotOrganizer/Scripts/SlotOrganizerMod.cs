using System.Collections.Generic;
using Pug.Platform;
using PugMod;
using UnityEngine;
using FileID = Pug.Platform.FilesystemManager.FileID;
using Object = UnityEngine.Object;

namespace SlotOrganizer
{
    /// <summary>
    /// Reorder worlds and characters in the main menu. Hover/select a slot and press
    /// Ctrl+Up / Ctrl+Down (or Page Up / Page Down) to swap it with the slot above/below;
    /// moving into an empty slot just moves it. A slot number is only the file name
    /// (characters link to worlds by GUID), so swapping the files is all it takes.
    /// </summary>
    public sealed class SlotOrganizerMod : IMod
    {
        public const string Name = "SlotOrganizer";
        public const string Version = "1.0.1";

        // Selection as of the previous frame. The menu also reads the arrow keys, so by the time
        // Update runs it may already have moved the selection; the last frame's slot is the one meant.
        private RadicalMenu lastMenu;
        private int lastSlot = -1;

        // Re-select the moved slot for a few frames in case the menu handles the arrow after us.
        private RadicalMenu pendingMenu;
        private int pendingSlot = -1;
        private int pendingFrames;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: Ctrl+Up/Down or PgUp/PgDn moves world and character slots.");
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }

        public void Update()
        {
            if (Manager.main == null || Manager.menu == null || Manager.sceneHandler == null || Manager.sceneHandler.isInGame)
            {
                lastMenu = null;
                return;
            }
            RadicalMenu menu = Manager.menu.GetTopMenu();
            if (!IsSlotMenu(menu))
            {
                lastMenu = null;
                pendingFrames = 0;
                return;
            }

            if (pendingFrames > 0)
            {
                pendingFrames--;
                if (pendingMenu == menu) Select(menu, pendingSlot);
            }

            int dir = 0;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (Input.GetKeyDown(KeyCode.PageUp) || (ctrl && Input.GetKeyDown(KeyCode.UpArrow))) dir = -1;
            else if (Input.GetKeyDown(KeyCode.PageDown) || (ctrl && Input.GetKeyDown(KeyCode.DownArrow))) dir = 1;

            if (dir != 0 && lastMenu == menu && lastSlot >= 0)
            {
                int target = lastSlot + dir;
                if (Move(menu, lastSlot, dir))
                {
                    Select(menu, target);
                    pendingMenu = menu;
                    pendingSlot = target;
                    pendingFrames = 3;
                    lastSlot = target;
                    SlotArrows.Sync(menu);
                    return;
                }
            }

            SlotArrows.Sync(menu);
            lastMenu = menu;
            lastSlot = SelectedSlot(menu);
        }

        /// <summary>Swaps slot <paramref name="slot"/> with its neighbour in direction <paramref name="dir"/> (-1 up, +1 down).</summary>
        internal static bool Move(RadicalMenu menu, int slot, int dir)
        {
            int target = slot + dir;
            return slot >= 0 && target >= 0 && target < SlotCount(menu) && Swap(menu, slot, target);
        }

        internal static bool IsSlotMenu(RadicalMenu menu)
        {
            if (menu is SelectWorldFromModMenu) return false;
            return menu is SelectWorldMenu || menu is ChooseCharacterMenu;
        }

        internal static int SlotCount(RadicalMenu menu)
        {
            return menu is SelectWorldMenu ? SaveManager.numberWorlds : SaveManager.numberCharacters;
        }

        // Slot index within the menu (0-based, as displayed minus one).
        private static int SelectedSlot(RadicalMenu menu)
        {
            RadicalMenuOption option = menu.GetSelectedMenuOption();
            if (option == null) return -1;
            if (menu is SelectWorldMenu worldMenu)
            {
                WorldSlot slot = option.GetComponentInParent<WorldSlot>();
                return slot == null ? -1 : System.Array.IndexOf(WorldSlots(worldMenu), slot);
            }
            SaveSlot saveSlot = option.GetComponentInParent<SaveSlot>();
            if (saveSlot == null) return -1;
            return System.Array.IndexOf(CharacterSlots((ChooseCharacterMenu)menu), saveSlot);
        }

        internal static WorldSlot[] WorldSlots(SelectWorldMenu menu) =>
            menu.saveSlotsContainer.GetComponentsInChildren<WorldSlot>(true);

        internal static SaveSlot[] CharacterSlots(ChooseCharacterMenu menu) =>
            menu.saveSlotsContainer.GetComponentsInChildren<SaveSlot>(true);

        internal static void Select(RadicalMenu menu, int slot)
        {
            RadicalMenuOption option = null;
            if (menu is SelectWorldMenu worldMenu)
            {
                WorldSlot[] slots = WorldSlots(worldMenu);
                if (slot >= slots.Length) return;
                WorldSlot s = slots[slot];
                option = s.playOption.gameObject.activeSelf ? (RadicalMenuOption)s.playOption : s.newWorldOption;
            }
            else
            {
                SaveSlot[] slots = CharacterSlots((ChooseCharacterMenu)menu);
                if (slot >= slots.Length) return;
                option = slots[slot].saveSlotPlayOption;
            }
            int index = menu.menuOptions.IndexOf(option);
            if (index >= 0 && menu.selectedIndex != index) menu.SelectOptionIndex(index);
        }

        private static bool Swap(RadicalMenu menu, int a, int b)
        {
            FilesystemManager fs = Manager.filesystemManager;
            fs.Flush();
            if (menu is SelectWorldMenu worldMenu)
            {
                if (!Manager.saves.WorldExists(a) && !Manager.saves.WorldExists(b)) return false;
                foreach (FileID type in new[] { FileID.WorldSave, FileID.WorldInfo, FileID.WorldGenerationParameters, FileID.ServerMapParts })
                    SwapFiles(fs, new FilesystemManager.File(type, a), new FilesystemManager.File(type, b));
                fs.Flush();
                Manager.saves.Init();
                worldMenu.UpdateSlots();
                Debug.Log($"[{Name}] Swapped world slots {a + 1} and {b + 1}");
                return true;
            }

            var charMenu = (ChooseCharacterMenu)menu;
            int offset = charMenu.isCreativeMenu ? SaveManager.creativeCharacterStartIndex : 0;
            int ca = a + offset, cb = b + offset;
            if (!Manager.saves.CharacterExists(ca) && !Manager.saves.CharacterExists(cb)) return false;
            SwapFiles(fs, new FilesystemManager.File(FileID.Save, ca), new FilesystemManager.File(FileID.Save, cb));
            SwapMaps(fs, ca, cb);
            fs.Flush();
            Manager.saves.Init();
            charMenu.SetupSaveSlots();
            Debug.Log($"[{Name}] Swapped character slots {a + 1} and {b + 1}");
            return true;
        }

        // Raw bytes so compressed files are copied as they are on disk.
        private static void SwapFiles(FilesystemManager fs, FilesystemManager.File fa, FilesystemManager.File fb)
        {
            byte[] da = fs.FileExists(fa) ? fs.Read(fa, raw: true) : null;
            byte[] db = fs.FileExists(fb) ? fs.Read(fb, raw: true) : null;
            Put(fs, fa, db);
            Put(fs, fb, da);
        }

        private static void Put(FilesystemManager fs, FilesystemManager.File file, byte[] data)
        {
            if (data != null) fs.Write(file, data, raw: true);
            else if (fs.FileExists(file)) fs.Delete(file);
        }

        // Character map files are maps/<character>/<server>.mapparts; move the whole set.
        private static void SwapMaps(FilesystemManager fs, int a, int b)
        {
            var mapsA = ReadMaps(fs, a);
            var mapsB = ReadMaps(fs, b);
            foreach (var m in mapsA) fs.Delete(new FilesystemManager.File(FileID.MapParts, a, m.Key));
            foreach (var m in mapsB) fs.Delete(new FilesystemManager.File(FileID.MapParts, b, m.Key));
            foreach (var m in mapsB) fs.Write(new FilesystemManager.File(FileID.MapParts, a, m.Key), m.Value, raw: true);
            foreach (var m in mapsA) fs.Write(new FilesystemManager.File(FileID.MapParts, b, m.Key), m.Value, raw: true);
        }

        private static List<KeyValuePair<int, byte[]>> ReadMaps(FilesystemManager fs, int character)
        {
            var list = new List<KeyValuePair<int, byte[]>>();
            foreach (FilesystemManager.File f in fs.GetAllFiles(FileID.MapParts, character))
            {
                byte[] data = fs.Read(f, raw: true);
                if (data != null) list.Add(new KeyValuePair<int, byte[]>(f.instance1, data));
            }
            return list;
        }
    }
}
