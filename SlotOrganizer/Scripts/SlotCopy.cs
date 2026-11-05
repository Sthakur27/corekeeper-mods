using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using HarmonyLib;
using Pug.Platform;
using UnityEngine;
using FileID = Pug.Platform.FilesystemManager.FileID;

namespace SlotOrganizer
{
    /// <summary>
    /// Copies a world or character into the first empty slot of the same list. The copy gets a new
    /// GUID (worlds: the ServerGuidCD inside the world save, characters: characterGuid) so the game
    /// treats it as a separate world/character instead of sharing map data with the original.
    /// </summary>
    internal static class SlotCopy
    {
        // Byte limit of the game's FixedString32Bytes name fields.
        internal const int MaxNameBytes = 29;

        /// <summary>Copies <paramref name="slot"/>; returns the new slot index, or -1.</summary>
        public static int Copy(RadicalMenu menu, int slot)
        {
            FilesystemManager fs = Manager.filesystemManager;
            fs.Flush();
            if (menu is SelectWorldMenu worldMenu)
            {
                if (!Manager.saves.WorldExists(slot)) return -1;
                int target = FirstEmpty(SaveManager.numberWorlds, i => Manager.saves.WorldExists(i));
                if (target < 0) return NoRoom();
                return CopyWorld(fs, worldMenu, slot, target);
            }

            var charMenu = (ChooseCharacterMenu)menu;
            int offset = charMenu.isCreativeMenu ? SaveManager.creativeCharacterStartIndex : 0;
            if (!Manager.saves.CharacterExists(slot + offset)) return -1;
            int t = FirstEmpty(SaveManager.numberCharacters, i => Manager.saves.CharacterExists(i + offset));
            if (t < 0) return NoRoom();
            return CopyCharacter(fs, charMenu, slot + offset, t + offset) ? t : -1;
        }

        private static int CopyWorld(FilesystemManager fs, SelectWorldMenu menu, int from, int to)
        {
            WorldInfo info = Manager.saves.GetWorldInfo(from);
            string oldGuid = info?.guid ?? "";
            string name = info?.name ?? "";

            foreach (FileID type in new[] { FileID.WorldSave, FileID.WorldInfo, FileID.WorldGenerationParameters, FileID.ServerMapParts })
                CopyFile(fs, new FilesystemManager.File(type, from), new FilesystemManager.File(type, to));

            string newGuid = oldGuid;
            if (oldGuid.Length == 32)
            {
                string candidate = System.Guid.NewGuid().ToString("N");
                var file = new FilesystemManager.File(FileID.WorldSave, to);
                byte[] data = ReplaceGuidInWorld(fs.Read(file, raw: true), oldGuid, candidate);
                if (data != null)
                {
                    fs.Write(file, data, raw: true);
                    newGuid = candidate;
                }
                else Debug.LogWarning($"[{SlotOrganizerMod.Name}] World {from + 1}: GUID not found in the world save; the copy keeps the same GUID.");
            }
            fs.Flush();
            Manager.saves.Init();

            WorldInfo copy = Manager.saves.GetWorldInfo(to);
            if (copy != null)
            {
                copy.guid = newGuid;
                copy.name = CopyName(name);
                Manager.saves.WriteWorldInfo(to);
                fs.Flush();
            }
            menu.UpdateSlots();
            Debug.Log($"[{SlotOrganizerMod.Name}] Copied world {from + 1} to slot {to + 1}");
            return to;
        }

        private static bool CopyCharacter(FilesystemManager fs, ChooseCharacterMenu menu, int from, int to)
        {
            CopyFile(fs, new FilesystemManager.File(FileID.Save, from), new FilesystemManager.File(FileID.Save, to));
            fs.DeleteAll(FileID.MapParts, to);
            foreach (FilesystemManager.File f in fs.GetAllFiles(FileID.MapParts, from))
            {
                byte[] data = fs.Read(f, raw: true);
                if (data != null) fs.Write(new FilesystemManager.File(FileID.MapParts, to, f.instance1), data, raw: true);
            }
            fs.Flush();
            Manager.saves.Init();

            var all = (CharacterData[])AccessTools.Field(typeof(SaveManager), "characterData").GetValue(Manager.saves);
            all[to].characterGuid = System.Guid.NewGuid().ToString("N");
            PlayerCustomization c = Manager.saves.GetCharacterCustomization(to);
            c.name = CopyName(c.name.Value);
            Manager.saves.SetCharacterCustomization(to, c);
            Manager.saves.WriteCharacter(to);
            fs.Flush();
            menu.SetupSaveSlots();
            Debug.Log($"[{SlotOrganizerMod.Name}] Copied character {from + 1} to slot {to + 1}");
            return true;
        }

