using System.Collections.Generic;
using Pug.Automation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FasterMachines.Systems
{
    /// <summary>
    /// Every timed machine (furnaces, kilns, cooking pot, table saw, egg incubator, ...) keeps one
    /// <c>CraftingTimerSlotBuffer.timeLeftToCraft</c> (seconds) per slot. The Burst automation systems set it to the
    /// item's full crafting time when an item starts, then subtract 1 every time the slot's PugTimer fires
    /// (once a second, or after <c>min(1, timeLeft)</c> seconds for the first tick).
    ///
    /// This server system runs right after those systems each tick. A value it did not expect (anything but the
    /// value it saw last tick, or that minus the 1-second decrement) means a fresh item, so it divides that value by
    /// the machine's multiplier. When less than a second is left it also shortens the pending PugTimer, so crafts
    /// can finish faster than once a second ("Instant" = one tick per item).
    /// </summary>
    [UpdateInGroup(typeof(PugAutomationCraftingSystemGroup))]
    [UpdateAfter(typeof(PugAutomationFinishCraftingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class CraftTimerSpeedSystem : PugSimulationSystemBase
    {
        private const float Epsilon = 0.001f;
        private const float MinSeconds = 0.0001f;

        private EntityQuery _crafters;
        private EntityQuery _tickRate;
        private readonly Dictionary<Entity, float> _last = new Dictionary<Entity, float>();
        private readonly HashSet<ObjectID> _logged = new HashSet<ObjectID>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _crafters = GetEntityQuery(
                ComponentType.ReadOnly<CrafterForSlotCD>(),
                ComponentType.ReadOnly<BigEntityRefCD>(),
                ComponentType.ReadOnly<PugTimerRefCD>());
            _tickRate = GetEntityQuery(ComponentType.ReadOnly<ClientServerTickRate>());
            RequireForUpdate(_crafters);
        }

        protected override void OnUpdate()
        {
            if (!MachineSpeeds.AnyTimedChanged)
            {
                _last.Clear();
                return;
            }
            int rate = 60;
            if (!_tickRate.IsEmpty)
            {
                int r = _tickRate.GetSingleton<ClientServerTickRate>().SimulationTickRate;
                if (r > 0) rate = r;
            }

            var entities = _crafters.ToEntityArray(Allocator.Temp);
            var slots = _crafters.ToComponentDataArray<CrafterForSlotCD>(Allocator.Temp);
            var bigRefs = _crafters.ToComponentDataArray<BigEntityRefCD>(Allocator.Temp);
            var timerRefs = _crafters.ToComponentDataArray<PugTimerRefCD>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                Entity big = bigRefs[i].Value;
                if (big == Entity.Null || !EntityManager.Exists(big) || !EntityManager.HasComponent<ObjectDataCD>(big)
                    || !EntityManager.HasBuffer<CraftingTimerSlotBuffer>(big))
                    continue;

                int slot = slots[i].slotIndex;
                var timers = EntityManager.GetBuffer<CraftingTimerSlotBuffer>(big, true);
                if ((uint)slot >= (uint)timers.Length) continue;

                ObjectID id = EntityManager.GetComponentData<ObjectDataCD>(big).objectID;
                float mult = MachineSpeeds.For(id);
                float v = timers[slot].timeLeftToCraft;

                bool continuing = _last.TryGetValue(e, out float last)
                    && (Mathf.Abs(v - last) < Epsilon || Mathf.Abs(v - (last - 1f)) < Epsilon);
                if (!continuing && v > 0f)
                {
                    LogOnce(id, v);
                    if (mult != 1f)
                    {
                        v = Mathf.Max(v / mult, MinSeconds);
                        var rw = EntityManager.GetBuffer<CraftingTimerSlotBuffer>(big);
                        var t = rw[slot];
                        t.timeLeftToCraft = v;
                        rw[slot] = t;
                    }
                }
                _last[e] = v;

                // Under a second left: the pending timer may still be set to a full second (it was started before we
                // shortened the value). Pull it in so the item finishes on time.
                if (mult != 1f && v > 0f && v < 1f)
                {
                    Entity timer = timerRefs[i].entity;
                    if (timer != Entity.Null && EntityManager.Exists(timer) && EntityManager.HasComponent<PugTimerCD>(timer))
                    {
                        var pt = EntityManager.GetComponentData<PugTimerCD>(timer);
                        int maxTicks = Mathf.Max(1, Mathf.CeilToInt(v * rate));
                        if (pt.timer > maxTicks)
                        {
                            pt.timer = maxTicks;
                            EntityManager.SetComponentData(timer, pt);
                        }
                    }
                }
            }

            // Forget crafters that no longer exist (machine picked up, chunk unloaded).
            if (_last.Count > entities.Length + 64)
            {
                var alive = new HashSet<Entity>();
                foreach (var e in entities) alive.Add(e);
                var dead = new List<Entity>();
                foreach (var kv in _last) if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
                foreach (var e in dead) _last.Remove(e);
            }

            entities.Dispose();
            slots.Dispose();
            bigRefs.Dispose();
            timerRefs.Dispose();
        }

        private void LogOnce(ObjectID id, float vanillaSeconds)
        {
            if (!_logged.Add(id)) return;
            Machine m = MachineSpeeds.Of(id);
            Debug.Log($"[{FasterMachinesMod.Name}] {id} ({m}) timer {vanillaSeconds:0.##}s at {MachineSpeeds.Describe(MachineSpeeds.Get(m))}");
        }
    }
}
