using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace LoadoutSharing.Systems
{
    /// <summary>
    /// Every frame, points each loadout's preset table at the effective slots (own item if the
    /// loadout has one, else loadout 1's). The game copies the active preset into EquipmentCD,
    /// so stats, armor visuals, bag size and pet all follow the fallback rule automatically.
    /// Runs in both worlds so the client and server agree. The server additionally grows the
    /// contained-objects buffer for characters loaded from older saves.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class LoadoutLayoutSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;
        private bool _isServer;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadWrite<EquipmentPresetsBuffer>(),
                ComponentType.ReadWrite<ContainedObjectsBuffer>());
            _isServer = World.IsServer();
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!SlotLayout.Ready) return;

            var entities = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity);
                if (presets.Length < SlotLayout.PresetCount) continue;

                var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(entity);
                if (_isServer)
                {
                    while (contained.Length <= SlotLayout.MaxIndex)
                        contained.Add(default);
                }

                for (int p = 0; p < SlotLayout.PresetCount; p++)
                {
                    var e = presets[p].equipment;
                    if (SlotLayout.ApplyTo(ref e, p, contained))
                        presets[p] = new EquipmentPresetsBuffer { equipment = e };
                }
            }
            entities.Dispose();
        }
    }
}
