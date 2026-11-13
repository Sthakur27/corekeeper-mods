using System;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using ModOptions;
using Rewired;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StatsHud
{
    /// <summary>
    /// A small text column next to the health bar: FPS, your damage per second, your movement speed and how far
    /// you are from the Core. Client-only (nothing is sent to the server, requiredOn 0).
    ///
    /// Cost: positions and frame times are read every frame (a few float ops); the text is rebuilt only
    /// <see cref="RefreshPerSecond"/> times a second and only re-rendered when it changed. DPS comes from the
    /// damage numbers the game already shows (<see cref="DamageMeter"/>), so it adds no world queries.
    /// </summary>
    public sealed class StatsHudMod : IMod
    {
        public const string Name = "StatsHud";
        public const string Version = "0.2.0";
        public const float RefreshPerSecond = 4f;

        public const string SettingsHint = "A small readout next to the health bar (show/hide key F3, rebind under Controls; hidden in menus). F4 shows/hides the buff icons and minion counter under the health bar (only their visibility). FPS; DPS = damage you dealt over the last 5 seconds "
            + "(your hits, arrows, bombs and other projectiles; burn/poison ticks not counted), and the last fight's average once you stop hitting; "
            + "speed in tiles per second; distance to the Core in tiles. Only on your screen.";

        public static readonly string[] Positions = { "Beside health bar", "Under health bar", "Bottom left corner" };

        internal static Setting<bool> Show, ShowFps, ShowDps, ShowSpeed, ShowCore, ShowBuffIcons;
        internal static Setting<int> LineGap;
        public const int DefaultLineGap = 3;
        /// <summary>Gap between lines in game pixels.</summary>
        internal static int LineGapPixels => LineGap != null ? LineGap.Value : DefaultLineGap;

        public const string ToggleKeyBind = "StatsHud_Toggle";
        public const string BuffsKeyBind = "StatsHud_ToggleBuffs";
        private const string Category = "Stats HUD";
        private static Player _rewired;
        internal static Setting<string> Position, Style;
        /// <summary>True when the readout sits in the bottom left corner and stacks upward from there.</summary>
        internal static bool BottomLeft => Position != null && Position.Value == Positions[2];
        public static readonly string[] Styles = { "Game font", "Plain" };
        internal static bool Plain => Style != null && Style.Value == Styles[1];

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            if (Application.isBatchMode) return; // dedicated server: no HUD, no keyboard
            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(LocalizationModule));
                int category = ControlMappingModule.AddNewCategory(Category);
                ControlMappingModule.AddKeyboardBind(ToggleKeyBind, KeyboardKeyCode.F3, categoryId: category);
                ControlMappingModule.AddKeyboardBind(BuffsKeyBind, KeyboardKeyCode.F4, categoryId: category);
                ControlMappingModule.rewiredStart += () => _rewired = ReInput.players.GetPlayer(0);
                LocalizationModule.AddTerm($"ControlMapper/{Category}Category", Category);
                LocalizationModule.AddTerm($"ControlMapper/{Category}Description", "Show or hide the FPS / DPS / speed / Core readout and the buff icons.");
                LocalizationModule.AddTerm($"ControlMapper/{ToggleKeyBind}", "Show / hide stats HUD");
                LocalizationModule.AddTerm($"ControlMapper/{BuffsKeyBind}", "Show / hide buff icons");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key bind: {e}");
            }
        }

        /// <summary>Overhaul-only feature: Sid's Overhaul builds the settings page via <see cref="RegisterSettings"/>.</summary>
        public void Init() { }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Toggle(out Show, "Show stats HUD", true)
                .Choice(out Position, "Position", Positions, Positions[0])
                .Choice(out Style, "Text style", Styles, Styles[0])
                .Toggle(out ShowFps, "FPS", true)
                .Toggle(out ShowDps, "DPS", true)
                .Toggle(out ShowSpeed, "Movement speed", true)
                .Toggle(out ShowCore, "Distance to the Core", true)
                .Stepper(out LineGap, "Space between lines (pixels)", 0, 32, DefaultLineGap)
                .Toggle(out ShowBuffIcons, "Show buff icons under the health bar", true);
            Position.OnChanged += _ => HudView.Relayout();
            LineGap.OnChanged += _ => HudView.Relayout();
        }

        public void Update()
        {
            PollToggleKey();
            HudView.Update();
            BuffIcons.Update();
        }

        /// <summary>The keys flip "Show stats HUD" / "Show buff icons", so the choice is saved and the menu shows it.</summary>
        private static void PollToggleKey()
        {
            if (_rewired == null || Show == null) return;
            if (Manager.input != null && Manager.input.textInputIsActive) return; // typing in chat
            if (Manager.menu != null && Manager.menu.IsAnyMenuActive()) return;
            if (Pressed(ToggleKeyBind, KeyCode.F3, ref _toggleBound)) Show.Value = !Show.Value;
            if (ShowBuffIcons != null && Pressed(BuffsKeyBind, KeyCode.F4, ref _buffsBound))
            {
                ShowBuffIcons.Value = !ShowBuffIcons.Value;
                Debug.Log($"[{Name}] buff icons key: now {(ShowBuffIcons.Value ? "shown" : "hidden")}");
            }
        }

        // -1 unknown, 0 no keyboard key mapped, 1 mapped; re-checked every few seconds (the player can rebind).
        private static int _toggleBound = -1, _buffsBound = -1;
        private static float _nextBindCheck;

        /// <summary>
        /// The bind's key went down. A bind added after the player's key layout was saved has no key in that saved
        /// layout (Rewired loads the saved map for the category instead of the defaults), so then the default key is
        /// read directly until the player assigns one under Controls.
        /// </summary>
        private static bool Pressed(string bind, KeyCode fallback, ref int bound)
        {
            if (_rewired.GetButtonDown(bind)) return true;
            if (bound < 0 || Time.unscaledTime >= _nextBindCheck)
            {
                if (Time.unscaledTime >= _nextBindCheck) _nextBindCheck = Time.unscaledTime + 3f;
                int now = _rewired.controllers.maps.GetFirstElementMapWithAction(ControllerType.Keyboard, bind, true) != null ? 1 : 0;
                if (now != bound) Debug.Log($"[{Name}] {bind}: {(now == 1 ? "key mapped" : "no key mapped, reading " + fallback + " directly")}");
                bound = now;
            }
            return bound == 0 && Input.GetKeyDown(fallback);
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
    }
}
