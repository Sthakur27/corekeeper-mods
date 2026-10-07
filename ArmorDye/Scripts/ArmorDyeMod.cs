using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArmorDye
{
    /// <summary>
    /// Armor Dye (prototype): dye helm, chest and pants pieces with "/dye". The dye is stored on the item
    /// (vanilla MealsEatenCD inventory aux data, see SPEC.md), so it is saved, replicated and follows the piece;
    /// every client recolors the armor layers of every player from it (ArmorRecolor).
    /// </summary>
    public sealed class ArmorDyeMod : IMod
    {
        public const string Name = "ArmorDye";
        public const string Version = "0.4.3";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            CoreLibMod.LoadSubmodule(typeof(CommandModule));
            if (InOverhaul(this)) return; // the overhaul registers every command in its assembly once
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (info == null)
            {
                Debug.LogError($"[{Name}] Mod metadata not found; /dye command unavailable.");
                return;
            }
            CommandModule.AddCommands(info.ModId, Name);
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update()
        {
            ArmorRecolor.TickPreviews();
            DyeUI.Tick();
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }
    }
}
