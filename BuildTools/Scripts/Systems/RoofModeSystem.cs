using PlayerEquipment;
using Pug.UnityExtensions;
using PugTilemap;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace BuildTools.Systems
{
    /// <summary>
    /// Roofing gadget modes. Vanilla's ToggleRoof (Burst) adds roof holes to the covered cells, or, when
    /// every cell already has one, removes them all; it only queues TileUpdateBuffer commands, which are
    /// applied later in the tick (UpdateSubMapSystemServer / the client's predicted copy). This system
    /// runs right after the equipment update and corrects those queued commands:
    ///  - Add roof only: drop queued hole additions and remove the holes in the area instead.
    ///  - Remove roof only: drop queued hole removals.
    /// The mode comes from the player's input bits (<see cref="InputBits"/>), so the server and the
    /// client's prediction agree. On the client only the local player is handled.
    /// </summary>
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateAfter(typeof(EquipmentUpdateSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class RoofModeSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;
        private EntityQuery _tileUpdates;
        private EntityQuery _database;
        private TileAccessor _tileAccessor;
        private bool _hasAccessor;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(ComponentType.ReadOnly<EquipmentSlotCD>(), ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<PlacementCD>(), ComponentType.ReadOnly<PlacementSizeByEquipmentTypeBuffer>(),
                ComponentType.ReadOnly<ClientInput>());
            _tileUpdates = GetEntityQuery(ComponentType.ReadWrite<TileUpdateBuffer>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            if (_tileUpdates.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter) return;

            bool server = World.IsServer();
            Entity localPlayer = Entity.Null;
            if (!server)
            {
                var main = Manager.main;
                if (main == null || main.player == null) return;
                localPlayer = main.player.entity;
            }

            var players = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!server && player != localPlayer) continue;
                if (EntityManager.GetComponentData<EquipmentSlotCD>(player).slotType != EquipmentSlotType.RoofingToolSlot) continue;
                var bits = InputBits.Decode(EntityManager.GetComponentData<ClientInput>(player).buttonSetMask);
                if (!bits.Present || bits.Roof == RoofMode.Toggle) continue;
                Correct(player, bits.Roof);
            }
            players.Dispose();
        }

        private void Correct(Entity player, RoofMode mode)
        {
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            var data = EntityManager.GetComponentData<EquippedObjectCD>(player).containedObject.objectData;
            int ceiling = PugDatabase.GetEntityObjectInfo(data.objectID, bank.databaseBankBlob, data.variation).prefabTileSize.x;
            var sizes = EntityManager.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(player, true);
            if (sizes.Length < PlacementSizeByEquipmentTypeBuffer.EquipmentWithPlacementSize) return;
            int edge = ToolSizes.EdgeFor(sizes[3].sizeVariationToPlace, ceiling); // index 3 = RoofingToolSlot
            int3 corner = EntityManager.GetComponentData<PlacementCD>(player).bestPositionToPlaceAt;
            int2 min = new int2(corner.x, corner.z), max = min + edge - 1;

            var updates = EntityManager.GetBuffer<TileUpdateBuffer>(_tileUpdates.GetSingletonEntity());
            bool droppedAdds = false;
            for (int i = updates.Length - 1; i >= 0; i--)
            {
                var u = updates[i];
                if (u.tile.tileType != TileType.roofHole) continue;
                if (math.any(u.position < min) || math.any(u.position > max)) continue;

                if (mode == RoofMode.AddRoof && u.command == TileUpdateBuffer.Command.Add)
                {
                    updates.RemoveAt(i);
                    droppedAdds = true;
                }
                else if (mode == RoofMode.ClearRoof && u.command == TileUpdateBuffer.Command.Remove)
                {
                    updates.RemoveAt(i);
                }
            }

            // Vanilla wanted to cut holes (the area was partly roofed): fill the existing holes instead.
            if (!droppedAdds) return;
            if (!_hasAccessor)
            {
                _tileAccessor = new TileAccessor(this);
                _hasAccessor = true;
            }
            _tileAccessor.Update(this);
            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.y; z <= max.y; z++)
                {
                    var cell = new int2(x, z);
                    if (_tileAccessor.GetTop(cell).tileType.IsWallTile()) continue;
                    if (!_tileAccessor.HasType(cell, TileType.roofHole)) continue;
                    EntityUtility.RemoveTile(0, TileType.roofHole, cell, updates, _tileAccessor);
                }
            }
        }
    }
}
