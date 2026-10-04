using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SidSettings
{
    /// <summary>
    /// Sid's Settings: the settings library and "Sid's Mods" menu used by Sid's mods (standalone, or
    /// built into Sid's Overhaul). Independent of Mod Settings Menu; both can be installed together.
    /// </summary>
    public sealed class SidSettingsMod : IMod
    {
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[SidSettings] v{Version}");
        }

        public void Init() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }

        public void Update()
        {
            UI.SettingsMenu.Update();
        }
    }
}
