using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MerchantRestock
{
    /// <summary>
    /// How often each merchant restocks, in one place (Settings > Mod Options > Merchant Restock).
    /// Vanilla restocks every 25-35 min; see <see cref="Systems.MerchantRestockSystem"/>.
    /// </summary>
    public sealed class MerchantRestockMod : IMod
    {
        public const string Name = "MerchantRestock";
        public const string Version = "1.2.0";

        public const string SettingsHint = "How often merchants refill their stock (vanilla: every 25-35 min). "
            + "'All merchants' applies to every merchant set to 'Same as all'. Applies right away, including to the current countdown. "
            + "'Minimum stock per item' raises how many of each regular stackable item a merchant stocks per restock "
            + "(gear and premium items stocked at 1-3 stay as they are). "
            + "Shift- or Ctrl-click on a shop slot buys the whole stack, or as many as you can afford. "
            + "In multiplayer the host's values count.";

        private static Setting<string> _all;
        private static Setting<string> _floor;
        private static Setting<bool> _quickBuy;
        private static readonly Setting<string>[] _perMerchant = new Setting<string>[RestockSettings.Merchants.Length];

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Merchant Restock").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Choice(out _all, "All merchants", RestockSettings.Ladder, RestockSettings.Vanilla);
            for (int i = 0; i < RestockSettings.Merchants.Length; i++)
                section.Choice(out _perMerchant[i], RestockSettings.Merchants[i].label, RestockSettings.MerchantLadder, RestockSettings.SameAsAll);

            section.Toggle(out _quickBuy, "Shift/Ctrl-click buys the whole stack", true);
            Patches.QuickBuyPatch.Enabled = _quickBuy.Value;
            _quickBuy.OnChanged += v => Patches.QuickBuyPatch.Enabled = v;

            section.Choice(out _floor, "Minimum stock per item", RestockSettings.FloorLadder, RestockSettings.FloorOff);
            _floor.OnChanged += v => RestockSettings.SetFloor(RestockSettings.ParseFloor(v));
            RestockSettings.SetFloor(RestockSettings.ParseFloor(_floor.Value));

            Apply();
            _all.OnChanged += _ => Apply();
            foreach (var s in _perMerchant) s.OnChanged += _ => Apply();
        }

        private static void Apply()
        {
            int all = RestockSettings.ParseSeconds(_all?.Value);
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < RestockSettings.Merchants.Length; i++)
            {
                string token = _perMerchant[i]?.Value ?? RestockSettings.SameAsAll;
                int seconds = token == RestockSettings.SameAsAll ? all : RestockSettings.ParseSeconds(token);
                RestockSettings.SetCap(RestockSettings.Merchants[i].id, seconds);
                parts.Add($"{RestockSettings.Merchants[i].label} {(seconds <= 0 ? "vanilla" : seconds < 60 ? seconds + " sec" : seconds / 60 + " min")}");
            }
            Debug.Log($"[{Name}] restock: {string.Join(", ", parts)}");
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
