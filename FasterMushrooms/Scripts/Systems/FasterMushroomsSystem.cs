using System;
using System.Collections.Generic;
using Pug.Properties;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FasterMushrooms.Systems
{
    /// <summary>
    /// Server-side, runs every tick right before the game's RootPlantGrowSystem. Three passes:
    ///
    /// 1. RootPlantCD on every mushroom entity: min/max time between spreads = vanilla / multiplier.
    ///    The vanilla values are remembered per ObjectID the first time an entity of that type is
    ///    seen (the game never changes them; they come straight from the prefab). Because this runs
    ///    before RootPlantGrowSystem, every spread timer it rolls already uses the shorter window.
    /// 2. PugFloraGrowerCD timers whose source entity is a mushroom: capped at
    ///    ceil(vanillaMax / multiplier * tickRate) ticks. Timers rolled from the scaled window are
    ///    always below the cap (no-op); timers that were rolled before the mod or before a settings
    ///    change are shortened to the cap. At 1x the cap equals the vanilla maximum, so nothing
    ///    changes.
    /// 3. GrowTimerCD entities that belong to a mushroom: StageTime = vanilla stage time (read from
    ///    the mushroom's ObjectPropertiesCD) / multiplier.
    ///
    /// All three compare against the target before writing, so a settings change takes effect on
    /// the next tick and the common case is read-only.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RootPlantGrowSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class FasterMushroomsSystem : PugSimulationSystemBase
    {
        /// <summary>Property hash the game uses for a plant's seconds-per-growth-stage (see PlantsGrowingSystem).</summary>
        private const int StageTimePropertyHash = -965595447;
        private const float Epsilon = 0.0005f;

        private struct SpreadWindow
        {
            public float Min;
            public float Max;
        }

        private EntityQuery _plants;
        private EntityQuery _growers;
        private EntityQuery _growTimers;
        private EntityQuery _tickRate;

        private readonly Dictionary<int, SpreadWindow> _vanillaByObject = new Dictionary<int, SpreadWindow>();
        // Per-entity "is this a mushroom root plant" answers; Entity includes the version so a
        // recycled index is a different key. Cleared periodically to stay small.
        private readonly Dictionary<Entity, bool> _mushroomCache = new Dictionary<Entity, bool>();
        private int _cacheAge;

        protected override void OnCreate()
        {
            base.OnCreate();

            _plants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<RootPlantCD>(), ComponentType.ReadOnly<ObjectDataCD>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            _growers = GetEntityQuery(ComponentType.ReadWrite<PugFloraGrowerCD>());
            _growTimers = GetEntityQuery(ComponentType.ReadWrite<GrowTimerCD>(), ComponentType.ReadOnly<GrowerRefCD>());
            _tickRate = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ClientServerTickRate>() },
                Options = EntityQueryOptions.IncludeSystems
            });

            RequireForUpdate(_plants);
        }

        protected override void OnUpdate()
        {
            float mult = MushroomSpeed.Multiplier;
            if (mult < 1f) mult = 1f;

            if (++_cacheAge > 600)
            {
                _cacheAge = 0;
                _mushroomCache.Clear();
            }

            ScaleSpreadWindows(mult);
            CapRunningSpreadTimers(mult);
            ScaleStageTimers(mult);
        }

        private void ScaleSpreadWindows(float mult)
        {
            var entities = _plants.ToEntityArray(Allocator.Temp);
            var plants = _plants.ToComponentDataArray<RootPlantCD>(Allocator.Temp);
            var objects = _plants.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                ObjectID id = objects[i].objectID;
                if (!MushroomSpeed.IsMushroom(id)) continue;

                var plant = plants[i];
                int key = (int)id;
                if (!_vanillaByObject.TryGetValue(key, out SpreadWindow vanilla))
                {
                    // First entity of this type: still carries the prefab (vanilla) values.
                    vanilla = new SpreadWindow { Min = plant.minTimeBetweenSpread, Max = plant.maxTimeBetweenSpread };
                    _vanillaByObject[key] = vanilla;
                }

                float targetMin = vanilla.Min / mult;
                float targetMax = vanilla.Max / mult;
                if (Math.Abs(plant.minTimeBetweenSpread - targetMin) < Epsilon &&
                    Math.Abs(plant.maxTimeBetweenSpread - targetMax) < Epsilon)
                    continue;

                plant.minTimeBetweenSpread = targetMin;
                plant.maxTimeBetweenSpread = targetMax;
                EntityManager.SetComponentData(entities[i], plant);
            }

            entities.Dispose();
            plants.Dispose();
            objects.Dispose();
        }

        private void CapRunningSpreadTimers(float mult)
        {
            if (_growers.IsEmptyIgnoreFilter) return;

            int tickRate = 60;
            if (!_tickRate.IsEmptyIgnoreFilter)
            {
                var rate = _tickRate.GetSingleton<ClientServerTickRate>();
                rate.ResolveDefaults();
                tickRate = rate.SimulationTickRate;
            }

            var entities = _growers.ToEntityArray(Allocator.Temp);
            var growers = _growers.ToComponentDataArray<PugFloraGrowerCD>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var grower = growers[i];
                if (!TryGetMushroomObject(grower.entity, out int objectKey)) continue;
                if (!_vanillaByObject.TryGetValue(objectKey, out SpreadWindow vanilla)) continue;

                int cap = (int)Math.Ceiling(vanilla.Max / mult * tickRate);
                if (cap < 1) cap = 1;
                if (grower.timer <= cap) continue;

                grower.timer = cap;
                EntityManager.SetComponentData(entities[i], grower);
            }

            entities.Dispose();
            growers.Dispose();
        }

        private void ScaleStageTimers(float mult)
        {
            if (_growTimers.IsEmptyIgnoreFilter) return;

            var entities = _growTimers.ToEntityArray(Allocator.Temp);
            var timers = _growTimers.ToComponentDataArray<GrowTimerCD>(Allocator.Temp);
            var refs = _growTimers.ToComponentDataArray<GrowerRefCD>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity plant = refs[i].Value;
                if (!TryGetMushroomObject(plant, out _)) continue;
                if (!EntityManager.HasComponent<ObjectPropertiesCD>(plant)) continue;

                float vanillaStage = EntityManager.GetComponentData<ObjectPropertiesCD>(plant).Get<float>(StageTimePropertyHash);
                if (vanillaStage <= 0f) continue;

                var timer = timers[i];
                float target = vanillaStage / mult;
                if (Math.Abs(timer.StageTime - target) < Epsilon) continue;

                timer.StageTime = target;
                EntityManager.SetComponentData(entities[i], timer);
            }

            entities.Dispose();
            timers.Dispose();
            refs.Dispose();
        }

        /// <summary>True if <paramref name="entity"/> is a live mushroom root plant; also returns its ObjectID key.</summary>
        private bool TryGetMushroomObject(Entity entity, out int objectKey)
        {
            objectKey = 0;
            if (entity == Entity.Null || !EntityManager.Exists(entity)) return false;

            if (_mushroomCache.TryGetValue(entity, out bool isMushroom) && !isMushroom) return false;

            if (!EntityManager.HasComponent<ObjectDataCD>(entity) || !EntityManager.HasComponent<RootPlantCD>(entity))
            {
                _mushroomCache[entity] = false;
                return false;
            }

            ObjectID id = EntityManager.GetComponentData<ObjectDataCD>(entity).objectID;
            bool result = MushroomSpeed.IsMushroom(id);
            _mushroomCache[entity] = result;
            objectKey = (int)id;
            return result;
        }
    }
}
