using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace LoadoutSharing.Systems
{
    /// <summary>
    /// Every frame, points each loadout's preset table at the effective slots (own item if the
    /// loadout has one, else the nearest lower loadout's). The game copies the active preset into EquipmentCD,
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
                ComponentType.ReadOnly<EquipmentPresetsBuffer>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>());
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
                // Buffers are read read-only and only taken read-write when something changes: a read-write
                // GetBuffer waits for every job using the buffer type (all chest inventories for contained).
                var presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity, true);
                if (presets.Length < SlotLayout.VanillaPresetCount) continue;
                int presetCount = System.Math.Min(SlotLayout.PresetCount, presets.Length);

                if (_isServer && EntityManager.GetBuffer<ContainedObjectsBuffer>(entity, true).Length <= SlotLayout.MaxIndex)
                {
                    var grow = EntityManager.GetBuffer<ContainedObjectsBuffer>(entity);
                    while (grow.Length <= SlotLayout.MaxIndex)
                        grow.Add(default);
                }
                var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(entity, true);

                bool writable = false;
                for (int p = 0; p < presetCount; p++)
                {
                    var e = presets[p].equipment;
                    if (SlotLayout.ApplyTo(ref e, p, contained))
                    {
                        if (!writable)
                        {
                            presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity);
                            writable = true;
                        }
                        presets[p] = new EquipmentPresetsBuffer { equipment = e };
                    }
                }
            }
            entities.Dispose();
        }
    }
}
