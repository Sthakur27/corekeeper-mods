using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace VehicleSpeed.Systems
{
    /// <summary>
    /// Boats: PlayerVelocityCalculationSystem multiplies the rider's velocity by BoatCD.speedMultiplier.
    /// Go-karts: VehicleRiding / PlayerVelocityCalculationSystem multiply the kart speed by VehicleCD.speedMultiplier.
    /// Neither field is replicated and both are only set at conversion, so this system sets them on
    /// every boat / kart in both the client and server worlds (the client predicts its own ride) to
    /// vanilla x multiplier. Vanilla values come from the prefab entities, never from the live
    /// entity, so saved or re-applied values can't compound. Runs every half second (new boats/karts
    /// placed) and immediately when a setting changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class VehicleSpeedSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 0.5f;

        private EntityQuery _boats, _boatPrefabs, _karts, _kartPrefabs;
        private readonly Dictionary<ObjectID, float> _vanillaBoat = new Dictionary<ObjectID, float>();
        private readonly Dictionary<ObjectID, float> _vanillaKart = new Dictionary<ObjectID, float>();
        private float _timer;
        private int _appliedVersion = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _boats = GetEntityQuery(ComponentType.ReadWrite<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>());
            _karts = GetEntityQuery(ComponentType.ReadWrite<VehicleCD>(), ComponentType.ReadOnly<ObjectDataCD>());
            _boatPrefabs = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<Prefab>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            _kartPrefabs = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<VehicleCD>(), ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<Prefab>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            RequireAnyForUpdate(_boats, _karts);
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f && _appliedVersion == SpeedSettings.Version) return;
            _timer = IntervalSeconds;
            _appliedVersion = SpeedSettings.Version;

            if (_vanillaBoat.Count == 0) ReadPrefabs(_boatPrefabs, _vanillaBoat, true);
            if (_vanillaKart.Count == 0) ReadPrefabs(_kartPrefabs, _vanillaKart, false);

            float boatMult = SpeedSettings.Boat;
            var boats = _boats.ToEntityArray(Allocator.Temp);
            foreach (var e in boats)
            {
                var id = EntityManager.GetComponentData<ObjectDataCD>(e).objectID;
                if (!_vanillaBoat.TryGetValue(id, out float vanilla)) continue;
                var boat = EntityManager.GetComponentData<BoatCD>(e);
                float target = vanilla * boatMult;
                if (boat.speedMultiplier == target) continue;
                boat.speedMultiplier = target;
                EntityManager.SetComponentData(e, boat);
            }
            boats.Dispose();

            float kartMult = SpeedSettings.GoKart;
            var karts = _karts.ToEntityArray(Allocator.Temp);
            foreach (var e in karts)
            {
                var id = EntityManager.GetComponentData<ObjectDataCD>(e).objectID;
                if (!_vanillaKart.TryGetValue(id, out float vanilla)) continue;
                var kart = EntityManager.GetComponentData<VehicleCD>(e);
                float target = vanilla * kartMult;
                if (kart.speedMultiplier == target) continue;
                kart.speedMultiplier = target;
                EntityManager.SetComponentData(e, kart);
            }
            karts.Dispose();
        }

        private void ReadPrefabs(EntityQuery query, Dictionary<ObjectID, float> into, bool boat)
        {
            var prefabs = query.ToEntityArray(Allocator.Temp);
            foreach (var p in prefabs)
            {
                var id = EntityManager.GetComponentData<ObjectDataCD>(p).objectID;
                if (into.ContainsKey(id)) continue;
                into[id] = boat
                    ? EntityManager.GetComponentData<BoatCD>(p).speedMultiplier
                    : EntityManager.GetComponentData<VehicleCD>(p).speedMultiplier;
            }
            prefabs.Dispose();
            if (into.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kv in into) parts.Add($"{kv.Key}={kv.Value:0.##}");
                UnityEngine.Debug.Log($"[{VehicleSpeedMod.Name}] Vanilla {(boat ? "boat speed multipliers" : "go-kart speed multipliers")} ({World.Name}): {string.Join(", ", parts)}");
            }
        }
    }
}
