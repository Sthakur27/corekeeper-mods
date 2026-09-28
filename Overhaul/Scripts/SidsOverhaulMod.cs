using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SidsOverhaul
{
    /// <summary>
    /// Central entry point of Sid's Overhaul, the single mod that bundles all of Sid's Core Keeper mods.
    /// Every feature keeps its own IMod (the loader creates all of them from this one assembly); this
    /// class only does what must happen once per mod:
    ///  - one Mod Settings Menu section (the menu keys sections by manifest name, so features skip
    ///    their own section inside the overhaul and register their options here, label-prefixed);
    ///  - one CoreLib chat command registration for the whole assembly (/xp, /quickbuff).
    /// Built by release/build_overhaul.py from the individual mod folders.
    /// </summary>
    public sealed class SidsOverhaulMod : IMod
    {
        public const string Name = "SidsOverhaul";
        public const string Version = "1.0.1";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (info == null)
            {
                Debug.LogError($"[{Name}] Mod metadata not found; chat commands unavailable.");
                return;
            }
            CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(CommandModule));
            CommandModule.AddCommands(info.ModId, Name);
        }

        public void Init()
        {
            var section = ModSettings.Section(this)
                .Hint("All of Sid's mods in one. Each option is prefixed with its feature. 1x / Off = vanilla. In multiplayer the host's values count.");

            AutoReplant.AutoReplantMod.RegisterSettings(section, "Auto Replant: ");
            QuickBuff.QuickBuffMod.RegisterSettings(section, "Quick Buff: ");
            BuffDurationFloor.BuffDurationFloorMod.RegisterSettings(section, "Buff Floor: ");
            PotionSeller.PotionSellerMod.RegisterSettings(section, "Potion Seller: ");
            FasterMushrooms.FasterMushroomsMod.RegisterSettings(section, "Mushrooms: ");
            BetterFishingLoot.BetterFishingLootMod.RegisterSettings(section, "Fishing Loot: ");
            DurabilityMultiplier.DurabilityMultiplierMod.RegisterSettings(section, "Durability: ");
            HardModeTuning.HardModeTuningMod.RegisterSettings(section, "Hard Mode: ");
            SkillXPMultiplier.SkillXPMultiplierMod.RegisterSettings(section, "Skill XP: ");

            section.Build();
            Debug.Log($"[{Name}] Loaded all features.");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
