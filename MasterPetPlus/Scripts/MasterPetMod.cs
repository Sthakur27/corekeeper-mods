using System.Linq;
using UnityEngine;
using PugMod;
using MasterPet.Helpers;

namespace MasterPet
{
    public class MasterPetMod : IMod
    {
        public const string Name = "Master Pet Plus";
        // Asset paths inside the bundle still start with Assets/MasterPet/ - do not change.
        public const string InternalName = "MasterPet";
        public const string Version = "1.1.0";

        internal static LoadedMod ModInfo { get; private set; }
        internal static AssetBundle AssetBundle { get; private set; }
        internal static GameObject UIPrefab { get; private set; }
        internal static GameObject OpenButtonPrefab { get; private set; }
        public static GameObject UIInstance { get; private set; }
        public static bool IsUIOpen { get; private set; }

        private GameObject _petTalentsWindow;
        private PetTalentsWindow _petTalentsWindowComponent;
        private GameObject _injectedButton;
        private bool _resetMoved;
        private bool _wasPetTalentsWindowActive;

        public void EarlyInit()
        {
            ModInfo = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (ModInfo == null || ModInfo.AssetBundles == null || ModInfo.AssetBundles.Count == 0) return;

            AssetBundle = ModInfo.AssetBundles[0];
            if (AssetBundle == null) return;

            UIPrefab = AssetBundle.LoadAsset<GameObject>($"Assets/{InternalName}/UI/MasterPetWindow.prefab");
            if (UIPrefab == null)
                Debug.LogError($"[{Name}] Failed to load MasterPetWindow.prefab");

            OpenButtonPrefab = AssetBundle.LoadAsset<GameObject>($"Assets/{InternalName}/UI/OpenMasterPetButton.prefab");
            if (OpenButtonPrefab == null)
                Debug.LogError($"[{Name}] Failed to load OpenMasterPetButton.prefab");
        }

        public void Init()
        {
            API.Client.OnWorldCreated += OnWorldCreated;
            API.Client.OnWorldDestroyed += OnWorldDestroyed;

            if (Manager.ecs?.ClientWorld != null && Manager.ecs.ClientWorld.IsCreated)
                OnWorldCreated();
        }

        public void Shutdown()
        {
            API.Client.OnWorldCreated -= OnWorldCreated;
            API.Client.OnWorldDestroyed -= OnWorldDestroyed;

            if (UIInstance != null)
            {
                Object.Destroy(UIInstance);
                UIInstance = null;
            }

            if (_injectedButton != null)
            {
                Object.Destroy(_injectedButton);
                _injectedButton = null;
            }

            if (AssetBundle != null)
            {
                AssetBundle.Unload(true);
                AssetBundle = null;
            }
        }

        public void Update()
        {
            if (UIInstance == null) return;

            IsUIOpen = UIInstance.activeSelf;

            if (IsUIOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                MasterPetSounds.PlayClose();
                UIInstance.SetActive(false);
                IsUIOpen = false;
                return;
            }

            if (_petTalentsWindow == null)
            {
                _petTalentsWindow = GameObject.Find("PetTalentsWindow");
                _petTalentsWindowComponent = null;
            }

            if (_petTalentsWindow != null && _petTalentsWindowComponent == null)
                _petTalentsWindowComponent = _petTalentsWindow.GetComponent<PetTalentsWindow>();

            bool isPetTalentsActive = _petTalentsWindowComponent != null && _petTalentsWindowComponent.isShowing;

            if (isPetTalentsActive && !_wasPetTalentsWindowActive)
            {
                if (!_resetMoved)
                {
                    UIHelper.SetResetPositions(_petTalentsWindow);
                    _resetMoved = true;
                }

                if (OpenButtonPrefab != null)
                {
                    if (_injectedButton == null)
                    {
                        _injectedButton = UIHelper.InjectOpenMasterPetButton(OpenButtonPrefab, _petTalentsWindow);
                    }
                    else
                    {
                        _injectedButton.SetActive(true);
                    }
                }
            }
            else if (!isPetTalentsActive && _injectedButton != null && _injectedButton.activeSelf)
            {
                _injectedButton.SetActive(false);
            }

            _wasPetTalentsWindowActive = isPetTalentsActive;
        }

        public void ModObjectLoaded(Object obj) { }

        private void OnWorldCreated()
        {
            if (UIPrefab != null && UIInstance == null)
            {
                UIInstance = Object.Instantiate(UIPrefab, API.Rendering.UICamera.transform);
                Object.DontDestroyOnLoad(UIInstance);
                UIInstance.SetActive(false);
            }
        }

        private void OnWorldDestroyed()
        {
            if (UIInstance != null)
            {
                Object.Destroy(UIInstance);
                UIInstance = null;
            }

            if (_injectedButton != null)
            {
                Object.Destroy(_injectedButton);
                _injectedButton = null;
            }

            _petTalentsWindow = null;
            _petTalentsWindowComponent = null;
            _resetMoved = false;
            _wasPetTalentsWindowActive = false;
        }
    }
}