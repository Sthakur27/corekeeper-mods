using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DurabilityMultiplier
{
    /// <summary>
    /// Tools, weapons and armor lose durability at a configurable fraction of the vanilla rate.
    ///
    /// Every durability loss in the game is a request the game leaves on the player entity for one
    /// tick (ReduceDurabilityOfEquippedTriggerCD for the held item, ReduceDurabilityOfAllEquipmentTriggerCD
    /// for armor on hit) and that the Burst ChangeDurabilitySystem consumes. <see cref="Systems.ScaleDurabilityLossSystem"/>
    /// runs one system earlier and scales or cancels those requests, so only the loss actually about
    /// to happen is affected, repairs are untouched and 1x is byte-for-byte vanilla.
    /// </summary>
    public sealed class DurabilityMultiplierMod : IMod
    {
        public const string Name = "DurabilityMultiplier";
        public const string Version = "1.1.0";

        private static Setting<string> _rate;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public const string SettingsHint = "How fast tools, weapons and armor wear out. 1x = vanilla, 0x = never. Applies instantly. In multiplayer the host's setting is the one that counts.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Durability Multiplier").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Choice(out _rate, "Durability loss rate", DurabilityRate.Ladder, DurabilityRate.DefaultToken);


            DurabilityRate.Set(DurabilityRate.Parse(_rate.Value));
            _rate.OnChanged += token =>
            {
                DurabilityRate.Set(DurabilityRate.Parse(token));
                Debug.Log($"[{Name}] Durability loss rate set to {DurabilityRate.Value:0.##}x");
            };

            Debug.Log($"[{Name}] Loaded. Durability loss rate: {DurabilityRate.Value:0.##}x");
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
