using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace MerchantRestock.Systems
{
    /// <summary>
    /// Server only. The game keeps each merchant's restock countdown in ObjectDataCD.amount (seconds):
    /// MerchantBuyInventorySystem (Burst) subtracts the elapsed time every 4 s and, once it drops below 1,
    /// refills the shelves and sets it to a random 1500-2100. Every second this system scales any countdown
    /// above the merchant's cap into 0..cap, keeping the game's random spread (5 min = about 3.5-5 min).
    /// Countdowns already at or below the cap are left alone, so it acts once per restock (and once when a
    /// setting is lowered) and the countdown itself stays vanilla.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantRestockSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 1f;

        private EntityQuery _merchants;
        private float _timer;

        protected override void OnCreate()
        {
            base.OnCreate();
            _merchants = GetEntityQuery(ComponentType.ReadOnly<MerchantCD>(), ComponentType.ReadWrite<ObjectDataCD>());
        }

        protected override void OnUpdate()
        {
            if (!World.IsServer()) return;
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmptyIgnoreFilter) return;

            var entities = _merchants.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var od = EntityManager.GetComponentData<ObjectDataCD>(entities[i]);
                int cap = RestockSettings.CapFor(od.objectID);
                if (cap <= 0 || od.amount <= cap) continue;
                od.amount = math.max(1, (int)((long)math.min(od.amount, RestockSettings.VanillaMaxSeconds) * cap / RestockSettings.VanillaMaxSeconds));
                EntityManager.SetComponentData(entities[i], od);
            }
            entities.Dispose();
        }
    }
}
