using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace DrillUnit.Systems
{
    /// <summary>
    /// The unit drills whatever it drives into (server): while the rider holds the use button and steers, a 3-wide, 2-deep block of
    /// wall tiles ahead plus the tiles beside the unit take <see cref="Damage"/> every <see cref="IntervalSeconds"/>
    /// (driving diagonally, both axes' blocks), through the game's TileDamageBuffer, like an explosion or a
    /// charging boss would. The second row starts breaking before the unit reaches it, so it keeps speed. Floors, bridges and
    /// walkable tiles are never hit; immune tiles stay immune (TileDamageSystem applies tile damage reduction).
    /// Drops are pulled to the rider.
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(TileDamageSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class DrillMineSystem : PugSimulationSystemBase
    {
        public const int Damage = 400;
        private const double IntervalSeconds = 0.2;
        private const int Depth = 2;          // rows ahead of the rider's cell
        private const float Diagonal = 0.35f; // input component that also counts as a drilling axis

        private EntityQuery _units, _tileDamage;
        private readonly Dictionary<Entity, double> _next = new Dictionary<Entity, double>();
        private readonly HashSet<int2> _cells = new HashSet<int2>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _units = GetEntityQuery(ComponentType.ReadOnly<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<ControlledByOtherEntityCD>());
            _tileDamage = GetEntityQuery(ComponentType.ReadWrite<TileDamageBuffer>());
            RequireForUpdate(_units);
        }

        protected override void OnUpdate()
        {
            var id = DrillUnitMod.Id;
            if (id == ObjectID.None || _tileDamage.IsEmptyIgnoreFilter) return;
            double now = World.Time.ElapsedTime;
            var units = _units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (EntityManager.GetComponentData<ObjectDataCD>(unit).objectID != id) continue;
                var rider = EntityManager.GetComponentData<ControlledByOtherEntityCD>(unit).controlledByEntity;
                if (!DrillUnitMod.IsRiding(EntityManager, rider, out var ridden) || ridden != unit) continue;
                if (!EntityManager.HasComponent<ClientInput>(rider)) continue;
                var input = EntityManager.GetComponentData<ClientInput>(rider);
                if (!InputBits.Drilling(input.buttonSetMask)) continue;
                float2 dir = input.movementDirection;
                if (math.lengthsq(dir) < 0.01f) continue;
                if (_next.TryGetValue(rider, out double next) && now < next) continue;
                _next[rider] = now + IntervalSeconds;

                dir = math.normalize(dir);
                float3 p = EntityManager.GetComponentData<LocalTransform>(rider).Position;
                var origin = new int2((int)math.round(p.x), (int)math.round(p.z));
                _cells.Clear();
                if (math.abs(dir.x) > Diagonal) AddArea(origin, new int2((int)math.sign(dir.x), 0));
                if (math.abs(dir.y) > Diagonal) AddArea(origin, new int2(0, (int)math.sign(dir.y)));
                var buffer = EntityManager.GetBuffer<TileDamageBuffer>(_tileDamage.GetSingletonEntity());
                foreach (var cell in _cells) Hit(buffer, rider, cell);
            }
            units.Dispose();
        }

        /// <summary>Rows 1..Depth ahead along <paramref name="forward"/>, 3 wide, plus the two cells beside the unit.</summary>
        private void AddArea(int2 origin, int2 forward)
        {
            var side = new int2(-forward.y, forward.x);
            for (int d = 0; d <= Depth; d++)
                for (int s = -1; s <= 1; s++)
                    if (d > 0 || s != 0) _cells.Add(origin + forward * d + side * s);
        }

        private static void Hit(DynamicBuffer<TileDamageBuffer> buffer, Entity rider, int2 cell)
        {
            buffer.Add(new TileDamageBuffer
            {
                position = cell,
                damage = Damage,
                causedByEntity = rider,
                canHitGround = false,
                canHitLowColliders = false,
                dontHitWalkableTiles = true,
                dontHitBridges = true,
                dontHitGroundSlime = true,
                pullAnyLootToPlayer = true,
            });
        }
    }
}
