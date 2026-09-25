using PugMod;
using UnityEngine;

namespace FiveLoadouts
{
    /// <summary>
    /// Raises the number of equipment loadouts from 3 to 5.
    ///
    /// Mechanism: the player prefab's EquipmentPresetsBuffer gets two more entries at prefab
    /// time (API.Authoring.OnObjectTypeAdded, in every world, so client and server agree), each
    /// with its own helm/necklace/chest/pants/ring/ring/off-hand slots appended to the
    /// ContainedObjectsBuffer, plus a private bag, lantern and pet slot. The game's Burst copy
    /// job indexes the buffer by ActiveEquipmentPresetCD.Value and clamps the client's request to
    /// the buffer length, so five entries need no Burst patch. The character window gets two
    /// more preset tabs (cloned at runtime) and the cycle hotkey wraps at 5.
    ///
    /// LoadoutSharing (Loadout Fallback) is optional. When it is present and updated (see
    /// LoadoutSharing.patch.md), loadouts 4 and 5 get the same fallback-to-loadout-1 rule as 2
    /// and 3. Without it they behave like vanilla 2 and 3 (own gear, shared bag/lantern/pet).
    /// </summary>
    public class FiveLoadoutsMod : IMod
    {
        public const string Name = "FiveLoadouts";
        public const string Version = "1.0.1";

        public void EarlyInit()
        {
            // The loader initialises dependencies first, so when LoadoutSharing is installed its
            // handler is subscribed (and runs) before ours: our slots always land after its six.
            API.Authoring.OnObjectTypeAdded += PresetLayout.OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded. Presets: {PresetLayout.PresetCount}.");
        }

        public void Init()
        {
            LoadoutSharingBridge.Probe();
        }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= PresetLayout.OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        /// <summary>Every frame on the client: make sure the character window has five tabs.</summary>
        public void Update()
        {
            PresetTabsUI.EnsureExtended();
        }
    }
}
