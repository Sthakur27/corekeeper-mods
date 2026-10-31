using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using PugMod;
using Rewired;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ItemSpawner
{
    /// <summary>
    /// Searchable spawn browser for hosts and admins. Press the Item Spawner key (default Z, rebind
    /// under Controls > Item Spawner) to open a grid of every item in the game, modded items included.
    /// Click puts one on the cursor, Ctrl-click a full stack on the cursor, Shift-click a full stack in
    /// the bag. Gear and tools spawn at the level chosen in the panel.
    ///
    /// Spawning uses the game's own CreateItem player command, so the server needs nothing extra
    /// (requiredOn 0). The admin check is client-side, the same rule as the vanilla debug tools.
    /// The panel reuses Runic Storage's browser prefab and category tabs.
    /// </summary>
    public sealed class ItemSpawnerMod : IMod
    {
        public const string Name = "ItemSpawner";
        public const string Version = "1.0.0";
        public const string ToggleKeyBind = "ItemSpawner_Toggle";
        private const string PrefabPath = "Assets/ConnectedStorageBrowser/ConnectedStorageBrowser.prefab";

        private static Player _rewiredPlayer;
        private GameObject _host;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            if (Application.isBatchMode) return; // dedicated server: nothing to show
            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(LocalizationModule));
                int category = ControlMappingModule.AddNewCategory("Item Spawner");
                ControlMappingModule.AddKeyboardBind(ToggleKeyBind, KeyboardKeyCode.Z, categoryId: category);
                ControlMappingModule.rewiredStart += () => _rewiredPlayer = ReInput.players.GetPlayer(0);
                LocalizationModule.AddTerm("ControlMapper/Item SpawnerCategory", "Item Spawner");
                LocalizationModule.AddTerm("ControlMapper/Item SpawnerDescription", "Open the item spawn browser (host and admins).");
                LocalizationModule.AddTerm($"ControlMapper/{ToggleKeyBind}", "Open item spawner");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key bind: {e}");
            }
        }

        public void Init()
        {
            if (Application.isBatchMode) return;
            _host = new GameObject("Item Spawner UI");
            Object.DontDestroyOnLoad(_host);
            _host.AddComponent<SpawnerUI>();
        }

        public void ModObjectLoaded(Object obj) { }

        public void Shutdown()
        {
            if (_host != null) Object.Destroy(_host);
        }

        public void Update()
        {
            if (_rewiredPlayer == null || !_rewiredPlayer.GetButtonDown(ToggleKeyBind)) return;
            if (SpawnerUI.Active)
            {
                if (!SpawnerUI.Typing) SpawnerUI.Close("toggle key");
                return;
            }
            var manager = Manager.main;
            if (manager == null || manager.player == null || Manager.ui == null || Manager.menu == null || Manager.input == null) return;
            if (Manager.menu.IsAnyMenuActive() || Manager.input.textInputIsActive) return;
            if (SpawnerUI.Prefab == null) SpawnerUI.Prefab = FindPrefab();
            SpawnerUI.Open();
        }

        // Runic Storage ships the browser layout; inside Sid's Overhaul it is in the overhaul's own bundle.
        private static GameObject FindPrefab()
        {
            foreach (var mod in API.ModLoader.LoadedMods)
            {
                if (mod.AssetBundles == null) continue;
                foreach (var bundle in mod.AssetBundles)
                {
                    if (bundle == null || !bundle.GetAllAssetNames().Any(n => n.Equals(PrefabPath, StringComparison.OrdinalIgnoreCase))) continue;
                    var prefab = bundle.LoadAsset<GameObject>(PrefabPath);
                    if (prefab != null) return prefab;
                }
            }
            Debug.LogError($"[{Name}] Browser layout not found. Install Runic Storage or Sid's Overhaul.");
            return null;
        }
    }
}
