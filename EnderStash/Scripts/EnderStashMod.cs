using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnderStash
{
    /// <summary>
    /// A personal stash per character, like Minecraft's ender chest: the Ender Chest (sold by the Fishing
    /// Merchant for 9999 coins, asset bundle in Bundles/) opens your stash instead of an inventory of its
    /// own. The stash is stored in extra slots on the character, so it is the same in every world and
    /// every Ender Chest, and is never dropped on death. Regular chests are vanilla. No settings.
    /// </summary>
    public sealed class EnderStashMod : IMod
    {
        public const string Name = "EnderStash";
        public const string Version = "2.0.0";

        private bool _subscribed;
        private bool _loggedId;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: {StashLayout.Size}-slot personal stash behind the Ender Chest.");
        }

        public void Init() { }

        public void Shutdown()
        {
            if (_subscribed) API.Authoring.OnObjectTypeAdded -= StashLayout.OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        public void Update()
        {
            // Subscribed on the first frame, after every mod's EarlyInit/Init subscription
            // (Loadout Fallback, Five Loadouts, vanity slots), so the stash slots are always the
            // last ones appended and other mods' saved slot indices never move.
            if (!_subscribed)
            {
                API.Authoring.OnObjectTypeAdded += StashLayout.OnObjectTypeAdded;
                _subscribed = true;
            }
            if (!_loggedId && EnderChest.Id != ObjectID.None)
            {
                _loggedId = true;
                Debug.Log($"[{Name}] Ender Chest registered as ObjectID {(int)EnderChest.Id}.");
            }
            EnderStashUI.Update();
        }
    }
}
