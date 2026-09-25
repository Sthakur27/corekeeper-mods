using PlayerEquipment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DurabilityMultiplier.Systems
{
    /// <summary>
    /// Runs right before the game's Burst ChangeDurabilitySystem (same group, EndPredictedSimulationSystemGroup)
    /// and rewrites the pending durability-loss requests on player entities before the game's jobs consume them:
    ///
    /// - Held item (ReduceDurabilityOfEquippedTriggerCD.triggerCounter, an integer loss): scaled with
    ///   stochastic rounding. counter * rate is split into a whole part, always applied, and a fractional
    ///   part applied with that probability. Expected loss is exactly counter * rate; a 0 result cancels
    ///   the request by disabling the trigger.
    /// - Armor on hit (ReduceDurabilityOfAllEquipmentTriggerCD damage/percentage): the game turns this into
    ///   a non-linear loss (min 1, a 3%-of-max-HP threshold), so it cannot be scaled through the data.
    ///   Instead each hit's armor loss is applied in full with probability rate, otherwise cancelled.
    ///
    /// Rolls use the player's own RandomCD, the replicated RNG the vanilla job rolls on too, so the
    /// client's prediction and the server agree and rollback re-simulation stays deterministic.
    /// At 1x this system does nothing at all; repairs (IncreaseDurabilityOfEquippedTriggerCD) are never touched.
    /// </summary>
    [UpdateInGroup(typeof(EndPredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(ChangeDurabilitySystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ScaleDurabilityLossSystem : PugSimulationSystemBase
    {
        private EntityQuery _heldTriggers;
        private EntityQuery _armorTriggers;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Enableable components only match while enabled, i.e. while a loss request is pending.
            // Simulate mirrors the vanilla job's filter (predicted/owned entities only on the client).
            _heldTriggers = GetEntityQuery(
                ComponentType.ReadWrite<ReduceDurabilityOfEquippedTriggerCD>(),
                ComponentType.ReadWrite<RandomCD>(),
                ComponentType.ReadOnly<Simulate>());
            _armorTriggers = GetEntityQuery(
                ComponentType.ReadWrite<ReduceDurabilityOfAllEquipmentTriggerCD>(),
                ComponentType.ReadWrite<RandomCD>(),
                ComponentType.ReadOnly<Simulate>());
            RequireAnyForUpdate(_heldTriggers, _armorTriggers);
        }

        protected override void OnUpdate()
        {
            float rate = DurabilityRate.Value;
            if (rate >= 1f) return; // vanilla: leave every request untouched, consume no randomness

            var held = _heldTriggers.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < held.Length; i++)
            {
                Entity player = held[i];
                var trigger = EntityManager.GetComponentData<ReduceDurabilityOfEquippedTriggerCD>(player);
                if (trigger.triggerCounter <= 0) continue;

                int keep = ScaleLoss(player, trigger.triggerCounter, rate);
                if (keep == trigger.triggerCounter) continue;

                trigger.triggerCounter = keep;
                EntityManager.SetComponentData(player, trigger);
                if (keep == 0)
                    EntityManager.SetComponentEnabled<ReduceDurabilityOfEquippedTriggerCD>(player, false);
            }
            held.Dispose();

            var armor = _armorTriggers.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < armor.Length; i++)
            {
                Entity player = armor[i];
                bool apply = rate > 0f && NextFloat(player) < rate;
                if (apply) continue;

                EntityManager.SetComponentData(player, default(ReduceDurabilityOfAllEquipmentTriggerCD));
                EntityManager.SetComponentEnabled<ReduceDurabilityOfAllEquipmentTriggerCD>(player, false);
            }
            armor.Dispose();
        }

        /// <summary>amount * rate with stochastic rounding: floor always, the remainder with its own probability.</summary>
        private int ScaleLoss(Entity player, int amount, float rate)
        {
            if (rate <= 0f) return 0;
            float scaled = amount * rate;
            int whole = (int)math.floor(scaled);
            float frac = scaled - whole;
            if (frac > 0f && NextFloat(player) < frac) whole++;
            return whole;
        }

        /// <summary>Advances the player's replicated RandomCD and returns a float in [0, 1).</summary>
        private float NextFloat(Entity player)
        {
            var randomCD = EntityManager.GetComponentData<RandomCD>(player);
            Unity.Mathematics.Random random = randomCD.Value;
            float value = random.NextFloat();
            randomCD.Value = random;
            EntityManager.SetComponentData(player, randomCD);
            return value;
        }
    }
}
