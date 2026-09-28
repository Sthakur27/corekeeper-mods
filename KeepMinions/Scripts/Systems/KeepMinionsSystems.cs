using System.Collections.Generic;
using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace KeepMinions.Systems
{
    /// <summary>
    /// Teleporting (portals, waypoints, recall) enables DisablePhysicsCD on the player for the whole
    /// 6.1 s sequence, and MinionHandlerSystem destroys every minion whose owner has physics
    /// disabled. For the one system update of MinionHandlerSystem this pre/post pair switches the
    /// flag off for players that are alive and in the Teleporting state, so their minions are not
    /// destroyed; every other system still sees the teleport exactly as vanilla. Death still
    /// dismisses minions as usual.
    /// </summary>
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    [UpdateBefore(typeof(MinionHandlerSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class HideTeleportFromMinionsSystem : PugSimulationSystemBase
    {
        internal static readonly List<Entity> Hidden = new List<Entity>();

        private EntityQuery _players;

        protected override void OnCreate()
        {
            UpdatesInRunGroup();
            _players = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PlayerStateCD>(),
                    ComponentType.ReadOnly<HealthCD>(),
                    ComponentType.ReadWrite<DisablePhysicsCD>()
                }
            });
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            Hidden.Clear();
            var entities = _players.ToEntityArray(Allocator.Temp);
            var states = _players.ToComponentDataArray<PlayerStateCD>(Allocator.Temp);
            var healths = _players.ToComponentDataArray<HealthCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!states[i].HasAnyState(PlayerStateEnum.Teleporting)) continue;
                if (healths[i].health <= 0) continue;
                if (!EntityManager.IsComponentEnabled<DisablePhysicsCD>(entities[i])) continue;
                EntityManager.SetComponentEnabled<DisablePhysicsCD>(entities[i], false);
                Hidden.Add(entities[i]);
            }
            entities.Dispose();
            states.Dispose();
            healths.Dispose();
            base.OnUpdate();
        }
    }

    /// <summary>Turns the teleport's DisablePhysicsCD back on right after MinionHandlerSystem ran.</summary>
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    [UpdateAfter(typeof(MinionHandlerSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class RestoreTeleportAfterMinionsSystem : PugSimulationSystemBase
    {
        protected override void OnCreate()
        {
            UpdatesInRunGroup();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            foreach (Entity player in HideTeleportFromMinionsSystem.Hidden)
            {
                if (EntityManager.Exists(player) && EntityManager.HasComponent<DisablePhysicsCD>(player))
                    EntityManager.SetComponentEnabled<DisablePhysicsCD>(player, true);
            }
            HideTeleportFromMinionsSystem.Hidden.Clear();
            base.OnUpdate();
        }
    }

    /// <summary>
    /// After the teleport jump the minions would be left at the old location, so any minion that
    /// ends up more than <see cref="MaxDistance"/> tiles from its living owner is moved next to them.
    /// Normal fighting never takes minions that far away.
    /// </summary>
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    [UpdateAfter(typeof(RestoreTeleportAfterMinionsSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class BringMinionsAlongSystem : PugSimulationSystemBase
    {
        private const float MaxDistance = 32f;

        private EntityQuery _minions;

        protected override void OnCreate()
        {
            UpdatesInRunGroup();
            _minions = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MinionCD>(),
                    ComponentType.ReadOnly<OwnerReferenceCD>(),
                    ComponentType.ReadWrite<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<EntityDestroyedCD>() }
            });
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var entities = _minions.ToEntityArray(Allocator.Temp);
            var owners = _minions.ToComponentDataArray<OwnerReferenceCD>(Allocator.Temp);
            var transforms = _minions.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity owner = owners[i].owner;
                if (owner == Entity.Null || !EntityManager.Exists(owner)) continue;
                if (!EntityManager.HasComponent<LocalTransform>(owner)) continue;
                if (EntityManager.HasComponent<DisablePhysicsCD>(owner) && EntityManager.IsComponentEnabled<DisablePhysicsCD>(owner))
                {
                    // Still mid-teleport: wait until the player has landed and physics is back.
                    continue;
                }

                float3 ownerPos = EntityManager.GetComponentData<LocalTransform>(owner).Position;
                LocalTransform t = transforms[i];
                if (math.distancesq(t.Position.xz, ownerPos.xz) <= MaxDistance * MaxDistance) continue;

                // Spread them a little so they do not stack on one tile.
                float angle = i * 2.39996f;
                t.Position = ownerPos + new float3(math.cos(angle) * 0.6f, 0f, math.sin(angle) * 0.6f);
                EntityManager.SetComponentData(entities[i], t);
            }
            entities.Dispose();
            owners.Dispose();
            transforms.Dispose();
            base.OnUpdate();
        }
    }
}
