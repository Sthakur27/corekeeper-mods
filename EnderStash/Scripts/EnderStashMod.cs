using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnderStash
{
    /// <summary>
    /// A personal stash per character, like Minecraft's ender chest: every chest window gets an
    /// extra button that switches it to your stash and back. The stash is stored in extra slots on
    /// the character, so it is the same in every world and is never dropped on death. No settings.
    /// </summary>
    public sealed class EnderStashMod : IMod
    {
        public const string Name = "EnderStash";
        public const string Version = "1.0.0";

        private bool _subscribed;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: {StashLayout.Size}-slot personal stash on every chest.");
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
            EnderStashUI.Update();
        }
    }
}
