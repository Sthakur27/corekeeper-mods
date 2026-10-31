using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Localization;
using ModOptions;
using PlayerEquipment;
using PugMod;
using Rewired;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuildTools
{
    /// <summary>
    /// Build Tools: bigger, resizable tool areas remembered per tool type, a block brush (size remembered
    /// per category), ground/wall mode and replace for blocks, and roofing gadget modes. A clean-room
    /// replacement for Placement Plus + Roofing Gadget Plus that leaves watering can sizes to Bigger
    /// Watering Cans (or vanilla): the size keys can only shrink a can.
    ///
    /// The client (<see cref="Update"/>) turns keys and saved choices into <see cref="InputBits"/>, which
    /// <see cref="Patches.SendInputPatch"/> adds to the player's own network input every frame. The
    /// systems read those bits in both worlds: <see cref="Systems.ToolSizeSystem"/> (tool sizes),
    /// <see cref="Systems.BrushPlaceSystem"/> (brush, block mode, replace) and
    /// <see cref="Systems.RoofModeSystem"/>. <see cref="Systems.ToolCeilingSystem"/> raises the ceilings
    /// of shovels, hoes, the roofing gadget and paint tools.
    /// </summary>
    public sealed class BuildToolsMod : IMod
    {
        public const string Name = "BuildTools";
        public const string Version = "0.4.1";

        public const string IncreaseKeyBind = "BuildTools_IncreaseSize";
        public const string DecreaseKeyBind = "BuildTools_DecreaseSize";
        public const string ShapeKeyBind = "BuildTools_BrushShape";
        public const string ModeKeyBind = "BuildTools_Mode";
        public const string ReplaceKeyBind = "BuildTools_Replace";

        /// <summary>Hold a size key this long and it repeats.</summary>
        private const float RepeatSeconds = 0.12f;
        private const int DefaultBrushEdge = 3;
        private const string RoofModeKey = "RoofMode";
        private const string BlockModeKey = "BlockMode";

        public const string SettingsHint = "Keypad + / - change the size of the held tool or block brush (down to 1x1), C changes the brush shape, V switches blocks between toggle / ground only / walls only and the roofing gadget between toggle / add roof / remove roof, hold Left Alt to replace blocks (rebind under Controls). Every tool type and block category remembers its own size.";

        private static Setting<int> _ceiling;
        private static Setting<bool> _remember;
        private static Setting<bool> _roofingNeverWears;

        public static bool RememberSizes => _remember?.Value ?? true;
        public static bool RoofingNeverWears => _roofingNeverWears?.Value ?? true;

        /// <summary>The brush the preview draws; Active only while a brush-able placeable is held.</summary>
        public static class ClientBrush
        {
            public static bool Active;
            public static Entity Prefab;
            public static BrushShape Shape;
            public static int Edge;
        }

        private static Player _rewiredPlayer;
        private float _plusHeld, _minusHeld;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            if (Application.isBatchMode) return; // dedicated server: no keyboard

            try
            {
                CoreLibMod.LoadSubmodule(typeof(ControlMappingModule));
                int category = ControlMappingModule.AddNewCategory("Build Tools");
                ControlMappingModule.AddKeyboardBind(IncreaseKeyBind, KeyboardKeyCode.KeypadPlus, categoryId: category);
                ControlMappingModule.AddKeyboardBind(DecreaseKeyBind, KeyboardKeyCode.KeypadMinus, categoryId: category);
                ControlMappingModule.AddKeyboardBind(ShapeKeyBind, KeyboardKeyCode.C, categoryId: category);
                ControlMappingModule.AddKeyboardBind(ModeKeyBind, KeyboardKeyCode.V, categoryId: category);
                ControlMappingModule.AddKeyboardBind(ReplaceKeyBind, KeyboardKeyCode.LeftAlt, categoryId: category);
                ControlMappingModule.rewiredStart += OnRewiredStart;

                CoreLibMod.LoadSubmodule(typeof(LocalizationModule));
                LocalizationModule.AddTerm("ControlMapper/Build ToolsCategory", "Build Tools");
                LocalizationModule.AddTerm("ControlMapper/Build ToolsDescription", "Tool and brush size, brush shape, block / roofing mode and replace.");
                LocalizationModule.AddTerm($"ControlMapper/{IncreaseKeyBind}", "Tool / brush size +");
                LocalizationModule.AddTerm($"ControlMapper/{DecreaseKeyBind}", "Tool / brush size -");
                LocalizationModule.AddTerm($"ControlMapper/{ShapeKeyBind}", "Brush shape");
                LocalizationModule.AddTerm($"ControlMapper/{ModeKeyBind}", "Block / roofing mode");
                LocalizationModule.AddTerm($"ControlMapper/{ReplaceKeyBind}", "Replace blocks (hold)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Name}] Failed to register the key binds: {e}");
            }
        }

        private static void OnRewiredStart()
        {
            _rewiredPlayer = ReInput.players.GetPlayer(0);
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Build Tools").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Stepper(out _ceiling, "Max tool size (shovel, hoe, roofing, paint)", ToolSizes.MinCeiling, ToolSizes.MaxCeiling, ToolSizes.DefaultCeiling)
                .Toggle(out _remember, "Remember sizes per tool and block type", true)
                .Toggle(out _roofingNeverWears, "Roofing gadget never wears out", true);

            SizeMemory.Persist = RememberSizes;
            _remember.OnChanged += v => SizeMemory.Persist = v;
            ApplyCeiling();
            _ceiling.OnChanged += _ => ApplyCeiling();
        }

        private static void ApplyCeiling()
        {
            ToolSizes.Ceiling = _ceiling?.Value ?? ToolSizes.DefaultCeiling;
            ToolSizes.Version++;
            Debug.Log($"[{Name}] Max tool size {ToolSizes.Ceiling}x{ToolSizes.Ceiling}, remember sizes {RememberSizes}, roofing never wears {RoofingNeverWears}.");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Update()
        {
            ClientBrush.Active = false;
            var world = API.Client.World;
            var manager = Manager.main;
            if (world == null || !world.IsCreated || manager == null || manager.player == null)
            {
                Patches.SendInputPatch.Bits = 0;
                return;
            }

            var em = world.EntityManager;
            Entity player = manager.player.entity;
            if (player == Entity.Null || !em.Exists(player) ||
                !em.HasComponent<EquipmentSlotCD>(player) || !em.HasComponent<EquippedObjectCD>(player) ||
                !em.HasBuffer<PlacementSizeByEquipmentTypeBuffer>(player)) return;

            var slot = em.GetComponentData<EquipmentSlotCD>(player).slotType;
            var held = em.GetComponentData<EquippedObjectCD>(player);

            int delta = 0;
            bool shapeKey = false, modeKey = false, replace = false, rotate = false;
            if (KeysAllowed())
            {
                if (Pressed(IncreaseKeyBind, ref _plusHeld)) delta++;
                if (Pressed(DecreaseKeyBind, ref _minusHeld)) delta--;
                shapeKey = _rewiredPlayer.GetButtonDown(ShapeKeyBind);
                modeKey = _rewiredPlayer.GetButtonDown(ModeKeyBind);
                replace = _rewiredPlayer.GetButton(ReplaceKeyBind);
                rotate = manager.player.inputModule != null && manager.player.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.ROTATE);
            }

            var roof = (RoofMode)Mathf.Clamp(SizeMemory.Get(RoofModeKey, 0), 0, (int)RoofMode.ClearRoof);
            var bits = new InputBits { Present = true, Roof = roof, Size = InputBits.NoSize };

            if (ToolSizes.HasSize(slot))
            {
                bits.Size = ToolWish(world, em, player, slot, held, delta, rotate);
                if (slot == EquipmentSlotType.RoofingToolSlot && modeKey)
                {
                    roof = (RoofMode)(((int)roof + 1) % ((int)RoofMode.ClearRoof + 1));
                    SizeMemory.Set(RoofModeKey, (int)roof);
                    bits.Roof = roof;
                    ChatNote.Show($"Roofing gadget: {Brush.Label(roof)}");
                }
            }
            else if (slot == EquipmentSlotType.PlaceObjectSlot)
            {
                UpdateBrush(world, held, delta, shapeKey, modeKey, replace, ref bits);
            }

            Patches.SendInputPatch.Bits = bits.Encode();
        }

        // ------------------------------------------------------------------ tools

        /// <summary>
        /// The size this tool type should have: the remembered one (if any), moved by the size keys, and
        /// R mirrored the way vanilla cycles it so the saved value follows. NoSize = leave vanilla alone.
        /// </summary>
        private static int ToolWish(World world, EntityManager em, Entity player, EquipmentSlotType slot, EquippedObjectCD held, int delta, bool rotate)
        {
            bool known = SizeMemory.TryGet(slot, out int wish);
            if (delta == 0 && !rotate) return known ? Mathf.Clamp(wish, 0, ToolSizes.MaxVariation) : InputBits.NoSize;

            int ceiling = CeilingOf(world, held.containedObject.objectData);
            if (ceiling <= 1) return known ? Mathf.Clamp(wish, 0, ToolSizes.MaxVariation) : InputBits.NoSize;

            int current = em.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(player).GetElementForEquipment(slot).sizeVariationToPlace;
            int from = known ? wish : current == PlacementSizeByEquipmentTypeBuffer.UNINITIALIZED_SIZE ? ceiling - 1 : current;
            // The stored value can be above this item's ceiling (shared category); step from what is shown.
            int shown = ToolSizes.EdgeFor(from, ceiling) - 1;
            int next = rotate ? (shown + 1) % ceiling : Mathf.Clamp(shown + delta, 0, ceiling - 1);
            SizeMemory.Set(slot, next);
            return next;
        }

        private static int CeilingOf(World world, ObjectDataCD data)
        {
            var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            if (query.IsEmptyIgnoreFilter) return 0;
            var bank = query.GetSingleton<PugDatabase.DatabaseBankCD>();
            return PugDatabase.GetEntityObjectInfo(data.objectID, bank.databaseBankBlob, data.variation).prefabTileSize.x;
        }

        // ------------------------------------------------------------------ brush

        private static void UpdateBrush(World world, EquippedObjectCD held, int delta, bool shapeKey, bool modeKey, bool replace, ref InputBits bits)
        {
            var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            if (query.IsEmptyIgnoreFilter) return;
            var bank = query.GetSingleton<PugDatabase.DatabaseBankCD>();
            var data = held.containedObject.objectData;
            ref var info = ref PugDatabase.GetEntityObjectInfo(data.objectID, bank.databaseBankBlob, data.variation);
            if (!Brush.TryClassify(world.EntityManager, held.equipmentPrefab, ref info, out var category)) return;

            string shapeKeyName = $"Brush.{category}.shape", edgeKeyName = $"Brush.{category}.edge";
            var shape = (BrushShape)Mathf.Clamp(SizeMemory.Get(shapeKeyName, 0), 0, (int)BrushShape.Column);
            int edge = Mathf.Clamp(SizeMemory.Get(edgeKeyName, DefaultBrushEdge), 2, Brush.MaxEdge);
            bool on = shape != BrushShape.Off;

            if (shapeKey)
            {
                shape = (BrushShape)(((int)shape + 1) % ((int)BrushShape.Column + 1));
                SizeMemory.Set(shapeKeyName, (int)shape);
                ChatNote.Show($"{Brush.Label(category)} brush: {Brush.Label(shape, edge)}");
            }
            else if (delta > 0)
            {
                // From 1x1 (off), + starts a 2x2 square; otherwise it grows the current shape.
                if (!on) { shape = BrushShape.Square; edge = 2; }
                else edge = Mathf.Min(edge + 1, Brush.MaxEdge);
                SizeMemory.Set(shapeKeyName, (int)shape);
                SizeMemory.Set(edgeKeyName, edge);
            }
            else if (delta < 0 && on)
            {
                // 2x2 shrinks to 1x1, which is the brush switched off.
                if (edge <= 2) shape = BrushShape.Off;
                else edge--;
                SizeMemory.Set(shapeKeyName, (int)shape);
                SizeMemory.Set(edgeKeyName, edge);
            }

            var block = (BlockMode)Mathf.Clamp(SizeMemory.Get(BlockModeKey, 0), 0, (int)BlockMode.Wall);
            if (category == BrushCategory.Walls)
            {
                if (modeKey)
                {
                    block = (BlockMode)(((int)block + 1) % ((int)BlockMode.Wall + 1));
                    SizeMemory.Set(BlockModeKey, (int)block);
                    ChatNote.Show($"Blocks: {Brush.Label(block)}");
                }
                bits.Block = block;
            }

            bits.Shape = shape;
            bits.Size = Brush.IsOn(shape, edge) ? edge : 1;
            bits.Replace = replace;

            ClientBrush.Active = Brush.IsOn(shape, edge);
            ClientBrush.Prefab = held.equipmentPrefab;
            ClientBrush.Shape = shape;
            ClientBrush.Edge = edge;
        }

        // ------------------------------------------------------------------ input

        private static bool KeysAllowed()
        {
            if (_rewiredPlayer == null) return false;
            if (Manager.ui == null || Manager.menu == null || Manager.input == null) return false;
            if (Manager.ui.isAnyInventoryShowing || Manager.menu.IsAnyMenuActive() || Manager.input.textInputIsActive) return false;
            var input = Manager.input.singleplayerInputModule;
            return input != null && input.InputEnabled;
        }

        private static bool Pressed(string action, ref float heldFor)
        {
            if (_rewiredPlayer.GetButtonDown(action))
            {
                heldFor = -0.25f; // a short pause before repeating starts
                return true;
            }
            if (!_rewiredPlayer.GetButton(action)) return false;
            heldFor += Time.unscaledDeltaTime;
            if (heldFor < RepeatSeconds) return false;
            heldFor = 0f;
            return true;
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
    }
}
