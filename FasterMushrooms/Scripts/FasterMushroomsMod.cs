using ModSettingsMenu.Settings;
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

        private SettingHandle<string> _speed;
        private SettingHandle<string> _respawn;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            ModSettings.Section(this)
                .Hint("How many times faster mycelium roots spread and mushrooms grow. 1x = vanilla.")
                .Choice(out _speed, "Mushroom growth speed", MushroomSpeed.Ladder, MushroomSpeed.DefaultToken)
                .Hint("How often picked wild mushrooms get a chance to respawn near you. Vanilla almost never respawns them near players.")
                .Choice(out _respawn, "Wild mushroom respawn", MushroomRespawn.Ladder, MushroomRespawn.DefaultToken)
                .Build();

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

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