        private static void CopyFile(FilesystemManager fs, FilesystemManager.File from, FilesystemManager.File to)
        {
            byte[] data = fs.FileExists(from) ? fs.Read(from, raw: true) : null;
            if (data != null) fs.Write(to, data, raw: true);
            else if (fs.FileExists(to)) fs.Delete(to);
        }

        private static int FirstEmpty(int count, System.Func<int, bool> exists)
        {
            for (int i = 0; i < count; i++)
                if (!exists(i)) return i;
            return -1;
        }

        private static int NoRoom()
        {
            Manager.menu.centerPopUpText.StartNewDisplaySequence("No empty slot to copy into.", null, menuInputCooldown: true, 0f, 1.5f,
                useUnscaledTime: true, 0f, 1f, localize: false, TextManager.FontFace.boldMedium, _ => { }, new List<string> { "ok" }, 10f);
            return -1;
        }

        /// <summary>"Name copy", shortened so it still fits the game's name limit.</summary>
        internal static string CopyName(string name)
        {
            const string suffix = " copy";
            name = (name ?? "").Trim();
            while (name.Length > 0 && Encoding.UTF8.GetByteCount(name + suffix) > MaxNameBytes)
                name = name.Substring(0, name.Length - 1).TrimEnd();
            return name + suffix;
        }

        /// <summary>
        /// The world save is Brotli (old saves: GZip) around the DOTS world; the world GUID is a
        /// Hash128 in it, stored as the GUID string's hex with each byte's two digits swapped.
        /// Returns the recompressed save with every occurrence replaced, or null if none was found.
        /// </summary>
        private static byte[] ReplaceGuidInWorld(byte[] raw, string oldGuid, string newGuid)
        {
            if (raw == null || raw.Length < 2) return null;
            bool gzip = raw[0] == 0x1f && raw[1] == 0x8b;
            byte[] data;
            try
            {
                using (var input = new MemoryStream(raw))
                using (Stream unzip = gzip ? (Stream)new GZipStream(input, CompressionMode.Decompress) : Brotli(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    unzip.CopyTo(output);
                    data = output.ToArray();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{SlotOrganizerMod.Name}] Could not read the world save: {e.Message}");
                return null;
            }

            byte[] from = GuidBytes(oldGuid), to = GuidBytes(newGuid);
            if (from == null || to == null) return null;
            int found = 0;
            for (int i = 0; i + 16 <= data.Length; i++)
            {
                int k = 0;
                while (k < 16 && data[i + k] == from[k]) k++;
                if (k < 16) continue;
                System.Array.Copy(to, 0, data, i, 16);
                found++;
                i += 15;
            }
            if (found == 0) return null;

            using (var output = new MemoryStream())
            {
                using (Stream zip = gzip ? (Stream)new GZipStream(output, System.IO.Compression.CompressionLevel.Fastest)
                                         : Brotli(output, System.IO.Compression.CompressionLevel.Fastest))
                    zip.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        // The mod compiler doesn't reference System.IO.Compression.dll, so BrotliStream (which the
        // game itself uses for world saves) is created by reflection from the loaded assembly.
        private static System.Type brotliType;

        private static Stream Brotli(Stream inner, object modeOrLevel)
        {
            if (brotliType == null)
            {
                foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    brotliType = asm.GetType("System.IO.Compression.BrotliStream", false);
                    if (brotliType != null) break;
                }
                if (brotliType == null) throw new System.InvalidOperationException("BrotliStream not available");
            }
            return (Stream)System.Activator.CreateInstance(brotliType, inner, modeOrLevel);
        }

        private static byte[] GuidBytes(string guid)
        {
            if (guid == null || guid.Length != 32) return null;
            var bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                string pair = new string(new[] { guid[i * 2 + 1], guid[i * 2] });
                if (!byte.TryParse(pair, System.Globalization.NumberStyles.HexNumber, null, out bytes[i])) return null;
            }
            return bytes;
        }
    }
}
