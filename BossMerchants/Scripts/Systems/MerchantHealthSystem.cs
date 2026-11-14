using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Server, every 3 s: sets the max health of every live merchant NPC (ours and the vanilla ones: anything
    /// with MerchantCD + HealthCD) to the "NPC max health" setting (<see cref="BossMerchantsConfig.HealthTarget"/>),
    /// or back to its prefab's value (100,000 for every merchant) when the setting is Default.
    ///
    /// HealthCD.maxHealth is the base value (buffs are added when the game reads it, see
    /// HealthCD.GetMaxHealthWithConditions) and both fields are ghost fields, so the server write reaches
    /// every client. Current health keeps its share of the max, so a merchant at full health stays full.
    /// Dead merchants (health 0) are left alone. The prefab is never changed, so it always holds the vanilla
    /// value, and switching back to Default restores it.
    /// Read-only in steady state: write access is only taken when a merchant's max health has to change.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantHealthSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 3f;
        // Staggered start so the merchant mods' timers don't share a tick.
        private float _timer = 0.9f;
        private EntityQuery _merchants;
        private readonly Dictionary<ObjectID, int> _baseMax = new Dictionary<ObjectID, int>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadOnly<HealthCD>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            if (!World.IsServer()) return;
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmpty) return;

            int setting = BossMerchantsConfig.HealthTarget;
            var entities = _merchants.ToEntityArray(Allocator.Temp);
            // Prefabs first: they hold the vanilla max health.
            for (int pass = 0; pass < 2; pass++)
            {
                bool prefabPass = pass == 0;
                for (int i = 0; i < entities.Length; i++)
                {
                    var e = entities[i];
                    if (EntityManager.HasComponent<Prefab>(e) != prefabPass) continue;
                    var id = EntityManager.GetComponentData<ObjectDataCD>(e).objectID;
                    var hp = EntityManager.GetComponentData<HealthCD>(e);
                    if (prefabPass)
                    {
                        if (!_baseMax.ContainsKey(id) && hp.maxHealth > 0)
                        {
                            _baseMax[id] = hp.maxHealth;
                            Debug.Log($"[{BossMerchantsMod.Name}] {id} base max health {hp.maxHealth}.");
                        }
                        continue;
                    }
                    if (!_baseMax.TryGetValue(id, out int baseMax) || hp.health <= 0) continue;
                    int target = setting > 0 ? setting : baseMax;
                    if (hp.maxHealth == target) continue;
                    int old = math.max(1, hp.maxHealth);
                    hp.health = math.clamp((int)math.round((double)hp.health * target / old), 1, target);
                    hp.maxHealth = target;
                    EntityManager.SetComponentData(e, hp);
                    Debug.Log($"[{BossMerchantsMod.Name}] {id} {e}: max health {old} -> {target}.");
                }
            }
            entities.Dispose();
        }
    }
}
