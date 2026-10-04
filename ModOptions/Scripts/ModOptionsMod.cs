using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModOptions
{
    /// <summary>
    /// Mod Options: a settings library and "Mod Options" menu for mods (standalone, or
    /// built into Sid's Overhaul). Independent of Mod Settings Menu; both can be installed together.
    /// </summary>
    public sealed class ModOptionsMod : IMod
    {
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[ModOptions] v{Version}");
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
