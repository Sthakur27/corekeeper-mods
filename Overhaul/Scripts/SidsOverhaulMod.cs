using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using ModOptions;
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
        public const string Version = "1.3.1";

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
            Page(null, Features.PageTitle, Features.KeyPrefix, Features.Hint, Features.RegisterSettings);
            // One page per feature in the Mod Options menu (none for switched-off features). Key prefixes keep the values saved by
            // earlier versions (one long section with prefixed labels) in SidsOverhaul/config.cfg.
            Page("AutoReplant", "Auto Replant", "Auto Replant: ", AutoReplant.AutoReplantMod.SettingsHint, AutoReplant.AutoReplantMod.RegisterSettings);
            Page("QuickBuff", "Quick Buff", "Quick Buff: ", QuickBuff.QuickBuffMod.SettingsHint, QuickBuff.QuickBuffMod.RegisterSettings);
            Page("BuffDurationFloor", "Buff Duration Floor", "Buff Floor: ", BuffDurationFloor.BuffDurationFloorMod.SettingsHint, BuffDurationFloor.BuffDurationFloorMod.RegisterSettings);
            Page("PotionSeller", "Potion Seller", "Potion Seller: ", PotionSeller.PotionSellerMod.SettingsHint, PotionSeller.PotionSellerMod.RegisterSettings);
            Page("FishSeller", "Fish Seller", "Fish Seller: ", FishSeller.FishSellerMod.SettingsHint, FishSeller.FishSellerMod.RegisterSettings);
            Page("MerchantRestock", "Merchant Restock", "Merchant Restock: ", MerchantRestock.MerchantRestockMod.SettingsHint, MerchantRestock.MerchantRestockMod.RegisterSettings);
            Page("FasterMushrooms", "Faster Mushrooms", "Mushrooms: ", FasterMushrooms.FasterMushroomsMod.SettingsHint, FasterMushrooms.FasterMushroomsMod.RegisterSettings);
            Page("BetterFishingLoot", "Better Fishing Loot", "Fishing Loot: ", BetterFishingLoot.BetterFishingLootMod.SettingsHint, BetterFishingLoot.BetterFishingLootMod.RegisterSettings);
            Page("DurabilityMultiplier", "Durability", "Durability: ", DurabilityMultiplier.DurabilityMultiplierMod.SettingsHint, DurabilityMultiplier.DurabilityMultiplierMod.RegisterSettings);
            Page("VehicleSpeed", "Vehicle Speed", "Vehicles: ", VehicleSpeed.VehicleSpeedMod.SettingsHint, VehicleSpeed.VehicleSpeedMod.RegisterSettings);
            Page("GoldenChance", "Golden Chance", "Golden: ", GoldenChance.GoldenChanceMod.SettingsHint, GoldenChance.GoldenChanceMod.RegisterSettings);
            Page("DifficultyTuning", "Difficulty Tuning (Hard)", "Hard Mode: ", DifficultyTuning.DifficultyTuningMod.SettingsHint, DifficultyTuning.DifficultyTuningMod.RegisterSettings);
            Page("DifficultyTuning", "Difficulty Tuning (Normal)", "Normal Mode: ", DifficultyTuning.DifficultyTuningMod.NormalSettingsHint, DifficultyTuning.DifficultyTuningMod.RegisterNormalSettings);
            Page("SkillXPMultiplier", "Skill XP Multiplier", "Skill XP: ", SkillXPMultiplier.SkillXPMultiplierMod.SettingsHint, SkillXPMultiplier.SkillXPMultiplierMod.RegisterSettings);
            Page("StimHits", "Stim Hits", "Stim Hits: ", StimHits.StimHitsMod.SettingsHint, StimHits.StimHitsMod.RegisterSettings);
            Page("ArmorDye", "Armor Dye", "Armor Dye: ", ArmorDye.DyeSettings.SettingsHint, ArmorDye.DyeSettings.RegisterSettings);
            Debug.Log($"[{Name}] Loaded all features.");
        }

        private void Page(string feature, string title, string keyPrefix, string hint, System.Action<SettingsPage> register)
        {
            if (feature != null && !Features.On(feature)) return;
            var page = SettingsPages.Create(this, title, keyPrefix).Hint(hint);
            register(page);
            page.Build();
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
