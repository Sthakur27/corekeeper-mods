using System.Globalization;
using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoldenChance
{
    /// <summary>Adds percentage points to the golden plant and bonus cooked-food talent rolls.</summary>
    public sealed class GoldenChanceMod : IMod
    {
        public const string Name = "GoldenChance";
        public const string Version = "2.0.0";

        public static readonly string[] Ladder = Enumerable.Range(0, 21).Select(i => "+" + (i * 5) + "%").ToArray();
        public const string DefaultToken = "+0%";

        private static Setting<string> _plants;
        private static Setting<string> _cooking;

        public static int PlantBonus { get; private set; }
        public static int CookingBonus { get; private set; }

        private float _nextRefresh;

        public const string SettingsHint = "Adds percentage points, even with no talent points. Vanilla max: plants 18% (3% base + 15% Expert Gardener); Master Chef 25% on BONUS cooked food only. Example: 18% + 5% = 23%. Each player's settings apply to their talents; Auto Replant's base override still applies.";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Golden Chance").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _plants, "Golden plant chance bonus", Ladder, DefaultToken)
                .Choice(out _cooking, "Golden food chance bonus", Ladder, DefaultToken);

            Apply(false);
            _plants.OnChanged += _ => Apply(true);
            _cooking.OnChanged += _ => Apply(true);
        }

        private static void Apply(bool refresh)
        {
            PlantBonus = Parse(_plants?.Value);
            CookingBonus = Parse(_cooking?.Value);
            Debug.Log($"[{Name}] Golden plants +{PlantBonus}%, bonus cooked food +{CookingBonus}%");
            if (refresh) Patches.TalentValuePatch.ResendTalentValues();
        }

        private static int Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            string s = token.Trim().TrimStart('+').TrimEnd('%');
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
                ? Mathf.Clamp(v, 0, 100) : 0;
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Shutdown() { Patches.TalentValuePatch.ClearRefreshState(); }
        public void ModObjectLoaded(Object obj) { }
        public void Update()
        {
            // Also repair the condition after a talent reset (vanilla resets through its blob table).
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.5f;
            Patches.TalentValuePatch.RefreshTalentValues();
        }
    }
}
