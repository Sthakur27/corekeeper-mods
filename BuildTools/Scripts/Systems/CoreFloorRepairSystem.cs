using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace BuildTools.Systems
{
    /// <summary>
    /// Repairs worlds hit by the Build Tools 0.6.3 Swap bug: Swap took the floor out from under the four
    /// protected Core cells (BrushPlaceSystem.IsCoreCell) and the game then refused the new one, leaving no
    /// floor in front of the Core. Once per world, on the server, any of those cells left without floor, or with
    /// the pit that removing it leaves behind, gets the Core's alien floor back and the pit removed. The tile is queued directly: the game's own AddTile refuses
    /// these cells outside creative, which is exactly why they ended up empty.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class CoreFloorRepairSystem : PugSimulationSystemBase
    {
        private static readonly int2[] CoreCells = { new int2(0, 0), new int2(-1, 1), new int2(0, 1), new int2(1, 1) };

        private EntityQuery _tileUpdates;
        private EntityQuery _database;
        private TileAccessor _tiles;
        private bool _ready, _done;

        protected override void OnCreate()
        {
            base.OnCreate();
            _tileUpdates = GetEntityQuery(ComponentType.ReadWrite<TileUpdateBuffer>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            if (_done || _tileUpdates.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter) return;
            if (!_ready)
            {
                _tiles = new TileAccessor(this);
                _ready = true;
            }
            _tiles.Update(this);

            foreach (var cell in CoreCells)
                if (!_tiles.IsInitialized(cell)) return; // map around the Core not loaded yet

            _done = true;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            int alien = PugDatabase.GetEntityObjectInfo(ObjectID.GroundAlienBlock, bank.databaseBankBlob).tileset;
            var updates = EntityManager.GetBuffer<TileUpdateBuffer>(_tileUpdates.GetSingletonEntity());
            foreach (var cell in CoreCells)
            {
                if (_tiles.HasType(cell, TileType.wall) || _tiles.HasType(cell, TileType.bridge)) continue;
                bool hasGround = _tiles.HasType(cell, TileType.ground);
                bool pit = _tiles.HasType(cell, TileType.pit), water = _tiles.HasType(cell, TileType.water);
                if (hasGround && !pit && !water) continue;

                // Taking the floor away leaves a pit (that is how dug-out ground works), and the pit is what
                // blocks the spot in front of the Core; a floor under it is not enough.
                if (!hasGround)
                    updates.Add(new TileUpdateBuffer
                    {
                        command = TileUpdateBuffer.Command.Add,
                        position = cell,
                        tile = new TileCD { tileset = alien, tileType = TileType.ground },
                    });
                if (pit)
                    updates.Add(new TileUpdateBuffer { command = TileUpdateBuffer.Command.Remove, position = cell, tile = new TileCD { tileType = TileType.pit } });
                if (water)
                    updates.Add(new TileUpdateBuffer { command = TileUpdateBuffer.Command.Remove, position = cell, tile = new TileCD { tileType = TileType.water } });
                Debug.Log($"[{BuildToolsMod.Name}] Repaired the Core floor at {cell.x},{cell.y} (floor added: {!hasGround}, pit removed: {pit}, water removed: {water}).");
            }
        }
    }
}
