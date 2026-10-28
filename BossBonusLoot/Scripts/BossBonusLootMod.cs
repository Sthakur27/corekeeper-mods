using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossBonusLoot
{
    /// <summary>
    /// Bosses drop guaranteed extra items on top of their normal loot (e.g. Ghorm: 40 Larva Meat +
    /// 40 Golden Larva Meat); the table is <see cref="Systems.BossBonusLootSystem.Bonus"/>. One
    /// setting: how much Larva Meat every monster drops (<see cref="Systems.LarvaMeatSystem"/>).
    /// </summary>
    public sealed class BossBonusLootMod : IMod
    {
        public const string Name = "BossBonusLoot";
        public const string Version = "1.6.0";

        public const string SettingsHint = "Multiplies the Larva Meat every monster drops (1x = vanilla). "
            + "Ghorm's fixed bonus is not scaled. Applies right away; in multiplayer the host's value counts.";

        private static readonly string[] LarvaLadder = { "1x", "2x", "3x", "5x", "10x" };

        private static Setting<string> _larva;

        /// <summary>Larva Meat amount multiplier read by LarvaMeatSystem.</summary>
        public static int LarvaMeatMultiplier { get; private set; } = 3;

        /// <summary>Bumped on every change so per-world systems know to re-apply.</summary>
        public static int LarvaMeatVersion { get; private set; }

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: bonus boss drops + Hydra gems x2 + Larva Meat multiplier.");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Boss Bonus Loot").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Choice(out _larva, "Larva Meat drops", LarvaLadder, "3x");
            SetLarva(_larva.Value);
            _larva.OnChanged += SetLarva;
        }

        private static void SetLarva(string token)
        {
            int v = int.TryParse(token?.TrimEnd('x'), out int p) && p > 0 ? p : 1;
            if (v == LarvaMeatMultiplier && LarvaMeatVersion > 0) return;
            LarvaMeatMultiplier = v;
            LarvaMeatVersion++;
            Debug.Log($"[{Name}] Larva Meat drops: {v}x");
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
