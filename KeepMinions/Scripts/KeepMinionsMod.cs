using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KeepMinions
{
    /// <summary>
    /// Summoned minions survive teleporting (portals, waypoints, recall) and arrive with you.
    /// No settings. Server side, see <see cref="Systems.HideTeleportFromMinionsSystem"/>.
    /// </summary>
    public sealed class KeepMinionsMod : IMod
    {
        public const string Name = "KeepMinions";
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version} loaded: minions survive teleports.");
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
