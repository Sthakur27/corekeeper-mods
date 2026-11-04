using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace DrillUnit.Systems
{
    /// <summary>
    /// The game's ResolveStuckPositionSystem (server, Burst) tests the player's spot against water and, for
    /// boat riders, land. Blocked for 0.1 s it moves the player back to a recent tile; still blocked 2 s later
    /// it kills them. A Drill Unit rider is legitimately over both, so right after that system its two timers
    /// are cleared each tick: the 0.1 s timer restarts every tick and never elapses.
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ResolveStuckPositionSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class StuckGuardSystem : PugSimulationSystemBase
    {
        private EntityQuery _units;

        protected override void OnCreate()
        {
            base.OnCreate();
            _units = GetEntityQuery(ComponentType.ReadOnly<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<ControlledByOtherEntityCD>());
            RequireForUpdate(_units);
        }

        protected override void OnUpdate()
        {
            var id = DrillUnitMod.Id;
            if (id == ObjectID.None) return;
            var units = _units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (EntityManager.GetComponentData<ObjectDataCD>(unit).objectID != id) continue;
                var rider = EntityManager.GetComponentData<ControlledByOtherEntityCD>(unit).controlledByEntity;
                if (!DrillUnitMod.IsRiding(EntityManager, rider, out var ridden) || ridden != unit) continue;
                if (!EntityManager.HasComponent<PlayerUnstuckCD>(rider)) continue;
                var unstuck = EntityManager.GetComponentData<PlayerUnstuckCD>(rider);
                if (!unstuck.stuckTimer.isRunning && !unstuck.killIfStuckTimer.isRunning) continue;
                unstuck.stuckTimer.ClearStart();
                unstuck.killIfStuckTimer.ClearStart();
                EntityManager.SetComponentData(rider, unstuck);
            }
            units.Dispose();
        }
    }
}
