using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using PhysicsCollider = Unity.Physics.PhysicsCollider;

namespace DrillUnit.Systems
{
    /// <summary>
    /// Water vs land is only the player's collision filter: the default collider collides with
    /// WaterCollider, the boat collider swaps that for ShorelineCollider (land). The game's Burst
    /// SelectPlayerColliderSystem picks one of those every tick; right after it, a Drill Unit rider gets a
    /// copy of the default collider that collides with neither, nor with DefaultLowCollider (pits and other
    /// low tiles, as the grappling hook collider does), so the unit crosses water, lava, pits and land.
    /// Runs in both worlds so the client's prediction matches the server.
    /// </summary>
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateAfter(typeof(SelectPlayerColliderSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class HoverColliderSystem : PugSimulationSystemBase
    {
        private const uint WaterCollider = 0x20000u, ShorelineCollider = 0x40000u, DefaultLowCollider = 0x100u;

        private EntityQuery _units;
        private BlobAssetReference<Collider> _hover;
        private bool _made;

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
                if (!EntityManager.HasComponent<PlayerColliderCD>(rider) || !EntityManager.HasComponent<PhysicsCollider>(rider)) continue;
                if (!_made)
                {
                    _hover = MakeHover(EntityManager.GetComponentData<PlayerColliderCD>(rider).defaultCollider);
                    _made = true;
                }
                var collider = EntityManager.GetComponentData<PhysicsCollider>(rider);
                if (collider.Value == _hover) continue;
                collider.Value = _hover;
                EntityManager.SetComponentData(rider, collider);
            }
            units.Dispose();
        }

        private static BlobAssetReference<Collider> MakeHover(BlobAssetReference<Collider> source)
        {
            var hover = source.Value.Clone();
            var filter = hover.Value.GetCollisionFilter();
            filter.CollidesWith &= ~(WaterCollider | ShorelineCollider | DefaultLowCollider);
            hover.Value.SetCollisionFilter(filter);
            return hover;
        }

        protected override void OnDestroy()
        {
            if (_made) _hover.Dispose();
            base.OnDestroy();
        }
    }
}
