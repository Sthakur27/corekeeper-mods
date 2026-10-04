using System.Linq;
using SidSettings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VehicleSpeed
{
    /// <summary>
    /// Boat and go-kart speed multipliers (1x / 2x / 3x / 5x / 10x) in the Mod Settings menu.
    /// Replaces the third-party Boat Turbo mod (disable that one: both write the boat multiplier).
    /// See <see cref="Systems.VehicleSpeedSystem"/>.
    /// </summary>
    public sealed class VehicleSpeedMod : IMod
    {
        public const string Name = "VehicleSpeed";
        public const string Version = "1.0.0";

        private static Setting<string> _boat;
        private static Setting<string> _goKart;

        public const string SettingsHint = "Boat and go-kart speed. 1x = vanilla. Applies instantly. In multiplayer everyone should use the host's values.";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Vehicle Speed").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _boat, "Boat speed", SpeedSettings.Ladder, SpeedSettings.DefaultToken)
                .Choice(out _goKart, "Go-kart speed", SpeedSettings.Ladder, SpeedSettings.DefaultToken);

            Apply();
            _boat.OnChanged += _ => Apply();
            _goKart.OnChanged += _ => Apply();
        }

        private static void Apply()
        {
            SpeedSettings.Boat = SpeedSettings.Parse(_boat?.Value);
            SpeedSettings.GoKart = SpeedSettings.Parse(_goKart?.Value);
            SpeedSettings.Version++;
            Debug.Log($"[{Name}] Boat speed {SpeedSettings.Boat:0.##}x, go-kart speed {SpeedSettings.GoKart:0.##}x");
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
