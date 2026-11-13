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
        public const string Version = "0.5.0";
        public const string TableObjectName = "Enchanting_Table"; // objectName in the bundle (EnchantingBuilder)
        public const string ButtonTerm = "Enchanting/EnchantButton";
        public const string PlainTerm = "Enchanting/Plain"; // "{0}": shows its format field as-is

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            CoreLibMod.LoadSubmodule(typeof(CommandModule), typeof(LocalizationModule));
            LocalizationModule.AddTerm(ButtonTerm, "Enchant (random)");
            LocalizationModule.AddTerm(PlainTerm, "{0}");
            LocalizationModule.AddTerm(EnchantRemoveButton.TitleTerm, "Remove top enchantment");
            LocalizationModule.AddTerm(EnchantRemoveButton.RerollTerm, "Reroll top enchantment");
            if (!InOverhaul(this)) // the overhaul registers every command in its assembly once
            {
                var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
                if (info != null) CommandModule.AddCommands(info.ModId, Name);
                else Debug.LogError($"[{Name}] Mod metadata not found; /enchant command unavailable.");
            }
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj)
        {
            if (obj is Pug.Sprite.SpriteAssetSkin skin && skin.name == EnchantTableSkinPatch.SkinName)
                EnchantTableSkinPatch.Skin = skin;
        }
        public void Update() { }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }
    }
}
