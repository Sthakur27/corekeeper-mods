using ModSettingsMenu.Settings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoReplant
{
    /// <summary>
    /// Auto Replant: when a player harvests a ripe crop, the same seed is planted back into that
    /// tile, consuming one seed of that plant from the player's inventory (the seed the harvest
    /// just dropped counts once it has been picked up). Each auto-replant has a configurable chance
    /// to come up as the golden variant of the plant.
    ///
    /// All game logic lives in <see cref="Systems.AutoReplantSystem"/> (server world only). This
    /// class just registers the Mod Settings section and exposes the live values.
    /// </summary>
    public sealed class AutoReplantMod : IMod
    {
        public const string Name = "AutoReplant";
        public const string Version = "1.0.0";

        public const bool DefaultEnabled = true;
        public const int DefaultGoldenChancePercent = 5;
        public const bool DefaultUseInventorySeeds = true;

        private static SettingHandle<bool> _enabled;
        private static SettingHandle<int> _goldenChance;
        private static SettingHandle<bool> _useInventorySeeds;

        /// <summary>Master switch. Off = vanilla behaviour.</summary>
        public static bool Enabled => _enabled != null ? _enabled.Value : DefaultEnabled;

        /// <summary>Base chance (0..100) that an auto-replant plants the golden variant.</summary>
        public static int GoldenChancePercent
        {
            get
            {
                int v = _goldenChance != null ? _goldenChance.Value : DefaultGoldenChancePercent;
                return v < 0 ? 0 : (v > 100 ? 100 : v);
            }
        }

        /// <summary>
        /// True: any seed of that plant already in the inventory may be used. False: only the seed
        /// the harvest itself dropped (detected as the seed count rising after the harvest) is used.
        /// </summary>
        public static bool UseInventorySeeds => _useInventorySeeds != null ? _useInventorySeeds.Value : DefaultUseInventorySeeds;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            ModSettings.Section(this)
                .Hint("Harvesting a ripe crop replants it from a seed in your inventory. Golden chance replaces the vanilla 3% base roll; your Gardening rare-plant bonus still adds on top.")
                .Toggle(out _enabled, "Auto replant", DefaultEnabled)
                .Stepper(out _goldenChance, "Golden plant chance (%)", 0, 100, DefaultGoldenChancePercent)
                .Toggle(out _useInventorySeeds, "Use seeds from inventory", DefaultUseInventorySeeds)
                .Build();

            Debug.Log($"[{Name}] Loaded. enabled={Enabled} golden={GoldenChancePercent}% useInventorySeeds={UseInventorySeeds}");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
