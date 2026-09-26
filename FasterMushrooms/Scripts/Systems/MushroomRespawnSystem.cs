using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace FasterMushrooms.Systems
{
    /// <summary>
    /// Wild mushroom respawn near players. Vanilla respawns environment objects through
    /// SpawnEnvironmentObjectsPeriodicallySystem, which visits one 16x16 area at a time (each area
    /// roughly once every 4 hours) and skips every area within 200 tiles of a player, so mushrooms
    /// you pick near your base basically never come back.
    ///
    /// Every <see cref="MushroomRespawn.IntervalSeconds"/> this system queues vanilla respawn
    /// requests (SpawnEnvironmentObjectsCD with respawn = true) for every 16x16 area within
    /// <see cref="MushroomRespawn.Radius"/> tiles of a player. While those requests are in flight
    /// the spawn chance of every non-mushroom respawn entry in the environment spawn table is zeroed,
    /// so only mushrooms roll; the vanilla rules stay (right ground and biome, free tile, not within
    /// 6 tiles of a player, fewer spawns when many mushrooms already exist). Once the game has
    /// consumed the requests the original chances are restored.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MushroomRespawnSystem : PugSimulationSystemBase
    {
        private const int AreaSize = 16;
        private const int MaxPendingFrames = 30;

        private EntityQuery _table;
        private EntityQuery _requests;
        private EntityQuery _players;

        private float _timer = 10f;
        private bool _pending;
        private int _pendingFrames;
        private Entity _tableEntity;
        private readonly List<KeyValuePair<int, WorldGenSettingDependentValue<float>>> _saved =
            new List<KeyValuePair<int, WorldGenSettingDependentValue<float>>>();
        private bool _loggedTable;

        protected override void OnCreate()
        {
            base.OnCreate();
            _table = GetEntityQuery(ComponentType.ReadWrite<EnvironmentSpawnObjectBuffer>());
            _requests = GetEntityQuery(ComponentType.ReadOnly<SpawnEnvironmentObjectsCD>());
            _players = GetEntityQuery(ComponentType.ReadOnly<PlayerGhost>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            if (_pending)
            {
                if (_requests.IsEmptyIgnoreFilter || ++_pendingFrames > MaxPendingFrames)
                    Restore();
                return;
            }

            float interval = MushroomRespawn.IntervalSeconds;
            if (interval <= 0f) return;

            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = interval;

            if (_table.IsEmptyIgnoreFilter || _players.IsEmptyIgnoreFilter) return;
            // A vanilla respawn is already queued: let it run untouched, try again next interval.
            if (!_requests.IsEmptyIgnoreFilter) return;

            var areas = CollectAreas();
            if (areas.Count == 0) return;

            _tableEntity = _table.GetSingletonEntity();
            var table = EntityManager.GetBuffer<EnvironmentSpawnObjectBuffer>(_tableEntity);
            LogTableOnce(table);

            int mushroomEntries = 0;
            _saved.Clear();
            for (int i = 0; i < table.Length; i++)
            {
                var entry = table[i];
                if (!entry.respawn) continue;
                if (MushroomSpeed.IsWildMushroom(entry.objectId)) { mushroomEntries++; continue; }
                _saved.Add(new KeyValuePair<int, WorldGenSettingDependentValue<float>>(i, entry.spawnChance));
                entry.spawnChance = default;
                table[i] = entry;
            }
            if (mushroomEntries == 0)
            {
                Restore();
                return;
            }

            foreach (int2 pos in areas)
            {
                Entity e = EntityManager.CreateEntity(typeof(SpawnEnvironmentObjectsCD));
                EntityManager.SetComponentData(e, new SpawnEnvironmentObjectsCD { respawn = true, position = pos });
            }
            _pending = true;
            _pendingFrames = 0;
        }

        private HashSet<int2> CollectAreas()
        {
            var result = new HashSet<int2>();
            int radius = MushroomRespawn.Radius;
            var transforms = _players.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int p = 0; p < transforms.Length; p++)
            {
                float3 pos = transforms[p].Position;
                int2 center = new int2((int)math.round(pos.x), (int)math.round(pos.z));
                int2 min = (int2)math.floor((float2)(center - radius) / AreaSize);
                int2 max = (int2)math.floor((float2)(center + radius) / AreaSize);
                for (int y = min.y; y <= max.y; y++)
                    for (int x = min.x; x <= max.x; x++)
                        result.Add(new int2(x, y) * AreaSize);
            }
            transforms.Dispose();
            return result;
        }

        private void Restore()
        {
            _pending = false;
            if (_saved.Count == 0 || _tableEntity == Entity.Null || !EntityManager.Exists(_tableEntity)) { _saved.Clear(); return; }
            var table = EntityManager.GetBuffer<EnvironmentSpawnObjectBuffer>(_tableEntity);
            foreach (var kv in _saved)
            {
                if (kv.Key >= table.Length) continue;
                var entry = table[kv.Key];
                entry.spawnChance = kv.Value;
                table[kv.Key] = entry;
            }
            _saved.Clear();
        }

        private void LogTableOnce(DynamicBuffer<EnvironmentSpawnObjectBuffer> table)
        {
            if (_loggedTable) return;
            _loggedTable = true;
            for (int i = 0; i < table.Length; i++)
            {
                var e = table[i];
                if (!e.respawn || !MushroomSpeed.IsWildMushroom(e.objectId)) continue;
                Debug.Log($"[FasterMushrooms] respawn entry #{i} {e.objectId} chance={e.spawnChance.normal} " +
                          $"maxPerTile={e.maxSpawnPerTile} maxPerRespawn={e.maxSpawnsPerRespawn} decay={e.respawnChanceDecay} " +
                          $"biome={e.spawnsInBiome} group+{e.alsoSpawnNextNObjectsFromSameBiome}");
            }
        }
    }
}
