using System.Collections.Generic;
using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace WormholePotion
{
    // The return point is our own copy of the vanilla waypoint map marker, so the map
    // lets a player standing at an active waypoint click it and teleport with the
    // game's own UI action. Unlike the vanilla one it is saved with the world.
    internal static class ReturnMarkers
    {
        // MapMarkerUIElement sends players to the marker position + (1, -0.25) for every
        // marker variation except 20; place the marker so they land where they drank.
        public static readonly float3 LandingOffset = new float3(1f, 0f, -0.25f);
        // Below this many tiles from the Core a way back would be pointless.
        private const float MinDistance = 12f;
        // Marker per player for this session: drinking again replaces your own previous marker.
        private static readonly Dictionary<Entity, Entity> owned = new Dictionary<Entity, Entity>();

        public static void Spawn(World world, Entity player, float3 origin, BlobAssetReference<PugDatabase.PugDatabaseBank> bank)
        {
            var marker = WormholePotionMod.Marker;
            if (marker == ObjectID.None) return;
            float3 spawn = PlayerControllerBurstableStatics.PLAYER_SPAWN_POSITION;
            if (math.distance(origin.xz, spawn.xz) < MinDistance) return;
            var em = world.EntityManager;
            if (owned.TryGetValue(player, out var previous) && IsMarker(em, previous))
                em.DestroyEntity(previous);
            owned.Remove(player);
            float3 position = new float3(origin.x, 0f, origin.z) - LandingOffset;
            var created = EntityUtility.CreateEntity(world, (Vector3)position, marker, 1, bank);
            if (created == Entity.Null) { Debug.LogWarning("[WormholePotion] Could not create the return marker."); return; }
            if (em.HasComponent<MapMarkerActivatedCD>(created))
                em.SetComponentData(created, new MapMarkerActivatedCD { Value = true, Hidden = false });
            owned[player] = created;
            Debug.Log($"[WormholePotion] Return marker {created} at {position.x:0.#}, {position.z:0.#}");
        }

        public static bool IsMarker(EntityManager em, Entity e) =>
            e != Entity.Null && em.Exists(e) && em.HasComponent<ObjectDataCD>(e)
            && em.GetComponentData<ObjectDataCD>(e).objectID == WormholePotionMod.Marker;
    }

    // Server only. Keeps return markers active (a loaded save starts them inactive) and
    // removes a marker once a player's teleport targets it: the trip is used up.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class ReturnMarkerSystem : PugSimulationSystemBase
    {
        private const float Tolerance = 0.5f;
        private EntityQuery markers, players;

        protected override void OnCreate()
        {
            base.OnCreate();
            markers = GetEntityQuery(ComponentType.ReadOnly<MapMarkerCD>(), ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadWrite<MapMarkerActivatedCD>());
            players = GetEntityQuery(ComponentType.ReadOnly<PlayerGhost>(), ComponentType.ReadOnly<PlayerStateCD>(),
                ComponentType.ReadOnly<TeleportingStateCD>());
        }

        protected override void OnUpdate()
        {
            var marker = WormholePotionMod.Marker;
            if (marker == ObjectID.None || !WormholePotionMod.Active || markers.IsEmpty) return;
            using (var entities = markers.ToEntityArray(Allocator.Temp))
            using (var people = players.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                if (EntityManager.GetComponentData<ObjectDataCD>(entity).objectID != marker) continue;
                var active = EntityManager.GetComponentData<MapMarkerActivatedCD>(entity);
                if (!active.Value || active.Hidden)
                    EntityManager.SetComponentData(entity, new MapMarkerActivatedCD { Value = true, Hidden = false });
                float3 landing = EntityManager.GetComponentData<LocalTransform>(entity).Position + ReturnMarkers.LandingOffset;
                foreach (var player in people)
                {
                    var state = EntityManager.GetComponentData<PlayerStateCD>(player);
                    if (!state.HasAnyState(PlayerStateEnum.Teleporting) && state.nextState != PlayerStateEnum.Teleporting) continue;
                    Vector3 target = EntityManager.GetComponentData<TeleportingStateCD>(player).targetPosition;
                    if (math.abs(target.x - landing.x) > Tolerance || math.abs(target.z - landing.z) > Tolerance) continue;
                    EntityManager.DestroyEntity(entity);
                    Debug.Log("[WormholePotion] Return marker used.");
                    break;
                }
            }
        }
    }
}
