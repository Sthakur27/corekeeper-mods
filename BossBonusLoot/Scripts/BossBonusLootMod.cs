using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossBonusLoot
{
    /// <summary>
    /// Bosses drop guaranteed extra items on top of their normal loot (e.g. Ghorm: 40 Larva Meat +
    /// 40 Golden Larva Meat). No settings; the table is <see cref="Systems.BossBonusLootSystem.Bonus"/>.
    /// </summary>
    public sealed class BossBonusLootMod : IMod
    {
        public const string Name = "BossBonusLoot";
        public const string Version = "1.5.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: bonus boss drops + Hydra gems x2.");
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
