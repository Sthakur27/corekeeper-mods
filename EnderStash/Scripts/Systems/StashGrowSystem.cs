using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace EnderStash.Systems
{
    /// <summary>
    /// Server: characters loaded from saves made before the mod have a shorter buffer; grow it so the
    /// stash slots exist (empty) and get saved from then on. Also runs <see cref="StashGuard"/>.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class StashGrowSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>());
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!StashLayout.Ready) return;
            var entities = _players.ToEntityArray(Allocator.Temp);
            var ghosts = _players.ToComponentDataArray<PlayerGhost>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                // Grows buffers from older saves, moves a stash whose slots shifted (StashGuard) and keeps
                // the layout marker after the stash. Checked read-only first: a read-write GetBuffer waits for
                // every job using ContainedObjectsBuffer, and in steady state there is nothing to write.
                if (!StashGuard.NeedsRelocate(EntityManager.GetBuffer<ContainedObjectsBuffer>(entities[i], true), ghosts[i].playerGuid)) continue;
                StashGuard.Relocate(EntityManager.GetBuffer<ContainedObjectsBuffer>(entities[i]), ghosts[i].playerGuid);
            }
            entities.Dispose();
            ghosts.Dispose();
        }
    }
}
