using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BuffDurationFloor.Systems
{
    /// <summary>
    /// Rewrites the buff durations stored on consumable item prefabs
    /// (GivesConditionsWhenConsumedBuffer: the raw entry and the "when cooked" entry of every
    /// element) so that no positive, timed buff is shorter than the configured floor.
    ///
    /// Runs in both the client and the server world because the consume job runs (predicted) in
    /// both and reads this data through a BufferLookup on the prefab entity. The original
    /// durations are remembered per prefab so lowering the floor later restores vanilla values.
    /// Work happens only when the floor changes or the number of consumable prefabs changes
    /// (e.g. another mod adds items); otherwise OnUpdate is a single entity count.
    ///
    /// Skipped on purpose: entries whose duration is 0 (instant effects such as healing or
    /// hunger), infinite durations, conditions flagged isPermanent (e.g. permanent max health
    /// from certain foods), conditions flagged isNegative in the game's ConditionsTable and,
    /// unless "Also extend healing" is on, health/mana regeneration effects (see
    /// <see cref="ConditionFilter"/>).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ConsumableDurationFloorSystem : PugSimulationSystemBase
    {
        private EntityQuery _consumables;
        private EntityQuery _table;
        private readonly Dictionary<Entity, float[]> _originals = new Dictionary<Entity, float[]>();
        private int _appliedVersion = -1;
        private int _appliedCount = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _consumables = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<GivesConditionsWhenConsumedBuffer>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            _table = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ConditionsTableCD>() },
                Options = EntityQueryOptions.IncludeSystems
            });
            RequireForUpdate(_consumables);
        }

        protected override void OnUpdate()
        {
            int count = _consumables.CalculateEntityCount();
            if (_appliedVersion == FloorSettings.Version && _appliedCount == count) return;

            if (!_table.TryGetSingleton<ConditionsTableCD>(out var table) || !table.Value.IsCreated) return;

            float floor = FloorSettings.FloorSeconds;
            int raised = 0, restored = 0, entries = 0;
            var seen = new HashSet<ConditionID>();
            var excluded = new HashSet<ConditionID>();

            var entities = _consumables.ToEntityArray(Allocator.Temp);
            int prefabCount = entities.Length;
            for (int e = 0; e < entities.Length; e++)
            {
                var entity = entities[e];
                var buffer = EntityManager.GetBuffer<GivesConditionsWhenConsumedBuffer>(entity);
                entries += buffer.Length;

                if (!_originals.TryGetValue(entity, out float[] original) || original.Length != buffer.Length * 2)
                {
                    original = new float[buffer.Length * 2];
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        original[2 * i] = buffer[i].conditionDataContainer.conditionData.duration;
                        original[2 * i + 1] = buffer[i].conditionDataContainer.conditionDataWhenCooked.duration;
                    }
                    _originals[entity] = original;
                }

                for (int i = 0; i < buffer.Length; i++)
                {
                    var element = buffer[i];
                    bool changed = Apply(ref element.conditionDataContainer.conditionData, original[2 * i], floor, in table, ref raised, ref restored, seen, excluded);
                    changed |= Apply(ref element.conditionDataContainer.conditionDataWhenCooked, original[2 * i + 1], floor, in table, ref raised, ref restored, seen, excluded);
                    if (changed) buffer[i] = element;
                }
            }
            entities.Dispose();

            _appliedVersion = FloorSettings.Version;
            _appliedCount = count;
            Debug.Log($"[BuffDurationFloor] {(isServer ? "server" : "client")}: floor {floor:0}s applied to {prefabCount} consumable prefabs ({entries} entries): {raised} durations raised, {restored} restored. "
                + $"Timed condition ids seen: {string.Join(", ", seen)}. Excluded (regen/permanent/negative): {string.Join(", ", excluded)}.");
        }

        /// <summary>Sets <c>data.duration</c> to max(original, floor) when the buff qualifies, else back to original.</summary>
        private static bool Apply(ref ConditionData data, float original, float floor, in ConditionsTableCD table, ref int raised, ref int restored,
            HashSet<ConditionID> seen, HashSet<ConditionID> excluded)
        {
            float target = original;
            bool timed = data.conditionID != ConditionID.None && original > 0f && !float.IsInfinity(original);
            if (timed) seen.Add(data.conditionID);
            if (ConditionFilter.Qualifies(data.conditionID, original, in table))
            {
                if (original < floor) target = floor;
            }
            else if (timed)
            {
                excluded.Add(data.conditionID);
            }

            if (data.duration == target) return false;
            if (target > original) raised++; else restored++;
            data.duration = target;
            return true;
        }
    }
}
