using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FiveLoadouts.Systems
{
    /// <summary>
    /// Fix-up for characters that already exist in a save.
    ///
    /// The game rebuilds every loaded entity from its prefab and then copies the saved
    /// components over it. EquipmentPresetsBuffer is not saved, so loaded characters get the
    /// prefab's five entries automatically. ContainedObjectsBuffer IS saved, at its old length,
    /// so the server grows it to cover our slots (the client receives it through replication).
    /// Should a player entity ever carry only three presets (a defensive case), both worlds
    /// append the prefab-time entries from the same static table, so they still agree.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class PresetMigrationSystem : PugSimulationSystemBase
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
            if (!PresetLayout.Ready) return;

            var entities = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                if (_isServer)
                {
                    var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(entity);
                    while (contained.Length <= PresetLayout.MaxIndex)
                        contained.Add(default);
                }

                var presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity);
                if (presets.Length == PresetLayout.VanillaPresetCount)
                {
                    for (int p = presets.Length; p < PresetLayout.PresetCount; p++)
                        presets.Add(new EquipmentPresetsBuffer { equipment = PresetLayout.PresetEquipment(p) });
                }
            }
            entities.Dispose();
        }
    }
}
