using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace InfiniteOreBoulders.Systems
{
    /// <summary>
    /// How vanilla ore boulders work (UpdateHealthFromBufferSystem + DropsLootWhenDamagedCD): every time
    /// a hit takes the boulder's health across another multiple of damageToDealToDropLoot, one ore drops;
    /// at 0 health the boulder is destroyed.
    ///
    /// This system runs on the server right before UpdateHealthFromBufferSystem and, for every ore boulder:
    ///  - refills its health to max, and
    ///  - caps this tick's queued damage (HealthChangeBuffer) so it cannot reach 0 from full health.
    /// The game then applies the damage and drops ore exactly as usual, so ore per damage dealt is vanilla,
    /// but the boulder never breaks. Ore boulders are found by ObjectID name ("...OreBoulder"), so new
    /// boulder types are covered automatically.
    /// </summary>
    [UpdateInGroup(typeof(UpdateHealthSystemGroup))]
    [UpdateBefore(typeof(UpdateHealthFromBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class InfiniteOreBouldersSystem : PugSimulationSystemBase
    {
        private EntityQuery _targets;
        private readonly Dictionary<int, bool> _isBoulder = new Dictionary<int, bool>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _targets = GetEntityQuery(
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<DropsLootWhenDamagedCD>(),
                ComponentType.ReadWrite<HealthCD>());
            RequireForUpdate(_targets);
        }

        private bool IsBoulder(ObjectID id)
        {
            int key = (int)id;
            if (!_isBoulder.TryGetValue(key, out bool yes))
            {
                yes = id.ToString().EndsWith("OreBoulder", StringComparison.Ordinal);
                _isBoulder[key] = yes;
            }
            return yes;
        }

        protected override void OnUpdate()
        {
            var entities = _targets.ToEntityArray(Allocator.Temp);
            var objects = _targets.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            var healths = _targets.ToComponentDataArray<HealthCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!IsBoulder(objects[i].objectID)) continue;
                var health = healths[i];
                if (health.maxHealth <= 1) continue;

                if (health.health != health.maxHealth)
                {
                    health.health = health.maxHealth;
                    EntityManager.SetComponentData(entities[i], health);
                }

                if (!EntityManager.HasBuffer<HealthChangeBuffer>(entities[i])) continue;
                var changes = EntityManager.GetBuffer<HealthChangeBuffer>(entities[i]);
                int budget = health.maxHealth - 1; // damage this tick may take it down to 1, never 0
                for (int c = 0; c < changes.Length; c++)
                {
                    var entry = changes[c];
                    var hc = entry.healthChange;
                    if (hc.amount >= 0 && !hc.wasKilled) continue;
                    int damage = hc.wasKilled ? budget : Math.Min(-hc.amount, budget);
                    hc.amount = -damage;
                    hc.wasKilled = false;
                    budget -= damage;
                    entry.healthChange = hc;
                    changes[c] = entry;
                }
            }
            entities.Dispose();
            objects.Dispose();
            healths.Dispose();
        }
    }
}
