using PugMod;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Loadout fallback (waterfall). In loadouts 2 and up every equipment slot
    /// (helmet, chest, pants, necklace, both rings, off-hand, bag, lantern, pet) uses its own
    /// item if it holds one, otherwise the nearest lower loadout's item (3 -> 2 -> 1). Inherited items
    /// show dimmed in the equipment UI; drop an item onto them to give that loadout its own.
    /// No settings.
    /// </summary>
    public class LoadoutSharingMod : IMod
    {
        public const string Name = "LoadoutSharing";
        public const string Version = "2.3.0";

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
