using System.Globalization;
using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoinDrops
{
    /// <summary>
    /// Every enemy drops Ancient Coins when it dies: square root of its max health (100 HP -> 10,
    /// 8302 HP -> 91), times the amount setting; bosses get an extra multiplier.
    /// See <see cref="Systems.CoinDropSystem"/>.
    /// </summary>
    public sealed class CoinDropsMod : IMod
    {
        public const string Name = "CoinDrops";
        public const string Version = "1.1.1";

        public const string SettingsHint = "Enemies killed by a player (or their pets, minions and bombs) drop Ancient Coins: "
            + "half the square root of their max health at the default 0.5x "
            + "(100 HP = 5 coins, 2500 HP = 25), about 20% random spread, at least 1. Bosses get the boss multiplier on top. "
            + "Applies right away; in multiplayer the host's values count.";

        private static readonly string[] AmountLadder = { "Off", "0.5x", "0.75x", "1x", "1.5x", "2x", "3x", "5x" };
        private const string DefaultAmount = "0.5x";

        private static readonly string[] BossLadder = { "1x", "2x", "3x", "5x", "10x" };
        private const string DefaultBoss = "3x";

        private static Setting<string> _amount;
        private static Setting<string> _boss;

        /// <summary>Coins per sqrt(max HP); 0 = off. Read by CoinDropSystem.</summary>
        public static float AmountMultiplier { get; private set; } = 0.5f;

        /// <summary>Extra multiplier for entities with BossCD.</summary>
        public static int BossMultiplier { get; private set; } = 3;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Coin Drops").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _amount, "Coin amount", AmountLadder, DefaultAmount)
                .Choice(out _boss, "Boss multiplier", BossLadder, DefaultBoss);

            Apply();
            _amount.OnChanged += _ => Apply();
            _boss.OnChanged += _ => Apply();
        }

        private static void Apply()
        {
            AmountMultiplier = ParseAmount(_amount?.Value);
            BossMultiplier = int.TryParse(_boss?.Value?.TrimEnd('x'), out int b) && b > 0 ? b : 1;
            Debug.Log($"[{Name}] Coin amount: " + (AmountMultiplier > 0f ? $"{AmountMultiplier:0.##}x sqrt(HP)" : "off")
                      + $", bosses {BossMultiplier}x");
        }

        /// <summary>"1.5x" -> 1.5, "Off" -> 0; unparseable -> 1.</summary>
        private static float ParseAmount(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            if (token == "Off") return 0f;
            return float.TryParse(token.TrimEnd('x'), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
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
