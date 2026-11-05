using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace DrillUnit.Systems
{
    /// <summary>
    /// Dig mode (server): while a Drill Unit rider in dig mode holds the use button (<see cref="InputBits.Digging"/>), the 3x3 cells
    /// around the rider are dug like a shovel does it: a TileDamageBuffer hit with canHitGround, so a placed
    /// floor, rug or rail comes up first, then the ground itself, leaving a pit (water next to it flows in).
    /// Drops are pulled to the rider. Pits, water, fences and bridges are left alone, immune tiles stay immune.
    /// The tiles a placed object stands on (its whole footprint: chest, table, plant, ...) are skipped, as losing
    /// their ground would pop it back into an item; creatures, critters and pets walking about don't count. That
    /// check scans placed objects, so it is cached per rider and redone only when the rider enters another
    /// cell or after <see cref="RescanSeconds"/> (objects placed or removed meanwhile), and only while digging.
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(TileDamageSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class DigGroundSystem : PugSimulationSystemBase
    {
        private const int Damage = 400;
        private const double IntervalSeconds = 0.15;
        private const float Reach = 4f; // objects further than this from the rider can't cover the 3x3 (largest footprints ~3x3)
        private const double RescanSeconds = 1.0;

        private EntityQuery _units, _tileDamage, _placed, _database;
        private readonly Dictionary<Entity, double> _next = new Dictionary<Entity, double>();
        private sealed class Cover
        {
            public int2 Cell;
            public double Until = double.MinValue;
            public readonly HashSet<int2> Cells = new HashSet<int2>();
        }

        private readonly Dictionary<Entity, Cover> _covers = new Dictionary<Entity, Cover>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _units = RiddenUnits.Query(this);
            _tileDamage = GetEntityQuery(ComponentType.ReadWrite<TileDamageBuffer>());
            _placed = GetEntityQuery(ComponentType.ReadOnly<DestroyEntityIfNotOnTileCD>(), ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ObjectDataCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            RequireForUpdate(_units);
        }

        protected override void OnUpdate()
        {
            var ridden = RiddenUnits.Get(this, _units);
            if (ridden.Count == 0 || _tileDamage.IsEmptyIgnoreFilter || _database.IsEmptyIgnoreFilter) return;
            double now = World.Time.ElapsedTime;
            foreach (var (unit, rider) in ridden)
            {
                if (!EntityManager.HasComponent<ClientInput>(rider)) continue;
                if (!InputBits.Digging(EntityManager.GetComponentData<ClientInput>(rider).buttonSetMask))
                {
                    _covers.Remove(rider);
                    continue;
                }
                if (_next.TryGetValue(rider, out double next) && now < next) continue;
                _next[rider] = now + IntervalSeconds;

                float3 p = EntityManager.GetComponentData<LocalTransform>(rider).Position;
                var centre = new int2((int)math.round(p.x), (int)math.round(p.z));
                if (!_covers.TryGetValue(rider, out var cover)) _covers[rider] = cover = new Cover();
                if (!cover.Cell.Equals(centre) || now >= cover.Until)
                {
                    CoveredCells(p, unit, cover.Cells);
                    cover.Cell = centre;
                    cover.Until = now + RescanSeconds;
                }
                var buffer = EntityManager.GetBuffer<TileDamageBuffer>(_tileDamage.GetSingletonEntity());
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var cell = centre + new int2(dx, dy);
                        if (LayGroundSystem.IsCoreCell(cell) || cover.Cells.Contains(cell)) continue;
                        buffer.Add(new TileDamageBuffer
                        {
                            position = cell,
                            damage = Damage,
                            causedByEntity = rider,
                            canHitGround = true,
                            canHitLowColliders = false,
                            dontHitBridges = true,
                            pullAnyLootToPlayer = true,
                        });
                    }
            }
        }

        /// <summary>Fills <paramref name="covered"/> with the footprint tiles of placed objects near the rider (not the unit).</summary>
        private void CoveredCells(float3 p, Entity unit, HashSet<int2> covered)
        {
            covered.Clear();
            var blob = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            var entities = _placed.ToEntityArray(Allocator.Temp);
            var transforms = _placed.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var data = _placed.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var q = transforms[i].Position;
                if (entities[i] == unit || math.abs(q.x - p.x) > Reach || math.abs(q.z - p.z) > Reach) continue;
                ref var info = ref PugDatabase.GetEntityObjectInfo(data[i].objectID, blob, data[i].variation);
                if (info.objectType == ObjectType.Creature || info.objectType == ObjectType.Critter
                    || info.objectType == ObjectType.Pet || info.objectType == ObjectType.PlayerType) continue;
                int2 size = math.max(info.prefabTileSize, new int2(1, 1)), offset = info.prefabCornerOffset;
                if (EntityManager.HasComponent<DirectionCD>(entities[i]))
                    EntityManager.GetComponentData<DirectionCD>(entities[i]).GetPrefabOffsetAndTileSize(offset, size, out offset, out size);
                var origin = new int2((int)math.round(q.x), (int)math.round(q.z));
                for (int x = 0; x < size.x; x++)
                    for (int y = 0; y < size.y; y++)
                        covered.Add(origin + offset + new int2(x, y));
            }
            entities.Dispose();
            transforms.Dispose();
            data.Dispose();
        }
    }
}
