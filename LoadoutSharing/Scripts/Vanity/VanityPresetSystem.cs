using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace LoadoutSharing.Systems
{
    /// <summary>
    /// Keeps every player's VanitySlotsCD pointed at the active loadout's vanity slots, in both worlds
    /// (the indices are deterministic, so client and server agree without syncing). The UI and the
    /// character sprite read vanity through PlayerController.vanitySlotsHandler, which VanitySync
    /// re-points on the client; this component is kept consistent for anything else that reads it.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class VanityPresetSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<ActiveEquipmentPresetCD>(),
                ComponentType.ReadWrite<VanitySlotsCD>());
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!VanityLayout.Ready) return;
            var entities = _players.ToEntityArray(Allocator.Temp);
            var presets = _players.ToComponentDataArray<ActiveEquipmentPresetCD>(Allocator.Temp);
            var current = _players.ToComponentDataArray<VanitySlotsCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                int p = presets[i].Value;
                var want = new VanitySlotsCD
                {
                    helmVanitySlotIndex = VanityLayout.Slot(p, VanityKind.Helm),
                    breastVanitySlotIndex = VanityLayout.Slot(p, VanityKind.Breast),
                    pantsVanitySlotIndex = VanityLayout.Slot(p, VanityKind.Pants)
                };
                var c = current[i];
                if (c.helmVanitySlotIndex == want.helmVanitySlotIndex &&
                    c.breastVanitySlotIndex == want.breastVanitySlotIndex &&
                    c.pantsVanitySlotIndex == want.pantsVanitySlotIndex) continue;
                EntityManager.SetComponentData(entities[i], want);
            }
            entities.Dispose();
            presets.Dispose();
            current.Dispose();
        }
    }
}
