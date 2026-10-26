using System;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using Rewired;
using UnityEngine;

namespace FiveLoadouts
{
    /// <summary>
    /// Direct keys for loadouts IV and V, next to vanilla's "Equip loadout 1/2/3" keys.
    /// Rewired actions cannot be added to the vanilla map, so these are CoreLib control-mapping
    /// binds (Controls menu, "Five Loadouts" category). They start unbound so they never clash
    /// with an existing key. The Controls menu shows "ControlMapper/" + name localization terms,
    /// so the category and both actions get English labels in vanilla's wording. A press calls
    /// PlayerController.SetActiveEquipmentPreset exactly like vanilla's preset keys do (only
    /// when no menu is open); the character window tabs follow on their own.
    /// </summary>
    public static class PresetHotkeys
    {
        public const string KeyLoadout4 = "FiveLoadouts_Loadout4";
        public const string KeyLoadout5 = "FiveLoadouts_Loadout5";
        private const string Category = "Five Loadouts";

        private static Player _rewired;

        /// <summary>Called from EarlyInit: registers the binds with CoreLib.</summary>
        public static void Register()
        {
            if (Application.isBatchMode) return; // dedicated server: no keyboard
            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(LocalizationModule));
                int category = ControlMappingModule.AddNewCategory(Category);
                ControlMappingModule.AddKeyboardBind(KeyLoadout4, KeyboardKeyCode.None, categoryId: category);
                ControlMappingModule.AddKeyboardBind(KeyLoadout5, KeyboardKeyCode.None, categoryId: category);
                ControlMappingModule.rewiredStart += OnRewiredStart;

                LocalizationModule.AddTerm($"ControlMapper/{Category}Category", Category);
                LocalizationModule.AddTerm($"ControlMapper/{Category}Description", "Direct keys for loadouts 4 and 5.");
                LocalizationModule.AddTerm($"ControlMapper/{KeyLoadout4}", "Equip loadout 4");
                LocalizationModule.AddTerm($"ControlMapper/{KeyLoadout5}", "Equip loadout 5");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{FiveLoadoutsMod.Name}] Failed to register loadout keys: {e}");
            }
        }

        private static void OnRewiredStart() => _rewired = ReInput.players.GetPlayer(0);

        /// <summary>Every frame on the client.</summary>
        public static void Poll()
        {
            if (_rewired == null) return;
            var manager = Manager.main;
            if (manager == null || manager.player == null) return;
            if (Manager.menu == null || Manager.menu.IsAnyMenuActive()) return;
            if (Manager.input != null && Manager.input.textInputIsActive) return;

            if (_rewired.GetButtonDown(KeyLoadout4)) manager.player.SetActiveEquipmentPreset(3);
            else if (_rewired.GetButtonDown(KeyLoadout5)) manager.player.SetActiveEquipmentPreset(4);
        }
    }
}
