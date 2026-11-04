using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace DrillUnit.Systems
{
    /// <summary>
    /// Keeps the unit from popping back into an item (server):
    /// - Placed objects carry DestroyEntityIfNotOnTileCD; every 1-2 s DestroyEntityIfNotOnTileSystem checks the
    ///   tile under them and, for a boat-type object, destroys it on anything but its placement tiles (for the
    ///   unit: walkable ground or water). Lava, pits and blocked tiles under a hovering unit therefore dropped it.
    ///   Its countdown is held above zero here, so that check never runs for the unit.
    /// - While ridden, its health (2, like a boat; it is picked up by hitting it) is kept full, so stray hits
    ///   and splash damage can't knock the rider out. Parked, it can still be picked up as usual.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class UnitKeepSystem : PugSimulationSystemBase
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
                if (EntityManager.HasComponent<DestroyEntityIfNotOnTileCD>(unit))
                {
                    var destroy = EntityManager.GetComponentData<DestroyEntityIfNotOnTileCD>(unit);
                    if (destroy.timer < 5f)
                    {
                        destroy.timer = 10f;
                        EntityManager.SetComponentData(unit, destroy);
                    }
                }
                var rider = EntityManager.GetComponentData<ControlledByOtherEntityCD>(unit).controlledByEntity;
                if (rider == Entity.Null || !EntityManager.HasComponent<HealthCD>(unit)) continue;
                var health = EntityManager.GetComponentData<HealthCD>(unit);
                if (health.health >= health.maxHealth || health.health <= 0) continue;
                health.health = health.maxHealth;
                EntityManager.SetComponentData(unit, health);
            }
            units.Dispose();
        }
    }
}
