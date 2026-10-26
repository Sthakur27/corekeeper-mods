using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace MushroomSlayer
{
    [UpdateInGroup(typeof(UpdateHealthSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(UpdateHealthFromBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class MushroomSlayerSystem : PugSimulationSystemBase
    {
        private EntityQuery _hits;
        private EntityQuery _time;
        private readonly List<NumberChange> _numbers = new List<NumberChange>();
        private readonly List<Entity> _traceTargets = new List<Entity>();
        private int _traceCount;
        private bool _announced;

        private struct NumberChange
        {
            public Entity Target;
            public Entity Attacker;
            public int Damage;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _hits = GetEntityQuery(ComponentType.ReadWrite<HealthChangeBuffer>());
            _time = GetEntityQuery(ComponentType.ReadOnly<NetworkTime>());
            RequireForUpdate(_hits);
        }

        protected override void OnUpdate()
        {
            if (!_announced)
            {
                _announced = true;
                Debug.Log("[MushroomSlayer:trace] system running in " + World.Name);
            }
            _traceTargets.Clear();
            _numbers.Clear();
            using (var buffers = _hits.ToEntityArray(Allocator.Temp))
            {
                foreach (var bufferEntity in buffers)
                {
                    // Read-only until a hit is rewritten: a writable GetBuffer syncs every
                    // reader of HealthChangeBuffer, and most ticks have nothing to change.
                    var hits = EntityManager.GetBuffer<HealthChangeBuffer>(bufferEntity, true);
                    bool writable = false;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var entry = hits[i];
                        var hit = entry.healthChange;
                        if (hit.amount >= 0 || !EntityManager.HasComponent<ObjectDataCD>(hit.entity) ||
                            !EntityManager.HasComponent<DamageReductionCD>(hit.entity)) continue;
                        var target = EntityManager.GetComponentData<ObjectDataCD>(hit.entity).objectID;
                        var armor = EntityManager.GetComponentData<DamageReductionCD>(hit.entity);
                        var original = hit;
                        bool rewritten = ArmorRule.Rewrite(ref hit, target, armor);
                        if (World.IsServer() && target == ObjectID.MushroomBrute && _traceCount < 80)
                        {
                            _traceCount++;
                            var state = EntityManager.HasComponent<StateInfoCD>(hit.entity)
                                ? EntityManager.GetComponentData<StateInfoCD>(hit.entity).currentState.ToString() : "unknown";
                            bool chargeConnected = EntityManager.HasComponent<ChargeAttackStateCD>(hit.entity) &&
                                EntityManager.GetComponentData<ChargeAttackStateCD>(hit.entity).chargeConnected;
                            var attacker = EntityManager.HasComponent<ObjectDataCD>(hit.causedByEntity)
                                ? EntityManager.GetComponentData<ObjectDataCD>(hit.causedByEntity).objectID.ToString() : hit.causedByEntity.ToString();
                            int health = EntityManager.HasComponent<HealthCD>(hit.entity)
                                ? EntityManager.GetComponentData<HealthCD>(hit.entity).health : -1;
                            Debug.Log("[MushroomSlayer:trace] #" + _traceCount + " target=" + hit.entity +
                                " hp=" + health + " source=" + attacker + " queued=" + original.amount +
                                " cap=" + armor.maxDamagePerHit + " flat=" + armor.reduction +
                                " bypassCap=" + original.bypassMaxDamagePerHit + " bypassFlat=" + original.bypassDamageReduction +
                                " kill=" + original.wasKilled + " state=" + state + " wallHit=" + chargeConnected +
                                " rewritten=" + rewritten + " result=" + hit.amount);
                            _traceTargets.Add(hit.entity);
                        }
                        if (!rewritten) continue;
                        if (!writable)
                        {
                            hits = EntityManager.GetBuffer<HealthChangeBuffer>(bufferEntity);
                            writable = true;
                        }
                        entry.healthChange = hit;
                        hits[i] = entry;
                        _numbers.Add(new NumberChange { Target = hit.entity, Attacker = hit.causedByEntity, Damage = -hit.amount });
                    }
                }
            }
            CorrectPlayerNumbers();
        }

        public void TraceAfterHealth()
        {
            foreach (var target in _traceTargets)
                Debug.Log("[MushroomSlayer:trace] after target=" + target + " hp=" +
                    (EntityManager.HasComponent<HealthCD>(target) ? EntityManager.GetComponentData<HealthCD>(target).health.ToString() : "removed"));
            _traceTargets.Clear();
        }

        private void CorrectPlayerNumbers()
        {
            if (_numbers.Count == 0 || _time.IsEmptyIgnoreFilter) return;
            var tick = _time.GetSingleton<NetworkTime>().ServerTick;
            var visited = new HashSet<Entity>();
            foreach (var number in _numbers)
            {
                var target = number.Target;
                if (!visited.Add(target) || !EntityManager.HasBuffer<GhostEffectEventBuffer>(target) ||
                    !EntityManager.HasComponent<GhostEffectEventBufferPointerCD>(target)) continue;
                var effects = EntityManager.GetBuffer<GhostEffectEventBuffer>(target);
                if (effects.Length == 0) continue;
                int next = EntityManager.GetComponentData<GhostEffectEventBufferPointerCD>(target).NextIndex;
                // The ring holds only the latest events. Match newest-to-newest so
                // multiple hits in one tick and overwritten ring entries stay aligned.
                int search = _numbers.Count - 1;
                for (int n = 0; n < effects.Length; n++)
                {
                    int index = (next - 1 - n + effects.Length * 2) % effects.Length;
                    var effect = effects[index];
                    if (effect.Tick != tick || effect.value.entity != target ||
                        (effect.value.effectID != EffectID.WhiteDamageNumber && effect.value.effectID != EffectID.CritNumber)) continue;
                    // PlayerCommand writes its numbers on the victim. Helper-based
                    // creature/pet attacks write on their attacker and already cap damage.
                    for (int j = search; j >= 0; j--)
                    {
                        var change = _numbers[j];
                        if (change.Target != target || change.Attacker != effect.value.entity2) continue;
                        effect.value.value1 = change.Damage;
                        effects[index] = effect;
                        search = j - 1;
                        break;
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(UpdateHealthSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(UpdateHealthFromBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MushroomSlayerTraceAfterSystem : PugSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            World.GetExistingSystemManaged<MushroomSlayerSystem>()?.TraceAfterHealth();
        }
    }
}
