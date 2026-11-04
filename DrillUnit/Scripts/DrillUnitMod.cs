using System;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using PlayerState;
using PugMod;
using Rewired;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillUnit
{
    /// <summary>
    /// Drill Unit: an end-game hover craft crafted at the Rift Statue. It is a boat to the game (BoatRiding,
    /// WASD, the held pickaxe/drill works from the seat), with three changes while someone rides it:
    /// <see cref="Systems.HoverColliderSystem"/> lets the rider cross water and land,
    /// <see cref="Systems.StuckGuardSystem"/> stops the game's "stuck" rescue from pulling the rider back
    /// (or killing them) over terrain a boat could not be on, and <see cref="Patches.SendInputPatch"/> drops
    /// attack input for anything that is not a mining tool. The lay-ground key (default G, rebind under
    /// Controls) toggles <see cref="Systems.LayGroundSystem"/>, which floors water, lava and pits under the
    /// rider with blocks from the inventory.
    /// </summary>
    public sealed class DrillUnitMod : IMod
    {
        public const string Name = "DrillUnit";
        public const string Version = "0.1.0";
        public const string ObjectName = "DrillUnit_Unit";
        public const string GroundKeyBind = "DrillUnit_LayGround";

        private static ObjectID _id;
        // Mod object IDs are fixed once registered; cache to skip the string lookup.
        public static ObjectID Id
        {
            get
            {
                if (_id == ObjectID.None) _id = API.Authoring.GetObjectID(ObjectName);
                return _id;
            }
        }

        /// <summary>Client: the lay-ground toggle, sent to the server in the input bits.</summary>
        public static bool LayGround;

        private static Player _rewiredPlayer;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            if (Application.isBatchMode) return; // dedicated server: no keyboard

            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule));
                int category = ControlMappingModule.AddNewCategory("Drill Unit");
                ControlMappingModule.AddKeyboardBind(GroundKeyBind, KeyboardKeyCode.G, categoryId: category);
                ControlMappingModule.rewiredStart += () => _rewiredPlayer = ReInput.players.GetPlayer(0);

                CoreLibMod.LoadSubmodule(typeof(LocalizationModule));
                LocalizationModule.AddTerm("ControlMapper/Drill UnitCategory", "Drill Unit");
                LocalizationModule.AddTerm("ControlMapper/Drill UnitDescription", "Keys while riding the Drill Unit.");
                LocalizationModule.AddTerm($"ControlMapper/{GroundKeyBind}", "Lay ground on / off");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key bind: {e}");
            }
        }

        public void Init() { }

        public void Update()
        {
            if (_rewiredPlayer == null || !_rewiredPlayer.GetButtonDown(GroundKeyBind) || !KeysAllowed()) return;
            var player = Manager.main != null ? Manager.main.player : null;
            if (player == null || player.world == null || !IsRiding(player.world.EntityManager, player.entity, out _)) return;
            LayGround = !LayGround;
            ChatNote.Show(LayGround ? "Drill Unit: laying ground over water, lava and pits" : "Drill Unit: lay ground off");
        }

        private static bool KeysAllowed()
        {
            if (Manager.ui == null || Manager.menu == null || Manager.input == null) return false;
            if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive() || Manager.input.textInputIsActive) return false;
            var input = Manager.input.singleplayerInputModule;
            return input != null && input.InputEnabled;
        }

        /// <summary>True when <paramref name="player"/> sits in a Drill Unit (boat riding state, controlling one).</summary>
        public static bool IsRiding(EntityManager em, Entity player, out Entity unit)
        {
            unit = Entity.Null;
            var id = Id;
            if (id == ObjectID.None || player == Entity.Null || !em.Exists(player)
                || !em.HasComponent<PlayerStateCD>(player) || !em.HasComponent<ControllingOtherEntityCD>(player)) return false;
            if (!em.GetComponentData<PlayerStateCD>(player).HasAnyState(PlayerStateEnum.BoatRiding)) return false;
            unit = em.GetComponentData<ControllingOtherEntityCD>(player).controlledEntity;
            return unit != Entity.Null && em.Exists(unit) && em.HasComponent<ObjectDataCD>(unit)
                && em.GetComponentData<ObjectDataCD>(unit).objectID == id;
        }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            RiftRecipe.EnsureRecipe(entity, em);
        }

        public void Shutdown() { API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded; }
        public void ModObjectLoaded(Object obj)
        {
            if (!(obj is Texture2D tex)) return;
            if (tex.name == "DrillUnit_front") Patches.UnitSkinPatch.Front = tex;
            else if (tex.name == "DrillUnit_back") Patches.UnitSkinPatch.Back = tex;
            else return;
            Debug.Log($"[{Name}] Skin sheet {tex.name} loaded ({tex.width}x{tex.height}).");
        }
    }
}
