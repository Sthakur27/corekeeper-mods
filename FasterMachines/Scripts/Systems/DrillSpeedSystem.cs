using System.Collections.Generic;
using Pug.Automation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FasterMachines.Systems
{
    /// <summary>
    /// Drills: each active drill gets a small <c>MinerCD</c> entity that hits its target tile for <c>damage</c> every
    /// <c>cooldown + 1</c> automation ticks. The vanilla values stay on the drill itself
    /// (<c>PugAutomationMinerConfigCD</c>, copied when the miner is created), so this server system recomputes the
    /// miner from those: first a shorter cooldown, and whatever the cooldown can't deliver (it can't go below 0)
    /// as extra damage per hit. Runs every half second (new drills) and immediately when the setting changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class DrillSpeedSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 0.5f;

        private EntityQuery _miners;
        private float _timer;
        private int _appliedVersion = -1;
        private readonly HashSet<ObjectID> _logged = new HashSet<ObjectID>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _miners = GetEntityQuery(ComponentType.ReadWrite<MinerCD>(), ComponentType.ReadOnly<BigEntityRefCD>());
            RequireForUpdate(_miners);
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f && _appliedVersion == MachineSpeeds.Version) return;
            _timer = IntervalSeconds;
            _appliedVersion = MachineSpeeds.Version;

            float mult = MachineSpeeds.Get(Machine.Drill);
            var entities = _miners.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                Entity drill = EntityManager.GetComponentData<BigEntityRefCD>(e).Value;
                if (drill == Entity.Null || !EntityManager.Exists(drill) || !EntityManager.HasComponent<PugAutomationMinerConfigCD>(drill))
                    continue;
                var cfg = EntityManager.GetComponentData<PugAutomationMinerConfigCD>(drill);
                LogOnce(drill, cfg);

                int period = Mathf.Max(1, cfg.cooldown + 1);
                int cooldown = Mathf.Max(0, Mathf.RoundToInt(period / mult) - 1);
                float rest = mult * (cooldown + 1) / period; // speed the cooldown couldn't give
                int damage = Mathf.Max(1, Mathf.RoundToInt(cfg.damage * rest));

                var miner = EntityManager.GetComponentData<MinerCD>(e);
                if (miner.cooldown == cooldown && miner.damage == damage && miner.timer <= cooldown) continue;
                miner.cooldown = cooldown;
                miner.damage = damage;
                if (miner.timer > cooldown) miner.timer = cooldown;
                EntityManager.SetComponentData(e, miner);
            }
            entities.Dispose();
        }

        private void LogOnce(Entity drill, PugAutomationMinerConfigCD cfg)
        {
            if (!EntityManager.HasComponent<ObjectDataCD>(drill)) return;
            ObjectID id = EntityManager.GetComponentData<ObjectDataCD>(drill).objectID;
            if (!_logged.Add(id)) return;
            Debug.Log($"[{FasterMachinesMod.Name}] Drill {id}: vanilla damage {cfg.damage} every {cfg.cooldown + 1} automation ticks; "
                + $"speed {MachineSpeeds.Describe(MachineSpeeds.Get(Machine.Drill))}");
        }
    }
}
