using Unity.Entities;
using Unity.NetCode;

namespace BetterFishingLoot.Systems
{
    /// <summary>
    /// Runs in both the client and the server world. As soon as the world's LootTableBankCD singleton
    /// exists (right after authoring conversion, long before anyone can cast a rod) it multiplies the
    /// weights of the rare fishing loot entries inside the bank blob, and does so again whenever the
    /// setting changes. Both worlds apply the same multiplier so client prediction matches the server.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class FishingLootWeightSystem : PugSimulationSystemBase
    {
        private EntityQuery _bank;
        private int _appliedVersion = -1;
        private int _appliedBankHash;

        protected override void OnCreate()
        {
            base.OnCreate();
            _bank = GetEntityQuery(ComponentType.ReadOnly<LootTableBankCD>());
            RequireForUpdate(_bank);
        }

        protected override void OnUpdate()
        {
            if (!_bank.TryGetSingleton(out LootTableBankCD bankCD) || !bankCD.Value.IsCreated) return;

            int bankHash = bankCD.Value.GetHashCode();
            if (_appliedVersion == FishingLootConfig.Version && _appliedBankHash == bankHash) return;

            try
            {
                FishingLootPatcher.Apply(bankCD.Value, FishingLootConfig.Multiplier, World.Name);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[{BetterFishingLootMod.Name}] Failed to apply fishing loot weights in {World.Name}: {e}");
            }
            _appliedVersion = FishingLootConfig.Version;
            _appliedBankHash = bankHash;
        }
    }
}
