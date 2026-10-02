using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PetEditor
{
    /// <summary>
    /// Pet Editor: edit the equipped pet from the vanilla pet talent window.
    ///  - "Level" row with - / + buttons (sets the pet's XP to the start of that level).
    ///  - Right-click a talent slot to pick any talent in the game for it.
    ///  - Shift + left-click a talent with a point to remove that point for free.
    /// The client only sends "/pet ..." commands (CoreLib command channel, see PetCommand); the server
    /// edits the pet item (XP = ContainedObjectsBuffer amount, talents = the item's PetTalentBuffer aux
    /// data), which then replicates like any vanilla change, so it works for every player in multiplayer.
    /// Written from the game's own code (PetTalentsWindow, PetExtensions, InventoryAuxData).
    /// </summary>
    public sealed class PetEditorMod : IMod
    {
        public const string Name = "PetEditor";
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            CoreLibMod.LoadSubmodule(typeof(CommandModule));
            if (InOverhaul(this)) return; // the overhaul registers every command in its assembly once
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (info == null)
            {
                Debug.LogError($"[{Name}] Mod metadata not found; /pet command unavailable.");
                return;
            }
            CommandModule.AddCommands(info.ModId, Name);
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { PetEditorUI.Tick(); }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }
    }
}
