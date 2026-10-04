using System.Globalization;
using System.Linq;
using SidSettings;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoldenChance
{
    /// <summary>
    /// Multiplies the bonus your talents give to golden plants (Gardening, "chance to gain rare plant")
    /// and golden cooked food (Cooking, "chance for extra cooked food to be rare"). Only the talent
    /// part is scaled: with no points in the talent you get vanilla odds (golden plants keep their 3%
    /// base). See <see cref="Patches.TalentValuePatch"/>.
    /// </summary>
    public sealed class GoldenChanceMod : IMod
    {
        public const string Name = "GoldenChance";
        public const string Version = "1.0.0";

        public static readonly string[] Ladder = { "1x", "1.5x", "2x", "3x" };
        public const string DefaultToken = "2x";

        private static Setting<string> _plants;
        private static Setting<string> _cooking;

        public static float PlantMultiplier { get; private set; } = 2f;
        public static float CookingMultiplier { get; private set; } = 2f;

        public const string SettingsHint = "Multiplies what your golden plant (Gardening) and golden cooking (Cooking) talents give. No talent points = vanilla odds. In multiplayer each player's own setting applies to them.";

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
                .Choice(out _plants, "Golden plant talent", Ladder, DefaultToken)
                .Choice(out _cooking, "Golden cooking talent", Ladder, DefaultToken);

            Apply(false);
            _plants.OnChanged += _ => Apply(true);
            _cooking.OnChanged += _ => Apply(true);
        }

        private static void Apply(bool refresh)
        {
            PlantMultiplier = Parse(_plants?.Value);
            CookingMultiplier = Parse(_cooking?.Value);
            Debug.Log($"[{Name}] Golden plant talent {PlantMultiplier:0.##}x, golden cooking talent {CookingMultiplier:0.##}x");
            if (refresh) Patches.TalentValuePatch.ResendTalentValues();
        }

        private static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f ? v : 1f;
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
