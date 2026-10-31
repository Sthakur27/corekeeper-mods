using System.Collections.Generic;
using CommandMinion;
using ECS.Components.Interaction;
using Inventory;
using Pug.Automation;
using Pug.Properties;
using PlayerEquipment;
using PugTilemap;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace BuildTools.Systems
{
    /// <summary>
    /// The block brush, ground/wall mode and replace, driven by the player's input bits
    /// (<see cref="InputBits"/>), while right-click is held with a brush-able placeable
    /// (<see cref="Brush.TryClassify"/>).
    ///
    /// Objects (seeds, decorations): vanilla places the aimed cell; the server adds the other cells.
    /// Tiles (blocks, floors) with a brush, a non-toggle block mode or replace: Build Tools takes over the
    /// aimed cell too. Vanilla's queued tile add and item use for that cell are taken back out of the
    /// TileUpdateBuffer / InventoryChangeBuffer right after the equipment update (in both worlds, so the
    /// client's prediction does not flash the vanilla result), and the server places every cell itself.
    ///
    /// Free cells are checked with the game's own PlacementHandler.CanPlaceObjectAtPositionForSlotType
    /// (reach, line of sight, blocking objects, tilled ground, immune tiles), with lookups built here
    /// the way EquipmentUpdateSystem builds them; placement mirrors PlaceObjectSlot.PlaceItem
    /// (EntityUtility.AddTile + a destroying ConsumeEntityAt; objects through a spawning ConsumeEntityAt,
    /// which adds DestroyEntityIfPlacementNotValidCD so a bad spot drops the item instead of losing it).
    /// Replace swaps a different block of the same layer for the held one and gives the old one back;
    /// outside creative it needs a pickaxe (walls) or shovel (floors) in the inventory and never touches
    /// obsidian or immune tiles.
    /// </summary>
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateAfter(typeof(EquipmentUpdateSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class BrushPlaceSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 0.25f;
        private const float ReplaceReach = 10f;

        private struct Job
        {
            public Entity player;
            public InputBits bits;
            public BrushCategory category;
            public bool takeover;
        }

        private EntityQuery _players;
        private EntityQuery _networkTime, _database, _worldInfo, _physics, _physicsHistory;
        private EntityQuery _inventoryChanges, _tileUpdates, _tileMap, _colliderCache;
        private EquipmentUpdateAspect.Lookup _aspects;
        private TileAccessor _tileAccessor;
        private bool _ready;
        private uint _tickRate;
        private readonly Dictionary<Entity, uint> _nextTick = new Dictionary<Entity, uint>();
        private readonly List<Job> _jobs = new List<Job>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(ComponentType.ReadOnly<EquipmentSlotCD>(), ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<PlacementCD>(), ComponentType.ReadOnly<ClientInput>());
            _networkTime = GetEntityQuery(ComponentType.ReadOnly<NetworkTime>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _worldInfo = GetEntityQuery(ComponentType.ReadOnly<WorldInfoCD>());
            _physics = GetEntityQuery(ComponentType.ReadOnly<PhysicsWorldSingleton>());
            _physicsHistory = GetEntityQuery(ComponentType.ReadOnly<PhysicsWorldHistorySingleton>());
            _inventoryChanges = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            _tileUpdates = GetEntityQuery(ComponentType.ReadWrite<TileUpdateBuffer>());
            _tileMap = GetEntityQuery(ComponentType.ReadOnly<TileWithTilesetToObjectDataMapCD>());
            _colliderCache = GetEntityQuery(ComponentType.ReadOnly<ColliderCacheCD>());
            _tickRate = (uint)PlatformConfiguration.Instance.SessionConfiguration.SimulationTickRate;
        }

        protected override void OnUpdate()
        {
            if (_networkTime.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter || _tileUpdates.IsEmptyIgnoreFilter) return;
            var tick = _networkTime.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid) return;
            uint now = tick.TickIndexForValidTick;

            bool server = World.IsServer();
            Entity localPlayer = Entity.Null;
            if (!server)
            {
                var main = Manager.main;
                if (main == null || main.player == null) return;
                localPlayer = main.player.entity;
            }

            var blob = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            _jobs.Clear();
            var players = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!server && player != localPlayer) continue;
                var input = EntityManager.GetComponentData<ClientInput>(player);
                var bits = InputBits.Decode(input.buttonSetMask);
                if (!bits.Present) continue;
                var slot = EntityManager.GetComponentData<EquipmentSlotCD>(player);
                if (slot.slotType != EquipmentSlotType.PlaceObjectSlot) continue;

                var held = EntityManager.GetComponentData<EquippedObjectCD>(player);
                var data = held.containedObject.objectData;
                if (data.objectID == ObjectID.None) continue;
                ref var info = ref PugDatabase.GetEntityObjectInfo(data.objectID, blob, data.variation);
                if (!Brush.TryClassify(EntityManager, held.equipmentPrefab, ref info, out var category)) continue;

                bool isTile = EntityManager.HasComponent<TileCD>(held.equipmentPrefab);
                bool brushOn = Brush.IsOn(bits.Shape, bits.Size);
                bool takeover = isTile && (brushOn || bits.Replace || (category == BrushCategory.Walls && bits.Block != BlockMode.Toggle));
                if (takeover) StripVanillaAnchor(player, held, ref info);
                if (!server || (!takeover && !brushOn)) continue;

                if (slot.secondInteractBlockedUntilRelease || !input.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown)) continue;
                if (_nextTick.TryGetValue(player, out uint next) && (int)(now - next) < 0) continue;
                _jobs.Add(new Job { player = player, bits = bits, category = category, takeover = takeover });
            }
            players.Dispose();
            if (_jobs.Count == 0) return;
            if (_worldInfo.IsEmptyIgnoreFilter || _physics.IsEmptyIgnoreFilter || _physicsHistory.IsEmptyIgnoreFilter ||
                _inventoryChanges.IsEmptyIgnoreFilter || _tileMap.IsEmptyIgnoreFilter || _colliderCache.IsEmptyIgnoreFilter) return;

            EntityManager.CompleteAllTrackedJobs();
            if (!_ready)
            {
                _aspects = new EquipmentUpdateAspect.Lookup(ref CheckedStateRef);
                _tileAccessor = new TileAccessor(this);
                _ready = true;
            }
            _aspects.Update(ref CheckedStateRef);
            _tileAccessor.Update(this);

            var shared = new EquipmentUpdateSharedData
            {
                currentTick = tick,
                databaseBank = _database.GetSingleton<PugDatabase.DatabaseBankCD>(),
                worldInfoCD = _worldInfo.GetSingleton<WorldInfoCD>(),
                tickRate = _tickRate,
                physicsWorld = _physics.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                physicsWorldHistory = _physicsHistory.GetSingleton<PhysicsWorldHistorySingleton>(),
                inventoryUpdateBufferEntity = _inventoryChanges.GetSingletonEntity(),
                tileUpdateBufferEntity = _tileUpdates.GetSingletonEntity(),
                tileAccessor = _tileAccessor,
                tileWithTilesetToObjectDataMapCD = _tileMap.GetSingleton<TileWithTilesetToObjectDataMapCD>(),
                colliderCacheCD = _colliderCache.GetSingleton<ColliderCacheCD>(),
                isServer = true,
                isFirstTimeFullyPredictingTick = true,
            };
            var lookups = BuildLookups();

            foreach (var job in _jobs)
            {
                int placed = PlaceBrush(job, in shared, in lookups);
                if (placed > 0) _nextTick[job.player] = now + (uint)math.max(1, (int)(_tickRate * IntervalSeconds));
            }
        }

        /// <summary>Takes vanilla's own placement of the aimed cell (tile add + item use) back out of this tick's queues.</summary>
        private void StripVanillaAnchor(Entity player, EquippedObjectCD held, ref PugDatabase.EntityObjectInfo info)
        {
            int3 anchor = EntityManager.GetComponentData<PlacementCD>(player).bestPositionToPlaceAt;
            var anchor2 = new int2(anchor.x, anchor.z);
            var updates = EntityManager.GetBuffer<TileUpdateBuffer>(_tileUpdates.GetSingletonEntity());
            bool removed = false;
            for (int i = updates.Length - 1; i >= 0; i--)
            {
                var u = updates[i];
                if (u.command != TileUpdateBuffer.Command.Add || !u.position.Equals(anchor2) || u.tile.tileset != info.tileset) continue;
                if (u.tile.tileType != info.tileType && u.tile.tileType != TileType.ground) continue;
                updates.RemoveAt(i);
                removed = true;
            }
            if (!removed || _inventoryChanges.IsEmptyIgnoreFilter) return;

            var changes = EntityManager.GetBuffer<InventoryChangeBuffer>(_inventoryChanges.GetSingletonEntity());
            for (int i = changes.Length - 1; i >= 0; i--)
            {
                var c = changes[i].inventoryChangeData;
                if (changes[i].playerEntity != player || c.inventoryAction != InventoryAction.ConsumeEntityAt) continue;
                if (c.inventory1 != player || c.index1 != held.equippedSlotIndex || !c.bool1) continue;
                changes.RemoveAt(i);
                return;
            }
        }

        private int PlaceBrush(Job job, in EquipmentUpdateSharedData shared, in LookupEquipmentUpdateData lookups)
        {
            var player = job.player;
            var held = EntityManager.GetComponentData<EquippedObjectCD>(player);
            var data = held.containedObject.objectData;
            var prefab = held.equipmentPrefab;
            if (held.isBroken || data.objectID == ObjectID.None || data.amount <= 0) return 0;

            var blob = shared.databaseBank.databaseBankBlob;
            ref var info = ref PugDatabase.GetEntityObjectInfo(data.objectID, blob, data.variation);
            bool isTile = EntityManager.HasComponent<TileCD>(prefab);

            bool godMode = EntityManager.HasComponent<GodModeCD>(player) && EntityManager.IsComponentEnabled<GodModeCD>(player);
            bool creativeWorld = shared.worldInfoCD.IsWorldModeEnabled(WorldMode.Creative);
            // Without the takeover, vanilla places (and pays for) the aimed cell this tick.
            int budget = godMode ? int.MaxValue : data.amount - (job.takeover ? 0 : 1);
            if (budget <= 0) return 0;

            var placementBackup = EntityManager.GetComponentData<PlacementCD>(player);
            int3 anchor = placementBackup.bestPositionToPlaceAt;
            int variation = data.variation;
            if (PlacementHandler.ObjectCanBeRotated(prefab, lookups.directionBasedOnVariationLookup, lookups.objectPropertiesLookup, lookups.directionLookup))
                variation = placementBackup.rotationVariationToPlace;

            float3 playerPos = EntityManager.GetComponentData<LocalTransform>(player).Position;
            var aspect = _aspects[player];
            var tilesChecked = new NativeHashMap<int3, bool>(16, Allocator.Temp);
            var found = new NativeList<PlacementHandler.EntityAndInfoFromPlacement>(Allocator.Temp);
            var inventoryChanges = EntityManager.GetBuffer<InventoryChangeBuffer>(shared.inventoryUpdateBufferEntity);
            var tileUpdates = EntityManager.GetBuffer<TileUpdateBuffer>(shared.tileUpdateBufferEntity);
            bool free = godMode || creativeWorld;
            bool hasPickaxe = free || HasToolType(player, blob, ObjectType.MiningPick);
            bool hasShovel = free || HasToolType(player, blob, ObjectType.Shovel);

            var cells = Brush.Cells(anchor, job.bits.Shape, job.bits.Size);
            bool groundPass = isTile && ToggleNeedsGround(cells, job, ref info, in shared);

            int placed = 0;
            foreach (var cell in cells)
            {
                if (placed >= budget) break;
                bool isAnchor = cell.x == anchor.x && cell.z == anchor.z;
                if (isAnchor && !job.takeover) continue;

                if (!isTile)
                {
                    if (!CanPlace(prefab, cell, in aspect, in shared, in lookups, tilesChecked, ref found)) continue;
                    inventoryChanges.Add(new InventoryChangeBuffer
                    {
                        inventoryChangeData = Create.ConsumeEntityAt(player, held.equippedSlotIndex, 1, destroy: false, godMode,
                            new float3(cell.x, 0f, cell.z), variation, default, data.objectID),
                        playerEntity = player,
                    });
                    placed++;
                    continue;
                }

                var pos = new int2(cell.x, cell.z);
                if (!LayerFor(pos, job, groundPass, ref info, in shared, out var layer)) continue;

                if (_tileAccessor.GetType(pos, layer, out var old))
                {
                    if (old.tileset == info.tileset || !job.bits.Replace) continue;
                    if (!CanReplace(pos, layer, old, playerPos, creativeWorld, hasPickaxe, hasShovel, blob, out var oldItem)) continue;

                    EntityUtility.RemoveTile(old.tileset, layer, pos, tileUpdates, _tileAccessor);
                    EntityUtility.AddTile(info.tileset, layer, pos, creativeWorld, tileUpdates);
                    if (!godMode && oldItem.objectID != ObjectID.None)
                        inventoryChanges.Add(new InventoryChangeBuffer
                        {
                            inventoryChangeData = Create.CreateItem(player, 0, oldItem.objectID, 1, playerPos, oldItem.variation),
                            playerEntity = player,
                        });
                }
                else
                {
                    if (!CanPlace(prefab, cell, in aspect, in shared, in lookups, tilesChecked, ref found)) continue;
                    // The vanilla check is for the item's own layer; the floor version must not land on existing floor.
                    if (layer == TileType.ground && _tileAccessor.HasType(pos, TileType.ground)) continue;
                    EntityUtility.AddTile(info.tileset, layer, pos, creativeWorld, tileUpdates);
                }

                inventoryChanges.Add(new InventoryChangeBuffer
                {
                    inventoryChangeData = Create.ConsumeEntityAt(player, held.equippedSlotIndex, 1, destroy: true, godMode, playerPos, data.variation),
                    playerEntity = player,
                });
                placed++;
            }

            tilesChecked.Dispose();
            found.Dispose();
            // The vanilla check writes a few (replicated) PlacementCD fields; put them back.
            EntityManager.SetComponentData(player, placementBackup);
            return placed;
        }

        private static bool CanPlace(Entity prefab, int3 cell, in EquipmentUpdateAspect aspect, in EquipmentUpdateSharedData shared,
            in LookupEquipmentUpdateData lookups, NativeHashMap<int3, bool> tilesChecked, ref NativeList<PlacementHandler.EntityAndInfoFromPlacement> found)
        {
            tilesChecked.Clear();
            found.Clear();
            return PlacementHandler.CanPlaceObjectAtPositionForSlotType(prefab, cell, 1, 1, tilesChecked, ref found, in aspect, in shared, in lookups) > 0;
        }

        /// <summary>
        /// Toggle mode works on the whole brush at once, like Placement Plus: while any cell still lacks
        /// floor, a pass only lays floor; once the area is floored, the next pass builds walls. (Vanilla's
        /// per-cell rule would mix floor and walls in one swipe.) Not with replace, which follows each
        /// cell's existing layer.
        /// </summary>
        private bool ToggleNeedsGround(System.Collections.Generic.List<int3> cells, Job job, ref PugDatabase.EntityObjectInfo info, in EquipmentUpdateSharedData shared)
        {
            if (job.category != BrushCategory.Walls || info.tileType != TileType.wall) return false;
            if (job.bits.Block != BlockMode.Toggle || job.bits.Replace) return false;
            if (PugDatabase.TryGetTileItemInfo(TileType.ground, (Tileset)info.tileset, in shared.tileWithTilesetToObjectDataMapCD).objectID == ObjectID.None) return false;
            foreach (var cell in cells)
            {
                var pos = new int2(cell.x, cell.z);
                if (!_tileAccessor.HasType(pos, TileType.ground) && !_tileAccessor.HasType(pos, TileType.bridge) && !_tileAccessor.HasType(pos, TileType.wall))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Which tile layer a cell gets. Wall blocks follow the block mode: toggle = floor pass or wall pass
        /// for the whole brush (<see cref="ToggleNeedsGround"/>; with replace, the layer that is there),
        /// ground = the block's floor version, wall = walls only, never on bare ground. Every other tile
        /// item places its own layer.
        /// </summary>
        private bool LayerFor(int2 pos, Job job, bool groundPass, ref PugDatabase.EntityObjectInfo info, in EquipmentUpdateSharedData shared, out TileType layer)
        {
            layer = info.tileType;
            if (job.category != BrushCategory.Walls || info.tileType != TileType.wall) return true;

            bool groundVersion = PugDatabase.TryGetTileItemInfo(TileType.ground, (Tileset)info.tileset, in shared.tileWithTilesetToObjectDataMapCD).objectID != ObjectID.None;
            bool hasFloor = _tileAccessor.HasType(pos, TileType.ground) || _tileAccessor.HasType(pos, TileType.bridge);
            switch (job.bits.Block)
            {
                case BlockMode.Ground:
                    if (!groundVersion) return false;
                    layer = TileType.ground;
                    return true;
                case BlockMode.Wall:
                    return hasFloor;
                default:
                    if (job.bits.Replace)
                    {
                        if (!_tileAccessor.HasType(pos, TileType.wall) && _tileAccessor.HasType(pos, TileType.ground) && groundVersion)
                            layer = TileType.ground;
                        else if (!hasFloor && groundVersion)
                            layer = TileType.ground;
                        return true;
                    }
                    if (groundPass)
                    {
                        layer = TileType.ground;
                        return true;
                    }
                    return hasFloor || !groundVersion;
            }
        }

        private bool CanReplace(int2 pos, TileType layer, TileCD old, float3 playerPos, bool creative, bool hasPickaxe, bool hasShovel,
            BlobAssetReference<PugDatabase.PugDatabaseBank> blob, out ObjectDataCD oldItem)
        {
            oldItem = PugDatabase.GetObjectData(old.tileset, layer, blob);
            if (math.distance(playerPos.xz, new float2(pos.x, pos.y)) > ReplaceReach) return false;
            if (_tileAccessor.HasType(pos, TileType.immune)) return false;
            if (!creative && (oldItem.objectID == ObjectID.WallObsidianBlock || oldItem.objectID == ObjectID.GroundObsidianBlock)) return false;
            if (layer == TileType.wall) return hasPickaxe;
            // Taking floor out from under a wall would remove the wall with it.
            if (_tileAccessor.HasType(pos, TileType.wall)) return false;
            return hasShovel;
        }

        private bool HasToolType(Entity player, BlobAssetReference<PugDatabase.PugDatabaseBank> blob, ObjectType type)
        {
            if (!EntityManager.HasBuffer<ContainedObjectsBuffer>(player)) return false;
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(player, true);
            for (int i = 0; i < items.Length; i++)
            {
                var d = items[i].objectData;
                if (d.objectID == ObjectID.None) continue;
                if (PugDatabase.GetEntityObjectInfo(d.objectID, blob, d.variation).objectType == type) return true;
            }
            return false;
        }

        private LookupEquipmentUpdateData BuildLookups()
        {
            return new LookupEquipmentUpdateData
            {
                secondaryUseLookup = GetComponentLookup<SecondaryUseCD>(true),
                cooldownLookup = GetComponentLookup<CooldownCD>(true),
                warmupLookup = GetComponentLookup<WarmupCD>(true),
                consumeManaLookup = GetComponentLookup<ConsumesManaCD>(true),
                levelLookup = GetComponentLookup<LevelCD>(true),
                levelEntitiesLookup = GetBufferLookup<LevelEntitiesBuffer>(true),
                parchementRecipeLookup = GetComponentLookup<RecipeCD>(true),
                objectDataLookup = GetComponentLookup<ObjectDataCD>(true),
                attackWithEquipmentLookup = GetComponentLookup<AttackWithEquipmentTag>(true),
                inventoryUpdateBuffer = GetBufferLookup<InventoryChangeBuffer>(true),
                cattleLookup = GetComponentLookup<CattleCD>(true),
                petCandyLookup = GetComponentLookup<PetCandyCD>(true),
                potionLookup = GetComponentLookup<PotionCD>(true),
                localTransformLookup = GetComponentLookup<LocalTransform>(true),
                petLookup = GetComponentLookup<PetCD>(true),
                playAnimationStateLookup = GetComponentLookup<PlayAnimationStateCD>(true),
                simulateLookup = GetComponentLookup<Simulate>(true),
                waitingForEatableSlotConsumeResultLookup = GetComponentLookup<WaitingForEatableSlotConsumeResultCD>(true),
                tileUpdateBufferLookup = GetBufferLookup<TileUpdateBuffer>(true),
                tileLookup = GetComponentLookup<TileCD>(true),
                objectPropertiesLookup = GetComponentLookup<ObjectPropertiesCD>(true),
                adaptiveEntityBufferLookup = GetBufferLookup<AdaptiveEntityBuffer>(true),
                directionBasedOnVariationLookup = GetComponentLookup<DirectionBasedOnVariationCD>(true),
                directionLookup = GetComponentLookup<DirectionCD>(true),
                sizeVariationLookup = GetComponentLookup<ResizableTileSizeCD>(true),
                playerGhostLookup = GetComponentLookup<PlayerGhost>(true),
                minionLookup = GetComponentLookup<MinionCD>(true),
                indestructibleLookup = GetComponentLookup<IndestructibleCD>(true),
                plantLookup = GetComponentLookup<PlantCD>(true),
                critterLookup = GetComponentLookup<CritterCD>(true),
                fireflyLookup = GetComponentLookup<FireflyCD>(true),
                requiresDrillLookup = GetComponentLookup<RequiresDrillCD>(true),
                surfacePriorityLookup = GetComponentLookup<SurfacePriorityCD>(true),
                electricityLookup = GetComponentLookup<ElectricityCD>(true),
                eventTerminalLookup = GetComponentLookup<EventTerminalCD>(true),
                waterSourceLookup = GetComponentLookup<WaterSourceCD>(true),
                paintToolLookup = GetComponentLookup<PaintToolCD>(true),
                paintableObjectLookup = GetComponentLookup<PaintableObjectCD>(true),
                growingLookup = GetComponentLookup<GrowingCD>(true),
                healthLookup = GetComponentLookup<HealthCD>(true),
                summarizedConditionsBufferLookup = GetBufferLookup<SummarizedConditionsBuffer>(true),
                reduceDurabilityOfEquippedTagLookup = GetComponentLookup<ReduceDurabilityOfEquippedTriggerCD>(true),
                summarizedConditionEffectsBufferLookup = GetBufferLookup<SummarizedConditionEffectsBuffer>(true),
                entityDestroyedLookup = GetComponentLookup<EntityDestroyedCD>(true),
                dontDropSelfLookup = GetComponentLookup<DontDropSelfCD>(true),
                dontDropLootLookup = GetComponentLookup<DontDropLootCD>(true),
                killedByPlayerLookup = GetComponentLookup<KilledByPlayerCD>(true),
                destructibleLookup = GetComponentLookup<DestructibleObjectCD>(true),
                canBeRemovedByWaterLookup = GetComponentLookup<CanBeRemovedByWaterCD>(true),
                groundDecorationLookup = GetComponentLookup<GroundDecorationCD>(true),
                diggableLookup = GetComponentLookup<DiggableCD>(true),
                pseudoTileLookup = GetComponentLookup<PseudoTileCD>(true),
                dontBlockDiggingLookup = GetComponentLookup<DontBlockDiggingCD>(true),
                fullnessLookup = GetComponentLookup<FullnessCD>(true),
                godModeLookup = GetComponentLookup<GodModeCD>(true),
                containedObjectsBufferLookup = GetBufferLookup<ContainedObjectsBuffer>(true),
                inventoryBufferLookup = GetBufferLookup<InventoryBuffer>(true),
                anvilLookup = GetComponentLookup<AnvilCD>(true),
                waypointLookup = GetComponentLookup<WayPointCD>(true),
                craftingLookup = GetComponentLookup<CraftingCD>(true),
                proximityTriggerLookup = GetComponentLookup<ProximityTriggerCD>(true),
                commandMinionLookup = GetComponentLookup<CommandMinionWeaponCD>(true),
                rootPlantLookup = GetComponentLookup<RootPlantCD>(true),
                mountLookup = GetComponentLookup<MountCD>(true),
                triggerSelectNewEnemyToAttackCommandLookup = GetComponentLookup<TriggerSelectEnemyToAttackForMinionCommandCD>(true),
                triggerAnimationOnDeathLookup = GetComponentLookup<TriggerAnimationOnDeathCD>(true),
                moveToPredictedByEntityDestroyedLookup = GetComponentLookup<MoveToPredictedByEntityDestroyedCD>(true),
                hasExplodedLookup = GetComponentLookup<HasExplodedCD>(true),
                doorLookup = GetComponentLookup<DoorCD>(true),
                affectObjectWhenMelodyPlayedLookup = GetComponentLookup<AffectObjectWhenMelodyPlayedCD>(true),
                blockInteractionLookup = GetComponentLookup<BlockInteractionCD>(true),
            };
        }
    }
}
