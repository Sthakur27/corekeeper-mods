using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using ModOptions;
using PugMod;
using Rewired;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArmorDye
{
    /// <summary>
    /// Armor Dye: dye armor, weapons and tools. The dye is stored on the item (vanilla MealsEatenCD inventory
    /// aux data, see SPEC.md), so it is saved, replicated and follows the piece; every client recolors the
    /// armor layers, held items, projectiles and icons of every player from it (ArmorRecolor,
    /// ProjectileRecolor). Pick dyes with the palette (hover an item + P, see DyeUI) or "/dye".
    /// </summary>
    public sealed class ArmorDyeMod : IMod
    {
        public const string Name = "ArmorDye";
        public const string Version = "0.5.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(CommandModule));
            if (!InOverhaul(this)) // the overhaul registers every command in its assembly once
            {
                var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
                if (info != null) CommandModule.AddCommands(info.ModId, Name);
                else Debug.LogError($"[{Name}] Mod metadata not found; /dye command unavailable.");
            }

            if (Application.isBatchMode) return; // dedicated server: no keyboard
            try
            {
                int category = ControlMappingModule.AddNewCategory("Armor Dye");
                ControlMappingModule.AddKeyboardBind(DyeUI.KeyBind, KeyboardKeyCode.P, categoryId: category);
                ControlMappingModule.rewiredStart += DyeUI.OnRewiredStart;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the palette key: {e}");
            }
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Armor Dye").Hint(DyeSettings.SettingsHint);
            DyeSettings.RegisterSettings(section);
            section.Build();
        }

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
