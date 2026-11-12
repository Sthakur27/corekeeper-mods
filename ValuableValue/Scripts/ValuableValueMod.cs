using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ValuableValue
{
    /// <summary>
    /// Valuables (gems, statues, ancient relics: every ObjectType.Valuable item) sell to merchants for a multiple of
    /// their vanilla price, 10x by default. Buy prices stay vanilla. See <see cref="Systems.ValuablePriceSystem"/>.
    /// </summary>
    public sealed class ValuableValueMod : IMod
    {
        public const string Name = "ValuableValue";
        public const string Version = "1.0.0";

        public const string SettingsHint = "How much merchants pay for valuables (gems, statues, relics and other items the game files as Valuable). "
            + "Buying prices are unchanged. Applies right away. In multiplayer the host's value counts for the coins you get.";

        public static readonly string[] Ladder = { "1x (vanilla)", "2x", "5x", "8x", "10x", "15x", "20x", "50x", "100x" };
        public const string Default = "10x";

        private static Setting<string> _multiplier;

        /// <summary>Live sell multiplier the system applies (1 = vanilla).</summary>
        public static int Multiplier { get; private set; } = 10;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        /// <summary>Overhaul-only feature: Sid's Overhaul builds the settings page via <see cref="RegisterSettings"/>.</summary>
        public void Init() { }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Choice(out _multiplier, "Valuables sell for", Ladder, Default);
            Apply();
            _multiplier.OnChanged += _ => Apply();
        }

        private static void Apply()
        {
            Multiplier = Parse(_multiplier?.Value);
            Debug.Log($"[{Name}] valuables sell for {Multiplier}x");
        }

        /// <summary>"10x" -> 10, "1x (vanilla)" -> 1; unknown -> 10.</summary>
        private static int Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 10;
            int x = token.IndexOf('x');
            return x > 0 && int.TryParse(token.Substring(0, x), out int n) && n >= 1 ? n : 10;
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
