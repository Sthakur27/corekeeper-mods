using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Localization;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Enchanting
{
    /// <summary>
    /// Enchanting: per-item random enchants (stat buffs and on-hit effects) stored in the item's vanilla
    /// PetTalentBuffer aux data and applied as permanent player conditions (EnchantEffectSystem). See SPEC.md.
    /// </summary>
    public sealed class EnchantingMod : IMod
    {
        public const string Name = "Enchanting";
        public const string Version = "0.1.0";
        public const string TableObjectName = "Enchanting_Table"; // objectName in the bundle (EnchantingBuilder)
        public const string ButtonTerm = "Enchanting/EnchantButton";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            CoreLibMod.LoadSubmodule(typeof(CommandModule), typeof(LocalizationModule));
            LocalizationModule.AddTerm(ButtonTerm, "Enchant (random)");
            if (!InOverhaul(this)) // the overhaul registers every command in its assembly once
            {
                var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
                if (info != null) CommandModule.AddCommands(info.ModId, Name);
                else Debug.LogError($"[{Name}] Mod metadata not found; /enchant command unavailable.");
            }
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }
    }
}
