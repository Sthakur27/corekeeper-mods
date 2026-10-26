using PugMod;
using System.Linq;
using ModOptions;
using UnityEngine;

namespace MushroomSlayer
{
    public sealed class MushroomSlayerMod : IMod
    {
        public const string Version = "1.1.1";
        public const string SettingsHint = "Gray armor: hits below the threshold deal 1 damage; stronger hits use the damage percentage. Red phase unchanged. Match settings on host and clients.";
        private static Setting<int> _threshold;
        private static Setting<int> _percent;
        public void EarlyInit()
        {
            Debug.Log("[MushroomSlayer] v" + Version + " loaded. Defaults: threshold 200, damage 10%.");
        }
        public void Init()
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (info != null && info.Metadata.name == "SidsOverhaul") return;
            var page = SettingsPages.Create(this, "Mushroom Slayer").Hint(SettingsHint);
            RegisterSettings(page);
            page.Build();
        }
        public static void RegisterSettings(SettingsPage page)
        {
            page.Stepper(out _threshold, "Damage threshold", 1, 10000, 200, 25);
            page.Stepper(out _percent, "Damage through armor (%)", 1, 100, 10, 1);
            ArmorRule.Threshold = _threshold.Value;
            ArmorRule.Percent = _percent.Value;
            _threshold.OnChanged += value => ArmorRule.Threshold = value;
            _percent.OnChanged += value => ArmorRule.Percent = value;
        }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
