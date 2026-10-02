using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfiniteOreBoulders
{
    /// <summary>
    /// Ore boulders never break: mine them forever, they keep dropping ore at the vanilla rate.
    /// No settings. Server side, see <see cref="Systems.InfiniteOreBouldersSystem"/>.
    /// </summary>
    public sealed class InfiniteOreBouldersMod : IMod
    {
        public const string Name = "InfiniteOreBoulders";
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: ore boulders never break.");
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
