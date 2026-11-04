using System.Collections.Generic;
using Inventory;
using PlayerEquipment;
using PugTilemap;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace DrillUnit.Systems
{
    /// <summary>
    /// Lay-ground mode (server): while a Drill Unit rider has it on (<see cref="InputBits.GroundBit"/>), the
    /// water, lava and pit cells within <see cref="Radius"/> of the rider (a 3x3 area) become floor. The floor is
    /// the ground version of the first
    /// block in the rider's inventory that has one (Dirt -> dirt floor), placed the way vanilla places a
    /// floor (EntityUtility.AddTile, which also removes the water/pit) and paid for with one block per cell.
    /// Creative worlds and god mode place for free. Tileset 2 and the Core cells are skipped, as AddTile
    /// would drop them silently.
    /// </summary>
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateAfter(typeof(EquipmentUpdateSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class LayGroundSystem : PugSimulationSystemBase
    {
        private EntityQuery _units, _database, _worldInfo, _inventoryChanges, _tileUpdates, _tileMap;
        private TileAccessor _tileAccessor;
        private bool _ready;
        public const int Radius = 1;
        private const double RecentSeconds = 1.0; // a queued floor shows up a frame later: don't pay twice
        private readonly Dictionary<Entity, int2> _lastCell = new Dictionary<Entity, int2>();
        private readonly Dictionary<int2, double> _recent = new Dictionary<int2, double>();
        private readonly Dictionary<int, int> _used = new Dictionary<int, int>();
        private readonly List<int2> _expired = new List<int2>();
        private readonly List<(Entity rider, int2 cell)> _jobs = new List<(Entity, int2)>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _units = GetEntityQuery(ComponentType.ReadOnly<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<ControlledByOtherEntityCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _worldInfo = GetEntityQuery(ComponentType.ReadOnly<WorldInfoCD>());
            _inventoryChanges = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            _tileUpdates = GetEntityQuery(ComponentType.ReadWrite<TileUpdateBuffer>());
            _tileMap = GetEntityQuery(ComponentType.ReadOnly<TileWithTilesetToObjectDataMapCD>());
            RequireForUpdate(_units);
        }

        protected override void OnUpdate()
        {
            var id = DrillUnitMod.Id;
            if (id == ObjectID.None) return;
            _jobs.Clear();
            var units = _units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (EntityManager.GetComponentData<ObjectDataCD>(unit).objectID != id) continue;
                var rider = EntityManager.GetComponentData<ControlledByOtherEntityCD>(unit).controlledByEntity;
                if (!DrillUnitMod.IsRiding(EntityManager, rider, out var ridden) || ridden != unit) continue;
                if (!EntityManager.HasComponent<ClientInput>(rider)) continue;
                if ((EntityManager.GetComponentData<ClientInput>(rider).buttonSetMask & InputBits.GroundBit) == 0)
                {
                    _lastCell.Remove(rider);
                    continue;
                }
                float3 p = EntityManager.GetComponentData<LocalTransform>(rider).Position;
                var cell = new int2((int)math.round(p.x), (int)math.round(p.z));
                if (_lastCell.TryGetValue(rider, out var last) && last.Equals(cell)) continue;
                _jobs.Add((rider, cell));
            }
            units.Dispose();
            if (_jobs.Count == 0 || _database.IsEmptyIgnoreFilter || _worldInfo.IsEmptyIgnoreFilter
                || _inventoryChanges.IsEmptyIgnoreFilter || _tileUpdates.IsEmptyIgnoreFilter || _tileMap.IsEmptyIgnoreFilter) return;

            EntityManager.CompleteAllTrackedJobs();
            if (!_ready)
            {
                _tileAccessor = new TileAccessor(this);
                _ready = true;
            }
            _tileAccessor.Update(this);

            var blob = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            var map = _tileMap.GetSingleton<TileWithTilesetToObjectDataMapCD>();
            bool creative = _worldInfo.GetSingleton<WorldInfoCD>().IsWorldModeEnabled(WorldMode.Creative);
            var tileUpdates = EntityManager.GetBuffer<TileUpdateBuffer>(_tileUpdates.GetSingletonEntity());
            var changes = EntityManager.GetBuffer<InventoryChangeBuffer>(_inventoryChanges.GetSingletonEntity());

            double now = World.Time.ElapsedTime;
            _expired.Clear();
            foreach (var kv in _recent) if (now > kv.Value) _expired.Add(kv.Key);
            foreach (var c in _expired) _recent.Remove(c);

            foreach (var (rider, centre) in _jobs)
            {
                bool god = EntityManager.HasComponent<GodModeCD>(rider) && EntityManager.IsComponentEnabled<GodModeCD>(rider);
                float3 pos = EntityManager.GetComponentData<LocalTransform>(rider).Position;
                bool outOfBlocks = false;
                _used.Clear();
                for (int dx = -Radius; dx <= Radius && !outOfBlocks; dx++)
                    for (int dy = -Radius; dy <= Radius; dy++)
                    {
                        var cell = centre + new int2(dx, dy);
                        if (_recent.ContainsKey(cell) || !NeedsFloor(cell)) continue;
                        if (!FindBlock(rider, blob, in map, out int slot, out int tileset, out int variation)) { outOfBlocks = true; break; }
                        EntityUtility.AddTile(tileset, TileType.ground, cell, creative, tileUpdates);
                        changes.Add(new InventoryChangeBuffer
                        {
                            inventoryChangeData = Create.ConsumeEntityAt(rider, slot, 1, destroy: true, god || creative, pos, variation),
                            playerEntity = rider,
                        });
                        if (!(god || creative)) _used[slot] = _used.TryGetValue(slot, out int n) ? n + 1 : 1;
                        _recent[cell] = now + RecentSeconds;
                    }
                if (!outOfBlocks) _lastCell[rider] = centre; // else retried when blocks show up
            }
        }

        private bool NeedsFloor(int2 cell)
        {
            if (IsCoreCell(cell)) return false;
            return _tileAccessor.HasType(cell, TileType.water) || _tileAccessor.HasType(cell, TileType.pit);
        }

        private static bool IsCoreCell(int2 p)
        {
            return (p.x == 0 && p.y == 0) || (p.x == 0 && p.y == 1) || (p.x == -1 && p.y == 1) || (p.x == 1 && p.y == 1);
        }

        /// <summary>First inventory stack whose tileset has a floor item: a wall block or a floor tile.</summary>
        private bool FindBlock(Entity rider, BlobAssetReference<PugDatabase.PugDatabaseBank> blob,
            in TileWithTilesetToObjectDataMapCD map, out int slot, out int tileset, out int variation)
        {
            slot = -1; tileset = 0; variation = 0;
            if (!EntityManager.HasBuffer<ContainedObjectsBuffer>(rider)) return false;
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(rider, true);
            for (int i = 0; i < items.Length; i++)
            {
                var d = items[i].objectData;
                if (d.objectID == ObjectID.None || d.amount - (_used.TryGetValue(i, out int used) ? used : 0) <= 0) continue;
                ref var info = ref PugDatabase.GetEntityObjectInfo(d.objectID, blob, d.variation);
                if (info.tileType != TileType.wall && info.tileType != TileType.ground) continue;
                if (info.tileset == 2) continue;
                if (PugDatabase.TryGetTileItemInfo(TileType.ground, (Tileset)info.tileset, in map).objectID == ObjectID.None) continue;
                slot = i; tileset = info.tileset; variation = d.variation;
                return true;
            }
            return false;
        }
    }
}
