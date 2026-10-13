// Test host only. The configuration implementation is compiled from the installed CoreLib source.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine { public static class Debug { public static void Log(object o) { } } }
namespace CoreLib
{
    public static class CoreLibMod { public static Logger log = new Logger(); }
    public class Logger { public void LogWarning(object o) { } public void LogError(object o) { throw new Exception(o.ToString()); } }
}
public static class Manager { public static Main main = new Main(); }
public class Main { public Player player = new Player(); }
public class Player { public bool guestMode; public int adminPrivileges; }
namespace PugMod
{
    public interface IMod { }
    public class LoadedMod { public Metadata Metadata = new Metadata(); public List<IMod> Handlers = new List<IMod>(); }
    public class Metadata { public string name; public string displayName; }
    public static class API
    {
        public static MemoryFilesystem ConfigFilesystem = new MemoryFilesystem();
        public static Loader ModLoader = new Loader();
    }
    public class Loader { public List<LoadedMod> LoadedMods = new List<LoadedMod>(); }
    public class MemoryFilesystem
    {
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public int Writes;
        public bool FileExists(string p) => Files.ContainsKey(p);
        public byte[] Read(string p) => Files[p];
        public void Write(string p, byte[] bytes) { Files[p] = bytes; Writes++; }
        public void CreateDirectory(string p) { }
    }
    public static class CheckedExtensions
    {
        public static string GetNameChecked(this Type t) => t.Name;
        public static IEnumerable<Attribute> GetCustomAttributesChecked(this Type t) => t.GetCustomAttributes(false).Cast<Attribute>();
    }
}
