using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Server, experimental (2026-11-05): minion on-hit enchants (EnchantDef.OnMinions, tome pool). A minion's hits
    /// use the minion's own summary as the attacker (EntityUtility.GetOwnerInfo: attacker = the minion), so the
    /// player's stats never reach them. EnchantEffectSystem collects the carried tome's OnMinions enchants per player
    /// (<see cref="EnchantEffectSystem.MinionWants"/>); this system keeps one marked entry per condition in every
    /// minion's ConditionsBuffer (same infinite-duration + Marker trick as on players).
    ///
    /// Safe conditions only: MinionHandlerSystem.SetConditionsJob re-adds a minion's ApplyBurning / poison /
    /// radioactive entries at the minion's level every tick, but leaves any other ID alone
    /// (MinionExtensions.GetUpgradedConditionValue returns 0), so ChanceOnHitToStun and LifeOnMinionHit stay ours.
    ///
    /// Cost: a few minions per player; the buffers are read read-only every <see cref="Interval"/> updates and
    /// written only when a value differs (new minion, tome changed).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnchantEffectSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnchantMinionSystem : PugSimulationSystemBase
    {
        public const int Interval = 10; // ~0.17 s at 60 Hz: a new minion gets its entries almost at once

        private EntityQuery _minions;
        private long _update;
        private readonly Dictionary<ConditionID, int> _wanted = new Dictionary<ConditionID, int>();
        private readonly Dictionary<ConditionID, int> _ours = new Dictionary<ConditionID, int>();
        private readonly HashSet<ConditionID> _kept = new HashSet<ConditionID>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _minions = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MinionCD>(),
                    ComponentType.ReadOnly<OwnerReferenceCD>(),
                    ComponentType.ReadOnly<ConditionsBuffer>(),
                },
                None = new[] { ComponentType.ReadOnly<EntityDestroyedCD>() },
            });
            RequireForUpdate(_minions);
        }

        protected override void OnUpdate()
        {
            if (++_update % Interval != 0 || _minions.IsEmpty) return;
            var minions = _minions.ToEntityArray(Allocator.Temp);
            foreach (var minion in minions)
            {
                var owner = EntityManager.GetComponentData<OwnerReferenceCD>(minion).owner;
                _wanted.Clear();
                EnchantEffectSystem.MinionWants(owner, _wanted);
                Sync(minion);
            }
            minions.Dispose();
        }

        // Make the minion's marked entries equal _wanted; read-only unless something differs.
        private void Sync(Entity minion)
        {
            var ro = EntityManager.GetBuffer<ConditionsBuffer>(minion, true);
            _ours.Clear();
            bool dirty = false;
            for (int i = 0; i < ro.Length; i++)
            {
                var c = ro[i].condition;
                if (!EnchantEffectSystem.IsOurs(c)) continue;
                var id = c.conditionData.conditionID;
                if (c.toBeRemoved || _ours.ContainsKey(id)) dirty = true;
                _ours[id] = c.conditionData.value;
            }
            if (_ours.Count != _wanted.Count) dirty = true;
            else
                foreach (var kv in _wanted)
                    if (!_ours.TryGetValue(kv.Key, out int v) || v != kv.Value) { dirty = true; break; }
            if (!dirty) return;

            var rw = EntityManager.GetBuffer<ConditionsBuffer>(minion);
            _kept.Clear();
            for (int i = rw.Length - 1; i >= 0; i--)
            {
                var elem = rw[i];
                if (!EnchantEffectSystem.IsOurs(elem.condition)) continue;
                var id = elem.condition.conditionData.conditionID;
                if (elem.condition.toBeRemoved || !_wanted.TryGetValue(id, out int want) || !_kept.Add(id))
                {
                    rw.RemoveAt(i);
                    continue;
                }
                if (elem.condition.conditionData.value != want)
                {
                    elem.condition.conditionData.value = want;
                    rw[i] = elem;
                }
            }
            foreach (var kv in _wanted)
            {
                if (_kept.Contains(kv.Key)) continue;
                rw.Add(new ConditionsBuffer
                {
                    condition = new Condition
                    {
                        conditionData = new ConditionData
                        {
                            conditionID = kv.Key,
                            value = kv.Value,
                            duration = float.PositiveInfinity,
                            valueMultiplier = EnchantEffectSystem.Marker,
                        },
                    },
                });
            }
            if (EnchantDebug.On)
            {
                var parts = new List<string>();
                foreach (var kv in _wanted) parts.Add($"{kv.Key}={kv.Value}");
                Debug.Log($"[{EnchantingMod.Name}] minion {minion.Index} enchant bonuses: {(parts.Count == 0 ? "none" : string.Join(", ", parts))}");
            }
        }

        /// <summary>The live minions owned by <paramref name="owner"/> (selftest, report).</summary>
        public static void MinionsOf(EntityManager em, Entity owner, List<Entity> into)
        {
            into.Clear();
            var q = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MinionCD>(), ComponentType.ReadOnly<OwnerReferenceCD>() },
                None = new[] { ComponentType.ReadOnly<EntityDestroyedCD>() },
            });
            var all = q.ToEntityArray(Allocator.Temp);
            foreach (var e in all)
                if (em.GetComponentData<OwnerReferenceCD>(e).owner == owner) into.Add(e);
            all.Dispose();
            q.Dispose();
        }
    }
}
