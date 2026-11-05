using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoinPouch
{
    /// <summary>
    /// A coin slot in the character window, under the vanity pants. Ancient Coins you pick up go there
    /// on their own, it holds up to <see cref="CoinLayout.MaxCoins"/>, it is shared by every loadout, and
    /// merchants (and anything else that costs coins) take payment from it like from the inventory.
    /// The slot is an extra one-slot inventory range on the player (<see cref="CoinLayout"/>), so the
    /// game's own pickup, buy and consume code already sees it; <see cref="Systems.CoinPouchSystem"/>
    /// moves coins that land in the inventory (vanilla stacks stop at 9999) into it.
    /// </summary>
    public sealed class CoinPouchMod : IMod
    {
        public const string Name = "CoinPouch";
        public const string Version = "1.1.0";

        private int _frames;
        private bool _subscribed;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: coin slot up to {CoinLayout.MaxCoins} coins.");
        }

        public void Init() { }

        public void Shutdown()
        {
            if (_subscribed) API.Authoring.OnObjectTypeAdded -= CoinLayout.OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        public void Update()
        {
            // Subscribed on the second frame: Ender Stash subscribes on its first, so the coin slot is
            // always the very last player slot (CoinLayout.Relocate relies on that).
            if (!_subscribed && ++_frames >= 2)
            {
                API.Authoring.OnObjectTypeAdded += CoinLayout.OnObjectTypeAdded;
                _subscribed = true;
            }
            CoinSlotUI.Update();
        }
    }
}
