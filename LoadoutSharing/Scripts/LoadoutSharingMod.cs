using PugMod;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>
    /// Loadout fallback. Loadout 1 is the base set. In loadouts 2 and 3 every equipment slot
    /// (helmet, chest, pants, necklace, both rings, off-hand, bag, lantern, pet) uses its own
    /// item if it holds one, otherwise it falls through to loadout 1's item. Inherited items
    /// show dimmed in the equipment UI; drop an item onto them to give that loadout its own.
    /// No settings.
    /// </summary>
    public class LoadoutSharingMod : IMod
    {
        public const string Name = "LoadoutSharing";
        public const string Version = "2.1.0";

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += SlotLayout.OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded (fallback mode).");
        }

        public void Init() { }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= SlotLayout.OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        /// <summary>Every frame: keep the equipment UI handlers on the effective slots.</summary>
        public void Update()
        {
            HandlerSync.SyncToEffective();
        }
    }
}
