using System.Linq;
using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterFishingLoot
{
    /// <summary>
    /// Makes rare fishing loot (Miner / Ninja / Diver / Rambo / Swamp Mage / Desert Guardian armor,
    /// rings, necklaces, pouches, lanterns, tools and every Rare-or-better item) bite more often.
    ///
    /// The fishing roll itself (PlayerState.Fishing → PugDatabase.GetRandomLoot) is Burst code and
    /// cannot be patched, but it is a plain weighted pick over the LootTableBank blob that every world
    /// holds as a singleton. <see cref="Systems.FishingLootWeightSystem"/> therefore rewrites the
    /// weights inside that blob: every "rare" entry of the ten *FishingLoot tables gets
    /// weight × multiplier, common junk keeps its vanilla weight. The separate *Fishes tables (what
    /// you get when a fish bites) are never touched, nor is the fish-vs-loot split.
    /// </summary>
    public sealed class BetterFishingLootMod : IMod
    {
        public const string Name = "BetterFishingLoot";
        public const string Version = "1.0.0";

        private static SettingHandle<string> _handle;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "Weight multiplier for rare fishing loot (armor, accessories, tools, Rare+ items). 1x = vanilla. Applies instantly, fish are unaffected.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul registers every feature's settings in one section
            var section = ModSettings.Section(this).Hint(SettingsHint);
            RegisterSettings(section, "");
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>, each label prefixed with <paramref name="prefix"/>.</summary>
        public static void RegisterSettings(SectionBuilder section, string prefix)
        {
            section
                .Choice(out _handle, prefix + "Rare loot multiplier", FishingLootConfig.Ladder, FishingLootConfig.DefaultToken);


            FishingLootConfig.Set(FishingLootConfig.Parse(_handle.Value));
            _handle.OnChanged += token => FishingLootConfig.Set(FishingLootConfig.Parse(token));

            Debug.Log($"[{Name}] Loaded. Rare fishing loot multiplier: {FishingLootConfig.Multiplier:0.##}x");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }


        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
