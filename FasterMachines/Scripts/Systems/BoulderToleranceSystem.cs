using System.Collections.Generic;
using Pug.Automation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FasterMachines.Systems
{
    /// <summary>
    /// Ore boulders resist fast drilling: each second the game sums the raw drill damage a boulder took and scales the
    /// next second's hits by <c>damageDecreaseFactor ^ (total * damageDecreaseExp)</c> (<c>MineableDamageDecreaseCD</c>,
    /// vanilla 0.9 ^ (total * 0.0137)). Past ~700 damage per second more drilling gives LESS ore, and at high drill
    /// speeds every hit rounds to 0. Dividing the exponent by the drill multiplier gives N-times-faster drills the same
    /// factor vanilla drills get, so ore output really scales N times. The vanilla exponent comes from each boulder's
    /// prefab (<see cref="Record"/>), so 1x restores it exactly. Bounded work: one pass over loaded boulders every two
    /// seconds (new chunks) and on setting changes; writes only when a value differs.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class BoulderToleranceSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 2f;
        // Vanilla damageDecreaseExp per object type, read from the prefabs.
        private static readonly Dictionary<ObjectID, float> VanillaExp = new Dictionary<ObjectID, float>();

        private EntityQuery _boulders;
        private float _timer;
        private int _appliedVersion = -1;
        private bool _logged;

        /// <summary>Called for every prefab (API.Authoring.OnObjectTypeAdded).</summary>
        public static void Record(Entity entity, GameObject authoring, EntityManager em)
        {
            if (!em.HasComponent<MineableDamageDecreaseCD>(entity) || !em.HasComponent<ObjectDataCD>(entity)) return;
            VanillaExp[em.GetComponentData<ObjectDataCD>(entity).objectID] = em.GetComponentData<MineableDamageDecreaseCD>(entity).damageDecreaseExp;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _boulders = GetEntityQuery(ComponentType.ReadWrite<MineableDamageDecreaseCD>(), ComponentType.ReadOnly<ObjectDataCD>());
            RequireForUpdate(_boulders);
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f && _appliedVersion == MachineSpeeds.Version) return;
            _timer = IntervalSeconds;
            _appliedVersion = MachineSpeeds.Version;
            if (VanillaExp.Count == 0) return;

            float mult = MachineSpeeds.Get(Machine.Drill);
            var entities = _boulders.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                if (!VanillaExp.TryGetValue(EntityManager.GetComponentData<ObjectDataCD>(e).objectID, out float vanilla)) continue;
                var cd = EntityManager.GetComponentData<MineableDamageDecreaseCD>(e);
                float want = vanilla / mult;
                if (cd.damageDecreaseExp == want) continue;
                cd.damageDecreaseExp = want;
                EntityManager.SetComponentData(e, cd);
            }
            entities.Dispose();
            if (!_logged)
            {
                _logged = true;
                Debug.Log($"[{FasterMachinesMod.Name}] Ore boulder drill tolerance scaled with drill speed ({VanillaExp.Count} boulder types)");
            }
        }
    }
}
