using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using PugMod;
using Rewired;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoLoadouts
{
    /// <summary>
    /// Auto Loadout Switch: link an item to a loadout, and the game switches to that loadout when you
    /// start holding that item (pickaxe -> mining gear, sword -> melee gear).
    ///
    /// Binding: hold the item, pick the loadout, press the bind key (Ctrl+L by default). Press it again in
    /// the same loadout to unlink. Links are per exact item type (ObjectID) and per character, stored on
    /// this client in AutoLoadouts/bindings.cfg.
    ///
    /// Switching is edge-triggered: it only fires on the frame the held item type changes. The mod never
    /// enforces a loadout afterwards, so picking another loadout by hand sticks until you next switch to a
    /// different linked item. Items without a link (food, blocks, torches) leave the loadout alone.
    /// The switch itself is PlayerController.SetActiveEquipmentPreset, the same call vanilla's loadout
    /// keys make, so it works with Five Loadouts and Shared Loadout Gear unchanged. Client only.
    /// </summary>
    public sealed class AutoLoadoutsMod : IMod
    {
        public const string Name = "AutoLoadouts";
        public const string Version = "1.0.0";

        public const string BindKey = "AutoLoadouts_Bind";
        public const string TooltipTerm = "AutoLoadouts/SwitchesTo"; // "Switches to loadout {0} when held"
        private const string Category = "Auto Loadout Switch";

        private static Player _rewired;
        private LoadedMod _info;

        /// <summary>Player entity we last saw; a new one (world join, respawn) resets tracking.</summary>
        private Entity _trackedPlayer = Entity.Null;
        private ObjectID _lastHeld = ObjectID.None;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            _info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (Application.isBatchMode) return; // dedicated server: no player, no keyboard

            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(LocalizationModule));
                int category = ControlMappingModule.AddNewCategory(Category);
                ControlMappingModule.AddKeyboardBind(BindKey, KeyboardKeyCode.L, ModifierKey.Control, categoryId: category);
                ControlMappingModule.rewiredStart += () => _rewired = ReInput.players.GetPlayer(0);

                LocalizationModule.AddTerm($"ControlMapper/{Category}Category", Category);
                LocalizationModule.AddTerm($"ControlMapper/{Category}Description", "Link the held item to the current loadout.");
                LocalizationModule.AddTerm($"ControlMapper/{BindKey}", "Link / unlink held item to this loadout");
                // Tooltip stat lines are always looked up as terms (dontLocalize is ignored there).
                LocalizationModule.AddTerm(TooltipTerm, "Switches to loadout {0} when held");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key bind: {e}");
            }
        }

        public void Init()
        {
            if (Application.isBatchMode) return;
            Bindings.Open(_info);
        }

        public void Update()
        {
            var manager = Manager.main;
            var player = manager != null ? manager.player : null;
            var world = API.Client.World;
            if (player == null || world == null || !world.IsCreated || player.entity == Entity.Null)
            {
                _trackedPlayer = Entity.Null;
                return;
            }

            ObjectID held = HeldItem(player);

            if (_trackedPlayer != player.entity)
            {
                // Fresh join or new player entity: remember what is in hand without switching, so loading
                // in never changes the loadout you logged out with.
                _trackedPlayer = player.entity;
                _lastHeld = held;
                Bindings.SelectCharacter();
                return;
            }

            if (BindPressed()) ToggleBinding(player, world, held);

            if (held == _lastHeld) return;
            _lastHeld = held;
            if (held == ObjectID.None) return;

            if (!Bindings.TryGet(held, out int preset)) return;
            if (preset == player.activeEquipmentPreset) return;
            if (preset >= PresetCount(player, world)) return; // e.g. linked to loadout 5, Five Loadouts removed
            player.SetActiveEquipmentPreset(preset);
        }

        private static ObjectID HeldItem(PlayerController player)
        {
            try
            {
                return player.GetHeldObject().objectID;
            }
            catch
            {
                return ObjectID.None; // inventory not set up yet
            }
        }

        private static void ToggleBinding(PlayerController player, World world, ObjectID held)
        {
            if (held == ObjectID.None)
            {
                EmoteNote.Show("Hold an item to link it");
                return;
            }

            int preset = player.activeEquipmentPreset;
            if (Bindings.TryGet(held, out int existing) && existing == preset)
            {
                Bindings.Remove(held);
                EmoteNote.Show($"Unlinked from loadout {Numeral(preset)}");
            }
            else
            {
                Bindings.Set(held, preset);
                EmoteNote.Show($"Linked to loadout {Numeral(preset)}");
            }
        }

        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        /// <summary>Loadout number as the character window's tabs show it (Roman numerals).</summary>
        public static string Numeral(int preset) => preset >= 0 && preset < Numerals.Length ? Numerals[preset] : (preset + 1).ToString();

        private static int PresetCount(PlayerController player, World world)
        {
            var em = world.EntityManager;
            if (!em.Exists(player.entity) || !em.HasBuffer<EquipmentPresetsBuffer>(player.entity)) return 3;
            return em.GetBuffer<EquipmentPresetsBuffer>(player.entity, true).Length;
        }

        private static bool BindPressed()
        {
            if (_rewired == null || !_rewired.GetButtonDown(BindKey)) return false;
            if (Manager.menu == null || Manager.menu.IsAnyMenuActive()) return false;
            if (Manager.input != null && Manager.input.textInputIsActive) return false;
            return true;
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
    }
}
