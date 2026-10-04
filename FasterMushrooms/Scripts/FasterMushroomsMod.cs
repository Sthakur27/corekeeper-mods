using System.Linq;
using SidSettings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FasterMushrooms
{
    /// <summary>
    /// Makes mushrooms (the flora that spreads over mycelium) grow and spread faster.
    ///
    /// Mushroom tiles are "root plants": the game's RootPlantGrowSystem rolls a random wait between
    /// RootPlantCD.minTimeBetweenSpread and maxTimeBetweenSpread for every candidate mycelium cell,
    /// and PugFloraSystem (Burst) counts those PugFloraGrowerCD timers down before a new mushroom
    /// tile appears. Both are Burst jobs, so nothing is patched; instead
    /// <see cref="Systems.FasterMushroomsSystem"/> divides the spread window on every mushroom
    /// entity by the multiplier, caps already-running spread timers to the new maximum, and
    /// shortens the per-stage grow timers (GrowTimerCD) of mushrooms the same way. Everything
    /// else (which tiles they can spread to, what they drop, other crops and root plants) stays
    /// vanilla. Runs on the server only, so it is authoritative in multiplayer.
    /// </summary>
    public sealed class FasterMushroomsMod : IMod
    {
        public const string Name = "FasterMushrooms";
        public const string Version = "1.1.0";

        private static Setting<string> _speed;
        private static Setting<string> _respawn;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "How many times faster mycelium roots spread and mushrooms grow. 1x = vanilla. How often picked wild mushrooms get a chance to respawn near you. Vanilla almost never respawns them near players.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Faster Mushrooms").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _speed, "Mushroom growth speed", MushroomSpeed.Ladder, MushroomSpeed.DefaultToken)
                .Choice(out _respawn, "Wild mushroom respawn", MushroomRespawn.Ladder, MushroomRespawn.DefaultToken);


            MushroomSpeed.Set(_speed.Value);
            _speed.OnChanged += token =>
            {
                MushroomSpeed.Set(token);
                Debug.Log($"[{Name}] Mushroom growth speed set to {MushroomSpeed.Multiplier:0.##}x");
            };

            MushroomRespawn.Set(_respawn.Value);
            _respawn.OnChanged += token =>
            {
                MushroomRespawn.Set(token);
                Debug.Log($"[{Name}] Wild mushroom respawn every {MushroomRespawn.IntervalSeconds:0}s (0 = off)");
            };

            Debug.Log($"[{Name}] Loaded. Mushroom growth speed {MushroomSpeed.Multiplier:0.##}x, respawn every {MushroomRespawn.IntervalSeconds:0}s");
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
