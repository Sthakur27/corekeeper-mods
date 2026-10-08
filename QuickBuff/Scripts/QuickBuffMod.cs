using System;
using System.Globalization;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.ControlMapping;
using ModOptions;
using PugMod;
using Rewired;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuickBuff
{
    /// <summary>
    /// Terraria-style quick buff. One key press (default B, rebindable under Controls > Quick Buff)
    /// consumes ONE of every distinct food and potion in the player's inventory that grants a timed
    /// buff, applying all of the buffs at once.
    ///
    /// Flow: the client polls the Rewired action in <see cref="Update"/> and sends a CoreLib chat
    /// command ("/quickbuff &lt;skip&gt; &lt;seconds&gt;") to the server. The server-side handler
    /// (<see cref="QuickBuffCommand"/>) runs <see cref="Systems.QuickBuffServerSystem"/>, which
    /// consumes the items through the game's own InventoryChangeBuffer (Create.ConsumeEntityAt, the
    /// same request the Eatable slot uses) and applies the effects with the same game helpers the
    /// vanilla EatableSlotConsumeResultEvaluationSystem uses. Works in single player and on
    /// servers (the command channel is an RPC).
    /// </summary>
    public sealed class QuickBuffMod : IMod
    {
        public const string Name = "QuickBuff";
        public const string Version = "1.4.0";

        /// <summary>Rewired action name registered with CoreLib's control mapping module.</summary>
        public const string UseKeyBind = "QuickBuff_Use";
        /// <summary>Quick heal: drink the first healing potion (default H).</summary>
        public const string HealKeyBind = "QuickBuff_Heal";
        /// <summary>Quick food: eat until full (default F).</summary>
        public const string FoodKeyBind = "QuickBuff_Food";

        /// <summary>Client-side guard between presses (matches the game's default eat cooldown).</summary>
        private const float PressCooldownSeconds = 0.4f;

        public const bool DefaultSkipActive = true;
        public const float DefaultSkipSeconds = 30f;
        public const bool DefaultFoodCookedOnly = true;
        public const bool DefaultFoodSkipFish = false;

        private static Setting<bool> _skipActive;
        private static Setting<float> _skipSeconds;
        private static Setting<bool> _foodCookedOnly;
        private static Setting<bool> _foodSkipFish;

        private static Player _rewiredPlayer;
        private float _nextPressAllowed;
        private float _nextHealAllowed;
        private float _nextFoodAllowed;
        private LoadedMod _modInfo;

        public static bool SkipActive => _skipActive?.Value ?? DefaultSkipActive;
        public static float SkipSeconds => _skipSeconds?.Value ?? DefaultSkipSeconds;
        public static bool FoodCookedOnly => _foodCookedOnly?.Value ?? DefaultFoodCookedOnly;
        public static bool FoodSkipFish => _foodSkipFish?.Value ?? DefaultFoodSkipFish;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");

            _modInfo = API.ModLoader.LoadedMods.FirstOrDefault(info => info.Handlers.Contains(this));
            if (_modInfo == null)
            {
                Debug.LogError($"[{Name}] Mod metadata not found; quick buff command unavailable.");
                return;
            }

            try
            {
                // The command module is the client -> server channel; it depends on control mapping.
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(CommandModule));
                if (!InOverhaul(this)) CommandModule.AddCommands(_modInfo.ModId, Name); // the overhaul registers all commands once
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the /quickbuff command: {e}");
                return;
            }

            if (Application.isBatchMode) return; // dedicated server: no keyboard, command handler is enough

            try
            {
                int category = ControlMappingModule.AddNewCategory("Quick Buff");
                ControlMappingModule.AddKeyboardBind(UseKeyBind, KeyboardKeyCode.B, categoryId: category);
                ControlMappingModule.AddKeyboardBind(HealKeyBind, KeyboardKeyCode.H, categoryId: category);
                ControlMappingModule.AddKeyboardBind(FoodKeyBind, KeyboardKeyCode.F, categoryId: category);
                ControlMappingModule.rewiredStart += OnRewiredStart;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key bind: {e}");
            }
        }

        private static void OnRewiredStart()
        {
            _rewiredPlayer = ReInput.players.GetPlayer(0);
        }

        public const string SettingsHint = "Press the Quick Buff key (default B, rebind under Controls) to eat one of every buff food and drink one of every buff potion in your inventory.";

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Quick Buff").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Toggle(out _skipActive, "Skip buffs that are still active", DefaultSkipActive)
                .Slider(out _skipSeconds, "Still active means more than (seconds)", 0f, 300f, DefaultSkipSeconds, 5f)
                .Toggle(out _foodCookedOnly, "Quick Food: only cooked food", DefaultFoodCookedOnly)
                .Toggle(out _foodSkipFish, "Quick Food: skip fish dishes", DefaultFoodSkipFish);


            Debug.Log($"[{Name}] Loaded. Skip active buffs: {SkipActive} (threshold {SkipSeconds:0}s). Key: {UseKeyBind}.");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }


        public void Update()
        {
            if (_rewiredPlayer == null) return;

            var manager = Manager.main;
            if (manager == null || manager.player == null) return;
            if (Manager.ui == null || Manager.menu == null || Manager.input == null) return;
            if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive() || Manager.input.textInputIsActive) return;

            var inputModule = Manager.input.singleplayerInputModule;
            if (inputModule == null || !inputModule.InputEnabled) return;

            bool heal = _rewiredPlayer.GetButtonDown(HealKeyBind);
            bool food = !heal && _rewiredPlayer.GetButtonDown(FoodKeyBind);
            if (!heal && !food && !_rewiredPlayer.GetButtonDown(UseKeyBind)) return;
            if (heal)
            {
                if (Time.unscaledTime < _nextHealAllowed) return;
                _nextHealAllowed = Time.unscaledTime + PressCooldownSeconds;
            }
            else if (food)
            {
                if (Time.unscaledTime < _nextFoodAllowed) return;
                _nextFoodAllowed = Time.unscaledTime + PressCooldownSeconds;
            }
            else
            {
                if (Time.unscaledTime < _nextPressAllowed) return;
                _nextPressAllowed = Time.unscaledTime + PressCooldownSeconds;
            }

            var comm = CommandModule.ClientCommSystem;
            if (comm == null)
            {
                Debug.LogWarning($"[{Name}] Command channel not ready yet.");
                return;
            }

            string command = heal ? "/quickheal"
                : food ? string.Format(CultureInfo.InvariantCulture, "/quickfood {0} {1:0.#} {2} {3}", SkipActive ? 1 : 0, SkipSeconds, FoodCookedOnly ? 1 : 0, FoodSkipFish ? 1 : 0)
                : string.Format(CultureInfo.InvariantCulture, "/quickbuff {0} {1:0.#}", SkipActive ? 1 : 0, SkipSeconds);
            comm.SendCommand(command, CommandFlags.None);
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
    }
}
