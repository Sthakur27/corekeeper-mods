using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Loadout fallback (waterfall). In loadouts 2 and up every equipment slot
    /// (helmet, chest, pants, necklace, both rings, off-hand, bag, lantern, pet) uses its own
    /// item if it holds one, otherwise the nearest lower loadout's item (3 -> 2 -> 1). Inherited items
    /// show dimmed in the equipment UI; drop an item onto them to give that loadout its own.
    /// One setting: "Vanity fallback" (default on) applies the same rule to the vanity slots.
    /// </summary>
    public class LoadoutSharingMod : IMod
    {
        public const string Name = "LoadoutSharing";
        public const string Version = "2.4.1";

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += SlotLayout.OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded (fallback mode).");
        }

        public void Init()
        {
            // Subscribed here, after every mod's EarlyInit subscription (Five Loadouts), so the vanity
            // slots are appended after all other extra slots and existing saves keep their indices.
            API.Authoring.OnObjectTypeAdded += VanityLayout.OnObjectTypeAdded;

            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Shared Loadout Gear & Vanity").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        public const string SettingsHint = "Empty vanity slots show the nearest earlier loadout's vanity piece, like armor does. Hide a piece in a loadout to stop it there.";

        private static Setting<bool> _vanityFallback;

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Toggle(out _vanityFallback, "Vanity fallback", true);
            VanitySync.Fallback = _vanityFallback.Value;
            _vanityFallback.OnChanged += on => VanitySync.Fallback = on;
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= SlotLayout.OnObjectTypeAdded;
            API.Authoring.OnObjectTypeAdded -= VanityLayout.OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        /// <summary>Every frame: keep the equipment UI handlers on the effective slots.</summary>
        public void Update()
        {
            HandlerSync.SyncToEffective();
            VanitySync.SyncAll();
            VanitySlotsUI.Update();
        }
    }
}
