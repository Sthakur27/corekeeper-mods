using Pug.Automation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace FuelPower.Systems
{
    // Uses the same query/group as FasterMachines. This is a diagnostic sampling point AFTER crafting,
    // not a safe place to prevent crafting. It intentionally performs no entity or inventory writes.
    [UpdateInGroup(typeof(PugAutomationCraftingSystemGroup))]
    [UpdateAfter(typeof(PugAutomationFinishCraftingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class FurnaceProbeSystem : PugSimulationSystemBase
    {
        private EntityQuery crafters;
        private double nextReport;
        private int reports;

        protected override void OnCreate()
        {
            base.OnCreate();
            crafters = GetEntityQuery(ComponentType.ReadOnly<CrafterForSlotCD>(),
                ComponentType.ReadOnly<BigEntityRefCD>(), ComponentType.ReadOnly<PugTimerRefCD>());
            RequireForUpdate(crafters);
        }

        protected override void OnUpdate()
        {
            // Bound logging and scans: at most twelve reports, no more than once every five seconds.
            if (reports >= 12 || World.Time.ElapsedTime < nextReport) return;
            nextReport = World.Time.ElapsedTime + 5;
            int found = 0;
            using (var entities = crafters.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var big = EntityManager.GetComponentData<BigEntityRefCD>(entity).Value;
                    if (big == Entity.Null || !EntityManager.Exists(big)
                        || !EntityManager.HasComponent<ObjectDataCD>(big)
                        || EntityManager.GetComponentData<ObjectDataCD>(big).objectID != ObjectID.Furnace
                        || !EntityManager.HasBuffer<CraftingTimerSlotBuffer>(big)) continue;
                    int slot = EntityManager.GetComponentData<CrafterForSlotCD>(entity).slotIndex;
                    var timers = EntityManager.GetBuffer<CraftingTimerSlotBuffer>(big, true);
                    if ((uint)slot >= (uint)timers.Length) continue;
                    if (found++ >= 8) continue;
                    Debug.Log("[" + FuelPowerMod.Name + "] furnace=" + big + " slot=" + slot
                        + " remaining=" + timers[slot].timeLeftToCraft + " (observation only)");
                }
            }
            if (found > 0) reports++;
        }
    }
}
