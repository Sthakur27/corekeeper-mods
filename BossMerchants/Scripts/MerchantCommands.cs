using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BossMerchants
{
    /// <summary>Explicit host-only cleanup; no polling and no client-to-server mutation RPC.</summary>
    public sealed class MerchantCommand : IClientCommandHandler
    {
        public string[] GetTriggerNames() => new[] { "morenpcs" };
        public string GetDescription() => "/morenpcs killall - remove ALL More NPCs merchants in the host's loaded world, including earned merchants. Vanilla NPCs and idols stay.";

        public CommandOutput Execute(string[] args)
        {
            if (args.Length != 1 || args[0] != "killall") return new CommandOutput(GetDescription());
            if (Manager.ecs == null || Manager.ecs.ServerWorld == null || !Manager.ecs.ServerWorld.IsCreated
                || !Manager.ecs.ServerWorld.IsServer() || Manager.main == null || Manager.main.player == null
                || Manager.load.IsLoading())
                return new CommandOutput("Run this command in a loaded world as the host (or in single player).", CommandStatus.Error);

            Catalog.Resolve(final: false);
            var em = Manager.ecs.ServerWorld.EntityManager;
            int count = 0;
            // Include disabled chunks, but never prefabs. Match catalog IDs as well as MerchantCD.
            using (var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MerchantCD>(), ComponentType.ReadOnly<ObjectDataCD>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            }))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var id = em.GetComponentData<ObjectDataCD>(entity).objectID;
                    if (id == ObjectID.None || Catalog.ByMerchantId(id) == null
                        || !em.HasComponent<EntityDestroyedCD>(entity) || em.IsComponentEnabled<EntityDestroyedCD>(entity)) continue;
                    // Let native destruction handle graphics, networking and entity cleanup.
                    // No raw DestroyEntity, health edits, loot farming or prefab changes.
                    em.SetComponentEnabled<EntityDestroyedCD>(entity, true);
                    count++;
                }
            }
            return new CommandOutput("Queued removal of " + count + " More NPCs merchants. Vanilla NPCs and idols are unchanged; valid idol rooms can summon replacements.");
        }
    }
}
